using System;
using SenseNet.ContentRepository.Storage.Events;

// ReSharper disable once CheckNamespace
namespace SenseNet.Events
{
    public class NodeDeletingEvent : ISnCancellableEvent<CancellableNodeEventArgs>
    {
        INodeEventArgs ISnEvent.NodeEventArgs => EventArgs;
        CancellableNodeEventArgs ISnCancellableEvent.CancellableEventArgs => EventArgs;
        public CancellableNodeEventArgs EventArgs { get; }

        public NodeDeletingEvent(CancellableNodeEventArgs args)
        {
            EventArgs = args;
        }

        public Action<NodeObserver> NodeObserverAction => observer =>
        {
            observer.OnNodeDeleting(null, EventArgs);
        };
    }
}
