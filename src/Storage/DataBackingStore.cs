using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.ContentRepository.Storage.Schema;
using System.IO;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using System.Diagnostics;
using SenseNet.ContentRepository.Storage.Security;
using System.Globalization;
using SenseNet.Configuration;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search;
using SenseNet.Security;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Storage
{
    public static class DataBackingStore
    {
        private static object _indexDocumentProvider_Sync = new object();
        private static IIndexDocumentProvider __indexDocumentProvider;
        private static IIndexDocumentProvider IndexDocumentProvider
        {
            get
            {
                if (__indexDocumentProvider == null)
                {
                    lock (_indexDocumentProvider_Sync)
                    {
                        if (__indexDocumentProvider == null)
                        {
                            var types = TypeResolver.GetTypesByInterface(typeof(IIndexDocumentProvider));
                            if (types.Length != 1)
                                throw new ApplicationException("More than one IIndexDocumentProvider");
                            __indexDocumentProvider = Activator.CreateInstance(types[0]) as IIndexDocumentProvider;
                        }
                    }
                }
                return __indexDocumentProvider;
            }
        }

        // ====================================================================== Get NodeHead

        internal static NodeHead GetNodeHead(int nodeId)
        {
            if (!CanExistInDatabase(nodeId))
                return null;

            string idKey = CreateNodeHeadIdCacheKey(nodeId);
            NodeHead item = (NodeHead)DistributedApplication.Cache.Get(idKey);

            if (item == null)
            {
                item = DataProvider.Current.LoadNodeHead(nodeId);
                if (item != null)
                    CacheNodeHead(item, idKey, CreateNodeHeadPathCacheKey(item.Path));
            }

            return item;
        }
        internal static NodeHead GetNodeHead(string path)
        {
            if (!CanExistInDatabase(path))
                return null;

            string pathKey = CreateNodeHeadPathCacheKey(path);
            NodeHead item = (NodeHead)DistributedApplication.Cache.Get(pathKey);

            if (item == null)
            {
                Debug.WriteLine("#GetNodeHead from db: " + path);
                item = DataProvider.Current.LoadNodeHead(path);
                if (item != null)
                    CacheNodeHead(item, CreateNodeHeadIdCacheKey(item.Id), pathKey);
            }
            return item;
        }
        internal static IEnumerable<NodeHead> GetNodeHeads(IEnumerable<int> idArray)
        {
            var nodeHeads = new List<NodeHead>();
            var unloadHeads = new List<int>();
            foreach (var id in idArray)
            {
                string idKey = CreateNodeHeadIdCacheKey(id);
                var item = (NodeHead)DistributedApplication.Cache.Get(idKey);
                if (item == null)
                    unloadHeads.Add(id);
                else
                    nodeHeads.Add(item);
            }

            if (unloadHeads.Count > 0)
            {
                foreach (var head in DataProvider.Current.LoadNodeHeads(unloadHeads))
                {
                    if (head != null)
                        CacheNodeHead(head, CreateNodeHeadIdCacheKey(head.Id), CreateNodeHeadPathCacheKey(head.Path));
                    nodeHeads.Add(head);
                }

                // sort the node heads aligned with the original list
                nodeHeads = (from id in idArray
                    join head in nodeHeads.Where(h => h != null)
                    on id equals head.Id
                    where head != null
                    select head).ToList();
            }
            return nodeHeads;
        }
        internal static NodeHead GetNodeHeadByVersionId(int versionId)
        {
            return DataProvider.Current.LoadNodeHeadByVersionId(versionId);
        }

        internal static void CacheNodeHead(NodeHead nodeHead)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");

            var idKey = CreateNodeHeadIdCacheKey(nodeHead.Id);
            var item = (NodeHead)DistributedApplication.Cache.Get(idKey);

            if (item != null)
                return;

            CacheNodeHead(nodeHead, idKey, CreateNodeHeadPathCacheKey(nodeHead.Path));
        }
        internal static void CacheNodeHead(NodeHead head, string idKey, string pathKey)
        {
            var dependencyForPathKey = CacheDependencyFactory.CreateNodeHeadDependency(head);
            var dependencyForIdKey = CacheDependencyFactory.CreateNodeHeadDependency(head);
            DistributedApplication.Cache.Insert(idKey, head, dependencyForIdKey);
            DistributedApplication.Cache.Insert(pathKey, head, dependencyForPathKey);
        }

        internal static bool NodeExists(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            if (!CanExistInDatabase(path))
                return false;

            // Look at the cache first
            var pathKey = DataBackingStore.CreateNodeHeadPathCacheKey(path);
            if (DistributedApplication.Cache.Get(pathKey) as NodeHead != null)
                return true;

            // If it wasn't in the cache, check the database
            return DataProvider.NodeExists(path);
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

        // ====================================================================== Get Versions

        internal static NodeHead.NodeVersion[] GetNodeVersions(int nodeId)
        {
            return DataProvider.Current.GetNodeVersions(nodeId);
        }

        // ====================================================================== Get NodeData

        internal static NodeToken GetNodeData(NodeHead head, int versionId)
        {
            int listId = head.ContentListId;
            int listTypeId = head.ContentListTypeId;

            var cacheKey = GenerateNodeDataVersionIdCacheKey(versionId);
            var nodeData = DistributedApplication.Cache.Get(cacheKey) as NodeData;

            NodeToken token = new NodeToken(head.Id, head.NodeTypeId, listId, listTypeId, versionId, null);
            token.NodeHead = head;
            if (nodeData == null)
            {
                DataProvider.Current.LoadNodeData(new NodeToken[] { token });
                nodeData = token.NodeData;
                if (nodeData != null) // lost version
                    CacheNodeData(nodeData, cacheKey);
            }
            else
            {
                token.NodeData = nodeData;
            }
            return token;
        }
        internal static NodeToken[] GetNodeData(NodeHead[] headArray, int[] versionIdArray)
        {
            var tokens = new List<NodeToken>();
            var tokensToLoad = new List<NodeToken>();
            for (var i = 0; i < headArray.Length; i++)
            {
                var head = headArray[i];
                var versionId = versionIdArray[i];

                int listId = head.ContentListId;
                int listTypeId = head.ContentListTypeId;

                NodeToken token = new NodeToken(head.Id, head.NodeTypeId, listId, listTypeId, versionId, null);
                token.NodeHead = head;
                tokens.Add(token);

                var cacheKey = GenerateNodeDataVersionIdCacheKey(versionId);
                var nodeData = DistributedApplication.Cache.Get(cacheKey) as NodeData;

                if (nodeData == null)
                    tokensToLoad.Add(token);
                else
                    token.NodeData = nodeData;
            }
            if (tokensToLoad.Count > 0)
            {
                DataProvider.Current.LoadNodeData(tokensToLoad);
                foreach (var token in tokensToLoad)
                {
                    var nodeData = token.NodeData;
                    if (nodeData != null) // lost version
                        CacheNodeData(nodeData);
                }
            }
            return tokens.ToArray();
        }
        // when create new
        internal static NodeData CreateNewNodeData(Node parent, NodeType nodeType, ContentListType listType, int listId)
        {
            var listTypeId = listType == null ? 0 : listType.Id;
            var parentId = parent == null ? 0 : parent.Id;
            var userId = AccessProvider.Current.GetOriginalUser().Id;
            var now = DataProvider.Current.RoundDateTime(DateTime.UtcNow);
            var name = String.Concat(nodeType.Name, "-", now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));
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
                LockDate = DataProvider.Current.DateTimeMinValue,
                LockToken = null,
                LastLockUpdate = DataProvider.Current.DateTimeMinValue,

                //TODO: IsSystem

                SavingState = default(ContentSavingState),
                ChangedData = null
            };
            privateData.VersionModificationDateChanged = false;
            privateData.VersionModifiedByIdChanged = false;
            privateData.ModificationDateChanged = false;
            privateData.ModifiedByIdChanged = false;
            return privateData;
        }

        internal static void CacheNodeData(NodeData nodeData, string cacheKey = null)
        {
            if (nodeData == null)
                throw new ArgumentNullException("nodeData");
            if (cacheKey == null)
                cacheKey = GenerateNodeDataVersionIdCacheKey(nodeData.VersionId);
            var dependency = CacheDependencyFactory.CreateNodeDataDependency(nodeData);
            DistributedApplication.Cache.Insert(cacheKey, nodeData, dependency);
        }
        public static bool IsInCache(NodeData nodeData) // for tests
        {
            if (nodeData == null)
                throw new ArgumentNullException("nodeData");
            var cacheKey = GenerateNodeDataVersionIdCacheKey(nodeData.VersionId);
            return DistributedApplication.Cache.Get(cacheKey) as NodeData != null;
        }

        public static void RemoveNodeDataFromCacheByVersionId(int versionId)
        {
            DistributedApplication.Cache.Remove(GenerateNodeDataVersionIdCacheKey(versionId));
        }

        // ====================================================================== 

        internal static object LoadProperty(int versionId, PropertyType propertyType)
        {
            if (propertyType.DataType == DataType.Text)
                return DataProvider.Current.LoadTextPropertyValue(versionId, propertyType.Id);
            if (propertyType.DataType == DataType.Binary)
                return DataProvider.Current.LoadBinaryPropertyValue(versionId, propertyType.Id);
            return propertyType.DefaultValue;
        }
        internal static Stream GetBinaryStream(int nodeId, int versionId, int propertyTypeId)
        {
            BinaryCacheEntity binaryCacheEntity = null;

            if (TransactionScope.IsActive)
            {
                binaryCacheEntity = DataProvider.Current.LoadBinaryCacheEntity(versionId, propertyTypeId);
                return new SnStream(binaryCacheEntity.Context, binaryCacheEntity.RawData);
            }

            // Try to load cached binary entity
            var cacheKey = BinaryCacheEntity.GetCacheKey(versionId, propertyTypeId);
            binaryCacheEntity = (BinaryCacheEntity)DistributedApplication.Cache.Get(cacheKey);
            if (binaryCacheEntity == null)
            {
                // Not in cache, load it from the database
                binaryCacheEntity = DataProvider.Current.LoadBinaryCacheEntity(versionId, propertyTypeId);

                // insert the binary cache entity into the 
                // cache only if we know the node id
                if (binaryCacheEntity != null && nodeId != 0)
                {
                    if (!RepositoryEnvironment.WorkingMode.Populating)
                        DistributedApplication.Cache.Insert(cacheKey, binaryCacheEntity, new NodeIdDependency(nodeId));
                }
            }

            // Not found even in the database
            if (binaryCacheEntity == null)
                return null;
            if (binaryCacheEntity.Length == -1)
                return null;
            return new SnStream(binaryCacheEntity.Context, binaryCacheEntity.RawData);
        }
        internal static Dictionary<int, string> LoadTextProperties(int versionId, int[] notLoadedPropertyTypeIds)
        {
            return DataProvider.Current.LoadTextPropertyValues(versionId, notLoadedPropertyTypeIds);
        }

        // ====================================================================== Transaction callback

        internal static void RemoveFromCache(NodeDataParticipant participant) // orig name: OnNodeDataCommit
        {
            // Do not fire any events if the node is new: 
            // it cannot effect any other content
            if (participant.IsNewNode)
                return;

            var data = participant.Data;

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
        internal static void OnNodeDataRollback(NodeDataParticipant participant)
        {
            participant.Data.Rollback();
        }

        // ====================================================================== Cache Key factory

        private static readonly string NODE_HEAD_PREFIX = "NodeHeadCache.";
        private static readonly string NODE_DATA_PREFIX = "NodeData.";

        public static string CreateNodeHeadPathCacheKey(string path)
        {
            return string.Concat(NODE_HEAD_PREFIX, path.ToLowerInvariant());
        }
        public static string CreateNodeHeadIdCacheKey(int nodeId)
        {
            return string.Concat(NODE_HEAD_PREFIX, nodeId);
        }
        internal static string GenerateNodeDataVersionIdCacheKey(int versionId)
        {
            return string.Concat(NODE_DATA_PREFIX, versionId);
        }

        // ====================================================================== Save Nodedata

        private const int maxDeadlockIterations = 3;
        private const int sleepIfDeadlock = 1000;

        internal static void SaveNodeData(Node node, NodeSaveSettings settings, IIndexPopulator populator, string originalPath, string newPath)
        {
            var isNewNode = node.Id == 0;
            var isOwnerChanged = node.Data.IsPropertyChanged("OwnerId");
            if (!isNewNode && isOwnerChanged)
                node.Security.Assert(PermissionType.TakeOwnership);

            var data = node.Data;
            var attempt = 0;

            using (var op = SnTrace.Database.StartOperation("SaveNodeData"))
            {
                while (true)
                {
                    attempt++;

                    var deadlockException = SaveNodeDataTransactional(node, settings, populator, originalPath, newPath);
                    if (deadlockException == null)
                        break;

                    SnTrace.Database.Write("DEADLOCK detected. Attempt: {0}/{1}, NodeId:{2}, Version:{3}, Path:{4}",
                        attempt, maxDeadlockIterations, node.Id, node.Version, node.Path);

                    if (attempt >= maxDeadlockIterations)
                        throw new Exception(string.Format("Error saving node. Id: {0}, Path: {1}", node.Id, node.Path), deadlockException);

                    SnLog.WriteWarning("Deadlock detected in SaveNodeData", properties:
                        new Dictionary<string, object>
                        {
                            {"Id: ", node.Id},
                            {"Path: ", node.Path},
                            {"Version: ", node.Version},
                            {"Attempt: ", attempt}
                        });

                    System.Threading.Thread.Sleep(sleepIfDeadlock);
                }
                op.Successful = true;
            }

            try
            {
                if (isNewNode)
                {
                    SecurityHandler.CreateSecurityEntity(node.Id, node.ParentId, node.OwnerId);
                }
                else if (isOwnerChanged)
                {
                    SecurityHandler.ModifyEntityOwner(node.Id, node.OwnerId);
                }
            }
            catch (EntityNotFoundException e)
            {
                SnLog.WriteException(e, $"Error during creating or modifying security entity: {node.Id}. Original message: {e}",
                    EventId.Security);
            }
            catch (SecurityStructureException) // suppressed
            {
                // no need to log this: somebody else already created or modified this security entity
            }

            if (isNewNode)
                SnTrace.ContentOperation.Write("Node created. Id:{0}, Path:{1}", data.Id, data.Path);
            else
                SnTrace.ContentOperation.Write("Node updated. Id:{0}, Path:{1}", data.Id, data.Path);
        }
        private static Exception SaveNodeDataTransactional(Node node, NodeSaveSettings settings, IIndexPopulator populator, string originalPath, string newPath)
        {
            IndexDocumentData indexDocument = null;
            bool hasBinary = false;

            var data = node.Data;
            var isNewNode = data.Id == 0;
            NodeDataParticipant participant = null;

            var msg = "Saving Node#" + node.Id + ", " + node.ParentPath + "/" + node.Name;
            var isLocalTransaction = !TransactionScope.IsActive;

            using (var op = SnTrace.Database.StartOperation(msg))
            {
                if (isLocalTransaction)
                    TransactionScope.Begin();

                try
                {
                    // collect data for populator
                    var populatorData = populator.BeginPopulateNode(node, settings, originalPath, newPath);

                    data.CreateSnapshotData();

                    participant = new NodeDataParticipant { Data = data, Settings = settings, IsNewNode = isNewNode };

                    TransactionScope.Participate(participant);

                    if (settings.NodeHead != null)
                    {
                        settings.LastMajorVersionIdBefore = settings.NodeHead.LastMajorVersionId;
                        settings.LastMinorVersionIdBefore = settings.NodeHead.LastMinorVersionId;
                    }

                    int lastMajorVersionId, lastMinorVersionId;
                    DataProvider.Current.SaveNodeData(data, settings, out lastMajorVersionId, out lastMinorVersionId);

                    settings.LastMajorVersionIdAfter = lastMajorVersionId;
                    settings.LastMinorVersionIdAfter = lastMinorVersionId;

                    // here we re-create the node head to insert it into the cache and refresh the version info);
                    if (lastMajorVersionId > 0 || lastMinorVersionId > 0)
                    {
                        var head = NodeHead.CreateFromNode(node, lastMinorVersionId, lastMajorVersionId);
                        if (MustCache(node.NodeType))
                        {
                            // participate cache items
                            var idKey = CreateNodeHeadIdCacheKey(head.Id);
                            var participant2 = new InsertCacheParticipant { CacheKey = idKey };
                            TransactionScope.Participate(participant2);
                            var pathKey = CreateNodeHeadPathCacheKey(head.Path);
                            var participant3 = new InsertCacheParticipant { CacheKey = pathKey };
                            TransactionScope.Participate(participant3);

                            CacheNodeHead(head, idKey, pathKey);
                        }

                        node.RefreshVersionInfo(head);

                        if (!settings.DeletableVersionIds.Contains(node.VersionId))
                        {
                            // Elevation: we need to create the index document with full
                            // control to avoid field access errors (indexing must be independent
                            // from the current users permissions).
                            using (new SystemAccount())
                            {
                                indexDocument = SaveIndexDocument(node, true, isNewNode, out hasBinary);
                            }
                        }
                    }

                    if (isLocalTransaction)
                        TransactionScope.Commit();

                    // populate index only if it is enabled on this content (e.g. preview images will be skipped)
                    using (var op2 = SnTrace.Index.StartOperation("Indexing node"))
                    {
                        if (node.IsIndexingEnabled)
                        {
                            using (new SystemAccount())
                                populator.CommitPopulateNode(populatorData, indexDocument);
                        }

                        if (indexDocument != null && hasBinary)
                        {
                            using (new SystemAccount())
                            {
                                indexDocument = SaveIndexDocument(node, indexDocument);
                                populator.FinalizeTextExtracting(populatorData, indexDocument);
                            }
                        }
                        op2.Successful = true;
                    }
                }
                catch (NodeIsOutOfDateException)
                {
                    RemoveFromCache(participant);
                    throw;
                }
                catch (System.Data.Common.DbException dbe)
                {
                    if (isLocalTransaction && IsDeadlockException(dbe))
                        return dbe;
                    throw SavingExceptionHelper(data, dbe);
                }
                catch (Exception e)
                {
                    var ee = SavingExceptionHelper(data, e);
                    if (ee == e)
                        throw;
                    else
                        throw ee;
                }
                finally
                {
                    if (isLocalTransaction && TransactionScope.IsActive)
                        TransactionScope.Rollback();
                }
                op.Successful = true;
            }
            return null;
        }

        private static bool MustCache(NodeType nodeType)
        {
            if (Cache.CacheContentAfterSaveMode != Cache.CacheContentAfterSaveOption.Containers)
                return Cache.CacheContentAfterSaveMode == Cache.CacheContentAfterSaveOption.All;
            
            var type = TypeResolver.GetType(nodeType.ClassName);
            return typeof(IFolder).IsAssignableFrom(type);
        }
        private static bool IsDeadlockException(System.Data.Common.DbException e)
        {
            // Avoid [SqlException (0x80131904): Transaction (Process ID ??) was deadlocked on lock resources with another process and has been chosen as the deadlock victim. Rerun the transaction.
            // CAUTION: Using e.ErrorCode and testing for HRESULT 0x80131904 will not work! you should use e.Number not e.ErrorCode
            var sqlEx = e as System.Data.SqlClient.SqlException;
            if (sqlEx == null)
                return false;
            var sqlExNumber = sqlEx.Number;
            var sqlExErrorCode = sqlEx.ErrorCode;
            var isDeadLock = sqlExNumber == 1205;
            // assert
            var messageParts = new[]
                                   {
                                       "was deadlocked on lock",
                                       "resources with another process and has been chosen as the deadlock victim. rerun the transaction"
                                   };
            var currentMessage = e.Message.ToLower();
            var isMessageDeadlock = !messageParts.Where(msgPart => !currentMessage.Contains(msgPart)).Any();

            if (sqlEx != null && isMessageDeadlock != isDeadLock)
                throw new Exception(String.Concat("Incorrect deadlock analysis",
                    ". Number: ", sqlExNumber,
                    ". ErrorCode: ", sqlExErrorCode,
                    ". Errors.Count: ", sqlEx.Errors.Count,
                    ". Original message: ", e.Message), e);
            return isDeadLock;
        }
        private static Exception SavingExceptionHelper(NodeData data, Exception catchedEx)
        {
            var message = "The content cannot be saved.";
            if (catchedEx.Message.StartsWith("Cannot insert duplicate key"))
            {
                message += " A content with the name you specified already exists.";

                var appExc = new NodeAlreadyExistsException(message, catchedEx); // new ApplicationException(message, catchedEx);
                appExc.Data.Add("NodeId", data.Id);
                appExc.Data.Add("Path", data.Path);
                appExc.Data.Add("OriginalPath", data.OriginalPath);

                appExc.Data.Add("ErrorCode", "ExistingNode");
                return appExc;
            }
            return catchedEx;
        }

        // ====================================================================== Index document save / load operations

        public static IndexDocumentData SaveIndexDocument(Node node, bool skipBinaries, bool isNew, out bool hasBinary)
        {
            if (node.Id == 0)
                throw new NotSupportedException("Cannot save the indexing information before node is not saved.");

            node.MakePrivateData(); // this is important because version timestamp will be changed.

            var doc = IndexDocumentProvider.GetIndexDocument(node, skipBinaries, isNew, out hasBinary);
            long? docSize = null;
            byte[] bytes;
            if (doc != null)
            {
                using (var docStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    formatter.Serialize(docStream, doc);
                    docStream.Flush();
                    docStream.Position = 0;
                    docSize = docStream.Length;
                    bytes = docStream.GetBuffer();
                    DataProvider.SaveIndexDocument(node.Data, bytes);
                }
            }
            else
            {
                bytes = new byte[0];
            }
            return CreateIndexDocumentData(node, doc, bytes, docSize);
        }
        public static IndexDocumentData SaveIndexDocument(Node node, IndexDocumentData indexDocumentData)
        {
            if (node.Id == 0)
                throw new NotSupportedException("Cannot save the indexing information before node is not saved.");

            node.MakePrivateData(); // this is important because version timestamp will be changed.

            var completedDocument = IndexDocumentProvider.CompleteIndexDocument(node, indexDocumentData.IndexDocument);

            long? docSize = null;
            byte[] bytes;
            if (completedDocument != null)
            {
                using (var docStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    formatter.Serialize(docStream, completedDocument);
                    docStream.Flush();
                    docStream.Position = 0;
                    docSize = docStream.Length;
                    bytes = docStream.GetBuffer();
                    DataProvider.SaveIndexDocument(node.Data, bytes);
                }
            }
            else
            {
                bytes = new byte[0];
            }
            return CreateIndexDocumentData(node, completedDocument, bytes, docSize);
        }

        internal static IndexDocumentData CreateIndexDocumentData(Node node, IndexDocument indexDocument, byte[] serializedIndexDocument, long? indexDocumentSize)
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
                IndexDocumentSize = indexDocumentSize,
                NodeTimestamp = node.NodeTimestamp,
                VersionTimestamp = node.VersionTimestamp
            };
        }
    }
}
