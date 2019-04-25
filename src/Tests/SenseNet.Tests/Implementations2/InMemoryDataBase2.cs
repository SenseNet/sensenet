using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using SenseNet.ContentRepository.Storage.DataModel;

namespace SenseNet.Tests.Implementations2 //UNDONE:DB -------CLEANUP: move to SenseNet.Tests.Implementations
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class InMemoryDataBase2 //UNDONE:DB -------Rename to InMemoryDataBase
    {
        /* ================================================================================================ WELL KNOWN COLLECTIONS */

        public DataCollection<NodeDoc> Nodes { get; } = new DataCollection<NodeDoc>(1247);
        public DataCollection<VersionDoc> Versions { get; } = new DataCollection<VersionDoc>(260);
        public DataCollection<BinaryPropertyDoc> BinaryProperties { get; } = new DataCollection<BinaryPropertyDoc>(112);
        public DataCollection<FileDoc> Files { get; } = new DataCollection<FileDoc>(112);
        public DataCollection<TreeLockDoc> TreeLocks { get; } = new DataCollection<TreeLockDoc>();
        public DataCollection<LogEntryDoc> LogEntries { get; } = new DataCollection<LogEntryDoc>();
        public DataCollection<IndexingActivityDoc> IndexingActivities { get; } = new DataCollection<IndexingActivityDoc>();

        /* ================================================================================================ ALL COLLECTIONS */

        private readonly Dictionary<Type, object> _collections;
        public DataCollection<T> GetCollection<T>()
        {
            return (DataCollection<T>)_collections[typeof(T)];
        }

        /* ================================================================================================ CONSTRUCTION */

        public InMemoryDataBase2()
        {
            _collections = new Dictionary<Type, object>
            {
                /* WELL KNOWN COLLECTIONS */
                {typeof(NodeDoc), Nodes},
                {typeof(VersionDoc), Versions},
                {typeof(BinaryPropertyDoc), BinaryProperties},
                {typeof(FileDoc), Files},
                {typeof(TreeLockDoc), TreeLocks},
                {typeof(LogEntryDoc), LogEntries},
                {typeof(IndexingActivityDoc), IndexingActivities},
                /* EXTENSIONS */
                {typeof(SharedLockDoc), new DataCollection<SharedLockDoc>()},
                {typeof(AccessTokenDoc), new DataCollection<AccessTokenDoc>()},
                {typeof(PackageDoc), new DataCollection<PackageDoc>()},
            };
        }

        /* ================================================================================================ SCHEMA */

        public string SchemaLock { get; set; }
        public RepositorySchemaData Schema { get; set; } = new RepositorySchemaData();

    }

}
