// ReSharper disable once CheckNamespace
namespace SenseNet.Events
{
    /// <summary>
    /// Marker interface for an <see cref="ISnEvent"/> that will not be fired on the
    /// any <see cref="IEventProcessor"/> except NodeObservers and <see cref="AuditLogEventProcessor"/>.
    /// </summary>
    public interface IInternalEvent
    {
    }
}
