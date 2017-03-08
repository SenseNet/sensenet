using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.Communication.Messaging;

namespace SenseNet.ContentRepository.Storage.Caching.DistributedActions
{
    [Serializable]
    public class PortletChangedMessage : ClusterMessage
    {
        public string PortletID;

        public PortletChangedMessage() { }
        public PortletChangedMessage(string portletID) { PortletID = portletID; }
    }
}