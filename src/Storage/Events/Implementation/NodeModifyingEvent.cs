using System;
using SenseNet.ContentRepository.Storage.Events;

// ReSharper disable once CheckNamespace
namespace SenseNet.Events
{
    public class NodeModifyingEvent : ISnCancellableEvent<CancellableNodeEventArgs>
    {
        public NodeModifyingEvent(CancellableNodeEventArgs args)
        {
            EventArgs = args;
        }

        INodeEventArgs ISnEvent.NodeEventArgs => EventArgs;
        CancellableNodeEventArgs ISnCancellableEvent.CancellableEventArgs => EventArgs;
        public CancellableNodeEventArgs EventArgs { get; }

        public Action<NodeObserver> NodeObserverAction => observer =>
        {
            observer.OnNodeModifying(null, (CancellableNodeEventArgs)EventArgs);
        };
    }
}
