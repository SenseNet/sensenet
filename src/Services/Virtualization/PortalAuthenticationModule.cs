using System;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Web.Security;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Security.ADSync;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using Newtonsoft.Json;
using SenseNet.Configuration;
using SenseNet.TokenAuthentication;

namespace SenseNet.Portal.Virtualization
{
    public class PortalAuthenticationModule : IHttpModule
    {
        public void Dispose()
        {
        }

        public void Init(HttpApplication application)
        {
            FormsAuthentication.Initialize();
            application.AuthenticateRequest += OnAuthenticateRequest;
            application.EndRequest += OnEndRequest; // Forms
        }

        private const string AccessSignatureName = "as";
        private const string RefreshSignatureName = "rs";
        private const string AccessHeaderName = "X-Access-Data";
        private const string RefreshHeaderName = "X-Refresh-Data";
        private const string AuthenticationTypeHeaderName = "X-Authentication-Type";
        private const string TokenLoginPath = "/sn-token/login";
        private const string TokenRefreshPath = "/sn-token/refresh";
        private static readonly string[] AcceptedTokenPaths = { TokenLoginPath, TokenRefreshPath };

        private static class HttpResponseStatusCode
        {
            public static int Unauthorized = 401;
            public static int Ok = 200;
        }
        private static ISecurityKey _securityKey;
        private static readonly object _keyLock = new object();

        private ISecurityKey SecurityKey
        {
            get
            {
                if (_securityKey == null)
                {
                    lock (_keyLock)
                    {
                        if (_securityKey == null)
                        {
                            _securityKey = EncryptionHelper.CreateSymmetricKey(Configuration.TokenAuthentication.SymmetricKeySecret);
                        }
                    }
                }
                return _securityKey;
            }
        }

        public Func<object, HttpContextBase> GetContext = sender => new HttpContextWrapper(((HttpApplication) sender).Context);
        public Func<object, HttpRequestBase> GetRequest = sender => new HttpRequestWrapper(((HttpApplication)sender).Context.Request);
        public Func<object, HttpResponseBase> GetResponse = sender => new HttpResponseWrapper(((HttpApplication)sender).Context.Response);
        public Func<IPrincipal> GetVisitorPrincipal = () => new PortalPrincipal(User.Visitor);
        public Func<string, IPrincipal> LoadUserPrincipal = userName => new PortalPrincipal(User.Load(userName));
        public Func<string, string, bool> IsUserValid = (userName, password) => Membership.ValidateUser(userName, password);
        public Func<IDisposable> GetSystemAccount = () => new SystemAccount();
        public Func<string> GetBasicAuthHeader = () => PortalContext.Current.BasicAuthHeaders;

        public bool DispatchBasicAuthentication(HttpContextBase context, out bool anonymAuthenticated)
        {
            anonymAuthenticated = false;

            var authHeader = GetBasicAuthHeader();
            if (authHeader == null || !authHeader.StartsWith("Basic "))
                return false;

            var base64Encoded = authHeader.Substring(6); // 6: length of "Basic "
            var bytes = Convert.FromBase64String(base64Encoded);
            string[] userPass = Encoding.UTF8.GetString(bytes).Split(":".ToCharArray());

            if (userPass.Length != 2)
            {
                context.User = GetVisitorPrincipal();
                anonymAuthenticated = true;
                return true;
            }
            try
            {
                var username = userPass[0];
                var password = userPass[1];

                // Elevation: we need to load the user here, regardless of the current users permissions
                using (GetSystemAccount())
                {
                    if (IsUserValid(username, password))
                    {
                        context.User = LoadUserPrincipal(username);
                    }
                    else
                    {
                        context.User = GetVisitorPrincipal();
                        anonymAuthenticated = true;
                    }
                }
            }
            catch (Exception e) // logged
            {
                SnLog.WriteException(e);
                context.User = GetVisitorPrincipal();
                anonymAuthenticated = true;
            }

            return true;
        }
        
