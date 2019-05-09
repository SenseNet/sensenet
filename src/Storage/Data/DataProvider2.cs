using System;
using System.Collections.Generic;
using System.Threading;
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

        /// <summary>
        /// Persists brand new objects that contain all static and dynamic properties of the node.
        /// Writes back the newly generated ids and timestamps to the provided [nodeHeadData], [versionData] 
        /// and [dynamicData] parameters:
        ///     NodeId, NodeTimestamp, VersionId, VersionTimestamp, BinaryPropertyIds, LastMajorVersionId, LastMinorVersionId.
        /// This method needs to be transactional. If an error occurs during execution, all data changes
        /// should be reverted to the original state by the data provider.
        /// Algorithm:
        ///  1 - Begin a new transaction
        ///  2 - Check the uniqueness of the [nodeHeadData].Path value. If that fails, throw a <see cref="NodeAlreadyExistsException"/>.
        ///  3 - Ensure a new unique NodeId and use it in the node head representation.
        ///  4 - Ensure a new unique VersionId and use it in the version head representation and any other version related data.
        ///  5 - Store (insert) the [versionData] representation.
        ///  6 - Ensure that the timestamp of the stored version is incremented.
        ///  7 - Store (insert) dynamic property data including long texts, binary properties and files.
        ///      Use the new versionId in these items. It is strongly recommended to manage BinaryProperties and files
        ///      using the BlobStorage API (e.g. BlobStorage.InsertBinaryProperty method).
        ///  8 - Collect last major and last minor versionIds.
        ///  9 - Store (insert) the [nodeHeadData] value. Use the new last major and minor versionIds.
        /// 10 - Ensure that the timestamp of the stored nodeHead is incremented.
        /// 11 - Write back the following changed values:
        ///      - new nodeId: [nodeHeadData].NodeId
        ///      - new versionId: [versionData].VersionId
        ///      - nodeHead timestamp: [nodeHeadData].Timestamp
        ///      - version timestamp: [versionData].Timestamp
        ///      - last major version id: [nodeHeadData].LastMajorVersionId
        ///      - last minor version id: [nodeHeadData].LastMinorVersionId
        ///      - If BinaryProperties or files are not managed using the BlobStorage API, update all changed 
        ///        ids and file ids of BinaryDataValue in the [dynamicData].BinaryProperties property.
        /// 12 - Commit the transaction. If there was a problem, rollback the transaction and throw an exception.
        ///      In case of error the data written into the parameters (new ids and changed timestamps)
        ///      will be dropped so rolling back these values is not necessary.
        /// </summary>
        /// <param name="nodeHeadData">Head data of the node. Contains identity information, place in the 
        /// content tree and the most important not-versioned property values.</param>
        /// <param name="versionData">Head information of the current version.</param>
        /// <param name="dynamicData">Metadata and blob data of the current version, categorized into collections:
        /// BinaryProperties: blob information (stream and metadata)
        /// LongTextProperties: long text values that can be lazy loaded.
        /// DynamicProperties: All dynamic property values except the binaries and long texts.
        /// </param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public abstract Task InsertNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Updates objects in the database that contain static and dynamic properties of the node.
        /// If the node is renamed (the Name property changed) updates the paths in the subtree.
        /// Writes back the newly generated ids and timestamps to the given [nodeHeadData], [versionData] 
        /// and [dynamicData] parameters:
        ///     NodeTimestamp, VersionTimestamp, BinaryPropertyIds, LastMajorVersionId, LastMinorVersionId.
        /// This method needs to be transactional. If an error occurs during execution, all data changes
        /// should be reverted to the original state by the data provider.
        /// Algorithm:
        ///  1 - Begin a new transaction
        ///  2 - Check if the node exists using the [nodeHeadData].NodeId value. Throw an <see cref="ContentNotFoundException"/> exception if the node is deleted.
        ///  3 - Check if the version exists using the [versionData].VersionId value. Throw an <see cref="ContentNotFoundException"/> exception if the version is deleted.
        ///  4 - Check concurrent updates: if the provided and stored [nodeHeadData].Timestap values are not equal, 
        ///      throw a <see cref="NodeIsOutOfDateException"/>.
        ///  5 - Update the stored version data by the [versionData].VersionId with the values in the [versionData] parameter.
        ///  6 - Ensure that the timestamp of the stored version is incremented.
        ///  7 - Delete unnecessary versions listed in the [versionIdsToDelete] parameter.
        ///  8 - Update all dynamic property data including long texts, binary properties and files.
        ///      Use the new versionId in these items. It is strongly recommended to manage BinaryProperties and files 
        ///      using the BlobStorage API (e.g. BlobStorage.UpdateBinaryProperty method).
        ///  9 - Collect last major and last minor versionIds.
        /// 10 - Update the [nodeHeadData] reresentation. Use the new last major and minor versionIds.
        /// 11 - Ensure that the timestamp of the stored nodeHead is incremented.
        /// 12 - Update paths in the subtree if the [originalPath] is not null. For example: if the [originalPath] 
        ///      is "/Root/Folder1", all paths starting with "/Root/Folder1/" ([originalPath] + trailing slash, 
        ///      case insensitive) will be changed: Replace [originalPath] with the new path in the 
        ///      [nodeHeadData].Path property.
        /// 13 - Write back the following changed values:
        ///      - new versionId: [versionData].VersionId
        ///      - nodeHead timestamp: [nodeHeadData].Timestamp
        ///      - version timestamp: [versionData].Timestamp
        ///      - last major version id: [nodeHeadData].LastMajorVersionId
        ///      - last minor version id: [nodeHeadData].LastMinorVersionId
        ///      - If BinaryProperties or files are not managed using the BlobStorage API, update all changed 
        ///        ids and file ids of BinaryDataValue in the [dynamicData].BinaryProperties property.
        /// 14 - Commit the transaction. If there was a problem, rollback the transaction and throw an exception.
        ///      In case of error the data written into the parameters (new ids and changed timestamps)
        ///      will be dropped so rolling back these values is not necessary.
        /// </summary>
        /// <param name="nodeHeadData">Head data of the node. Contains identity information, place in the 
        /// content tree and the most important not-versioned property values.</param>
        /// <param name="versionData">Head information of the current version.</param>
        /// <param name="dynamicData">Metadata and blob data of the current version, categorized into collections:
        /// BinaryProperties: blob information (stream and metadata)
        /// LongTextProperties: long textual values that can be lazy loaded.
        /// DynamicProperties: All dynamic property values except the binaries and long texts.
        /// </param>
        /// <param name="versionIdsToDelete">Defines the versions that need to be deleted. Can be empty but not null.</param>
        /// <param name="originalPath">Contains the node's original path if it is renamed. Null if the name was not changed.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public abstract Task UpdateNodeAsync(
            NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData, IEnumerable<int> versionIdsToDelete,
            string originalPath = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Copies all objects that contain static and dynamic properties of the node (except the nodeHead representation)
        /// and updates the copy with the provided data. Source version is identified by the [versionData].VersionId. Updates the paths
        /// in the subtree if the node is renamed (i.e. Name property changed). Target version descriptor is the [expectedVersionId]
        /// parameter.
        /// Writes back the newly generated data to the [nodeHeadData], [versionData] and [dynamicData] parameters:
        ///     NodeTimestamp, VersionId, VersionTimestamp, BinaryPropertyIds, LastMajorVersionId, LastMinorVersionId.
        /// This method needs to be transactional. If an error occurs during execution, all data changes
        /// should be reverted to the original state by the data provider.
        ///  1 - Begin a new transaction
        ///  2 - Check if the node exists using the [nodeHeadData].NodeId value. Throw an <see cref="ContentNotFoundException"/> exception if the node is deleted.
        ///  3 - Check if the version exists using the [versionData].VersionId value. Throw an <see cref="ContentNotFoundException"/> exception if the version is deleted.
        ///  4 - Check concurrent updates: if the provided and stored [nodeHeadData].Timestap values are not equal, 
        ///      throw a <see cref="NodeIsOutOfDateException"/>.
        ///  5 - Determine the target version: if [expectedVersionId] is not null, load the existing instance, 
        ///      otherwise create a new one.
        ///  6 - Copy the source version head data to the target representation and update it with the values in [versionData].
        ///  7 - Ensure that the timestamp of the stored version is incremented.
        ///  8 - Copy the dynamic data to the target and update it with the values in [dynamicData].DynamicProperties.
        ///  9 - Copy the longText data to the target and update it with the values in [dynamicData].LongTextProperties.
        /// 10 - Save binary properties to the target version (copying old values is unnecessary because all 
        ///      binary properties were loaded before save).
        ///      It is strongly recommended to manage BinaryProperties and files 
        ///      using the BlobStorage API (e.g. BlobStorage.InsertBinaryProperty method).
        /// 11 - Delete unnecessary versions listed in the [versionIdsToDelete] parameter.
        /// 12 - Collect last major and last minor versionIds.
        /// 13 - Update the [nodeHeadData] reresentation. Use the new last major and minor versionIds.
        /// 14 - Ensure that the timestamp of the stored nodeHead is incremented.
        /// 15 - Update paths in the subtree if the [originalPath] is not null. For example: if the [originalPath] 
        ///      is "/Root/Folder1", all paths starting with "/Root/Folder1/" ([originalPath] + trailing slash, 
        ///      case insensitive) will be changed: Replace [originalPath] with the new path in the 
        ///      [nodeHeadData].Path property.
        /// 16 - Write back the following changed values:
        ///      - new versionId: [versionData].VersionId
        ///      - nodeHead timestamp: [nodeHeadData].Timestamp
        ///      - version timestamp: [versionData].Timestamp
        ///      - last major version id: [nodeHeadData].LastMajorVersionId
        ///      - last minor version id: [nodeHeadData].LastMinorVersionId
        ///      - If BinaryProperties or files are not managed using the BlobStorage API, update all changed 
        ///        ids and file ids of BinaryDataValue in the [dynamicData].BinaryProperties property.
        /// 17 - Commit the transaction. If there was a problem, rollback the transaction and throw an exception.
        ///      In case of error the data written into the parameters (new ids and changed timestamps)
        ///      will be dropped so rolling back these values is not necessary.
        /// </summary>
        /// <param name="nodeHeadData">Head data of the node. Contains identity information, place in the 
        /// content tree and the most important not-versioned property values.</param>
        /// <param name="versionData">Head information of the current version.</param>
        /// <param name="dynamicData">Metadata and blob data of the current version, categorized into collections:
        /// BinaryProperties: blob information (stream and metadata)
        /// LongTextProperties: long textual values that can be lazy loaded.
        /// DynamicProperties: All dynamic property values except the binaries and long texts.
        /// </param>
        /// <param name="versionIdsToDelete">Defines the versions that need to be deleted. Can be empty but not null.</param>
        /// <param name="expectedVersionId">Id of the target version. 0 means: need to create a new version.</param>
        /// <param name="originalPath">Contains the node's original path if it is renamed. Null if the name was not changed.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public abstract Task CopyAndUpdateNodeAsync(
            NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData, IEnumerable<int> versionIdsToDelete,
            int expectedVersionId = 0, string originalPath = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Updates the paths in the subtree if the node is renamed (i.e. Name property changed).
        /// This method needs to be transactional. If an error occurs during execution, all data changes
        /// should be reverted to the original state by the data provider.
        /// Algorithm:
        ///  1 - Begin a new transaction
        ///  2 - Check if the node exists using the [nodeHeadData].NodeId value. Throw an <see cref="ContentNotFoundException"/> exception if the node is deleted.
        ///  3 - Check concurrent updates: if the provided and stored [nodeHeadData].Timestap values are not equal, 
        ///      throw a <see cref="NodeIsOutOfDateException"/>.
        ///  4 - Delete unnecessary versions listed in the [versionIdsToDelete] parameter.
        ///  5 - Collect last major and last minor versionIds.
        ///  6 - Update the [nodeHeadData] reresentation. Use the new last major and minor versionIds.
        ///  7 - Ensure that the timestamp of the stored nodeHead is incremented.
        ///  8 - Write back the following changed values:
        ///      - nodeHead timestamp: [nodeHeadData].Timestamp
        ///      - last major version id: [nodeHeadData].LastMajorVersionId
        ///      - last minor version id: [nodeHeadData].LastMinorVersionId
        ///  9 - Commit the transaction. If there was a problem, rollback the transaction and throw an exception.
        ///      In case of error the data written into the parameters (new ids and changed timestamps)
        ///      will be dropped so rolling back these values is not necessary.
        /// </summary>
        /// <param name="nodeHeadData">Head data of the node. Contains identity information, place in the 
        /// content tree and the most important not-versioned property values.</param>
        /// <param name="versionIdsToDelete">Defines the versions that need to be deleted. Can be empty but not null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public abstract Task UpdateNodeHeadAsync(NodeHeadData nodeHeadData, IEnumerable<int> versionIdsToDelete,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns loaded NodeData by the given versionIds
        /// </summary>
        public abstract Task<IEnumerable<NodeData>> LoadNodesAsync(int[] versionIds, CancellationToken cancellationToken = default(CancellationToken));

        public abstract Task DeleteNodeAsync(NodeHeadData nodeHeadData, CancellationToken cancellationToken = default(CancellationToken));

        public abstract Task MoveNodeAsync(NodeHeadData sourceNodeHeadData, int targetNodeId, long targetTimestamp,
            CancellationToken cancellationToken = default(CancellationToken));

        public abstract Task<Dictionary<int, string>> LoadTextPropertyValuesAsync(int versionId, int[] notLoadedPropertyTypeIds,
            CancellationToken cancellationToken = default(CancellationToken));

        public abstract Task<BinaryDataValue> LoadBinaryPropertyValueAsync(int versionId, int propertyTypeId,
            CancellationToken cancellationToken = default(CancellationToken));

        public abstract Task<bool> NodeExistsAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /* =============================================================================================== NodeHead */

        public abstract Task<NodeHead> LoadNodeHeadAsync(string path, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<NodeHead> LoadNodeHeadAsync(int nodeId, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<NodeHead> LoadNodeHeadByVersionIdAsync(int versionId, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<IEnumerable<NodeHead>> LoadNodeHeadsAsync(IEnumerable<int> heads, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<NodeHead.NodeVersion[]> GetNodeVersions(int nodeId, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<IEnumerable<VersionNumber>> GetVersionNumbersAsync(int nodeId, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<IEnumerable<VersionNumber>> GetVersionNumbersAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /* =============================================================================================== NodeQuery */

        public abstract Task<int> InstanceCountAsync(int[] nodeTypeIds, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<IEnumerable<int>> GetChildrenIdentfiersAsync(int parentId, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<IEnumerable<int>> QueryNodesByTypeAndPathAndNameAsync(int[] nodeTypeIds, string[] pathStart,
            bool orderByPath, string name, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<IEnumerable<int>> QueryNodesByTypeAndPathAndPropertyAsync(int[] nodeTypeIds, string pathStart,
            bool orderByPath, List<QueryPropertyData> properties, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<IEnumerable<int>> QueryNodesByReferenceAndTypeAsync(string referenceName, int referredNodeId,
            int[] nodeTypeIds, CancellationToken cancellationToken = default(CancellationToken));

        /* =============================================================================================== Tree */

        public abstract Task<IEnumerable<NodeType>> LoadChildTypesToAllowAsync(int nodeId, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<List<ContentListType>> GetContentListTypesInTreeAsync(string path, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<IEnumerable<EntityTreeNodeData>> LoadEntityTreeAsync(CancellationToken cancellationToken = default(CancellationToken));

        /* =============================================================================================== TreeLock */

        public abstract Task<int> AcquireTreeLockAsync(string path, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<bool> IsTreeLockedAsync(string path, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task ReleaseTreeLockAsync(int[] lockIds, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<Dictionary<int, string>> LoadAllTreeLocksAsync(CancellationToken cancellationToken = default(CancellationToken));

        /* =============================================================================================== IndexDocument */

        public abstract Task SaveIndexDocumentAsync(NodeData nodeData, IndexDocument indexDoc, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task SaveIndexDocumentAsync(int versionId, IndexDocument indexDoc, CancellationToken cancellationToken = default(CancellationToken));

        public abstract Task<IEnumerable<IndexDocumentData>> LoadIndexDocumentsAsync(IEnumerable<int> versionIds,
            CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<IEnumerable<IndexDocumentData>> LoadIndexDocumentsAsync(string path, int[] excludedNodeTypes,
            CancellationToken cancellationToken = default(CancellationToken));

        public abstract Task<IEnumerable<int>> LoadNotIndexedNodeIdsAsync(int fromId, int toId, CancellationToken cancellationToken = default(CancellationToken));

        /* =============================================================================================== IndexingActivity */

        public abstract Task<int> GetLastIndexingActivityIdAsync(CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int fromId, int toId, int count,
            bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int[] gaps, bool executingUnprocessedActivities,
            IIndexingActivityFactory activityFactory, CancellationToken cancellationToken = default(CancellationToken));
        //public abstract Task<IIndexingActivity[]> LoadExecutableIndexingActivitiesAsync(
        //    IIndexingActivityFactory activityFactory, int maxCount, int runningTimeoutInSeconds, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<ExecutableIndexingActivitiesResult> LoadExecutableIndexingActivitiesAsync(
            IIndexingActivityFactory activityFactory, int maxCount, int runningTimeoutInSeconds, int[] waitingActivityIds,
            CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task RegisterIndexingActivityAsync(IIndexingActivity activity, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task UpdateIndexingActivityRunningStateAsync(int indexingActivityId, IndexingActivityRunningState runningState,
            CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task RefreshIndexingActivityLockTimeAsync(int[] waitingIds, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task DeleteFinishedIndexingActivitiesAsync(CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task DeleteAllIndexingActivitiesAsync(CancellationToken cancellationToken = default(CancellationToken));

        /* =============================================================================================== Schema */

        public abstract Task<RepositorySchemaData> LoadSchemaAsync(CancellationToken cancellationToken = default(CancellationToken));
        public abstract SchemaWriter CreateSchemaWriter();

        /// <summary>
        /// Checks the given schemaTimestamp equality. If different, throws an error: Storage schema is out of date.
        /// Checks the schemaLock existence. If there is, throws an error
        /// otherwise create a SchemaLock and return its value.
        /// </summary>
        public abstract Task<string> StartSchemaUpdateAsync(long schemaTimestamp);
        /// <summary>
        /// Checks the given schemaLock equality. If different, throws an illegal operation error.
        /// Returns a newly generated schemaTimestamp.
        /// </summary>
        public abstract Task<long> FinishSchemaUpdateAsync(string schemaLock);

        /* =============================================================================================== Logging */

        public abstract Task WriteAuditEventAsync(AuditEventInfo auditEvent, CancellationToken cancellationToken = default(CancellationToken));

        /* =============================================================================================== Provider Tools */

        public abstract DateTime RoundDateTime(DateTime d);
        public abstract bool IsCacheableText(string text);
        public abstract Task<string> GetNameOfLastNodeWithNameBaseAsync(int parentId, string namebase, string extension,
            CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<long> GetTreeSizeAsync(string path, bool includeChildren, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<int> GetNodeCountAsync(string path, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<int> GetVersionCountAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /* =============================================================================================== Infrastructure */

        public abstract Task InstallInitialDataAsync(InitialData data, CancellationToken cancellationToken = default(CancellationToken));

        /* =============================================================================================== Tools */

        public abstract Task<long> GetNodeTimestampAsync(int nodeId, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<long> GetVersionTimestampAsync(int versionId, CancellationToken cancellationToken = default(CancellationToken));

        /* =============================================================================================== Test support */

        public abstract Task SetFileStagingAsync(int fileId, bool staging);
        public abstract Task DeleteFileAsync(int fileId);
    }
}
