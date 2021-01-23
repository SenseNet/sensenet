using System;
using SenseNet.ContentRepository.Storage.Events;

// ReSharper disable once CheckNamespace
namespace SenseNet.Events
{
    public class NodeCreatingEvent : ISnCancellableEvent<CancellableNodeEventArgs>
    {
        INodeEventArgs ISnEvent.NodeEventArgs => EventArgs;
        CancellableNodeEventArgs ISnCancellableEvent.CancellableEventArgs => EventArgs;
        public CancellableNodeEventArgs EventArgs { get; }

        public NodeCreatingEvent(CancellableNodeEventArgs args)
        {
            EventArgs = args;
        }

        public Action<NodeObserver> NodeObserverAction => observer =>
        {
            observer.OnNodeCreating(null, EventArgs);
        };
    }
}
