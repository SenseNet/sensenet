using System;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.Events
{
    public class NodeDeletedEvent : ISnEvent<NodeEventArgs>, INodeObserverEvent, IAuditLogEvent
    {
        INodeEventArgs ISnEvent.NodeEventArgs => EventArgs;
        public AuditEvent AuditEvent => AuditEvent.ContentUpdated;
        public NodeEventArgs EventArgs { get; }

        public NodeDeletedEvent(NodeEventArgs args)
        {
            EventArgs = args;
        }

        public Action<NodeObserver> NodeObserverAction => observer =>
        {
            observer.OnNodeDeleted(null, EventArgs);
        };
    }
}
