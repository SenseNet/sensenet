using System;
using System.Security.Authentication;
using System.Web;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.TokenAuthentication;
using System.Linq;
using System.Security.Principal;
using SenseNet.ContentRepository.Security;
using SenseNet.Services.Virtualization;

namespace SenseNet.Portal.Virtualization
{
    public class TokenAuthentication
    {
        private const string AccessSignatureCookieName = "as";
        private const string RefreshSignatureCookieName = "rs";
        private const string AccessHeadAndPayloadCookieName = "ahp";
        private const string AccessHeaderName = "X-Access-Data";
        private const string RefreshHeaderName = "X-Refresh-Data";
        private const string AuthenticationActionHeaderName = "X-Authentication-Action";
        private const string LoginActionName = "TokenLogin";
        private const string LogoutActionName = "TokenLogout";
        private const string AccessActionName = "TokenAccess";
        private const string RefreshActionName = "TokenRefresh";
        private const string TokenLoginPath = "/sn-token/login";
        private const string TokenLogoutPath = "/sn-token/logout";
        private const string TokenRefreshPath = "/sn-token/refresh";
        private static readonly string[] TokenPaths = {TokenLoginPath, TokenLogoutPath, TokenRefreshPath };
        private static readonly string[] TokenActions = {LoginActionName, LogoutActionName, AccessActionName, RefreshActionName };
        private static class HttpResponseStatusCode
        {
            public static int Unauthorized = 401;
            public static int Ok = 200;
        }

        public TokenAuthentication(IUltimateLogoutProvider logoutProvider = null)
        {
            _logoutProvider = logoutProvider;
        }

