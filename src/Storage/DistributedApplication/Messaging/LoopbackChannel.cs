using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace SenseNet.Communication.Messaging
{
    public class LoopbackChannel : ClusterChannel
    {
        public LoopbackChannel(IClusterMessageFormatter formatter, 
            ClusterMemberInfo clusterMemberInfo) : base(formatter, clusterMemberInfo)
        {
        }
        protected override void InternalSend(System.IO.Stream messageBody, bool isDebugMessage)
        {
            this.OnMessageReceived(messageBody);
        }
        public override bool RestartingAllChannels { get { return false; } }
        public override void RestartAllChannels()
        {
            // do nothing
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
        protected override void InternalSend(System.IO.Stream messageBody, bool isDebugMessage)
        {
            // do nothing
        }
        public override bool RestartingAllChannels { get { return false; } }
        public override void RestartAllChannels()
        {
            // do nothing
        }
    }
}