using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using STT = System.Threading.Tasks;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Tests.Implementations
{
    //UNDONE:DB -------Delete original InMemoryDataProvider and use this. Move to the Tests project
    public class InMemoryDataProvider2 : DataProvider2
    {
        internal const int TextAlternationSizeLimit = 4000;

        // ReSharper disable once InconsistentNaming
        internal InMemoryDataBase2 DB = new InMemoryDataBase2();

        /* ============================================================================================================= Nodes */

        public override STT.Task InsertNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData)
        {
            //UNDONE:DB Lock? Transaction?

            var nodeId = DB.GetNextNodeId();
            nodeHeadData.NodeId = nodeId;

            var versionId = DB.GetNextVersionId();
            versionData.VersionId = versionId;
            dynamicData.VersionId = versionId;
            versionData.NodeId = nodeId;

            var versionDoc = CreateVersionDoc(versionData, dynamicData);
            DB.Versions[versionId] = versionDoc;
            versionData.Timestamp = versionDoc.Timestamp;

            foreach (var item in dynamicData.BinaryProperties)
                SaveBinaryProperty(item.Value, versionId, item.Key.Id, true, true);

            // Manage last versionIds and timestamps
            LoadLastVersionIds(nodeId, out var lastMajorVersionId, out var lastMinorVersionId);
            nodeHeadData.LastMajorVersionId = lastMajorVersionId;
            nodeHeadData.LastMinorVersionId = lastMinorVersionId;

            var nodeDoc = CreateNodeDoc(nodeHeadData);
            nodeDoc.LastMajorVersionId = lastMajorVersionId;
            nodeDoc.LastMinorVersionId = lastMinorVersionId;
            DB.Nodes[nodeId] = nodeDoc;
            nodeHeadData.Timestamp = nodeDoc.Timestamp;

            return STT.Task.CompletedTask;
        }

        public override STT.Task UpdateNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData, IEnumerable<int> versionIdsToDelete)
        {
            //UNDONE:DB Lock? Transaction?

            // Executes these:
            // INodeWriter: UpdateNodeRow(nodeData);
            // INodeWriter: UpdateVersionRow(nodeData, out lastMajorVersionId, out lastMinorVersionId);
            // DataProvider: private static void SaveNodeProperties(NodeData nodeData, SavingAlgorithm savingAlgorithm, INodeWriter writer, bool isNewNode)
            // DataProvider: protected internal abstract void DeleteVersion(int versionId, NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId);

            if (!DB.Nodes.TryGetValue(nodeHeadData.NodeId, out var nodeDoc))
                throw new Exception($"Cannot update a deleted Node. Id: {nodeHeadData.NodeId}, path: {nodeHeadData.Path}.");
            if (nodeDoc.Timestamp != nodeHeadData.Timestamp)
                throw new Exception($"Node is out of date Id: {nodeHeadData.NodeId}, path: {nodeHeadData.Path}.");

            // Get VersionDoc and update
            if (!DB.Versions.TryGetValue(versionData.VersionId, out var versionDoc))
                throw new Exception($"Version not found. VersionId: {versionData.VersionId} NodeId: {nodeHeadData.NodeId}, path: {nodeHeadData.Path}.");
            var versionId = versionData.VersionId;
            dynamicData.VersionId = versionData.VersionId;
            UpdateVersionDoc(versionDoc, versionData, dynamicData);

            // Delete unnecessary versions
            DeleteVersionsAsync(versionIdsToDelete);

            // Update NodeDoc (create a new nodeDoc instance)
            nodeDoc = CreateNodeDoc(nodeHeadData);
            LoadLastVersionIds(nodeHeadData.NodeId, out var lastMajorVersionId, out var lastMinorVersionId);
            nodeDoc.LastMajorVersionId = lastMajorVersionId;
            nodeDoc.LastMinorVersionId = lastMinorVersionId;
            DB.Nodes[nodeDoc.NodeId] = nodeDoc;

            // Manage BinaryProperties
            foreach (var item in dynamicData.BinaryProperties)
                SaveBinaryProperty(item.Value, versionId, item.Key.Id, true, false);

            nodeHeadData.Timestamp = nodeDoc.Timestamp;
            versionData.Timestamp = versionDoc.Timestamp;
            nodeHeadData.LastMajorVersionId = lastMajorVersionId;
            nodeHeadData.LastMinorVersionId = lastMinorVersionId;

            return STT.Task.CompletedTask;
        }

        public override STT.Task CopyAndUpdateNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData,
            IEnumerable<int> versionIdsToDelete, int currentVersionId, int expectedVersionId = 0)
        {
            if (!DB.Nodes.TryGetValue(nodeHeadData.NodeId, out var nodeDoc))
                throw new Exception($"Cannot update a deleted Node. Id: {nodeHeadData.NodeId}, path: {nodeHeadData.Path}.");
            if (nodeDoc.Timestamp != nodeHeadData.Timestamp)
                throw new Exception($"Node is out of date Id: {nodeHeadData.NodeId}, path: {nodeHeadData.Path}.");

            // Get existing VersionDoc and update
            if (!DB.Versions.TryGetValue(currentVersionId, out var currentVersionDoc))
                throw new Exception($"Version not found. VersionId: {versionData.VersionId} NodeId: {nodeHeadData.NodeId}, path: {nodeHeadData.Path}.");
            var versionId = expectedVersionId == 0 ? DB.GetNextVersionId() : expectedVersionId;
            var versionDoc = CloneVersionDoc(currentVersionDoc);
            versionDoc.VersionId = versionId;
            versionData.VersionId = versionId;
            dynamicData.VersionId = versionId;
            UpdateVersionDoc(versionDoc, versionData, dynamicData);

            // Add or change updated VersionDoc
            DB.Versions[versionId] = versionDoc;

            // Delete unnecessary versions
            DeleteVersionsAsync(versionIdsToDelete);

            // UpdateNodeDoc
            nodeDoc = CreateNodeDoc(nodeHeadData);
            LoadLastVersionIds(nodeHeadData.NodeId, out var lastMajorVersionId, out var lastMinorVersionId);
            nodeDoc.LastMajorVersionId = lastMajorVersionId;
            nodeDoc.LastMinorVersionId = lastMinorVersionId;
            DB.Nodes[nodeDoc.NodeId] = nodeDoc;

            // Manage BinaryProperties
            foreach (var item in dynamicData.BinaryProperties)
                SaveBinaryProperty(item.Value, versionId, item.Key.Id, false, true);

            nodeHeadData.Timestamp = nodeDoc.Timestamp;
            versionData.Timestamp = versionDoc.Timestamp;
            nodeHeadData.LastMajorVersionId = lastMajorVersionId;
            nodeHeadData.LastMinorVersionId = lastMinorVersionId;

            return STT.Task.CompletedTask;
        }

        public override STT.Task UpdateNodeHeadAsync(NodeHeadData nodeHeadData, IEnumerable<int> versionIdsToDelete)
        {
            if (!DB.Nodes.TryGetValue(nodeHeadData.NodeId, out var nodeDoc))
                throw new Exception($"Cannot update a deleted Node. Id: {nodeHeadData.NodeId}, path: {nodeHeadData.Path}.");
            if (nodeDoc.Timestamp != nodeHeadData.Timestamp)
                throw new Exception($"Node is out of date Id: {nodeHeadData.NodeId}, path: {nodeHeadData.Path}.");

            // Delete unnecessary versions
            DeleteVersionsAsync(versionIdsToDelete);

            // Update NodeDoc (create a new nodeDoc instance)
            nodeDoc = CreateNodeDoc(nodeHeadData);
            LoadLastVersionIds(nodeHeadData.NodeId, out var lastMajorVersionId, out var lastMinorVersionId);
            nodeDoc.LastMajorVersionId = lastMajorVersionId;
            nodeDoc.LastMinorVersionId = lastMinorVersionId;
            DB.Nodes[nodeDoc.NodeId] = nodeDoc;

            // Update return values
            nodeHeadData.Timestamp = nodeDoc.Timestamp;
            nodeHeadData.LastMajorVersionId = lastMajorVersionId;
            nodeHeadData.LastMinorVersionId = lastMinorVersionId;

            return STT.Task.CompletedTask;
        }

        public override STT.Task UpdateSubTreePathAsync(string oldPath, string newPath)
        {
            foreach (var nodeDoc in DB.Nodes.Values
                                            .Where(n => n.Path.StartsWith(oldPath + "/", StringComparison.OrdinalIgnoreCase))
                                            .ToArray())
            {
                nodeDoc.Path = newPath + nodeDoc.Path.Substring(oldPath.Length);
            }
            return STT.Task.CompletedTask;
        }

        public override Task<IEnumerable<NodeData>> LoadNodesAsync(int[] versionIdArray)
        {
            List<NodeData> result = new List<NodeData>();
            foreach (var versionId in versionIdArray)
            {
                // Do not load deleted doc
                if (!DB.Versions.TryGetValue(versionId, out var versionDoc))
                    continue;
                // Do not load node by orphaned version
                if (!DB.Nodes.TryGetValue(versionDoc.NodeId, out var nodeDoc))
                    continue;

                var nodeData = new NodeData(nodeDoc.NodeTypeId, nodeDoc.ContentListTypeId)
                {
                    Id = nodeDoc.NodeId,
                    NodeTypeId = nodeDoc.NodeTypeId,
                    ContentListId = nodeDoc.ContentListId,
                    ContentListTypeId = nodeDoc.ContentListTypeId,
                    CreatingInProgress = nodeDoc.CreatingInProgress,
                    IsDeleted = nodeDoc.IsDeleted,
                    ParentId = nodeDoc.ParentNodeId,
                    Name = nodeDoc.Name,
                    DisplayName = nodeDoc.DisplayName,
                    Path = nodeDoc.Path,
                    Index = nodeDoc.Index,
                    Locked = nodeDoc.Locked,
                    LockedById = nodeDoc.LockedById,
                    ETag = nodeDoc.ETag,
                    LockType = nodeDoc.LockType,
                    LockTimeout = nodeDoc.LockTimeout,
                    LockDate = nodeDoc.LockDate,
                    LockToken = nodeDoc.LockToken,
                    LastLockUpdate = nodeDoc.LastLockUpdate,
                    VersionId = versionId,
                    Version = versionDoc.Version,
                    CreationDate = nodeDoc.CreationDate,
                    CreatedById = nodeDoc.CreatedById,
                    ModificationDate = nodeDoc.ModificationDate,
                    ModifiedById = nodeDoc.ModifiedById,
                    IsSystem = nodeDoc.IsSystem,
                    OwnerId = nodeDoc.OwnerId,
                    SavingState = nodeDoc.SavingState,
                    ChangedData = versionDoc.ChangedData,
                    VersionCreationDate = versionDoc.CreationDate,
                    VersionCreatedById = versionDoc.CreatedById,
                    VersionModificationDate = versionDoc.ModificationDate,
                    VersionModifiedById = versionDoc.ModifiedById,
                    NodeTimestamp = nodeDoc.Timestamp,
                    VersionTimestamp = versionDoc.Timestamp
                };

                var dynamicProps = versionDoc.DynamicProperties;
                foreach (var propertyType in nodeData.PropertyTypes)
                    if (dynamicProps.TryGetValue(propertyType.Name, out var value))
                        nodeData.SetDynamicRawData(propertyType, GetClone(value, propertyType.DataType));

                result.Add(nodeData);
            }
            return STT.Task.FromResult((IEnumerable<NodeData>) result);
        }

        public override STT.Task DeleteNodeAsync(int nodeId, long timestamp)
        {
            if (!DB.Nodes.TryGetValue(nodeId, out var nodeDoc))
                return STT.Task.CompletedTask;

            if (nodeDoc.Timestamp != timestamp)
                throw new NodeIsOutOfDateException($"Cannot delete the node. It is out of date. NodeId:{nodeId}, " +
                                                   $"Path:\"{nodeDoc.Path}\"");

            var path = nodeDoc.Path;
            var nodeIds = DB.Nodes.Values
                .Where(n => n.Path.StartsWith(path, StringComparison.OrdinalIgnoreCase))
                .Select(n => n.NodeId)
                .ToArray();
            var versionIds = DB.Versions.Values
                .Where(v => nodeIds.Contains(v.NodeId))
                .Select(v => v.VersionId)
                .ToArray();
            var binPropAndfileIds = DB.BinaryProperties.Values
                .Where(b => versionIds.Contains(b.VersionId))
                .Select(b => new {b.BinaryPropertyId, b.FileId})
                .ToArray();

            foreach (var item in binPropAndfileIds)
            {
                DB.BinaryProperties.Remove(item.BinaryPropertyId);
                DB.Files.Remove(item.FileId);
            }
            foreach (var versionId in versionIds)
                DB.Versions.Remove(versionId);
            foreach (var nId in nodeIds)
                DB.Nodes.Remove(nId);

            return STT.Task.CompletedTask;
        }

        public override STT.Task MoveNodeAsync(int sourceNodeId, int targetNodeId, long sourceTimestamp, long targetTimestamp)
        {
            if (!DB.Nodes.TryGetValue(sourceNodeId, out var sourceNode))
                throw new DataException("Cannot move node, it does not exist.");

            if (!DB.Nodes.TryGetValue(targetNodeId, out var targetNode))
                throw new DataException("Cannot move node, target does not exist.");

            if(sourceTimestamp != sourceNode.Timestamp)
                throw new NodeIsOutOfDateException($"Cannot move the node. It is out of date. NodeId:{sourceNodeId}, " +
                                                   $"Path:{sourceNode.Path}, TargetPath: {targetNode.Path}");

            sourceNode.ParentNodeId = targetNodeId;

            var path = sourceNode.Path;
            var nodes = DB.Nodes.Values
                .Where(n => n.NodeId == sourceNode.NodeId ||
                            n.Path.StartsWith(path + RepositoryPath.PathSeparator, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var sourceParentPath = RepositoryPath.GetParentPath(sourceNode.Path);

            foreach (var node in nodes)
                node.Path = node.Path.Replace(sourceParentPath, targetNode.Path);

            return STT.Task.CompletedTask;
        }

        public override Task<Dictionary<int, string>> LoadTextPropertyValuesAsync(int versionId, int[] notLoadedPropertyTypeIds)
        {
            var result = new Dictionary<int, string>();

            if(!DB.Versions.TryGetValue(versionId, out var versionDoc))
                return STT.Task.FromResult(result);

            var collection = versionDoc.DynamicProperties;
            result = collection.Keys
                .Select(PropertyType.GetByName)
                .Where(x => notLoadedPropertyTypeIds.Contains(x.Id))
                .ToDictionary(x => x.Id, x => (string) collection[x.Name]);

            return STT.Task.FromResult(result);
        }

        [SuppressMessage("ReSharper", "ExpressionIsAlwaysNull")]
        public override Task<BinaryDataValue> LoadBinaryPropertyValueAsync(int versionId, int propertyTypeId)
        {
            BinaryDataValue result = null;

            var binaryDoc = DB.BinaryProperties.Values.FirstOrDefault(x =>
                x.VersionId == versionId && x.PropertyTypeId == propertyTypeId);
            if (binaryDoc == null)
                return STT.Task.FromResult(result);

            if (!DB.Files.TryGetValue(binaryDoc.FileId, out var fileDoc))
                return STT.Task.FromResult(result);
            if (fileDoc.Staging)
                return STT.Task.FromResult(result);

            result = new BinaryDataValue
            {
                Id = binaryDoc.BinaryPropertyId,
                FileId = binaryDoc.FileId,
                Checksum = null,
                FileName = new BinaryFileName(fileDoc.FileNameWithoutExtension, fileDoc.Extension),
                ContentType = fileDoc.ContentType,
                Size = fileDoc.Size,
                BlobProviderName = fileDoc.BlobProvider,
                BlobProviderData = fileDoc.BlobProviderData,
                Timestamp = fileDoc.Timestamp
            };
            return STT.Task.FromResult(result);
        }

        public override Task<bool> NodeExistsAsync(string path)
        {
            var result = DB.Nodes.Any(x=>x.Value.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
            return STT.Task.FromResult(result);
        }

        /* ============================================================================================================= NodeHead */

        public override Task<NodeHead> LoadNodeHeadAsync(string path)
        {
            NodeHead result = null;
            var nodeDoc = DB.Nodes.Values.FirstOrDefault(x => x.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
            if(nodeDoc != null)
                result = NodeDocToNodeHead(nodeDoc);
            return STT.Task.FromResult(result);
        }

        public override Task<NodeHead> LoadNodeHeadAsync(int nodeId)
        {
            NodeHead result = null;
            if (DB.Nodes.TryGetValue(nodeId, out var nodeDoc))
                result = NodeDocToNodeHead(nodeDoc);
            return STT.Task.FromResult(result);
        }

        public override Task<NodeHead> LoadNodeHeadByVersionIdAsync(int versionId)
        {
            var versionDoc = DB.Versions.Values.FirstOrDefault(x => x.VersionId == versionId);
            if (versionDoc == null)
                return null;

            var nodeDoc = DB.Nodes.Values.FirstOrDefault(x => x.NodeId == versionDoc.NodeId);
            if (nodeDoc == null)
                return null;

            return STT.Task.FromResult(NodeDocToNodeHead(nodeDoc));
        }

        public override Task<IEnumerable<NodeHead>> LoadNodeHeadsAsync(IEnumerable<int> heads)
        {
            var headIds = heads.ToArray();
            IEnumerable<NodeHead> result = DB.Nodes
                .Where(x => headIds.Contains(x.Key))
                .Select(x => NodeDocToNodeHead(x.Value))
                .ToArray();
            return STT.Task.FromResult(result);
        }

        public override Task<NodeHead.NodeVersion[]> GetNodeVersions(int nodeId)
        {
            var result = DB.Versions.Values
                .Where(x => x.NodeId == nodeId)
                .OrderBy(x => x.Version.Major)
                .ThenBy(x => x.Version.Minor)
                .Select(x => new NodeHead.NodeVersion(x.Version.Clone(), x.VersionId))
                .ToArray();
            return STT.Task.FromResult(result);
        }

        private NodeHead NodeDocToNodeHead(NodeDoc nodeDoc)
        {
            return new NodeHead(
                nodeDoc.NodeId,
                nodeDoc.Name,
                nodeDoc.DisplayName,
                nodeDoc.Path,
                nodeDoc.ParentNodeId,
                nodeDoc.NodeTypeId,
                nodeDoc.ContentListTypeId,
                nodeDoc.ContentListId,
                nodeDoc.CreationDate,
                nodeDoc.ModificationDate,
                nodeDoc.LastMinorVersionId,
                nodeDoc.LastMajorVersionId,
                nodeDoc.OwnerId,
                nodeDoc.CreatedById,
                nodeDoc.ModifiedById,
                nodeDoc.Index,
                nodeDoc.LockedById,
                nodeDoc.Timestamp);
        }

        /* ============================================================================================================= Tree */

        public override Task<IEnumerable<NodeType>> LoadChildTypesToAllowAsync(int nodeId)
        {
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

            var permeableList = new[] {"Folder", "Page"}
                .Select(x => ActiveSchema.NodeTypes[x])
                .Where(x => x != null)
                .Select(x => x.Id)
                .ToList();

            var typeIdList = new List<int>();
            if (DB.Nodes.TryGetValue(nodeId, out var nodeDoc))
            {
                typeIdList.Add(nodeDoc.NodeTypeId);
                CollectChildTypesToAllow(nodeDoc, permeableList, typeIdList);
            }
            var result = typeIdList.Distinct().Select(x => ActiveSchema.NodeTypes.GetItemById(x)).ToArray();
            return STT.Task.FromResult((IEnumerable<NodeType>)result);
        }

        private void CollectChildTypesToAllow(NodeDoc root, List<int> permeableList, List<int> typeIdList)
        {
            foreach (var child in DB.Nodes.Values.Where(x => x.ParentNodeId == root.NodeId))
            {
                typeIdList.Add(child.NodeTypeId);
                if (permeableList.Contains(child.NodeTypeId))
                    CollectChildTypesToAllow(child, permeableList, typeIdList);
            }
        }

        /* ============================================================================================================= TreeLock */

        public override int AcquireTreeLock(string path)
        {
            var parentChain = GetParentChain(path);
            var timeMin = GetObsoleteLimitTime();

            if (DB.TreeLocks.Values
                .Any(t => t.LockedAt > timeMin &&
                          (parentChain.Contains(t.Path) ||
                           t.Path.StartsWith(path + "/", StringComparison.InvariantCultureIgnoreCase))))
                return 0;

            var newTreeLockId = DB.GetNextTreeLockId();
            DB.TreeLocks.Add(newTreeLockId, new TreeLockDoc
            {
                TreeLockId = newTreeLockId,
                Path = path,
                LockedAt = DateTime.Now
            });

            return newTreeLockId;
        }

        public override bool IsTreeLocked(string path)
        {
            var parentChain = GetParentChain(path);
            var timeMin = GetObsoleteLimitTime();

            return DB.TreeLocks.Values
                .Any(t => t.LockedAt > timeMin &&
                          (parentChain.Contains(t.Path) ||
                           t.Path.StartsWith(path + "/", StringComparison.InvariantCultureIgnoreCase)));
        }

        public override void ReleaseTreeLock(int[] lockIds)
        {
            foreach (var lockId in lockIds)
                DB.TreeLocks.Remove(lockId);
        }

        public override Dictionary<int, string> LoadAllTreeLocks()
        {
            return DB.TreeLocks.Values.ToDictionary(t => t.TreeLockId, t => t.Path);
        }

        private string[] GetParentChain(string path)
        {
            var paths = path.Split(RepositoryPath.PathSeparatorChars, StringSplitOptions.RemoveEmptyEntries);
            paths[0] = "/" + paths[0];
            for (int i = 1; i < paths.Length; i++)
                paths[i] = paths[i - 1] + "/" + paths[i];
            return paths.Reverse().ToArray();
        }
        private DateTime GetObsoleteLimitTime()
        {
            return DateTime.Now.AddHours(-8.0);
        }

        /* ============================================================================================================= IndexDocument */

        public override STT.Task SaveIndexDocumentAsync(NodeData nodeData, IndexDocument indexDoc)
        {
            if (DB.Versions.TryGetValue(nodeData.VersionId, out var versionDoc))
            {
                string serializedDoc;
                using (var writer = new StringWriter())
                {
                    JsonSerializer.Create(new JsonSerializerSettings
                    {
                        Converters = new List<JsonConverter> {new IndexFieldJsonConverter()},
                        NullValueHandling = NullValueHandling.Ignore,
                        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                        Formatting = Formatting.Indented
                    }).Serialize(writer, indexDoc);
                    serializedDoc = writer.GetStringBuilder().ToString();
                }
                versionDoc.IndexDocument = serializedDoc;
                nodeData.VersionTimestamp = versionDoc.Timestamp;
            }
            return STT.Task.CompletedTask;
        }

        /* ============================================================================================================= Schema */

        public override Task<RepositorySchemaData> LoadSchemaAsync()
        {
            return STT.Task.FromResult(DB.Schema.Clone());
        }

        public override SchemaWriter CreateSchemaWriter()
        {
            var newSchema = DB.Schema.Clone();
            return new InMemorySchemaWriter(newSchema, () =>
            {
                newSchema.Timestamp = DB.Schema.Timestamp + 1L;
                DB.Schema = newSchema;
            });
        }

        public override string StartSchemaUpdate_EXPERIMENTAL(long schemaTimestamp)
        {
            if(schemaTimestamp != DB.Schema.Timestamp)
                throw new DataException("Storage schema is out of date.");
            if (DB.SchemaLock != null)
                throw new DataException("Schema is locked by someone else.");
            DB.SchemaLock = Guid.NewGuid().ToString();
            return DB.SchemaLock;
        }

        public override long FinishSchemaUpdate_EXPERIMENTAL(string schemaLock)
        {
            if(schemaLock != DB.SchemaLock)
                throw new DataException("Schema is locked by someone else.");
            DB.SchemaLock = null;
            return DB.Schema.Timestamp;
        }

        /* ============================================================================================================= Provider Tools */

        public override DateTime RoundDateTime(DateTime d)
        {
            return new DateTime(d.Ticks / 100000 * 100000);
        }

        public override bool IsCacheableText(string text)
        {
            return text?.Length < TextAlternationSizeLimit;
        }

        /* ============================================================================================================= Infrastructure */

        private void LoadLastVersionIds(int nodeId, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            var allVersions = DB.Versions.Values
                .Where(v => v.NodeId == nodeId)
                .OrderBy(v => v.Version.Major)
                .ThenBy(v => v.Version.Minor)
                .ThenBy(v => v.Version.Status)
                .ToArray();
            var lastMinorVersion = allVersions.LastOrDefault();
            lastMinorVersionId = lastMinorVersion?.VersionId ?? 0;

            var majorVersions = allVersions
                .Where(v => v.Version.Minor == 0 && v.Version.Status == VersionStatus.Approved)
                .ToArray();

            var lastMajorVersion = majorVersions.LastOrDefault();
            lastMajorVersionId = lastMajorVersion?.VersionId ?? 0;
        }

        //UNDONE:DB -------Delete GetNodeTimestamp feature
        public override long GetNodeTimestamp(int nodeId)
        {
            if (!DB.Nodes.TryGetValue(nodeId, out var existing))
                return 0L;
            return existing.Timestamp;
        }

        //UNDONE:DB -------Delete GetVersionTimestamp feature
        public override long GetVersionTimestamp(int versionId)
        {
            if (!DB.Versions.TryGetValue(versionId, out var existing))
                return 0L;
            return existing.Timestamp;
        }

        public override void InstallInitialData(InitialData data)
        {
            DB.Schema = data.Schema;
            ContentTypeManager.Reset();

            foreach (var node in data.Nodes)
            {
                var versionId = node.LastMajorVersionId;
                if (versionId != node.LastMinorVersionId)
                    throw new NotSupportedException("Cannot install a node with more than one versions.");
                var version = data.Versions.FirstOrDefault(x => x.VersionId == versionId);
                if(version == null)
                    throw new NotSupportedException("Cannot install a node without a versions.");
                var props = data.DynamicProperties.FirstOrDefault(x => x.VersionId == versionId);
                InstallNode(node, version, props);
            }
            ContentTypeManager.Reset();
        }
        private void InstallNode(NodeHeadData nData, VersionData vData, DynamicPropertyData dData)
        {
            DB.Nodes.Add(nData.NodeId, new NodeDoc
            {
                NodeId = nData.NodeId,
                NodeTypeId = nData.NodeTypeId,
                ParentNodeId = nData.ParentNodeId,
                Name = nData.Name,
                Path = nData.Path,
                LastMinorVersionId = nData.LastMinorVersionId,
                LastMajorVersionId = nData.LastMajorVersionId,

                ContentListTypeId = nData.ContentListTypeId,
                ContentListId = nData.ContentListId,
                CreatingInProgress = nData.CreatingInProgress,
                IsDeleted = nData.IsDeleted,
                Index = nData.Index,
                Locked = nData.Locked,
                LockedById = nData.LockedById,
                ETag = nData.ETag,
                LockType = nData.LockType,
                LockTimeout = nData.LockTimeout,
                LockDate = nData.LockDate == default(DateTime)  ? new DateTime(1900, 1, 1): nData.LockDate,
                LockToken = nData.LockToken ?? string.Empty,
                LastLockUpdate = nData.LastLockUpdate == default(DateTime) ? new DateTime(1900, 1, 1) : nData.LastLockUpdate,
                CreationDate = nData.CreationDate == default(DateTime) ? DateTime.UtcNow : nData.CreationDate,
                CreatedById = nData.CreatedById == 0 ? 1 : nData.CreatedById,
                ModificationDate = nData.ModificationDate == default(DateTime) ? DateTime.UtcNow : nData.ModificationDate,
                ModifiedById = nData.ModifiedById == 0 ? 1 : nData.ModifiedById,
                DisplayName = nData.DisplayName,
                IsSystem = nData.IsSystem,
                OwnerId = nData.OwnerId,
                SavingState = nData.SavingState
            });

            DB.Versions.Add(vData.VersionId, new VersionDoc
            {
                VersionId = vData.VersionId,
                NodeId = vData.NodeId,
                Version = vData.Version,
                CreationDate = vData.CreationDate == default(DateTime) ? DateTime.UtcNow : vData.CreationDate,
                CreatedById = vData.CreatedById == 0 ? 1 : vData.CreatedById,
                ModificationDate = vData.ModificationDate == default(DateTime) ? DateTime.UtcNow : vData.ModificationDate,
                ModifiedById = vData.ModifiedById == 0 ? 1 : vData.ModifiedById,
                IndexDocument = null,
                ChangedData = vData.ChangedData,
                DynamicProperties = dData?.DynamicProperties?.ToDictionary(x => x.Key.Name, x => x.Value) ?? new Dictionary<string, object>()
            });

            if (dData != null)
            {
                if (dData.BinaryProperties != null)
                {
                    foreach (var binPropItem in dData.BinaryProperties)
                    {
                        var propertyType = binPropItem.Key;
                        var binProp = binPropItem.Value;

                        DB.BinaryProperties.Add(binProp.Id, new BinaryPropertyDoc
                        {
                            BinaryPropertyId = binProp.Id,
                            FileId = binProp.FileId,
                            VersionId = dData.VersionId,
                            PropertyTypeId = ActiveSchema.PropertyTypes[propertyType.Name].Id
                        });

                        var blobProviderName = binProp.BlobProviderName;
                        if (blobProviderName == null && binProp.BlobProviderData != null
                                                     && binProp.BlobProviderData.StartsWith("/Root", StringComparison.OrdinalIgnoreCase))
                            blobProviderName = typeof(FileSystemReaderBlobProvider).FullName;

                        DB.Files.Add(binProp.FileId, new FileDoc
                        {
                            FileId = binProp.FileId,
                            FileNameWithoutExtension = binProp.FileName.FileNameWithoutExtension,
                            Extension = binProp.FileName.Extension,
                            ContentType = binProp.ContentType,
                            Size = binProp.Size,
                            Timestamp = binProp.Timestamp,
                            BlobProvider = blobProviderName,
                            BlobProviderData = binProp.BlobProviderData,
                        });
                    }
                }
            }
        }

        /* ====================================================================== Tools */

        private void SaveBinaryProperty(BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode, bool isNewProperty)
        {
            if (value == null || value.IsEmpty)
                BlobStorage.DeleteBinaryProperty(versionId, propertyTypeId);
            else if (value.Id == 0 || isNewProperty/* || savingAlgorithm != SavingAlgorithm.UpdateSameVersion*/)
                BlobStorage.InsertBinaryProperty(value, versionId, propertyTypeId, isNewNode);
            else
                BlobStorage.UpdateBinaryProperty(value);
        }

        private NodeDoc CreateNodeDoc(NodeHeadData nodeHeadData)
        {
            return new NodeDoc
            {
                NodeId = nodeHeadData.NodeId,
                NodeTypeId = nodeHeadData.NodeTypeId,
                ContentListTypeId = nodeHeadData.ContentListTypeId,
                ContentListId = nodeHeadData.ContentListId,
                CreatingInProgress = nodeHeadData.CreatingInProgress,
                IsDeleted = nodeHeadData.IsDeleted,
                //IsInherited = nodeHeadData.???,
                ParentNodeId = nodeHeadData.ParentNodeId,
                Name = nodeHeadData.Name,
                Path = nodeHeadData.Path,
                Index = nodeHeadData.Index,
                Locked = nodeHeadData.Locked,
                LockedById = nodeHeadData.LockedById,
                ETag = nodeHeadData.ETag,
                LockType = nodeHeadData.LockType,
                LockTimeout = nodeHeadData.LockTimeout,
                LockDate = nodeHeadData.LockDate,
                LockToken = nodeHeadData.LockToken,
                LastLockUpdate = nodeHeadData.LastLockUpdate,
                //LastMinorVersionId will be set later.
                //LastMajorVersionId will be set later.
                CreationDate = nodeHeadData.CreationDate,
                CreatedById = nodeHeadData.CreatedById,
                ModificationDate = nodeHeadData.ModificationDate,
                ModifiedById = nodeHeadData.ModifiedById,
                DisplayName = nodeHeadData.DisplayName,
                IsSystem = nodeHeadData.IsSystem,
                OwnerId = nodeHeadData.OwnerId,
                SavingState = nodeHeadData.SavingState,
                // Timestamp handled by the new instance itself.
            };
        }
        private VersionDoc CreateVersionDoc(VersionData versionData, DynamicPropertyData dynamicData)
        {
            // Clone property values
            var dynamicProperties = new Dictionary<string, object>();
            foreach (var item in dynamicData.DynamicProperties)
            {
                var propertyType = item.Key;
                switch (propertyType.DataType)
                {
                    case DataType.String:
                    case DataType.Text:
                    case DataType.Int:
                    case DataType.Currency:
                    case DataType.DateTime:
                        dynamicProperties.Add(propertyType.Name, GetClone(item.Value, propertyType.DataType));
                        break;
                    case DataType.Reference:
                        // Do not store empty references.
                        if (EmptyReferencesFilter(propertyType, item.Value))
                            dynamicProperties.Add(propertyType.Name, GetClone(item.Value, propertyType.DataType));
                        break;
                    case DataType.Binary:
                        // Do nothing. These properties are managed by the caller
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return new VersionDoc
            {
                VersionId = versionData.VersionId,
                NodeId = versionData.NodeId,
                Version = versionData.Version.Clone(),
                CreationDate = versionData.CreationDate,
                CreatedById = versionData.CreatedById,
                ModificationDate = versionData.ModificationDate,
                ModifiedById = versionData.ModifiedById,
                ChangedData = null, //UNDONE:------- Set clone of original or delete this property
                DynamicProperties = dynamicProperties,
                // IndexDocument will be set later.
                // Timestamp handled by the new instance itself.
            };
        }
        private void UpdateVersionDoc(VersionDoc versionDoc, VersionData versionData, DynamicPropertyData dynamicData)
        {
            versionDoc.NodeId = versionData.NodeId;
            versionDoc.Version = versionData.Version.Clone();
            versionDoc.CreationDate = versionData.CreationDate;
            versionDoc.CreatedById = versionData.CreatedById;
            versionDoc.ModificationDate = versionData.ModificationDate;
            versionDoc.ModifiedById = versionData.ModifiedById;
            versionDoc.ChangedData = null; //UNDONE:------- Set clone of original or delete this property

            var target = versionDoc.DynamicProperties;
            foreach (var sourceItem in dynamicData.DynamicProperties)
            {
                var propertyType = sourceItem.Key;
                var dataType = propertyType.DataType;
                if (dataType == DataType.Binary)
                    // Handled by higher level
                    continue;
                var clone = GetClone(sourceItem.Value, dataType);
                if (dataType == DataType.Reference)
                {
                    // Remove empty references
                    if (!((IEnumerable<int>)clone).Any())
                    {
                        target.Remove(propertyType.Name);
                        continue;
                    }
                }
                target[propertyType.Name] = clone;
            }
        }
        // ReSharper disable once UnusedMethodReturnValue.Local
        private STT.Task DeleteVersionsAsync(IEnumerable<int> versionIdsToDelete)
        {
            foreach (var versionId in versionIdsToDelete)
            {
                foreach (var binPropId in DB.BinaryProperties.Values
                    .Where(x => x.VersionId == versionId)
                    .Select(x => x.BinaryPropertyId)
                    .ToArray())
                {
                    DB.BinaryProperties.Remove(binPropId);
                }
                DB.Versions.Remove(versionId);
            }
            return STT.Task.CompletedTask;
        }
        private bool EmptyReferencesFilter(PropertyType propertyType, object value)
        {
            if (propertyType.DataType != DataType.Reference)
                return true;
            if (value == null)
                return false;
            return ((IEnumerable<int>)value).Any();
        }

        private VersionDoc CloneVersionDoc(VersionDoc source)
        {
            return new VersionDoc
            {
                VersionId = source.VersionId,
                NodeId = source.NodeId,
                Version = source.Version.Clone(),
                CreationDate = source.CreationDate,
                CreatedById = source.CreatedById,
                ModificationDate = source.ModificationDate,
                ModifiedById = source.ModifiedById,
                ChangedData = source.ChangedData,
                DynamicProperties = CloneDynamicProperties(source.DynamicProperties),
                IndexDocument = source.IndexDocument
                // Timestamp handled by the new instance itself.
            };
        }
        private Dictionary<string, object> CloneDynamicProperties(Dictionary<string, object> source)
        {
            return source.ToDictionary(x => x.Key, x => GetClone(x.Value, PropertyType.GetByName(x.Key).DataType));
        }
        private object GetClone(object value, DataType dataType)
        {
            if (value == null)
                return null;

            switch (dataType)
            {
                case DataType.String:
                case DataType.Text:
                    return new string(value.ToString().ToCharArray());
                case DataType.Int:
                    return (int)value;
                case DataType.Currency:
                    return (decimal)value;
                case DataType.DateTime:
                    return new DateTime(((DateTime)value).Ticks);
                case DataType.Binary:
                    return CloneBinaryProperty((BinaryDataValue)value);
                case DataType.Reference:
                    return ((IEnumerable<int>)value).ToArray();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private BinaryDataValue CloneBinaryProperty(BinaryDataValue original)
        {
            return new BinaryDataValue
            {
                Id = original.Id,
                Stream = CloneStream(original.Stream),
                FileId = original.FileId,
                Size = original.Size,
                FileName = original.FileName,
                ContentType = original.ContentType,
                Checksum = original.Checksum,
                Timestamp = original.Timestamp,
                BlobProviderName = original.BlobProviderName,
                BlobProviderData = original.BlobProviderData,
            };
        }
        private Stream CloneStream(Stream original)
        {
            if (original is MemoryStream memStream)
                return new MemoryStream(memStream.GetBuffer().ToArray());
            throw new NotImplementedException();
        }
    }
}