using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using System.Collections.Specialized;

namespace SenseNet.Portal.AppModel
{
    public interface IHttpAction
    {
        NodeHead AppNode { get; set; }
        NodeHead TargetNode { get; set; }
        IHttpActionContext Context { get; set; }

        void Execute();
        void AssertPermissions();
        bool CheckPermission();
    }
    public interface IDefaultHttpAction : IHttpAction
    {
    }
    public interface IRedirectHttpAction : IHttpAction
    {
        string TargetUrl { get; set; }
        bool EndResponse { get; set; }
    }
    public interface IRewriteHttpAction : IHttpAction
    {
        string Path { get; set; }
        bool? RebaseClientPath { get; set; }

        string FilePath { get; set; }
        string PathInfo { get; set; }
        string QueryString { get; set; }
        bool? SetClientFilePath { get; set; }
    }
    public interface IDownloadHttpAction : IRewriteHttpAction
    {
        string BinaryPropertyName { get; set; }
    }
    public interface IRemapHttpAction : IHttpAction
    {
        NodeHead HttpHandlerNode { get; set; }
    }

    public interface IHttpActionFactory
    {
        IDefaultHttpAction CreateDefaultAction(IHttpActionContext context, NodeHead targetNode, NodeHead appNode);
        IRedirectHttpAction CreateRedirectAction(IHttpActionContext context, NodeHead targetNode, NodeHead appNode, string targetUrl, bool permanent, bool endResponse);
        IRemapHttpAction CreateRemapAction(IHttpActionContext context, NodeHead targetNode, NodeHead appNode, NodeHead httpHandlerNode);
        IRemapHttpAction CreateRemapAction(IHttpActionContext context, NodeHead targetNode, NodeHead appNode, Type httpHandlerType);
        IRewriteHttpAction CreateRewriteAction(IHttpActionContext context, NodeHead targetNode, NodeHead appNode, string path);
        IRewriteHttpAction CreateRewriteAction(IHttpActionContext context, NodeHead targetNode, NodeHead appNode, string path, bool rebaseClientPath);
        IRewriteHttpAction CreateRewriteAction(IHttpActionContext context, NodeHead targetNode, NodeHead appNode, string filePath, string pathInfo, string queryString);
        IRewriteHttpAction CreateRewriteAction(IHttpActionContext context, NodeHead targetNode, NodeHead appNode, string filePath, string pathInfo, string queryString, bool setClientFilePath);
        IDownloadHttpAction CreateDownloadAction(IHttpActionContext context, NodeHead targetNode, NodeHead appNode, string path, string binaryPropertyName);
    }

    public interface IHttpActionContext
    {
        string RequestedUrl { get; }
        Uri RequestedUri { get; }
        NodeHead GetRequestedNode();
        string RequestedActionName { get; }
        string RequestedApplicationNodePath { get; }
        string RequestedContextNodePath { get; }
        NameValueCollection Params { get; }
        IHttpActionFactory GetActionFactory();
        bool IsWebdavRequest { get; }
        IHttpAction CurrentAction { get; set; }
        string DeviceName { get; }
    }
}
