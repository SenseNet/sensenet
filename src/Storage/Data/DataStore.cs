using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Main data access API of the Content Repository. Defines methods for loading and saving 
    /// <see cref="Node"/>s and other repository elements.
    /// </summary>
    public static class DataStore
    {
        /// <summary>
        /// Default size limit for preloading and caching long text values.
        /// </summary>
        public const int TextAlternationSizeLimit = 4000;

        /// <summary>
        /// Gets the current DataProvider instance.
        /// </summary>
        public static DataProvider DataProvider => Providers.Instance.DataProvider;

        /// <summary>
        /// Gets the allowed length of the Path of a <see cref="Node"/>.
        /// </summary>
        public static int PathMaxLength => DataProvider.PathMaxLength;
        /// <summary>
        /// Gets the allowed minimum of a <see cref="DateTime"/> value.
        /// </summary>
        public static DateTime DateTimeMinValue => DataProvider.DateTimeMinValue;
        /// <summary>
        /// Gets the allowed maximum of a <see cref="DateTime"/> value.
        /// </summary>
        public static DateTime DateTimeMaxValue => DataProvider.DateTimeMaxValue;
        /// <summary>
        /// Gets the allowed minimum of a <see cref="decimal"/> value.
        /// </summary>
        public static decimal DecimalMinValue => DataProvider.DecimalMinValue;
        /// <summary>
        /// Gets the allowed maximum of a <see cref="decimal"/> value.
        /// </summary>
        public static decimal DecimalMaxValue => DataProvider.DecimalMaxValue;

        /// <summary>
        /// Returns a data provider extension instance by it's type.
        /// The type need to be an implementation of the <see cref="IDataProviderExtension"/> interface.
        /// </summary>
        /// <typeparam name="T">Type of the requested extension.</typeparam>
        /// <returns>Requested data provider extension instance or null.</returns>
        public static T GetDataProviderExtension<T>() where T : class, IDataProviderExtension
        {
            return DataProvider.GetExtension<T>();
        }

        public static void SetDataProviderExtension(Type providerType, IDataProviderExtension provider)
        {
            DataProvider.SetExtension(providerType, provider);
        }

        /// <summary>
        /// Restores the underlying dataprovider to the initial state after system start.
        /// </summary>
        public static void Reset()
        {
            DataProvider.Reset();
        }

        /* =============================================================================================== Installation */

        /// <summary>
        /// Prepares the initial valid state of the underlying database using the provided storage-model structure.
        /// The database structure (tables, collections, indexes) should be prepared before calling this method 
        /// which happens during the installation process.
        /// </summary>
        /// <param name="data">A storage-model structure to install.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static Task InstallInitialDataAsync(InitialData data, CancellationToken cancellationToken)
        {
            //UNDONE: is this method necessary? InstallDatabaseAsync is able to handle this.
            return DataProvider.InstallInitialDataAsync(data, cancellationToken);
        }

        /// <summary>
        /// Returns the Content tree representation for building the security model.
        /// Every node and leaf contains only the Id, ParentId and OwnerId of the node.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps an enumerable <see cref="EntityTreeNodeData"/>
        /// as the Content tree representation.</returns>
        public static Task<IEnumerable<EntityTreeNodeData>> LoadEntityTreeAsync(CancellationToken cancellationToken)
        {
            return DataProvider.LoadEntityTreeAsync(cancellationToken);
        }

        /// <summary>
        /// Checks if the database exists and is ready to accept new items.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a bool value that is
        /// true if the database already exists and contains the necessary schema.</returns>
        public static Task<bool> IsDatabaseReadyAsync(CancellationToken cancellationToken)
        {
            return DataProvider.IsDatabaseReadyAsync(cancellationToken);
        }

        /// <summary>
        /// Creates the database schema and fills it with the necessary initial data.
        /// </summary>
        /// <param name="initialData">Optional initial data.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static async Task InstallDatabaseAsync(InitialData initialData, CancellationToken cancellationToken)
        {
            await DataProvider.InstallDatabaseAsync(cancellationToken).ConfigureAwait(false);
            await InstallInitialDataAsync(initialData ?? InitialData.Load(new SenseNetServicesInitialData()),
                cancellationToken).ConfigureAwait(false);
        }

        /* =============================================================================================== Nodes */

        /// <summary>
        /// Saves <see cref="Node"/> data to the underlying database. A new version may be created or
        /// an existing one updated based on the algorithm and values provided in the settings parameter.
        /// </summary>
        /// <remarks>This method is responsible for saving the data to the storage and managing the
        /// cache after the operation. It will also update the last version number and timestamp
        /// information stored in the settings and node data parameter objects.</remarks>
        /// <param name="nodeData">Data to save to the database.</param>
        /// <param name="settings">Defines the saving algorithm of the provided data.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the new node head
        /// containing the latest version and time identifiers.</returns>
        internal static async Task<NodeHead> SaveNodeAsync(NodeData nodeData, NodeSaveSettings settings, CancellationToken cancellationToken)
        {
            if (nodeData == null)
                throw new ArgumentNullException(nameof(nodeData));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var isNewNode = nodeData.Id == 0;

            try
            {
                var nodeHeadData = nodeData.GetNodeHeadData();
                var savingAlgorithm = settings.GetSavingAlgorithm();
                var renamed = !isNewNode && nodeData.PathChanged && nodeData.SharedData != null;
                if (settings.NeedToSaveData)
                {
                    var versionData = nodeData.GetVersionData();
                    DynamicPropertyData dynamicData;
                    switch (savingAlgorithm)
                    {
                        case SavingAlgorithm.CreateNewNode:
                            dynamicData = nodeData.GetDynamicData(false);
                            await DataProvider.InsertNodeAsync(nodeHeadData, versionData, dynamicData,
                                cancellationToken).ConfigureAwait(false);
                            // Write back the new NodeId
                            nodeData.Id = nodeHeadData.NodeId;
                            break;
                        case SavingAlgorithm.UpdateSameVersion:
                            dynamicData = nodeData.GetDynamicData(false);
                            if (renamed)
                                await DataProvider.UpdateNodeAsync(
                                    nodeHeadData, versionData, dynamicData, settings.DeletableVersionIds,
                                    cancellationToken, nodeData.SharedData.Path).ConfigureAwait(false);
                            else
                                await DataProvider.UpdateNodeAsync(
                                    nodeHeadData, versionData, dynamicData, settings.DeletableVersionIds, cancellationToken)
                                    .ConfigureAwait(false);
                            break;
                        case SavingAlgorithm.CopyToNewVersionAndUpdate:
                            dynamicData = nodeData.GetDynamicData(true);
                            if (renamed)
                                // Copy to brand new version and rename
                                await DataProvider.CopyAndUpdateNodeAsync(
                                    nodeHeadData, versionData, dynamicData, settings.DeletableVersionIds, cancellationToken,
                                    0, nodeData.SharedData.Path).ConfigureAwait(false);
                            else
                                // Copy to brand new version
                                await DataProvider.CopyAndUpdateNodeAsync(
                                    nodeHeadData, versionData, dynamicData, settings.DeletableVersionIds, cancellationToken)
                                    .ConfigureAwait(false);
                            break;
                        case SavingAlgorithm.CopyToSpecifiedVersionAndUpdate:
                            dynamicData = nodeData.GetDynamicData(true);
                            if (renamed)
                                // Copy to specified version and rename
                                await DataProvider.CopyAndUpdateNodeAsync(
                                    nodeHeadData, versionData, dynamicData, settings.DeletableVersionIds,
                                    cancellationToken,
                                    settings.ExpectedVersionId, nodeData.SharedData.Path).ConfigureAwait(false);
                            else
                                // Copy to specified version
                                await DataProvider.CopyAndUpdateNodeAsync(
                                    nodeHeadData, versionData, dynamicData, settings.DeletableVersionIds,
                                    cancellationToken,
                                    settings.ExpectedVersionId).ConfigureAwait(false);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("Unknown SavingAlgorithm: " + savingAlgorithm);
                    }
                    // Write back the version level changed values
                    nodeData.VersionId = versionData.VersionId;
                    nodeData.VersionTimestamp = versionData.Timestamp;
                }
                else
                {
                    await DataProvider.UpdateNodeHeadAsync(nodeHeadData, settings.DeletableVersionIds,
                        cancellationToken).ConfigureAwait(false);
                }
                // Write back NodeHead level changed values
                var lastMajorVersionId = nodeHeadData.LastMajorVersionId;
                var lastMinorVersionId = nodeHeadData.LastMinorVersionId;
                settings.LastMajorVersionIdAfter = lastMajorVersionId;
                settings.LastMinorVersionIdAfter = lastMinorVersionId;
                nodeData.NodeTimestamp = nodeHeadData.Timestamp;

                // Cache manipulations
                if (!isNewNode)
                    RemoveFromCache(nodeData);

                // here we re-create the node head to insert it into the cache and refresh the version info);
                var head = NodeHead.CreateFromNode(nodeData, lastMinorVersionId, lastMajorVersionId);
                if (MustCache(head.NodeTypeId))
                {
                    var idKey = CreateNodeHeadIdCacheKey(head.Id);
                    var pathKey = CreateNodeHeadPathCacheKey(head.Path);
                    CacheNodeHead(head, idKey, pathKey);
                }

                return head;
            }
            catch (Exception e)
            {
                throw GetException(e);
            }
        }
        private static bool MustCache(int nodeTypeId)
        {
            var nodeType = ActiveSchema.NodeTypes.GetItemById(nodeTypeId);
            if (CacheConfiguration.CacheContentAfterSaveMode != CacheConfiguration.CacheContentAfterSaveOption.Containers)
                return CacheConfiguration.CacheContentAfterSaveMode == CacheConfiguration.CacheContentAfterSaveOption.All;
            return nodeType.IsInstaceOfOrDerivedFrom(NodeType.GetByName("Folder"));
        }

        /// <summary>
        /// A loads a node from the database.
        /// </summary>
        /// <param name="head">A node head representing the node.</param>
        /// <param name="versionId">Version identifier.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a node token
        /// containing the information for constructing the appropriate node object.</returns>
        internal static async Task<NodeToken> LoadNodeAsync(NodeHead head, int versionId, CancellationToken cancellationToken)
        {
            return (await LoadNodesAsync(new[] {head}, new[] {versionId}, cancellationToken).ConfigureAwait(false))
                .FirstOrDefault();
        }
        /// <summary>
        /// A loads an array of nodes from the database.
        /// </summary>
        /// <param name="headArray">A node head array representing the nodes to load.</param>
        /// <param name="versionIdArray">Version identifier array containing version ids of individual nodes.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a node token array
        /// containing the information for constructing the appropriate node objects.</returns>
        internal static async Task<NodeToken[]> LoadNodesAsync(NodeHead[] headArray, int[] versionIdArray, CancellationToken cancellationToken)
        {
            var tokens = new List<NodeToken>();
            var tokensToLoad = new List<NodeToken>();
            try
            {
                for (var i = 0; i < headArray.Length; i++)
                {
                    var head = headArray[i];
                    var versionId = versionIdArray[i];

                    var token = new NodeToken(head.Id, head.NodeTypeId, head.ContentListId, head.ContentListTypeId, 
                        versionId, null)
                    {
                        NodeHead = head
                    };
                    tokens.Add(token);

                    var cacheKey = GenerateNodeDataVersionIdCacheKey(versionId);
                    if (Cache.Get(cacheKey) is NodeData nodeData)
                        token.NodeData = nodeData;
                    else
                        tokensToLoad.Add(token);
                }
                if (tokensToLoad.Count > 0)
                {
                    var versionIds = tokensToLoad.Select(x => x.VersionId).ToArray();
                    var loadedCollection = await DataProvider.LoadNodesAsync(versionIds, cancellationToken).ConfigureAwait(false);
                    foreach (var nodeData in loadedCollection)
                    {
                        if (nodeData != null) // lost version
                        {
                            CacheNodeData(nodeData);
                            var token = tokensToLoad.First(x => x.VersionId == nodeData.VersionId);
                            token.NodeData = nodeData;
                        }
                    }
                }
                return tokens.ToArray();
            }
            catch (Exception e)
            {
                throw GetException(e);
            }
        }
        /// <summary>
        /// Deletes a node from the database.
        /// </summary>
        /// <param name="nodeHead">A node data representing the node.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        internal static Task DeleteNodeAsync(NodeHead nodeHead, CancellationToken cancellationToken)
        {
            return DataProvider.DeleteNodeAsync(nodeHead.GetNodeHeadData(), cancellationToken);
        }
        /// <summary>
        /// Deletes a node from the database.
        /// </summary>
        /// <param name="nodeData">A node data representing the node.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        internal static Task DeleteNodeAsync(NodeData nodeData, CancellationToken cancellationToken)
        {
            return DataProvider.DeleteNodeAsync(nodeData.GetNodeHeadData(), cancellationToken);
        }
        /// <summary>
        /// Moves a node to the provided target container.
        /// </summary>
        /// <param name="sourceNodeHead">A node data representing the node to move.</param>
        /// <param name="targetNodeId">Id of the container where the node will be moved.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        internal static async Task MoveNodeAsync(NodeHead sourceNodeHead, int targetNodeId, CancellationToken cancellationToken)
        {
            var sourceNodeHeadData = sourceNodeHead.GetNodeHeadData();
            await DataProvider.MoveNodeAsync(sourceNodeHeadData, targetNodeId, cancellationToken).ConfigureAwait(false);
            sourceNodeHead.Timestamp = sourceNodeHeadData.Timestamp;
        }
        /// <summary>
        /// Moves a node to the provided target container.
        /// </summary>
        /// <param name="sourceNodeData">A node data representing the node to move.</param>
        /// <param name="targetNodeId">Id of the container where the node will be moved.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        internal static async Task MoveNodeAsync(NodeData sourceNodeData, int targetNodeId, CancellationToken cancellationToken)
        {
            var sourceNodeHeadData = sourceNodeData.GetNodeHeadData();
            await DataProvider.MoveNodeAsync(sourceNodeHeadData, targetNodeId, cancellationToken).ConfigureAwait(false);
            if (!sourceNodeData.IsShared)
                sourceNodeData.NodeTimestamp = sourceNodeHeadData.Timestamp;
        }

        /// <summary>
        /// Loads the provided text property values from the database.
        /// </summary>
        /// <param name="versionId">Version identifier.</param>
        /// <param name="propertiesToLoad">A <see cref="PropertyType"/> id set to load.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a dictionary
        /// containing the loaded text property values.</returns>
        internal static Task<Dictionary<int, string>> LoadTextPropertyValuesAsync(int versionId, int[] propertiesToLoad, CancellationToken cancellationToken)
        {
            return DataProvider.LoadTextPropertyValuesAsync(versionId, propertiesToLoad, cancellationToken);
        }
        /// <summary>
        /// Loads the provided binary property value from the database.
        /// </summary>
        /// <param name="versionId">Version identifier.</param>
        /// <param name="propertyTypeId">A <see cref="PropertyType"/> id to load.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a binary data
        /// containing the information to load the stream.</returns>
        internal static Task<BinaryDataValue> LoadBinaryPropertyValueAsync(int versionId, int propertyTypeId, CancellationToken cancellationToken)
        {
            return DataProvider.LoadBinaryPropertyValueAsync(versionId, propertyTypeId, cancellationToken);
        }

        /// <summary>
        /// Checks if a node exists with the provided path.
        /// </summary>
        /// <param name="path">Path of a node.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a boolean value
        /// that is true if the database contains a node with the provided path.</returns>
        public static async Task<bool> NodeExistsAsync(string path, CancellationToken cancellationToken)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (!CanExistInDatabase(path))
                return false;

            // Look at the cache first
            var pathKey = CreateNodeHeadPathCacheKey(path);
            if (Cache.Get(pathKey) is NodeHead)
                return true;

            return await DataProvider.NodeExistsAsync(path, cancellationToken).ConfigureAwait(false);
        }

        internal static NodeData CreateNewNodeData(Node parent, NodeType nodeType, ContentListType listType, int listId)
        {
            var listTypeId = listType?.Id ?? 0;
            var parentId = parent?.Id ?? 0;
            var userId = AccessProvider.Current.GetOriginalUser().Id;
            var now = RoundDateTime(DateTime.UtcNow);
            var name = string.Concat(nodeType.Name, "-", now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));
            var path = (parent == null) ? "/" + name : RepositoryPath.Combine(parent.Path, name);
            var versionNumber = new VersionNumber(1, 0, VersionStatus.Approved);

            var privateData = new NodeData(nodeType, listType)
            {
                IsShared = false,
                SharedData = null,

                Id = 0,
                NodeTypeId = nodeType.Id,
                ContentListTypeId = listTypeId,
                ContentListId = listId,

                ParentId = parentId,
                Name = name,
                Path = path,
                Index = 0,
                IsDeleted = false,

                CreationDate = now,
                ModificationDate = now,
                CreatedById = userId,
                ModifiedById = userId,
                OwnerId = userId,

                VersionId = 0,
                Version = versionNumber,
                VersionCreationDate = now,
                VersionModificationDate = now,
                VersionCreatedById = userId,
                VersionModifiedById = userId,

                Locked = false,
                LockedById = 0,
                ETag = null,
                LockType = 0,
                LockTimeout = 0,
                LockDate = DateTimeMinValue,
                LockToken = null,
                LastLockUpdate = DateTimeMinValue,

                //TODO: IsSystem

                SavingState = default(ContentSavingState),
                ChangedData = null,

                VersionModificationDateChanged = false,
                VersionModifiedByIdChanged = false,
                ModificationDateChanged = false,
                ModifiedByIdChanged = false
            };
            return privateData;
        }

        /* ----------------------------------------------------------------------------------------------- */

        internal static Stream GetBinaryStream(int nodeId, int versionId, int propertyTypeId)
        {
            // Try to load cached binary entity
            var cacheKey = BinaryCacheEntity.GetCacheKey(versionId, propertyTypeId);
            var binaryCacheEntity = (BinaryCacheEntity)Cache.Get(cacheKey);
            if (binaryCacheEntity == null)
            {
                // Not in cache, load it from the database
                binaryCacheEntity = BlobStorage.LoadBinaryCacheEntityAsync(versionId, propertyTypeId, CancellationToken.None)
                    .GetAwaiter().GetResult();

                // insert the binary cache entity into the 
                // cache only if we know the node id
                if (binaryCacheEntity != null && nodeId != 0)
                {
                    if (!RepositoryEnvironment.WorkingMode.Populating)
                    {
                        var head = NodeHead.Get(nodeId);
                        Cache.Insert(cacheKey, binaryCacheEntity,
                            CacheDependencyFactory.CreateBinaryDataDependency(nodeId, head.Path, head.NodeTypeId));
                    }
                }
            }

            // Not found even in the database
            if (binaryCacheEntity == null || binaryCacheEntity.Length == -1)
                return null;

            return new SnStream(binaryCacheEntity.Context, binaryCacheEntity.RawData);
        }

        /* =============================================================================================== NodeHead */

        /// <summary>
        /// Loads a node head from the database.
        /// </summary>
        /// <param name="path">Path of a node.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a node head
        /// for the provided path or null if it does not exist.</returns>
        internal static async Task<NodeHead> LoadNodeHeadAsync(string path, CancellationToken cancellationToken)
        {
            if (!CanExistInDatabase(path))
                return null;

            var pathKey = CreateNodeHeadPathCacheKey(path);
            var item = (NodeHead)Cache.Get(pathKey);
            if (item == null)
            {
                item = await DataProvider.LoadNodeHeadAsync(path, cancellationToken).ConfigureAwait(false);
                if (item != null)
                    CacheNodeHead(item, CreateNodeHeadIdCacheKey(item.Id), pathKey);
            }

            return item;
        }
        /// <summary>
        /// Loads a node head from the database.
        /// </summary>
        /// <param name="nodeId">Id of a node.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a node head
        /// for the provided id or null if it does not exist.</returns>
        internal static async Task<NodeHead> LoadNodeHeadAsync(int nodeId, CancellationToken cancellationToken)
        {
            if (!CanExistInDatabase(nodeId))
                return null;

            var idKey = CreateNodeHeadIdCacheKey(nodeId);
            var item = (NodeHead)Cache.Get(idKey);
            if (item == null)
            {
                item = await DataProvider.LoadNodeHeadAsync(nodeId, cancellationToken).ConfigureAwait(false);
                if (item != null)
                    CacheNodeHead(item, idKey, CreateNodeHeadPathCacheKey(item.Path));
            }

            return item;
        }
        /// <summary>
        /// Loads a node head from the database.
        /// </summary>
        /// <param name="versionId">Id of a specific node version.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a node head
        /// for the provided version id or null if it does not exist.</returns>
        internal static Task<NodeHead> LoadNodeHeadByVersionIdAsync(int versionId, CancellationToken cancellationToken)
        {
            return DataProvider.LoadNodeHeadByVersionIdAsync(versionId, cancellationToken);
        }
        /// <summary>
        /// Loads node heads from the database.
        /// </summary>
        /// <remarks>This method may return fewer node heads than requested in case not all of them exist.</remarks>
        /// <param name="nodeIds">Ids of the node heads to load.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a list of Node heads for the provided ids.</returns>
        internal static async Task<IEnumerable<NodeHead>> LoadNodeHeadsAsync(IEnumerable<int> nodeIds, CancellationToken cancellationToken)
        {
            var nodeHeads = new List<NodeHead>();
            var headIdsToLoad = new List<int>();
            var nodeIdArray = nodeIds as int[] ?? nodeIds.ToArray();
            foreach (var id in nodeIdArray)
            {
                string idKey = CreateNodeHeadIdCacheKey(id);
                var item = (NodeHead)Cache.Get(idKey);
                if (item == null)
                    headIdsToLoad.Add(id);
                else
                    nodeHeads.Add(item);
            }

            if (headIdsToLoad.Count > 0)
            {
                var heads = await DataProvider.LoadNodeHeadsAsync(headIdsToLoad, cancellationToken).ConfigureAwait(false);

                foreach (var head in heads)
                {
                    if (head != null)
                        CacheNodeHead(head, CreateNodeHeadIdCacheKey(head.Id), CreateNodeHeadPathCacheKey(head.Path));
                    nodeHeads.Add(head);
                }

                // sort the node heads aligned with the original list
                nodeHeads = (from id in nodeIdArray
                             join head in nodeHeads.Where(h => h != null)
                                on id equals head.Id
                             where head != null
                             select head).ToList();
            }
            return nodeHeads;
        }

        /// <summary>
        /// Returns version numbers representing all versions of the requested <see cref="Node"/>.
        /// </summary>
        /// <param name="nodeId">Node identifier.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a list of version numbers.</returns>
        public static Task<IEnumerable<NodeHead.NodeVersion>> GetVersionNumbersAsync(int nodeId, CancellationToken cancellationToken)
        {
            return DataProvider.GetVersionNumbersAsync(nodeId, cancellationToken);
        }
        /// <summary>
        /// Returns version numbers representing all versions of the requested <see cref="Node"/>.
        /// </summary>
        /// <param name="path">Node path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a list of version numbers.</returns>
        public static Task<IEnumerable<NodeHead.NodeVersion>> GetVersionNumbersAsync(string path, CancellationToken cancellationToken)
        {
            return DataProvider.GetVersionNumbersAsync(path, cancellationToken);
        }

        /// <summary>
        /// Load node heads in multiple subtrees.
        /// Used by internal APIs. Dot not use this method in your code.
        /// </summary>
        /// <param name="paths">Node path list.</param>
        /// <param name="resolveAll">Resolve all paths or only the first one that is found.</param>
        /// <param name="resolveChildren">Resolve child content or not.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a list of collected node heads.</returns>
        internal static Task<IEnumerable<NodeHead>> LoadNodeHeadsFromPredefinedSubTreesAsync(IEnumerable<string> paths, bool resolveAll, bool resolveChildren, CancellationToken cancellationToken)
        {
            return DataProvider.LoadNodeHeadsFromPredefinedSubTreesAsync(paths, resolveAll, resolveChildren, cancellationToken);
        }

        /* =============================================================================================== NodeQuery */

        /// <summary>
        /// Gets the count of nodes with the provided node types.
        /// </summary>
        /// <remarks>This methods expects a flattened list of node types. It does not return nodes of derived types.</remarks>
        /// <param name="nodeTypeIds">Array of node type ids. </param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the count of nodes of the requested types.</returns>
        internal static Task<int> InstanceCountAsync(int[] nodeTypeIds, CancellationToken cancellationToken)
        {
            return DataProvider.InstanceCountAsync(nodeTypeIds, cancellationToken);
        }
        /// <summary>
        /// Gets the ids of nodes that are children of the provided parent. Only direct children are collected, not the whole subtree.
        /// </summary>
        /// <param name="parentId">Parent node id.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps child node identifiers.</returns>
        internal static Task<IEnumerable<int>> GetChildrenIdentifiersAsync(int parentId, CancellationToken cancellationToken)
        {
            return DataProvider.GetChildrenIdentifiersAsync(parentId, cancellationToken);
        }
        /// <summary>
        /// Queries <see cref="Node"/>s by their path.
        /// </summary>
        /// <param name="pathStart">Case insensitive repository path of the required subtree or null.</param>
        /// <param name="orderByPath">True if the result set needs to be ordered by Path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the set of found <see cref="Node"/> identifiers.</returns>
        internal static Task<IEnumerable<int>> QueryNodesByPathAsync(string pathStart, bool orderByPath, CancellationToken cancellationToken)
        {
            return QueryNodesByTypeAndPathAsync(null, pathStart, orderByPath, cancellationToken);
        }
        /// <summary>
        /// Queries <see cref="Node"/>s by their type.
        /// </summary>
        /// <param name="nodeTypeIds">Ids of relevant NodeTypes.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the set of found <see cref="Node"/> identifiers.</returns>
        internal static Task<IEnumerable<int>> QueryNodesByTypeAsync(int[] nodeTypeIds, CancellationToken cancellationToken)
        {
            return QueryNodesByTypeAndPathAsync(nodeTypeIds, new string[0], false, cancellationToken);
        }
        /// <summary>
        /// Queries <see cref="Node"/>s by the provided criteria. Any parameter may be null or empty.
        /// There are AND logical relations among the criteria and OR relations among elements of each.
        /// </summary>
        /// <param name="nodeTypeIds">Ids of relevant NodeTypes or null.</param>
        /// <param name="pathStart">Case insensitive repository path of the relevant subtree or null.</param>
        /// <param name="orderByPath">True if the result set needs to be ordered by Path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the set of found <see cref="Node"/> identifiers.</returns>
        internal static Task<IEnumerable<int>> QueryNodesByTypeAndPathAsync(int[] nodeTypeIds, string pathStart, bool orderByPath, CancellationToken cancellationToken)
        {
            return QueryNodesByTypeAndPathAndNameAsync(nodeTypeIds, pathStart, orderByPath, null, cancellationToken);
        }
        /// <summary>
        /// Queries <see cref="Node"/>s by the provided criteria. Any parameter may be null or empty.
        /// There are AND logical relations among the criteria and OR relations among elements of each.
        /// </summary>
        /// <param name="nodeTypeIds">Ids of relevant NodeTypes or null.</param>
        /// <param name="pathStart">Case insensitive repository paths of relevant subtrees or null.</param>
        /// <param name="orderByPath">True if the result set needs to be ordered by Path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the set of found <see cref="Node"/> identifiers.</returns>
        internal static Task<IEnumerable<int>> QueryNodesByTypeAndPathAsync(int[] nodeTypeIds, string[] pathStart, bool orderByPath, CancellationToken cancellationToken)
        {
            return QueryNodesByTypeAndPathAndNameAsync(nodeTypeIds, pathStart, orderByPath, null, cancellationToken);
        }
        /// <summary>
        /// Queries <see cref="Node"/>s by the provided criteria. Any parameter may be null or empty.
        /// There are AND logical relations among the criteria and OR relations among elements of each.
        /// </summary>
        /// <param name="nodeTypeIds">Ids of relevant NodeTypes or null.</param>
        /// <param name="pathStart">Case insensitive repository path of the relevant subtree or null.</param>
        /// <param name="orderByPath">True if the result set needs to be ordered by Path.</param>
        /// <param name="name">Name of the relevant <see cref="Node"/>s or null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the set of found <see cref="Node"/> identifiers.</returns>
        internal static Task<IEnumerable<int>> QueryNodesByTypeAndPathAndNameAsync(int[] nodeTypeIds, string pathStart, bool orderByPath, string name, CancellationToken cancellationToken)
        {
            return QueryNodesByTypeAndPathAndNameAsync(nodeTypeIds, new[] { pathStart }, orderByPath, name, cancellationToken);
        }
        /// <summary>
        /// Queries <see cref="Node"/>s by the provided criteria. Any parameter may be null or empty.
        /// There are AND logical relations among the criteria and OR relations among elements of each.
        /// </summary>
        /// <param name="nodeTypeIds">Ids of relevant NodeTypes or null.</param>
        /// <param name="pathStart">Case insensitive repository paths of relevant subtrees or null.</param>
        /// <param name="orderByPath">True if the result set needs to be ordered by Path.</param>
        /// <param name="name">Name of the relevant <see cref="Node"/>s or null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the set of found <see cref="Node"/> identifiers.</returns>
        internal static Task<IEnumerable<int>> QueryNodesByTypeAndPathAndNameAsync(int[] nodeTypeIds, string[] pathStart, bool orderByPath, string name, CancellationToken cancellationToken)
        {
            return DataProvider.QueryNodesByTypeAndPathAndNameAsync(nodeTypeIds, pathStart, orderByPath, name, cancellationToken);
        }

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
        public static Task<IEnumerable<int>> QueryNodesByTypeAndPathAndPropertyAsync(int[] nodeTypeIds, string pathStart, bool orderByPath, List<QueryPropertyData> properties, CancellationToken cancellationToken)
        {
            return DataProvider.QueryNodesByTypeAndPathAndPropertyAsync(nodeTypeIds, pathStart, orderByPath, properties, cancellationToken);
        }

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
        internal static Task<IEnumerable<int>> QueryNodesByReferenceAndTypeAsync(string referenceName, int referredNodeId, int[] nodeTypeIds, CancellationToken cancellationToken)
        {
            return DataProvider.QueryNodesByReferenceAndTypeAsync(referenceName, referredNodeId, nodeTypeIds, cancellationToken);
        }

        /* =============================================================================================== Tree */

        /// <summary>
        /// Gets all the types that can be found in a subtree and are relevant in case of move or copy operations.
        /// </summary>
        /// <remarks>Not all types will be returned, only the ones that should be allowed on the target container.</remarks>
        /// <param name="nodeId">Node identifier.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a list of node types in a subtree.</returns>
        internal static Task<IEnumerable<NodeType>> LoadChildTypesToAllowAsync(int nodeId, CancellationToken cancellationToken)
        {
            return DataProvider.LoadChildTypesToAllowAsync(nodeId, cancellationToken);
        }
        /// <summary>
        /// Gets a list of content list types in a subtree.
        /// </summary>
        /// <param name="path">Subtree path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps
        /// a list of content list types that are found in a subtree.</returns>
        internal static Task<List<ContentListType>> GetContentListTypesInTreeAsync(string path, CancellationToken cancellationToken)
        {
            return DataProvider.GetContentListTypesInTreeAsync(path, cancellationToken);
        }

        /* =============================================================================================== TreeLock */

        /// <summary>
        /// Locks a subtree exclusively. The return value is 0 if the path is locked in the parent axis or in the subtree.
        /// </summary>
        /// <param name="path">Node path to lock.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the newly created
        /// tree lock Id for the requested path or 0.</returns>
        internal static Task<int> AcquireTreeLockAsync(string path, CancellationToken cancellationToken)
        {
            return DataProvider.AcquireTreeLockAsync(path, GetTreeLockTimeLimit(), cancellationToken);
        }
        /// <summary>
        /// Checks whether the provided path is locked.
        /// </summary>
        /// <param name="path">Node path to lock.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a boolean value
        /// that is true if the path is already locked.</returns>
        internal static Task<bool> IsTreeLockedAsync(string path, CancellationToken cancellationToken)
        {
            return DataProvider.IsTreeLockedAsync(path, GetTreeLockTimeLimit(), cancellationToken);
        }

        /// <summary>
        /// Releases the locks represented by the provided lock id array.
        /// </summary>
        /// <param name="lockIds">Array of lock identifiers.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        internal static Task ReleaseTreeLockAsync(int[] lockIds, CancellationToken cancellationToken)
        {
            return DataProvider.ReleaseTreeLockAsync(lockIds, cancellationToken);
        }
        /// <summary>
        /// Gets all tree locks in the system.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a lock id, path dictionary.</returns>
        internal static Task<Dictionary<int, string>> LoadAllTreeLocksAsync(CancellationToken cancellationToken)
        {
            return DataProvider.LoadAllTreeLocksAsync(cancellationToken);
        }
        private static DateTime GetTreeLockTimeLimit()
        {
            return DateTime.UtcNow.AddHours(-8.0);
        }

        /* =============================================================================================== IndexDocument */

        private static IIndexDocumentProvider IndexDocumentProvider => Providers.Instance.IndexDocumentProvider;

        /// <summary>
        /// Generates the index document of a node and saves it to the database.
        /// </summary>
        /// <param name="node">The Node to index.</param>
        /// <param name="skipBinaries">True if binary properties should be skipped.</param>
        /// <param name="isNew">True if the node is new.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the result including
        /// index document data.</returns>
        public static async Task<SavingIndexDocumentDataResult> SaveIndexDocumentAsync(Node node, bool skipBinaries, bool isNew,
            CancellationToken cancellationToken)
        {
            if (node.Id == 0)
                throw new NotSupportedException("Cannot save indexing information before the node is saved.");

            node.MakePrivateData(); // this is important because version timestamp will be changed.

            var doc = IndexDocumentProvider.GetIndexDocument(node, skipBinaries, isNew, out var hasBinary);
            var serializedIndexDocument = doc.Serialize();

            await SaveIndexDocumentAsync(node.Data, doc, cancellationToken).ConfigureAwait(false);

            return new SavingIndexDocumentDataResult
            {
                IndexDocumentData = CreateIndexDocumentData(node, doc, serializedIndexDocument),
                HasBinary = hasBinary
            };
        }

        /// <summary>
        /// Completes the index document of a node and saves it to the database.
        /// </summary>
        /// <param name="node">The Node to index.</param>
        /// <param name="indexDocumentData">Index document data assembled by previous steps in the save operation.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the resulting index document data.</returns>
        public static async Task<IndexDocumentData> SaveIndexDocumentAsync(Node node, IndexDocumentData indexDocumentData,
            CancellationToken cancellationToken)
        {
            if (node.Id == 0)
                throw new NotSupportedException("Cannot save the indexing information before node is not saved.");

            node.MakePrivateData(); // this is important because version timestamp will be changed.

            var completedDocument = IndexDocumentProvider.CompleteIndexDocument(node, indexDocumentData.IndexDocument);
            var serializedIndexDocument = completedDocument.Serialize();

            await SaveIndexDocumentAsync(node.Data, completedDocument, cancellationToken).ConfigureAwait(false);

            return CreateIndexDocumentData(node, completedDocument, serializedIndexDocument);
        }
        private static IndexDocumentData CreateIndexDocumentData(Node node, IndexDocument indexDocument, string serializedIndexDocument)
        {
            return new IndexDocumentData(indexDocument, serializedIndexDocument)
            {
                NodeTypeId = node.NodeTypeId,
                VersionId = node.VersionId,
                NodeId = node.Id,
                ParentId = node.ParentId,
                Path = node.Path,
                IsSystem = node.IsSystem,
                IsLastDraft = node.IsLatestVersion,
                IsLastPublic = node.IsLastPublicVersion,
                NodeTimestamp = node.NodeTimestamp,
                VersionTimestamp = node.VersionTimestamp
            };
        }
        private static async Task SaveIndexDocumentAsync(NodeData nodeData, IndexDocument indexDoc, CancellationToken cancellationToken)
        {
            var timestamp = await SaveIndexDocumentAsync(nodeData.VersionId, indexDoc, cancellationToken).ConfigureAwait(false);
            if (timestamp != 0)
                nodeData.VersionTimestamp = timestamp;
        }

        /// <summary>
        /// Serializes the index document of a node and saves it to the database.
        /// </summary>
        /// <param name="versionId">The id of the target node version.</param>
        /// <param name="indexDoc">Index document assembled by previous steps in the save operation.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the new version timestamp.</returns>
        public static async Task<long> SaveIndexDocumentAsync(int versionId, IndexDocument indexDoc, CancellationToken cancellationToken)
        {
            var serialized = indexDoc.Serialize();
            return await DataProvider.SaveIndexDocumentAsync(versionId, serialized, cancellationToken).ConfigureAwait(false);
        }

        /* ----------------------------------------------------------------------------------------------- Load IndexDocument */

        /// <summary>
        /// Gets the index document for the provided version id.
        /// </summary>
        /// <param name="versionId">Version identifier.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the index document or null.</returns>
        public static async Task<IndexDocumentData> LoadIndexDocumentByVersionIdAsync(int versionId, CancellationToken cancellationToken)
        {
            var result = await DataProvider.LoadIndexDocumentsAsync(new []{versionId}, cancellationToken).ConfigureAwait(false);
            return result.FirstOrDefault();
        }
        /// <summary>
        /// Gets index documents for the provided version ids.
        /// </summary>
        /// <param name="versionIds">Version identifiers.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the list of loaded index documents.</returns>
        public static Task<IEnumerable<IndexDocumentData>> LoadIndexDocumentsAsync(IEnumerable<int> versionIds, CancellationToken cancellationToken)
        {
            return DataProvider.LoadIndexDocumentsAsync(versionIds, cancellationToken);
        }
        /// <summary>
        /// Gets index document data for the provided subtree.
        /// </summary>
        /// <param name="path">Node path.</param>
        /// <param name="excludedNodeTypes">Array of node types that should be skipped during collecting index document data.</param>
        /// <returns>The list of loaded index documents.</returns>
        public static IEnumerable<IndexDocumentData> LoadIndexDocumentsAsync(string path, int[] excludedNodeTypes)
        {
            return DataProvider.LoadIndexDocumentsAsync(path, excludedNodeTypes);
        }
        /// <summary>
        /// Gets ids of nodes that do not have an index document saved in the database.
        /// This method is used by the install process, do not use it from your code.
        /// </summary>
        /// <param name="fromId">Starting node id.</param>
        /// <param name="toId">Max node id.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the list of node ids.</returns>
        public static Task<IEnumerable<int>> LoadNotIndexedNodeIdsAsync(int fromId, int toId, CancellationToken cancellationToken)
        {
            return DataProvider.LoadNotIndexedNodeIdsAsync(fromId, toId, cancellationToken);
        }

        /* =============================================================================================== IndexingActivity */

        /// <summary>
        /// Gets the latest IndexingActivityId or 0.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        public static Task<int> GetLastIndexingActivityIdAsync(CancellationToken cancellationToken)
        {
            return DataProvider.GetLastIndexingActivityIdAsync(cancellationToken);
        }
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
        public static Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int fromId, int toId, int count, bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory, CancellationToken cancellationToken)
        {
            return DataProvider.LoadIndexingActivitiesAsync(fromId, toId, count, executingUnprocessedActivities, activityFactory, cancellationToken);
        }

        /// <summary>
        /// Gets indexing activities defined by the provided id array.
        /// </summary>
        /// <param name="gaps">Array of activity ids to load.</param>
        /// <param name="executingUnprocessedActivities">True if the method is called during executing
        /// unprocessed indexing activities at system startup.</param>
        /// <param name="activityFactory">Factory class for creating activity instances.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the array of indexing activities.</returns>
        public static Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int[] gaps, bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory, CancellationToken cancellationToken)
        {
            return DataProvider.LoadIndexingActivitiesAsync(gaps, executingUnprocessedActivities, activityFactory, cancellationToken);
        }

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
        public static Task<ExecutableIndexingActivitiesResult> LoadExecutableIndexingActivitiesAsync(IIndexingActivityFactory activityFactory, int maxCount, int runningTimeoutInSeconds, int[] waitingActivityIds, CancellationToken cancellationToken)
        {
            return DataProvider.LoadExecutableIndexingActivitiesAsync(activityFactory, maxCount, runningTimeoutInSeconds, waitingActivityIds, cancellationToken);
        }
        /// <summary>
        /// Registers an indexing activity in the database.
        /// </summary>
        /// <param name="activity">Indexing activity.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static Task RegisterIndexingActivityAsync(IIndexingActivity activity, CancellationToken cancellationToken)
        {
            return DataProvider.RegisterIndexingActivityAsync(activity, cancellationToken);
        }
        /// <summary>
        /// Set the state of an indexing activity.
        /// </summary>
        /// <param name="indexingActivityId">Indexing activity id.</param>
        /// <param name="runningState">A state to set in the database.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static Task UpdateIndexingActivityRunningStateAsync(int indexingActivityId, IndexingActivityRunningState runningState, CancellationToken cancellationToken)
        {
            return DataProvider.UpdateIndexingActivityRunningStateAsync(indexingActivityId, runningState, cancellationToken);
        }
        /// <summary>
        /// Refresh the lock time of multiple indexing activities.
        /// </summary>
        /// <param name="waitingIds">Activity ids that we are still working on or waiting for.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static Task RefreshIndexingActivityLockTimeAsync(int[] waitingIds, CancellationToken cancellationToken)
        {
            return DataProvider.RefreshIndexingActivityLockTimeAsync(waitingIds, cancellationToken);
        }
        /// <summary>
        /// Deletes finished activities. Called by a cleanup background process.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static Task DeleteFinishedIndexingActivitiesAsync(CancellationToken cancellationToken)
        {
            return DataProvider.DeleteFinishedIndexingActivitiesAsync(cancellationToken);
        }
        /// <summary>
        /// Deletes all activities from the database.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static Task DeleteAllIndexingActivitiesAsync(CancellationToken cancellationToken)
        {
            return DataProvider.DeleteAllIndexingActivitiesAsync(cancellationToken);
        }

        /* =============================================================================================== Schema */

        /// <summary>
        /// Gets the whole schema definition from the database, including property types, node types and content list types.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the schema definition.</returns>
        internal static Task<RepositorySchemaData> LoadSchemaAsync(CancellationToken cancellationToken)
        {
            return DataProvider.LoadSchemaAsync(cancellationToken);
        }

        /// <summary>
        /// Initiates a schema update operation and locks the schema exclusively.
        /// </summary>
        /// <param name="schemaTimestamp">The current known timestamp of the schema.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the generated schema lock token.</returns>
        internal static Task<string> StartSchemaUpdateAsync(long schemaTimestamp, CancellationToken cancellationToken)
        {
            try
            {
                return DataProvider.StartSchemaUpdateAsync(schemaTimestamp, cancellationToken);
            }
            catch (Exception e)
            {
                throw GetException(e);
            }
        }
        /// <summary>
        /// Creates a schema writer supported by the current data provider.
        /// </summary>
        /// <returns>A schema writer object.</returns>
        internal static SchemaWriter CreateSchemaWriter()
        {
            return DataProvider.CreateSchemaWriter();
        }
        /// <summary>
        /// Completes a schema write operation and releases the lock.
        /// </summary>
        /// <param name="schemaLock">The lock token generated by the start operation before.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the new schema timestamp.</returns>
        internal static Task<long> FinishSchemaUpdateAsync(string schemaLock, CancellationToken cancellationToken)
        {
            try
            {
                return DataProvider.FinishSchemaUpdateAsync(schemaLock, cancellationToken);
            }
            catch (Exception e)
            {
                throw GetException(e);
            }
        }

        #region Backward compatibility

        private static readonly int _contentListStartPage = 10000000;
        internal static readonly int StringPageSize = 80;
        internal static readonly int IntPageSize = 40;
        internal static readonly int DateTimePageSize = 25;
        internal static readonly int CurrencyPageSize = 15;

        public static IDictionary<DataType, int> ContentListMappingOffsets { get; } =
            new ReadOnlyDictionary<DataType, int>(new Dictionary<DataType, int>
            {
            {DataType.String, StringPageSize * _contentListStartPage},
            {DataType.Int, IntPageSize * _contentListStartPage},
            {DataType.DateTime, DateTimePageSize * _contentListStartPage},
            {DataType.Currency, CurrencyPageSize * _contentListStartPage},
            {DataType.Binary, 0},
            {DataType.Reference, 0},
            {DataType.Text, 0}
        });

        #endregion

        /* =============================================================================================== Logging */

        /// <summary>
        /// Writes an audit event to the database.
        /// </summary>
        /// <param name="auditEvent">Audit event info.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public static Task WriteAuditEventAsync(AuditEventInfo auditEvent, CancellationToken cancellationToken)
        {
            return DataProvider.WriteAuditEventAsync(auditEvent, cancellationToken);
        }

        /// <summary>
        /// Gets the last several audit events from the database.
        /// Intended for internal use.
        /// </summary>
        /// <param name="count">Number of events to load.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a list of audit events.</returns>
        public static Task<IEnumerable<AuditLogEntry>> LoadLastAuditEventsAsync(int count,
            CancellationToken cancellationToken)
        {
            return DataProvider.LoadLastAuditEventsAsync(count, cancellationToken);
        }

        /* =============================================================================================== Tools */

        /// <summary>
        /// Returns the provided DateTime value with reduced precision.
        /// </summary>
        /// <param name="d">The DateTime value to round.</param>
        /// <returns>The rounded DateTime value.</returns>
        public static DateTime RoundDateTime(DateTime d)
        {
            return DataProvider.RoundDateTime(d);
        }
        /// <summary>
        /// Checks whether the text is short enough to be cached.
        /// By default it uses the <see cref="TextAlternationSizeLimit"/> value as a limit.
        /// </summary>
        /// <param name="value">Text value to test for.</param>
        /// <returns>True if the text can be added to the cache.</returns>
        public static bool IsCacheableText(string value)
        {
            return DataProvider.IsCacheableText(value);
        }
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
        public static Task<string> GetNameOfLastNodeWithNameBaseAsync(int parentId, string namebase, string extension, CancellationToken cancellationToken)
        {
            return DataProvider.GetNameOfLastNodeWithNameBaseAsync(parentId, namebase, extension, cancellationToken);
        }
        /// <summary>
        /// Gets the size of the requested node or the whole subtree.
        /// The size of a Node is the summary of all stream lengths in all committed BinaryProperties.
        /// The size does not contain staged streams (when upload is in progress) and orphaned (deleted) streams.
        /// </summary>
        /// <param name="path">Path of the requested node.</param>
        /// <param name="includeChildren">True if the algorithm should count in all nodes in the whole subtree.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the size of the requested node or subtree.</returns>
        public static Task<long> GetTreeSizeAsync(string path, bool includeChildren, CancellationToken cancellationToken)
        {
            return DataProvider.GetTreeSizeAsync(path, includeChildren, cancellationToken);
        }
        /// <summary>
        /// Gets the count of nodes in the whole Content Repository.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the count of nodes.</returns>
        public static Task<int> GetNodeCountAsync(CancellationToken cancellationToken)
        {
            return GetNodeCountAsync(null, cancellationToken);
        }
        /// <summary>
        /// Gets the count of nodes in a subtree. The number will include the subtree root node.
        /// </summary>
        /// <param name="path">Subtree path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the count of nodes.</returns>
        public static Task<int> GetNodeCountAsync(string path, CancellationToken cancellationToken)
        {
            return DataProvider.GetNodeCountAsync(path, cancellationToken);
        }
        /// <summary>
        /// Gets the count of node versions in the whole Content Repository.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the count of versions.</returns>
        public static Task<int> GetVersionCountAsync(CancellationToken cancellationToken)
        {
            return DataProvider.GetVersionCountAsync(null, cancellationToken);
        }
        /// <summary>
        /// Gets the count of node versions in a subtree. The number will include the versions of the subtree root node.
        /// </summary>
        /// <param name="path">Subtree path.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the count of versions.</returns>
        public static Task<int> GetVersionCountAsync(string path, CancellationToken cancellationToken)
        {
            return DataProvider.GetVersionCountAsync(path, cancellationToken);
        }

        internal static bool CanExistInDatabase(int id)
        {
            return id > 0;
        }
        internal static bool CanExistInDatabase(string path)
        {
            if (String.IsNullOrEmpty(path))
                return false;

            if (!path.StartsWith("/root", StringComparison.OrdinalIgnoreCase))
                return false;

            if (path.EndsWith("/$count", StringComparison.OrdinalIgnoreCase))
                return false;

            if (path.EndsWith("signalr/send", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        internal static Exception GetException(Exception e, string message = null)
        {
            if (DataProvider.IsDeadlockException(e))
                return new TransactionDeadlockedException("Transaction was deadlocked.", e);

            switch (e)
            {
                case ContentNotFoundException _: return e;
                case NodeAlreadyExistsException _: return e;
                case NodeIsOutOfDateException _: return e;
                case ArgumentNullException _: return e;
                case ArgumentOutOfRangeException _: return e;
                case ArgumentException _: return e;
                case DataException _: return e;
                case NotSupportedException _: return e;
                case SnNotSupportedException _: return e;
                case NotImplementedException _: return e;
            }
            return new DataException(message ??
                "A database exception occured during execution of the operation." +
                " See InnerException for details.", e);
        }

        /* =============================================================================================== */

        private static readonly string NodeHeadPrefix = "NodeHeadCache.";
        private static readonly string NodeDataPrefix = "NodeData.";
        internal static string CreateNodeHeadPathCacheKey(string path)
        {
            return string.Concat(NodeHeadPrefix, path.ToLowerInvariant());
        }
        internal static string CreateNodeHeadIdCacheKey(int nodeId)
        {
            return string.Concat(NodeHeadPrefix, nodeId);
        }
        internal static string GenerateNodeDataVersionIdCacheKey(int versionId)
        {
            return string.Concat(NodeDataPrefix, versionId);
        }

        internal static void CacheNodeHead(NodeHead nodeHead)
        {
            var idKey = CreateNodeHeadIdCacheKey(nodeHead.Id);
            if (null != Cache.Get(idKey))
                return;
            CacheNodeHead(nodeHead, idKey, CreateNodeHeadPathCacheKey(nodeHead.Path));
        }
        internal static void CacheNodeHead(NodeHead head, string idKey, string pathKey)
        {
            var dependencyForPathKey = CacheDependencyFactory.CreateNodeHeadDependency(head);
            var dependencyForIdKey = CacheDependencyFactory.CreateNodeHeadDependency(head);
            Cache.Insert(idKey, head, dependencyForIdKey);
            Cache.Insert(pathKey, head, dependencyForPathKey);
        }
        internal static void CacheNodeData(NodeData nodeData, string cacheKey = null)
        {
            if (nodeData == null)
                throw new ArgumentNullException(nameof(nodeData));
            if (cacheKey == null)
                cacheKey = GenerateNodeDataVersionIdCacheKey(nodeData.VersionId);
            var dependency = CacheDependencyFactory.CreateNodeDataDependency(nodeData);
            Cache.Insert(cacheKey, nodeData, dependency);
        }

        internal static void RemoveFromCache(NodeData data)
        {
            // Remove items from Cache by the OriginalPath, before getting an update
            // of a - occassionally differring - path from the database
            if (data.PathChanged)
            {
                PathDependency.FireChanged(data.OriginalPath);
            }

            if (data.ContentListTypeId != 0 && data.ContentListId == 0)
            {
                // If list, invalidate full subtree
                PathDependency.FireChanged(data.Path);
            }
            else
            {
                // If not a list, invalidate item
                NodeIdDependency.FireChanged(data.Id);
            }
        }
        /// <summary>
        /// Removes the node data from the cache by its version id.
        /// </summary>
        /// <param name="versionId">Version id.</param>
        public static void RemoveNodeDataFromCacheByVersionId(int versionId)
        {
            Cache.Remove(GenerateNodeDataVersionIdCacheKey(versionId));
        }
    }
}