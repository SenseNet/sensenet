using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tools.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.Diagnostics
{
    public class DatabaseAuditEventWriter : IAuditEventWriter
    {
        public void Write(IAuditEvent auditEvent, IDictionary<string, object> properties)
        {
            DataStore.WriteAuditEventAsync(new AuditEventInfo(auditEvent, properties)).Wait();
        }
    }
}
