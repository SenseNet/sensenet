using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SenseNet.Communication.Messaging
{
    [Serializable]
    public class UnknownMessageType : ClusterMessage
    {
        private Stream _messageData;
        public Stream MessageData
        {
            get { return _messageData; }
            set { _messageData = value; }
        }

        public UnknownMessageType() { }
        public UnknownMessageType(Stream messageData) { MessageData = messageData; }
    }
}