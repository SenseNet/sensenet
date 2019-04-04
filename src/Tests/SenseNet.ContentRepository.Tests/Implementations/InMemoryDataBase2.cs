using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;

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
    }
}
