using System;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    /// <summary>
    /// Represents an event that will trigger cache invalidation by id, path or other dependencies.
    /// </summary>
    /// <typeparam name="T">Type of the data used for invalidation.</typeparam>
    public class CacheEvent<T>
    {
        private readonly EventServer<T> _eventServer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheEvent{T}"/> class.
        /// </summary>
        /// <param name="partitionSize">Size of a single event partition list.</param>
        public CacheEvent(int partitionSize)
        {
            _eventServer = new EventServer<T>(partitionSize);
        }

        /// <summary>
        /// Subscribes to this event.
        /// </summary>
        public void Subscribe(EventHandler<EventArgs<T>> eventHandler)
        {
            _eventServer.TheEvent += eventHandler;
        }
        /// <summary>
        /// Unsubscribes from this event.
        /// </summary>
        public void Unsubscribe(EventHandler<EventArgs<T>> eventHandler)
        {
            _eventServer.TheEvent -= eventHandler;
        }
        /// <summary>
        /// Notifies all subscribed event handlers about a change.
        /// </summary>
        public void Fire(object sender, T data)
        {
            _eventServer.Fire(sender, data);
        }

        /// <summary>
        /// Gets the count of event subscriptions.
        /// </summary>
        public int[] GetCounts()
        {
            return _eventServer.GetCounts();
        }
    }
}
