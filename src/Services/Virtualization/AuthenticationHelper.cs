using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Security;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using System.Security.Principal;
using SenseNet.ContentRepository.Security;

namespace SenseNet.Portal.Virtualization
{
    public static class AuthenticationHelper
    {
        private static readonly string authHeaderName = "WWW-Authenticate";

        public static void DenyAccess(HttpApplication application)
        {
            DenyAccess(application?.Context);
        }
        public static void DenyAccess(HttpContext context)
        {
            if(context == null)
                throw new ArgumentNullException(nameof(context));

            context.Response.Clear();
            context.Response.StatusCode = 401;
            context.Response.Status = "401 Unauthorized";
            context.Response.End();
        }

        public static void ForceBasicAuthentication(HttpContext context)
        {
            context.Response.Clear();
            context.Response.Buffer = true;
            context.Response.StatusCode = 401;
            context.Response.Status = "401 Unauthorized";

            // make sure that the auth header appears only once in the response
            if (context.Response.Headers.AllKeys.Contains(authHeaderName))
                context.Response.Headers.Remove(authHeaderName);

            context.Response.AddHeader(authHeaderName, "Basic");
            context.Response.End();
        }

        public static void ThrowForbidden(string contentNameOrPath = null)
        {
            throw new HttpException(403, SNSR.GetString(SNSR.Exceptions.HttpAction.Forbidden_1, contentNameOrPath ?? string.Empty));
        }

        public static void ThrowNotFound(string contentNameOrPath = null)
        {
            throw new HttpException(404, SNSR.GetString(SNSR.Exceptions.HttpAction.NotFound_1, contentNameOrPath ?? string.Empty));
        }

        [ODataAction]
        public static object Login(Content content, string username, string password)
        {
            if (string.IsNullOrEmpty(username))
            {
                Logout();
                throw new OData.ODataException(OData.ODataExceptionCode.Forbidden);
            }

            if (Membership.ValidateUser(username, password))
            {
                // we need to work with the full username that contains the domain: SetAuthCookie expects that
                if (!username.Contains("\\"))
                    username = IdentityManagement.DefaultDomain + "\\" + username;

                if (User.Current.IsAuthenticated)
                {
                    // if this is the user that is already logged in, return with a success code
                    if (string.CompareOrdinal(User.Current.Username, username) == 0)
                        using (new SystemAccount())
                        {
                            FormsAuthentication.SetAuthCookie(username, true);
                            return Content.Create(User.Load(username) as User);
                        }

                    // logged in as a different user: we have to log out first
                    Logout();
                }

                var info = new CancellableLoginInfo { UserName = username };
                LoginExtender.OnLoggingIn(info);
                if (info.Cancel)
                    throw new OData.ODataException(OData.ODataExceptionCode.Forbidden);

                SnLog.WriteAudit(AuditEvent.LoginSuccessful, new Dictionary<string, object>
                {
                    {"UserName", username},
                    {"ClientAddress", RepositoryTools.GetClientIpAddress()}
                });

                LoginExtender.OnLoggedIn(new LoginInfo { UserName = username });

                
                using (new SystemAccount())
                {
                    FormsAuthentication.SetAuthCookie(username, true);
                    return Content.Create(User.Load(username) as User);
                }
            }
            
            throw new OData.ODataException(OData.ODataExceptionCode.Forbidden);
        }

        public static void Logout()
        {
            var info = new CancellableLoginInfo { UserName = User.Current.Username };
            LoginExtender.OnLoggingOut(info);

            if (info.Cancel)
                return;
            
            FormsAuthentication.SignOut();

            SnLog.WriteAudit(AuditEvent.Logout,
                new Dictionary<string, object>
                {
                    {"UserName", User.Current.Username},
                    {"ClientAddress", RepositoryTools.GetClientIpAddress()}
                });

            LoginExtender.OnLoggedOut(new LoginInfo { UserName = User.Current.Username });

            if (HttpContext.Current != null)
            {
                if (HttpContext.Current.Session != null)
                    HttpContext.Current.Session.Abandon();

                // remove session cookie
                var sessionCookie = new HttpCookie(GetSessionIdCookieName(), string.Empty)
                {
                    Expires = DateTime.UtcNow.AddDays(-1)
                };

                HttpContext.Current.Response.Cookies.Add(sessionCookie);
            }
        }

        private static string GetSessionIdCookieName()
        {
            var sessionStateSection = (SessionStateSection)ConfigurationManager.GetSection("system.web/sessionState");
            return sessionStateSection != null ? sessionStateSection.CookieName : string.Empty;
        }

        public static Func<object, HttpContextBase> GetContext = sender => new HttpContextWrapper(((HttpApplication)sender).Context);
        public static Func<object, HttpRequestBase> GetRequest = sender => new HttpRequestWrapper(((HttpApplication)sender).Context.Request);
        public static Func<object, HttpResponseBase> GetResponse = sender => new HttpResponseWrapper(((HttpApplication)sender).Context.Response);

        public static Func<IPrincipal> GetVisitorPrincipal = () => new PortalPrincipal(User.Visitor);
        public static Func<string, IPrincipal> LoadUserPrincipal = userName => new PortalPrincipal(User.Load(userName));

        public static Func<string, string, bool> IsUserValid = (userName, password) => Membership.ValidateUser(userName, password);

        public static Func<IDisposable> GetSystemAccount = () => new SystemAccount();
        public static Func<string> GetBasicAuthHeader = () => PortalContext.Current.BasicAuthHeaders;

    }
}
