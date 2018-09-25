using System;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    public class EventArgs<T> : EventArgs
    {
        public T Data { get; }
        public EventArgs(T data)
        {
            Data = data;
        }
    }
}
