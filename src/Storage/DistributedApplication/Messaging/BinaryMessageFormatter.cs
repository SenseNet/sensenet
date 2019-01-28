using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Runtime.Serialization;
using SenseNet.Diagnostics;

namespace SenseNet.Communication.Messaging
{
    public class BinaryMessageFormatter : IClusterMessageFormatter
    {
        #region IMessageFormatter Members

        public ClusterMessage Deserialize(Stream data)
        {
            var bf = new BinaryFormatter();
            ClusterMessage message;

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

        public Stream Serialize(ClusterMessage message)
        {
            var ms = new MemoryStream();
            var bf = new BinaryFormatter();

            bf.Serialize(ms, message); ms.Flush(); ms.Position = 0;

            return ms;
        }

        #endregion
    }
}