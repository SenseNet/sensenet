using System;
using System.Threading;
using System.Threading.Tasks;
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

        public Task SendAsync(CancellationToken cancellationToken)
        {
            return DistributedApplication.ClusterChannel.SendAsync(this, cancellationToken);
        }

        protected TimeSpan _messageLifeTime;
    }

}