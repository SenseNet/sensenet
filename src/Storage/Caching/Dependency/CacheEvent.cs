using System;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    public class CacheEvent<T>
    {
        private readonly EventServer<T> _eventServer;

        public CacheEvent(int partitionSize)
        {
            _eventServer = new EventServer<T>(partitionSize);
        }

        public void Subscribe(EventHandler<EventArgs<T>> eventHandler)
        {
            _eventServer.TheEvent += eventHandler;
        }
        public void Unsubscribe(EventHandler<EventArgs<T>> eventHandler)
        {
            _eventServer.TheEvent -= eventHandler;
        }
        public void Fire(object sender, T data)
        {
            _eventServer.Fire(sender, data);
        }
    }
}
