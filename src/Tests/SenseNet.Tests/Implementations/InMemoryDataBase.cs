using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SenseNet.ContentRepository.Storage.DataModel;

namespace SenseNet.Tests.Implementations
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public partial class InMemoryDataBase
    {
        /* ================================================================================================ WELL KNOWN COLLECTIONS */

        public DataCollection<NodeDoc> Nodes { get; }
        public DataCollection<VersionDoc> Versions { get; }
        public DataCollection<LongTextPropertyDoc> LongTextProperties { get; }
        public DataCollection<BinaryPropertyDoc> BinaryProperties { get; }
        public DataCollection<FileDoc> Files { get; }
        public DataCollection<TreeLockDoc> TreeLocks { get; }
        public DataCollection<LogEntryDoc> LogEntries { get; }
        public DataCollection<IndexingActivityDoc> IndexingActivities { get; }

        /* ================================================================================================ ALL COLLECTIONS */

        private readonly Dictionary<Type, object> _collections;
        public DataCollection<T> GetCollection<T>() where T : IDataDocument
        {
            return (DataCollection<T>)_collections[typeof(T)];
        }

        /* ================================================================================================ CONSTRUCTION */

        public InMemoryDataBase()
        {
            Nodes = new DataCollection<NodeDoc>(this, 1247);
            Versions = new DataCollection<VersionDoc>(this, 260);
            LongTextProperties = new DataCollection<LongTextPropertyDoc>(this);
            BinaryProperties = new DataCollection<BinaryPropertyDoc>(this, 112);
            Files = new DataCollection<FileDoc>(this, 112);
            TreeLocks = new DataCollection<TreeLockDoc>(this);
            LogEntries = new DataCollection<LogEntryDoc>(this);
            IndexingActivities = new DataCollection<IndexingActivityDoc>(this);

            _collections = new Dictionary<Type, object>
            {
                /* WELL KNOWN COLLECTIONS */
                {typeof(NodeDoc), Nodes},
                {typeof(VersionDoc), Versions},
                {typeof(LongTextPropertyDoc), LongTextProperties},
                {typeof(BinaryPropertyDoc), BinaryProperties},
                {typeof(FileDoc), Files},
                {typeof(TreeLockDoc), TreeLocks},
                {typeof(LogEntryDoc), LogEntries},
                {typeof(IndexingActivityDoc), IndexingActivities},
                /* EXTENSIONS */
                {typeof(SharedLockDoc), new DataCollection<SharedLockDoc>(this)},
                {typeof(AccessTokenDoc), new DataCollection<AccessTokenDoc>(this)},
                {typeof(PackageDoc), new DataCollection<PackageDoc>(this)},
            };
        }

        /* ================================================================================================ SCHEMA */

        public string SchemaLock { get; set; }
        public RepositorySchemaData Schema { get; set; } = new RepositorySchemaData();

    }

}
