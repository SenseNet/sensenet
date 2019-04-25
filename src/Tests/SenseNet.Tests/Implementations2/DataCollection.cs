using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace SenseNet.Tests.Implementations2
{
    public class DataCollection<T> : IEnumerable<T>
    {
        private readonly List<T> _list = new List<T>();
        public int Count => _list.Count;

        public DataCollection(int lastId = 0)
        {
            _lastId = lastId;
        }

        private int _lastId;
        public int GetNextId()
        {
            return Interlocked.Increment(ref _lastId);
        }

        public void Add(T item)
        {
            _list.Add(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        public void Clear()
        {
            _list.Clear();
        }

        public void Remove(T item)
        {
            _list.Remove(item);
        }

        public void RemoveAll(Func<T, bool> func)
        {
            _list.RemoveAll(new Predicate<T>(func));
        }
    }
}
