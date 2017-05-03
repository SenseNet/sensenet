using System;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal.Virtualization;
using System.Web;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.Tools;

namespace SenseNet.Portal.AppModel
{
    public static class HttpActionManager
    {
        public static IHttpAction CreateAction(IHttpActionContext context)
        {
            var action = CreateActionPrivate(context, null, null, null, null, null);
            context.CurrentAction = action;
            return action;
        }
        private static IHttpAction CreateActionPrivate(IHttpActionContext actionContext, IHttpActionFactory actionFactory, NodeHead requestedNode, string requestedActionName, string requestedApplicationNodePath, string requestedDevice)
        {
            IHttpAction action = null;

            var factory = actionFactory ?? actionContext.GetActionFactory();
            var contextNode = requestedNode ?? actionContext.GetRequestedNode();
            var actionName = requestedActionName ?? actionContext.RequestedActionName;
            var appNodePath = requestedApplicationNodePath ?? actionContext.RequestedApplicationNodePath;
            var portalContext = (PortalContext)actionContext;

            // ================================================= #1: preconditions

            action = GetODataAction(factory, portalContext, contextNode);
            if (action != null)
                return action;

            // webdav request?
            action = GetWebdavAction(factory, portalContext, contextNode);
            if (action != null)
                return action;

            // ----------------------------------------------- forward to start page if context is a Site

            action = GetSiteStartPageAction(factory, portalContext, contextNode);
            if (action != null)
                return action;

            // ----------------------------------------------- smart url

            action = GetSmartUrlAction(factory, portalContext, contextNode);
            if (action != null)
                return action;

            // ----------------------------------------------- outer resource

            action = GetExternalResourceAction(factory, portalContext, contextNode);
            if (action != null)
                return action;

            // ----------------------------------------------- context is external page

            action = GetExternalPageAction(factory, portalContext, contextNode, actionName, appNodePath);
            if (action != null)
                return action;

            // ----------------------------------------------- context is IHttpHandlerNode

            if (string.IsNullOrEmpty(actionName))
            {
                action = GetIHttpHandlerAction(factory, portalContext, contextNode, contextNode);
                if (action != null)
                    return action;
            }

            // ----------------------------------------------- default context action

            action = GetDefaultContextAction(factory, portalContext, contextNode, actionName, appNodePath);
            if (action != null)
                return action;

            // ================================================= #2: FindApplication(node, action);

            var appNode = FindApplication(contextNode, actionName, appNodePath, actionContext.DeviceName);
            if (appNode == null)
                return factory.CreateRewriteAction(actionContext, contextNode, null, GetRewritePath(contextNode, portalContext));

            // ----------------------------------------------- AppNode is IHttpHandlerNode 

            action = GetIHttpHandlerAction(factory, portalContext, contextNode, appNode);
            if (action != null)
                return action;

            // ----------------------------------------------- page and site

            return factory.CreateRewriteAction(actionContext, contextNode, appNode, GetRewritePath(appNode, portalContext));
        }

        private static IHttpAction GetSiteStartPageAction(IHttpActionFactory factory, PortalContext portalContext, NodeHead contextNode)
        {
            if (contextNode == null)
                return null;
            if (!contextNode.GetNodeType().IsInstaceOfOrDerivedFrom("Site"))
                return null;
            Node startPage = null;

            using (new SystemAccount())
            {
                var contextSite = Node.Load<Site>(contextNode.Id);
                if (contextSite != null && (portalContext.ActionName == null || portalContext.ActionName.ToLower() == "browse"))
                    startPage = contextSite.StartPage;
                if (startPage == null)
                    return null;
            }

            var relPath = startPage.Path;
            if (portalContext.Site != null)
                relPath = relPath.Replace(portalContext.Site.Path, string.Empty);

            return factory.CreateRedirectAction(portalContext, contextNode, null, relPath, false, true);
        }
        private static IHttpAction GetSmartUrlAction(IHttpActionFactory factory, PortalContext portalContext, NodeHead contextNode)
        {
            string smartUrl = GetSmartUrl(portalContext);
            if (smartUrl != null)
                return factory.CreateRedirectAction(portalContext, contextNode, null, smartUrl, false, true);
            return null;
        }
        private static IHttpAction GetExternalResourceAction(IHttpActionFactory factory, PortalContext portalContext, NodeHead contextNode)
        {
            if (contextNode == null)
                return factory.CreateDefaultAction(portalContext, contextNode, null);
            return null;
        }
        private static IHttpAction GetExternalPageAction(IHttpActionFactory factory, PortalContext portalContext, NodeHead contextNode, string actionName, string appNodePath)
        {
            if (contextNode == null)
                return null;
            if (actionName != null)
                return null;
            if (appNodePath != null)
                return null;

            string outerUrl = null;
            AccessProvider.ChangeToSystemAccount();
            try
            {
                var page = Node.LoadNode(contextNode.Id) as GenericContent;
                if (page != null && page.NodeType.IsInstaceOfOrDerivedFrom("Page"))
                    if (Convert.ToBoolean(page["IsExternal"]))
                        outerUrl = page.GetProperty<string>("OuterUrl");
            }
            finally
            {
                AccessProvider.RestoreOriginalUser();
            }
            if (outerUrl != null)
                return factory.CreateRedirectAction(portalContext, contextNode, null, outerUrl, false, true);
            return null;
        }
        private static IHttpAction GetWebdavAction(IHttpActionFactory factory, PortalContext portalContext, NodeHead contextNode)
        {
            if (!portalContext.IsWebdavRequest)
                return null;

            return GetIHttpHandlerAction(factory, portalContext, contextNode, typeof(SenseNet.Services.WebDav.WebDavHandler));
        }
        private static IHttpAction GetODataAction(IHttpActionFactory factory, PortalContext portalContext, NodeHead contextNode)
        {
            if (!portalContext.IsOdataRequest)
                return null;
            return GetIHttpHandlerAction(factory, portalContext, contextNode, typeof(SenseNet.Portal.OData.ODataHandler));
        }

