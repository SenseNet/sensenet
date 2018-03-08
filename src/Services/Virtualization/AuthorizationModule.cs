using System;
using System.Web;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Preview;
using SenseNet.Security;

namespace SenseNet.Portal.Virtualization
{
    internal class AuthorizationModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.AuthorizeRequest += OnAuthorizeRequest;
        }

        private void OnAuthorizeRequest(object sender, EventArgs e)
        {
            AuthorizeRequest((sender as HttpApplication)?.Context);
        }
        internal void AuthorizeRequest(HttpContext context)
        {
            PortalContext currentPortalContext = PortalContext.Current;
            if (currentPortalContext == null)
                return;
            var currentUser = context?.User.Identity as User;

            // deny access for visitors in case of webdav or office protocol requests, if they have no See access to the content
            if (currentUser != null && currentUser.Id == Identifiers.VisitorUserId && (currentPortalContext.IsOfficeProtocolRequest || currentPortalContext.IsWebdavRequest))
            {
                if (!currentPortalContext.IsRequestedResourceExistInRepository ||
                    currentPortalContext.ContextNodeHead == null ||
                    !SecurityHandler.HasPermission(currentPortalContext.ContextNodeHead, PermissionType.See))
                {
                    AuthenticationHelper.ForceBasicAuthentication(HttpContext.Current);
                }
            }

            if (context == null)
                return;

            if (currentPortalContext.IsRequestedResourceExistInRepository)
            {
                var authMode = currentPortalContext.AuthenticationMode;
                if (string.IsNullOrEmpty(authMode))
                    authMode = WebApplication.DefaultAuthenticationMode;

                bool appPerm;
                if (authMode == "Forms")
                {
                    appPerm = currentPortalContext.CurrentAction.CheckPermission();
                }
                else if (authMode == "Windows")
                {
                    currentPortalContext.CurrentAction.AssertPermissions();
                    appPerm = true;
                }
                else
                {
                    throw new NotSupportedException("None authentication is not supported");
                }

                var path = currentPortalContext.RepositoryPath;
                var nodeHead = NodeHead.Get(path);
                var permissionValue = SecurityHandler.GetPermission(nodeHead, PermissionType.Open);

                if (permissionValue == PermissionValue.Allowed && DocumentPreviewProvider.Current.IsPreviewOrThumbnailImage(nodeHead))
                {
                    // In case of preview images we need to make sure that they belong to a content version that
                    // is accessible by the user (e.g. must not serve images for minor versions if the user has
                    // access only to major versions of the content).
                    if (!DocumentPreviewProvider.Current.IsPreviewAccessible(nodeHead))
                        permissionValue = PermissionValue.Denied;
                }
                else if (permissionValue != PermissionValue.Allowed && appPerm && DocumentPreviewProvider.Current.HasPreviewPermission(nodeHead))
                {
                    // In case Open permission is missing: check for Preview permissions. If the current Document
                    // Preview Provider allows access to a preview, we should allow the user to access the content.
                    permissionValue = PermissionValue.Allowed;
                }

                if (permissionValue != PermissionValue.Allowed)
                    if (nodeHead.Id == Identifiers.PortalRootId)
                        if (currentPortalContext.IsOdataRequest)
                            if (currentPortalContext.ODataRequest.IsMemberRequest)
                                permissionValue = PermissionValue.Allowed;

                if (permissionValue != PermissionValue.Allowed || !appPerm)
                {
                    if (currentPortalContext.IsOdataRequest)
                    {
                        AuthenticationHelper.ThrowForbidden();
                    }
                    switch (authMode)
                    {
                        case "Forms":
                            if (User.Current.IsAuthenticated)
                            {
                                // user is authenticated, but has no permissions: return 403
                                context.Response.StatusCode = 403;
                                context.Response.Flush();
                                context.Response.Close();
                            }
                            else
                            {
                                // let webdav and office protocol handle authentication - in these cases redirecting to a login page makes no sense
                                if (PortalContext.Current.IsWebdavRequest || PortalContext.Current.IsOfficeProtocolRequest)
                                    return;

                                // user is not authenticated and visitor has no permissions: redirect to login page
                                // Get the login page Url (eg. http://localhost:1315/home/login)
                                string loginPageUrl = currentPortalContext.GetLoginPageUrl();
                                // Append trailing slash
                                if (loginPageUrl != null && !loginPageUrl.EndsWith("/"))
                                    loginPageUrl = loginPageUrl + "/";

                                // Cut down the querystring (eg. drop ?Param1=value1@Param2=value2)
                                string currentRequestUrlWithoutQueryString = currentPortalContext.RequestedUri.GetComponents(UriComponents.Scheme | UriComponents.Host | UriComponents.Port | UriComponents.Path, UriFormat.Unescaped);

                                // Append trailing slash
                                if (!currentRequestUrlWithoutQueryString.EndsWith("/"))
                                    currentRequestUrlWithoutQueryString = currentRequestUrlWithoutQueryString + "/";

                                // Redirect to the login page, if neccessary.
                                if (currentRequestUrlWithoutQueryString != loginPageUrl)
                                    context.Response.Redirect(loginPageUrl + "?OriginalUrl=" + System.Web.HttpUtility.UrlEncode(currentPortalContext.RequestedUri.ToString()), true);
                            }
                            break;
                        default:
                            AuthenticationHelper.DenyAccess(context);
                            break;
                    }
                }
            }
        }
        public void Dispose()
        {
        }
    }
}