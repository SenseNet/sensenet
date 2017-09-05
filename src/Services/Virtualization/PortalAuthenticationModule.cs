using System;
using System.Linq;
using System.Reflection;
using System.Security.Authentication;
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
using SenseNet.Search;
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




        public bool DispatchBasicAuthentication(HttpContextBase context, out bool anonymAuthenticated)
        {
            anonymAuthenticated = false;

            var authHeader = AuthenticationHelper.GetBasicAuthHeader();
            if (authHeader == null || !authHeader.StartsWith("Basic "))
                return false;

            var base64Encoded = authHeader.Substring(6); // 6: length of "Basic "
            var bytes = Convert.FromBase64String(base64Encoded);
            string[] userPass = Encoding.UTF8.GetString(bytes).Split(":".ToCharArray());

            if (userPass.Length != 2)
            {
                context.User = AuthenticationHelper.GetVisitorPrincipal();
                anonymAuthenticated = true;
                return true;
            }
            try
            {
                var username = userPass[0];
                var password = userPass[1];

                // Elevation: we need to load the user here, regardless of the current users permissions
                using (AuthenticationHelper.GetSystemAccount())
                {
                    if (AuthenticationHelper.IsUserValid(username, password))
                    {
                        context.User = AuthenticationHelper.LoadUserPrincipal(username);
                    }
                    else
                    {
                        context.User = AuthenticationHelper.GetVisitorPrincipal();
                        anonymAuthenticated = true;
                    }
                }
            }
            catch (Exception e) // logged
            {
                SnLog.WriteException(e);
                context.User = AuthenticationHelper.GetVisitorPrincipal();
                anonymAuthenticated = true;
            }

            return true;
        }

        public void OnAuthenticateRequest(object sender, EventArgs e)
        {
            var application = sender as HttpApplication;
            var context = AuthenticationHelper.GetContext(sender); //HttpContext.Current;
            bool anonymAuthenticated;
            var basicAuthenticated = DispatchBasicAuthentication(context, out anonymAuthenticated);

            new TokenAuthentication().Authenticate(application, basicAuthenticated, anonymAuthenticated);

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
