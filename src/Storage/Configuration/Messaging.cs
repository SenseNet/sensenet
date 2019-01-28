// ReSharper disable once CheckNamespace
namespace SenseNet.Configuration
{
    public class Messaging : SnConfig
    {
        private const string SectionName = "sensenet/messaging";

        public static string MessageQueueName { get; internal set; } = GetString(SectionName, "MsmqChannelQueueName", string.Empty);

        /// <summary>
        /// Retention time of messages in the message queue in seconds. Default: 10, minimum: 2
        /// </summary>
        public static int MessageRetentionTime { get; internal set; } = GetInt(SectionName, "MessageRetentionTime", 10, 2);

        /// <summary>
        /// Defines the time interval between reconnect attempts (in seconds).  Default value: 1 sec.
        /// </summary>
        public static int ReconnectDelay { get; internal set; } = GetInt(SectionName, "ReconnectDelay", 1);

        /// <summary>
        /// Number of clusterchannel message processor threads. Default is 5.
        /// </summary>
        public static int MessageProcessorThreadCount { get; internal set; } = GetInt(SectionName, "MessageProcessorThreadCount", 5);

        /// <summary>
        /// Max number of messages processed by a single clusterchannel message processor thread. Default is 100.
        /// </summary>
        public static int MessageProcessorThreadMaxMessages { get; internal set; } = GetInt(SectionName, "MessageProcessorThreadMaxMessages", 100);

        /// <summary>
        /// Number of messages in process queue to trigger delaying of incoming requests. Default is 1000.
        /// </summary>
        public static int DelayRequestsOnHighMessageCountUpperLimit { get; internal set; } = GetInt(SectionName, 
            "DelayRequestsOnHighMessageCountUpperLimit", 1000);

        /// <summary>
        /// Number of messages in process queue to switch off delaying of incoming requests. Default is 500.
        /// </summary>
        public static int DelayRequestsOnHighMessageCountLowerLimit { get; internal set; } = GetInt(SectionName, 
            "DelayRequestsOnHighMessageCountLowerLimit", 500);

        /// <summary>
        /// Max size (in bytes) of indexdocument that can be sent over MSMQ. Default is 200000. Larger indexdocuments will be retrieved from db. 
        /// </summary>
        public static int MsmqIndexDocumentSizeLimit { get; internal set; } = GetInt(SectionName, "MsmqIndexDocumentSizeLimit", 200000);

        /// <summary>
        /// Cluster channel checking interval in seconds. Default: 60, minimum 10.
        /// </summary>
        public static int ClusterChannelMonitorInterval { get; internal set; } = GetInt(SectionName, "ClusterChannelMonitorInterval", 60, 10);

        /// <summary>
        /// Time limit for receiving response of the cluster channel test message in seconds. Default: 10, minimum 1.
        /// </summary>
        public static int ClusterChannelMonitorTimeout { get; internal set; } = GetInt(SectionName, "ClusterChannelMonitorTimeout", 10, 1);
    }
}
