using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.Communication.Messaging;

namespace SenseNet.ContentRepository.Storage.Caching.DistributedActions
{
    [Obsolete("Do not use this class anymore.")]
    [Serializable]
    public class PortletChangedMessage : ClusterMessage
    {
        public override string TraceMessage => null;

        public string PortletID;

        public PortletChangedMessage() { }
        public PortletChangedMessage(string portletID) { PortletID = portletID; }
    }
}