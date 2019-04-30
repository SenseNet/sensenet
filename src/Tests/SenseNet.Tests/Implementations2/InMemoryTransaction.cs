using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Tests.Implementations2
{
    public class InMemoryTransaction : IDbTransaction
    {
        /* ================================================================================= Participants */

        public void AddInsert<T>(DataCollection<T> collection, T item) where T : IDataDocument
        {
            _participants.Add(new Insert<T>(collection, item));
        }
        public void AddUpdate<T>(DataCollection<T> collection, T item) where T : IDataDocument
        {
            _participants.Add(new Update<T>(collection, item));
        }

        private class Insert<T> : ITransactionParticipant where T : IDataDocument
        {
            private readonly DataCollection<T> _collection;
            private readonly T _item;

            public Insert(DataCollection<T> collection, T item)
            {
                _collection = collection;
                _item = item;
            }

            public void Commit()
            {
                _collection.Add(_item);
            }

            public void Rollback()
            {
                // do nothing
            }

        }
        private class Update<T> : ITransactionParticipant where T : IDataDocument
        {
            private readonly DataCollection<T> _collection;
            private readonly T _item;

            public Update(DataCollection<T> collection, T item)
            {
                _collection = collection;
                _item = item;
            }

            public void Commit()
            {
                var existing = _collection.FirstOrDefault(x => x.Id == _item.Id);
                if (existing != null)
                {
                    _collection.Remove(existing);
                    _collection.Add(_item);
                }
            }

            public void Rollback()
            {
                // do nothing
            }
        }

        /* ================================================================================= Construction */

        private readonly InMemoryDataBase2 _db;
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private bool _lockWasTaken;

        public InMemoryTransaction(InMemoryDataBase2 db)
        {
            _db = db;
            Monitor.Enter(_db, ref _lockWasTaken);
        }

        /* ================================================================================= Transaction functinality */

        private readonly List<ITransactionParticipant> _participants = new List<ITransactionParticipant>();

        public IDbConnection Connection => null;
        public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

        public void Dispose()
        {
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

        public IEnumerable<VersionDoc> GetVersionsByNodeId(int nodeId)
        {
            throw new NotImplementedException();
        }
    }
}
