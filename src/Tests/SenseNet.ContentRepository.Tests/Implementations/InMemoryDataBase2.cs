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
        /// <summary>
        /// NodeId --> NodeDoc (NodeHead)
        /// </summary>
        public Dictionary<int, NodeDoc> Nodes { get; } = new Dictionary<int, NodeDoc>();
        private int __lastNodeId = 1247;
        public int GetNextNodeId()
        {
            return Interlocked.Increment(ref __lastNodeId);
        }

        /* ================================================================================================ Versions */
        /// <summary>
        /// VersionId --> VersionDoc (NodeData minus NodeHead)
        /// </summary>
        public Dictionary<int, VersionDoc> Versions { get; } = new Dictionary<int, VersionDoc>();
        private int __lastVersionId = 260;
        public int GetNextVersionId()
        {
            return Interlocked.Increment(ref __lastVersionId);
        }

        /* ================================================================================================ BinaryProperties */
        /// <summary>
        /// BinaryPropertyId --> BinaryPropertyDoc
        /// </summary>
        public Dictionary<int, BinaryPropertyDoc> BinaryProperties { get; } = new Dictionary<int, BinaryPropertyDoc>();
        private int __binaryPropertyId = 112;
        public int GetNextBinaryPropertyId()
        {
            return Interlocked.Increment(ref __binaryPropertyId);
        }

        /* ================================================================================================ Files */
        /// <summary>
        /// FileId --> FileDoc
        /// </summary>
        public Dictionary<int, FileDoc> Files { get; } = new Dictionary<int, FileDoc>();
        private int __fileId = 112;
        public int GetNextFileId()
        {
            return Interlocked.Increment(ref __fileId);
        }

        /* ================================================================================================ Files */
        /// <summary>
        /// TreeLockId --> TreeLockDoc
        /// </summary>
        public Dictionary<int, TreeLockDoc> TreeLocks { get; } = new Dictionary<int, TreeLockDoc>();
        private int __treeLockId = 0;
        public int GetNextTreeLockId()
        {
            return Interlocked.Increment(ref __treeLockId);
        }

        /* ================================================================================================ Files */
        /// <summary>
        /// LogId --> LogEntryDoc
        /// </summary>
        public Dictionary<int, LogEntryDoc> LogEntries { get; } = new Dictionary<int, LogEntryDoc>();
        private int __logId = 0;
        public int GetNextLogId()
        {
            return Interlocked.Increment(ref __logId);
        }

    }
}
