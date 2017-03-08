using System;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using System.Web;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.Virtualization;
using SenseNet.ApplicationModel;
using SenseNet.Tools;

namespace SenseNet.Portal.AppModel
{
    internal abstract class HttpAction : IHttpAction
    {
        private NodeHead _appNode;

        public NodeHead AppNode
        {
            get
            {
                if (_appNode != null)
                    return _appNode;
                if (TargetNode == null)
                    return null;
                if (!IsApplication(TargetNode))
                    return null;
                return TargetNode;
            }
            set
            {
                if (value != null)
                {
                    if (!IsApplication(value))
                    {
                        var nodeType = value.GetNodeType();
                        throw new ApplicationException(SNSR.GetString(SNSR.Exceptions.HttpAction.NodeIsNotAnApplication_3, value.Path, nodeType.Name, nodeType.ClassName));
                    }
                }
                _appNode = value;
            }
        }
        public NodeHead TargetNode { get; set; }
        public IHttpActionContext Context { get; set; }

        public abstract void Execute();

        private bool IsApplication(NodeHead appNode)
        {
            var type = TypeResolver.GetType(appNode.GetNodeType().ClassName);
            return typeof(Application).IsAssignableFrom(type);
        }

        public virtual bool CheckPermission()
        {
            if (TargetNode != null)
            {
                if (!SecurityHandler.HasPermission(TargetNode, PermissionType.See))
                    ThrowNotFound();
            }
            if (_appNode != null)
            {
                if (!SecurityHandler.HasPermission(_appNode, PermissionType.RunApplication))
                    return false;
                if (TargetNode != null)
                {
                    Application appNode = null;

                    // Elevation: we should check required permissions here, 
                    // regardless of the users permissions for the application.
                    using (new SystemAccount())
                    {
                        appNode = Node.Load<Application>(_appNode.Id);
                    }

                    if (!ActionFramework.HasRequiredPermissions(appNode, TargetNode))
                        return false;
                }
            }
            return true;
        }

        public virtual void AssertPermissions()
        {
            if(!CheckPermission())
                ThrowForbidden();
        }

        protected void ThrowNotFound()
        {
            throw new HttpException(404, SNSR.GetString(SNSR.Exceptions.HttpAction.NotFound_1, TargetNode == null ? string.Empty : TargetNode.Name));
        }
        protected void ThrowForbidden()
        {
            throw new HttpException(404, SNSR.GetString(SNSR.Exceptions.HttpAction.Forbidden_1, TargetNode == null ? string.Empty : TargetNode.Name));
        }
    }

    internal class DefaultHttpAction : HttpAction, IDefaultHttpAction
    {
        public override void Execute()
        {
            // Do nothing
        }
    }
    internal class RedirectHttpAction : HttpAction, IRedirectHttpAction
    {
        public string TargetUrl { get; set; }
        public bool EndResponse { get; set; }
        public bool Permanent { get; set; }

        public override void Execute()
        {
            if(Permanent)
                RedirectPermanently(HttpContext.Current.Response, TargetUrl);
            else
                HttpContext.Current.Response.Redirect(TargetUrl, EndResponse);
        }
        private static void RedirectPermanently(HttpResponse response, string url)
        {
            response.Clear();
            response.Status = "301 Moved Permanently";
            response.AddHeader("Location", url);
            response.End();
        }
    }
    internal class RewriteHttpAction : HttpAction, IRewriteHttpAction
    {
        public string Path { get; set; }
        public bool? RebaseClientPath { get; set; }

        public string FilePath { get; set; }
        public string PathInfo { get; set; }
        public string QueryString { get; set; }
        public bool? SetClientFilePath { get; set; }

        public override void Execute()
        {
            var ctx = HttpContext.Current;
            if (Path != null)
            {
                if (RebaseClientPath == null)
                {
                    ctx.Items["OriginalPath"] = ctx.Request.Path;
                    ctx.RewritePath(Path);
                }
                else
                    HttpContext.Current.RewritePath(Path, RebaseClientPath.Value);
            }
            else
            {
                if (SetClientFilePath == null)
                    HttpContext.Current.RewritePath(FilePath, PathInfo, QueryString);
                else
                    HttpContext.Current.RewritePath(FilePath, PathInfo, QueryString, SetClientFilePath.Value);
            }
        }
    }
    internal class DownloadHttpAction : RewriteHttpAction, IDownloadHttpAction
    {
        public string BinaryPropertyName { get; set; }

        public override void AssertPermissions()
        {
            var isOwner = TargetNode.CreatorId == User.Current.Id;
            if (!SecurityHandler.HasPermission(TargetNode, PermissionType.See))
                base.ThrowNotFound();
            if (!SecurityHandler.HasPermission(TargetNode, PermissionType.Open))
                base.ThrowForbidden();
        }
    }
    internal class RemapHttpAction : HttpAction, IRemapHttpAction
    {
        public NodeHead HttpHandlerNode { get; set; }
        public Type HttpHandlerType { get; set; }

        public override void Execute()
        {
            IHttpHandler handler;

            if (HttpHandlerType != null)
            {
                handler = (IHttpHandler)Activator.CreateInstance(HttpHandlerType);
            }
            else
            {
                using (new SystemAccount())
                {
                    VersionNumber version = null;
                    var versionStr = PortalContext.Current.VersionRequest;

                    handler = string.IsNullOrEmpty(versionStr) || !VersionNumber.TryParse(versionStr, out version)
                        ? (IHttpHandler)Node.LoadNode(HttpHandlerNode.Id) 
                        : (IHttpHandler)Node.LoadNode(HttpHandlerNode.Id, version);
                }
            }
            HttpContext.Current.RemapHandler(handler);
        }
        public override bool CheckPermission()
        {
            if (!base.CheckPermission())
                return false;

            // content that serve themselves as IHttpHandlers - e.g. images - do not require a Run application permission
            if (HttpHandlerNode != null && (TargetNode != null && TargetNode.Id != HttpHandlerNode.Id))
                if (!SecurityHandler.HasPermission(HttpHandlerNode, PermissionType.RunApplication))
                    return false;
            return true;

        }
    }
}
