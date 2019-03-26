using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{
    public class SaveResult //UNDONE:DB ?? Delete SaveResult or move to implementation ??
    {
        public int NodeId;
        public int VersionId;
        public long NodeTimestamp;
        public long VersionTimestamp;
        public string Path;

        public int LastMajorVersionId;
        public int LastMinorVersionId;

        public SaveResult()
        {
            NodeId = -1;
            VersionId = -1;
            NodeTimestamp = -1L;
            VersionTimestamp = -1L;
            Path = null;
            LastMajorVersionId = -1;
            LastMinorVersionId = -1;
        }

        public VersionNumber Version;
        public Dictionary<PropertyType, int> BinaryPropertyIds;
    }

    /// <summary>
    /// .... Expected minimal object structure: Nodes -> Versions -> BinaryProperties -> Files
    /// </summary>
    public abstract class DataProvider2
    {
        /* ============================================================================================================= Nodes */

        // Executes these:
        // INodeWriter: void InsertNodeAndVersionRows(NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId);
        // DataProvider: private static void SaveNodeProperties(NodeData nodeData, SavingAlgorithm savingAlgorithm, INodeWriter writer, bool isNewNode)
        /// <summary>
        /// Persists a brand new objects that contains all static and dynamic properties of the actual node.
        /// </summary>
        /// <param name="nodeData">NodeData that will be inserted to.</param>
        /// <returns>A SaveResult instance with the newly generated data:
        /// NodeId, NodeTimestamp, VersionId, VersionTimestamp, BinaryPropertyIds, LastMajorVersionId, LastMinorVersionId.</returns>
        public abstract Task<SaveResult> InsertNodeAsync(NodeData nodeData);
        // Executes these:
        // INodeWriter: UpdateNodeRow(nodeData);
        // INodeWriter: UpdateVersionRow(nodeData, out lastMajorVersionId, out lastMinorVersionId);
        // DataProvider: private static void SaveNodeProperties(NodeData nodeData, SavingAlgorithm savingAlgorithm, INodeWriter writer, bool isNewNode)
        // DataProvider: protected internal abstract void DeleteVersion(int versionId, NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId);
        public abstract Task<SaveResult> UpdateNodeAsync(NodeData nodeData, IEnumerable<int> versionIdsToDelete);
        // Executes these:
        // INodeWriter: UpdateNodeRow(nodeData);
        // INodeWriter: CopyAndUpdateVersion(nodeData, settings.CurrentVersionId, out lastMajorVersionId, out lastMinorVersionId);
        // DataProvider: private static void SaveNodeProperties(NodeData nodeData, SavingAlgorithm savingAlgorithm, INodeWriter writer, bool isNewNode)
        // DataProvider: protected internal abstract void DeleteVersion(int versionId, NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId);
        public abstract Task<SaveResult> CopyAndUpdateNodeAsync(NodeData nodeData, int currentVersionId, IEnumerable<int> versionIdsToDelete);
        // Executes these:
        // INodeWriter: UpdateNodeRow(nodeData);
        // INodeWriter: CopyAndUpdateVersion(nodeData, settings.CurrentVersionId, settings.ExpectedVersionId, out lastMajorVersionId, out lastMinorVersionId);
        // DataProvider: private static void SaveNodeProperties(NodeData nodeData, SavingAlgorithm savingAlgorithm, INodeWriter writer, bool isNewNode)
        // DataProvider: protected internal abstract void DeleteVersion(int versionId, NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId);
        public abstract Task<SaveResult> CopyAndUpdateNodeAsync(NodeData nodeData, int currentVersionId, int expectedVersionId, IEnumerable<int> versionIdsToDelete);
        // Executes these:
        // INodeWriter: UpdateNodeRow(nodeData);
        public abstract Task UpdateNodeHeadAsync(NodeData nodeData);
        // Executes these:
        // INodeWriter: UpdateSubTreePath(string oldPath, string newPath);
        public abstract Task UpdateSubTreePathAsync(string oldPath, string newPath);

        /// <summary>
        /// Returns loaded NodeData by the given versionIds
        /// </summary>
        public abstract Task<IEnumerable<NodeData>> LoadNodesAsync(int[] versionIds);

        public abstract Task DeleteNodeAsync(int nodeId, long timestamp);
        public abstract Task MoveNodeAsync(int sourceNodeId, int targetNodeId, long sourceTimestamp, long targetTimestamp);

        /* ============================================================================================================= NodeHead */

        public abstract Task<NodeHead> LoadNodeHeadAsync(string path);
        public abstract Task<NodeHead> LoadNodeHeadAsync(int nodeId);
        public abstract Task<NodeHead> LoadNodeHeadByVersionIdAsync(int versionId);
        public abstract Task<IEnumerable<NodeHead>> LoadNodeHeadsAsync(IEnumerable<int> heads);

        /* ============================================================================================================= Schema */

        public abstract Task<DataSet> LoadSchemaAsync();
        public abstract SchemaWriter CreateSchemaWriter();

        //UNDONE:DB ------Refactor: Move to SchemaWriter? Delete the freature and implement individually in the providers?
        /// <summary>
        /// Checks the given schemaTimestamp equality. If different, throws an error: Storage schema is out of date.
        /// Checks the schemaLock existence. If there is, throws an error
        /// otherwise create a SchemaLock and return its value.
        /// </summary>
        public abstract string StartSchemaUpdate_EXPERIMENTAL(long schemaTimestamp); // original: AssertSchemaTimestampAndWriteModificationDate(long timestamp);
        //UNDONE:DB ------Refactor: Move to SchemaWriter? Delete the freature and implement individually in the providers?
        /// <summary>
        /// Checks the given schemaLock equality. If different, throws an illegal operation error.
        /// Returns a newly generated schemaTimestamp.
        /// </summary>
        public abstract long FinishSchemaUpdate_EXPERIMENTAL(string schemaLock);

        /* ============================================================================================================= Tools */

        public abstract DateTime RoundDateTime(DateTime d);
        public DataProviderChecker Checker { get; } = new DataProviderChecker();

        //UNDONE:DB -------Delete GetNodeTimestamp feature
        public abstract long GetNodeTimestamp(int nodeId);
        //UNDONE:DB -------Delete GetVersionTimestamp feature
        public abstract long GetVersionTimestamp(int versionId);
    }
}
