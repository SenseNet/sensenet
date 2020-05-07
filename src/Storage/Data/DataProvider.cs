using System;
using System.Collections.Generic;
using System.Data;
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
    /// Base class for database-related operations in sensenet. Derived classes contain
    /// the platform-specific implementations of data operations, for example saving or
    /// loading nodes.
    /// </summary>
    /// <remarks>
    /// Recommended minimal object structure: Nodes -> Versions --> BinaryProperties -> Files
    ///                                                         |-> LongTextProperties
    ///                                                         |-> ReferenceProperties
    /// Additional structure: TreeLocks, LogEntries, IndexingActivities
    /// </remarks>
    public abstract class DataProvider
    {
        /// <summary>
        /// Gets the allowed length of the Path of a <see cref="Node"/>.
        /// In case of MSSQL the unique index size is 900 bytes, this is why the default value 
        /// of this property is 450 unicode characters.
        /// </summary>
        public virtual int PathMaxLength { get; } = 450;
        /// <summary>
        /// Gets the allowed minimum of a <see cref="DateTime"/> value.
        /// </summary>
        public virtual DateTime DateTimeMinValue { get; } = DateTime.MinValue;
        /// <summary>
        /// Gets the allowed maximum of a <see cref="DateTime"/> value.
        /// </summary>
        public virtual DateTime DateTimeMaxValue { get; } = DateTime.MaxValue;
        /// <summary>
        /// Gets the allowed minimum of a <see cref="decimal"/> value.
        /// </summary>
        public virtual decimal DecimalMinValue { get; } = decimal.MinValue;
        /// <summary>
        /// Gets the allowed maximum of a <see cref="decimal"/> value.
        /// </summary>
        public virtual decimal DecimalMaxValue { get; } = decimal.MinValue;

        /// <summary>
        /// Restores the dataprovider to the initial state after system start.
        /// </summary>
        public virtual void Reset()
        {
            // Do nothing if the provider is stateless.
        }

        /* =============================================================================================== Extensions */

        private readonly Dictionary<Type, IDataProviderExtension> _dataProvidersByType = new Dictionary<Type, IDataProviderExtension>();

        protected internal virtual void SetExtension(Type providerType, IDataProviderExtension provider)
        {
            _dataProvidersByType[providerType] = provider;
        }

        protected internal virtual T GetExtension<T>() where T : class, IDataProviderExtension
        {
            if (_dataProvidersByType.TryGetValue(typeof(T), out var provider))
                return provider as T;
            return null;
        }

        /* =============================================================================================== Nodes */

        /// <summary>
        /// Persists brand new objects that contain all static and dynamic properties of the node.
        /// Writes back the newly generated ids and timestamps to the provided [nodeHeadData], [versionData] 
        /// and [dynamicData] parameters: NodeId, NodeTimestamp, VersionId, VersionTimestamp, BinaryPropertyIds,
        /// LastMajorVersionId, LastMinorVersionId. This method needs to be transactional. If an error occurs during execution,
        /// all data changes should be reverted to the original state by the data provider.
        /// </summary>
        /// <remarks>
        /// Algorithm:
        ///  1 - Begin a new transaction
        ///  2 - Check the uniqueness of the [nodeHeadData].Path value. If that fails, throw a <see cref="NodeAlreadyExistsException"/>.
        ///  3 - Create a new unique NodeId and set it in the node head instance.
        ///  4 - Create a new unique VersionId and set it in the version head instance and any other version related data.
        ///  5 - Store the [versionData] instance containing the new NodeId.
        ///  6 - Ensure that the timestamp of the stored version is incremented.
        ///  7 - Store dynamic property data including long texts, binary properties and files.
        ///      Use the new versionId in these items. It is strongly recommended to manage BinaryProperties and files
        ///      using the BlobStorage API (e.g. BlobStorage.InsertBinaryProperty method).
        ///  8 - Collect last major and last minor versionIds.
        ///  9 - Store the [nodeHeadData] value. Use the new last major and minor versionIds.
        /// 10 - Ensure that the timestamp of the stored nodeHead is incremented.
        /// 11 - Write the following changed values to the parameter objects:
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
        /// </remarks>
        /// <param name="nodeHeadData">Head data of the node. Contains identity information, place in the 
        /// content tree and the most important not-versioned property values.</param>
        /// <param name="versionData">Head information of the current version.</param>
        /// <param name="dynamicData">Metadata and blob data of the current version, categorized into collections:
        /// BinaryProperties: blob information (stream and metadata)
        /// LongTextProperties: long text values that can be lazy loaded.
        /// DynamicProperties: all dynamic property values except binaries and long texts.
        /// </param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        /// <exception cref="NodeAlreadyExistsException">A Node with this path already exists in the database.</exception>
        /// <exception cref="DataException">The operation causes a database-related error.</exception>
        /// <exception cref="OperationCanceledException">The operation has been cancelled.</exception>
        public abstract Task InsertNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData,
            CancellationToken cancellationToken);

        /// <summary>
        /// Updates objects in the database that contain static and dynamic properties of the node.
        /// If the node is renamed (the Name property changed) updates paths in the subtree.
        /// Writes the newly generated ids and timestamps to the [nodeHeadData], [versionData] 
        /// and [dynamicData] parameters:
        ///     NodeTimestamp, VersionTimestamp, BinaryPropertyIds, LastMajorVersionId, LastMinorVersionId.
        /// This method needs to be transactional. If an error occurs during execution, all data changes
        /// should be reverted to the original state by the data provider.
        /// </summary>
        /// <remarks>
        /// Algorithm:
        ///  1 - Begin a new transaction
        ///  2 - Check if the node exists using the [nodeHeadData].NodeId value. Throw a
        /// <see cref="ContentNotFoundException"/> exception if the node is deleted.
        ///  3 - Check if the version exists using the [versionData].VersionId value. Throw a
        /// <see cref="ContentNotFoundException"/> exception if the version is deleted.
        ///  4 - Check concurrent updates: if the provided and stored [nodeHeadData].Timestamp values are not equal, 
        ///      throw a <see cref="NodeIsOutOfDateException"/>.
        ///  5 - Update the stored version data by the [versionData].VersionId with the values in the [versionData] parameter.
        ///  6 - Ensure that the timestamp of the stored version is incremented.
        ///  7 - Delete unnecessary versions listed in the [versionIdsToDelete] parameter.
        ///  8 - Update all dynamic property data including long texts, binary properties and files.
        ///      Use the new versionId in these items. It is strongly recommended to manage BinaryProperties and files 
        ///      using the BlobStorage API (e.g. BlobStorage.UpdateBinaryProperty method).
        ///  9 - Collect last major and last minor versionIds.
        /// 10 - Update the [nodeHeadData] instance. Use the new last major and minor versionIds.
        /// 11 - Ensure that the timestamp of the stored nodeHead is incremented.
        /// 12 - Update paths in the subtree if the [originalPath] is not null. For example: if the [originalPath] 
        ///      is "/Root/Folder1", all paths starting with "/Root/Folder1/" ([originalPath] + trailing slash, 
        ///      case insensitive) will be changed: Replace [originalPath] with the new path in the 
        ///      [nodeHeadData].Path property.
        /// 13 - Write the following changed values to the parameter objects:
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
        /// </remarks>
        /// <param name="nodeHeadData">Head data of the node. Contains identity information, place in the 
        /// content tree and the most important not-versioned property values.</param>
        /// <param name="versionData">Head information of the current version.</param>
        /// <param name="dynamicData">Metadata and blob data of the current version, categorized into collections:
        /// BinaryProperties: blob information (stream and metadata)
        /// LongTextProperties: long textual values that can be lazy loaded.
        /// DynamicProperties: All dynamic property values except the binaries and long texts.
        /// </param>
        /// <param name="versionIdsToDelete">Defines the versions that need to be deleted. Can be empty but not null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <param name="originalPath">Contains the node's original path if it has been renamed. Null if the name has not changed.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        /// <exception cref="ContentNotFoundException">Any part of Node identified by [nodeHeadData].Id or [versionData].Id is missing.</exception>
        /// <exception cref="NodeIsOutOfDateException">The change you want to save is based on outdated basic data.</exception>
        /// <exception cref="DataException">The operation causes a database-related error.</exception>
        /// <exception cref="OperationCanceledException">The operation has been cancelled.</exception>
        public abstract Task UpdateNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData,
            IEnumerable<int> versionIdsToDelete, CancellationToken cancellationToken, string originalPath = null);

        /// <summary>
        /// Copies all objects that contain static and dynamic properties of the node (except the nodeHead instance)
        /// and updates the copy with the provided data. Source version is identified by the [versionData].VersionId. Updates the paths
        /// in the subtree if the node is renamed (i.e. Name property changed). Target version descriptor is the [expectedVersionId]
        /// parameter.
        /// Writes back the newly generated data to the [nodeHeadData], [versionData] and [dynamicData] parameters:
        ///     NodeTimestamp, VersionId, VersionTimestamp, BinaryPropertyIds, LastMajorVersionId, LastMinorVersionId.
        /// This method needs to be transactional. If an error occurs during execution, all data changes
        /// should be reverted to the original state by the data provider.
        /// </summary>
        /// <remarks>
        /// Algorithm:
        ///  1 - Begin a new transaction
        ///  2 - Check if the node exists using the [nodeHeadData].NodeId value. Throw an <see cref="ContentNotFoundException"/> exception if the node is deleted.
        ///  3 - Check if the version exists using the [versionData].VersionId value. Throw an <see cref="ContentNotFoundException"/> exception if the version is deleted.
        ///  4 - Check concurrent updates: if the provided and stored [nodeHeadData].Timestamp values are not equal, 
        ///      throw a <see cref="NodeIsOutOfDateException"/>.
        ///  5 - Determine the target version: if [expectedVersionId] is not null, load the existing instance, 
        ///      otherwise create a new one.
        ///  6 - Copy the source version head data to the target instance and update it with the values in [versionData].
        ///  7 - Ensure that the timestamp of the stored version is incremented.
        ///  8 - Copy the dynamic data to the target and update it with the values in [dynamicData].DynamicProperties.
        ///  9 - Copy the longText data to the target and update it with the values in [dynamicData].LongTextProperties.
        /// 10 - Save binary properties to the target version (copying old values is unnecessary because all 
        ///      binary properties were loaded before save).
        ///      It is strongly recommended to manage BinaryProperties and files 
        ///      using the BlobStorage API (e.g. BlobStorage.InsertBinaryProperty method).
        /// 11 - Delete unnecessary versions listed in the [versionIdsToDelete] parameter.
        /// 12 - Collect last major and last minor versionIds.
        /// 13 - Update the [nodeHeadData] instance. Use the new last major and minor versionIds.
        /// 14 - Ensure that the timestamp of the stored nodeHead is incremented.
        /// 15 - Update paths in the subtree if the [originalPath] is not null. For example: if the [originalPath] 
        ///      is "/Root/Folder1", all paths starting with "/Root/Folder1/" ([originalPath] + trailing slash, 
        ///      case insensitive) will be changed: Replace [originalPath] with the new path in the 
        ///      [nodeHeadData].Path property.
        /// 16 - Write the following changed values to the parameter objects:
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
        /// </remarks>
        /// <param name="nodeHeadData">Head data of the node. Contains identity information, place in the 
        /// content tree and the most important not-versioned property values.</param>
        /// <param name="versionData">Head information of the current version.</param>
        /// <param name="dynamicData">Metadata and blob data of the current version, categorized into collections:
        /// BinaryProperties: blob information (stream and metadata)
        /// LongTextProperties: long textual values that can be lazy loaded.
        /// DynamicProperties: All dynamic property values except the binaries and long texts.
        /// </param>
        /// <param name="versionIdsToDelete">Defines the versions that need to be deleted. Can be empty but not null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <param name="expectedVersionId">Id of the target version. 0 means: need to create a new version.</param>
        /// <param name="originalPath">Contains the node's original path if it has been renamed. Null if the name has not changed.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        /// <exception cref="ContentNotFoundException">Any part of Node identified by [nodeHeadData].Id or [versionData].Id is missing.</exception>
        /// <exception cref="NodeIsOutOfDateException">The change you want to save is based on outdated basic data.</exception>
        /// <exception cref="DataException">The operation causes a database-related error.</exception>
        /// <exception cref="OperationCanceledException">The operation has been cancelled.</exception>
        public abstract Task CopyAndUpdateNodeAsync(
            NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData, IEnumerable<int> versionIdsToDelete,
            CancellationToken cancellationToken, int expectedVersionId = 0, string originalPath = null);

        /// <summary>
        /// Updates the paths in the subtree if the node is renamed (i.e. Name property changed).
        /// This method needs to be transactional. If an error occurs during execution, all data changes
        /// should be reverted to the original state by the data provider.
        /// </summary>
        /// <remarks>
        /// Algorithm:
        ///  1 - Begin a new transaction
        ///  2 - Check if the node exists using the [nodeHeadData].NodeId value. Throw an <see cref="ContentNotFoundException"/> exception if the node is deleted.
        ///  3 - Check concurrent updates: if the provided and stored [nodeHeadData].Timestamp values are not equal, 
        ///      throw a <see cref="NodeIsOutOfDateException"/>.
        ///  4 - Delete unnecessary versions listed in the [versionIdsToDelete] parameter.
        ///  5 - Collect last major and last minor versionIds.
        ///  6 - Update the [nodeHeadData] instance. Use the new last major and minor versionIds.
        ///  7 - Ensure that the timestamp of the stored nodeHead is incremented.
        ///  8 - Write the following changed values to the parameter objects:
        ///      - nodeHead timestamp: [nodeHeadData].Timestamp
        ///      - last major version id: [nodeHeadData].LastMajorVersionId
        ///      - last minor version id: [nodeHeadData].LastMinorVersionId
        ///  9 - Commit the transaction. If there was a problem, rollback the transaction and throw an exception.
        ///      In case of error the data written into the parameters (new ids and changed timestamps)
        ///      will be dropped so rolling back these values is not necessary.
        /// </remarks>
        /// <param name="nodeHeadData">Head data of the node. Contains identity information, place in the 
        /// content tree and the most important not-versioned property values.</param>
        /// <param name="versionIdsToDelete">Defines the versions that need to be deleted. Can be empty but not null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        /// <exception cref="ContentNotFoundException">Any part of Node identified by [nodeHeadData].Id is missing.</exception>
        /// <exception cref="NodeIsOutOfDateException">The change you want to save is based on outdated basic data.</exception>
        /// <exception cref="DataException">The operation causes a database-related error.</exception>
        /// <exception cref="OperationCanceledException">The operation has been cancelled.</exception>
        public abstract Task UpdateNodeHeadAsync(NodeHeadData nodeHeadData, IEnumerable<int> versionIdsToDelete,
            CancellationToken cancellationToken);

        /// <summary>
        /// Loads node data items by the provided versionId set. If a node is not found by it's versionId, the item must be skipped.
        /// </summary>
        /// <remarks>
        /// Algorithm:
        /// 1 - Enumerate [versionIds].
        /// 2 - Load NodeHeadData and VersionData instances by the current versionId.
        /// 3 - Skip further operations if an item is missing.
        /// 4 - Create a new NodeData instance. The constructor parameters may be in the NodeHead instance.
        /// 5 - Fill all properties of the new NodeData instance from the NodeHeadData and VersionData instances.
        /// 6 - Load all dynamic properties by PropertyTypes collection of the new NodeData instance.
        ///     Every property value need to be set to the NodeData instance with the NodeData.SetDynamicRawData method.
        ///     Do not load binary property values (DataType.Binary).
        ///     Do not load text properties (DataType.Text) that are longer than the DataStore.TextAlternationSizeLimit value.
        /// 7 - Return the collected NodeData set.
        /// </remarks>
        /// <param name="versionIds">VersionIds of nodes to load.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the loaded NodeData set.</returns>
        /// <exception cref="DataException">The operation causes a database-related error.</exception>
        /// <exception cref="OperationCanceledException">The operation has been cancelled.</exception>
        public abstract Task<IEnumerable<NodeData>> LoadNodesAsync(int[] versionIds,
            CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the specified Node and its subtree including node head data, all versions and any other
        /// related data of the Node.
        /// Deleting related File records can be skipped if Files data is handled separately.
        /// This method needs to be transactional. If an error occurs during execution, all deleted data
        /// should be reverted to the original state by the data provider.
        /// </summary>
        /// <param name="nodeHeadData">Head data of the node. Contains identity information, place in the 
        /// content tree and the most important not-versioned property values.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        /// <exception cref="NodeIsOutOfDateException">The operation was initiated on outdated data.</exception>
        /// <exception cref="DataException">The operation causes a database-related error.</exception>
        /// <exception cref="OperationCanceledException">The operation has been cancelled.</exception>
        public abstract Task DeleteNodeAsync(NodeHeadData nodeHeadData, CancellationToken cancellationToken);

        /// <summary>
        /// Moves the Node to the specified container.
        /// This method needs to be transactional. If an error occurs during execution, all deleted data
        /// should be reverted to the original state by the data provider.
        /// </summary>
        /// <param name="sourceNodeHeadData">Head data of the node to move.</param>
        /// <param name="targetNodeId">Identifier of the target container.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public abstract Task MoveNodeAsync(NodeHeadData sourceNodeHeadData, int targetNodeId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Returns a Dictionary&lt;int, string&gt; instance that contains LongTextProperty values 
        /// for the provided property types of the requested version.
        /// </summary>
        /// <param name="versionId">Id of the requested version.</param>
        /// <param name="propertiesToLoad">Ids of requested <see cref="PropertyType"/>s.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the loaded Dictionary&lt;int, string&gt; instance.</returns>
        /// <exception cref="DataException">The operation causes a database-related error.</exception>
        /// <exception cref="OperationCanceledException">The operation has been cancelled.</exception>
        public abstract Task<Dictionary<int, string>> LoadTextPropertyValuesAsync(int versionId, int[] propertiesToLoad,
            CancellationToken cancellationToken);

        /// <summary>
        /// Loads metadata of a single binary property.
        /// </summary>
        /// <param name="versionId">Id of the requested version.</param>
        /// <param name="propertyTypeId">Requested PropertyTypeId.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the loaded <see cref="BinaryDataValue"/> instance or null.</returns>
        public abstract Task<BinaryDataValue> LoadBinaryPropertyValueAsync(int versionId, int propertyTypeId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Checks if a node exists with the provided path.
        /// </summary>
        /// <param name="path">Path of a node.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a boolean value
        /// that is true if the database contains a node with the provided path.</returns>
        public abstract Task<bool> NodeExistsAsync(string path, CancellationToken cancellationToken);

        /* =============================================================================================== NodeHead */

        /// <summary>
        /// Loads a <see cref="NodeHead"/> instance.
        /// Returns null if the requested object does not exist in the database.
        /// </summary>
        /// <param name="path">Repository path of the requested node.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the loaded <see cref="NodeHead"/> instance or null.</returns>
        /// <exception cref="DataException">The operation causes a database-related error.</exception>
        /// <exception cref="OperationCanceledException">The operation has been cancelled.</exception>
        public abstract Task<NodeHead> LoadNodeHeadAsync(string path,
            CancellationToken cancellationToken);

        /// <summary>
        /// Loads a <see cref="NodeHead"/> instance.
        /// Returns null if the requested object does not exist in the database.
        /// </summary>
        /// <param name="nodeId">Id of the requested node.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the loaded <see cref="NodeHead"/> instance or null.</returns>
        /// <exception cref="DataException">The operation causes a database-related error.</exception>
        /// <exception cref="OperationCanceledException">The operation has been cancelled.</exception>
        public abstract Task<NodeHead> LoadNodeHeadAsync(int nodeId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Loads a <see cref="NodeHead"/> instance.
        /// Returns null if the requested object does not exist in the database.
        /// </summary>
        /// <param name="versionId">Id of the requested version.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the loaded <see cref="NodeHead"/> instance or null.</returns>
        /// <exception cref="DataException">The operation causes a database-related error.</exception>
        /// <exception cref="OperationCanceledException">The operation has been cancelled.</exception>
        public abstract Task<NodeHead> LoadNodeHeadByVersionIdAsync(int versionId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Loads a set of <see cref="NodeHead"/> instances.
        /// </summary>
        /// <param name="nodeIds">The requested node ids.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns></returns>
        public abstract Task<IEnumerable<NodeHead>> LoadNodeHeadsAsync(IEnumerable<int> nodeIds,
            CancellationToken cancellationToken);

        /// <summary>
        /// Returns version numbers representing all versions of the requested <see cref="Node"/>.
        /// </summary>
        /// <param name="nodeId">Node identifier.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a list of version numbers.</returns>
        public abstract Task<IEnumerable<NodeHead.NodeVersion>> GetVersionNumbersAsync(int nodeId, CancellationToken cancellationToken);
        /// <summary>
        /// Returns version numbers representing all versions of the requested <see cref="Node"/>.
        /// </summary>
        /// <param name="path">Node path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a list of version numbers.</returns>
        public abstract Task<IEnumerable<NodeHead.NodeVersion>> GetVersionNumbersAsync(string path, CancellationToken cancellationToken);

        /// <summary>
        /// Load node heads in multiple subtrees.
        /// </summary>
        /// <param name="paths">Node path list.</param>
        /// <param name="resolveAll">Resolve all paths or only the first one that is found.</param>
        /// <param name="resolveChildren">Resolve child content or not.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a list of collected node heads.</returns>
        public abstract Task<IEnumerable<NodeHead>> LoadNodeHeadsFromPredefinedSubTreesAsync(IEnumerable<string> paths, bool resolveAll, bool resolveChildren,
            CancellationToken cancellationToken);

        /* =============================================================================================== NodeQuery */

        /// <summary>
        /// Gets the count of nodes with the provided node types.
        /// </summary>
        /// <remarks>This methods expects a flattened list of node types. It does not return nodes of derived types.</remarks>
        /// <param name="nodeTypeIds">Array of node type ids. </param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the count of nodes of the requested types.</returns>
        public abstract Task<int> InstanceCountAsync(int[] nodeTypeIds, CancellationToken cancellationToken);
        /// <summary>
        /// Gets the ids of nodes that are children of the provided parent. Only direct children are collected, not the whole subtree.
        /// </summary>
        /// <param name="parentId">Parent node id.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps child node identifiers.</returns>
        public abstract Task<IEnumerable<int>> GetChildrenIdentifiersAsync(int parentId, CancellationToken cancellationToken);
        /// <summary>
        /// Queries <see cref="Node"/>s by the provided criteria. Any parameter may be null or empty.
        /// There are AND logical relations among the criteria and OR relations among elements of each.
        /// </summary>
        /// <param name="nodeTypeIds">Ids of the relevant NodeTypes or null.</param>
        /// <param name="pathStart">Case insensitive repository paths of relevant subtrees or null.</param>
        /// <param name="orderByPath">True if the result set need to be ordered by Path.</param>
        /// <param name="name">Name of the relevant <see cref="Node"/>s or null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the set of found <see cref="Node"/> identifiers.</returns>
        public abstract Task<IEnumerable<int>> QueryNodesByTypeAndPathAndNameAsync(int[] nodeTypeIds, string[] pathStart,
            bool orderByPath, string name, CancellationToken cancellationToken);
        /// <summary>
        /// Queries <see cref="Node"/>s by the provided criteria. Any parameter may be null or empty.
        /// There are AND logical relations among the criteria and OR relations among elements of each.
        /// </summary>
        /// <param name="nodeTypeIds">Ids of relevant NodeTypes or null.</param>
        /// <param name="pathStart">Case insensitive repository path of the relevant subtree or null.</param>
        /// <param name="orderByPath">True if the result set needs to be ordered by Path.</param>
        /// <param name="properties">List of properties that need to be included in the query expression.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the set of found <see cref="Node"/> identifiers.</returns>
        public abstract Task<IEnumerable<int>> QueryNodesByTypeAndPathAndPropertyAsync(int[] nodeTypeIds, string pathStart,
            bool orderByPath, List<QueryPropertyData> properties, CancellationToken cancellationToken);
        /// <summary>
        /// Queries <see cref="Node"/>s by a reference property.
        /// </summary>
        /// <remarks>For example: a list of books from a certain author. In this case the node type is Book,
        /// the reference property is Author and the referred node id is the author id.</remarks>
        /// <param name="referenceName">Name of the reference property to search for.</param>
        /// <param name="referredNodeId">Id of a referred node that the property value should contain.</param>
        /// <param name="nodeTypeIds">Ids of relevant NodeTypes or null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the set of found <see cref="Node"/> identifiers.</returns>
        public abstract Task<IEnumerable<int>> QueryNodesByReferenceAndTypeAsync(string referenceName, int referredNodeId,
            int[] nodeTypeIds, CancellationToken cancellationToken);

        /* =============================================================================================== Tree */

        // /Root
        // /Root/Site1
        // /Root/Site1/Folder1
        // /Root/Site1/Folder1/Folder2
        // /Root/Site1/Folder1/Folder3
        // /Root/Site1/Folder1/Folder3/Task1
        // /Root/Site1/Folder1/DocLib1
        // /Root/Site1/Folder1/DocLib1/File1
        // /Root/Site1/Folder1/DocLib1/SystemFolder1
        // /Root/Site1/Folder1/DocLib1/SystemFolder1/File2
        // /Root/Site1/Folder1/MemoList
        // /Root/Site2
        //
        // Move /Root/Site1/Folder1 to /Root/Site2
        // Expected type list: Folder, Task1, DocLib1, MemoList

        /// <summary>
        /// Gets all the types that can be found in a subtree and are relevant in case of move or copy operations.
        /// </summary>
        /// <remarks>Not all types will be returned, only the ones that should be allowed on the target container.</remarks>
        /// <param name="nodeId">Node identifier.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a list of node types in a subtree.</returns>
        public abstract Task<IEnumerable<NodeType>> LoadChildTypesToAllowAsync(int nodeId, CancellationToken cancellationToken);
        /// <summary>
        /// Gets a list of content list types in a subtree.
        /// </summary>
        /// <param name="path">Subtree path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps
        /// a list of content list types that are found in a subtree.</returns>
        public abstract Task<List<ContentListType>> GetContentListTypesInTreeAsync(string path, CancellationToken cancellationToken);

        /* =============================================================================================== TreeLock */

        /// <summary>
        /// Locks a subtree exclusively.
        /// The return value is 0 if the path is locked in the parent axis or in the subtree.
        /// Checking tree lock existence and creating new lock is an atomic operation.
        /// </summary>
        /// <param name="path">The requested path.</param>
        /// <param name="timeLimit">A <see cref="DateTime"/> value, older tree locks than that are considered to be expired.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the Id of the newly created tree lock or 0.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <c>path</c> is null.</exception>
        /// <exception cref="InvalidPathException">Thrown when <c>path</c> is invalid.</exception>
        /// <exception cref="DataException">The operation causes a database-related error.</exception>
        /// <exception cref="OperationCanceledException">The operation has been cancelled.</exception>
        public abstract Task<int> AcquireTreeLockAsync(string path, DateTime timeLimit, CancellationToken cancellationToken);

        /// <summary>
        /// Checks whether the provided path is locked.
        /// </summary>
        /// <param name="path">The requested path.</param>
        /// <param name="timeLimit">An expiration time limit. Older tree locks are considered
        /// to be expired and are ignored.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a boolean value
        /// that is true if the path is locked.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <c>path</c> is null.</exception>
        /// <exception cref="InvalidPathException">Thrown when <c>path</c> is invalid.</exception>
        /// <exception cref="DataException">The operation causes a database-related error.</exception>
        /// <exception cref="OperationCanceledException">The operation has been cancelled.</exception>
        public abstract Task<bool> IsTreeLockedAsync(string path, DateTime timeLimit,
            CancellationToken cancellationToken);

        /// <summary>
        /// Deletes one or more tree locks by the provided Id set.
        /// </summary>
        /// <param name="lockIds">Ids of the tree locks to delete.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        /// <exception cref="DataException">The operation causes a database-related error.</exception>
        /// <exception cref="OperationCanceledException">The operation has been cancelled.</exception>
        public abstract Task ReleaseTreeLockAsync(int[] lockIds, CancellationToken cancellationToken);
        /// <summary>
        /// Loads all existing tree locks (including expired elements) as an Id, Path dictionary.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the result as an Id, Path dictionary.</returns>
        /// <exception cref="DataException">The operation causes a database-related error.</exception>
        /// <exception cref="OperationCanceledException">The operation has been cancelled.</exception>
        public abstract Task<Dictionary<int, string>> LoadAllTreeLocksAsync(CancellationToken cancellationToken);

        /* =============================================================================================== IndexDocument */

        /// <summary>
        /// Persists the given <see cref="IndexDocument"/> of the version represented by the passed versionId.
        /// Returns the owner version's modified timestamp, if the save task updates the owner version
        /// Returns 0L, if the index document storage is independent from the owner version.
        /// </summary>
        /// <param name="versionId">Id of the requested version.</param>
        /// <param name="indexDoc">The serialized <see cref="IndexDocument"/> that will be saved.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the modified VersionTimestamp or 0L.</returns>
        /// <exception cref="DataException">The operation causes a database-related error.</exception>
        /// <exception cref="OperationCanceledException">The operation has been cancelled.</exception>
        public abstract Task<long> SaveIndexDocumentAsync(int versionId, string indexDoc,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the index document for the provided version ids.
        /// </summary>
        /// <param name="versionIds">Version id array.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the index document array.</returns>
        public abstract Task<IEnumerable<IndexDocumentData>> LoadIndexDocumentsAsync(IEnumerable<int> versionIds, CancellationToken cancellationToken);

        //TODO: Make async version if the .NET framework allows the async enumerable with "yield return".
        public abstract IEnumerable<IndexDocumentData> LoadIndexDocumentsAsync(string path, int[] excludedNodeTypes);

        /// <summary>
        /// Gets ids of nodes that do not have an index document saved in the database.
        /// This method is used by the install process, do not use it from your code.
        /// </summary>
        /// <param name="fromId">Starting node id.</param>
        /// <param name="toId">Max node id.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the list of node ids.</returns>
        public abstract Task<IEnumerable<int>> LoadNotIndexedNodeIdsAsync(int fromId, int toId, CancellationToken cancellationToken);

        /* =============================================================================================== IndexingActivity */

        /// <summary>
        /// Deletes all restore points from the database.
        /// This method is used in the centralized indexing scenario.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public abstract Task DeleteRestorePointsAsync(CancellationToken cancellationToken);
        /// <summary>
        /// Gets the current <see cref="IndexingActivityStatus"/> instance
        /// containing the last executed indexing activity id and ids of missing indexing activities.
        /// This method is used in the centralized indexing scenario.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the current
        /// <see cref="IndexingActivityStatus"/> instance.</returns>
        public abstract Task<IndexingActivityStatus> GetCurrentIndexingActivityStatusAsync(
            CancellationToken cancellationToken);
        /// <summary>
        /// Restores the indexing activity status.
        /// This method is used in the centralized indexing scenario.
        /// </summary>
        /// <param name="status">An <see cref="IndexingActivityStatus"/> instance that contains the latest executed activity id and gaps.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public abstract Task<IndexingActivityStatusRestoreResult> RestoreIndexingActivityStatusAsync(IndexingActivityStatus status,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the latest IndexingActivityId or 0.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the latest IndexingActivity Id ot 0.</returns>
        public abstract Task<int> GetLastIndexingActivityIdAsync(
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets indexing activities between the two provided ids.
        /// </summary>
        /// <remarks>All three boundary parameters are necessary for the algorithm to work because we have to make sure
        /// that no activity is loaded that have a bigger id than the one sealed at the beginning of the process.</remarks>
        /// <param name="fromId">Start activity id.</param>
        /// <param name="toId">Last accepted id.</param>
        /// <param name="count">Maximum number of loaded activities (page size).</param>
        /// <param name="executingUnprocessedActivities">True if the method is called during executing
        /// unprocessed indexing activities at system startup.</param>
        /// <param name="activityFactory">Factory class for creating activity instances.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the array of indexing activities.</returns>
        public abstract Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int fromId, int toId, int count,
            bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory, CancellationToken cancellationToken);
        /// <summary>
        /// Gets indexing activities defined by the provided id array.
        /// </summary>
        /// <param name="gaps">Array of activity ids to load.</param>
        /// <param name="executingUnprocessedActivities">True if the method is called during executing
        /// unprocessed indexing activities at system startup.</param>
        /// <param name="activityFactory">Factory class for creating activity instances.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the array of indexing activities.</returns>
        public abstract Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int[] gaps, bool executingUnprocessedActivities,
            IIndexingActivityFactory activityFactory, CancellationToken cancellationToken);
        /// <summary>
        /// Gets executable and finished indexing activities.
        /// </summary>
        /// <remarks>This method loads a limited number of executable activities and also the ones that
        /// were started long ago (beyond a timeout limit). It also checks whether any of the blocking
        /// activities (ones that we are waiting for) have been completed by other app domains.</remarks>
        /// <param name="activityFactory">Factory class for creating activity instances.</param>
        /// <param name="maxCount">Maximum number of loaded activities.</param>
        /// <param name="runningTimeoutInSeconds">Timeout for running activities.</param>
        /// <param name="waitingActivityIds">An array of activities that this appdomain is waiting for.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the executable activity result.</returns>
        public abstract Task<ExecutableIndexingActivitiesResult> LoadExecutableIndexingActivitiesAsync(
            IIndexingActivityFactory activityFactory, int maxCount, int runningTimeoutInSeconds, int[] waitingActivityIds,
            CancellationToken cancellationToken);
        /// <summary>
        /// Registers an indexing activity in the database.
        /// </summary>
        /// <param name="activity">Indexing activity.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public abstract Task RegisterIndexingActivityAsync(IIndexingActivity activity, CancellationToken cancellationToken);
        /// <summary>
        /// Set the state of an indexing activity.
        /// </summary>
        /// <param name="indexingActivityId">Indexing activity id.</param>
        /// <param name="runningState">A state to set in the database.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public abstract Task UpdateIndexingActivityRunningStateAsync(int indexingActivityId, IndexingActivityRunningState runningState,
            CancellationToken cancellationToken);
        /// <summary>
        /// Refresh the lock time of multiple indexing activities.
        /// </summary>
        /// <param name="waitingIds">Activity ids that we are still working on or waiting for.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public abstract Task RefreshIndexingActivityLockTimeAsync(int[] waitingIds, CancellationToken cancellationToken);
        /// <summary>
        /// Deletes finished activities. Called by a cleanup background process.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public abstract Task DeleteFinishedIndexingActivitiesAsync(CancellationToken cancellationToken);
        /// <summary>
        /// Deletes all activities from the database.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public abstract Task DeleteAllIndexingActivitiesAsync(CancellationToken cancellationToken);

        /* =============================================================================================== Schema */

        /// <summary>
        /// Gets the whole schema definition from the database, including property types, node types and content list types.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the loaded <see cref="RepositorySchemaData"/> instance.</returns>
        public abstract Task<RepositorySchemaData> LoadSchemaAsync(
            CancellationToken cancellationToken);
        /// <summary>
        /// Creates a schema writer for the current data platform.
        /// </summary>
        /// <returns>A schema writer object.</returns>
        public abstract SchemaWriter CreateSchemaWriter();

        /// <summary>
        /// Initiates a schema update operation and locks the schema exclusively.
        /// </summary>
        /// <remarks>
        /// Algorithm:
        /// 1 - Checks the given schemaTimestamp equality. If different, throws an error: "Storage schema is out of date."
        /// 2 - Checks the schemaLock existence. If there is, throws an error: "Schema is locked by someone else."
        /// 3 - Locks the schema exclusively against other modifications and return a schema lock token.
        /// </remarks>
        /// <param name="schemaTimestamp">The current known timestamp of the schema.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the generated schema lock token.</returns>
        /// <exception cref="DataException">The operation causes a database-related error.</exception>
        public abstract Task<string> StartSchemaUpdateAsync(long schemaTimestamp, CancellationToken cancellationToken);
        /// <summary>
        /// Completes a schema write operation and releases the lock.
        /// </summary>
        /// <remarks>
        /// Algorithm:
        /// 1 - Checks whether the provided schemaLock is still valid. If it is different
        /// from the one in the database, it throws an error.
        /// 2 - Returns a newly generated schemaTimestamp.
        /// </remarks>
        /// <param name="schemaLock">Schema lock token.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the new schema timestamp.</returns>
        /// <exception cref="DataException">The operation causes a database-related error.</exception>
        public abstract Task<long> FinishSchemaUpdateAsync(string schemaLock, CancellationToken cancellationToken);

        /* =============================================================================================== Logging */

        /// <summary>
        /// Writes the <see cref="AuditEventInfo"/> to the database.
        /// </summary>
        /// <param name="auditEvent">The <see cref="AuditEventInfo"/> object that will be saved.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public abstract Task WriteAuditEventAsync(AuditEventInfo auditEvent,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the last several audit events from the database.
        /// Intended for internal use.
        /// </summary>
        /// <param name="count">Number of events to load.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a list of audit events.</returns>
        public abstract Task<IEnumerable<AuditLogEntry>> LoadLastAuditEventsAsync(int count,
            CancellationToken cancellationToken);

        /* =============================================================================================== Provider Tools */

        /// <summary>
        /// Returns the provided DateTime value with reduced precision.
        /// </summary>
        /// <param name="d">The DateTime value to round.</param>
        /// <returns>The rounded DateTime value.</returns>
        public abstract DateTime RoundDateTime(DateTime d);
        /// <summary>
        /// Checks whether the text is short enough to be cached.
        /// </summary>
        /// <param name="text">Text value to test for.</param>
        /// <returns>True if the text can be added to the cache.</returns>
        public abstract bool IsCacheableText(string text);
        /// <summary>
        /// Gets the name of a node with the provided name base and the biggest incremental name index.
        /// </summary>
        /// <remarks>For example if there are multiple files in the folder with names like MyDoc(1).docx, MyDoc(2).docx,
        /// this method will return the one with the biggest number.</remarks>
        /// <param name="parentId">Id of the container.</param>
        /// <param name="namebase">Name prefix.</param>
        /// <param name="extension">File extension.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the found file name.</returns>
        public abstract Task<string> GetNameOfLastNodeWithNameBaseAsync(int parentId, string namebase, string extension,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the size of the requested node or the whole subtree.
        /// The size of a Node is the summary of all stream lengths in all committed BinaryProperties.
        /// The size does not contain staged streams (when upload is in progress) and orphaned (deleted) streams.
        /// </summary>
        /// <param name="path">Path of the requested node.</param>
        /// <param name="includeChildren">True if the algorithm should count in all nodes in the whole subtree.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the size of the requested node or subtree.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <c>path</c> is null.</exception>
        /// <exception cref="InvalidPathException">Thrown when <c>path</c> is invalid.</exception>
        /// <exception cref="DataException">The operation causes a database-related error.</exception>
        /// <exception cref="OperationCanceledException">The operation has been cancelled.</exception>
        public abstract Task<long> GetTreeSizeAsync(string path, bool includeChildren,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the count of nodes in a subtree. The number will include the subtree root node.
        /// </summary>
        /// <param name="path">Subtree path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the count of nodes.</returns>
        public abstract Task<int> GetNodeCountAsync(string path, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the count of node versions in the subtree defined by the path parameter.
        /// </summary>
        /// <param name="path">Path of the requested subtree.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the count of versions.</returns>
        public abstract Task<int> GetVersionCountAsync(string path, CancellationToken cancellationToken);

        /* =============================================================================================== Installation */

        /// <summary>
        /// Prepares the initial valid state of the database using the provided storage-model structure.
        /// The database structure (tables, collections, indexes) should be prepared before calling this method 
        /// which happens during the installation process.
        /// </summary>
        /// <param name="data">A storage-model structure to install.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public abstract Task InstallInitialDataAsync(InitialData data, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the Content tree representation for building the security model.
        /// Every node and leaf contains only the Id, ParentId and OwnerId of the node.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps an enumerable <see cref="EntityTreeNodeData"/>
        /// as the Content tree representation.</returns>
        public abstract Task<IEnumerable<EntityTreeNodeData>> LoadEntityTreeAsync(CancellationToken cancellationToken);
        
        /// <summary>
        /// Checks if the database exists and is ready to accept new items.
        /// If this method returns false, the client should call the <see cref="InstallDatabaseAsync"/> method
        /// to prepare the database.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a boolean value
        /// that is true if the database schema is ready.</returns>
        public virtual Task<bool> IsDatabaseReadyAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        /// <summary>
        /// Prepares the database schema for accepting new items.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public virtual Task InstallDatabaseAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /* =============================================================================================== Tools */

        /// <summary>
        /// If overridden in a derived class transforms or wraps any exception into a DataException if the
        /// type of the exception is not one of the following:
        ///   ContentNotFoundException, NodeAlreadyExistsException, NodeIsOutOfDateException
        ///   ArgumentNullException, ArgumentOutOfRangeException, ArgumentException
        ///   NotSupportedException, SnNotSupportedException, NotImplementedException
        /// </summary>
        /// <param name="innerException">Original exception</param>
        /// <param name="message">Optional message if the original exception will be transformed.</param>
        /// <returns>Transformed or wrapped exception.</returns>
        protected virtual Exception GetException(Exception innerException, string message = null)
        {
            return DataStore.GetException(innerException, message);
        }

        /// <summary>
        /// Gets whether an exception refers to a transaction deadlock.
        /// </summary>
        /// <param name="exception">The exception instance to check.</param>
        /// <returns>True if the provided exception refers to a transaction deadlock.</returns>
        public abstract bool IsDeadlockException(Exception exception);
    }
}
