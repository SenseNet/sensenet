using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Caching.Legacy
{
    public class NodeTypeDependencyImpl : System.Web.Caching.CacheDependency
    {
        private readonly int _nodeTypeId;

        public NodeTypeDependencyImpl(int nodeTypeId)
        {
            _nodeTypeId = nodeTypeId;
            try
            {
                lock (SnCache.EventSync)
                {
                    SnCache.NodeTypeChanged.TheEvent += NodeTypeDependency_NodeTypeChanged;
                }
            }
            finally
            {
                FinishInit();
            }
        }

        private void NodeTypeDependency_NodeTypeChanged(object sender, EventArgs<int> e)
        {
            if (e.Data == _nodeTypeId)
            {
                NotifyDependencyChanged(this, e);
                SnTrace.Repository.Write("Cache invalidated by nodeTypeId: " + _nodeTypeId);
            }
        }

        protected override void DependencyDispose()
        {
            lock (SnCache.EventSync)
            {
                SnCache.NodeTypeChanged.TheEvent -= NodeTypeDependency_NodeTypeChanged;
            }
        }
    }
}
