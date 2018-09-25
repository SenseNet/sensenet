using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Caching.Legacy
{
    internal class PortletDependencyImpl : System.Web.Caching.CacheDependency
    {
        private readonly string _portletId;

        public PortletDependencyImpl(string portletId)
        {
            _portletId = portletId;
            try
            {
                PortletDependency.Subscribe(PortletDependency_Changed);
            }
            finally
            {
                FinishInit();
            }
        }
        protected override void DependencyDispose()
        {
            PortletDependency.Unsubscribe(PortletDependency_Changed);
        }

        private void PortletDependency_Changed(object sender, EventArgs<string> e)
        {
            if (e.Data == _portletId)
            {
                NotifyDependencyChanged(this, e);
                SnTrace.Repository.Write("Cache invalidated by portletId: " + _portletId);
            }
        }
    }
}
