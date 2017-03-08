using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    internal class EventServer<T>
    {
        private class EventWrapper
        {
            public event EventHandler<EventArgs<T>> TheEvent;
            public void Fire(object sender, T data)
            {
                var theEvent = TheEvent;
                if (theEvent != null)
                    theEvent(sender, new EventArgs<T>(data));
            }
        }

        private static int defaultClientCount = 50;
        private EventWrapper[] wrappers;

        public EventServer(int clientCount)
        {
            wrappers = new EventWrapper[Math.Max(clientCount, defaultClientCount)];
            for (int i = 0; i < wrappers.Length; i++)
                wrappers[i] = new EventWrapper();
        }

        public event EventHandler<EventArgs<T>> TheEvent
        {
            add
            {
                var slot = value.Target.GetHashCode() % wrappers.Length;
                var wrapper = wrappers[slot];
                wrapper.TheEvent += value;
            }
            remove
            {
                var slot = value.Target.GetHashCode() % wrappers.Length;
                var wrapper = wrappers[slot];
                wrapper.TheEvent -= value;
            }
        }

        public void Fire(object sender, T data)
        {
            foreach (var wrapper in wrappers)
                wrapper.Fire(sender, data);
        }
    }
}
