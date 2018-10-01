using System;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    /// <summary>
    /// A generic event args type for cache invalidation events.
    /// </summary>
    public class EventArgs<T> : EventArgs
    {
        /// <summary>
        /// Changed data - for example a content id or path.
        /// </summary>
        public T Data { get; }
        /// <summary>
        /// Initializes a new instance of the <see cref="EventArgs{T}"/> class.
        /// </summary>
        /// <param name="data">Changed data - for example a content id or path.</param>
        public EventArgs(T data)
        {
            Data = data;
        }
    }
}
