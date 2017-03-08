using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository;

namespace SenseNet.Communication.Messaging
{
    [Serializable]
    public abstract class ClusterMessage
    {
        public ClusterMessage() { }

        internal ClusterMessage(ClusterMemberInfo sender)
        {
            this.SenderInfo = sender;
        }

		public ClusterMemberInfo SenderInfo { get; internal set; }

        public void Send()
        {
            DistributedApplication.ClusterChannel.Send(this);
        }

        protected TimeSpan _messageLifeTime;
    }

}