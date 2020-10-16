using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.Diagnostics;
using SenseNet.Tools.Diagnostics;

namespace SenseNet.Storage.Diagnostics
{
    public class InactiveAuditEventWriter : IAuditEventWriter
    {
        public void Write(IAuditEvent auditEvent, IDictionary<string, object> properties)
        {
            // do nothing
        }
    }
}
