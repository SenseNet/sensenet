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
    /// .... Expected minimal object structure: Nodes -> Versions -> BinaryProperties -> Files
    /// </summary>
    public abstract class DataProvider2
    {
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

        // Executes these:
        // INodeWriter: void InsertNodeAndVersionRows(NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId);
        // DataProvider: private static void SaveNodeProperties(NodeData nodeData, SavingAlgorithm savingAlgorithm, INodeWriter writer, bool isNewNode)
        // SUMMARY
        // Persists a brand new objects that contains all static and dynamic properties of the actual node.
        // Write back the newly generated data to the given "nodeData":
        //     NodeId, NodeTimestamp, VersionId, VersionTimestamp, BinaryPropertyIds.
        // Write back the modified data into the given "settings"
        //     LastMajorVersionId, LastMinorVersionId.
        public abstract Task InsertNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData);
        // Executes these:
        // INodeWriter: UpdateNodeRow(nodeData);
        // INodeWriter: UpdateVersionRow(nodeData, out lastMajorVersionId, out lastMinorVersionId);
        // DataProvider: private static void SaveNodeProperties(NodeData nodeData, SavingAlgorithm savingAlgorithm, INodeWriter writer, bool isNewNode)
        // DataProvider: protected internal abstract void DeleteVersion(int versionId, NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId);
        public abstract Task UpdateNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData, IEnumerable<int> versionIdsToDelete);
        // Executes these:
        // INodeWriter: UpdateNodeRow(nodeData);
        // INodeWriter: CopyAndUpdateVersion(nodeData, settings.CurrentVersionId, settings.ExpectedVersionId, out lastMajorVersionId, out lastMinorVersionId);
        // DataProvider: private static void SaveNodeProperties(NodeData nodeData, SavingAlgorithm savingAlgorithm, INodeWriter writer, bool isNewNode)
        // DataProvider: protected internal abstract void DeleteVersion(int versionId, NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId);
        public abstract Task CopyAndUpdateNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData, IEnumerable<int> versionIdsToDelete, int currentVersionId, int expectedVersionId = 0);
        // Executes these:
        // INodeWriter: UpdateNodeRow(nodeData);
        public abstract Task UpdateNodeHeadAsync(NodeHeadData nodeHeadData, IEnumerable<int> versionIdsToDelete);
        // Executes these:
        // INodeWriter: UpdateSubTreePath(string oldPath, string newPath);
        public abstract Task UpdateSubTreePathAsync(string oldPath, string newPath);

        /// <summary>
        /// Returns loaded NodeData by the given versionIds
        /// </summary>
        public abstract Task<IEnumerable<NodeData>> LoadNodesAsync(int[] versionIds);

        public abstract Task DeleteNodeAsync(int nodeId, long timestamp);
        public abstract Task MoveNodeAsync(int sourceNodeId, int targetNodeId, long sourceTimestamp, long targetTimestamp);

        public abstract Task<Dictionary<int, string>> LoadTextPropertyValuesAsync(int versionId, int[] notLoadedPropertyTypeIds);
        public abstract Task<BinaryDataValue> LoadBinaryPropertyValueAsync(int versionId, int propertyTypeId);

        public abstract Task<bool> NodeExistsAsync(string path);

        /* =============================================================================================== NodeHead */

        public abstract Task<NodeHead> LoadNodeHeadAsync(string path);
        public abstract Task<NodeHead> LoadNodeHeadAsync(int nodeId);
        public abstract Task<NodeHead> LoadNodeHeadByVersionIdAsync(int versionId);
        public abstract Task<IEnumerable<NodeHead>> LoadNodeHeadsAsync(IEnumerable<int> heads);
        public abstract Task<NodeHead.NodeVersion[]> GetNodeVersions(int nodeId);
        public abstract IEnumerable<VersionNumber> GetVersionNumbers(int nodeId);
        public abstract IEnumerable<VersionNumber> GetVersionNumbers(string path);

        /* =============================================================================================== NodeQuery */

        public abstract int InstanceCount(int[] nodeTypeIds);
        public abstract IEnumerable<int> GetChildrenIdentfiers(int parentId);
        public abstract IEnumerable<int> QueryNodesByPath(string pathStart, bool orderByPath);
        public abstract IEnumerable<int> QueryNodesByType(int[] nodeTypeIds);
        public abstract IEnumerable<int> QueryNodesByTypeAndPath(int[] nodeTypeIds, string pathStart, bool orderByPath);
        public abstract IEnumerable<int> QueryNodesByTypeAndPath(int[] nodeTypeIds, string[] pathStart, bool orderByPath);
        public abstract IEnumerable<int> QueryNodesByTypeAndPathAndName(int[] nodeTypeIds, string pathStart, bool orderByPath, string name);
        public abstract IEnumerable<int> QueryNodesByTypeAndPathAndName(int[] nodeTypeIds, string[] pathStart, bool orderByPath, string name);
        public abstract IEnumerable<int> QueryNodesByTypeAndPathAndProperty(int[] nodeTypeIds, string pathStart, bool orderByPath, List<QueryPropertyData> properties);
        public abstract IEnumerable<int> QueryNodesByReferenceAndType(string referenceName, int referredNodeId, int[] nodeTypeIds);

        /* =============================================================================================== Tree */

        public abstract Task<IEnumerable<NodeType>> LoadChildTypesToAllowAsync(int nodeId);
        public abstract List<ContentListType> GetContentListTypesInTree(string path);

        /* =============================================================================================== TreeLock */

        public abstract int AcquireTreeLock(string path);
        public abstract bool IsTreeLocked(string path);
        public abstract void ReleaseTreeLock(int[] lockIds);
        public abstract Dictionary<int, string> LoadAllTreeLocks();

        /* =============================================================================================== IndexDocument */

        public abstract Task SaveIndexDocumentAsync(NodeData nodeData, IndexDocument indexDoc);

        public abstract IEnumerable<IndexDocumentData> LoadIndexDocuments(IEnumerable<int> versionIds);
        public abstract IEnumerable<IndexDocumentData> LoadIndexDocuments(string path, int[] excludedNodeTypes);

        /* =============================================================================================== IndexingActivity */

        public abstract IIndexingActivity[] LoadIndexingActivities(int fromId, int toId, int count, bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory);
        public abstract IIndexingActivity[] LoadIndexingActivities(int[] gaps, bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory);
        public abstract void RegisterIndexingActivity(IIndexingActivity activity);
        public abstract IIndexingActivity[] LoadExecutableIndexingActivities(IIndexingActivityFactory activityFactory, int maxCount, int runningTimeoutInSeconds);
        public abstract IIndexingActivity[] LoadExecutableIndexingActivities(IIndexingActivityFactory activityFactory, int maxCount, int runningTimeoutInSeconds, int[] waitingActivityIds, out int[] finishedActivitiyIds);
        public abstract void UpdateIndexingActivityRunningState(int indexingActivityId, IndexingActivityRunningState runningState);
        public abstract void RefreshIndexingActivityLockTime(int[] waitingIds);
        public abstract int GetLastIndexingActivityId();
        public abstract void DeleteFinishedIndexingActivities();
        public abstract void DeleteAllIndexingActivities();

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

        public abstract void WriteAuditEvent(AuditEventInfo auditEvent);

        /* =============================================================================================== Provider Tools */

        public abstract DateTime RoundDateTime(DateTime d);
        public abstract bool IsCacheableText(string text);
        public abstract string GetNameOfLastNodeWithNameBase(int parentId, string namebase, string extension);
        public abstract long GetTreeSize(string path, bool includeChildren);

        /* =============================================================================================== Infrastructure */

        public abstract void InstallInitialData(InitialData data);

        /* =============================================================================================== Tools */

        //UNDONE:DB -------Delete GetNodeTimestamp method
        public abstract long GetNodeTimestamp(int nodeId);
        //UNDONE:DB -------Delete GetVersionTimestamp method
        public abstract long GetVersionTimestamp(int versionId);

    }
}
