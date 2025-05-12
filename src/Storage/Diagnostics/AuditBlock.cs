using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace SenseNet.Diagnostics;

/// <summary>
/// Ensures that an audit event is logged when the block is disposed.
/// WARNING: writes event only if the code exit unexpectedly from the using block (Successful == false).
/// The successful block must be performed with explicit audit writing.
/// </summary>
public class AuditBlock : IDisposable
{
    private readonly AuditEvent _auditEvent;
    private readonly string _operationName;
    private readonly IDictionary<string, object> _properties;
    private readonly DateTime _startedAt;

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