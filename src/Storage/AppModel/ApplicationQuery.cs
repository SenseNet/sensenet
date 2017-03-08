using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.AppModel
{
    public enum HierarchyOption
    {
        Type, Path, TypeAndPath, PathAndType
    }
    public class ApplicationQuery : IDisposable
    {
        public string AppFolderName { get; private set; }
        private bool ResolveAll { get; set; }
        private bool ResolveChildren { get; set; }
        private HierarchyOption Option { get; set; }
        private bool UseCache { get; set; }

        public ApplicationQuery(string appFolderName, bool resolveAll, bool resolveChildren, HierarchyOption option)
            : this(appFolderName, resolveAll, resolveChildren, option, false)
        {
        }
        public ApplicationQuery(string appFolderName, bool resolveAll, bool resolveChildren, HierarchyOption option, bool useCache)
        {
            AppFolderName = appFolderName;
            ResolveAll = resolveAll;
            ResolveChildren = resolveChildren;
            Option = option;
            UseCache = useCache;
            if (useCache)
                AppCacheInvalidator.Invalidate += new EventHandler<AppCacheInvalidateEventArgs>(AppCacheInvalidator_Invalidate);
        }

        public IEnumerable<NodeHead> ResolveApplication(string appName, NodeHead contextNode)
        {
            return ResolveApplications(appName, contextNode.Path, ActiveSchema.NodeTypes.GetItemById(contextNode.NodeTypeId));
        }
        public IEnumerable<NodeHead> ResolveApplications(string appName, Node contextNode)
        {
            return ResolveApplications(appName, contextNode.Path, contextNode.NodeType);
        }
        public IEnumerable<NodeHead> ResolveApplications(string appName, string contextNodePath, string nodeTypeName)
        {
            return ResolveApplications(appName, contextNodePath, ActiveSchema.NodeTypes[nodeTypeName]);
        }
        public IEnumerable<NodeHead> ResolveApplications(string appName, string contextNodePath, NodeType nodeType)
        {
            var paths = ApplicationResolver.GetAvailablePaths(contextNodePath, nodeType, this.AppFolderName, appName, Option);
            if (ResolveAll)
                return ResolveAllByPaths(paths, ResolveChildren);
            else
                return new NodeHead[] { ResolveFirstByPaths(paths) };
        }

        public IEnumerable<string> GetAvailablePaths(string appName, NodeHead contextNode)
        {
            return GetAvailablePaths(appName, contextNode.Path, ActiveSchema.NodeTypes.GetItemById(contextNode.NodeTypeId));
        }
        public IEnumerable<string> GetAvailablePaths(string appName, Node contextNode)
        {
            return GetAvailablePaths(appName, contextNode.Path, contextNode.NodeType);
        }
        public IEnumerable<string> GetAvailablePaths(string appName, string contextNodePath, string nodeTypeName)
        {
            return GetAvailablePaths(appName, contextNodePath, ActiveSchema.NodeTypes[nodeTypeName]);
        }
        public IEnumerable<string> GetAvailablePaths(string appName, string contextNodePath, NodeType nodeType)
        {
            return ApplicationResolver.GetAvailablePaths(contextNodePath, nodeType, this.AppFolderName, appName, Option);
        }

        public NodeHead ResolveFirstByPaths(IEnumerable<string> paths)
        {
            if (UseCache && AppCache != null)
                return ResolveByPathsFromCache(paths, false, false).FirstOrDefault();
            return ApplicationResolver.ResolveFirstByPaths(paths);
        }
        public IEnumerable<NodeHead> ResolveAllByPaths(IEnumerable<string> paths, bool resolveChildren)
        {
            if (UseCache && AppCache != null)
                return ResolveByPathsFromCache(paths, resolveChildren, true);
            return ApplicationResolver.ResolveAllByPaths(paths, resolveChildren);
        }

        // ======================================================================================= Cache

        private static object _loaderLock = new object();
        private static IApplicationCache _appCache;
        private static IEnumerable<string> EmptyCache = new string[0];
        private static IApplicationCache AppCache
        {
            get
            {
                if (_appCache == null)
                {
                    lock (_loaderLock)
                    {
                        if (_appCache == null)
                        {
                            _appCache = TypeHandler.ResolveProvider<IApplicationCache>();
                        }
                    }
                }
                return _appCache;
            }
        }
        internal void Invalidate(string path)
        {
            if (AppCache != null)
                AppCache.Invalidate(AppFolderName, path);
        }

        private IEnumerable<NodeHead> ResolveByPathsFromCache(IEnumerable<string> paths, bool resolveChildren, bool all)
        {
            var cache = AppCache.GetPaths(AppFolderName);

            var heads = new List<NodeHead>();
            foreach (var path in paths)
            {
                foreach (var x in cache)
                {
                    if (resolveChildren)
                    {
                        if (String.Compare(RepositoryPath.GetParentPath(x), path, true) == 0)
                        {
                            var head = NodeHead.Get(x);
                            if (head != null)
                                heads.Add(head);
                        }
                    }
                    else
                    {
                        if (String.Compare(x, path, true) == 0)
                        {
                            var head = NodeHead.Get(path);
                            if (head != null)
                                heads.Add(head);
                        }
                        if (!all)
                            return heads;
                    }
                }
            }
            return heads;
        }

        #region IDisposable Members

        private bool _disposed;

        ~ApplicationQuery()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this._disposed)
                if (disposing)
                    AppCacheInvalidator.Invalidate -= new EventHandler<AppCacheInvalidateEventArgs>(AppCacheInvalidator_Invalidate);
            _disposed = true;
        }

        #endregion

        private void AppCacheInvalidator_Invalidate(object sender, AppCacheInvalidateEventArgs e)
        {
            if (AppCache == null)
                return;
            var cachedPaths = AppCache.GetPaths(AppFolderName);

            foreach (var name in e.Path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (String.Compare(name, AppFolderName, true) == 0)
                {
                    Invalidate(e.Path);
                    return;
                }
            }

            var path = e.Path;
            foreach (var item in cachedPaths)
            {
                if (item.StartsWith(path, StringComparison.OrdinalIgnoreCase))
                {
                    Invalidate(e.Path);
                    return;
                }
            }
        }
    }

    internal sealed class AppCacheInvalidator : NodeObserver
    {
        public static event EventHandler<AppCacheInvalidateEventArgs> Invalidate;

        protected override void OnNodeCopied(object sender, NodeOperationEventArgs e)
        {
            OnInvalidate(sender, e.TargetNode.Path);
        }
        protected override void OnNodeCreated(object sender, NodeEventArgs e)
        {
            OnInvalidate(sender, e.SourceNode.Path);
        }
        protected override void OnNodeDeleted(object sender, NodeEventArgs e)
        {
            OnInvalidate(sender, e.SourceNode.Path);
        }
        protected override void OnNodeDeletedPhysically(object sender, NodeEventArgs e)
        {
            OnInvalidate(sender, e.SourceNode.Path);
        }
        protected override void OnNodeMoved(object sender, NodeOperationEventArgs e)
        {
            OnInvalidate(sender, e.SourceNode.Path);
            OnInvalidate(sender, e.TargetNode.Path);
        }

        public static void OnInvalidate(object sender, string path)
        {
            if (Invalidate != null)
                Invalidate(sender, new AppCacheInvalidateEventArgs(path));
        }
    }
    internal class AppCacheInvalidateEventArgs : EventArgs
    {
        public string Path { get; private set; }

        public AppCacheInvalidateEventArgs(string path)
        {
            Path = path;
        }
    }

}