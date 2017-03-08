using System.Web.Caching;
using SenseNet.ContentRepository.Storage.Caching.DistributedActions;
using Cache = SenseNet.Configuration.Cache;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    /// <summary>
    /// Creates a dependency that is notified when the portlet changes to invalidate
    /// the related cache item
    /// </summary>
    public class PortletDependency : CacheDependency
    {
        public static object _eventSync = new object();
        private string _portletID;
        private static readonly EventServer<string> Changed = new EventServer<string>(Cache.PortletDependencyEventPartitions);

        public PortletDependency(string portletId)
        {
            try
            {
                this._portletID = portletId;
                lock (_eventSync)
                {
                    Changed.TheEvent += PortletDependency_Changed;
                }
            }
            finally
            {
                this.FinishInit();
            }
        }

        private void PortletDependency_Changed(object sender, EventArgs<string> e)
        {
            if (e.Data == _portletID)
            {
                this.NotifyDependencyChanged(this, e);
            }
        }

        protected override void DependencyDispose()
        {
            lock (_eventSync)
            {
                Changed.TheEvent -= PortletDependency_Changed;
            }
        }

        public static void NotifyChange(string portletId)
        {
            new PortletChangedAction(portletId).Execute();
        }
        public static void FireChanged(string portletId)
        {
            lock (_eventSync)
            {
                Changed.Fire(null, portletId);
            }
        }
    }
}
