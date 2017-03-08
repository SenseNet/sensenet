using SenseNet.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Diagnostics
{
    public class AuditBlock : IDisposable
    {
        private AuditEvent _auditEvent;
        private string _operationName;
        private IDictionary<string, object> _properties;
        private DateTime _startedAt;

        public bool Successful { get; set; }

        public AuditBlock(AuditEvent auditEvent, string operationName, IDictionary<string, object> properties)
        {
            _auditEvent = auditEvent;
            _operationName = operationName ?? auditEvent.ToString();
            _properties = properties ?? new Dictionary<string, object>();
            _startedAt = DateTime.UtcNow;
        }

        public void Dispose()
        {
            if (!Successful)
            {
                _properties.Add("Execution", "UNSUCCESSFUL");
                _properties.Add("OriginalEvent", _auditEvent);
                _properties.Add("StartedAt", _startedAt);

                var result = new AuditEvent(_auditEvent.AuditCategory, _auditEvent.EventId, "UNSUCCESSFUL " + _auditEvent.AuditCategory, _operationName);
                SnLog.WriteAudit(result, _properties);
            }
        }
    }
}
