using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Communication.Messaging
{
    public interface IClusterChannel
    {
        ClusterMemberInfo ClusterMemberInfo { get; }
        Task SendAsync(ClusterMessage message, CancellationToken cancellationToken);

        event MessageReceivedEventHandler MessageReceived;
        event ReceiveExceptionEventHandler ReceiveException;
        event SendExceptionEventHandler SendException;

        Task StartAsync(CancellationToken cancellationToken);
        Task ShutDownAsync(CancellationToken cancellationToken);
        Task PurgeAsync(CancellationToken cancellationToken);

        bool AllowMessageProcessing { get; set; }
        int IncomingMessageCount { get; }

        string ReceiverName { get; }
        List<string> SenderNames { get; }

        bool RestartingAllChannels { get; }
        Task RestartAllChannelsAsync(CancellationToken cancellationToken);
    }
    
    public delegate void MessageReceivedEventHandler(object sender, MessageReceivedEventArgs args);
    public delegate void ReceiveExceptionEventHandler(object sender, ExceptionEventArgs args);
    public delegate void SendExceptionEventHandler(object sender, ExceptionEventArgs args);

    public class MessageReceivedEventArgs : EventArgs
    {
        public ClusterMessage Message { get; set; }

        public MessageReceivedEventArgs() { }
        public MessageReceivedEventArgs(ClusterMessage message) { Message = message; }
    }

    public class ExceptionEventArgs : EventArgs
    {
        public Exception Exception { get; set; }
        public ClusterMessage Message { get; set; }

        public ExceptionEventArgs() { }
        public ExceptionEventArgs(Exception exception, ClusterMessage message)
        {
            Exception = exception;
            Message = message;
        }
    }
    
    public interface IClusterMessageFormatter
    {
        ClusterMessage Deserialize(Stream data);
        Stream Serialize(ClusterMessage message);
    }
}