using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    public class EventArgs<T> : EventArgs
    {
        public T Data { get; private set; }
        public EventArgs(T data)
        {
            Data = data;
        }
    }
}
