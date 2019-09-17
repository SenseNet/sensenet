using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SenseNet.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Communication.Messaging
{
    
    public abstract class ClusterChannel : IClusterChannel
    {
        /* ============================================================================== Members */
        private static List<ClusterMessage> _incomingMessages;
        private static volatile int _messagesCount;
        protected static bool _shutdown;
        private static readonly object MessageListSwitchSync = new object();
        protected IClusterMessageFormatter m_formatter;
        protected ClusterMemberInfo m_clusterMemberInfo;

        public bool AllowMessageProcessing { get; set; }

        /* ============================================================================== Properties */
        public ClusterMemberInfo ClusterMemberInfo => m_clusterMemberInfo;
        public int IncomingMessageCount { get; private set; }

        /* ============================================================================== Events */
        public event MessageReceivedEventHandler MessageReceived;
        public event ReceiveExceptionEventHandler ReceiveException;
        public event SendExceptionEventHandler SendException;

        /* ============================================================================== Init */
        public ClusterChannel(IClusterMessageFormatter formatter, ClusterMemberInfo clusterMemberInfo)
        {
            _incomingMessages = new List<ClusterMessage>();
            
            m_formatter = formatter;
            m_clusterMemberInfo = clusterMemberInfo;

            // initiate processing threads
            for (var i = 0; i < Configuration.Messaging.MessageProcessorThreadCount; i++)
            {
                try
                {
                    var threadStart = new ParameterizedThreadStart(CheckProcessableMessages);
                    var thread = new Thread(threadStart)
                    {
                        Name = i.ToString()
                    };
                    thread.Start();

                    SnTrace.Messaging.Write("ClusterChannel: 'CheckProcessableMessages' thread started. ManagedThreadId: {0}", thread.ManagedThreadId);
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex);
                }
            }
        }
        protected virtual Task StartMessagePumpAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
        protected virtual Task StopMessagePumpAsync(CancellationToken cancellationToken)
        {
            _shutdown = true;
            return Task.CompletedTask;
        }
        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            SnTrace.Messaging.Write("ClusterChannel start");
            return StartMessagePumpAsync(cancellationToken);
        }
        public virtual Task ShutDownAsync(CancellationToken cancellationToken)
        {
            SnTrace.Messaging.Write("ClusterChannel shutdown");
            return StopMessagePumpAsync(cancellationToken);
        }

        /* ============================================================================== Send */
        public virtual async Task SendAsync(ClusterMessage message, CancellationToken cancellationToken)
        {
            try
            {
                message.SenderInfo = m_clusterMemberInfo;
                message.SenderInfo.ClusterMemberID = ReceiverName;

                Stream messageStream = m_formatter.Serialize(message);
                SnTrace.Messaging.Write("Sending a '{0}' message", message.GetType().FullName);

                await InternalSendAsync(messageStream, message is DebugMessage, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) // logged
            {
                SnLog.WriteException(e);
                SnTrace.Messaging.WriteError("Sending a '{0}' message. Exception: {1}", message.GetType().FullName, e);
                OnSendException(message, e);
            }
        }
        protected abstract Task InternalSendAsync(Stream messageBody, bool isDebugMessage, CancellationToken cancellationToken);

        /* ============================================================================== Receive */

        private void CheckProcessableMessages(object data)
        {
            CheckProcessableMessagesAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        private async Task CheckProcessableMessagesAsync(CancellationToken cancellationToken)
        {
            SnTrace.Messaging.Write("ClusterChannel: CheckProcessableMessages started.");

            while (true)
            {
                try
                {
                    if (AllowMessageProcessing)
                    {
                        List<ClusterMessage> messagesToProcess;
                        while ((messagesToProcess = GetProcessableMessages()) != null)
                        {
                            var count = messagesToProcess.Count;
#pragma warning disable 420
                            Interlocked.Add(ref _messagesCount, count);
#pragma warning restore 420

                            // process all messages in the queue
                            for (var i = 0; i < count; i++)
                            {
                                //TODO: [async] handle cancellation during message processing
                                //TODO: [async] process messages in parallel if possible
                                await ProcessSingleMessageAsync(messagesToProcess[i], cancellationToken).ConfigureAwait(false);
                                messagesToProcess[i] = null;
                            }

#pragma warning disable 420
                            Interlocked.Add(ref _messagesCount, -count);
#pragma warning restore 420

                            if (_shutdown)
                                return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex);
                }

                // no messages to process, wait some time and continue checking incoming messages
                Thread.Sleep(100);
               
                if (_shutdown)
                    return;
            }
        }
        private List<ClusterMessage> GetProcessableMessages()
        {
            List<ClusterMessage> messagesToProcess;

            lock (MessageListSwitchSync)
            {
                IncomingMessageCount = _incomingMessages.Count;

                if (IncomingMessageCount == 0)
                    return null;

                if (IncomingMessageCount <= Configuration.Messaging.MessageProcessorThreadMaxMessages)
                {
                    // if total message count is smaller than the maximum allowed, process all of them and empty incoming queue
                    messagesToProcess = _incomingMessages;
                    _incomingMessages = new List<ClusterMessage>();
                }
                else
                {
                    // process the maximum allowed number of messages, leave the rest in the incoming queue
                    messagesToProcess = _incomingMessages.Take(Configuration.Messaging.MessageProcessorThreadMaxMessages).ToList();
                    _incomingMessages = _incomingMessages.Skip(Configuration.Messaging.MessageProcessorThreadMaxMessages).ToList();
                }
            }

            return messagesToProcess;
        }
        private async Task ProcessSingleMessageAsync(object parameter, CancellationToken cancellationToken)
        {
            var message = parameter as ClusterMessage;

            if (message is DistributedAction msg)
            {
                var isMe = msg.SenderInfo.IsMe;
                SnTrace.Messaging.Write("Processing a '{0}' message. IsMe: {1}", message.GetType().FullName, isMe);

                await msg.DoActionAsync(true, msg.SenderInfo.IsMe, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                SnTrace.Messaging.Write("Processing a '{0}' message.", message?.GetType().FullName ?? "unknown");
                if (message is PingMessage pingMessage)
                {
                    var pm = new PongMessage {PingId = pingMessage.Id};
                    await pm.SendAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
        }
        protected internal virtual void OnMessageReceived(Stream messageBody)
        {
            ClusterMessage message = m_formatter.Deserialize(messageBody);
            SnTrace.Messaging.Write("Received a '{0}' message.", message.GetType().FullName);

            lock (MessageListSwitchSync)
            {
                _incomingMessages.Add(message);

                //TODO: trace incoming and total message count
                // var totalMessages = _incomingMessages.Count + _messagesCount;
            }
        }

        public virtual string ReceiverName => null;
        public virtual List<string> SenderNames => new List<string>();
        public abstract bool RestartingAllChannels { get; }
        public abstract Task RestartAllChannelsAsync(CancellationToken cancellationToken);

        /* ============================================================================== Purge */
        public virtual Task PurgeAsync(CancellationToken cancellationToken)
        {
            // do nothing
            return Task.CompletedTask;
        }

        /* ============================================================================== Error handling */
        protected virtual void OnSendException(ClusterMessage message, Exception exception)
        {
            SendException?.Invoke(this, new ExceptionEventArgs(exception, message));
        }
        protected virtual void OnReceiveException(Exception exception)
        {
            ReceiveException?.Invoke(this, new ExceptionEventArgs(exception, null));
        }
    }
}