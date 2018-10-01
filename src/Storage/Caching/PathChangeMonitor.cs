using SenseNet.ContentRepository.Storage.Caching.Dependency;

namespace SenseNet.ContentRepository.Storage.Caching
{
    internal class PathChangeMonitor : ChangeMonitorBase
    {
        private readonly string _path;

        public PathChangeMonitor(string path)
        {
            _path = path;
            try
            {
                PathDependency.Subscribe(Changed);
            }
            finally
            {
                InitializationComplete();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                PathDependency.Unsubscribe(Changed);
        }

        private void Changed(object sender, EventArgs<string> e)
        {
            if (PathDependency.IsChanged(e.Data, _path))
                OnChanged(null);
        }
    }
}
