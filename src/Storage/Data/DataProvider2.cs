using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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


        /* =============================================================================================== General API */

        protected async Task<int> ExecuteNonQueryAsync(string script, Action<DbCommand> setParams = null)
        {
            using (var connection = CreateConnection())
            using (var cmd = CreateCommand())
            {
                cmd.Connection = connection;
                cmd.CommandText = script;
                //cmd.Transaction = ????

                setParams?.Invoke(cmd);

                connection.Open();
                return await cmd.ExecuteNonQueryAsync();
            }
        }

        protected Task<T> ExecuteScalarAsync<T>(string script, Func<object, T> callback)
        {
            return ExecuteScalarAsync(script, null, callback);
        }
        protected async Task<T> ExecuteScalarAsync<T>(string script, Action<DbCommand> setParams, Func<object, T> callback)
        {
            using (var connection = CreateConnection())
            using (var cmd = CreateCommand())
            {
                cmd.Connection = connection;
                cmd.CommandText = script;
                //cmd.Transaction = ????

                setParams?.Invoke(cmd);

                connection.Open();
                var value = await cmd.ExecuteScalarAsync();
                return callback(value);
            }
        }

        protected Task<T> ExecuteReaderAsync<T>(string script, Func<DbDataReader, Task<T>> callback)
        {
            return ExecuteReaderAsync(script, null, callback);
        }
        protected async Task<T> ExecuteReaderAsync<T>(string script, Action<DbCommand> setParams, Func<DbDataReader, Task<T>> callbackAsync)
        {
            using (var connection = CreateConnection())
            using (var cmd = CreateCommand())
            {
                cmd.Connection = connection;
                cmd.CommandText = script;
                //cmd.Transaction = ????

                setParams?.Invoke(cmd);

                connection.Open();
                var reader = await cmd.ExecuteReaderAsync();
                return await callbackAsync(reader);
            }
        }

        public abstract DbCommand CreateCommand();
        public abstract DbConnection CreateConnection();
        public abstract DbParameter CreateParameter();

        protected DbParameter CreateParameter(string name, DbType dbType, object value)
        {
            var prm = CreateParameter();
            prm.ParameterName = name;
            prm.DbType = dbType;
            prm.Value = value;
            return prm;
        }
        protected DbParameter CreateParameter(string name, DbType dbType, int size, object value)
        {
            var prm = CreateParameter();
            prm.ParameterName = name;
            prm.DbType = dbType;
            prm.Size = size;
            prm.Value = value;
            return prm;
        }

        /* =============================================================================================== General Impl */

        public virtual string GetTreeSizeDemoScript => throw new NotSupportedException();

        public virtual async Task<long> GetTreeSizeDemoAsync(string path, bool includeChildren)
        {
            RepositoryPath.CheckValidPath(path);

            return await ExecuteScalarAsync(GetTreeSizeDemoScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        CreateParameter("@IncludeChildren", DbType.Byte, includeChildren ? (byte) 1 : 0),
                        CreateParameter("@NodePath", DbType.AnsiString, DataStore.PathMaxLength, path),
                    });
                }, value => (long) value
            );
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
        ///  3 - Ensure a new unique NodeId and use it in the node head representation.
        ///  4 - Ensure a new unique VersionId and use it in the version head representation and any other version related data.
        ///  5 - Store (insert) the [versionData] representation with the new NodeId.
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
        /// </remarks>
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
        /// <exception cref="NodeAlreadyExistsException">The [nodeHeadData].Path already exists in the database.</exception>
        /// <exception cref="DataException">The operation causes any database-related error.</exception>
        /// <exception cref="OperationCanceledException">The token has had cancellation requested.</exception>
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
        /// </summary>
        /// <remarks>
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
        /// <param name="originalPath">Contains the node's original path if it is renamed. Null if the name was not changed.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        /// <exception cref="ContentNotFoundException">Any part of Node identified by [nodeHeadData].Id or [versionData].Id is missing.</exception>
        /// <exception cref="NodeIsOutOfDateException">The change you want to save is based on outdated basic data.</exception>
        /// <exception cref="DataException">The operation causes any database-related error.</exception>
        /// <exception cref="OperationCanceledException">The token has had cancellation requested.</exception>
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
        /// </summary>
        /// <remarks>
        /// Algorithm:
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
        /// <param name="expectedVersionId">Id of the target version. 0 means: need to create a new version.</param>
        /// <param name="originalPath">Contains the node's original path if it is renamed. Null if the name was not changed.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        /// <exception cref="ContentNotFoundException">Any part of Node identified by [nodeHeadData].Id or [versionData].Id is missing.</exception>
        /// <exception cref="NodeIsOutOfDateException">The change you want to save is based on outdated basic data.</exception>
        /// <exception cref="DataException">The operation causes any database-related error.</exception>
        /// <exception cref="OperationCanceledException">The token has had cancellation requested.</exception>
        public abstract Task CopyAndUpdateNodeAsync(
            NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData, IEnumerable<int> versionIdsToDelete,
            int expectedVersionId = 0, string originalPath = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Updates the paths in the subtree if the node is renamed (i.e. Name property changed).
        /// This method needs to be transactional. If an error occurs during execution, all data changes
        /// should be reverted to the original state by the data provider.
        /// </summary>
        /// <remarks>
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
        /// </remarks>
        /// <param name="nodeHeadData">Head data of the node. Contains identity information, place in the 
        /// content tree and the most important not-versioned property values.</param>
        /// <param name="versionIdsToDelete">Defines the versions that need to be deleted. Can be empty but not null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        /// <exception cref="ContentNotFoundException">Any part of Node identified by [nodeHeadData].Id is missing.</exception>
        /// <exception cref="NodeIsOutOfDateException">The change you want to save is based on outdated basic data.</exception>
        /// <exception cref="DataException">The operation causes any database-related error.</exception>
        /// <exception cref="OperationCanceledException">The token has had cancellation requested.</exception>
        public abstract Task UpdateNodeHeadAsync(NodeHeadData nodeHeadData, IEnumerable<int> versionIdsToDelete,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Loads node representations by the given versionId set. If a node not found by it's versionId, the item need to be skipped.
        /// Every loaded node representation need to be transformed to a new NodeData instance. Returns filled NodeData set.
        /// Returns empty set instead of null if no node was loaded.
        /// </summary>
        /// <remarks>
        /// Algorithm:
        /// 1 - Enumerate [versionIds].
        /// 2 - Load NodeHeadData and VersionData representations by the current versionId.
        /// 3 - Skip further operations if any item is missing.
        /// 4 - Create a new NodeData. The constructor parameters may be in the NodeHead representation.
        /// 5 - Fill all properties of the new NodeData instance from the NodeHeadData and VersionData representations.
        /// 6 - Load all dynamic properties by PropertyTypes colleciton of the new NodeData instance.
        ///     Every property value need to be set to the NodeData instance with the NodeData.SetDynamicRawData method.
        ///     Do not load binary properties (DataType.Binary).
        ///     Do not load and text properties (DataType.Text) that are longer than the DataStore.TextAlternationSizeLimit.
        /// 7 - Return the filled or empty NodeData set.
        /// </remarks>
        /// <param name="versionIds">Define versionIds that should to be loaded.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and contains the loaded NodeData set.</returns>
        /// <exception cref="DataException">The operation causes any database-related error.</exception>
        /// <exception cref="OperationCanceledException">The token has had cancellation requested.</exception>
        public abstract Task<IEnumerable<NodeData>> LoadNodesAsync(int[] versionIds, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Deletes the specified Node and its whole subtree including all head data, all versions and any other related part of the Node.
        /// Deletion of the related File representations can be skipped if the Files is handled separatelly.
        /// This method needs to be transactional. If an error occurs during execution, all deleted data
        /// should be reverted to the original state by the data provider.
        /// </summary>
        /// <param name="nodeHeadData">Head data of the node. Contains identity information, place in the 
        /// content tree and the most important not-versioned property values.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        /// <exception cref="NodeIsOutOfDateException">The operation is requested on outdated data.</exception>
        /// <exception cref="DataException">The operation causes any database-related error.</exception>
        /// <exception cref="OperationCanceledException">The token has had cancellation requested.</exception>
        public abstract Task DeleteNodeAsync(NodeHeadData nodeHeadData, CancellationToken cancellationToken = default(CancellationToken));

        public abstract Task MoveNodeAsync(NodeHeadData sourceNodeHeadData, int targetNodeId, long targetTimestamp,
            CancellationToken cancellationToken = default(CancellationToken));

        public abstract Task<Dictionary<int, string>> LoadTextPropertyValuesAsync(int versionId, int[] notLoadedPropertyTypeIds,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Loads a metadata of a single blob. Uses the BlobStorage.LoadBinaryProperty(int, int) method.
        /// </summary>
        /// <param name="versionId">Requested VersionId.</param>
        /// <param name="propertyTypeId">Requested PropertyTypeId.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and contains the loaded <see cref="BinaryDataValue"/> instance or null.</returns>
        public abstract Task<BinaryDataValue> LoadBinaryPropertyValueAsync(int versionId, int propertyTypeId,
            CancellationToken cancellationToken = default(CancellationToken));

        public abstract Task<bool> NodeExistsAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /* =============================================================================================== NodeHead */

        /// <summary>
        /// Loads a <see cref="NodeHead"/> instance by given path.
        /// Returns null if the requested object does not exist in the database.
        /// </summary>
        /// <param name="path">Repository path of the requested object.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and contains the loaded <see cref="NodeHead"/> instance or null.</returns>
        /// <exception cref="DataException">The operation causes any database-related error.</exception>
        /// <exception cref="OperationCanceledException">The token has had cancellation requested.</exception>
        public abstract Task<NodeHead> LoadNodeHeadAsync(string path, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Loads a <see cref="NodeHead"/> instance by given nodeId.
        /// Returns null if the requested object does not exist in the database.
        /// </summary>
        /// <param name="nodeId">Id of the requested object.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and contains the loaded <see cref="NodeHead"/> instance or null.</returns>
        /// <exception cref="DataException">The operation causes any database-related error.</exception>
        /// <exception cref="OperationCanceledException">The token has had cancellation requested.</exception>
        public abstract Task<NodeHead> LoadNodeHeadAsync(int nodeId, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Loads a <see cref="NodeHead"/> instance by given versionId.
        /// Returns null if the requested object does not exist in the database.
        /// </summary>
        /// <param name="versionId">VersionId of the requested object.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and contains the loaded <see cref="NodeHead"/> instance or null.</returns>
        /// <exception cref="DataException">The operation causes any database-related error.</exception>
        /// <exception cref="OperationCanceledException">The token has had cancellation requested.</exception>
        public abstract Task<NodeHead> LoadNodeHeadByVersionIdAsync(int versionId, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Loads a set of <see cref="NodeHead"/> instances by given nodeIds.
        /// </summary>
        /// <param name="nodeIds">Requested ids.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns></returns>
        public abstract Task<IEnumerable<NodeHead>> LoadNodeHeadsAsync(IEnumerable<int> nodeIds, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<NodeHead.NodeVersion[]> GetNodeVersions(int nodeId, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<IEnumerable<VersionNumber>> GetVersionNumbersAsync(int nodeId, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<IEnumerable<VersionNumber>> GetVersionNumbersAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /* =============================================================================================== NodeQuery */

        public abstract Task<int> InstanceCountAsync(int[] nodeTypeIds, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<IEnumerable<int>> GetChildrenIdentfiersAsync(int parentId, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Queries the <see cref="Node"/>s by the given criterias. Every criteria can be null or empty.
        /// There are AND logical relations among the kind of criterias but OR relations among elements of the each criterion.
        /// </summary>
        /// <param name="nodeTypeIds">Ids of the relevant NodeTypes or null.</param>
        /// <param name="pathStart">Case insensitive repository paths of the relevant subtrees or null.</param>
        /// <param name="orderByPath">True if the result set need to be ordered by Path.</param>
        /// <param name="name">Name of the relevant <see cref="Node"/>s or null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and contains the set of found <see cref="Node"/>s' identifiers.</returns>
        public abstract Task<IEnumerable<int>> QueryNodesByTypeAndPathAndNameAsync(int[] nodeTypeIds, string[] pathStart,
            bool orderByPath, string name, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<IEnumerable<int>> QueryNodesByTypeAndPathAndPropertyAsync(int[] nodeTypeIds, string pathStart,
            bool orderByPath, List<QueryPropertyData> properties, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<IEnumerable<int>> QueryNodesByReferenceAndTypeAsync(string referenceName, int referredNodeId,
            int[] nodeTypeIds, CancellationToken cancellationToken = default(CancellationToken));

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
        public abstract Task<IEnumerable<NodeType>> LoadChildTypesToAllowAsync(int nodeId, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<List<ContentListType>> GetContentListTypesInTreeAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /* =============================================================================================== TreeLock */

        /// <summary>
        /// Returns the newly created tree lock Id for the requested path.
        /// The return value is 0 if the path is locked in the parent axis or in the subtree.
        /// Checking tree lock existence and creating new lock is an atomic operation.
        /// </summary>
        /// <param name="path">The requested path.</param>
        /// <param name="timeLimit">A <see cref="DateTime"/> value, older tree locks than that are considered to be expired.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and contains the Id of the newly created tree lock or 0.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <c>path</c> is null.</exception>
        /// <exception cref="InvalidPathException">Thrown when <c>path</c> is invalid.</exception>
        /// <exception cref="DataException">The operation causes any database-related error.</exception>
        /// <exception cref="OperationCanceledException">The token has had cancellation requested.</exception>
        public abstract Task<int> AcquireTreeLockAsync(string path, DateTime timeLimit, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Returns a boolean value that indicates whether the requested path is locked or not.
        /// </summary>
        /// <param name="path">The requested path.</param>
        /// <param name="timeLimit">A <see cref="DateTime"/> value, older tree locks than that are considered to be expired.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and contains a boolen value as result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <c>path</c> is null.</exception>
        /// <exception cref="InvalidPathException">Thrown when <c>path</c> is invalid.</exception>
        /// <exception cref="DataException">The operation causes any database-related error.</exception>
        /// <exception cref="OperationCanceledException">The token has had cancellation requested.</exception>
        public abstract Task<bool> IsTreeLockedAsync(string path, DateTime timeLimit, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Deletes one or more tree locks by the given Id set.
        /// </summary>
        /// <param name="lockIds">Ids of the tree locks to delete.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        /// <exception cref="DataException">The operation causes any database-related error.</exception>
        /// <exception cref="OperationCanceledException">The token has had cancellation requested.</exception>
        public abstract Task ReleaseTreeLockAsync(int[] lockIds, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Loads all existing tree locks (including expired elements) as an Id, Path dictionary.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and contains the result as an Id, Path dictionary.</returns>
        /// <exception cref="DataException">The operation causes any database-related error.</exception>
        /// <exception cref="OperationCanceledException">The token has had cancellation requested.</exception>
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

        /// <summary>
        /// Returns the biggest IndexingActivityId. If there is not any activity, returns 0.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and contains the biggest IndexingActivity Id ot 0.</returns>
        public abstract Task<int> GetLastIndexingActivityIdAsync(CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int fromId, int toId, int count,
            bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int[] gaps, bool executingUnprocessedActivities,
            IIndexingActivityFactory activityFactory, CancellationToken cancellationToken = default(CancellationToken));
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

        /// <summary>
        /// Loads the whole schema of the repository.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and contains the loaded <see cref="RepositorySchemaData"/> instance.</returns>
        public abstract Task<RepositorySchemaData> LoadSchemaAsync(CancellationToken cancellationToken = default(CancellationToken));
        public abstract SchemaWriter CreateSchemaWriter();

        /// <summary>
        /// Initiates a schema update and returns an exclusive lock token.
        /// </summary>
        /// <remarks>
        /// Algorithm:
        /// 1 - Checks the given schemaTimestamp equality. If different, throws an error: "Storage schema is out of date."
        /// 2 - Checks the schemaLock existence. If there is, throws an error: "Schema is locked by someone else."
        /// 3 - Locks the schema exclusively against other modifications and return a schema lock token.
        /// </remarks>
        /// <param name="schemaTimestamp">Timestamp value of the last loaded <see cref="RepositorySchemaData"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A string value as the schema lock token.</returns>
        /// <exception cref="DataException">The operation causes any database-related error.</exception>
        public abstract Task<string> StartSchemaUpdateAsync(long schemaTimestamp, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Finishes the schema update, releases the exclusive schema lock and return the new schema timestamp.
        /// </summary>
        /// <remarks>
        /// Algorithm:
        /// 1 - Checks the given schemaLock equality. If different, throws an error.
        /// 2 - Returns a newly generated schemaTimestamp.
        /// </remarks>
        /// <param name="schemaLock">Schema lock token</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>New schema timestamp.</returns>
        /// <exception cref="DataException">The operation causes any database-related error.</exception>
        public abstract Task<long> FinishSchemaUpdateAsync(string schemaLock, CancellationToken cancellationToken = default(CancellationToken));

        /* =============================================================================================== Logging */

        /// <summary>
        /// Inserts the given <see cref="AuditEventInfo"/> to the database.
        /// </summary>
        /// <param name="auditEvent">The <see cref="AuditEventInfo"/> object that will be saved.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public abstract Task WriteAuditEventAsync(AuditEventInfo auditEvent, CancellationToken cancellationToken = default(CancellationToken));

        /* =============================================================================================== Provider Tools */

        /// <summary>
        /// Returns a passed <see cref="DateTime"/> value with reduced precision.
        /// </summary>
        /// <param name="d"><see cref="DateTime"/> value to round.</param>
        /// <returns>The rounded <see cref="DateTime"/> value.</returns>
        public abstract DateTime RoundDateTime(DateTime d);
        public abstract bool IsCacheableText(string text);
        public abstract Task<string> GetNameOfLastNodeWithNameBaseAsync(int parentId, string namebase, string extension,
            CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Returns the size of the requested node or all the whole subree.
        /// The size of a Node is the summary of all stream lengths in all committed BinaryProperties.
        /// The size does not contain staged streams (uplad is in progress) and orphaned (deleted) streams.
        /// </summary>
        /// <param name="path">Valid repository path of the requested node.</param>
        /// <param name="includeChildren">False if only the requested node's size is relevant.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and contains the a size of the requested subtree.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <c>path</c> is null.</exception>
        /// <exception cref="InvalidPathException">Thrown when <c>path</c> is invalid.</exception>
        /// <exception cref="DataException">The operation causes any database-related error.</exception>
        /// <exception cref="OperationCanceledException">The token has had cancellation requested.</exception>
        public abstract Task<long> GetTreeSizeAsync(string path, bool includeChildren, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<int> GetNodeCountAsync(string path, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<int> GetVersionCountAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /* =============================================================================================== Installation */

        /// <summary>
        /// Prepares the initial valid state of the underlying database by the given storage-model structure.
        /// The database structure (tables, collections, indexes) are already prepared.
        /// This method is called tipically in the installation workflow.
        /// </summary>
        /// <param name="data">A storage-model structure to install.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public abstract Task InstallInitialDataAsync(InitialData data, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Returns the Content tree representation for building the security model.
        /// Every node and leaf contains only the Id, ParentId and OwnerId of the node.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>An enumerable <see cref="EntityTreeNodeData"/> as the Content tree representation.</returns>
        public abstract Task<IEnumerable<EntityTreeNodeData>> LoadEntityTreeAsync(CancellationToken cancellationToken = default(CancellationToken));

        /* =============================================================================================== Tools */

        protected Exception GetException(Exception e)
        {
            return DataStore.GetException(e);
        }

    }
}
