using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using SenseNet.ContentRepository.Storage.DataModel;

namespace SenseNet.Tests.Implementations2 //UNDONE:DB -------CLEANUP: move to SenseNet.Tests.Implementations
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class InMemoryDataBase2 //UNDONE:DB -------Rename to InMemoryDataBase
    {
        private readonly Dictionary<Type, object> _collections = new Dictionary<Type, object>
        {
            {typeof(NodeDoc), new DataCollection<NodeDoc>()},
            {typeof(VersionDoc), new DataCollection<VersionDoc>()},
            {typeof(BinaryPropertyDoc), new DataCollection<BinaryPropertyDoc>()},
            {typeof(FileDoc), new DataCollection<FileDoc>()},
            {typeof(TreeLockDoc), new DataCollection<TreeLockDoc>()},
            {typeof(LogEntryDoc), new DataCollection<LogEntryDoc>()},
            {typeof(IndexingActivityDoc), new DataCollection<IndexingActivityDoc>()},
            /* ====================================================================== EXTENSIONS */
            {typeof(SharedLockDoc), new DataCollection<SharedLockDoc>()},
            {typeof(AccessTokenDoc), new DataCollection<AccessTokenDoc>()},
        };
        public DataCollection<T> GetCollection<T>()
        {
            return (DataCollection<T>) _collections[typeof(T)];
        }

        /* ================================================================================================ SCHEMA */

        public string SchemaLock { get; set; }
        public RepositorySchemaData Schema { get; set; } = new RepositorySchemaData();

        /* ================================================================================================ Nodes */

        public List<NodeDoc> Nodes { get; } = new List<NodeDoc>();
        private int __lastNodeId = 1247;
        public int GetNextNodeId()
        {
            return Interlocked.Increment(ref __lastNodeId);
        }

        /* ================================================================================================ Versions */

        public List<VersionDoc> Versions { get; } = new List<VersionDoc>();
        private int __lastVersionId = 260;
        public int GetNextVersionId()
        {
            return Interlocked.Increment(ref __lastVersionId);
        }

        /* ================================================================================================ BinaryProperties */

        public List<BinaryPropertyDoc> BinaryProperties { get; } = new List<BinaryPropertyDoc>();
        private int __binaryPropertyId = 112;
        public int GetNextBinaryPropertyId()
        {
            return Interlocked.Increment(ref __binaryPropertyId);
        }

        /* ================================================================================================ Files */

        public List<FileDoc> Files { get; } = new List<FileDoc>();
        private int __fileId = 112;
        public int GetNextFileId()
        {
            return Interlocked.Increment(ref __fileId);
        }

        /* ================================================================================================ Files */

        public List<TreeLockDoc> TreeLocks { get; } = new List<TreeLockDoc>();
        private int __treeLockId = 0;
        public int GetNextTreeLockId()
        {
            return Interlocked.Increment(ref __treeLockId);
        }

        /* ================================================================================================ LogEntries */

        public List<LogEntryDoc> LogEntries { get; } = new List<LogEntryDoc>();
        private int __logId = 0;
        public int GetNextLogId()
        {
            return Interlocked.Increment(ref __logId);
        }

        /* ================================================================================================ IndexingActivities */

        public List<IndexingActivityDoc> IndexingActivities { get; } = new List<IndexingActivityDoc>();
        private int __indexingActivityId = 0;
        public int GetNextIndexingActivitiyId()
        {
            return Interlocked.Increment(ref __indexingActivityId);
        }

        /* ================================================================================================ IndexingActivities */
        //UNDONE:@@ SharedLockDoc is an extension
        public List<SharedLockDoc> SharedLocks { get; } = new List<SharedLockDoc>();
        private int __sharedLockId = 0;
        public int GetNextSharedLockId()
        {
            return Interlocked.Increment(ref __sharedLockId);
        }
    }

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
    }
}
