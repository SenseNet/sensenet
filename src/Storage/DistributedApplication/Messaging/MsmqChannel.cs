using System;
using System.Collections.Generic;
using System.Text;
using System.EnterpriseServices;
using System.Configuration;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Messaging;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Storage.Data;
using System.Linq;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Communication.Messaging
{
    /// <summary>
    /// Provides MSMQ based cluster messaging channel.
    /// </summary>
    /// 
    public class MsmqChannelProvider : ClusterChannel
    {
        [Serializable]
        public sealed class StartCheckMessage : DebugMessage
        {
            public StartCheckMessage()
            {
                Message = "Queue test on startup.";
            }
        }
    

        internal List<MessageQueue> _sendQueues;
        private List<bool> _sendQueuesAvailable;
        private ReaderWriterLockSlim _senderLock = new ReaderWriterLockSlim();
        internal MessageQueue _receiveQueue;
        private bool _receiverQueueAvailable;
        private System.Messaging.BinaryMessageFormatter _formatter = new System.Messaging.BinaryMessageFormatter();

        public MsmqChannelProvider(IClusterMessageFormatter formatter,
            ClusterMemberInfo memberInfo)
            : base(formatter, memberInfo)
        { }

        private string _receiverName;
        public override string ReceiverName
        {
            get { return _receiverName; }
        }
        public override List<string> SenderNames
        {
            get { return _sendQueues.Select(x => x.Path).ToList(); }
        }

        private bool _restartingAllChannels;
        public override bool RestartingAllChannels { get { return _restartingAllChannels; } }
        public override void RestartAllChannels()
        {
            if (RestartingAllChannels)
                return;

            _restartingAllChannels = true;

            _senderLock.EnterWriteLock();
            try
            {
                // the queue must be closed and the connection cache cleared before we try to reconnect
                _receiveQueue.Close();
                for (int i = 0; i < _sendQueues.Count; i++)
                    _sendQueues[i].Close();

                MessageQueue.ClearConnectionCache();

                if (Configuration.Messaging.MsmqReconnectDelay > 0)
                    Thread.Sleep(Configuration.Messaging.MsmqReconnectDelay * 1000);

                // reconnect
                _receiveQueue = CreateQueue(_receiveQueue.Path);
                for (int i = 0; i < _sendQueues.Count; i++)
                    _sendQueues[i] = CreateQueue(_sendQueues[i].Path);

                Thread.Sleep(500);
            }
            finally
            {
                _senderLock.ExitWriteLock();
                _restartingAllChannels = false;

            }
        }

        private Message CreateMessage(Stream messageBody)
        {
            var m = new Message(messageBody);
            m.TimeToBeReceived = TimeSpan.FromSeconds(Configuration.Messaging.MessageRetentionTime);
            return m;
        }

        private bool SendToAllQueues(Message message, bool debugMessage)
        {
            var success = true;
            if (debugMessage)
            {
                // if a receiver queue is not available at the moment due to a previous error, don't send the message
                if (_receiverQueueAvailable)
                {
                    try
                    {
                        _receiveQueue.Send(message);
                    }
                    catch (MessageQueueException mex)
                    {
                        SnLog.WriteException(mex);
                        _receiverQueueAvailable = false;    // indicate that the queue is out of order
                        success = false;
                    }
                    catch (Exception ex)
                    {
                        SnLog.WriteException(ex);
                        _receiverQueueAvailable = false;    // indicate that the queue is out of order
                        success = false;
                    }
                }
            }
            for (var i = 0; i < _sendQueues.Count; i++)
            {
                // if a sender queue is not available at the moment due to a previous error, don't send the message
                if (!_sendQueuesAvailable[i])
                    continue;

                try
                {
                    _sendQueues[i].Send(message);
                }
                catch (MessageQueueException mex)
                {
                    SnLog.WriteException(mex);
                    _sendQueuesAvailable[i] = false;    // indicate that the queue is out of order
                    success = false;
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex);
                    _sendQueuesAvailable[i] = false;    // indicate that the queue is out of order
                    success = false;
                }
            }
            return success;
        }

        private void RepairQueues() 
        {
            bool repairHappened = false;
            if (!_receiverQueueAvailable)
            {
                try
                {
                    _receiveQueue = RecoverQueue(_receiveQueue);

                    // indicate that the queue is up and running
                    _receiverQueueAvailable = true;
                    repairHappened = true;
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex);
                }
            }
            for (var i = 0; i < _sendQueues.Count; i++)
            {
                if (!_sendQueuesAvailable[i])
                {
                    try
                    {
                        _sendQueues[i] = RecoverQueue(_sendQueues[i]);

                        // indicate that the queue is up and running
                        _sendQueuesAvailable[i] = true;
                        repairHappened = true;
                    }
                    catch (Exception ex)
                    {
                        SnLog.WriteException(ex);
                    }
                }
            }
            if (repairHappened)
                SnLog.WriteInformation("Send queues have been repaired.");
        }

        protected override void InternalSend(System.IO.Stream messageBody, bool debugMessage)
        {
            var message = CreateMessage(messageBody);
            message.Formatter = _formatter;

            // try to send message to all queues. we enter read lock, since another thread could paralelly repair any of the queues
            bool success;
            _senderLock.EnterReadLock();
            try
            {
                success = SendToAllQueues(message, debugMessage);
            }
            finally
            {
                _senderLock.ExitReadLock();
            }

            // check if any of the queues needs to be restarted
            if (!success)
            {
                // enter write lock, so no send will occur
                _senderLock.EnterWriteLock();
                try
                {
                    RepairQueues();
                }
                finally
                {
                    _senderLock.ExitWriteLock();
                }
            }
        }

        private MessageQueue CreateQueue(string queuepath)
        {
            var q = new MessageQueue(queuepath);
            q.Formatter = new System.Messaging.BinaryMessageFormatter();
            return q;
        }

        protected override void StartMessagePump()
        {
            var queuepaths = Configuration.Messaging.MessageQueueName.Split(';');
            if (queuepaths.Length < 2)
                throw new Exception("No queues have been initialized. Please verify you have provided at least 2 queue paths: first for local, the rest for remote queues!");

            try
            {
                _receiveQueue = CreateQueue(queuepaths[0]);
                _receiverName = ClusterMemberInfo.Current.Machine + _receiveQueue.Path.Substring(1);

                _receiverQueueAvailable = true;
                CheckQueue(_receiveQueue);
                var receiverThread = new Thread(new ThreadStart(ReceiveMessages));
                receiverThread.Start();

                _sendQueues = new List<MessageQueue>();
                _sendQueuesAvailable = new List<bool>();
                foreach (var queuepath in queuepaths.Skip(1))
                {
                    var sendQueue = CreateQueue(queuepath);
                    CheckQueue(sendQueue);
                    _sendQueues.Add(sendQueue);
                    _sendQueuesAvailable.Add(true);
                }
            }
            catch (Exception ex)
            {                
                SnLog.WriteException(ex);
            }
        }

        private void CheckQueue(MessageQueue queue)
        {
            // send a dummy message to local queue to make sure the queue is readable
            var dummyMessage = new StartCheckMessage();
            dummyMessage.SenderInfo = m_clusterMemberInfo;
            var messageBody = m_formatter.Serialize(dummyMessage);

            var message = CreateMessage(messageBody);
            message.Formatter = _formatter;

            try
            {
                queue.Send(message);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("There was an error connecting to queue ({0}).", queue.Path), ex);
            }
        }

        private MessageQueue RecoverQueue(MessageQueue queue)
        {
            // the queue must be closed and the connection cache cleared before we try to reconnect
            queue.Close();
            MessageQueue.ClearConnectionCache();

            if (Configuration.Messaging.MsmqReconnectDelay > 0)
                Thread.Sleep(Configuration.Messaging.MsmqReconnectDelay * 1000);

            // reconnect
            var x = CreateQueue(queue.Path);

            return x;
        }

        private void ReceiveMessages()
        {
            while (true)
            {
                try
                {
                    var message = _receiveQueue.Receive(TimeSpan.FromSeconds(1));
                    if (_shutdown)
                        return;

                    OnMessageReceived(message.Body as Stream);
                }
                catch (ThreadAbortException tex)
                {
                    // suppress threadabortexception on shutdown
                    if (_shutdown)
                        return;

                    SnTrace.Messaging.Write("Thread aborted during receiving a message from the queue ({0}): {1}", _receiveQueue.Path, tex);
                    SnLog.WriteException(tex, String.Format("An error occurred when receiving from the queue ({0}).", _receiveQueue.Path));
                }
                catch (MessageQueueException mex)
                {
                    // check if receive timed out: this is not a problem
                    if (mex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                    {
                        if (_shutdown)
                            return;
                        continue;
                    }

                    SnTrace.Messaging.Write("An error occurred when receiving from the queue ({0}): {1}", _receiveQueue.Path, mex);
                    SnLog.WriteException(mex, String.Format("An error occurred when receiving from the queue ({0}).", _receiveQueue.Path));
                    HandleReceiveException(mex);

                    try
                    {
                        _receiveQueue = RecoverQueue(_receiveQueue);
                    }
                    catch (Exception ex)
                    {
                        SnTrace.Messaging.Write("An error occurred when trying to recover a messaging queue ({0}): {1}", _receiveQueue.Path, ex);
                        SnLog.WriteException(ex);
                    }
                    var thread = new Thread(new ThreadStart(ReceiveMessages));
                    SnTrace.Messaging.Write("Restart thread T" + Thread.CurrentThread.Name);
                    thread.Name = Thread.CurrentThread.Name;
                    thread.Start();
                    return;
                }
                catch (Exception e)
                {
                    SnTrace.Messaging.Write("An error occurred when receiving from the queue ({0}): {1}", _receiveQueue.Path, e);
                    SnLog.WriteException(e, string.Format("An error occurred when receiving from the queue ({0}).", _receiveQueue.Path));
                    HandleReceiveException(e);
                }
            }
        }

        internal void HandleReceiveException(Exception e)
        {
            OnReceiveException(e);
        }

        public override void Purge()
        {
            var count = 0;
            var iterator = _receiveQueue.GetMessageEnumerator2();
            while (iterator.MoveNext())
                count++;

            SnLog.WriteInformation("MsmqChannel Purge", EventId.Messaging, properties: new Dictionary<string, object>
            {
                {"MachineName", _receiveQueue.MachineName},
                {"DeletedMessages", count}
            });

            _receiveQueue.Purge();
        }
    }
}