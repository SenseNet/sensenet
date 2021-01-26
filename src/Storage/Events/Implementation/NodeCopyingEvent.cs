using System;
using SenseNet.ContentRepository.Storage.Events;

// ReSharper disable once CheckNamespace
namespace SenseNet.Events
{
    internal class NodeCopyingEvent : ISnCancellableEvent<CancellableNodeOperationEventArgs>
    {
        INodeEventArgs ISnEvent.NodeEventArgs => EventArgs;
        CancellableNodeEventArgs ISnCancellableEvent.CancellableEventArgs => EventArgs;
        public CancellableNodeOperationEventArgs EventArgs { get; }

        public NodeCopyingEvent(CancellableNodeOperationEventArgs args)
        {
            EventArgs = args;
        }

        public Action<NodeObserver> NodeObserverAction => observer =>
        {
            observer.OnNodeCopying(null, EventArgs);
        };
    }
}
