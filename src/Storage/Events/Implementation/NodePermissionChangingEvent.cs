using System;
using SenseNet.ContentRepository.Storage.Events;

// ReSharper disable once CheckNamespace
namespace SenseNet.Events
{
    internal class NodePermissionChangingEvent : ISnCancellableEvent<CancellablePermissionChangingEventArgs>
    {
        INodeEventArgs ISnEvent.NodeEventArgs => EventArgs;
        CancellableNodeEventArgs ISnCancellableEvent.CancellableEventArgs => EventArgs;
        public CancellablePermissionChangingEventArgs EventArgs { get; }

        public NodePermissionChangingEvent(CancellablePermissionChangingEventArgs args)
        {
            EventArgs = args;
        }

        public Action<NodeObserver> NodeObserverAction => observer =>
        {
            observer.OnPermissionChanging(null, EventArgs);
        };
    }
}
