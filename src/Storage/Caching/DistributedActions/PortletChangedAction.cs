using System;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.Communication.Messaging;

namespace SenseNet.ContentRepository.Storage.Caching.DistributedActions
{
    [Obsolete("Do not use this class anymore.")]
    [Serializable]
    public class PortletChangedAction : DistributedAction
    {
        public string PortletID;

        public PortletChangedAction() { }
        public PortletChangedAction(string portletID) 
        {
            PortletID = portletID;
        }

        public override Task DoActionAsync(bool onRemote, bool isFromMe, CancellationToken cancellationToken)
        {
            if (!(onRemote && isFromMe))
                PortletDependency.FireChanged(this.PortletID);

            return Task.CompletedTask;
        }

        public override string ToString()
        {
            return "Portlet changed: " + this.PortletID;
        }
    }
}