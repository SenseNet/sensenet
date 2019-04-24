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
            if (DataStore.Enabled) DataStore.WriteAuditEventAsync(new AuditEventInfo(auditEvent, properties)).Wait(); else DataProvider.Current.WriteAuditEvent(new AuditEventInfo(auditEvent, properties)); //DB:ok
        }
    }
}
