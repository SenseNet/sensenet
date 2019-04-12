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
            if (DataStore.Enabled)
                DataStore.WriteAuditEvent(new AuditEventInfo(auditEvent, properties));
            else
                DataProvider.Current.WriteAuditEvent(new AuditEventInfo(auditEvent, properties));
        }
    }
}
