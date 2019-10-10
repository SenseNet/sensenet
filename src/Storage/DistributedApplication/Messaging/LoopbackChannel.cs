using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Communication.Messaging
{
    public class LoopbackChannel : ClusterChannel
    {
        public LoopbackChannel(IClusterMessageFormatter formatter, 
            ClusterMemberInfo clusterMemberInfo) : base(formatter, clusterMemberInfo)
        {
        }
        protected override Task InternalSendAsync(System.IO.Stream messageBody, bool isDebugMessage, CancellationToken cancellationToken)
        {
            this.OnMessageReceived(messageBody);
            return Task.CompletedTask;
        }
        public override bool RestartingAllChannels => false;

        public override Task RestartAllChannelsAsync(CancellationToken cancellationToken)
        {
            // do nothing
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Provides a dummy "Null" cluster channel. The sent messages will be ignored.
    /// </summary>
    public class VoidChannel : ClusterChannel
    {
        public VoidChannel(IClusterMessageFormatter formatter, 
            ClusterMemberInfo clusterMemberInfo) : base(formatter, clusterMemberInfo)
        {
        }
        protected override Task InternalSendAsync(System.IO.Stream messageBody, bool isDebugMessage, CancellationToken cancellationToken)
        {
            // do nothing
            return Task.CompletedTask;
        }
        public override bool RestartingAllChannels => false;

        public override Task RestartAllChannelsAsync(CancellationToken cancellationToken)
        {
            // do nothing
            return Task.CompletedTask;
        }
    }
}