using SenseNet.ContentRepository.Storage.Caching.Dependency;

namespace SenseNet.ContentRepository.Storage.Caching.Builtin
{
    internal class NodeIdChangeMonitor : ChangeMonitorBase
    {
        private readonly int _nodeId;

        public NodeIdChangeMonitor(int nodeId)
        {
            _nodeId = nodeId;
            try
            {
                NodeIdDependency.Subscribe(Changed);
            }
            finally
            {
                InitializationComplete();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                NodeIdDependency.Unsubscribe(Changed);
        }

        private void Changed(object sender, EventArgs<int> e)
        {
            if (NodeIdDependency.IsChanged(e.Data, _nodeId))
                OnChanged(null);
        }
    }
}