        public void OnAuthenticateRequest(object sender, EventArgs e)
        {
            var application = sender as HttpApplication;
            var context = GetContext(sender); //HttpContext.Current;
            var request = GetRequest(sender);
            bool anonymAuthenticated;
            
            var basicAuthenticated = DispatchBasicAuthentication(context, out anonymAuthenticated);

            if (IsTokenAuthenticationRequested(request))
            {

                if (basicAuthenticated && anonymAuthenticated)
                {
                    SnLog.WriteException(new UnauthorizedAccessException("Invalid user."));
                    context.Response.StatusCode = HttpResponseStatusCode.Unauthorized;
                    context.Response.Flush();
                    if (application?.Context != null)
                    {
                        application.CompleteRequest();
                    }
                }
                else
                {
                    TokenAuthenticate(basicAuthenticated, context, application);
                }
                return;
            }
            // if it is a simple basic authentication case
            if (basicAuthenticated)
            {
                return;
            }

            string authenticationType = null;
            string repositoryPath = string.Empty;

            // Get the current PortalContext
            var currentPortalContext = PortalContext.Current;
            if (currentPortalContext != null)
                authenticationType = currentPortalContext.AuthenticationMode;

            // default authentication mode
            if (string.IsNullOrEmpty(authenticationType))
                authenticationType = WebApplication.DefaultAuthenticationMode;

            // if no site auth mode, no web.config default, then exception...
            if (string.IsNullOrEmpty(authenticationType))
                throw new ApplicationException(
                    "The engine could not determine the authentication mode for this request. This request does not belong to a site, and there was no default authentication mode set in the web.config.");

            switch (authenticationType)
            {
                case "Windows":
                    EmulateWindowsAuthentication(application);
                    SetApplicationUser(application, authenticationType);
                    break;
                case "Forms":
                    application.Context.User = null;
                    CallInternalOnEnter(sender, e);
                    SetApplicationUser(application, authenticationType);
                    break;
                case "None":
                    // "None" authentication: set the Visitor Identity
                    application.Context.User = new PortalPrincipal(User.Visitor);
                    break;
                default:
                    Site site = null;
                    var problemNode = Node.LoadNode(repositoryPath);
                    if (problemNode != null)
                    {
                        site = Site.GetSiteByNode(problemNode);
                        if (site != null)
                            authenticationType = site.GetAuthenticationType(application.Context.Request.Url);
                    }

                    var message = site == null
                        ? string.Format(
                            HttpContext.GetGlobalResourceObject("Portal", "DefaultAuthenticationNotSupported") as string,
                            authenticationType)
                        : string.Format(
                            HttpContext.GetGlobalResourceObject("Portal", "AuthenticationNotSupportedOnSite") as string,
                            site.Name, authenticationType);

                    throw new NotSupportedException(message);
            }
        }

        private string GetAuthenticationTypeHeader(HttpRequestBase request)
        {
            return request.Headers[AuthenticationTypeHeaderName] ?? request.Headers[AuthenticationTypeHeaderName.ToLower()];
        }

        private string GetRefreshHeader(HttpRequestBase request)
        {
            return request.Headers[RefreshHeaderName] ?? request.Headers[RefreshHeaderName.ToLower()];
        }
        private string GetAccessHeader(HttpRequestBase request)
        {
            return request.Headers[AccessHeaderName] ?? request.Headers[AccessHeaderName.ToLower()];
        }

        private bool IsTokenAuthenticationRequested(HttpRequestBase request)
        {
            return request.IsSecureConnection && (GetAuthenticationTypeHeader(request) == "Token"
                || AcceptedTokenPaths.Contains(request.Url.AbsolutePath,  StringComparer.InvariantCultureIgnoreCase)
                || !string.IsNullOrWhiteSpace(GetAccessHeader(request)));
        }

