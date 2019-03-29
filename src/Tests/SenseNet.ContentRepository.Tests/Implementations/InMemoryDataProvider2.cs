using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Search;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Tests.Implementations
{
    //UNDONE:DB -------Delete original InMemoryDataProvider and use this. Move to the Tests project
    public class InMemoryDataProvider2 : DataProvider2
    {
        // ReSharper disable once InconsistentNaming
        private int __lastNodeId = 1247; // Uses the GetNextNodeId() method.

        /// <summary>
        /// NodeId, NodeHead data
        /// </summary>
        private readonly Dictionary<int, NodeDoc> _nodes = new Dictionary<int, NodeDoc>();

        // ReSharper disable once InconsistentNaming
        private int __lastVersionId = 260; // Uses the GetNextVersionId() method.

        /// <summary>
        /// VersionId, NodeData minus NodeHead
        /// </summary>
        private readonly Dictionary<int, VersionDoc> _versions = new Dictionary<int, VersionDoc>();

        private string _schemaLock;
        private RepositorySchemaData _schema = new RepositorySchemaData();

        /* ============================================================================================================= Nodes */

        public override async Task<SaveResult> InsertNodeAsync(NodeData nodeData)
        {
            //UNDONE:DB Lock? Transaction?

            var saveResult = new SaveResult();

            var nodeId = GetNextNodeId();
            saveResult.NodeId = nodeId;
            var nodeHeadData = GetNodeHeadData(nodeData);
            nodeHeadData.NodeId = nodeId;

            var versionId = GetNextVersionId();
            saveResult.VersionId = versionId;
            var versionData = GetVersionData(nodeData);
            versionData.VersionId = versionId;
            versionData.NodeId = nodeId;

            _versions[versionId] = versionData;

            //UNDONE:DB BinaryIds?

            LoadLastVersionIds(nodeId, out var lastMajorVersionId, out var lastMinorVersionId);
            nodeHeadData.LastMajorVersionId = lastMajorVersionId;
            nodeHeadData.LastMinorVersionId = lastMinorVersionId;

            saveResult.LastMajorVersionId = lastMajorVersionId;
            saveResult.LastMinorVersionId = lastMinorVersionId;

            _nodes[nodeId] = nodeHeadData;

            saveResult.NodeTimestamp = nodeHeadData.Timestamp;
            saveResult.VersionTimestamp = versionData.Timestamp;

            return await System.Threading.Tasks.Task.FromResult(saveResult);
        }

        public override Task<SaveResult> UpdateNodeAsync(NodeData nodeData, IEnumerable<int> versionIdsToDelete)
        {
            //UNDONE:DB Lock? Transaction?

            // Executes these:
            // INodeWriter: UpdateNodeRow(nodeData);
            // INodeWriter: UpdateVersionRow(nodeData, out lastMajorVersionId, out lastMinorVersionId);
            // DataProvider: private static void SaveNodeProperties(NodeData nodeData, SavingAlgorithm savingAlgorithm, INodeWriter writer, bool isNewNode)
            // DataProvider: protected internal abstract void DeleteVersion(int versionId, NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId);

            if (!_nodes.TryGetValue(nodeData.Id, out var nodeDoc))
                throw new Exception($"Cannot update a deleted Node. Id: {nodeData.Id}, path: {nodeData.Path}.");
            if (nodeDoc.Timestamp != nodeData.NodeTimestamp)
                throw new Exception($"Node is out of date Id: {nodeData.Id}, path: {nodeData.Path}.");

            var versionDoc = _versions[nodeData.VersionId];

            // UpdateVersionRow
            versionDoc.NodeId = nodeData.Id;
            versionDoc.Version = nodeData.Version;
            versionDoc.CreationDate = nodeData.VersionCreationDate;
            versionDoc.CreatedById = nodeData.VersionCreatedById;
            versionDoc.ModificationDate = nodeData.VersionModificationDate;
            versionDoc.ModifiedById = nodeData.VersionModifiedById;
            versionDoc.ChangedData = nodeData.ChangedData?.ToArray();

            // UpdateNodeRow
            nodeDoc.NodeTypeId = nodeData.NodeTypeId;
            nodeDoc.ContentListTypeId = nodeData.ContentListTypeId;
            nodeDoc.ContentListId = nodeData.ContentListId;
            nodeDoc.CreatingInProgress = nodeData.CreatingInProgress;
            nodeDoc.IsDeleted = nodeData.IsDeleted;
            //nodeHeadData.IsInherited = nodeData.IsInherited;
            nodeDoc.ParentNodeId = nodeData.ParentId;
            nodeDoc.Name = nodeData.Name;
            nodeDoc.DisplayName = nodeData.DisplayName;
            nodeDoc.Path = nodeData.Path;
            nodeDoc.Index = nodeData.Index;
            nodeDoc.Locked = nodeData.Locked;
            nodeDoc.LockedById = nodeData.LockedById;
            nodeDoc.ETag = nodeData.ETag;
            nodeDoc.LockType = nodeData.LockType;
            nodeDoc.LockTimeout = nodeData.LockTimeout;
            nodeDoc.LockDate = nodeData.LockDate;
            nodeDoc.LockToken = nodeData.LockToken;
            nodeDoc.LastLockUpdate = nodeData.LastLockUpdate;
            nodeDoc.CreationDate = nodeData.CreationDate;
            nodeDoc.CreatedById = nodeData.CreatedById;
            nodeDoc.ModificationDate = nodeData.ModificationDate;
            nodeDoc.ModifiedById = nodeData.ModifiedById;
            nodeDoc.IsSystem = nodeData.IsSystem;
            nodeDoc.OwnerId = nodeData.OwnerId;
            nodeDoc.SavingState = nodeData.SavingState;
            LoadLastVersionIds(nodeData.Id, out var lastMajorVersionId, out var lastMinorVersionId);
            nodeDoc.LastMajorVersionId = lastMajorVersionId;
            nodeDoc.LastMinorVersionId = lastMinorVersionId;

            //UNDONE:DB BinaryIds?

            var result = new SaveResult
            {
                NodeTimestamp = nodeDoc.Timestamp,
                VersionTimestamp = versionDoc.Timestamp,
                LastMajorVersionId = lastMajorVersionId,
                LastMinorVersionId = lastMinorVersionId
            };
            return System.Threading.Tasks.Task.FromResult(result);
        }

        public override Task<SaveResult> CopyAndUpdateNodeAsync(NodeData nodeData, int settingsCurrentVersionId,
            IEnumerable<int> versionIdsToDelete)
        {
            throw new NotImplementedException();
        }

        public override Task<SaveResult> CopyAndUpdateNodeAsync(NodeData nodeData, int currentVersionId,
            int expectedVersionId,
            IEnumerable<int> versionIdsToDelete)
        {
            throw new NotImplementedException();
        }

        public override System.Threading.Tasks.Task UpdateNodeHeadAsync(NodeData nodeData)
        {
            throw new NotImplementedException();
        }

        public override System.Threading.Tasks.Task UpdateSubTreePathAsync(string oldPath, string newPath)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<NodeData>> LoadNodesAsync(int[] versionIdArray)
        {
            List<NodeData> result = new List<NodeData>();
            foreach (var versionId in versionIdArray)
            {
                // Do not load deleted doc
                if (!_versions.TryGetValue(versionId, out var versionDoc))
                    continue;
                // Do not load node by orphaned version
                if (!_nodes.TryGetValue(versionDoc.NodeId, out var nodeDoc))
                    continue;

                result.Add(new NodeData(nodeDoc.NodeTypeId, nodeDoc.ContentListTypeId)
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
                });

                //UNDONE:DB dynamic properties
            }
            return System.Threading.Tasks.Task.FromResult((IEnumerable<NodeData>) result);
        }

        public override System.Threading.Tasks.Task DeleteNodeAsync(int nodeId, long timestamp)
        {
            throw new NotImplementedException();
        }

        public override System.Threading.Tasks.Task MoveNodeAsync(int sourceNodeId, int targetNodeId,
            long sourceTimestamp, long targetTimestamp)
        {
            throw new NotImplementedException();
        }

        public override Task<Dictionary<int, string>> LoadTextPropertyValuesAsync(int versionId,
            int[] notLoadedPropertyTypeIds)
        {
            //UNDONE:DB dynamic properties
            var result = new Dictionary<int, string>();
            return System.Threading.Tasks.Task.FromResult(result);
        }

        /* ============================================================================================================= NodeHead */

        public override Task<NodeHead> LoadNodeHeadAsync(string path)
        {
            throw new NotImplementedException();
        }

        public override Task<NodeHead> LoadNodeHeadAsync(int nodeId)
        {
            if (!_nodes.TryGetValue(nodeId, out var nodeDoc))
                return null;

            return System.Threading.Tasks.Task.FromResult(NodeDocToNodeHead(nodeDoc));
        }

        public override Task<NodeHead> LoadNodeHeadByVersionIdAsync(int versionId)
        {
            var versionDoc = _versions.Values.FirstOrDefault(x => x.VersionId == versionId);
            if (versionDoc == null)
                return null;

            var nodeDoc = _nodes.Values.FirstOrDefault(x => x.NodeId == versionDoc.NodeId);
            if (nodeDoc == null)
                return null;

            return System.Threading.Tasks.Task.FromResult(NodeDocToNodeHead(nodeDoc));
        }

        public override Task<IEnumerable<NodeHead>> LoadNodeHeadsAsync(IEnumerable<int> heads)
        {
            throw new NotImplementedException();
        }

        public override Task<NodeHead.NodeVersion[]> GetNodeVersions(int nodeId)
        {
            var result = _versions.Values
                .Where(x => x.NodeId == nodeId)
                .OrderBy(x => x.Version.Major)
                .ThenBy(x => x.Version.Minor)
                .Select(x => new NodeHead.NodeVersion(x.Version.Clone(), x.VersionId))
                .ToArray();
            return System.Threading.Tasks.Task.FromResult(result);
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

        public override Task<SaveResult> SaveIndexDocumentAsync(NodeData nodeData, IndexDocument indexDoc)
        {
            var result = new SaveResult();
            if (_versions.TryGetValue(nodeData.VersionId, out var versionDoc))
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
                result.VersionTimestamp = versionDoc.Timestamp;
            }
            return System.Threading.Tasks.Task.FromResult(result);
        }

        /* ============================================================================================================= Schema */

        public override Task<RepositorySchemaData> LoadSchemaAsync()
        {
            return System.Threading.Tasks.Task.FromResult(_schema.Clone());
        }

        public override SchemaWriter CreateSchemaWriter()
        {
            var newSchema = _schema.Clone();
            return new InMemorySchemaWriter(newSchema, () =>
            {
                newSchema.Timestamp = _schema.Timestamp + 1L;
                _schema = newSchema;
            });
        }

        public override string StartSchemaUpdate_EXPERIMENTAL(long schemaTimestamp)
        {
            if(schemaTimestamp != _schema.Timestamp)
                throw new DataException("Storage schema is out of date.");
            if (_schemaLock != null)
                throw new DataException("Schema is locked by someone else.");
            _schemaLock = Guid.NewGuid().ToString();
            return _schemaLock;
        }

        public override long FinishSchemaUpdate_EXPERIMENTAL(string schemaLock)
        {
            if(schemaLock != _schemaLock)
                throw new DataException("Schema is locked by someone else.");
            _schemaLock = null;
            return _schema.Timestamp;
        }

        /* ============================================================================================================= Tools */

        public override DateTime RoundDateTime(DateTime d)
        {
            return new DateTime(d.Ticks / 100000 * 100000);
        }

        /* ============================================================================================================= Infrastructure */

        private int GetNextNodeId()
        {
            return Interlocked.Increment(ref __lastNodeId);
        }

        private int GetNextVersionId()
        {
            return Interlocked.Increment(ref __lastVersionId);
        }

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
            return new VersionDoc
            {
                VersionId = nodeData.VersionId,
                NodeId = nodeData.Id,
                Version = nodeData.Version,
                CreationDate = nodeData.VersionCreationDate,
                CreatedById = nodeData.VersionCreatedById,
                ModificationDate = nodeData.VersionModificationDate,
                ModifiedById = nodeData.VersionModifiedById,
                //IndexDocument = ____,
                ChangedData = nodeData.ChangedData,
                //Timestamp = ____,
            };
        }

        private void LoadLastVersionIds(int nodeId, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            var allVersions = _versions.Values
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
            if (!_nodes.TryGetValue(nodeId, out var existing))
                return 0L;
            return existing.Timestamp;
        }

        //UNDONE:DB -------Delete GetVersionTimestamp feature
        public override long GetVersionTimestamp(int versionId)
        {
            if (!_versions.TryGetValue(versionId, out var existing))
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
            _nodes.Add(nodeId, new NodeDoc
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
            _versions.Add(versionId, new VersionDoc
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
            });
        }

        #region private class InMemorySchemaWriter : SchemaWriter

        private class InMemorySchemaWriter : SchemaWriter
        {
            private readonly Action _finishedCallback;
            private readonly RepositorySchemaData _schema;

            public InMemorySchemaWriter(RepositorySchemaData originalSchema, Action finishedCallback)
            {
                _schema = originalSchema;
                _finishedCallback = finishedCallback;
            }

            public override void Open()
            {
                // Do nothing
            }
            public override void Close()
            {
                _finishedCallback();
            }

            public override void CreatePropertyType(string name, DataType dataType, int mapping, bool isContentListProperty)
            {
                _schema.PropertyTypes.Add(new PropertyTypeData
                {
                    Id = GetMaxId(_schema.PropertyTypes) + 1,
                    Name = name,
                    DataType = dataType,
                    Mapping = mapping,
                    IsContentListProperty = isContentListProperty
                });
            }

            public override void DeletePropertyType(PropertyType propertyType)
            {
                var pt =_schema.PropertyTypes.FirstOrDefault(p => p.Name == propertyType.Name &&
                                                                  p.IsContentListProperty == propertyType.IsContentListProperty);
                if (pt != null)
                    _schema.PropertyTypes.Remove(pt);
            }

            public override void CreateNodeType(NodeType parent, string name, string className)
            {
                _schema.NodeTypes.Add(new NodeTypeData
                {
                    Id = Math.Max(GetMaxId(_schema.NodeTypes), GetMaxId(_schema.ContentListTypes)) + 1,
                    Name = name,
                    ParentName = parent?.Name,
                    ClassName = className,
                    Properties = new List<string>()
                });
            }

            public override void ModifyNodeType(NodeType nodeType, NodeType parent, string className)
            {
                var nt = _schema.NodeTypes.FirstOrDefault(p => p.Name == nodeType.Name);
                if (nt == null)
                    return;
                nt.ParentName = parent?.Name;
                nt.ClassName = className;
            }

            public override void DeleteNodeType(NodeType nodeType)
            {
                var nt = _schema.NodeTypes.FirstOrDefault(p => p.Name == nodeType.Name);
                if (nt != null)
                    _schema.NodeTypes.Remove(nt);
            }

            public override void CreateContentListType(string name)
            {
                _schema.ContentListTypes.Add(new ContentListTypeData
                {
                    Id = Math.Max(GetMaxId(_schema.NodeTypes), GetMaxId(_schema.ContentListTypes)) + 1,
                    Name = name,
                    Properties = new List<string>()
                });
            }

            public override void DeleteContentListType(ContentListType contentListType)
            {
                var ct = _schema.ContentListTypes.FirstOrDefault(p => p.Name == contentListType.Name);
                if (ct != null)
                    _schema.ContentListTypes.Remove(ct);
            }

            public override void AddPropertyTypeToPropertySet(PropertyType propertyType, PropertySet owner, bool isDeclared)
            {
                List<string> properties;
                var nt = _schema.NodeTypes.FirstOrDefault(p => p.Name == owner.Name);
                if (nt != null)
                {
                    if (!isDeclared)
                        return;
                    properties = nt.Properties;
                }
                else
                {
                    var ct = _schema.ContentListTypes.FirstOrDefault(p => p.Name == owner.Name);
                    if (ct == null)
                        return;
                    properties = ct.Properties;
                }

                if (!properties.Contains(propertyType.Name))
                    properties.Add(propertyType.Name);
            }

            public override void RemovePropertyTypeFromPropertySet(PropertyType propertyType, PropertySet owner)
            {
                var nt = _schema.NodeTypes.FirstOrDefault(p => p.Name == owner.Name);
                if (nt != null)
                {
                    nt.Properties.Remove(propertyType.Name);
                    return;
                }
                var ct = _schema.ContentListTypes.FirstOrDefault(p => p.Name == owner.Name);
                ct?.Properties.Remove(propertyType.Name);
            }

            public override void UpdatePropertyTypeDeclarationState(PropertyType propertyType, NodeType owner, bool isDeclared)
            {
                var nt = _schema.NodeTypes.FirstOrDefault(p => p.Name == owner.Name);
                if (nt == null)
                    return;

                if (isDeclared)
                {
                    if (!nt.Properties.Contains(propertyType.Name))
                        nt.Properties.Add(propertyType.Name);
                }
                else
                {
                    nt.Properties.Remove(propertyType.Name);
                }
            }

            /* ========================================================================================================= Tools */

            [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
            private int GetMaxId(IEnumerable<ISchemaItemData> list)
            {
                return list.Any() ? list.Max(p => p.Id) : 0;
            }

        }

        #endregion

    }

    internal class IndexFieldJsonConverter : JsonConverter<IndexField>
    {
        public override void WriteJson(JsonWriter writer, IndexField value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Name");
            writer.WriteValue(value.Name);
            writer.WritePropertyName("Type");
            writer.WriteValue(value.Type.ToString());
            if (value.Mode != IndexingMode.Default)
            {
                writer.WritePropertyName("Mode");
                writer.WriteValue(value.Mode.ToString());
            }
            if (value.Store != IndexStoringMode.Default)
            {
                writer.WritePropertyName("Store");
                writer.WriteValue(value.Store.ToString());
            }
            if (value.TermVector != IndexTermVector.Default)
            {
                writer.WritePropertyName("TermVector");
                writer.WriteValue(value.TermVector.ToString());
            }
            writer.WritePropertyName("Value");
            switch (value.Type)
            {
                case IndexValueType.String:
                    writer.WriteValue(value.StringValue);
                    break;
                case IndexValueType.Bool:
                    writer.WriteValue(value.BooleanValue);
                    break;
                case IndexValueType.Int:
                    writer.WriteValue(value.IntegerValue);
                    break;
                case IndexValueType.Long:
                    writer.WriteValue(value.LongValue);
                    break;
                case IndexValueType.Float:
                    writer.WriteValue(value.SingleValue);
                    break;
                case IndexValueType.Double:
                    writer.WriteValue(value.DoubleValue);
                    break;
                case IndexValueType.DateTime:
                    writer.WriteValue(value.DateTimeValue);
                    break;
                case IndexValueType.StringArray:
                    writer.WriteStartArray();
                    writer.WriteRaw("\"" + string.Join("\",\"", value.StringArrayValue) + "\"");
                    writer.WriteEndArray();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            writer.WriteEndObject();
        }

        public override IndexField ReadJson(JsonReader reader, Type objectType, IndexField existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    #region Data Document Classes

    internal class NodeDoc
    {
        private int _nodeId;
        private int _nodeTypeId;
        private int _contentListTypeId;
        private int _contentListId;
        private bool _creatingInProgress;
        private bool _isDeleted;
        private int _parentNodeId;
        private string _name;
        private string _displayName;
        private string _path;
        private int _index;
        private bool _locked;
        private int _lockedById;
        private string _eTag;
        private int _lockType;
        private int _lockTimeout;
        private DateTime _lockDate;
        private string _lockToken;
        private DateTime _lastLockUpdate;
        private int _lastMinorVersionId;
        private int _lastMajorVersionId;
        private DateTime _creationDate;
        private int _createdById;
        private DateTime _modificationDate;
        private int _modifiedById;
        private bool _isSystem;
        private int _ownerId;
        private ContentSavingState _savingState;
        private long _timestamp;

        // ReSharper disable once ConvertToAutoProperty
        public int NodeId
        {
            get => _nodeId;
            set => _nodeId = value;
        }

        public int NodeTypeId
        {
            get => _nodeTypeId;
            set
            {
                _nodeTypeId = value;
                SetTimestamp();
            }
        }

        public int ContentListTypeId
        {
            get => _contentListTypeId;
            set
            {
                _contentListTypeId = value;
                SetTimestamp();
            }
        }

        public int ContentListId
        {
            get => _contentListId;
            set
            {
                _contentListId = value;
                SetTimestamp();
            }
        }

        public bool CreatingInProgress
        {
            get => _creatingInProgress;
            set
            {
                _creatingInProgress = value;
                SetTimestamp();
            }
        }

        public bool IsDeleted
        {
            get => _isDeleted;
            set
            {
                _isDeleted = value;
                SetTimestamp();
            }
        }

        public int ParentNodeId
        {
            get => _parentNodeId;
            set
            {
                _parentNodeId = value;
                SetTimestamp();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                SetTimestamp();
            }
        }

        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                SetTimestamp();
            }
        }

        public string Path
        {
            get => _path;
            set
            {
                _path = value;
                SetTimestamp();
            }
        }

        public int Index
        {
            get => _index;
            set
            {
                _index = value;
                SetTimestamp();
            }
        }

        public bool Locked
        {
            get => _locked;
            set
            {
                _locked = value;
                SetTimestamp();
            }
        }

        public int LockedById
        {
            get => _lockedById;
            set
            {
                _lockedById = value;
                SetTimestamp();
            }
        }

        public string ETag
        {
            get => _eTag;
            set
            {
                _eTag = value;
                SetTimestamp();
            }
        }

        public int LockType
        {
            get => _lockType;
            set
            {
                _lockType = value;
                SetTimestamp();
            }
        }

        public int LockTimeout
        {
            get => _lockTimeout;
            set
            {
                _lockTimeout = value;
                SetTimestamp();
            }
        }

        public DateTime LockDate
        {
            get => _lockDate;
            set
            {
                _lockDate = value;
                SetTimestamp();
            }
        }

        public string LockToken
        {
            get => _lockToken;
            set
            {
                _lockToken = value;
                SetTimestamp();
            }
        }

        public DateTime LastLockUpdate
        {
            get => _lastLockUpdate;
            set
            {
                _lastLockUpdate = value;
                SetTimestamp();
            }
        }

        public int LastMinorVersionId
        {
            get => _lastMinorVersionId;
            set
            {
                _lastMinorVersionId = value;
                SetTimestamp();
            }
        }

        public int LastMajorVersionId
        {
            get => _lastMajorVersionId;
            set
            {
                _lastMajorVersionId = value;
                SetTimestamp();
            }
        }

        public DateTime CreationDate
        {
            get => _creationDate;
            set
            {
                _creationDate = value;
                SetTimestamp();
            }
        }

        public int CreatedById
        {
            get => _createdById;
            set
            {
                _createdById = value;
                SetTimestamp();
            }
        }

        public DateTime ModificationDate
        {
            get => _modificationDate;
            set
            {
                _modificationDate = value;
                SetTimestamp();
            }
        }

        public int ModifiedById
        {
            get => _modifiedById;
            set
            {
                _modifiedById = value;
                SetTimestamp();
            }
        }

        public bool IsSystem
        {
            get => _isSystem;
            set
            {
                _isSystem = value;
                SetTimestamp();
            }
        }

        public int OwnerId
        {
            get => _ownerId;
            set
            {
                _ownerId = value;
                SetTimestamp();
            }
        }

        public ContentSavingState SavingState
        {
            get => _savingState;
            set
            {
                _savingState = value;
                SetTimestamp();
            }
        }

        public long Timestamp => _timestamp;

        private static long _lastTimestamp;

        private void SetTimestamp()
        {
            _timestamp = Interlocked.Increment(ref _lastTimestamp);
        }

        public NodeDoc Clone()
        {
            return new NodeDoc
            {
                _nodeId = _nodeId,
                _nodeTypeId = _nodeTypeId,
                _contentListTypeId = _contentListTypeId,
                _contentListId = _contentListId,
                _creatingInProgress = _creatingInProgress,
                _isDeleted = _isDeleted,
                _parentNodeId = _parentNodeId,
                _name = _name,
                _displayName = _displayName,
                _path = _path,
                _index = _index,
                _locked = _locked,
                _lockedById = _lockedById,
                _eTag = _eTag,
                _lockType = _lockType,
                _lockTimeout = _lockTimeout,
                _lockDate = _lockDate,
                _lockToken = _lockToken,
                _lastLockUpdate = _lastLockUpdate,
                _lastMinorVersionId = _lastMinorVersionId,
                _lastMajorVersionId = _lastMajorVersionId,
                _creationDate = _creationDate,
                _createdById = _createdById,
                _modificationDate = _modificationDate,
                _modifiedById = _modifiedById,
                _isSystem = _isSystem,
                _ownerId = _ownerId,
                _savingState = _savingState,
                _timestamp = _timestamp,
            };
        }
    }

    internal class VersionDoc
    {
        private int _versionId;
        private int _nodeId;
        private VersionNumber _version;
        private DateTime _creationDate;
        private int _createdById;
        private DateTime _modificationDate;
        private int _modifiedById;
        private string _indexDocument; //UNDONE:DB --- Do not store IndexDocument in the VersionDoc
        private IEnumerable<ChangedData> _changedData;
        private long _timestamp;

        // ReSharper disable once ConvertToAutoProperty
        public int VersionId
        {
            get => _versionId;
            set => _versionId = value;
        }

        public int NodeId
        {
            get => _nodeId;
            set
            {
                _nodeId = value;
                SetTimestamp();
            }
        }

        /// <summary>
        /// Gets or sets the clone of a VersionNumber
        /// </summary>
        public VersionNumber Version
        {
            get => _version.Clone();
            set
            {
                _version = value.Clone();
                SetTimestamp();
            }
        }

        public DateTime CreationDate
        {
            get => _creationDate;
            set
            {
                _creationDate = value;
                SetTimestamp();
            }
        }

        public int CreatedById
        {
            get => _createdById;
            set
            {
                _createdById = value;
                SetTimestamp();
            }
        }

        public DateTime ModificationDate
        {
            get => _modificationDate;
            set
            {
                _modificationDate = value;
                SetTimestamp();
            }
        }

        public int ModifiedById
        {
            get => _modifiedById;
            set
            {
                _modifiedById = value;
                SetTimestamp();
            }
        }

        public string IndexDocument
        {
            get => _indexDocument;
            set
            {
                _indexDocument = value;
                SetTimestamp();
            }
        }

        public IEnumerable<ChangedData> ChangedData
        {
            get => _changedData;
            set
            {
                _changedData = value;
                SetTimestamp();
            }
        }

        public long Timestamp => _timestamp;
        //UNDONE:DB dynamic properties

        private static long _lastTimestamp;

        private void SetTimestamp()
        {
            _timestamp = Interlocked.Increment(ref _lastTimestamp);
        }

        public VersionDoc Clone()
        {
            return new VersionDoc
            {
                _versionId = _versionId,
                _nodeId = _nodeId,
                _version = _version,
                _creationDate = _creationDate,
                _createdById = _createdById,
                _modificationDate = _modificationDate,
                _modifiedById = _modifiedById,
                _indexDocument = _indexDocument,
                _changedData = _changedData?.ToArray(),
                _timestamp = _timestamp,
            };
        }
    }

    #endregion
}