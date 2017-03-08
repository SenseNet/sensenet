using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository
{
    public class WorkflowNotificationEventArgs : EventArgs
    {
        public int NodeId { get; private set; }
        public string NotificationType { get; private set; }
        public object Info { get; private set; }

        public WorkflowNotificationEventArgs(int nodeId, string notificationType, object info)
        {
            this.NodeId = nodeId;
            this.NotificationType = notificationType;
            this.Info = info;
        }
    }
}
