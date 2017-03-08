using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Diagnostics;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Activation;

namespace SenseNet.Services
{
    [ServiceContract(Namespace = LogService.LogServiceNamespace)]
    public interface ILogService
    {
        [OperationContract]
        void Write(ClientLogEntry entry);
    }

    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class LogService : ILogService
    {
        public const string LogServiceNamespace = "http://schemas.sensenet.com/services/log";

        public void Write(ClientLogEntry entry)
        {
            switch (entry.Severity)
            {
                case TraceEventType.Critical:
                case TraceEventType.Error:
                    SnLog.WriteError(entry.Message, entry.EventId, entry.Categories, entry.Priority, entry.Title, entry.Properties);
                    break;
                case TraceEventType.Warning:
                    SnLog.WriteWarning(entry.Message, entry.EventId, entry.Categories, entry.Priority, entry.Title, entry.Properties);
                    break;
                case TraceEventType.Verbose:
                    // do nothing: verbose log should be written using SnTrace
                    break;
                case TraceEventType.Information:
                    SnLog.WriteInformation(entry.Message, entry.EventId, entry.Categories, entry.Priority, entry.Title, entry.Properties);
                    break;
                default:
                    SnLog.WriteInformation(entry.Severity + ": " + entry.Message, entry.EventId, entry.Categories, entry.Priority, entry.Title, entry.Properties);
                    break;
            }

        }
    }

    [DataContract]
    public class ClientLogEntry
    {
        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public string[] Categories { get; set; }
        [DataMember]
        public int Priority { get; set; }
        [DataMember]
        public int EventId { get; set; }
        [DataMember]
        public TraceEventType Severity { get; set; }
        [DataMember]
        public string Title { get; set; }
        [DataMember]
        public IDictionary<string, object> Properties { get; set; }
    }
}
