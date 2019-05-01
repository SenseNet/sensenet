using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Search.Querying;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing;
using BlobStorage = SenseNet.ContentRepository.Storage.Data.BlobStorage;
using STT = System.Threading.Tasks;

namespace SenseNet.Tests.Implementations2 //UNDONE:DB -------CLEANUP: move to SenseNet.Tests.Implementations
{
    //UNDONE:DB -------Delete original InMemoryDataProvider and use this. Move to the Tests project
    public class InMemoryDataProvider2 : DataProvider2
    {
        public const int TextAlternationSizeLimit = 4000;

        // ReSharper disable once InconsistentNaming
        public InMemoryDataBase2 DB { get; } = new InMemoryDataBase2();

        /* =============================================================================================== Nodes */

        public override STT.Task InsertNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData)
        {
            using (var transaction = DB.BeginTransaction())
            {
                try
                {
                    // Check unique keys.
                    if (DB.Nodes.Any(n => n.Path.Equals(nodeHeadData.Path, StringComparison.OrdinalIgnoreCase)))
                        throw new NodeAlreadyExistsException();

                    var nodeId = DB.Nodes.GetNextId();
                    nodeHeadData.NodeId = nodeId;

                    var versionId = DB.Versions.GetNextId();
                    versionData.VersionId = versionId;
                    dynamicData.VersionId = versionId;
                    versionData.NodeId = nodeId;

                    var versionDoc = CreateVersionDocSafe(versionData, dynamicData);
                    DB.Versions.Insert(versionDoc);
                    versionData.Timestamp = versionDoc.Timestamp;

                    // Manage LongTextProperties
                    foreach (var item in dynamicData.LongTextProperties)
                        SaveLongTextPropertySafe(versionId, item.Key.Id, item.Value);

                    // Manage BinaryProperties
                    foreach (var item in dynamicData.BinaryProperties)
                        SaveBinaryPropertySafe(item.Value, versionId, item.Key.Id, true, true);

                    // Manage last versionIds and timestamps
                    (int lastMajorVersionId, int lastMinorVersionId) = LoadLastVersionIdsSafe(nodeId);
                    nodeHeadData.LastMajorVersionId = lastMajorVersionId;
                    nodeHeadData.LastMinorVersionId = lastMinorVersionId;

                    var nodeDoc = CreateNodeDocSafe(nodeHeadData);
                    nodeDoc.LastMajorVersionId = lastMajorVersionId;
                    nodeDoc.LastMinorVersionId = lastMinorVersionId;
                    DB.Nodes.Insert(nodeDoc);
                    nodeHeadData.Timestamp = nodeDoc.Timestamp;

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            return STT.Task.CompletedTask;
        }

        public override STT.Task UpdateNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicPropertyData dynamicData, IEnumerable<int> versionIdsToDelete)
        {
            // Executes these:
            // INodeWriter: UpdateNodeRow(nodeData);
            // INodeWriter: UpdateVersionRow(nodeData, out lastMajorVersionId, out lastMinorVersionId);
            // DataProvider: private static void SaveNodeProperties(NodeData nodeData, SavingAlgorithm savingAlgorithm, INodeWriter writer, bool isNewNode)
            // DataProvider: protected internal abstract void DeleteVersion(int versionId, NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId);
            using (var transaction = DB.BeginTransaction())
            {
                try
                {
                    var existingNodeDoc = DB.Nodes.FirstOrDefault(x => x.NodeId == nodeHeadData.NodeId);
                    if (existingNodeDoc == null)
                        throw new Exception(
                            $"Cannot update a deleted Node. Id: {nodeHeadData.NodeId}, path: {nodeHeadData.Path}.");
                    if (existingNodeDoc.Timestamp != nodeHeadData.Timestamp)
                        throw new NodeIsOutOfDateException(
                            $"Node is out of date Id: {nodeHeadData.NodeId}, path: {nodeHeadData.Path}.");

                    // Get VersionDoc and update
                    var versionDoc = DB.Versions.FirstOrDefault(x => x.VersionId == versionData.VersionId);
                    if (versionDoc == null)
                        throw new Exception(
                            $"Version not found. VersionId: {versionData.VersionId} NodeId: {nodeHeadData.NodeId}, path: {nodeHeadData.Path}.");
                    var updatedVersionDoc = CloneVersionDocSafe(versionDoc);
                    DB.Versions.Remove(versionDoc);
                    var versionId = versionData.VersionId;
                    dynamicData.VersionId = versionData.VersionId;
                    UpdateVersionDocSafe(updatedVersionDoc, versionData, dynamicData);
                    DB.Versions.Insert(updatedVersionDoc);

                    // Delete unnecessary versions
                    DeleteVersionsSafe(versionIdsToDelete);

                    // Update NodeDoc (create a new nodeDoc instance)
                    var nodeDoc = CreateNodeDocSafe(nodeHeadData);
                    (int lastMajorVersionId, int lastMinorVersionId) = LoadLastVersionIdsSafe(nodeHeadData.NodeId);
                    nodeDoc.LastMajorVersionId = lastMajorVersionId;
                    nodeDoc.LastMinorVersionId = lastMinorVersionId;
                    DB.Nodes.Remove(existingNodeDoc);
                    DB.Nodes.Insert(nodeDoc);

                    // Manage LongTextProperties
                    foreach (var item in dynamicData.LongTextProperties)
                        SaveLongTextPropertySafe(versionId, item.Key.Id, item.Value);

                    // Manage BinaryProperties
                    foreach (var item in dynamicData.BinaryProperties)
                        SaveBinaryPropertySafe(item.Value, versionId, item.Key.Id, true, false);

                    versionData.Timestamp = versionDoc.Timestamp;
                    nodeHeadData.Timestamp = nodeDoc.Timestamp;
                    nodeHeadData.LastMajorVersionId = lastMajorVersionId;
                    nodeHeadData.LastMinorVersionId = lastMinorVersionId;

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            return STT.Task.CompletedTask;
        }

        public override STT.Task CopyAndUpdateNodeAsync(NodeHeadData nodeHeadData, VersionData versionData,
            DynamicPropertyData dynamicData, IEnumerable<int> versionIdsToDelete, int currentVersionId, int expectedVersionId = 0)
        {
            using (var transaction = DB.BeginTransaction())
            {
                try
                {
                    var existingNodeDoc = DB.Nodes.FirstOrDefault(x => x.NodeId == nodeHeadData.NodeId);
                    if (existingNodeDoc == null)
                        throw new Exception(
                            $"Cannot update a deleted Node. Id: {nodeHeadData.NodeId}, path: {nodeHeadData.Path}.");
                    if (existingNodeDoc.Timestamp != nodeHeadData.Timestamp)
                        throw new NodeIsOutOfDateException(
                            $"Node is out of date Id: {nodeHeadData.NodeId}, path: {nodeHeadData.Path}.");

                    // Get existing VersionDoc and update
                    var currentVersionDoc = DB.Versions.FirstOrDefault(x => x.VersionId == versionData.VersionId);
                    if (currentVersionDoc == null)
                        throw new Exception(
                            $"Version not found. VersionId: {versionData.VersionId} NodeId: {nodeHeadData.NodeId}, path: {nodeHeadData.Path}.");
                    var versionId = expectedVersionId == 0 ? DB.Versions.GetNextId() : expectedVersionId;
                    var versionDoc = CloneVersionDocSafe(currentVersionDoc);
                    versionDoc.VersionId = versionId;
                    versionData.VersionId = versionId;
                    dynamicData.VersionId = versionId;
                    UpdateVersionDocSafe(versionDoc, versionData, dynamicData);

                    // Add or change updated VersionDoc
                    DB.Versions.Remove(versionId);
                    DB.Versions.Insert(versionDoc);

                    // Manage LongTextProperties
                    CopyLongTextPropertiesSafe(currentVersionId, versionId);
                    foreach (var item in dynamicData.LongTextProperties)
                        SaveLongTextPropertySafe(versionId, item.Key.Id, item.Value);

                    // Manage BinaryProperties
                    // (copy old values is unnecessary because all binary properties were loaded before save).
                    foreach (var item in dynamicData.BinaryProperties)
                        SaveBinaryPropertySafe(item.Value, versionId, item.Key.Id, false, true);

                    // Delete unnecessary versions
                    DeleteVersionsSafe(versionIdsToDelete);

                    // UpdateNodeDoc
                    var nodeDoc = CreateNodeDocSafe(nodeHeadData);
                    (int lastMajorVersionId, int lastMinorVersionId) = LoadLastVersionIdsSafe(nodeHeadData.NodeId);
                    nodeDoc.LastMajorVersionId = lastMajorVersionId;
                    nodeDoc.LastMinorVersionId = lastMinorVersionId;
                    DB.Nodes.Remove(existingNodeDoc);
                    DB.Nodes.Insert(nodeDoc);

                    versionData.Timestamp = versionDoc.Timestamp;
                    nodeHeadData.Timestamp = nodeDoc.Timestamp;
                    nodeHeadData.LastMajorVersionId = lastMajorVersionId;
                    nodeHeadData.LastMinorVersionId = lastMinorVersionId;

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            return STT.Task.CompletedTask;
        }

        public override STT.Task UpdateNodeHeadAsync(NodeHeadData nodeHeadData, IEnumerable<int> versionIdsToDelete)
        {
            using (var transaction = DB.BeginTransaction())
            {
                try
                {
                    var existingNodeDoc = DB.Nodes.FirstOrDefault(x => x.NodeId == nodeHeadData.NodeId);
                    if (existingNodeDoc == null)
                        throw new Exception(
                            $"Cannot update a deleted Node. Id: {nodeHeadData.NodeId}, path: {nodeHeadData.Path}.");
                    if (existingNodeDoc.Timestamp != nodeHeadData.Timestamp)
                        throw new NodeIsOutOfDateException(
                            $"Node is out of date Id: {nodeHeadData.NodeId}, path: {nodeHeadData.Path}.");

                    // Delete unnecessary versions
                    DeleteVersionsSafe(versionIdsToDelete);

                    // Update NodeDoc (create a new nodeDoc instance)
                    var nodeDoc = CreateNodeDocSafe(nodeHeadData); 
                    (int lastMajorVersionId, int lastMinorVersionId) = LoadLastVersionIdsSafe(nodeHeadData.NodeId);
                    nodeDoc.LastMajorVersionId = lastMajorVersionId;
                    nodeDoc.LastMinorVersionId = lastMinorVersionId;
                    DB.Nodes.Remove(existingNodeDoc);
                    DB.Nodes.Insert(nodeDoc);

                    // Update return values
                    nodeHeadData.Timestamp = nodeDoc.Timestamp;
                    nodeHeadData.LastMajorVersionId = lastMajorVersionId;
                    nodeHeadData.LastMinorVersionId = lastMinorVersionId;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            return STT.Task.CompletedTask;
        }

        public override STT.Task UpdateSubTreePathAsync(string oldPath, string newPath)
        {
            lock (DB)
            {
                foreach (var nodeDoc in DB.Nodes
                    .Where(n => n.Path.StartsWith(oldPath + "/", StringComparison.OrdinalIgnoreCase))
                    .ToArray())
                {
                    nodeDoc.Path = newPath + nodeDoc.Path.Substring(oldPath.Length);
                }
            }
            return STT.Task.CompletedTask;
        }

        public override Task<IEnumerable<NodeData>> LoadNodesAsync(int[] versionIdArray)
        {
            lock (DB)
            {
                List<NodeData> result = new List<NodeData>();
                foreach (var versionId in versionIdArray)
                {
                    // Do not load deleted doc
                    var versionDoc = DB.Versions.FirstOrDefault(x => x.VersionId == versionId);
                    if (versionDoc == null)
                        continue;
                    // Do not load node by orphaned version
                    var nodeDoc = DB.Nodes.FirstOrDefault(x => x.NodeId == versionDoc.NodeId);
                    if (nodeDoc == null)
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
                            nodeData.SetDynamicRawData(propertyType, GetCloneSafe(value, propertyType.DataType));

                    // Load appropriate LongTextProperties
                    var longTextPropertyTypeIds = nodeData.PropertyTypes
                        .Where(p => p.DataType == DataType.Text)
                        .Select(p => p.Id)
                        .ToArray();
                    var longTextProps = DB.LongTextProperties
                        .Where(x => x.VersionId == versionId &&
                                    longTextPropertyTypeIds.Contains(x.PropertyTypeId) &&
                                    x.Length < TextAlternationSizeLimit)
                        .ToDictionary(x => ActiveSchema.PropertyTypes.GetItemById(x.PropertyTypeId), x => x);
                    foreach (var item in longTextProps)
                        nodeData.SetDynamicRawData(item.Key, GetCloneSafe(item.Value.Value, DataType.Text));

                    result.Add(nodeData);
                }
                return STT.Task.FromResult((IEnumerable<NodeData>)result);
            }
        }

        public override STT.Task DeleteNodeAsync(int nodeId, long timestamp)
        {
            lock (DB)
            {
                var nodeDoc = DB.Nodes.FirstOrDefault(x => x.NodeId == nodeId);
                if (nodeDoc == null)
                    return STT.Task.CompletedTask;

                if (nodeDoc.Timestamp != timestamp)
                    throw new NodeIsOutOfDateException($"Cannot delete the node. It is out of date. NodeId:{nodeId}, " +
                                                       $"Path:\"{nodeDoc.Path}\"");

                var path = nodeDoc.Path;
                var nodeIds = DB.Nodes
                    .Where(n => n.Path.StartsWith(path, StringComparison.OrdinalIgnoreCase))
                    .Select(n => n.NodeId)
                    .ToArray();
                var versionIds = DB.Versions
                    .Where(v => nodeIds.Contains(v.NodeId))
                    .Select(v => v.VersionId)
                    .ToArray();
                var longTextPropIds = DB.LongTextProperties
                    .Where(l => versionIds.Contains(l.VersionId))
                    .Select(l => l.LongTextPropertyId)
                    .ToArray();
                var binPropAndfileIds = DB.BinaryProperties
                    .Where(b => versionIds.Contains(b.VersionId))
                    .Select(b => new { b.BinaryPropertyId, b.FileId })
                    .ToArray();

                foreach (var longTextPropId in longTextPropIds)
                    DB.LongTextProperties.Remove(longTextPropId);

                foreach (var item in binPropAndfileIds)
                {
                    DB.BinaryProperties.Remove(item.BinaryPropertyId);
                    // Delete files is not necessary but if we do it here, maintenance service can be switched off.
                    DB.Files.Remove(item.FileId);
                }

                foreach (var versionId in versionIds)
                    DB.Versions.Remove(versionId);

                foreach (var nId in nodeIds)
                    DB.Nodes.Remove(nId);
            }
            return STT.Task.CompletedTask;
        }

        public override STT.Task MoveNodeAsync(int sourceNodeId, int targetNodeId, long sourceTimestamp, long targetTimestamp)
        {
            lock (DB)
            {
                var sourceNode = DB.Nodes.FirstOrDefault(x => x.NodeId == sourceNodeId);
                if (sourceNode == null)
                    throw new DataException("Cannot move node, it does not exist.");

                var targetNode = DB.Nodes.FirstOrDefault(x => x.NodeId == targetNodeId);
                if (targetNode == null)
                    throw new DataException("Cannot move node, target does not exist.");

                if (sourceTimestamp != sourceNode.Timestamp)
                    throw new NodeIsOutOfDateException($"Cannot move the node. It is out of date. NodeId:{sourceNodeId}, " +
                                                       $"Path:{sourceNode.Path}, TargetPath: {targetNode.Path}");

                sourceNode.ParentNodeId = targetNodeId;

                var path = sourceNode.Path;
                var nodes = DB.Nodes
                    .Where(n => n.NodeId == sourceNode.NodeId ||
                                n.Path.StartsWith(path + RepositoryPath.PathSeparator, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                var sourceParentPath = RepositoryPath.GetParentPath(sourceNode.Path);

                foreach (var node in nodes)
                    node.Path = node.Path.Replace(sourceParentPath, targetNode.Path);
            }
            return STT.Task.CompletedTask;
        }

        public override Task<Dictionary<int, string>> LoadTextPropertyValuesAsync(int versionId, int[] notLoadedPropertyTypeIds)
        {
            lock (DB)
            {
                var result = DB.LongTextProperties
                    .Where(x => x.VersionId == versionId && notLoadedPropertyTypeIds.Contains(x.PropertyTypeId))
                    .ToDictionary(x => x.PropertyTypeId, x => x.Value);
                return STT.Task.FromResult(result);
            }
        }

        [SuppressMessage("ReSharper", "ExpressionIsAlwaysNull")]
        public override Task<BinaryDataValue> LoadBinaryPropertyValueAsync(int versionId, int propertyTypeId)
        {
            lock (DB)
            {
                BinaryDataValue result = null;

                var binaryDoc = DB.BinaryProperties.FirstOrDefault(x =>
                    x.VersionId == versionId && x.PropertyTypeId == propertyTypeId);
                if (binaryDoc == null)
                    return STT.Task.FromResult(result);

                var fileDoc = DB.Files.FirstOrDefault(x => x.FileId == binaryDoc.FileId);
                if (fileDoc == null)
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
        }

        public override Task<bool> NodeExistsAsync(string path)
        {
            lock (DB)
            {
                var result = DB.Nodes.Any(x => x.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
                return STT.Task.FromResult(result);
            }
        }

        /* =============================================================================================== NodeHead */

        public override Task<NodeHead> LoadNodeHeadAsync(string path)
        {
            lock (DB)
            {
                NodeHead result = null;
                var nodeDoc = DB.Nodes.FirstOrDefault(x => x.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
                if (nodeDoc != null)
                    result = NodeDocToNodeHeadSafe(nodeDoc);
                return STT.Task.FromResult(result);
            }
        }

        public override Task<NodeHead> LoadNodeHeadAsync(int nodeId)
        {
            lock (DB)
            {
                NodeHead result = null;
                var nodeDoc = DB.Nodes.FirstOrDefault(x => x.NodeId == nodeId);
                if (nodeDoc != null)
                    result = NodeDocToNodeHeadSafe(nodeDoc);
                return STT.Task.FromResult(result);
            }
        }

        public override Task<NodeHead> LoadNodeHeadByVersionIdAsync(int versionId)
        {
            lock (DB)
            {
                var versionDoc = DB.Versions.FirstOrDefault(x => x.VersionId == versionId);
                if (versionDoc == null)
                    return null;

                var nodeDoc = DB.Nodes.FirstOrDefault(x => x.NodeId == versionDoc.NodeId);
                if (nodeDoc == null)
                    return null;

                return STT.Task.FromResult(NodeDocToNodeHeadSafe(nodeDoc));
            }
        }

        public override Task<IEnumerable<NodeHead>> LoadNodeHeadsAsync(IEnumerable<int> heads)
        {
            lock (DB)
            {
                var headIds = heads.ToArray();
                IEnumerable<NodeHead> result = DB.Nodes
                    .Where(x => headIds.Contains(x.NodeId))
                    .Select(NodeDocToNodeHeadSafe)
                    .ToArray();
                return STT.Task.FromResult(result);
            }
        }

        public override Task<NodeHead.NodeVersion[]> GetNodeVersions(int nodeId)
        {
            lock (DB)
            {
                var result = DB.Versions
                    .Where(x => x.NodeId == nodeId)
                    .OrderBy(x => x.Version.Major)
                    .ThenBy(x => x.Version.Minor)
                    .Select(x => new NodeHead.NodeVersion(x.Version.Clone(), x.VersionId))
                    .ToArray();
                return STT.Task.FromResult(result);
            }
        }

        public override Task<IEnumerable<VersionNumber>> GetVersionNumbersAsync(int nodeId)
        {
            lock (DB)
            {
                var versions = DB.Versions.Where(r => r.NodeId == nodeId).Select(r => r.Version).ToArray();
                return STT.Task.FromResult((IEnumerable<VersionNumber>)versions);
            }
        }

        public override Task<IEnumerable<VersionNumber>> GetVersionNumbersAsync(string path)
        {
            lock (DB)
            {
                var node = DB.Nodes.FirstOrDefault(x => x.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
                if (node == null)
                    return STT.Task.FromResult((IEnumerable<VersionNumber>)new VersionNumber[0]);
                return GetVersionNumbersAsync(node.NodeId);
            }
        }

        private NodeHead NodeDocToNodeHeadSafe(NodeDoc nodeDoc)
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

        /* =============================================================================================== NodeQuery */

        public override Task<int> InstanceCountAsync(int[] nodeTypeIds)
        {
            lock (DB)
            {
                var result = DB.Nodes.Count(n => nodeTypeIds.Contains(n.NodeTypeId));
                return STT.Task.FromResult(result);
            }
        }
        public override Task<IEnumerable<int>> GetChildrenIdentfiersAsync(int parentId)
        {
            lock (DB)
            {
                var result = DB.Nodes.Where(n => n.ParentNodeId == parentId).Select(n => n.NodeId).ToArray();
                return STT.Task.FromResult((IEnumerable<int>)result);
            }
        }
        public override Task<IEnumerable<int>> QueryNodesByTypeAndPathAndNameAsync(int[] nodeTypeIds, string[] pathStart, bool orderByPath, string name)
        {
            lock (DB)
            {
                IEnumerable<NodeDoc> nodes = DB.Nodes;
                if (nodeTypeIds != null)
                    nodes = nodes
                        .Where(n => nodeTypeIds.Contains(n.NodeTypeId))
                        .ToList();

                if (name != null)
                    nodes = nodes
                        .Where(n => n.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                        .ToList();

                if (pathStart != null && pathStart.Length > 0)
                {
                    var paths = pathStart.Select(p => p.EndsWith("/") ? p : p + "/").ToArray();
                    nodes = nodes
                        .Where(n => paths.Any(p => n.Path.StartsWith(p, StringComparison.InvariantCultureIgnoreCase)))
                        .ToList();
                }

                if (orderByPath)
                    nodes = nodes
                        .OrderBy(n => n.Path)
                        .ToList();

                var result = nodes.Select(n => n.NodeId).ToArray();
                return STT.Task.FromResult((IEnumerable<int>)result);
            }
        }
        public override Task<IEnumerable<int>> QueryNodesByTypeAndPathAndPropertyAsync(int[] nodeTypeIds, string pathStart, bool orderByPath, List<QueryPropertyData> properties)
        {
            //UNDONE:DB: Partially implemented.
            lock (DB)
            {
                IEnumerable<NodeDoc> nodes = DB.Nodes;
                if (nodeTypeIds != null)
                    nodes = nodes
                        .Where(n => nodeTypeIds.Contains(n.NodeTypeId))
                        .ToList();

                if (pathStart != null)
                {
                    var path = pathStart.EndsWith("/") ? pathStart : pathStart + "/";
                    nodes = nodes
                        .Where(n => n.Path.StartsWith(path, StringComparison.InvariantCultureIgnoreCase))
                        .ToList();
                }

                if (properties != null)
                {
                    var versionIds = nodes.Select(n => n.LastMinorVersionId).ToArray();
                    var flatRows = DB.Versions.Where(f => versionIds.Contains(f.VersionId)).ToArray();
                    var resultVersions = flatRows
                        .Where(v =>
                        {
                            foreach (var property in properties)
                            {
                                if (property.QueryOperator != Operator.Equal)
                                    throw new SnNotSupportedException($"NodeQuery by 'Operator.{property.QueryOperator}' property operator is not supported.");

                                var pt = PropertyType.GetByName(property.PropertyName);
                                if (pt == null)
                                    throw new SnNotSupportedException($"NodeQuery by '{property.PropertyName}' property is not supported.");

                                var pm = pt.GetDatabaseInfo();
                                var colName = pm.ColumnName;
                                var dt = pt.DataType;
                                var _ = int.Parse(colName.Split('_')[1]) - 1;
                                switch (dt)
                                {
                                    case DataType.String:
                                        if ((string)v.DynamicProperties[pt.Name] != (string)property.Value)
                                            return false;
                                        break;
                                    case DataType.Int:
                                        if ((int)v.DynamicProperties[pt.Name] != (int)property.Value)
                                            return false;
                                        break;
                                    case DataType.Currency:
                                        if ((decimal)v.DynamicProperties[pt.Name] != (decimal)property.Value)
                                            return false;
                                        break;
                                    case DataType.DateTime:
                                        if ((DateTime)v.DynamicProperties[pt.Name] != (DateTime)property.Value)
                                            return false;
                                        break;
                                    default:
                                        throw new SnNotSupportedException($"NodeQuery by 'DataType.{dt}' property data type is not supported.");
                                }
                            }
                            return true;
                        })
                        .Select(f => f.VersionId)
                        .ToArray();

                    nodes = nodes
                        .Where(n => resultVersions.Contains(n.LastMinorVersionId))
                        .ToList();
                }

                if (orderByPath)
                    nodes = nodes
                        .OrderBy(n => n.Path)
                        .ToList();

                var ids = nodes.Select(n => n.NodeId);

                return STT.Task.FromResult((IEnumerable<int>)ids.ToArray());
            }
        }
        public override Task<IEnumerable<int>> QueryNodesByReferenceAndTypeAsync(string referenceName, int referredNodeId, int[] nodeTypeIds)
        {
            //UNDONE:DB ----Not tested: QueryNodesByReferenceAndType
            lock (DB)
            {
                if (referenceName == null)
                    throw new ArgumentNullException(nameof(referenceName));
                if (referenceName.Length == 0)
                    throw new ArgumentException("Argument referenceName cannot be empty.", nameof(referenceName));
                var referenceProperty = ActiveSchema.PropertyTypes[referenceName];
                if (referenceProperty == null)
                    throw new ArgumentException("PropertyType is not found: " + referenceName, nameof(referenceName));

                IEnumerable<NodeDoc> nodes = (nodeTypeIds == null || nodeTypeIds.Length == 0)
                    ? DB.Nodes
                    : DB.Nodes.Where(n => nodeTypeIds.Contains(n.NodeTypeId));

                var result = nodes
                    .SelectMany(n => new[] { n.LastMajorVersionId, n.LastMinorVersionId })
                    .Distinct()
                    .Select(i => DB.Versions.FirstOrDefault(v => v.VersionId == i))
                    .Where(v =>
                    {
                        if (v == null)
                            return false;
                        if (!v.DynamicProperties.TryGetValue(referenceName, out var refs))
                            return false;
                        return ((int[])refs).Contains(referredNodeId);
                    })
                    .Select(v => v.NodeId)
                    .ToArray();

                return STT.Task.FromResult((IEnumerable<int>)result);
            }
        }

        /* =============================================================================================== Tree */

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

            lock (DB)
            {
                var permeableList = new[] { "Folder", "Page" }
                    .Select(x => ActiveSchema.NodeTypes[x])
                    .Where(x => x != null)
                    .Select(x => x.Id)
                    .ToList();

                var typeIdList = new List<int>();

                var nodeDoc = DB.Nodes.FirstOrDefault(x => x.NodeId == nodeId);
                if (nodeDoc != null)
                {
                    typeIdList.Add(nodeDoc.NodeTypeId);
                    CollectChildTypesToAllow(nodeDoc, permeableList, typeIdList);
                }
                var result = typeIdList.Distinct().Select(x => ActiveSchema.NodeTypes.GetItemById(x)).ToArray();
                return STT.Task.FromResult((IEnumerable<NodeType>)result);
            }
        }

        public override Task<List<ContentListType>> GetContentListTypesInTreeAsync(string path)
        {
            lock (DB)
            {
                var result = DB.Nodes
                    .Where(n => n.ContentListId == 0 && n.ContentListTypeId != 0 &&
                                n.Path.StartsWith(path, StringComparison.InvariantCultureIgnoreCase))
                    .Select(n => NodeTypeManager.Current.ContentListTypes.GetItemById(n.ContentListTypeId))
                    .ToList();
                return STT.Task.FromResult(result);
            }
        }
        private void CollectChildTypesToAllow(NodeDoc root, List<int> permeableList, List<int> typeIdList)
        {
            lock (DB)
            {
                foreach (var child in DB.Nodes.Where(x => x.ParentNodeId == root.NodeId))
                {
                    typeIdList.Add(child.NodeTypeId);
                    if (permeableList.Contains(child.NodeTypeId))
                        CollectChildTypesToAllow(child, permeableList, typeIdList);
                }
            }
        }

        public override Task<IEnumerable<EntityTreeNodeData>> LoadEntityTreeAsync()
        {
            lock (DB)
            {
                var result = DB.Nodes
                    .OrderBy(n => n.Path)
                    .Select(n => new EntityTreeNodeData
                    {
                        Id = n.NodeId,
                        ParentId = n.ParentNodeId,
                        OwnerId = n.OwnerId
                    })
                    .ToArray();
                return STT.Task.FromResult((IEnumerable<EntityTreeNodeData>)result);
            }
        }

        /* =============================================================================================== TreeLock */

        public override Task<int> AcquireTreeLockAsync(string path)
        {
            lock (DB)
            {
                var parentChain = GetParentChainSafe(path);
                var timeMin = GetObsoleteLimitTimeSafe();

                if (DB.TreeLocks
                    .Any(t => t.LockedAt > timeMin &&
                              (parentChain.Contains(t.Path) ||
                               t.Path.StartsWith(path + "/", StringComparison.InvariantCultureIgnoreCase))))
                    return STT.Task.FromResult(0);

                var newTreeLockId = DB.TreeLocks.GetNextId();
                DB.TreeLocks.Insert(new TreeLockDoc
                {
                    TreeLockId = newTreeLockId,
                    Path = path,
                    LockedAt = DateTime.UtcNow
                });

                return STT.Task.FromResult(newTreeLockId);
            }
        }

        public override Task<bool> IsTreeLockedAsync(string path)
        {
            lock (DB)
            {
                var parentChain = GetParentChainSafe(path);
                var timeMin = GetObsoleteLimitTimeSafe();

                var result = DB.TreeLocks
                    .Any(t => t.LockedAt > timeMin &&
                              (parentChain.Contains(t.Path) ||
                               t.Path.StartsWith(path + "/", StringComparison.InvariantCultureIgnoreCase)));
                return STT.Task.FromResult(result);
            }
        }

        public override STT.Task ReleaseTreeLockAsync(int[] lockIds)
        {
            lock (DB)
            {
                foreach (var lockId in lockIds)
                    DB.TreeLocks.Remove(lockId);
            }
            return STT.Task.CompletedTask;
        }

        public override Task<Dictionary<int, string>> LoadAllTreeLocksAsync()
        {
            lock (DB)
            {
                var result = DB.TreeLocks.ToDictionary(t => t.TreeLockId, t => t.Path);
                return STT.Task.FromResult(result);
            }
        }

        private string[] GetParentChainSafe(string path)
        {
            var paths = path.Split(RepositoryPath.PathSeparatorChars, StringSplitOptions.RemoveEmptyEntries);
            paths[0] = "/" + paths[0];
            for (int i = 1; i < paths.Length; i++)
                paths[i] = paths[i - 1] + "/" + paths[i];
            return paths.Reverse().ToArray();
        }
        private DateTime GetObsoleteLimitTimeSafe()
        {
            return DateTime.UtcNow.AddHours(-8.0);
        }

        /* =============================================================================================== IndexDocument */

        public override STT.Task SaveIndexDocumentAsync(NodeData nodeData, IndexDocument indexDoc)
        {
            lock (DB)
            {
                var versionDoc = DB.Versions.FirstOrDefault(x => x.VersionId == nodeData.VersionId);
                if (versionDoc != null)
                {
                    var serializedDoc = indexDoc.Serialize();
                    versionDoc.IndexDocument = serializedDoc;
                    nodeData.VersionTimestamp = versionDoc.Timestamp;
                }
            }
            return STT.Task.CompletedTask;
        }

        public override STT.Task SaveIndexDocumentAsync(int versionId, IndexDocument indexDoc)
        {
            lock (DB)
            {
                var versionDoc = DB.Versions.FirstOrDefault(x => x.VersionId == versionId);
                if (versionDoc != null)
                {
                    var serializedDoc = indexDoc.Serialize();
                    versionDoc.IndexDocument = serializedDoc;
                }
            }
            return STT.Task.CompletedTask;
        }

        public override Task<IEnumerable<IndexDocumentData>> LoadIndexDocumentsAsync(IEnumerable<int> versionIds)
        {
            lock (DB)
            {
                var result = versionIds.Select(LoadIndexDocumentByVersionIdSafe).Where(i => i != null).ToArray();
                return STT.Task.FromResult((IEnumerable<IndexDocumentData>)result);
            }
        }
        public override Task<IEnumerable<IndexDocumentData>> LoadIndexDocumentsAsync(string path, int[] excludedNodeTypes)
        {
            lock (DB)
            {
                var result = new List<IndexDocumentData>();
                var pathExt = path + "/";

                var collection = excludedNodeTypes == null || excludedNodeTypes.Length == 0
                    ? DB.Nodes
                    : DB.Nodes.Where(n => !excludedNodeTypes.Contains(n.NodeTypeId));

                foreach (var node in collection.Where(n => n.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase) ||
                                                           n.Path.StartsWith(pathExt, StringComparison.InvariantCultureIgnoreCase)).ToArray())
                foreach (var version in DB.Versions.Where(v => v.NodeId == node.NodeId).ToArray())
                    result.Add(CreateIndexDocumentDataSafe(node, version));

                return STT.Task.FromResult((IEnumerable<IndexDocumentData>)result);
            }
        }

        public override Task<IEnumerable<int>> LoadNotIndexedNodeIdsAsync(int fromId, int toId)
        {
            lock (DB)
            {
                var result = DB.Versions
                    .Where(v => v.IndexDocument == null && v.NodeId >= fromId && v.NodeId <= toId)
                    .Select(v => v.NodeId)
                    .ToArray();
                return STT.Task.FromResult((IEnumerable<int>)result);
            }
        }

        private IndexDocumentData LoadIndexDocumentByVersionIdSafe(int versionId)
        {
            var version = DB.Versions.FirstOrDefault(v => v.VersionId == versionId);
            if (version == null)
                return null;
            var node = DB.Nodes.FirstOrDefault(n => n.NodeId == version.NodeId);
            if (node == null)
                return null;
            return CreateIndexDocumentDataSafe(node, version);
        }
        private IndexDocumentData CreateIndexDocumentDataSafe(NodeDoc node, VersionDoc version)
        {
            var approved = version.Version.Status == VersionStatus.Approved;
            var isLastMajor = node.LastMajorVersionId == version.VersionId;

            return new IndexDocumentData(null, version.IndexDocument)
            {
                NodeTypeId = node.NodeTypeId,
                VersionId = version.VersionId,
                NodeId = node.NodeId,
                ParentId = node.ParentNodeId,
                Path = node.Path,
                IsSystem = node.IsSystem,
                IsLastDraft = node.LastMinorVersionId == version.VersionId,
                IsLastPublic = approved && isLastMajor,
                NodeTimestamp = node.Timestamp,
                VersionTimestamp = version.Timestamp,
            };
        }

        /* =============================================================================================== IndexingActivity */

        public override Task<int> GetLastIndexingActivityIdAsync()
        {
            lock (DB.IndexingActivities)
            {
                var result = DB.IndexingActivities.Count == 0 ? 0 : DB.IndexingActivities.Max(r => r.IndexingActivityId);
                return STT.Task.FromResult(result);
            }
        }

        public override Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int fromId, int toId, int count, bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory)
        {
            var result = new List<IIndexingActivity>();
            lock (DB.IndexingActivities)
            {
                var activities = DB.IndexingActivities.Where(r => r.IndexingActivityId >= fromId && r.IndexingActivityId <= toId).Take(count).ToArray();
                foreach (var activityRecord in activities)
                {
                    var activity = LoadFullIndexingActivitySafe(activityRecord, executingUnprocessedActivities, activityFactory);
                    if (activity != null)
                        result.Add(activity);
                }
            }
            return STT.Task.FromResult(result.ToArray());
        }

        public override Task<IIndexingActivity[]> LoadIndexingActivitiesAsync(int[] gaps, bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory)
        {
            var result = new List<IIndexingActivity>();
            lock (DB.IndexingActivities)
            {
                var activities = DB.IndexingActivities.Where(r => gaps.Contains(r.IndexingActivityId)).ToArray();
                foreach (var activityRecord in activities)
                {
                    var activity = LoadFullIndexingActivitySafe(activityRecord, executingUnprocessedActivities, activityFactory);
                    if (activity != null)
                        result.Add(activity);
                }
            }

            return STT.Task.FromResult(result.ToArray());
        }

        public override Task<IIndexingActivity[]> LoadExecutableIndexingActivitiesAsync(IIndexingActivityFactory activityFactory, int maxCount, int runningTimeoutInSeconds)
        {
            var output = new List<IIndexingActivity>();
            var recordsToStart = new List<IndexingActivityDoc>();
            var timeLimit = DateTime.UtcNow.AddSeconds(-runningTimeoutInSeconds);
            lock (DB.IndexingActivities)
            {
                foreach (var @new in DB.IndexingActivities
                                        .Where(x => x.RunningState == IndexingActivityRunningState.Waiting || (x.RunningState == IndexingActivityRunningState.Running && x.LockTime < timeLimit))
                                        .OrderBy(x => x.IndexingActivityId))
                {
                    if (!DB.IndexingActivities.Any(old =>
                         (old.IndexingActivityId < @new.IndexingActivityId) &&
                         (
                             (old.RunningState == IndexingActivityRunningState.Waiting || old.RunningState == IndexingActivityRunningState.Running) &&
                             (
                                 @new.NodeId == old.NodeId ||
                                 (@new.VersionId != 0 && @new.VersionId == old.VersionId) ||
                                 @new.Path.StartsWith(old.Path + "/", StringComparison.OrdinalIgnoreCase) ||
                                 old.Path.StartsWith(@new.Path + "/", StringComparison.OrdinalIgnoreCase)
                             )
                         )
                    ))
                        recordsToStart.Add(@new);
                }

                foreach (var record in recordsToStart.Take(maxCount))
                {
                    record.RunningState = IndexingActivityRunningState.Running;
                    record.LockTime = DateTime.UtcNow;

                    var activity = LoadFullIndexingActivitySafe(record, false, activityFactory);
                    if (activity != null)
                        output.Add(activity);
                }
            }

            return STT.Task.FromResult(output.ToArray());
        }

        public override Task<ExecutableIndexingActivitiesResult> LoadExecutableIndexingActivitiesAsync(IIndexingActivityFactory activityFactory, int maxCount, int runningTimeoutInSeconds, int[] waitingActivityIds)
        {
            var activities = LoadExecutableIndexingActivitiesAsync(activityFactory, maxCount, runningTimeoutInSeconds).Result;
            lock (DB.IndexingActivities)
            {
                var finishedActivitiyIds = DB.IndexingActivities
                    .Where(x => waitingActivityIds.Contains(x.IndexingActivityId) && x.RunningState == IndexingActivityRunningState.Done)
                    .Select(x => x.IndexingActivityId)
                    .ToArray();
                return STT.Task.FromResult(new ExecutableIndexingActivitiesResult
                {
                    Activities = activities,
                    FinishedActivitiyIds = finishedActivitiyIds
                });
            }
        }

        public override STT.Task RegisterIndexingActivityAsync(IIndexingActivity activity)
        {
            lock (DB.IndexingActivities)
            {
                var newId = DB.IndexingActivities.GetNextId();

                DB.IndexingActivities.Insert(new IndexingActivityDoc
                {
                    IndexingActivityId = newId,
                    ActivityType = activity.ActivityType,
                    CreationDate = DateTime.UtcNow,
                    RunningState = activity.RunningState,
                    LockTime = activity.LockTime,
                    NodeId = activity.NodeId,
                    VersionId = activity.VersionId,
                    Path = activity.Path,
                    Extension = activity.Extension
                });

                activity.Id = newId;
            }
            return STT.Task.CompletedTask;
        }

        public override STT.Task UpdateIndexingActivityRunningStateAsync(int indexingActivityId, IndexingActivityRunningState runningState)
        {
            lock (DB.IndexingActivities)
            {
                var activity = DB.IndexingActivities.FirstOrDefault(r => r.IndexingActivityId == indexingActivityId);
                if (activity != null)
                    activity.RunningState = runningState;
            }
            return STT.Task.CompletedTask;
        }

        public override STT.Task RefreshIndexingActivityLockTimeAsync(int[] waitingIds)
        {
            lock (DB.IndexingActivities)
            {
                var now = DateTime.UtcNow;
                foreach (var waitingId in waitingIds)
                {
                    var activity = DB.IndexingActivities.FirstOrDefault(r => r.IndexingActivityId == waitingId);
                    if (activity != null)
                        activity.LockTime = now;
                }
            }
            return STT.Task.CompletedTask;
        }

        public override STT.Task DeleteFinishedIndexingActivitiesAsync()
        {
            lock (DB.IndexingActivities)
                foreach(var existing in DB.IndexingActivities.Where(x => x.RunningState == IndexingActivityRunningState.Done).ToArray())
                    DB.IndexingActivities.Remove(existing);
            return STT.Task.CompletedTask;
        }

        public override STT.Task DeleteAllIndexingActivitiesAsync()
        {
            lock (DB.IndexingActivities)
            {
                DB.IndexingActivities.Clear();
            }
            return STT.Task.CompletedTask;
        }

        private IIndexingActivity LoadFullIndexingActivitySafe(IndexingActivityDoc activityDoc, bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory)
        {
            var nodeDoc = DB.Nodes.FirstOrDefault(r => r.NodeId == activityDoc.NodeId);
            var versionDoc = DB.Versions.FirstOrDefault(r => r.VersionId == activityDoc.VersionId);
            var activity = activityFactory.CreateActivity(activityDoc.ActivityType);

            activity.Id = activityDoc.IndexingActivityId;
            activity.ActivityType = activityDoc.ActivityType;
            activity.CreationDate = activityDoc.CreationDate;
            activity.RunningState = activityDoc.RunningState;
            activity.LockTime = activityDoc.LockTime;
            activity.NodeId = activityDoc.NodeId;
            activity.VersionId = activityDoc.VersionId;
            activity.Path = activityDoc.Path;
            activity.FromDatabase = true;
            activity.IsUnprocessedActivity = executingUnprocessedActivities;
            activity.Extension = activityDoc.Extension;

            if (versionDoc?.IndexDocument != null && nodeDoc != null)
            {
                activity.IndexDocumentData = new IndexDocumentData(null, versionDoc.IndexDocument)
                {
                    NodeTypeId = nodeDoc.NodeTypeId,
                    VersionId = activity.VersionId,
                    NodeId = activity.NodeId,
                    ParentId = nodeDoc.ParentNodeId,
                    Path = activity.Path,
                    IsSystem = nodeDoc.IsSystem,
                    IsLastDraft = nodeDoc.LastMinorVersionId == activity.VersionId,
                    IsLastPublic = versionDoc.Version.Status == VersionStatus.Approved && nodeDoc.LastMajorVersionId == activity.VersionId,
                    NodeTimestamp = nodeDoc.Timestamp,
                    VersionTimestamp = versionDoc.Timestamp,
                };
            }

            return activity;
        }

        /* =============================================================================================== Schema */

        public override Task<RepositorySchemaData> LoadSchemaAsync()
        {
            lock (DB)
                return STT.Task.FromResult(DB.Schema.Clone());
        }

        public override SchemaWriter CreateSchemaWriter()
        {
            lock (DB)
            {
                var newSchema = DB.Schema.Clone();
                return new InMemorySchemaWriter(newSchema, () =>
                {
                    newSchema.Timestamp = DB.Schema.Timestamp + 1L;
                    DB.Schema = newSchema;
                });
            }
        }

        public override string StartSchemaUpdate_EXPERIMENTAL(long schemaTimestamp)
        {
            lock (DB)
            {
                if (schemaTimestamp != DB.Schema.Timestamp)
                    throw new DataException("Storage schema is out of date.");
                if (DB.SchemaLock != null)
                    throw new DataException("Schema is locked by someone else.");
                DB.SchemaLock = Guid.NewGuid().ToString();
                return DB.SchemaLock;
            }
        }

        public override long FinishSchemaUpdate_EXPERIMENTAL(string schemaLock)
        {
            lock (DB)
            {
                if (schemaLock != DB.SchemaLock)
                    throw new DataException("Schema is locked by someone else.");
                DB.SchemaLock = null;
                return DB.Schema.Timestamp;
            }
        }

        /* =============================================================================================== Logging */

        public override STT.Task WriteAuditEventAsync(AuditEventInfo auditEvent)
        {
            lock (DB)
            {
                var newId = DB.LogEntries.GetNextId();

                DB.LogEntries.Insert(new LogEntryDoc
                {
                    LogId = newId,
                    EventId = auditEvent.EventId,
                    Category = auditEvent.Category,
                    Priority = auditEvent.Priority,
                    Severity = auditEvent.Severity,
                    Title = auditEvent.Title,
                    ContentId = auditEvent.ContentId,
                    ContentPath = auditEvent.ContentPath,
                    UserName = auditEvent.UserName,
                    LogDate = auditEvent.Timestamp,
                    MachineName = auditEvent.MachineName,
                    AppDomainName = auditEvent.AppDomainName,
                    ProcessId = auditEvent.ProcessId,
                    ProcessName = auditEvent.ProcessName,
                    ThreadName = auditEvent.ThreadName,
                    Win32ThreadId = auditEvent.ThreadId,
                    Message = auditEvent.Message,
                    FormattedMessage = auditEvent.FormattedMessage,
                });
            }
            return STT.Task.CompletedTask;
        }

        /* =============================================================================================== Provider Tools */

        public override DateTime RoundDateTime(DateTime d)
        {
            return new DateTime(d.Ticks / 100000 * 100000);
        }

        public override bool IsCacheableText(string text)
        {
            return text?.Length < TextAlternationSizeLimit;
        }

        public override Task<string> GetNameOfLastNodeWithNameBaseAsync(int parentId, string namebase, string extension)
        {
            var regex = new Regex((namebase + "\\([0-9]*\\)" + extension).ToLowerInvariant());
            var existingName = DB.Nodes
                .Where(n => n.ParentNodeId == parentId && regex.IsMatch(n.Name.ToLowerInvariant()))
                .Select(n => n.Name.ToLowerInvariant())
                .OrderByDescending(GetSuffix)
                .FirstOrDefault();
            return STT.Task.FromResult(existingName);
        }
        private int GetSuffix(string name)
        {
            var p0 = name.LastIndexOf("(", StringComparison.Ordinal);
            if (p0 < 0)
                return 0;
            var p1 = name.IndexOf(")", p0, StringComparison.Ordinal);
            if (p1 < 0)
                return 0;
            var suffix = p1 - p0 > 1 ? name.Substring(p0 + 1, p1 - p0 - 1) : "0";
            var order = int.Parse(suffix);
            return order;
        }

        public override Task<long> GetTreeSizeAsync(string path, bool includeChildren)
        {
            lock (DB)
            {
                var collection = includeChildren
                    ? DB.Nodes
                        .Where(n => n.Path == path || n.Path.StartsWith(path + "/", StringComparison.OrdinalIgnoreCase))
                    : DB.Nodes
                        .Where(n => n.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                        .SelectMany(n => DB.Nodes.Where(n1 => n1.ParentNodeId == n.NodeId));

                var result = collection
                    .SelectMany(n => DB.Versions.Where(v => v.NodeId == n.NodeId))
                    .SelectMany(v => DB.BinaryProperties.Where(b => b.VersionId == v.VersionId))
                    .SelectMany(b => DB.Files.Where(f => f.FileId == b.FileId))
                    .Select(f => f.Size).Sum();

                return STT.Task.FromResult(result);
            }
        }

        public override Task<int> GetNodeCountAsync(string path)
        {
            lock (DB)
            {
                if (string.IsNullOrEmpty(path) || path == RepositoryPath.PathSeparator)
                    return STT.Task.FromResult(DB.Nodes.Count);

                var count = DB.Nodes.Count(x => x.Path.StartsWith(path + RepositoryPath.PathSeparator, StringComparison.InvariantCultureIgnoreCase)
                    || x.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase));
                return STT.Task.FromResult(count);
            }
        }

        public override Task<int> GetVersionCountAsync(string path)
        {
            lock (DB)
            {
                if (string.IsNullOrEmpty(path) || path == RepositoryPath.PathSeparator)
                    return STT.Task.FromResult(DB.Versions.Count);

                var count = DB.Nodes.Join(DB.Versions, n => n.NodeId, v => v.NodeId,
                        (node, version) => new { Node = node, Version = version })
                    .Count(
                        x =>
                            x.Node.Path.StartsWith(path + RepositoryPath.PathSeparator,
                                StringComparison.InvariantCultureIgnoreCase)
                            || x.Node.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase));
                return STT.Task.FromResult(count);
            }
        }

        public override Task<long> GetNodeTimestampAsync(int nodeId)
        {
            lock (DB)
            {
                var nodeDoc = DB.Nodes.FirstOrDefault(x => x.NodeId == nodeId);
                var result = nodeDoc?.Timestamp ?? 0L;
                return STT.Task.FromResult(result);
            }
        }

        public override Task<long> GetVersionTimestampAsync(int versionId)
        {
            lock (DB)
            {
                var versionDoc = DB.Versions.FirstOrDefault(x => x.VersionId == versionId);
                var result = versionDoc?.Timestamp ?? 0L;
                return STT.Task.FromResult(result);
            }
        }

        /* =============================================================================================== Installation */

        public override STT.Task InstallInitialDataAsync(InitialData data)
        {
            lock (DB)
            {
                DB.Schema = data.Schema.Clone();
                ContentTypeManager.Reset();

                foreach (var node in data.Nodes)
                {
                    var versionId = node.LastMajorVersionId;
                    if (versionId != node.LastMinorVersionId)
                        throw new NotSupportedException("Cannot install a node with more than one versions.");
                    var version = data.Versions.FirstOrDefault(x => x.VersionId == versionId);
                    if (version == null)
                        throw new NotSupportedException("Cannot install a node without a versions.");
                    var props = data.DynamicProperties.FirstOrDefault(x => x.VersionId == versionId);
                    InstallNodeSafe(node, version, props);
                }
                ContentTypeManager.Reset();
            }
            return STT.Task.CompletedTask;
        }
        private void InstallNodeSafe(NodeHeadData nData, VersionData vData, DynamicPropertyData dData)
        {
            DB.Nodes.Insert(new NodeDoc
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

            DB.Versions.Insert(new VersionDoc
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

            if (dData?.LongTextProperties != null)
            {
                foreach (var longTextPropItem in dData.LongTextProperties)
                {
                    var propertyType = longTextPropItem.Key;
                    var value = longTextPropItem.Value;

                    DB.LongTextProperties.Insert(new LongTextPropertyDoc
                    {
                        LongTextPropertyId = DB.LongTextProperties.GetNextId(),
                        VersionId = dData.VersionId,
                        PropertyTypeId = ActiveSchema.PropertyTypes[propertyType.Name].Id,
                        Length = value.Length,
                        Value = value
                    });
                }
            }

            if (dData?.BinaryProperties != null)
            {
                foreach (var binPropItem in dData.BinaryProperties)
                {
                    var propertyType = binPropItem.Key;
                    var binProp = binPropItem.Value;

                    DB.BinaryProperties.Insert(new BinaryPropertyDoc
                    {
                        BinaryPropertyId = binProp.Id,
                        FileId = binProp.FileId,
                        VersionId = dData.VersionId,
                        PropertyTypeId = ActiveSchema.PropertyTypes[propertyType.Name].Id
                    });

                    var blobProviderName = binProp.BlobProviderName;
                    if (blobProviderName == null && binProp.BlobProviderData != null
                        && binProp.BlobProviderData.StartsWith("/Root", StringComparison.OrdinalIgnoreCase))
                    {
                        blobProviderName = binProp.BlobProviderData.StartsWith(Repository.ContentTypesFolderPath, StringComparison.OrdinalIgnoreCase)
                            ? typeof(ContentTypeStringBlobProvider).FullName
                            : typeof(FileSystemReaderBlobProvider).FullName;
                    }

                    DB.Files.Insert(new FileDoc
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

        /* ====================================================================== Tools */

        private void CopyLongTextPropertiesSafe(int sourceVersionId, int targetVersionId)
        {
            foreach (var existing in DB.LongTextProperties.Where(x => x.VersionId == targetVersionId).ToArray())
                DB.LongTextProperties.Remove(existing);

            foreach (var src in DB.LongTextProperties.Where(x => x.VersionId == sourceVersionId).ToArray())
            {
                DB.LongTextProperties.Insert(new LongTextPropertyDoc
                {
                    LongTextPropertyId = DB.LongTextProperties.GetNextId(),
                    VersionId = targetVersionId,
                    PropertyTypeId = src.PropertyTypeId,
                    Length = src.Length,
                    Value = src.Value
                });
            }
        }
        private void SaveLongTextPropertySafe(int versionId, int propertyTypeId, string value)
        {
            var existing = DB.LongTextProperties
                .FirstOrDefault(x => x.VersionId == versionId && x.PropertyTypeId == propertyTypeId);
            if (existing != null)
                DB.LongTextProperties.Remove(existing);
            if (value == null)
                return;
            DB.LongTextProperties.Insert(new LongTextPropertyDoc
            {
                LongTextPropertyId = DB.LongTextProperties.GetNextId(),
                VersionId = versionId,
                PropertyTypeId = propertyTypeId,
                Length = value.Length,
                Value = value
            });
        }
        private void SaveBinaryPropertySafe(BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode, bool isNewProperty)
        {
            if (value == null || value.IsEmpty)
                BlobStorage.DeleteBinaryProperty(versionId, propertyTypeId);
            else if (value.Id == 0 || isNewProperty/* || savingAlgorithm != SavingAlgorithm.UpdateSameVersion*/)
                BlobStorage.InsertBinaryProperty(value, versionId, propertyTypeId, isNewNode);
            else
                BlobStorage.UpdateBinaryProperty(value);
        }

        private (int, int) LoadLastVersionIdsSafe(int nodeId)
        {
            var allVersions = DB.Versions
                .Where(v => v.NodeId == nodeId)
                .OrderBy(v => v.Version.Major)
                .ThenBy(v => v.Version.Minor)
                .ThenBy(v => v.Version.Status)
                .ToArray();
            var lastMinorVersion = allVersions.LastOrDefault();
            var lastMinorVersionId = lastMinorVersion?.VersionId ?? 0;

            var majorVersions = allVersions
                .Where(v => v.Version.Minor == 0 && v.Version.Status == VersionStatus.Approved)
                .ToArray();

            var lastMajorVersion = majorVersions.LastOrDefault();
            var lastMajorVersionId = lastMajorVersion?.VersionId ?? 0;

            return (lastMajorVersionId, lastMinorVersionId);
        }

        private NodeDoc CreateNodeDocSafe(NodeHeadData nodeHeadData)
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
        private VersionDoc CreateVersionDocSafe(VersionData versionData, DynamicPropertyData dynamicData)
        {
            // Clone property values
            var dynamicProperties = new Dictionary<string, object>();
            foreach (var item in dynamicData.DynamicProperties)
            {
                var propertyType = item.Key;
                switch (propertyType.DataType)
                {
                    case DataType.String:
                    //case DataType.Text:
                    case DataType.Int:
                    case DataType.Currency:
                    case DataType.DateTime:
                        dynamicProperties.Add(propertyType.Name, GetCloneSafe(item.Value, propertyType.DataType));
                        break;
                    case DataType.Reference:
                        // Do not store empty references.
                        if (EmptyReferencesFilterSafe(propertyType, item.Value))
                            dynamicProperties.Add(propertyType.Name, GetCloneSafe(item.Value, propertyType.DataType));
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
                ChangedData = null, //UNDONE:DB -------Set clone of original or delete this property
                DynamicProperties = dynamicProperties,
                // IndexDocument will be set later.
                // Timestamp handled by the new instance itself.
            };
        }
        private void UpdateVersionDocSafe(VersionDoc versionDoc, VersionData versionData, DynamicPropertyData dynamicData)
        {
            versionDoc.NodeId = versionData.NodeId;
            versionDoc.Version = versionData.Version.Clone();
            versionDoc.CreationDate = versionData.CreationDate;
            versionDoc.CreatedById = versionData.CreatedById;
            versionDoc.ModificationDate = versionData.ModificationDate;
            versionDoc.ModifiedById = versionData.ModifiedById;
            versionDoc.ChangedData = null; //UNDONE:DB -------Set clone of original or delete this property

            var target = versionDoc.DynamicProperties;
            foreach (var sourceItem in dynamicData.DynamicProperties)
            {
                var propertyType = sourceItem.Key;
                var dataType = propertyType.DataType;
                if (dataType == DataType.Text || dataType == DataType.Binary)
                    // Handled by higher level
                    throw new Exception($"This property cannot be processed here: {propertyType.Name}:{dataType}");
                var clone = GetCloneSafe(sourceItem.Value, dataType);
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
        private void DeleteVersionsSafe(IEnumerable<int> versionIdsToDelete)
        {
            foreach (var versionId in versionIdsToDelete)
            {
                foreach (var binPropId in DB.BinaryProperties
                    .Where(x => x.VersionId == versionId)
                    .Select(x => x.BinaryPropertyId)
                    .ToArray())
                {
                    var item = DB.BinaryProperties.FirstOrDefault(x => x.BinaryPropertyId == binPropId);
                    DB.BinaryProperties.Remove(item);
                }

                foreach (var longTextPropId in DB.LongTextProperties
                    .Where(x => x.VersionId == versionId)
                    .Select(x => x.LongTextPropertyId)
                    .ToArray())
                {
                    var item = DB.LongTextProperties.FirstOrDefault(x => x.LongTextPropertyId == longTextPropId);
                    DB.LongTextProperties.Remove(item);
                }
                var versionItem = DB.Versions.FirstOrDefault(x => x.VersionId == versionId);
                DB.Versions.Remove(versionItem);
            }
        }
        private bool EmptyReferencesFilterSafe(PropertyType propertyType, object value)
        {
            if (propertyType.DataType != DataType.Reference)
                return true;
            if (value == null)
                return false;
            return ((IEnumerable<int>)value).Any();
        }

        private VersionDoc CloneVersionDocSafe(VersionDoc source)
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
                DynamicProperties = CloneDynamicPropertiesSafe(source.DynamicProperties),
                IndexDocument = source.IndexDocument
                // Timestamp handled by the new instance itself.
            };
        }
        private Dictionary<string, object> CloneDynamicPropertiesSafe(Dictionary<string, object> source)
        {
            return source.ToDictionary(x => x.Key, x => GetCloneSafe(x.Value, PropertyType.GetByName(x.Key).DataType));
        }
        private object GetCloneSafe(object value, DataType dataType)
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
                    return CloneBinaryPropertySafe((BinaryDataValue)value);
                case DataType.Reference:
                    return ((IEnumerable<int>)value).ToList();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private BinaryDataValue CloneBinaryPropertySafe(BinaryDataValue original)
        {
            return new BinaryDataValue
            {
                Id = original.Id,
                Stream = CloneStreamSafe(original.Stream),
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
        private Stream CloneStreamSafe(Stream original)
        {
            if (original is MemoryStream memStream)
                return new MemoryStream(memStream.GetBuffer().ToArray());
            throw new NotImplementedException();
        }
    }
}