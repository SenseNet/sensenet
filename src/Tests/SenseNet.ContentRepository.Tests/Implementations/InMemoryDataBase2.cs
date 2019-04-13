using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using SenseNet.ContentRepository.Storage.DataModel;

namespace SenseNet.ContentRepository.Tests.Implementations
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class InMemoryDataBase2 //UNDONE:DB -------Rename to InMemoryDataBase
    {
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

    }
}