        private static IHttpAction GetIHttpHandlerAction(IHttpActionFactory factory, PortalContext portalContext, NodeHead contextNode, Type httpHandlerType)
        {
            return factory.CreateRemapAction(portalContext, contextNode, null, httpHandlerType);
        }
        private static IHttpAction GetIHttpHandlerAction(IHttpActionFactory factory, PortalContext portalContext, NodeHead contextNode, NodeHead handlerNode)
        {
            var nodeType = handlerNode.GetNodeType();
            Type appType = TypeResolver.GetType(nodeType.ClassName);
            if (typeof(IHttpHandler).IsAssignableFrom(appType))
                return factory.CreateRemapAction(portalContext, contextNode, null, handlerNode);
            return null;
        }
        private static IHttpAction GetDefaultContextAction(IHttpActionFactory factory, PortalContext portalContext, NodeHead contextNode, string actionName, string appNodePath)
        {
            if (String.IsNullOrEmpty(actionName) && String.IsNullOrEmpty(appNodePath))
            {
                if(!String.IsNullOrEmpty(portalContext.QueryStringNodePropertyName))
                    return factory.CreateDownloadAction(portalContext, contextNode, null, GetRewritePath(contextNode, portalContext), portalContext.QueryStringNodePropertyName);
                var nodeType = contextNode.GetNodeType();
                if (nodeType.IsInstaceOfOrDerivedFrom("Page"))
                    return factory.CreateRewriteAction(portalContext, contextNode, null, GetRewritePath(contextNode, portalContext));
                if (nodeType.IsInstaceOfOrDerivedFrom("File"))
                    return factory.CreateDownloadAction(portalContext, contextNode, null, GetRewritePath(contextNode, portalContext), PortalContext.DefaultNodePropertyName);
            }
            return null;
        }

        // ---------------------------------------------------------------------

        private static NodeHead FindApplication(NodeHead requestedNodeHead, string actionName, string appNodePath, string device)
        {
            if (appNodePath != null)
                return NodeHead.Get(appNodePath);
            if (String.IsNullOrEmpty(actionName))
                actionName = "Browse";

            Content content;

            using (new SystemAccount())
            {
                content = Content.Load(requestedNodeHead.Id);

                // self dispatch
                var genericContent = content.ContentHandler as GenericContent;
                if (genericContent != null)
                {
                    var selfDispatchApp = genericContent.GetApplication(actionName);
                    if (selfDispatchApp != null)
                        return selfDispatchApp;
                }

                bool appExists;
                var app = ApplicationStorage.Instance.GetApplication(actionName, content, out appExists, device);

                if (!string.IsNullOrEmpty(actionName) && !appExists)
                    throw new UnknownActionException(string.Format("Action '{0}' does not exist", HttpUtility.HtmlEncode(actionName)), actionName);

                return app != null ? NodeHead.Get(app.Id) : null;
            }
        }

        // ---------------------------------------------------------------------

        private static string GetRewritePath(NodeHead appNodeHead, PortalContext portalContext)
        {
            if (!string.IsNullOrEmpty(portalContext.QueryStringNodePropertyName))
                return appNodeHead.Path;
            
            var contextNodeType = appNodeHead.GetNodeType();

            if (contextNodeType.IsInstaceOfOrDerivedFrom("Page"))
                return appNodeHead.Path + PortalContext.InRepositoryPageSuffix;
            if (contextNodeType.IsInstaceOfOrDerivedFrom("Site"))
                throw new NotSupportedException("This site does not have a main page.");

            return appNodeHead.Path;
        }

        private static string GetSmartUrl(PortalContext portalContext)
        {
            if (portalContext == null)
                throw new ArgumentNullException(nameof(portalContext));

            if (portalContext.SiteRelativePath == null)
                return null;
            if (portalContext.Site?.Path == null)
                return null;

            var siteRelativePath = portalContext.SiteRelativePath.ToLowerInvariant();
            var sitePath = portalContext.Site.Path.ToLowerInvariant();

            string smartUrlTargetPath;

            PortalContext.SmartUrls.TryGetValue(string.Concat(sitePath, ":", siteRelativePath), out smartUrlTargetPath);

            if (smartUrlTargetPath == null)
                return null;

            var resolvedSmartUrl = string.Concat(
                portalContext.RequestedUri.Scheme,
                "://",
                portalContext.SiteUrl,
                smartUrlTargetPath,
                portalContext.RequestedUri.Query
            );

            return resolvedSmartUrl;
        }

    }
}
