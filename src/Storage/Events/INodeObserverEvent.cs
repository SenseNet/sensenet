using System;
using SenseNet.ContentRepository.Storage.Events;

// ReSharper disable once CheckNamespace
namespace SenseNet.Events
{
    /// <summary>
    /// Declares API elements for an <see cref="ISnEvent"/> that helps to call an old school NodeObserver method.
    /// </summary>
    public interface INodeObserverEvent : ISnEvent
    {
        Action<NodeObserver> NodeObserverAction { get; }
    }
}
