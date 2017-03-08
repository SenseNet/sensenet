using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Configuration;
using SenseNet.ContentRepository;
using SenseNet.Diagnostics;
using System.Threading;
using System.Diagnostics;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.Communication.Messaging
{
    
    public abstract class ClusterChannel : IClusterChannel
    {
        /* ============================================================================== Members */
        private static List<ClusterMessage> _incomingMessages;
        private static volatile int _messagesCount;
        protected static bool _shutdown;
        private static object _messageListSwitchSync = new object();
        protected IClusterMessageFormatter m_formatter;
        protected ClusterMemberInfo m_clusterMemberInfo;

        public bool AllowMessageProcessing { get; set; }

        /* ============================================================================== Properties */
        public ClusterMemberInfo ClusterMemberInfo
        {
            get { return m_clusterMemberInfo; }
        }
        private int _incomingMessageCount;
        public int IncomingMessageCount
        {
            get
            {
                return _incomingMessageCount;
            }
        }

        /* ============================================================================== Events */
        public event MessageReceivedEventHandler MessageReceived;
        public event ReceiveExceptionEventHandler ReceiveException;
        public event SendExceptionEventHandler SendException;

        /* ============================================================================== Init */
        public ClusterChannel(IClusterMessageFormatter formatter, ClusterMemberInfo clusterMemberInfo)
        {
            _incomingMessages = new List<ClusterMessage>();
            CounterManager.Reset("IncomingMessages");
            CounterManager.Reset("TotalMessagesToProcess");

            m_formatter = formatter;
            m_clusterMemberInfo = clusterMemberInfo;

            // initiate processing threads
            for (var i = 0; i < Configuration.Messaging.MessageProcessorThreadCount; i++)
            {
                try
                {
                    var thstart = new ParameterizedThreadStart(CheckProcessableMessages);
                    var thread = new Thread(thstart);
                    thread.Name = i.ToString();
                    thread.Start();
                    SnTrace.Messaging.Write("ClusterChannel: 'CheckProcessableMessages' thread started. ManagedThreadId: {0}", thread.ManagedThreadId);
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex);
                }
            }
        }
        protected virtual void StartMessagePump()
        {

        }
        protected virtual void StopMessagePump()
        {
            _shutdown = true;
        }
        public virtual void Start()
        {
            SnTrace.Messaging.Write("ClusterChannel start");
            StartMessagePump();
        }
        public virtual void ShutDown()
        {
            SnTrace.Messaging.Write("ClusterChannel shutdown");
            StopMessagePump();
        }

        /* ============================================================================== Send */
        public virtual void Send(ClusterMessage message)
        {
            try
            {
                message.SenderInfo = m_clusterMemberInfo;
                message.SenderInfo.ClusterMemberID = this.ReceiverName;
                Stream messageStream = m_formatter.Serialize(message);
                SnTrace.Messaging.Write("Sending a '{0}' message", message.GetType().FullName);
                InternalSend(messageStream, message is DebugMessage);
            }
            catch (Exception e) // logged
            {
                SnLog.WriteException(e);
                SnTrace.Messaging.WriteError("Sending a '{0}' message. Exception: {1}", message.GetType().FullName, e);
                OnSendException(message, e);
            }
        }
        protected abstract void InternalSend(Stream messageBody, bool isDebugMessage);

        /* ============================================================================== Receive */
        private void CheckProcessableMessages(object parameter)
        {
            List<ClusterMessage> messagesToProcess;
            while (true)
            {
                try
                {
                    if (AllowMessageProcessing)
                    {
                        while ((messagesToProcess = GetProcessableMessages()) != null)
                        {
                            var count = messagesToProcess.Count;
#pragma warning disable 420
                            Interlocked.Add(ref _messagesCount, count);
#pragma warning restore 420

                            // process all messages in the queue
                            for (var i = 0; i < count; i++)
                            {
                                ProcessSingleMessage(messagesToProcess[i]);
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
            List<ClusterMessage> messagesToProcess = null;
            lock (_messageListSwitchSync)
            {
                _incomingMessageCount = _incomingMessages.Count;

                if (_incomingMessageCount == 0)
                    return null;

                if (_incomingMessageCount <= Configuration.Messaging.MessageProcessorThreadMaxMessages)
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
        private void ProcessSingleMessage(object parameter)
        {
            var message = parameter as ClusterMessage;
            var msg = message as DistributedAction;
            if (msg != null)
            {
                var isMe = msg.SenderInfo.IsMe;
                SnTrace.Messaging.Write("Processing a '{0}' message. IsMe: {1}", message.GetType().FullName, isMe);
                msg.DoAction(true, msg.SenderInfo.IsMe);
            }
            else
            {
                SnTrace.Messaging.Write("Processing a '{0}' message.", message.GetType().FullName);
                var pingMessage = message as PingMessage;
                if (pingMessage != null)
                    new PongMessage() { PingId = pingMessage.Id }.Send();
            }
            if (MessageReceived != null)
                MessageReceived(this, new MessageReceivedEventArgs(message));
        }
        internal virtual void OnMessageReceived(Stream messageBody)
        {
            ClusterMessage message = m_formatter.Deserialize(messageBody);
            SnTrace.Messaging.Write("Received a '{0}' message.", message.GetType().FullName);

            lock (_messageListSwitchSync)
            {
                _incomingMessages.Add(message);
                CounterManager.SetRawValue("IncomingMessages", Convert.ToInt64(_incomingMessages.Count));
                var totalMessages = _incomingMessages.Count + _messagesCount;
                CounterManager.SetRawValue("TotalMessagesToProcess", Convert.ToInt64(totalMessages));
            }
        }

        public virtual string ReceiverName { get { return null; } }
        public virtual List<string> SenderNames { get { return new List<string>(); } }
        public abstract bool RestartingAllChannels { get; }
        public abstract void RestartAllChannels();

        /* ============================================================================== Purge */
        public virtual void Purge()
        {
            // do nothing
        }

        /* ============================================================================== Error handling */
        protected virtual void OnSendException(ClusterMessage message, Exception exception)
        {
            if (SendException != null)
                SendException(this, new ExceptionEventArgs(exception, message));
        }
        protected virtual void OnReceiveException(Exception exception)
        {
            if (ReceiveException != null)
                ReceiveException(this, new ExceptionEventArgs(exception, null));
        }
    }
}