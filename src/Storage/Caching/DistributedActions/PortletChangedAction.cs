using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
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

        public override void DoAction(bool onRemote, bool isFromMe)
        {
            if (!(onRemote && isFromMe))
                PortletDependency.FireChanged(this.PortletID);
        }

        public override string ToString()
        {
            return "Portlet changed: " + this.PortletID;
        }
    }
}