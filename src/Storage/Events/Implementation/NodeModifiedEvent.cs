using System;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.Events
{
    public class NodeModifiedEvent : ISnEvent<NodeEventArgs>, INodeObserverEvent, IAuditLogEvent
    {
        INodeEventArgs ISnEvent.NodeEventArgs => EventArgs;
        public AuditEvent AuditEvent => AuditEvent.ContentUpdated;

        public NodeModifiedEvent(NodeEventArgs args)
        {
            EventArgs = args;
        }

        public NodeEventArgs EventArgs { get; }

        public Action<NodeObserver> NodeObserverAction => observer =>
        {
            observer.OnNodeModified(null, EventArgs);
        };
    }
}
