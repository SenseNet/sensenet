using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace SenseNet.ContentRepository.Volatile
{
    public partial class InMemoryDataBase
    {
        private InMemoryTransaction _transacton;

        public IDbTransaction BeginTransaction()
        {
            _transacton?.Rollback();
            _transacton = new InMemoryTransaction(this);
            return _transacton;
        }

        internal void AddInsert<T>(DataCollection<T> collection, T item) where T : IDataDocument
        {
            _transacton?.AddInsert(collection, item);
        }
        //internal void AddUpdate<T>(DataCollection<T> collection, T item) where T : IDataDocument, new()
        //{
        //    _transacton?.AddUpdate(collection, item);
        //}
        internal void AddDelete<T>(DataCollection<T> collection, T item) where T : IDataDocument
        {
            _transacton?.AddDelete(collection, item);
        }

        private class InMemoryTransaction : IDbTransaction
        {
            /* ================================================================================= Participants */

            private readonly List<IParticipant> _participants = new List<IParticipant>();

            internal void AddInsert<T>(DataCollection<T> collection, T item) where T : IDataDocument
            {
                _participants.Add(new Insert<T>(collection, item));
            }
            //internal void AddUpdate<T>(DataCollection<T> collection, T item) where T : IDataDocument, new()
            //{
            //    _participants.Add(new Update<T>(collection, item));
            //}
            internal void AddDelete<T>(DataCollection<T> collection, T item) where T : IDataDocument
            {
                _participants.Add(new Delete<T>(collection, item));
            }

            private interface IParticipant
            {
                object Item { get; }
                void Commit();
                void Rollback();
            }
            private abstract class Participant<T> : IParticipant where T : IDataDocument
            {
                protected DataCollection<T> Collection { get; }
                public T Item { get; }
                object IParticipant.Item => Item;
                protected Participant(DataCollection<T> collection, T item)
                {
                    Collection = collection;
                    Item = item;
                }

                public virtual void Commit() { }
                public virtual void Rollback() { }
            }
            [DebuggerDisplay("{ToString()}")]
            private class Insert<T> : Participant<T> where T : IDataDocument
            {
                public Insert(DataCollection<T> collection, T item) : base(collection, item) { }
                public override void Rollback()
                {
                    Collection.RemoveInternal(Item);
                }
                public override string ToString()
                {
                    return $"Insert<{typeof(T).Name}> {Item.Id}";
                }
            }
            //private class Update<T> : Participant<T> where T : IDataDocument
            //{
            //    public Update(DataCollection<T> collection, T item) : base(collection, item) { }
            //    public override void Commit()
            //    {
            //        var existing = Collection.FirstOrDefault(x => x.Id == Item.Id);
            //        if (existing != null)
            //        {
            //            Collection.Remove(existing);
            //            Collection.Insert(Item);
            //        }
            //    }
            //}
            [DebuggerDisplay("{ToString()}")]
            private class Delete<T> : Participant<T> where T : IDataDocument
            {
                public Delete(DataCollection<T> collection, T item) : base(collection, item) { }
                public override void Rollback()
                {
                    var existing = Collection.FirstOrDefault(x => x.Id == Item.Id);
                    if (existing == null)
                        Collection.InsertInternal(Item);
                }
                public override string ToString()
                {
                    return $"Delete<{typeof(T).Name}> {Item.Id}";
                }
            }

            /* ================================================================================= Construction */

            private readonly InMemoryDataBase _db;
            // ReSharper disable once FieldCanBeMadeReadOnly.Local
            private bool _lockWasTaken;

            public InMemoryTransaction(InMemoryDataBase db)
            {
                _db = db;
                Monitor.Enter(_db, ref _lockWasTaken);
            }

            /* ================================================================================= Transaction functinality */


            public IDbConnection Connection => null;
            public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

            public void Dispose()
            {
                _db._transacton = null;
                _participants.Clear();
                if (_lockWasTaken)
                    Monitor.Exit(_db);
            }

            public void Commit()
            {
                foreach (var participant in _participants)
                    participant.Commit();
            }

            public void Rollback()
            {
                for (int i = _participants.Count - 1; i >= 0; i--)
                    _participants[i].Rollback();
            }
        }
    }
}
