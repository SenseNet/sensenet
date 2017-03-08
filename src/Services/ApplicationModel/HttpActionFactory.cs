using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using System.IO;
using System.Collections.Specialized;
using SenseNet.Portal.Virtualization;
using System.Web;
using SenseNet.ContentRepository.Storage.Security;
using System.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using System.Diagnostics;

namespace SenseNet.Portal.AppModel
{
    internal class HttpActionFactory : IHttpActionFactory
    {
        public IDefaultHttpAction CreateDefaultAction(IHttpActionContext context, NodeHead targetNode, NodeHead appNode)
        {
            return new DefaultHttpAction
            {
                Context = context,
                TargetNode = targetNode,
                AppNode = appNode
            };
        }
        public IRedirectHttpAction CreateRedirectAction(IHttpActionContext context, NodeHead targetNode, NodeHead appNode, string targetUrl, bool permanent, bool endResponse)
        {
            return new RedirectHttpAction
            {
                Context = context,
                TargetNode = targetNode,
                AppNode = appNode,
                TargetUrl = targetUrl,
                Permanent = permanent,
                EndResponse = endResponse,
            };
        }

        public IRemapHttpAction CreateRemapAction(IHttpActionContext context, NodeHead targetNode, NodeHead appNode, Type httpHandlerType)
        {
            return new RemapHttpAction
            {
                Context = context,
                TargetNode = targetNode,
                AppNode = appNode,
                HttpHandlerType = httpHandlerType
            };
        }
        public IRemapHttpAction CreateRemapAction(IHttpActionContext context, NodeHead targetNode, NodeHead appNode, NodeHead httpHandlerNode)
        {
            return new RemapHttpAction
            {
                Context = context,
                TargetNode = targetNode,
                AppNode = appNode,
                HttpHandlerNode = httpHandlerNode
            };
        }
        public IRewriteHttpAction CreateRewriteAction(IHttpActionContext context, NodeHead targetNode, NodeHead appNode, string path)
        {
            return new RewriteHttpAction
            {
                Context = context,
                TargetNode = targetNode,
                AppNode = appNode,
                Path = path
            };
        }
        public IRewriteHttpAction CreateRewriteAction(IHttpActionContext context, NodeHead targetNode, NodeHead appNode, string path, bool rebaseClientPath)
        {
            return new RewriteHttpAction
            {
                Context = context,
                TargetNode = targetNode,
                AppNode = appNode,
                Path = path,
                RebaseClientPath = rebaseClientPath
            };
        }
        public IRewriteHttpAction CreateRewriteAction(IHttpActionContext context, NodeHead targetNode, NodeHead appNode, string filePath, string pathInfo, string queryString)
        {
            return new RewriteHttpAction
            {
                Context = context,
                TargetNode = targetNode,
                AppNode = appNode,
                FilePath = filePath,
                PathInfo = pathInfo,
                QueryString = queryString
            };
        }
        public IRewriteHttpAction CreateRewriteAction(IHttpActionContext context, NodeHead targetNode, NodeHead appNode, string filePath, string pathInfo, string queryString, bool setClientFilePath)
        {
            return new RewriteHttpAction
            {
                Context = context,
                TargetNode = targetNode,
                AppNode = appNode,
                FilePath = filePath,
                PathInfo = pathInfo,
                QueryString = queryString,
                SetClientFilePath = setClientFilePath
            };
        }

        public IDownloadHttpAction CreateDownloadAction(IHttpActionContext context, NodeHead targetNode, NodeHead appNode, string path, string binaryPropertyName)
        {
            return new DownloadHttpAction
            {
                Context = context,
                TargetNode = targetNode,
                AppNode = appNode,
                Path = path,
                BinaryPropertyName = binaryPropertyName
            };
        }

    }

}
