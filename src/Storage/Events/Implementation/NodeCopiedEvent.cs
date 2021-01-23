using System;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.Events
{
    public class NodeCopiedEvent : ISnEvent<NodeOperationEventArgs>, INodeObserverEvent, IAuditLogEvent
    {
        INodeEventArgs ISnEvent.NodeEventArgs => EventArgs;
        public AuditEvent AuditEvent => AuditEvent.ContentUpdated;
        public NodeOperationEventArgs EventArgs { get; }

        public NodeCopiedEvent(NodeOperationEventArgs args)
        {
            EventArgs = args;
        }

        public Action<NodeObserver> NodeObserverAction => observer =>
        {
            observer.OnNodeCopied(null, EventArgs);
        };
    }
}
