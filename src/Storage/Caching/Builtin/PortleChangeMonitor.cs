using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Caching.Dependency;

namespace SenseNet.ContentRepository.Storage.Caching.Builtin
{
    internal class PortleChangeMonitor : ChangeMonitorBase
    {
        private readonly string _portletId;

        public PortleChangeMonitor(string portletId)
        {
            _portletId = portletId;
            try
            {
                PortletDependency.Subscribe(Changed);
            }
            finally
            {
                InitializationComplete();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                PortletDependency.Unsubscribe(Changed);
        }

        private void Changed(object sender, EventArgs<string> e)
        {
            if (PortletDependency.IsChanged(e.Data, _portletId))
                OnChanged(null);
        }
    }
}
