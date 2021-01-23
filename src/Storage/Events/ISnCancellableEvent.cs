using SenseNet.ContentRepository.Storage.Events;

// ReSharper disable once CheckNamespace
namespace SenseNet.Events
{
    /// <summary>
    /// Defines a cancellable <see cref="INodeObserverEvent"/>. These events are always internals.
    /// </summary>
    public interface ISnCancellableEvent : INodeObserverEvent
    {
        CancellableNodeEventArgs CancellableEventArgs { get; }
    }

    /// <summary>
    /// Defines a cancellable <see cref="INodeObserverEvent"/> and the specialized set of data.
    /// These events are always internals.
    /// </summary>
    /// <typeparam name="T">The type of the set of data class (<see cref="CancellableNodeEventArgs"/> or
    /// any inherited class).</typeparam>
    public interface ISnCancellableEvent<out T> : ISnCancellableEvent, ISnEvent<T> where T : CancellableNodeEventArgs
    {

    }
}