        private enum TokenAction { TokenLogin, TokenLogout, TokenAccess, TokenRefresh }
        private static ISecurityKey _securityKey;
        private IUltimateLogoutProvider _logoutProvider;
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
                            _securityKey =
                                EncryptionHelper.CreateSymmetricKey(Configuration.TokenAuthentication.SymmetricKeySecret);
                        }
                    }
                }
                return _securityKey;
            }
        }


        public bool Authenticate(HttpApplication application, bool basicAuthenticated, bool anonymAuthenticated)
        {
            var context = AuthenticationHelper.GetContext(application); //HttpContext.Current;
            var request = AuthenticationHelper.GetRequest(application);
            bool headerMark, uriMark;
            string actionHeader, uri, accessHeadAndPayload;

            if (IsTokenAuthenticationRequested(request, out headerMark, out uriMark, out actionHeader, out uri, out accessHeadAndPayload))
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
                    TokenAuthenticate(basicAuthenticated, headerMark, uriMark, actionHeader, uri, accessHeadAndPayload, context, application);
                }
                return true;
            }
            return false;
        }

        private void TokenAuthenticate(bool basicAuthenticated, bool headerMark, bool uriMark, string actionHeader, string uri, string headAndPayLoad, HttpContextBase context, HttpApplication application)
        {
            bool endRequest = false;
            try
            {
                TokenAction tokenAction;
                string tokenHeadAndPayload = headAndPayLoad;
                if (headerMark)
                {
                    if (!Enum.TryParse(actionHeader, true, out tokenAction))
                    {
                        throw new AuthenticationException("Invalid action header for header mark token authentication.");
                    }
                    if (tokenAction == TokenAction.TokenAccess || tokenAction == TokenAction.TokenLogout)
                    {
                        tokenHeadAndPayload = GetAccessHeader(context.Request);
                    }
                    else if (tokenAction == TokenAction.TokenRefresh)
                    {
                        tokenHeadAndPayload = GetRefreshHeader(context.Request);
                    }
                }
                else if (uriMark)
                {
                    switch (uri)
                    {
                        case TokenLoginPath:
                            tokenAction = TokenAction.TokenLogin;
                            break;
                        case TokenLogoutPath:
                            tokenAction = TokenAction.TokenLogout;
                            break;
                        case TokenRefreshPath:
                            tokenAction = TokenAction.TokenRefresh;
                            break;
                        default:
                            throw new AuthenticationException("Invalid login uri for token authentication.");
                    }
                }
                else if (!string.IsNullOrWhiteSpace(headAndPayLoad))
                {
                    tokenAction = TokenAction.TokenAccess;
                }
                else
                {
                    throw new AuthenticationException("Invalid method for token authentication.");
                }

                var validFrom = DateTime.UtcNow;
                var tokenManager = GetTokenManager(validFrom);

                switch (tokenAction)
                {
                    case TokenAction.TokenLogin:
                        endRequest = true;
                        TokenLogin(basicAuthenticated, validFrom, tokenManager, context);
                        break;
                    case TokenAction.TokenLogout:
                        endRequest = true;
                        TokenLogout(tokenHeadAndPayload, tokenManager, context);
                        break;
                    case TokenAction.TokenAccess:
                        TokenAccess(tokenHeadAndPayload, tokenManager, context);
                        break;
                    case TokenAction.TokenRefresh:
                        endRequest = true;
                        TokenRefresh(tokenHeadAndPayload, validFrom, tokenManager, context);
                        break;
                }
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex);
                if (endRequest)
                {
                    context.Response.StatusCode = HttpResponseStatusCode.Unauthorized;
                }
                else
                {
                    context.User = AuthenticationHelper.GetVisitorPrincipal();
                }
            }
            finally
            {
                if (endRequest)
                {
                    context.Response.Flush();
                    if (application.Context != null)
                    {
                        application.CompleteRequest();
                    }
                }
            }
        }


        /// <summary>
        /// Assembles the necessary artifacts (cookies and response tokens). This should be called only 
        /// if the user is already authenticated.
        /// </summary>
        internal void TokenLogin(HttpContextBase context, HttpApplication application)
        {
            var validFrom = DateTime.UtcNow;
            var tokenManager = GetTokenManager(validFrom);

            TokenLogin(true, validFrom, tokenManager, context);
        }

        private TokenManager GetTokenManager(DateTime validFrom)
        {
            var tokenHandler = new JwsSecurityTokenHandler();

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

            return new TokenManager(SecurityKey, tokenHandler, generateTokenParameter);
        }

        private void TokenLogin(bool basicAuthenticated, DateTime validFrom, TokenManager tokenManager, HttpContextBase context)
        {
            if (!basicAuthenticated)
            {
                throw new AuthenticationException("Missing basic authentication.");
            }
            // user has just authenticated by basic auth, so let's emit a set of tokens and cookies in response 
            var userName = context.User.Identity.Name;
            var roleName = String.Empty;

            // emit both access and refresh token and cookie 
            EmitTokensAndCookies(context, tokenManager, validFrom, userName, roleName, true);
            context.Response.StatusCode = HttpResponseStatusCode.Ok;
        }

        private void TokenLogout(string accessHeadAndPayload, TokenManager tokenManager, HttpContextBase context)
        {
            if (!String.IsNullOrWhiteSpace(accessHeadAndPayload))
            {
                var authCookie = CookieHelper.GetCookie(context.Request, AccessSignatureCookieName);
                if (authCookie == null)
                {
                    throw new UnauthorizedAccessException("Missing access cookie.");
                }

                var accessSignature = authCookie.Value;
                var principal = tokenManager.ValidateToken(accessHeadAndPayload + "." + accessSignature, false);
                if (principal == null)
                {
                    throw new UnauthorizedAccessException("Invalid access token.");
                }
                if (!bool.TryParse(AuthenticationHelper.GetRequestParameterValue(context, "ultimateLogout"), out var ultimateLogout))
                {
                    ultimateLogout = Configuration.TokenAuthentication.DefaultUltimateLogout;
                }
                // ultimately log out only if the user has not been logged out already, if he has, just a local logout executes
                if (ultimateLogout)
                {
                    var userName = principal.Identity.Name;
                    ultimateLogout = !UserHasLoggedOut(tokenManager, userName, accessHeadAndPayload);
                }
                _logoutProvider?.UltimateLogout(ultimateLogout);
                //AuthenticationHelper.Logout(ultimateLogout);
                CookieHelper.DeleteCookie(context.Response, AccessSignatureCookieName);
                CookieHelper.DeleteCookie(context.Response, AccessHeadAndPayloadCookieName);
                CookieHelper.DeleteCookie(context.Response, RefreshSignatureCookieName);
                context.Response.StatusCode = HttpResponseStatusCode.Ok;
            }
        }

        private void TokenAccess(string accessHeadAndPayload, TokenManager tokenManager, HttpContextBase context)
        {
            if (!string.IsNullOrWhiteSpace(accessHeadAndPayload))
            {
                var authCookie = CookieHelper.GetCookie(context.Request, AccessSignatureCookieName);
                if (authCookie == null)
                {
                    throw new UnauthorizedAccessException("Missing access cookie.");
                }

                var accessSignature = authCookie.Value;
                var tokenPrincipal = tokenManager.ValidateToken(accessHeadAndPayload + "." + accessSignature);
                if (tokenPrincipal == null)
                {
                    throw new UnauthorizedAccessException("Invalid access token.");
                }
                var userName = tokenManager.GetPayLoadValue(accessHeadAndPayload.Split(Convert.ToChar("."))[1], "name");
                PortalPrincipal portalPrincipal;
                using (AuthenticationHelper.GetSystemAccount())
                {
                    portalPrincipal = AuthenticationHelper.LoadPortalPrincipal(userName);
                }
                AssertUserHasNotLoggedOut(tokenManager, portalPrincipal, accessHeadAndPayload);

                using (AuthenticationHelper.GetSystemAccount())
                {
                    context.User = portalPrincipal;
                }
            }
        }

        private void TokenRefresh(string refreshHeadAndPayload, DateTime validFrom, TokenManager tokenManager, HttpContextBase context)
        {
            var authCookie = CookieHelper.GetCookie(context.Request, RefreshSignatureCookieName);
            if (authCookie == null)
            {
                throw new UnauthorizedAccessException("Missing refresh cookie.");
            }

            var refreshSignature = authCookie.Value;
            var tokenPrincipal = tokenManager.ValidateToken(refreshHeadAndPayload + "." + refreshSignature);
            if (tokenPrincipal == null)
            {
                throw new UnauthorizedAccessException("Invalid access token.");
            }
            var userName = tokenPrincipal.Identity.Name;

            AssertUserHasNotLoggedOut(tokenManager, userName, refreshHeadAndPayload);

            var roleName = String.Empty;
            // emit access token and cookie only
            EmitTokensAndCookies(context, tokenManager, validFrom, userName, roleName, false);
            context.Response.StatusCode = HttpResponseStatusCode.Ok;
        }

        private void AssertUserHasNotLoggedOut(TokenManager tokenManager, string userName, string tokenheadAndPayload)
        {
            PortalPrincipal portalPrincipal;
            using (AuthenticationHelper.GetSystemAccount())
            {
                portalPrincipal = _logoutProvider.LoadPortalPrincipalForLogout(userName);
            }
            AssertUserHasNotLoggedOut(tokenManager, portalPrincipal, tokenheadAndPayload);
        }

        private void AssertUserHasNotLoggedOut(TokenManager tokenManager, PortalPrincipal portalPrincipal, string tokenheadAndPayload)
        {
            if (UserHasLoggedOut(tokenManager, portalPrincipal, tokenheadAndPayload))
            {
                throw new UnauthorizedAccessException("Invalid access token.");
            }
        }

        private bool UserHasLoggedOut(TokenManager tokenManager, string userName, string tokenheadAndPayload)
        {
            PortalPrincipal portalPrincipal;
            using (AuthenticationHelper.GetSystemAccount())
            {
                portalPrincipal = _logoutProvider.LoadPortalPrincipalForLogout(userName);
            }
            return UserHasLoggedOut(tokenManager, portalPrincipal, tokenheadAndPayload);
        }

        private bool UserHasLoggedOut(TokenManager tokenManager, PortalPrincipal portalPrincipal, string tokenheadAndPayload)
        {
            var lastLoggedOut = (portalPrincipal?.Identity as IUser)?.LastLoggedOut;
            return DateTime.Compare(lastLoggedOut.GetValueOrDefault(), TokenCreationTime(tokenManager, tokenheadAndPayload)) <= 0;
        }

        private DateTime TokenCreationTime(TokenManager tokenManager, string headAndPayload)
        {
            var seconds = int.Parse(tokenManager.GetPayLoadValue(headAndPayload.Split(Convert.ToChar("."))[1], "iat"));
            return tokenManager.GetDateFromNumericDate(seconds);
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

            CookieHelper.InsertSecureCookie(context.Response, accessSignature, AccessSignatureCookieName, accessExpiration);
            CookieHelper.InsertSecureCookie(context.Response, accessHeadAndPayload, AccessHeadAndPayloadCookieName, accessExpiration);

            tokenResponse.access = accessHeadAndPayload;

            if (refreshTokenAsWell)
            {
                var refreshSignatureIndex = refreshToken.LastIndexOf('.');
                var refreshSignature = refreshToken.Substring(refreshSignatureIndex + 1);
                var refreshHeadAndPayload = refreshToken.Substring(0, refreshSignatureIndex);
                var refreshExpiration = accessExpiration.AddMinutes(Configuration.TokenAuthentication.RefreshLifeTimeInMinutes);

                CookieHelper.InsertSecureCookie(context.Response, refreshSignature, RefreshSignatureCookieName, refreshExpiration);

                tokenResponse.refresh = refreshHeadAndPayload;
            }

            context.Response.Write(JsonConvert.SerializeObject(tokenResponse, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore }));
        }

        private bool IsTokenAuthenticationRequested(HttpRequestBase request, out bool headerMark, out bool uriMark, out string actionHeader, out string uri, out string headAndPayload)
        {
            actionHeader = GetAuthenticationActionHeader(request);
            uri = request.Url.AbsolutePath;
            headerMark = TokenActions.Contains(actionHeader, StringComparer.InvariantCultureIgnoreCase);
            uriMark = TokenPaths.Contains(uri, StringComparer.InvariantCultureIgnoreCase);
            var cookie = CookieHelper.GetCookie(request, AccessHeadAndPayloadCookieName);
            headAndPayload = cookie == null ? request.Headers[RefreshHeaderName] : cookie.Value;
            return request.IsSecureConnection && (headerMark || uriMark || headAndPayload != null);
        }

        private string GetAuthenticationActionHeader(HttpRequestBase request)
        {
            return request.Headers[AuthenticationActionHeaderName] ??
                   request.Headers[AuthenticationActionHeaderName.ToLower()];
        }

        private string GetRefreshHeader(HttpRequestBase request)
        {
            return request.Headers[RefreshHeaderName] ?? request.Headers[RefreshHeaderName.ToLower()];
        }

        private string GetAccessHeader(HttpRequestBase request)
        {
            return request.Headers[AccessHeaderName] ?? request.Headers[AccessHeaderName.ToLower()];
        }

        private class TokenResponse
        {
            public string access;
            public string refresh;
        }
    }
}