        private void TokenAuthenticate(bool basicAuthenticated, HttpContextBase context, HttpApplication application)
        {
            try
            {
                var tokenHandler = new JwsSecurityTokenHandler();
                var validFrom = DateTime.UtcNow;

                ITokenParameters generateTokenParameter = new TokenParameters
                {
                    Audience = Configuration.TokenAuthentication.Audience,
                    Issuer = Configuration.TokenAuthentication.Issuer,
                    Subject = Configuration.TokenAuthentication.Subject,
                    EncryptionAlgorithm = Configuration.TokenAuthentication.EncriptionAlgorithm,
                    AccessLifeTimeInMinutes = Configuration.TokenAuthentication.AccessLifeTimeInMinutes,
                    RefreshLifeTimeInMinutes = Configuration.TokenAuthentication.RefreshLifeTimeInMinutes,
                    ClockSkewInMinutes = Configuration.TokenAuthentication.ClockSkewInMinutes,
                    ValidFrom = validFrom,
                    ValidateLifeTime = true
                };

                var tokenManager = new TokenManager(SecurityKey, tokenHandler, generateTokenParameter);

                if (basicAuthenticated)
                {
                    // user has just authenticated by basic auth, so let's emit a set of tokens and cookies in response 
                    try
                    {
                        var userName = context.User.Identity.Name;
                        var roleName = string.Empty;

                        // emit both access and refresh token and cookie 
                        EmitTokensAndCookies(context, tokenManager, validFrom, userName, roleName, true);
                        context.Response.StatusCode = HttpResponseStatusCode.Ok;
                    }
                    catch (Exception ex)
                    {
                        SnLog.WriteException(ex);
                        context.Response.StatusCode = HttpResponseStatusCode.Unauthorized;
                    }
                    finally
                    {
                        context.Response.Flush();
                        if (application.Context != null)
                        {
                            application.CompleteRequest();
                        }
                    }
                    return;
                }

                // user has not been authenticated yet, so there must be a valid token and cookie in the request
                var header = GetRefreshHeader(context.Request);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    // we got a refresh token
                    try
                    {
                        var authCookie = CookieHelper.GetCookie(context.Request, RefreshSignatureName);
                        if (authCookie == null)
                        {
                            throw new UnauthorizedAccessException("Missing refresh cookie.");
                        }

                        var refreshHeadAndPayload = header;
                        var refreshSignature = authCookie.Value;
                        var principal = tokenManager.ValidateToken(refreshHeadAndPayload + "." + refreshSignature);
                        var userName = principal.Identity.Name;
                        var roleName = string.Empty;

                        // emit access token and cookie only
                        EmitTokensAndCookies(context, tokenManager, validFrom, userName, roleName, false);
                        context.Response.StatusCode = HttpResponseStatusCode.Ok;
                    }
                    catch (Exception ex)
                    {
                        SnLog.WriteException(ex);
                        context.Response.StatusCode = HttpResponseStatusCode.Unauthorized;
                    }
                    finally
                    {
                        context.Response.Flush();
                        if (application.Context != null)
                        {
                            application.CompleteRequest();
                        }
                    }
                    return;
                }

                header = GetAccessHeader(context.Request);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    // we got an access token
                    var authCookie = CookieHelper.GetCookie(context.Request, AccessSignatureName);
                    if (authCookie == null)
                    {
                        throw new UnauthorizedAccessException("Missing access cookie.");
                    }

                    var accessHeadAndPayload = header;
                    var accessSignature = authCookie.Value;
                    var principal = tokenManager.ValidateToken(accessHeadAndPayload + "." + accessSignature);
                    if (principal == null)
                    {
                        throw new UnauthorizedAccessException("Invalid access token.");
                    }
                    var userName = tokenManager.GetPayLoadValue(accessHeadAndPayload.Split(Convert.ToChar("."))[1], "name");
                    using (new SystemAccount())
                    {
                        context.User = LoadUserPrincipal(userName);
                    }
                }
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex);
                if (!basicAuthenticated)
                {
                    context.User = GetVisitorPrincipal();
                }
            }
        }

        private void EmitTokensAndCookies(HttpContextBase context, TokenManager tokenManager, DateTime validFrom, string userName, string roleName, bool refreshTokenAsWell)
        {
            string refreshToken;
            var token = tokenManager.GenerateToken(userName, roleName, out refreshToken, refreshTokenAsWell);
            var tokenResponse = new TokenResponse();
            var accessSignatureIndex = token.LastIndexOf('.');
            var accessSignature = token.Substring(accessSignatureIndex + 1);
            var accessHeadAndPayload = token.Substring(0, accessSignatureIndex);
            var accessExpiration = validFrom.AddMinutes(Configuration.TokenAuthentication.AccessLifeTimeInMinutes);

            CookieHelper.InsertSecureCookie(context.Response, accessSignature, AccessSignatureName, accessExpiration);

            tokenResponse.access = accessHeadAndPayload;

            if (refreshTokenAsWell)
            { 
                var refreshSignatureIndex = refreshToken.LastIndexOf('.');
                var refreshSignature = refreshToken.Substring(refreshSignatureIndex + 1);
                var refreshHeadAndPayload = refreshToken.Substring(0, refreshSignatureIndex);
                var refreshExpiration = accessExpiration.AddMinutes(Configuration.TokenAuthentication.RefreshLifeTimeInMinutes);

                CookieHelper.InsertSecureCookie(context.Response, refreshSignature, RefreshSignatureName, refreshExpiration);

                tokenResponse.refresh = refreshHeadAndPayload;
            }

            context.Response.Write(JsonConvert.SerializeObject(tokenResponse, new JsonSerializerSettings {DefaultValueHandling = DefaultValueHandling.Ignore}));
        }

        private class TokenResponse
        {
            public string access;
            public string refresh;
        }

        private static void CallInternalOnEnter(object sender, EventArgs e)
        {
            FormsAuthenticationModule formsAuthenticationModule = new FormsAuthenticationModule();
            MethodInfo formsAuthenticationModuleOnEnterMethodInfo = formsAuthenticationModule.GetType().GetMethod("OnEnter", BindingFlags.Instance | BindingFlags.NonPublic);
            formsAuthenticationModuleOnEnterMethodInfo.Invoke(
                formsAuthenticationModule,
                new object[] { sender, e });
        }

        private static void SetApplicationUser(HttpApplication application, string authenticationType)
        {
            if (application.User == null || !application.User.Identity.IsAuthenticated)
            {
                var visitor = User.Visitor;

                MembershipExtenderBase.Extend(visitor);

                var visitorPrincipal = new PortalPrincipal(visitor);
                application.Context.User = visitorPrincipal;
            }
            else
            {
                string domain, username, fullUsername;
                fullUsername = application.User.Identity.Name;
                int slashIndex = fullUsername.IndexOf('\\');
                if (slashIndex < 0)
                {
                    domain = string.Empty;
                    username = fullUsername;
                }
                else
                {
                    domain = fullUsername.Substring(0, slashIndex);
                    username = fullUsername.Substring(slashIndex + 1);
                }

                User user;

                if (authenticationType == "Windows")
                {
                    var widentity = application.User.Identity as WindowsIdentity;   // get windowsidentity object before elevation
                    using (new SystemAccount())
                    {
                        // force relational engine here, because index doesn't exist install time
                        user = User.Load(domain, username, ExecutionHint.ForceRelationalEngine);
                        if (user != null)
                            user.WindowsIdentity = widentity;

                        // create non-existing installer user
                        if (user == null && !string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(username))
                        {
                            application.Application.Add("SNInstallUser", fullUsername);

                            if (PortalContext.Current != null &&
                                PortalContext.Current.Site != null &&
                                Group.Administrators.Members.Count() == 1)
                            {
                                user = User.RegisterUser(fullUsername);
                            }
                        }
                    }

                    if (user != null)
                        AccessProvider.Current.SetCurrentUser(user);
                }
                else
                {
                    // if forms AD auth and virtual AD user is configured
                    // load virtual user properties from AD
                    var ADProvider = DirectoryProvider.Current;
                    if (ADProvider != null)
                    {
                        if (ADProvider.IsVirtualADUserEnabled(domain))
                        {
                            var virtualUserPath = "/Root/IMS/BuiltIn/Portal/VirtualADUser";
                            using (new SystemAccount())
                            {
                                user = Node.LoadNode(virtualUserPath) as User;
                            }

                            if (user != null)
                            {
                                user.SetProperty("Domain", domain);
                                user.Enabled = true;
                                ADProvider.SyncVirtualUserFromAD(domain, username, user);
                            }
                        }
                        else
                        {
                            using (new SystemAccount())
                            {
                                user = User.Load(domain, username);
                            }
                        }
                    }
                    else
                    {
                        using (new SystemAccount())
                        {
                            user = User.Load(domain, username);
                        }
                    }
                }

                // Current user will be the Visitor if the resolved user is not available
                if (user == null || !user.Enabled)
                    user = User.Visitor;

                MembershipExtenderBase.Extend(user);

                var appUser = new PortalPrincipal(user);
                application.Context.User = appUser;
            }
        }

        private void OnEndRequest(object sender, EventArgs e)
        {
            var application = sender as HttpApplication;
            var authType = application.Context.Items["AuthType"] as string;

            if (authType == "Forms")
            {
                var formsAuthenticationModule = new FormsAuthenticationModule();
                var formsAuthenticationModuleOnEnterMethodInfo =
                    formsAuthenticationModule.GetType().GetMethod("OnLeave", BindingFlags.Instance | BindingFlags.NonPublic);

                formsAuthenticationModuleOnEnterMethodInfo.Invoke(
                    formsAuthenticationModule,
                    new[] { sender, e });
            }

            SnTrace.Web.Write("PortalAuthenticationModule.OnEndRequest. Url:{0}, StatusCode:{1}",
                HttpContext.Current.Request.Url, HttpContext.Current.Response.StatusCode);
        }

        private void EmulateWindowsAuthentication(HttpApplication application)
        {
            WindowsIdentity identity = null;

            if (HttpRuntime.UsingIntegratedPipeline)
            {
                WindowsPrincipal user = null;
                if (HttpRuntime.IsOnUNCShare && application.Request.IsAuthenticated)
                {
                    var applicationIdentityToken = (IntPtr)typeof (System.Web.Hosting.HostingEnvironment)
                        .GetProperty("ApplicationIdentityToken", BindingFlags.NonPublic | BindingFlags.Static)
                        .GetGetMethod().Invoke(null, null);

                    var wi = new WindowsIdentity(
                        applicationIdentityToken, 
                        application.User.Identity.AuthenticationType,
                        WindowsAccountType.Normal, 
                        true);

                    user = new WindowsPrincipal(wi);
                }
                else
                {
                    user = application.Context.User as WindowsPrincipal;
                }

                if (user != null)
                {
                    identity = user.Identity as WindowsIdentity;

                    object[] setPrincipalNoDemandParameters = { null, false };
                    var setPrincipalNoDemandParameterTypes = new[] { typeof(IPrincipal), typeof(bool) };
                    var setPrincipalNoDemandMethodInfo = application.Context.GetType().GetMethod("SetPrincipalNoDemand", BindingFlags.Instance | BindingFlags.NonPublic, null, setPrincipalNoDemandParameterTypes, null);

                    setPrincipalNoDemandMethodInfo.Invoke(application.Context, setPrincipalNoDemandParameters);
                }
            }
            else
            {
                HttpWorkerRequest workerRequest =
                    (HttpWorkerRequest)application.Context.GetType().GetProperty("WorkerRequest", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true).Invoke(application.Context, null);

                string logonUser = workerRequest.GetServerVariable("LOGON_USER");
                string authType = workerRequest.GetServerVariable("AUTH_TYPE");

                if (logonUser == null) logonUser = string.Empty;
                if (authType == null) authType = string.Empty;

                if (logonUser.Length == 0 && authType.Length == 0 || authType.ToLower() == "basic")
                {
                    identity = WindowsIdentity.GetAnonymous();
                }
                else
                {
                    identity = new WindowsIdentity(workerRequest.GetUserToken(), authType, System.Security.Principal.WindowsAccountType.Normal, true);
                }
            }

            if (identity != null)
            {
                WindowsPrincipal wp = new WindowsPrincipal(identity);

                object[] setPrincipalNoDemandParameters = new object[] { wp, false };
                Type[] setPrincipalNoDemandParameterTypes = new Type[] { typeof(IPrincipal), typeof(bool) };
                MethodInfo setPrincipalNoDemandMethodInfo = application.Context.GetType().GetMethod("SetPrincipalNoDemand", BindingFlags.Instance | BindingFlags.NonPublic, null, setPrincipalNoDemandParameterTypes, null);
                setPrincipalNoDemandMethodInfo.Invoke(application.Context, setPrincipalNoDemandParameters);
            }

            // return 401 if user is not authenticated:
            //  - application.Context.User might be null for ContentStore GetTreeNodeAllChildren?... request
            //  - currentPortalUser.Id might be startupuserid or visitoruserid if browser did not send 'negotiate' auth header yet
            //  - currentPortalUser might be null if application.Context.User.Identity is null or not an IUser
            IUser currentPortalUser = null;
            if (application.Context.User != null)
                currentPortalUser = application.Context.User.Identity as IUser;

            if ((application.Context.User == null) || (currentPortalUser != null &&
                (currentPortalUser.Id == Identifiers.StartupUserId ||
                currentPortalUser.Id == Identifiers.VisitorUserId)))
            {
                if (!IsLocalAxdRequest())
                    AuthenticationHelper.DenyAccess(application);
            }

        }
        private bool IsLocalAxdRequest()
        {
            var req = HttpContext.Current.Request;
            if (!req.IsLocal)
                return false;

            var path = req.Url.LocalPath;
            if (String.Equals(path, "/webresource.axd", StringComparison.OrdinalIgnoreCase))
                return true;
            if (String.Equals(path, "/scriptresource.axd", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
    }
}
