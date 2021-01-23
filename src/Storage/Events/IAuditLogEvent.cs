using SenseNet.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.Events
{
    /// <summary>
    /// Declares API elements for an <see cref="ISnEvent"/> that helps to write it to the Audit log.
    /// </summary>
    public interface IAuditLogEvent : ISnEvent
    {
        AuditEvent AuditEvent { get; }
    }
}
