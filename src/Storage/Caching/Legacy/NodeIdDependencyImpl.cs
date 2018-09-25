using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Caching.Legacy
{
    internal class NodeIdDependencyImpl : System.Web.Caching.CacheDependency
    {
        private readonly int _nodeId;

        public NodeIdDependencyImpl(int nodeId)
        {
            _nodeId = nodeId;
            try
            {
                lock (SnCache.EventSync)
                {
                    //Changed.TheEvent += NodeIdDependency_NodeIdChanged;
                    SnCache.NodeIdChanged.TheEvent += NodeIdDependency_NodeIdChanged;
                }
            }
            finally
            {
                FinishInit();
            }
        }

        private void NodeIdDependency_NodeIdChanged(object sender, EventArgs<int> e)
        {
            if (_nodeId == e.Data)
            {
                NotifyDependencyChanged(this, e);
                SnTrace.Repository.Write("Cache invalidated by nodeId: " + _nodeId);
            }
        }

        protected override void DependencyDispose()
        {
            lock (SnCache.EventSync)
            {
                SnCache.NodeIdChanged.TheEvent -= NodeIdDependency_NodeIdChanged;
            }
        }
    }
}
