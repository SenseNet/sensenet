using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// ... Recommended minimal object structure: Nodes -> Versions --> BinaryProperties -> Files
    ///                                                         |-> LongTextProperties
    /// ... Additional structure: TreeLocks, LogEntries, IndexingActivities
    /// </summary>
    public abstract class DataProvider2
    {
        /// <summary>
        /// ... (MSSQL: unique index size is 900 byte)
        /// </summary>
        public virtual int PathMaxLength { get; } = 450;
        public virtual DateTime DateTimeMinValue { get; } = DateTime.MinValue;
        public virtual DateTime DateTimeMaxValue { get; } = DateTime.MaxValue;
        public virtual decimal DecimalMinValue { get; } = decimal.MinValue;
        public virtual decimal DecimalMaxValue { get; } = decimal.MinValue;

        public virtual void Reset()
        {
            // Do nothing if the provider is stateless.
        }

        /* =============================================================================================== Extensions */

        private readonly Dictionary<Type, IDataProviderExtension> _dataProvidersByType = new Dictionary<Type, IDataProviderExtension>();

        public virtual void SetExtension(Type providerType, IDataProviderExtension provider)
        {
            _dataProvidersByType[providerType] = provider;
        }

        internal T GetExtensionInstance<T>() where T : class, IDataProviderExtension
        {
            if (_dataProvidersByType.TryGetValue(typeof(T), out var provider))
                return provider as T;
            return null;
        }

        /* =============================================================================================== Nodes */

        // Original SqlProvider executes these:
        // - INodeWriter: void InsertNodeAndVersionRows(NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId);
        // - DataProvider: private static void SaveNodeProperties(NodeData nodeData, SavingAlgorithm savingAlgorithm, INodeWriter writer, bool isNewNode)
        /// <summary>
        /// Persists a brand new objects that contains all static and dynamic properties of the actual node.
        /// Write back the newly generated data to the given nodeHeadData and versionData parameters:
        ///     NodeId, NodeTimestamp, VersionId, VersionTimestamp, BinaryPropertyIds.
        /// Write back the modified data into the given "settings"
        ///     LastMajorVersionId, LastMinorVersionId.
        /// ... Need to be transactional
        /// ... Algorithm:
        ///  1 - Begin a new transaction
        ///  2 - Check the [nodeHeadData].Path uniqueness. If not, throw NodeAlreadyExistsException.
        ///  3 - Ensure the new unique NodeId and write back to the [nodeHeadData].NodeId.
        ///  4 - Ensure the new unique VersionId and write back to the [versionData].VersionId and dynamicData.VersionId.
        ///  5 - Store (insert) the [versionData] representation.
        ///  6 - Ensure that the timestamp of the stored version is incremented and write back this value to the [versionData].Timestamp.
        ///  7 - Store (insert) all representation of the dynamic property data including long texts, binary properties and files.
        ///      Use the new versionId in these items.
        ///  8 - Collect last versionIds (last major and last minor).
        ///  9 - Store (insert) the [nodeHeadData] reresentation. Use the last major and minor versionIds.
        /// 10 - Ensure that the timestamp of the stored nodeHead is incremented and write back this value to the [nodeHeadData].Timestamp.
        /// 11 - Commit the transaction. If there is any problem, rollback the transaction and throw/rethrow an exception.
        ///      In case of error the written back data (new ids and changed timestamps)
        ///      will be dropped so rollback these data is not necessary.
        /// </summary>
        /// <param name="nodeHeadData">Head data of the node. Contains identical information, place in the Big-tree and the most important
        /// not-versioned property values.</param>
        /// <param name="versionData">Head information of the current version.</param>
        /// <param name="dynamicData">Metadata and blob data of the current version. Separated to some sub collections:
        /// BinaryProperties: Contain blob information (stream and metadata)
        /// LongTextProperties: Contain long textual values that can be lazy loaded.
        /// DynamicProperties: All dynamic property values except the binaries and long texts.
        /// </param>
        /// <returns>An awaitable object.</returns>
        public abstract Task InsertNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData);

        // Original SqlProvider executes these:
        // - INodeWriter: UpdateNodeRow(nodeData);
        // - INodeWriter: UpdateVersionRow(nodeData, out lastMajorVersionId, out lastMinorVersionId);
        // - DataProvider: private static void SaveNodeProperties(NodeData nodeData, SavingAlgorithm savingAlgorithm, INodeWriter writer, bool isNewNode)
        // - DataProvider: protected internal abstract void DeleteVersion(int versionId, NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId);
        /// <summary>
        /// ... Need to be transactional
        /// ... Algorithm:
        ///  1 - Begin a new transaction
        ///  2 - Check the node existence by [nodeHeadData].NodeId. Throw an ____ exception if the node is deleted.
        ///  3 - Check the version existence by [versionData].VersionId. Throw an ____ exception if the version is deleted.
        ///  4 - Check the concurrent update. If the [nodeHeadData].Timestap and stored not timestamp are not equal, throw a NodeIsOutOfDateException
        ///  5 - Update the stored version head data implementation by the [versionData].VersionId with the [versionData].
        ///  6 - Ensure that the timestamp of the stored version is incremented and write back this value to the [versionData].Timestamp.
        ///  7 - Delete version representations by the given [versionIdsToDelete]
        ///  8 - Update all representation of the dynamic property data including long texts, binary properties and files.
        ///      Use the new versionId in these items.
        ///  9 - Collect last versionIds (last major and last minor).
        /// 10 - Update the [nodeHeadData] reresentation. Use the last major and minor versionIds.
        /// 11 - Ensure that the timestamp of the stored nodeHead is incremented and write back this value to the [nodeHeadData].Timestamp.
        /// 12 - Update paths in the subtree if the [originalPath] is not null. For example: if the [originalPath] is "/Root/Folder1",
        ///      1 - All path will be changed if it starts with "/Root/Folder1/" ([originalPath] + trailing slash, case insensitive).
        ///      2 - Replace the [original path] to the new path in the [nodeHeadData].Path.
        /// 13 - Commit the transaction. If there is any problem, rollback the transaction and throw/rethrow an exception.
        ///      In case of error the written back data (new ids and changed timestamps)
        ///      will be dropped so rollback these data is not necessary.
        /// </summary>
        /// <param name="nodeHeadData">Head data of the node. Contains identical information, place in the Big-tree and the most important
        /// not-versioned property values.</param>
        /// <param name="versionData">Head information of the current version.</param>
        /// <param name="dynamicData">Metadata and blob data of the current version. Separated to some sub collections:
        /// BinaryProperties: Contain blob information (stream and metadata)
        /// LongTextProperties: Contain long textual values that can be lazy loaded.
        /// DynamicProperties: All dynamic property values except the binaries and long texts.
        /// </param>
        /// <param name="versionIdsToDelete">Set of versionIds that defines the versions that need to be deleted. Can be empty but never null.</param>
        /// <param name="originalPath">Contains the node's original path if it is renamed. Null if the name was not changed.</param>
        /// <returns></returns>
        public abstract Task UpdateNodeAsync(
            NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData, IEnumerable<int> versionIdsToDelete,
            string originalPath = null);

        // Original SqlProvider executes these:
        // INodeWriter: UpdateNodeRow(nodeData);
        // INodeWriter: CopyAndUpdateVersion(nodeData, settings.CurrentVersionId, settings.ExpectedVersionId, out lastMajorVersionId, out lastMinorVersionId);
        // DataProvider: private static void SaveNodeProperties(NodeData nodeData, SavingAlgorithm savingAlgorithm, INodeWriter writer, bool isNewNode)
        // DataProvider: protected internal abstract void DeleteVersion(int versionId, NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId);
        /// <summary>
        /// ... Need to be transactional
        ///  1 - Begin a new transaction
        ///  2 - Check the node existence by [nodeHeadData].NodeId. Throw an ____ exception if the node is deleted.
        ///  3 - Check the version existence by [versionData].VersionId. Throw an ____ exception if the version is deleted.
        ///  4 - Check the concurrent update. If the [nodeHeadData].Timestap and stored not timestamp are not equal, throw a NodeIsOutOfDateException
        /// 
        /// 
        /// 
        /// 
        ///  5 - Update the stored version head data implementation by the [versionData].VersionId with the [versionData].
        ///  6 - Ensure that the timestamp of the stored version is incremented and write back this value to the [versionData].Timestamp.
        ///  7 - Delete version representations by the given [versionIdsToDelete]
        ///  8 - Update all representation of the dynamic property data including long texts, binary properties and files.
        ///      Use the new versionId in these items.
        ///  9 - Collect last versionIds (last major and last minor).
        /// 10 - Update the [nodeHeadData] reresentation. Use the last major and minor versionIds.
        /// 11 - Ensure that the timestamp of the stored nodeHead is incremented and write back this value to the [nodeHeadData].Timestamp.
        /// 12 - Update paths in the subtree if the [originalPath] is not null. For example: if the [originalPath] is "/Root/Folder1",
        ///      1 - All path will be changed if it starts with "/Root/Folder1/" ([originalPath] + trailing slash, case insensitive).
        ///      2 - Replace the [original path] to the new path in the [nodeHeadData].Path.
        /// 13 - Commit the transaction. If there is any problem, rollback the transaction and throw/rethrow an exception.
        ///      In case of error the written back data (new ids and changed timestamps)
        ///      will be dropped so rollback these data is not necessary.
        /// </summary>
        /// <param name="nodeHeadData">Head data of the node. Contains identical information, place in the Big-tree and the most important
        /// not-versioned property values.</param>
        /// <param name="versionData">Head information of the current version.</param>
        /// <param name="dynamicData">Metadata and blob data of the current version. Separated to some sub collections:
        /// BinaryProperties: Contain blob information (stream and metadata)
        /// LongTextProperties: Contain long textual values that can be lazy loaded.
        /// DynamicProperties: All dynamic property values except the binaries and long texts.
        /// </param>
        /// <param name="versionIdsToDelete">Set of versionIds that defines the versions that need to be deleted. Can be empty but never null.</param>
        /// <param name="currentVersionId">Id of the source version</param>
        /// <param name="expectedVersionId">Id of the target version. 0 means: need to create a new version.</param>
        /// <param name="originalPath">Contains the node's original path if it is renamed. Null if the name was not changed.</param>
        /// <returns></returns>
        public abstract Task CopyAndUpdateNodeAsync(
            NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData, IEnumerable<int> versionIdsToDelete,
            int currentVersionId, int expectedVersionId = 0,
            string originalPath = null);

        // Executes these:
        // INodeWriter: UpdateNodeRow(nodeData);
        public abstract Task UpdateNodeHeadAsync(NodeHeadData nodeHeadData, IEnumerable<int> versionIdsToDelete);

        /// <summary>
        /// Returns loaded NodeData by the given versionIds
        /// </summary>
        public abstract Task<IEnumerable<NodeData>> LoadNodesAsync(int[] versionIds);

        public abstract Task DeleteNodeAsync(NodeHeadData nodeHeadData);

        public abstract Task MoveNodeAsync(NodeHeadData sourceNodeHeadData, int targetNodeId, long targetTimestamp);

        public abstract Task<Dictionary<int, string>> LoadTextPropertyValuesAsync(int versionId, int[] notLoadedPropertyTypeIds);

        public abstract Task<BinaryDataValue> LoadBinaryPropertyValueAsync(int versionId, int propertyTypeId);

        public abstract Task<bool> NodeExistsAsync(string path);

        /* =============================================================================================== NodeHead */

        public abstract Task<NodeHead> LoadNodeHeadAsync(string path);
        public abstract Task<NodeHead> LoadNodeHeadAsync(int nodeId);
        public abstract Task<NodeHead> LoadNodeHeadByVersionIdAsync(int versionId);
        public abstract Task<IEnumerable<NodeHead>> LoadNodeHeadsAsync(IEnumerable<int> heads);
        public abstract Task<NodeHead.NodeVersion[]> GetNodeVersions(int nodeId);
        public abstract Task<IEnumerable<VersionNumber>> GetVersionNumbersAsync(int nodeId);
        public abstract Task<IEnumerable<VersionNumber>> GetVersionNumbersAsync(string path);

        /* =============================================================================================== NodeQuery */

        public abstract Task<int> InstanceCountAsync(int[] nodeTypeIds);
        public abstract Task<IEnumerable<int>> GetChildrenIdentfiersAsync(int parentId);
        public abstract Task<IEnumerable<int>> QueryNodesByTypeAndPathAndNameAsync(int[] nodeTypeIds, string[] pathStart, bool orderByPath, string name);
        public abstract Task<IEnumerable<int>> QueryNodesByTypeAndPathAndPropertyAsync(int[] nodeTypeIds, string pathStart, bool orderByPath, List<QueryPropertyData> properties);
        public abstract Task<IEnumerable<int>> QueryNodesByReferenceAndTypeAsync(string referenceName, int referredNodeId, int[] nodeTypeIds);

        /* =============================================================================================== Tree */

        public abstract Task<IEnumerable<NodeType>> LoadChildTypesToAllowAsync(int nodeId);
        public abstract Task<List<ContentListType>> GetContentListTypesInTreeAsync(string path);
        public abstract Task<IEnumerable<EntityTreeNodeData>> LoadEntityTreeAsync();

        /* =============================================================================================== TreeLock */

        public abstract Task<int> AcquireTreeLockAsync(string path);
        public abstract Task<bool> IsTreeLockedAsync(string path);
        public abstract Task ReleaseTreeLockAsync(int[] lockIds);
        public abstract Task<Dictionary<int, string>> LoadAllTreeLocksAsync();

        /* =============================================================================================== IndexDocument */

        public abstract Task SaveIndexDocumentAsync(NodeData nodeData, IndexDocument indexDoc);
        public abstract Task SaveIndexDocumentAsync(int versionId, IndexDocument indexDoc);

        public abstract Task<IEnumerable<IndexDocumentData>> LoadIndexDocumentsAsync(IEnumerable<int> versionIds);
        public abstract Task<IEnumerable<IndexDocumentData>> LoadIndexDocumentsAsync(string path, int[] excludedNodeTypes);

        public abstract Task<IEnumerable<int>> LoadNotIndexedNodeIdsAsync(int fromId, int toId);

        /* =============================================================================================== IndexingActivity */

        public abstract Task<int> GetLastIndexingActivityIdAsync();
        public abstract Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int fromId, int toId, int count, bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory);
        public abstract Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int[] gaps, bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory);
        public abstract Task<IIndexingActivity[]> LoadExecutableIndexingActivitiesAsync(IIndexingActivityFactory activityFactory, int maxCount, int runningTimeoutInSeconds);
        public abstract Task<ExecutableIndexingActivitiesResult> LoadExecutableIndexingActivitiesAsync(IIndexingActivityFactory activityFactory, int maxCount, int runningTimeoutInSeconds, int[] waitingActivityIds);
        public abstract Task RegisterIndexingActivityAsync(IIndexingActivity activity);
        public abstract Task UpdateIndexingActivityRunningStateAsync(int indexingActivityId, IndexingActivityRunningState runningState);
        public abstract Task RefreshIndexingActivityLockTimeAsync(int[] waitingIds);
        public abstract Task DeleteFinishedIndexingActivitiesAsync();
        public abstract Task DeleteAllIndexingActivitiesAsync();

        /* =============================================================================================== Schema */

        public abstract Task<RepositorySchemaData> LoadSchemaAsync();
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

        /* =============================================================================================== Logging */

        public abstract Task WriteAuditEventAsync(AuditEventInfo auditEvent);

        /* =============================================================================================== Provider Tools */

        public abstract DateTime RoundDateTime(DateTime d);
        public abstract bool IsCacheableText(string text);
        public abstract Task<string> GetNameOfLastNodeWithNameBaseAsync(int parentId, string namebase, string extension);
        public abstract Task<long> GetTreeSizeAsync(string path, bool includeChildren);
        public abstract Task<int> GetNodeCountAsync(string path);
        public abstract Task<int> GetVersionCountAsync(string path);

        /* =============================================================================================== Infrastructure */

        public abstract Task InstallInitialDataAsync(InitialData data);

        /* =============================================================================================== Tools */

        public abstract Task<long> GetNodeTimestampAsync(int nodeId);
        public abstract Task<long> GetVersionTimestampAsync(int versionId);
    }
}
