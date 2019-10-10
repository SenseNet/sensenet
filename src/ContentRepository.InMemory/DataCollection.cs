using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SenseNet.ContentRepository.InMemory
{
    public class DataCollection<T> : IEnumerable<T> where T : IDataDocument
    {
        private readonly InMemoryDataBase _db;
        private readonly List<T> _list = new List<T>();
        public int Count => _list.Count;

        public DataCollection(InMemoryDataBase db, int lastId = 0)
        {
            _db = db;
            _lastId = lastId;
        }

        private int _lastId;
        public int GetNextId()
        {
            return Interlocked.Increment(ref _lastId);
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


        public void Insert(T item)
        {
            _list.Add(item);
            _db.AddInsert(this, item);
        }
        internal void InsertInternal(T item)
        {
            _list.Add(item);
        }
        public void Remove(int id)
        {
            Remove(_list.FirstOrDefault(x => x.Id == id));
        }
        public void Remove(T item)
        {
            if (item == null)
                return;
            _list.Remove(item);
            _db.AddDelete(this, item);
        }
        public void RemoveInternal(T item)
        {
            _list.Remove(item);
        }
    }
}
