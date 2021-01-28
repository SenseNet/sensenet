using System;
using SenseNet.ContentRepository.Storage.Events;

// ReSharper disable once CheckNamespace
namespace SenseNet.Events
{
    internal class NodeForcedDeletingEvent : ISnCancellableEvent<CancellableNodeEventArgs>
    {
        INodeEventArgs ISnEvent.NodeEventArgs => EventArgs;
        CancellableNodeEventArgs ISnCancellableEvent.CancellableEventArgs => EventArgs;
        public CancellableNodeEventArgs EventArgs { get; }

        public NodeForcedDeletingEvent(CancellableNodeEventArgs args)
        {
            EventArgs = args;
        }

        public Action<NodeObserver> NodeObserverAction => observer =>
        {
            observer.OnNodeDeletingPhysically(null, EventArgs);
        };
    }
}
