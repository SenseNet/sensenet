using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    public class NodeTypeDependencyImplementation : System.Web.Caching.CacheDependency
    {
        private readonly int _nodeTypeId;

        public NodeTypeDependencyImplementation(int nodeTypeId)
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
