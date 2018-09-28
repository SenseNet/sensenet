using System;
using System.Linq;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    internal class EventServer<T>
    {
        private class EventWrapper
        {
            public event EventHandler<EventArgs<T>> TheEvent;
            public void Fire(object sender, T data)
            {
                TheEvent?.Invoke(sender, new EventArgs<T>(data));
            }
            public int GetCount()
            {
                return TheEvent?.GetInvocationList().Length ?? 0;
            }
        }

        private static int defaultClientCount = 50;
        private readonly EventWrapper[] _wrappers;

        public EventServer(int clientCount)
        {
            var count = clientCount < 1 ? defaultClientCount : clientCount;
            _wrappers = new EventWrapper[count];
            for (int i = 0; i < _wrappers.Length; i++)
                _wrappers[i] = new EventWrapper();
        }

        public event EventHandler<EventArgs<T>> TheEvent
        {
            add
            {
                var slot = value.Target.GetHashCode() % _wrappers.Length;
                var wrapper = _wrappers[slot];
                wrapper.TheEvent += value;
            }
            remove
            {
                var slot = value.Target.GetHashCode() % _wrappers.Length;
                var wrapper = _wrappers[slot];
                wrapper.TheEvent -= value;
            }
        }

        public void Fire(object sender, T data)
        {
            foreach (var wrapper in _wrappers)
                wrapper.Fire(sender, data);
        }

        public int[] GetCounts()
        {
            return _wrappers.Select(w => w.GetCount()).ToArray();
        }
    }
}
