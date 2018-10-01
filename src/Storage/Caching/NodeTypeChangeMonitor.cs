using SenseNet.ContentRepository.Storage.Caching.Dependency;

namespace SenseNet.ContentRepository.Storage.Caching
{
    internal class NodeTypeChangeMonitor : ChangeMonitorBase
    {
        private readonly int _nodeTypeId;

        public NodeTypeChangeMonitor(int nodeTypeId)
        {
            _nodeTypeId = nodeTypeId;
            try
            {
                NodeTypeDependency.Subscribe(Changed);
            }
            finally
            {
                InitializationComplete();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                NodeTypeDependency.Unsubscribe(Changed);
        }

        private void Changed(object sender, EventArgs<int> e)
        {
            if (NodeTypeDependency.IsChanged(e.Data, _nodeTypeId))
                OnChanged(null);
        }
    }
}
