using SenseNet.Communication.Messaging;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Search.Querying;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository
{
    public abstract class TreeCache<T> : NodeObserver where T : Node
    {
        public new static TreeCache<T> GetInstance(Type type)
        {
            return (TreeCache<T>)NodeObserver.GetInstance(type);
        }

        private readonly object _sync = new object();
        private Dictionary<string, TNode> __items;
        protected Dictionary<string, TNode> Items
        {
            get
            {
                if (__items == null)
                {
                    lock (_sync)
                    {
                        if (__items == null)
                            using (new SystemAccount())
                                __items = Build(LoadItems());
                    }
                }
                return __items;
            }
        }

        protected virtual void Invalidate()
        {
            new TreeCacheInvalidatorDistributedAction<T>().Execute();
        }
        protected abstract void InstanceChanged();
        protected void InvalidatePrivate()
        {
            __items = null;

            SnTrace.System.Write("{0} tree cache invalidated.", typeof(T).Name);
        }

        protected override void OnReset(object sender, EventArgs e)
        {
            base.OnReset(sender, e);
            InstanceChanged();
        }

        protected override void OnNodeCreated(object sender, NodeEventArgs e)
        {
            base.OnNodeCreated(sender, e);
            if (e.SourceNode is T)
                Invalidate();
        }
        protected override void OnNodeModified(object sender, NodeEventArgs e)
        {
            base.OnNodeModified(sender, e);
            if(e.OriginalSourcePath!=e.SourceNode.Path)
                if (IsSubtreeContaining(e.OriginalSourcePath))
                    Invalidate();
        }
        protected override void OnNodeDeleted(object sender, NodeEventArgs e)
        {
            base.OnNodeDeleted(sender, e);
            if (IsSubtreeContaining(e.SourceNode.Path))
                Invalidate();
        }
        protected override void OnNodeDeletedPhysically(object sender, NodeEventArgs e)
        {
            base.OnNodeDeletedPhysically(sender, e);
            if (IsSubtreeContaining(e.SourceNode.Path))
                Invalidate();
        }
        protected override void OnNodeCopied(object sender, NodeOperationEventArgs e)
        {
            base.OnNodeCopied(sender, e);
            if (IsSubtreeContaining(e.SourceNode.Path))
                Invalidate();
        }
        protected override void OnNodeMoved(object sender, NodeOperationEventArgs e)
        {
            base.OnNodeMoved(sender, e);
            if (IsSubtreeContaining(e.OriginalSourcePath))
                Invalidate();
        }

        protected bool IsSubtreeContaining(string path)
        {
            var subpath = path + RepositoryPath.PathSeparator;

            return Items.Keys.Any(p =>
                (string.Compare(p, path, StringComparison.InvariantCultureIgnoreCase) == 0) ||
                p.StartsWith(subpath, StringComparison.InvariantCultureIgnoreCase));
        }

        private Dictionary<string, TNode> Build(List<TNode> items)
        {
            return items.ToDictionary(x => x.Path, x => x);
        }
        protected virtual List<TNode> LoadItems()
        {
            throw new SnNotSupportedException();
        }

        protected virtual TNode FindNearestItem(string path, Func<string, string> transform)
        {
            var pr = RepositoryPath.IsValidPath(path);
            if (pr != RepositoryPath.PathResult.Correct)
                throw RepositoryPath.GetInvalidPathException(pr, path);

            var p = path.ToLowerInvariant();
            TNode tnode;
            while (true)
            {
                if (Items.TryGetValue(transform(p), out tnode))
                    return tnode;
                if (p == "/root" || p == "/" || p == string.Empty)
                    break;
                p = RepositoryPath.GetParentPath(p);
            }
            return null;
        }
        protected virtual TNode[] FindNearestItems(string path, Func<string, string> transform)
        {
            var pr = RepositoryPath.IsValidPath(path);
            if (pr != RepositoryPath.PathResult.Correct)
                throw RepositoryPath.GetInvalidPathException(pr, path);

            var p = path.ToLowerInvariant();

            while (true)
            {
                // find all items in the same folder
                var items = Items.Where(kv => 
                    string.Compare(RepositoryPath.GetParentPath(kv.Key), transform(p), StringComparison.InvariantCultureIgnoreCase) == 0)
                    .Select(kv => kv.Value).ToArray();
                if (items.Length > 0)
                    return items;
                if (p == "/root" || p == "/" || p == string.Empty)
                    break;
                p = RepositoryPath.GetParentPath(p);
            }
            return null;
        }

        protected internal class TNode
        {
            public string Path;
            public int Id;
        }

        protected static class Tools
        {
            public static List<TNode> LoadItemsByContentType(string contentTypeName)
            {
                var nodeType = ActiveSchema.NodeTypes[contentTypeName];
                if (nodeType == null)
                    return new List<TNode>();

                return NodeQuery.QueryNodesByType(nodeType, false).Nodes
                    .Select(x => new TNode { Id = x.Id, Path = x.Path.ToLowerInvariant() })
                    .ToList();
            }
        }

        [Serializable]
        private class TreeCacheInvalidatorDistributedAction<Q> : DistributedAction where Q : Node
        {
            public override void DoAction(bool onRemote, bool isFromMe)
            {
                if (onRemote && isFromMe)
                    return;
                var instance = (TreeCache<Q>)NodeObserver.GetInstanceByGenericBaseType(typeof(TreeCache<Q>));
                instance.InvalidatePrivate();
            }
        }
    }
}
