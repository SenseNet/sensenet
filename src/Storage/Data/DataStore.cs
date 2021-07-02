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
using SenseNet.Data;
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
    public class DataStore : IDataStore
    {
        /// <summary>
        /// Default size limit for preloading and caching long text values.
        /// </summary>
        public int TextAlternationSizeLimit => DataProvider.TextAlternationSizeLimit;

        /// <summary>
        /// Gets the current DataProvider instance.
        /// </summary>
        //public DataProvider DataProvider => Providers.Instance.DataProvider;
        public DataProvider DataProvider { get; }

        public int PathMaxLength => DataProvider.PathMaxLength;
        public DateTime DateTimeMinValue => DataProvider.DateTimeMinValue;
        public DateTime DateTimeMaxValue => DataProvider.DateTimeMaxValue;
        public decimal DecimalMinValue => DataProvider.DecimalMinValue;
        public decimal DecimalMaxValue => DataProvider.DecimalMaxValue;

        public DataStore(DataProvider dataProvider)
        {
            DataProvider = dataProvider;
        }

        [Obsolete("Use DataProvider.GetExtension method.", true)]
        public T GetDataProviderExtension<T>() where T : class, IDataProviderExtension
        {
            return DataProvider.GetExtension<T>();
        }
        [Obsolete("Use DataProvider.SetExtension method", true)]
        public void SetDataProviderExtension(Type providerType, IDataProviderExtension provider)
        {
            DataProvider.SetExtension(providerType, provider);
        }

        public void Reset()
        {
            DataProvider.Reset();
        }

        /* =============================================================================================== Installation */

        public Task InstallInitialDataAsync(InitialData data, CancellationToken cancellationToken)
        {
            return DataProvider.InstallInitialDataAsync(data, cancellationToken);
        }

        public Task<IEnumerable<EntityTreeNodeData>> LoadEntityTreeAsync(CancellationToken cancellationToken)
        {
            return DataProvider.LoadEntityTreeAsync(cancellationToken);
        }

        public Task<bool> IsDatabaseReadyAsync(CancellationToken cancellationToken)
        {
            return DataProvider.IsDatabaseReadyAsync(cancellationToken);
        }

        public async Task InstallDatabaseAsync(InitialData initialData, CancellationToken cancellationToken)
        {
            await DataProvider.InstallDatabaseAsync(cancellationToken).ConfigureAwait(false);
            await InstallInitialDataAsync(initialData ?? InitialData.Load(new SenseNetServicesInitialData(), null),
                cancellationToken).ConfigureAwait(false);
        }

        /* =============================================================================================== Nodes */

        public async Task<NodeHead> SaveNodeAsync(NodeData nodeData, NodeSaveSettings settings, CancellationToken cancellationToken)
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
                throw DataProvider.GetRealException(e);
            }
        }
        private bool MustCache(int nodeTypeId)
        {
            var nodeType = ActiveSchema.NodeTypes.GetItemById(nodeTypeId);
            if (CacheConfiguration.CacheContentAfterSaveMode != CacheConfiguration.CacheContentAfterSaveOption.Containers)
                return CacheConfiguration.CacheContentAfterSaveMode == CacheConfiguration.CacheContentAfterSaveOption.All;
            return nodeType.IsInstaceOfOrDerivedFrom(NodeType.GetByName("Folder"));
        }

        public async Task<NodeToken> LoadNodeAsync(NodeHead head, int versionId, CancellationToken cancellationToken)
        {
            return (await LoadNodesAsync(new[] {head}, new[] {versionId}, cancellationToken).ConfigureAwait(false))
                .FirstOrDefault();
        }
        public async Task<NodeToken[]> LoadNodesAsync(NodeHead[] headArray, int[] versionIdArray, CancellationToken cancellationToken)
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

                    var cacheKey = CreateNodeDataVersionIdCacheKey(versionId);
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
                throw DataProvider.GetRealException(e);
            }
        }
        public Task DeleteNodeAsync(NodeHead nodeHead, CancellationToken cancellationToken)
        {
            return DataProvider.DeleteNodeAsync(nodeHead.GetNodeHeadData(), cancellationToken);
        }
        public Task DeleteNodeAsync(NodeData nodeData, CancellationToken cancellationToken)
        {
            return DataProvider.DeleteNodeAsync(nodeData.GetNodeHeadData(), cancellationToken);
        }
        public async Task MoveNodeAsync(NodeHead sourceNodeHead, int targetNodeId, CancellationToken cancellationToken)
        {
            var sourceNodeHeadData = sourceNodeHead.GetNodeHeadData();
            await DataProvider.MoveNodeAsync(sourceNodeHeadData, targetNodeId, cancellationToken).ConfigureAwait(false);
            sourceNodeHead.Timestamp = sourceNodeHeadData.Timestamp;
        }
        public async Task MoveNodeAsync(NodeData sourceNodeData, int targetNodeId, CancellationToken cancellationToken)
        {
            var sourceNodeHeadData = sourceNodeData.GetNodeHeadData();
            await DataProvider.MoveNodeAsync(sourceNodeHeadData, targetNodeId, cancellationToken).ConfigureAwait(false);
            if (!sourceNodeData.IsShared)
                sourceNodeData.NodeTimestamp = sourceNodeHeadData.Timestamp;
        }

        public Task<Dictionary<int, string>> LoadTextPropertyValuesAsync(int versionId, int[] propertiesToLoad, CancellationToken cancellationToken)
        {
            return DataProvider.LoadTextPropertyValuesAsync(versionId, propertiesToLoad, cancellationToken);
        }
        public Task<BinaryDataValue> LoadBinaryPropertyValueAsync(int versionId, int propertyTypeId, CancellationToken cancellationToken)
        {
            return DataProvider.LoadBinaryPropertyValueAsync(versionId, propertyTypeId, cancellationToken);
        }

        public async Task<bool> NodeExistsAsync(string path, CancellationToken cancellationToken)
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

        public NodeData CreateNewNodeData(Node parent, NodeType nodeType, ContentListType listType, int listId)
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

        public Stream GetBinaryStream(int nodeId, int versionId, int propertyTypeId)
        {
            // Try to load cached binary entity
            var cacheKey = BinaryCacheEntity.GetCacheKey(versionId, propertyTypeId);
            var binaryCacheEntity = (BinaryCacheEntity)Cache.Get(cacheKey);
            if (binaryCacheEntity == null)
            {
                //TODO: [DIBLOB] get the storage service through the constructor later

                // Not in cache, load it from the database
                binaryCacheEntity = Providers.Instance.BlobStorage.LoadBinaryCacheEntityAsync(versionId, propertyTypeId, CancellationToken.None)
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

            return new SnStream(binaryCacheEntity.Context, Providers.Instance.BlobStorage, binaryCacheEntity.RawData);
        }

        /* =============================================================================================== NodeHead */

        public async Task<NodeHead> LoadNodeHeadAsync(string path, CancellationToken cancellationToken)
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
        public async Task<NodeHead> LoadNodeHeadAsync(int nodeId, CancellationToken cancellationToken)
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
        public Task<NodeHead> LoadNodeHeadByVersionIdAsync(int versionId, CancellationToken cancellationToken)
        {
            return DataProvider.LoadNodeHeadByVersionIdAsync(versionId, cancellationToken);
        }
        public async Task<IEnumerable<NodeHead>> LoadNodeHeadsAsync(IEnumerable<int> nodeIds, CancellationToken cancellationToken)
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

        public Task<IEnumerable<NodeHead.NodeVersion>> GetVersionNumbersAsync(int nodeId, CancellationToken cancellationToken)
        {
            return DataProvider.GetVersionNumbersAsync(nodeId, cancellationToken);
        }
        public Task<IEnumerable<NodeHead.NodeVersion>> GetVersionNumbersAsync(string path, CancellationToken cancellationToken)
        {
            return DataProvider.GetVersionNumbersAsync(path, cancellationToken);
        }

        public Task<IEnumerable<NodeHead>> LoadNodeHeadsFromPredefinedSubTreesAsync(IEnumerable<string> paths, bool resolveAll, bool resolveChildren, CancellationToken cancellationToken)
        {
            return DataProvider.LoadNodeHeadsFromPredefinedSubTreesAsync(paths, resolveAll, resolveChildren, cancellationToken);
        }

        /* =============================================================================================== NodeQuery */

        public Task<int> InstanceCountAsync(int[] nodeTypeIds, CancellationToken cancellationToken)
        {
            return DataProvider.InstanceCountAsync(nodeTypeIds, cancellationToken);
        }
        public Task<IEnumerable<int>> GetChildrenIdentifiersAsync(int parentId, CancellationToken cancellationToken)
        {
            return DataProvider.GetChildrenIdentifiersAsync(parentId, cancellationToken);
        }
        public Task<IEnumerable<int>> QueryNodesByPathAsync(string pathStart, bool orderByPath, CancellationToken cancellationToken)
        {
            return QueryNodesByTypeAndPathAsync(null, pathStart, orderByPath, cancellationToken);
        }
        public Task<IEnumerable<int>> QueryNodesByTypeAsync(int[] nodeTypeIds, CancellationToken cancellationToken)
        {
            return QueryNodesByTypeAndPathAsync(nodeTypeIds, new string[0], false, cancellationToken);
        }
        public Task<IEnumerable<int>> QueryNodesByTypeAndPathAsync(int[] nodeTypeIds, string pathStart, bool orderByPath, CancellationToken cancellationToken)
        {
            return QueryNodesByTypeAndPathAndNameAsync(nodeTypeIds, pathStart, orderByPath, null, cancellationToken);
        }
        public Task<IEnumerable<int>> QueryNodesByTypeAndPathAsync(int[] nodeTypeIds, string[] pathStart, bool orderByPath, CancellationToken cancellationToken)
        {
            return QueryNodesByTypeAndPathAndNameAsync(nodeTypeIds, pathStart, orderByPath, null, cancellationToken);
        }
        public Task<IEnumerable<int>> QueryNodesByTypeAndPathAndNameAsync(int[] nodeTypeIds, string pathStart, bool orderByPath, string name, CancellationToken cancellationToken)
        {
            return QueryNodesByTypeAndPathAndNameAsync(nodeTypeIds, new[] { pathStart }, orderByPath, name, cancellationToken);
        }
        public Task<IEnumerable<int>> QueryNodesByTypeAndPathAndNameAsync(int[] nodeTypeIds, string[] pathStart, bool orderByPath, string name, CancellationToken cancellationToken)
        {
            return DataProvider.QueryNodesByTypeAndPathAndNameAsync(nodeTypeIds, pathStart, orderByPath, name, cancellationToken);
        }

        public Task<IEnumerable<int>> QueryNodesByTypeAndPathAndPropertyAsync(int[] nodeTypeIds, string pathStart, bool orderByPath, List<QueryPropertyData> properties, CancellationToken cancellationToken)
        {
            return DataProvider.QueryNodesByTypeAndPathAndPropertyAsync(nodeTypeIds, pathStart, orderByPath, properties, cancellationToken);
        }

        public Task<IEnumerable<int>> QueryNodesByReferenceAndTypeAsync(string referenceName, int referredNodeId, int[] nodeTypeIds, CancellationToken cancellationToken)
        {
            return DataProvider.QueryNodesByReferenceAndTypeAsync(referenceName, referredNodeId, nodeTypeIds, cancellationToken);
        }

        /* =============================================================================================== Tree */

        public Task<IEnumerable<NodeType>> LoadChildTypesToAllowAsync(int nodeId, CancellationToken cancellationToken)
        {
            return DataProvider.LoadChildTypesToAllowAsync(nodeId, cancellationToken);
        }
        public Task<List<ContentListType>> GetContentListTypesInTreeAsync(string path, CancellationToken cancellationToken)
        {
            return DataProvider.GetContentListTypesInTreeAsync(path, cancellationToken);
        }

        /* =============================================================================================== TreeLock */

        public Task<int> AcquireTreeLockAsync(string path, CancellationToken cancellationToken)
        {
            return DataProvider.AcquireTreeLockAsync(path, GetTreeLockTimeLimit(), cancellationToken);
        }
        public Task<bool> IsTreeLockedAsync(string path, CancellationToken cancellationToken)
        {
            return DataProvider.IsTreeLockedAsync(path, GetTreeLockTimeLimit(), cancellationToken);
        }

        public Task ReleaseTreeLockAsync(int[] lockIds, CancellationToken cancellationToken)
        {
            return DataProvider.ReleaseTreeLockAsync(lockIds, cancellationToken);
        }
        public Task<Dictionary<int, string>> LoadAllTreeLocksAsync(CancellationToken cancellationToken)
        {
            return DataProvider.LoadAllTreeLocksAsync(cancellationToken);
        }
        private DateTime GetTreeLockTimeLimit()
        {
            return DateTime.UtcNow.AddHours(-8.0);
        }

        /* =============================================================================================== IndexDocument */

        private IIndexDocumentProvider IndexDocumentProvider => Providers.Instance.IndexDocumentProvider;

        public async Task<SavingIndexDocumentDataResult> SaveIndexDocumentAsync(Node node, bool skipBinaries, bool isNew,
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

        public async Task<IndexDocumentData> SaveIndexDocumentAsync(Node node, IndexDocumentData indexDocumentData,
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
        public IndexDocumentData CreateIndexDocumentData(Node node, IndexDocument indexDocument, string serializedIndexDocument)
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
        private async Task SaveIndexDocumentAsync(NodeData nodeData, IndexDocument indexDoc, CancellationToken cancellationToken)
        {
            var timestamp = await SaveIndexDocumentAsync(nodeData.VersionId, indexDoc, cancellationToken).ConfigureAwait(false);
            if (timestamp != 0)
                nodeData.VersionTimestamp = timestamp;
        }

        public async Task<long> SaveIndexDocumentAsync(int versionId, IndexDocument indexDoc, CancellationToken cancellationToken)
        {
            var serialized = indexDoc.Serialize();
            return await DataProvider.SaveIndexDocumentAsync(versionId, serialized, cancellationToken).ConfigureAwait(false);
        }

        /* ----------------------------------------------------------------------------------------------- Load IndexDocument */

        public async Task<IndexDocumentData> LoadIndexDocumentByVersionIdAsync(int versionId, CancellationToken cancellationToken)
        {
            var result = await DataProvider.LoadIndexDocumentsAsync(new []{versionId}, cancellationToken).ConfigureAwait(false);
            return result.FirstOrDefault();
        }
        public Task<IEnumerable<IndexDocumentData>> LoadIndexDocumentsAsync(IEnumerable<int> versionIds, CancellationToken cancellationToken)
        {
            return DataProvider.LoadIndexDocumentsAsync(versionIds, cancellationToken);
        }
        public IEnumerable<IndexDocumentData> LoadIndexDocumentsAsync(string path, int[] excludedNodeTypes)
        {
            return DataProvider.LoadIndexDocumentsAsync(path, excludedNodeTypes);
        }
        public Task<IEnumerable<int>> LoadNotIndexedNodeIdsAsync(int fromId, int toId, CancellationToken cancellationToken)
        {
            return DataProvider.LoadNotIndexedNodeIdsAsync(fromId, toId, cancellationToken);
        }

        /* =============================================================================================== IndexingActivity */

        public Task DeleteRestorePointsAsync(CancellationToken cancellationToken)
        {
            return DataProvider.DeleteRestorePointsAsync(cancellationToken);
        }

        public Task<IndexingActivityStatus> LoadCurrentIndexingActivityStatusAsync(CancellationToken cancellationToken)
        {
            return DataProvider.LoadCurrentIndexingActivityStatusAsync(cancellationToken);
        }
        public Task<IndexingActivityStatusRestoreResult> RestoreIndexingActivityStatusAsync(IndexingActivityStatus status, CancellationToken cancellationToken)
        {
            return DataProvider.RestoreIndexingActivityStatusAsync(status, cancellationToken);
        }

        public Task<int> GetLastIndexingActivityIdAsync(CancellationToken cancellationToken)
        {
            return DataProvider.GetLastIndexingActivityIdAsync(cancellationToken);
        }
        public Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int fromId, int toId, int count, bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory, CancellationToken cancellationToken)
        {
            return DataProvider.LoadIndexingActivitiesAsync(fromId, toId, count, executingUnprocessedActivities, activityFactory, cancellationToken);
        }

        public Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int[] gaps, bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory, CancellationToken cancellationToken)
        {
            return DataProvider.LoadIndexingActivitiesAsync(gaps, executingUnprocessedActivities, activityFactory, cancellationToken);
        }

        public Task<ExecutableIndexingActivitiesResult> LoadExecutableIndexingActivitiesAsync(IIndexingActivityFactory activityFactory, int maxCount, int runningTimeoutInSeconds, int[] waitingActivityIds, CancellationToken cancellationToken)
        {
            return DataProvider.LoadExecutableIndexingActivitiesAsync(activityFactory, maxCount, runningTimeoutInSeconds, waitingActivityIds, cancellationToken);
        }
        public Task RegisterIndexingActivityAsync(IIndexingActivity activity, CancellationToken cancellationToken)
        {
            return DataProvider.RegisterIndexingActivityAsync(activity, cancellationToken);
        }
        public Task UpdateIndexingActivityRunningStateAsync(int indexingActivityId, IndexingActivityRunningState runningState, CancellationToken cancellationToken)
        {
            return DataProvider.UpdateIndexingActivityRunningStateAsync(indexingActivityId, runningState, cancellationToken);
        }
        public Task RefreshIndexingActivityLockTimeAsync(int[] waitingIds, CancellationToken cancellationToken)
        {
            return DataProvider.RefreshIndexingActivityLockTimeAsync(waitingIds, cancellationToken);
        }
        public Task DeleteFinishedIndexingActivitiesAsync(CancellationToken cancellationToken)
        {
            return DataProvider.DeleteFinishedIndexingActivitiesAsync(cancellationToken);
        }
        public Task DeleteAllIndexingActivitiesAsync(CancellationToken cancellationToken)
        {
            return DataProvider.DeleteAllIndexingActivitiesAsync(cancellationToken);
        }

        /* =============================================================================================== Schema */

        public Task<RepositorySchemaData> LoadSchemaAsync(CancellationToken cancellationToken)
        {
            return DataProvider.LoadSchemaAsync(cancellationToken);
        }

        public Task<string> StartSchemaUpdateAsync(long schemaTimestamp, CancellationToken cancellationToken)
        {
            try
            {
                return DataProvider.StartSchemaUpdateAsync(schemaTimestamp, cancellationToken);
            }
            catch (Exception e)
            {
                throw DataProvider.GetRealException(e);
            }
        }
        public SchemaWriter CreateSchemaWriter()
        {
            return DataProvider.CreateSchemaWriter();
        }
        public Task<long> FinishSchemaUpdateAsync(string schemaLock, CancellationToken cancellationToken)
        {
            try
            {
                return DataProvider.FinishSchemaUpdateAsync(schemaLock, cancellationToken);
            }
            catch (Exception e)
            {
                throw DataProvider.GetRealException(e);
            }
        }

        /* =============================================================================================== Logging */

        public Task WriteAuditEventAsync(AuditEventInfo auditEvent, CancellationToken cancellationToken)
        {
            return DataProvider.WriteAuditEventAsync(auditEvent, cancellationToken);
        }

        public Task<IEnumerable<AuditLogEntry>> LoadLastAuditEventsAsync(int count,
            CancellationToken cancellationToken)
        {
            return DataProvider.LoadLastAuditEventsAsync(count, cancellationToken);
        }

        /* =============================================================================================== Tools */

        public DateTime RoundDateTime(DateTime d)
        {
            return DataProvider.RoundDateTime(d);
        }
        public bool IsCacheableText(string value)
        {
            return DataProvider.IsCacheableText(value);
        }
        public Task<string> GetNameOfLastNodeWithNameBaseAsync(int parentId, string namebase, string extension, CancellationToken cancellationToken)
        {
            return DataProvider.GetNameOfLastNodeWithNameBaseAsync(parentId, namebase, extension, cancellationToken);
        }
        public Task<long> GetTreeSizeAsync(string path, bool includeChildren, CancellationToken cancellationToken)
        {
            return DataProvider.GetTreeSizeAsync(path, includeChildren, cancellationToken);
        }
        public Task<int> GetNodeCountAsync(CancellationToken cancellationToken)
        {
            return GetNodeCountAsync(null, cancellationToken);
        }
        public Task<int> GetNodeCountAsync(string path, CancellationToken cancellationToken)
        {
            return DataProvider.GetNodeCountAsync(path, cancellationToken);
        }
        public Task<int> GetVersionCountAsync(CancellationToken cancellationToken)
        {
            return DataProvider.GetVersionCountAsync(null, cancellationToken);
        }
        public Task<int> GetVersionCountAsync(string path, CancellationToken cancellationToken)
        {
            return DataProvider.GetVersionCountAsync(path, cancellationToken);
        }

        private bool CanExistInDatabase(int id)
        {
            return id > 0;
        }
        private bool CanExistInDatabase(string path)
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

        /* =============================================================================================== */

        private readonly string NodeHeadPrefix = "NodeHeadCache.";
        private readonly string NodeDataPrefix = "NodeData.";
        private string CreateNodeHeadPathCacheKey(string path)
        {
            return string.Concat(NodeHeadPrefix, path.ToLowerInvariant());
        }
        private string CreateNodeHeadIdCacheKey(int nodeId)
        {
            return string.Concat(NodeHeadPrefix, nodeId);
        }
        public string CreateNodeDataVersionIdCacheKey(int versionId)
        {
            return string.Concat(NodeDataPrefix, versionId);
        }

        private void CacheNodeHead(NodeHead nodeHead)
        {
            var idKey = CreateNodeHeadIdCacheKey(nodeHead.Id);
            if (null != Cache.Get(idKey))
                return;
            CacheNodeHead(nodeHead, idKey, CreateNodeHeadPathCacheKey(nodeHead.Path));
        }
        private void CacheNodeHead(NodeHead head, string idKey, string pathKey)
        {
            var dependencyForPathKey = CacheDependencyFactory.CreateNodeHeadDependency(head);
            var dependencyForIdKey = CacheDependencyFactory.CreateNodeHeadDependency(head);
            Cache.Insert(idKey, head, dependencyForIdKey);
            Cache.Insert(pathKey, head, dependencyForPathKey);
        }
        public void CacheNodeData(NodeData nodeData, string cacheKey = null)
        {
            if (nodeData == null)
                throw new ArgumentNullException(nameof(nodeData));
            if (cacheKey == null)
                cacheKey = CreateNodeDataVersionIdCacheKey(nodeData.VersionId);
            var dependency = CacheDependencyFactory.CreateNodeDataDependency(nodeData);
            Cache.Insert(cacheKey, nodeData, dependency);
        }

        private void RemoveFromCache(NodeData data)
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
        public void RemoveNodeDataFromCacheByVersionId(int versionId)
        {
            Cache.Remove(CreateNodeDataVersionIdCacheKey(versionId));
        }
    }
}