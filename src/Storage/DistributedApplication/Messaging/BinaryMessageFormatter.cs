using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;
using SenseNet.Diagnostics;

namespace SenseNet.Communication.Messaging
{
    public class BinaryMessageFormatter : IClusterMessageFormatter
    {
        #region IMessageFormatter Members

        public ClusterMessage Deserialize(System.IO.Stream data)
        {
            BinaryFormatter bf = new BinaryFormatter();
            ClusterMessage message = null;
            try
            {
                message = (ClusterMessage)bf.Deserialize(data);
            }
            catch (SerializationException e) // logged
            {
                SnLog.WriteException(e);
                message = new UnknownMessageType(data);
            }
            return message;
        }

        public object InternalHeaderHandler(Header[] headers)
        {
            object o = headers;
            return o;
        }
        public System.IO.Stream Serialize(ClusterMessage message)
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, message); ms.Flush(); ms.Position = 0;
            return ms;
        }

        #endregion
    }
}