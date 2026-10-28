using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace SenseNet.Diagnostics;

/// <summary>
/// Automatically logs failures on disposal when <see cref="Successful"/> is false (the default).
/// Successful operations require an explicit audit write:
/// <code>
/// var auditEvent = new AuditEvent("MyOperation", 500001);
/// using (var audit = new AuditBlock(auditEvent, "Operation", properties))
/// {
///     PerformOperation();
///     SnLog.WriteAudit(auditEvent, properties); // Explicit success audit.
///     audit.Successful = true;
/// }
/// </code>
/// Example failure entry if <c>PerformOperation()</c> throws (selected fields):
/// <code>
/// EventId: 500001
/// Title: UNSUCCESSFUL MyOperation
/// Message: Operation
/// ExtendedProperties.Execution: UNSUCCESSFUL
/// </code>
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