using System;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.Events
{
    internal class NodePermissionChangedEvent : ISnEvent<PermissionChangedEventArgs>, INodeObserverEvent, IAuditLogEvent
    {
        INodeEventArgs ISnEvent.NodeEventArgs => EventArgs;
        public AuditEvent AuditEvent => AuditEvent.PermissionChanged;
        public PermissionChangedEventArgs EventArgs { get; }

        public NodePermissionChangedEvent(PermissionChangedEventArgs args)
        {
            EventArgs = args;
        }

        public Action<NodeObserver> NodeObserverAction => observer =>
        {
            observer.OnPermissionChanged(null, EventArgs);
        };
    }
}
