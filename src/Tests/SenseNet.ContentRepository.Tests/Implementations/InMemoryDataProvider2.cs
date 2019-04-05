using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using STT = System.Threading.Tasks;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
        // ReSharper disable once InconsistentNaming
        internal InMemoryDataBase2 DB = new InMemoryDataBase2();

        /* ============================================================================================================= Nodes */

        //public override STT.Task InsertNodeAsync(NodeData nodeData, NodeSaveSettings settings)
        //{
        //    //UNDONE:DB Lock? Transaction?

        //    var nodeId = DB.GetNextNodeId();
        //    nodeData.Id = nodeId;
        //    var nodeHeadData = GetNodeHeadData(nodeData);
        //    nodeHeadData.NodeId = nodeId;

        //    var versionId = DB.GetNextVersionId();
        //    nodeData.VersionId = versionId;
        //    var versionData = GetVersionData(nodeData);
        //    versionData.VersionId = versionId;
        //    versionData.NodeId = nodeId;

        //    DB.Versions[versionId] = versionData;

        //    // Manage BinaryProperties
        //    foreach (var binPropType in nodeData.PropertyTypes.Where(x => x.DataType == DataType.Binary))
        //    {
        //        var value = nodeData.GetDynamicRawData(binPropType) ?? binPropType.DefaultValue;
        //        var binValue = (BinaryDataValue)value;
        //        SaveBinaryProperty(binValue, versionId, binPropType, true, SavingAlgorithm.CreateNewNode);
        //    }

        //    // Manage last versionIds and timestamps

        //    LoadLastVersionIds(nodeId, out var lastMajorVersionId, out var lastMinorVersionId);
        //    nodeHeadData.LastMajorVersionId = lastMajorVersionId;
        //    nodeHeadData.LastMinorVersionId = lastMinorVersionId;
        //    settings.LastMajorVersionIdAfter = lastMajorVersionId;
        //    settings.LastMinorVersionIdAfter = lastMinorVersionId;

        //    DB.Nodes[nodeId] = nodeHeadData;

        //    nodeData.NodeTimestamp = nodeHeadData.Timestamp;
        //    nodeData.VersionTimestamp = versionData.Timestamp;

        //    return STT.Task.CompletedTask;
        //}
        public override STT.Task InsertNodeAsync(NodeHeadData nodeHeadData, VersionData versionData, DynamicData dynamicData)
        {
            //UNDONE:DB Lock? Transaction?

            var nodeId = DB.GetNextNodeId();
            nodeHeadData.NodeId = nodeId;
            nodeHeadData.NodeId = nodeId;

            var versionId = DB.GetNextVersionId();
            versionData.VersionId = versionId;
            versionData.NodeId = nodeId;

            var versionDoc = CreateVersionDocForInsert(versionData, dynamicData);
            DB.Versions[versionId] = versionDoc;
            versionData.Timestamp = versionDoc.Timestamp;

            foreach (var item in dynamicData.BinaryProperties)
                SaveBinaryProperty(item.Value, versionId, item.Key, true, SavingAlgorithm.CreateNewNode);

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
        private void SaveBinaryProperty(BinaryDataValue value, int versionId, PropertyType propertyType, bool isNewNode, SavingAlgorithm savingAlgorithm)
        {
            if (value == null || value.IsEmpty)
                BlobStorage.DeleteBinaryProperty(versionId, propertyType.Id);
            else if (value.Id == 0 || savingAlgorithm != SavingAlgorithm.UpdateSameVersion)
                BlobStorage.InsertBinaryProperty(value, versionId, propertyType.Id, isNewNode);
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
        private VersionDoc CreateVersionDocForInsert(VersionData versionData, DynamicData dynamicData)
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


        public override STT.Task UpdateNodeAsync(NodeData nodeData, NodeSaveSettings settings, IEnumerable<int> versionIdsToDelete)
        {
            //UNDONE:DB Lock? Transaction?

            // Executes these:
            // INodeWriter: UpdateNodeRow(nodeData);
            // INodeWriter: UpdateVersionRow(nodeData, out lastMajorVersionId, out lastMinorVersionId);
            // DataProvider: private static void SaveNodeProperties(NodeData nodeData, SavingAlgorithm savingAlgorithm, INodeWriter writer, bool isNewNode)
            // DataProvider: protected internal abstract void DeleteVersion(int versionId, NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId);

            if (!DB.Nodes.TryGetValue(nodeData.Id, out var nodeDoc))
                throw new Exception($"Cannot update a deleted Node. Id: {nodeData.Id}, path: {nodeData.Path}.");
            if (nodeDoc.Timestamp != nodeData.NodeTimestamp)
                throw new Exception($"Node is out of date Id: {nodeData.Id}, path: {nodeData.Path}.");

            DeleteVersionsAsync(versionIdsToDelete);

            var versionId = nodeData.VersionId;
            var versionDoc = GetVersionData(nodeData);

            // UpdateVersionRow
            DB.Versions[versionId] = versionDoc;

            // UpdateNodeRow
            nodeDoc = GetNodeHeadData(nodeData);
            LoadLastVersionIds(nodeData.Id, out var lastMajorVersionId, out var lastMinorVersionId);
            nodeDoc.LastMajorVersionId = lastMajorVersionId;
            nodeDoc.LastMinorVersionId = lastMinorVersionId;
            DB.Nodes[nodeDoc.NodeId] = nodeDoc;

            // Manage BinaryProperties
            foreach (var binPropType in nodeData.PropertyTypes.Where(x => x.DataType == DataType.Binary))
            {
                var value = nodeData.GetDynamicRawData(binPropType) ?? binPropType.DefaultValue;
                var binValue = (BinaryDataValue)value;
                SaveBinaryProperty(binValue, versionId, binPropType, true, SavingAlgorithm.UpdateSameVersion);
            }

            nodeData.NodeTimestamp = nodeDoc.Timestamp;
            nodeData.VersionTimestamp = versionDoc.Timestamp;
            settings.LastMajorVersionIdAfter = lastMajorVersionId;
            settings.LastMinorVersionIdAfter = lastMinorVersionId;

            return STT.Task.CompletedTask;
        }
        /// <summary>
        /// WARNING! LAST VERSIONIDS NEED TO BE UPDATED IN THE RELATED NODEDOCS AND NODESAVESETTINGS
        /// </summary>
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

        public override STT.Task CopyAndUpdateNodeAsync(NodeData nodeData, NodeSaveSettings settings,
            IEnumerable<int> versionIdsToDelete, int currentVersionId, int expectedVersionId = 0)
        {
            throw new NotImplementedException();
            //if (!DB.Nodes.TryGetValue(nodeData.Id, out var nodeDoc))
            //    throw new Exception($"Cannot update a deleted Node. Id: {nodeData.Id}, path: {nodeData.Path}.");
            //if (nodeDoc.Timestamp != nodeData.NodeTimestamp)
            //    throw new Exception($"Node is out of date Id: {nodeData.Id}, path: {nodeData.Path}.");


            //if (!DB.Versions.TryGetValue(currentVersionId, out var currentVersionDoc))
            //    throw new Exception($"Version not found. VersionId: {currentVersionId}.");

            //// Get updated version data by nodeData
            //var changedDynamicData = nodeData.GetDynamicData();
            //var versionDoc = GetVersionData(changedDynamicData, currentVersionDoc);

            //// UpdateVersionRow
            //var versionId = expectedVersionId == 0 ? DB.GetNextVersionId() : expectedVersionId;
            //nodeData.VersionId = versionId;
            //versionDoc.VersionId = versionId;
            //versionDoc.Version = nodeData.Version;
            //DB.Versions[versionId] = versionDoc;

            //// Delete unnecessary versions
            //DeleteVersionsAsync(versionIdsToDelete);

            //// UpdateNodeRow
            //nodeDoc = GetNodeHeadData(nodeData);
            //LoadLastVersionIds(nodeData.Id, out var lastMajorVersionId, out var lastMinorVersionId);
            //nodeDoc.LastMajorVersionId = lastMajorVersionId;
            //nodeDoc.LastMinorVersionId = lastMinorVersionId;
            //DB.Nodes[nodeDoc.NodeId] = nodeDoc;

            //// Manage BinaryProperties
            //foreach (var item in changedDynamicData.Where(x => x.Key.DataType == DataType.Binary))
            //{
            //    var propertyType = item.Key;
            //    var value = item.Value ?? propertyType.DefaultValue;
            //    SaveBinaryProperty((BinaryDataValue)value, versionId, propertyType, false, SavingAlgorithm.CopyToNewVersionAndUpdate);
            //}

            //nodeData.NodeTimestamp = nodeDoc.Timestamp;
            //nodeData.VersionTimestamp = versionDoc.Timestamp;
            //settings.LastMajorVersionIdAfter = lastMajorVersionId;
            //settings.LastMinorVersionIdAfter = lastMinorVersionId;

            //return STT.Task.CompletedTask;
        }

        public override STT.Task UpdateNodeHeadAsync(NodeData nodeData)
        {
            throw new NotImplementedException();
        }

        public override STT.Task UpdateSubTreePathAsync(string oldPath, string newPath)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override STT.Task MoveNodeAsync(int sourceNodeId, int targetNodeId,
            long sourceTimestamp, long targetTimestamp)
        {
            throw new NotImplementedException();
        }

        public override Task<Dictionary<int, string>> LoadTextPropertyValuesAsync(int versionId,
            int[] notLoadedPropertyTypeIds)
        {
            //UNDONE:DB!! dynamic properties: theoretically not called but need to test
            var result = new Dictionary<int, string>();
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

        /* ============================================================================================================= NodeHead */

        public override Task<NodeHead> LoadNodeHeadAsync(string path)
        {
            throw new NotImplementedException();
        }

        public override Task<NodeHead> LoadNodeHeadAsync(int nodeId)
        {
            if (!DB.Nodes.TryGetValue(nodeId, out var nodeDoc))
                return null;

            return STT.Task.FromResult(NodeDocToNodeHead(nodeDoc));
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
            throw new NotImplementedException();
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

        /* ============================================================================================================= Infrastructure */

        private NodeDoc GetNodeHeadData(NodeData nodeData)
        {
            return new NodeDoc
            {
                NodeId = nodeData.Id,
                NodeTypeId = nodeData.NodeTypeId,
                ContentListTypeId = nodeData.ContentListTypeId,
                ContentListId = nodeData.ContentListId,
                CreatingInProgress = nodeData.CreatingInProgress,
                IsDeleted = nodeData.IsDeleted,
                //IsInherited = nodeData.???,
                ParentNodeId = nodeData.ParentId,
                Name = nodeData.Name,
                Path = nodeData.Path,
                Index = nodeData.Index,
                Locked = nodeData.Locked,
                LockedById = nodeData.LockedById,
                ETag = nodeData.ETag,
                LockType = nodeData.LockType,
                LockTimeout = nodeData.LockTimeout,
                LockDate = nodeData.LockDate,
                LockToken = nodeData.LockToken,
                LastLockUpdate = nodeData.LastLockUpdate,
                //LastMinorVersionId = nodeData,
                //LastMajorVersionId = nodeData,
                CreationDate = nodeData.CreationDate,
                CreatedById = nodeData.CreatedById,
                ModificationDate = nodeData.ModificationDate,
                ModifiedById = nodeData.ModifiedById,
                DisplayName = nodeData.DisplayName,
                IsSystem = nodeData.IsSystem,
                OwnerId = nodeData.OwnerId,
                SavingState = nodeData.SavingState,
                //Timestamp = nodeData,
            };
        }

        private VersionDoc GetVersionData(NodeData nodeData)
        {
            // Tune property values
            var dynamicData = new Dictionary<string, object>();
            foreach (var propertyType in nodeData.PropertyTypes)
            {
                // Set default if needed
                var value = nodeData.GetDynamicRawData(propertyType) ?? propertyType.DefaultValue;

                // Tune values by data type
                switch (propertyType.DataType)
                {
                    case DataType.String:
                    case DataType.Text:
                    case DataType.Int:
                    case DataType.Currency:
                    case DataType.DateTime:
                        dynamicData.Add(propertyType.Name, GetClone(value, propertyType.DataType));
                        break;
                    case DataType.Reference:
                        // Optional filter: do not store empty references.
                        if (EmptyReferencesFilter(propertyType, value))
                            dynamicData.Add(propertyType.Name, GetClone(value, propertyType.DataType));
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
                VersionId = nodeData.VersionId,
                NodeId = nodeData.Id,
                Version = nodeData.Version.Clone(),
                CreationDate = nodeData.VersionCreationDate,
                CreatedById = nodeData.VersionCreatedById,
                ModificationDate = nodeData.VersionModificationDate,
                ModifiedById = nodeData.VersionModifiedById,
                ChangedData = nodeData.ChangedData,
                DynamicProperties = dynamicData,
                // IndexDocument and Timestamp will be set later.
            };
        }
        private VersionDoc GetVersionData(IDictionary<PropertyType, object> changedDynamicData, VersionDoc sourceVersionDoc)
        {
            var dynamicData = CloneDynamicProperties(sourceVersionDoc.DynamicProperties);
            foreach (var item in changedDynamicData)
            {
                var propertyType = item.Key;
                var dataType = propertyType.DataType;
                if (dataType == DataType.Binary)
                    // Handled by higher level
                    continue;
                var clone = GetClone(item.Value, dataType);
                if (dataType == DataType.Reference)
                {
                    // Remove empty references
                    if (!((IEnumerable<int>) clone).Any())
                    {
                        dynamicData.Remove(propertyType.Name);
                        continue;
                    }
                }
                dynamicData[propertyType.Name] = clone;
            }

            return new VersionDoc
            {
                VersionId = sourceVersionDoc.VersionId,
                NodeId = sourceVersionDoc.NodeId,
                Version = sourceVersionDoc.Version.Clone(),
                CreationDate = sourceVersionDoc.CreationDate,
                CreatedById = sourceVersionDoc.CreatedById,
                ModificationDate = sourceVersionDoc.ModificationDate,
                ModifiedById = sourceVersionDoc.ModifiedById,
                ChangedData = sourceVersionDoc.ChangedData,
                DynamicProperties = dynamicData,
                // IndexDocument and Timestamp will be set later.
            };
        }
        private bool EmptyReferencesFilter(PropertyType propertyType, object value)
        {
            if (propertyType.DataType != DataType.Reference)
                return true;
            if (value == null)
                return false;
            return ((IEnumerable<int>)value).Any();
        }

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

        public override void InstallDefaultStructure()
        {
            InstallNode(1, 1, 3, 5, "Admin", "/Root/IMS/BuiltIn/Portal/Admin");
            InstallNode(2, 2, 4, 0, "Root", "/Root");
            InstallNode(3, 3, 6, 2, "IMS", "/Root/IMS");
            InstallNode(4, 4, 7, 3, "BuiltIn", "/Root/IMS/BuiltIn");
            InstallNode(5, 5, 8, 4, "Portal", "/Root/IMS/BuiltIn/Portal");
            InstallNode(6, 6, 3, 5, "Visitor", "/Root/IMS/BuiltIn/Portal/Visitor");
            InstallNode(7, 7, 2, 5, "Administrators", "/Root/IMS/BuiltIn/Portal/Administrators");
            InstallNode(8, 8, 2, 5, "Everyone", "/Root/IMS/BuiltIn/Portal/Everyone");
            InstallNode(9, 9, 2, 5, "Owners", "/Root/IMS/BuiltIn/Portal/Owners");
            InstallNode(10, 10, 3, 5, "Somebody", "/Root/IMS/BuiltIn/Portal/Somebody");
            InstallNode(11, 11, 2, 5, "Operators", "/Root/IMS/BuiltIn/Portal/Operators");
            InstallNode(12, 12, 3, 5, "Startup", "/Root/IMS/BuiltIn/Portal/Startup");
        }

        private void InstallNode(int nodeId, int versionId, int nodeTypeId, int parentNodeId, string name, string path)
        {
            DB.Nodes.Add(nodeId, new NodeDoc
            {
                NodeId = nodeId,
                NodeTypeId = nodeTypeId,
                ParentNodeId = parentNodeId,
                Name = name,
                Path = path,
                LastMinorVersionId = versionId,
                LastMajorVersionId = versionId,

                ContentListTypeId = 0,
                ContentListId = 0,
                CreatingInProgress = false,
                IsDeleted = false,
                Index = 0,
                Locked = false,
                LockedById = 0,
                ETag = null,
                LockType = 0,
                LockTimeout = 0,
                LockDate = new DateTime(1900, 1, 1),
                LockToken = string.Empty,
                LastLockUpdate = new DateTime(1900, 1, 1),
                CreationDate = DateTime.UtcNow,
                CreatedById = 1,
                ModificationDate = DateTime.UtcNow,
                ModifiedById = 1,
                DisplayName = null,
                IsSystem = false,
                OwnerId = 1,
                SavingState = ContentSavingState.Finalized,
            });
            DB.Versions.Add(versionId, new VersionDoc
            {
                VersionId = versionId,
                NodeId = nodeId,
                Version = new VersionNumber(1, 0, VersionStatus.Approved),
                CreationDate = DateTime.UtcNow,
                CreatedById = 1,
                ModificationDate = DateTime.UtcNow,
                ModifiedById = 1,
                IndexDocument = null,
                ChangedData = null,
                DynamicProperties = new Dictionary<string, object>()
            });
        }

        /* ====================================================================== Tools */

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