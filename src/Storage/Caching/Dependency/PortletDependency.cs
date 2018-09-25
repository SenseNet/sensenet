using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    internal class PortletDependencyImplementation : System.Web.Caching.CacheDependency
    {
        private readonly string _portletId;
        //private static readonly EventServer<string> Changed = new EventServer<string>(Cache.PortletDependencyEventPartitions);

        public PortletDependencyImplementation(string portletId)
        {
            try
            {
                _portletId = portletId;
                lock (SnCache.EventSync)
                {
                    SnCache.PortletChanged.TheEvent += PortletDependency_Changed;
                }
            }
            finally
            {
                FinishInit();
            }
        }

        private void PortletDependency_Changed(object sender, EventArgs<string> e)
        {
            if (e.Data == _portletId)
            {
                NotifyDependencyChanged(this, e);
                SnTrace.Repository.Write("Cache invalidated by portletId: " + _portletId);
            }
        }

        protected override void DependencyDispose()
        {
            lock (SnCache.EventSync)
            {
                SnCache.PortletChanged.TheEvent -= PortletDependency_Changed;
            }
        }

        //public static void NotifyChange(string portletId)
        //{
        //    new PortletChangedAction(portletId).Execute();
        //}
        //public static void FireChanged(string portletId)
        //{
        //    lock (SnCache.EventSync)
        //    {
        //        SnCache.PortletChanged.Fire(null, portletId);
        //    }
        //}
    }
}
