using System;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;

namespace SenseNet.Communication.Messaging
{
    [Serializable]
    public abstract class ClusterMessage
    {
        public abstract string TraceMessage { get; }

        public ClusterMessage() { }

        internal ClusterMessage(ClusterMemberInfo sender)
        {
            this.SenderInfo = sender;
        }

		public ClusterMemberInfo SenderInfo { get; set; }

        public Task SendAsync(CancellationToken cancellationToken)
        {
            return Providers.Instance.ClusterChannelProvider.SendAsync(this, cancellationToken);
        }

        protected TimeSpan _messageLifeTime;
    }

}