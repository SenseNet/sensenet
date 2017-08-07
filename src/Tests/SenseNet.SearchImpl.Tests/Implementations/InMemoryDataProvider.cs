using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Lucene.Net.Support;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;
using SenseNet.Security;

namespace SenseNet.SearchImpl.Tests.Implementations
{
    internal partial class InMemoryDataProvider : DataProvider
    {
        #region NOT IMPLEMENTED

        public override System.Collections.Generic.Dictionary<DataType, int> ContentListMappingOffsets
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string DatabaseName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override IDataProcedureFactory DataProcedureFactory
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        public override DateTime DateTimeMaxValue => DateTime.MaxValue;

        public override DateTime DateTimeMinValue => DateTime.MinValue;

        public override decimal DecimalMaxValue => decimal.MaxValue;

        public override decimal DecimalMinValue => decimal.MinValue;

        public override int PathMaxLength => 150;

        #region NOT IMPLEMENTED

        protected internal override int ContentListStartPage
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override void AssertSchemaTimestampAndWriteModificationDate(long timestamp)
        {
            throw new NotImplementedException();
        }

        public override ContentRepository.Storage.Data.ITransactionProvider CreateTransaction()
        {
            throw new NotImplementedException();
        }

        public override void DeleteAllIndexingActivities()
        {
            throw new NotImplementedException();
        }

        public override void DeleteAllPackages()
        {
            throw new NotImplementedException();
        }

        public override void DeletePackage(Package package)
        {
            throw new NotImplementedException();
        }

        public override int GetLastActivityId()
        {
            throw new NotImplementedException();
        }

        public override string GetNameOfLastNodeWithNameBase(int parentId, string namebase, string extension)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<string> GetScriptsForDatabaseBackup()
        {
            throw new NotImplementedException();
        }

        public override SenseNet.ContentRepository.Storage.Data.IDataProcedure GetTimestampDataForOneNodeIntegrityCheck(string path, int[] excludedNodeTypeIds)
        {
            throw new NotImplementedException();
        }

        public override ContentRepository.Storage.Data.IDataProcedure GetTimestampDataForRecursiveIntegrityCheck(string path, int[] excludedNodeTypeIds)
        {
            throw new NotImplementedException();
        }

        public override bool IsPackageExist(string componentId, PackageType packageType, Version version)
        {
            throw new NotImplementedException();
        }

        public override IIndexingActivity[] LoadIndexingActivities(int[] gaps, bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory)
        {
            throw new NotImplementedException();
        }

        public override IIndexingActivity[] LoadIndexingActivities(int fromId, int toId, int count, bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<ComponentInfo> LoadInstalledComponents()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Package> LoadInstalledPackages()
        {
            throw new NotImplementedException();
        }

        public override void LoadManifest(Package package)
        {
            throw new NotImplementedException();
        }

        #endregion

        public override void RegisterIndexingActivity(IIndexingActivity activity)
        {
            var newId = IndexingActivity.Count == 0 ? 1 : IndexingActivity.Max(r => r.NodeId) + 1;

            IndexingActivity.Add(new IndexingActivityRecord
            {
                IndexingActivityId = newId,
                ActivityType = activity.ActivityType.ToString(),
                CreationDate=DateTime.UtcNow,
                NodeId = activity.NodeId,
                VersionId = activity.VersionId,
                SingleVersion = activity.SingleVersion,
                MoveOrRename = activity.MoveOrRename,
                IsLastDraftValue = activity.IsLastDraftValue,
                Path = activity.Path,
                Extension = activity.Extension
            });

            activity.Id = newId;
        }

        public override DateTime RoundDateTime(DateTime d)
        {
            return new DateTime(d.Ticks / 100000 * 100000);
        }

        #region NOT IMPLEMENTED

        public override void SavePackage(Package package)
        {
            throw new NotImplementedException();
        }

        public override void UpdatePackage(Package package)
        {
            throw new NotImplementedException();
        }

        protected internal override int AcquireTreeLock(string path)
        {
            throw new NotImplementedException();
        }

        protected internal override void CheckScriptInternal(string commandText)
        {
            throw new NotImplementedException();
        }

        protected internal override void CommitChunk(int versionId, int propertyTypeId, string token, long fullSize, ContentRepository.Storage.BinaryDataValue source = null)
        {
            throw new NotImplementedException();
        }

        protected internal override void CopyFromStream(int versionId, string token, Stream input)
        {
            throw new NotImplementedException();
        }

        protected internal override IndexBackup CreateBackup(int backupNumber)
        {
            throw new NotImplementedException();
        }

        protected internal override ContentRepository.Storage.Data.IDataProcedure CreateDataProcedureInternal(string commandText, ContentRepository.Storage.Data.ConnectionInfo connectionInfo)
        {
            throw new NotImplementedException();
        }

        protected internal override ContentRepository.Storage.Data.IDataProcedure CreateDataProcedureInternal(string commandText, string connectionName = null, ContentRepository.Storage.Data.InitialCatalog initialCatalog = 0)
        {
            throw new NotImplementedException();
        }

        protected internal override INodeWriter CreateNodeWriter()
        {
            return new InMemoryNodeWriter();
        }

        protected override System.Data.IDbDataParameter CreateParameterInternal()
        {
            throw new NotImplementedException();
        }

        protected internal override SchemaWriter CreateSchemaWriter()
        {
            throw new NotImplementedException();
        }

        protected internal override DataOperationResult DeleteNodeTree(int nodeId)
        {
            throw new NotImplementedException();
        }

        protected internal override DataOperationResult DeleteNodeTreePsychical(int nodeId, long timestamp)
        {
            throw new NotImplementedException();
        }

        protected internal override void DeleteVersion(int versionId, NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            throw new NotImplementedException();
        }

        protected override string GetAppModelScriptPrivate(IEnumerable<string> paths, bool all, bool resolveChildren)
        {
            throw new NotImplementedException();
        }

        protected internal override ContentRepository.Storage.Data.BlobStorageContext GetBlobStorageContext(int fileId, bool clearStream = false, int versionId = 0, int propertyTypeId = 0)
        {
            throw new NotImplementedException();
        }

        protected internal override IEnumerable<int> GetChildrenIdentfiers(int nodeId)
        {
            throw new NotImplementedException();
        }

        protected internal override List<ContentListType> GetContentListTypesInTree(string path)
        {
            throw new NotImplementedException();
        }

        protected internal override IEnumerable<int> GetIdsOfNodesThatDoNotHaveIndexDocument(int fromId, int toId)
        {
            throw new NotImplementedException();
        }

        protected internal override IndexDocumentData GetIndexDocumentDataFromReader(System.Data.Common.DbDataReader reader)
        {
            throw new NotImplementedException();
        }

        protected override Guid GetLastIndexBackupNumber()
        {
            throw new NotImplementedException();
        }

        protected internal override NodeHead.NodeVersion[] GetNodeVersions(int nodeId)
        {
            throw new NotImplementedException();
        }

        protected override int GetPermissionLogEntriesCountAfterMomentInternal(DateTime moment)
        {
            throw new NotImplementedException();
        }

        protected override PropertyMapping GetPropertyMappingInternal(PropertyType propType)
        {
            throw new NotImplementedException();
        }

        protected override string GetSecurityControlStringForTestsInternal()
        {
            throw new NotImplementedException();
        }

        protected internal override long GetTreeSize(string path, bool includeChildren)
        {
            throw new NotImplementedException();
        }

        protected internal override VersionNumber[] GetVersionNumbers(string path)
        {
            throw new NotImplementedException();
        }

        protected internal override VersionNumber[] GetVersionNumbers(int nodeId)
        {
            throw new NotImplementedException();
        }

        protected internal override bool HasChild(int nodeId)
        {
            throw new NotImplementedException();
        }

        protected override void InitializeForTestsPrivate()
        {
            throw new NotImplementedException();
        }

        protected internal override void InstallDefaultSecurityStructure()
        {
            throw new NotImplementedException();
        }

        protected internal override int InstanceCount(int[] nodeTypeIds)
        {
            throw new NotImplementedException();
        }

        protected internal override bool IsCacheableText(string text)
        {
            throw new NotImplementedException();
        }

        #endregion

        protected internal override bool IsTreeLocked(string path)
        {
            return false; //UNDONE:!!! TreeLock is not supported
        }

        #region NOT IMPLEMENTED

        protected override void KeepOnlyLastIndexBackup()
        {
            throw new NotImplementedException();
        }

        protected internal override System.Collections.Generic.Dictionary<int, string> LoadAllTreeLocks()
        {
            throw new NotImplementedException();
        }

        protected internal override ContentRepository.Storage.Data.BinaryCacheEntity LoadBinaryCacheEntity(int nodeVersionId, int propertyTypeId)
        {
            throw new NotImplementedException();
        }

        protected internal override byte[] LoadBinaryFragment(int fileId, long position, int count)
        {
            throw new NotImplementedException();
        }

        #endregion

        protected internal override ContentRepository.Storage.BinaryDataValue LoadBinaryPropertyValue(int versionId, int propertyTypeId)
        {
            return null; //UNDONE:!!! LoadBinaryPropertyValue is not supported
        }

        #region NOT IMPLEMENTED

        protected internal override IEnumerable<NodeType> LoadChildTypesToAllow(int sourceNodeId)
        {
            throw new NotImplementedException();
        }

        protected internal override IEnumerable<IndexDocumentData> LoadIndexDocumentByVersionId(IEnumerable<int> versionId)
        {
            throw new NotImplementedException();
        }

        #endregion

        protected internal override IndexDocumentData LoadIndexDocumentByVersionId(int versionId)
        {
            var version = Versions.FirstOrDefault(v => v.VersionId == versionId);
            if (version == null)
                return null;
            var node = Nodes.FirstOrDefault(n => n.NodeId == version.NodeId);
            if (node == null)
                return null;

            var approved = version.Version.Status == VersionStatus.Approved;
            var isLastMajor = node.LastMajorVersionId == versionId;

            var bytes = version.IndexDocument ?? new byte[0];

            return new IndexDocumentData(null, bytes)
            {
                NodeTypeId = node.NodeTypeId,
                VersionId = versionId,
                NodeId = node.NodeId,
                ParentId = node.ParentNodeId,
                Path = node.Path,
                IsSystem = node.IsSystem,
                IsLastDraft = node.LastMinorVersionId == versionId,
                IsLastPublic = approved && isLastMajor,
                //NodeTimestamp = node.Timestamp,
                //VersionTimestamp = version.Timestamp,
            };
        }

        #region NOT IMPLEMENTED

        protected internal override IEnumerable<IndexDocumentData> LoadIndexDocumentsByPath(string path, int[] excludedNodeTypes)
        {
            throw new NotImplementedException();
        }

        protected internal override IndexBackup LoadLastBackup()
        {
            throw new NotImplementedException();
        }

        #endregion

        protected internal override NodeHead LoadNodeHead(int nodeId)
        {
            return CreateNodeHead(Nodes.FirstOrDefault(r => r.NodeId == nodeId));
        }
        protected internal override NodeHead LoadNodeHead(string path)
        {
            return
                CreateNodeHead(
                    Nodes.FirstOrDefault(r => string.Equals(r.Path, path, StringComparison.InvariantCultureIgnoreCase)));
        }
        private NodeHead CreateNodeHead(NodeRecord r)
        {
            return r == null
                ? null
                : new NodeHead(r.NodeId, r.Name, r.DisplayName, r.Path, r.ParentNodeId,
                    r.NodeTypeId, r.ContentListTypeId, r.ContentListId, r.NodeCreationDate,
                    r.NodeModificationDate, r.LastMinorVersionId, r.LastMajorVersionId, r.OwnerId,
                    r.NodeCreatedById, r.NodeModifiedById, r.Index, r.LockedById, r.NodeTimestamp);
        }

        #region NOT IMPLEMENTED

        protected internal override NodeHead LoadNodeHeadByVersionId(int versionId)
        {
            throw new NotImplementedException();
        }

        protected internal override IEnumerable<NodeHead> LoadNodeHeads(IEnumerable<int> heads)
        {
            throw new NotImplementedException();
        }

        #endregion

        protected internal override void LoadNodes(System.Collections.Generic.Dictionary<int, NodeBuilder> buildersByVersionId)
        {
            foreach (var versionId in buildersByVersionId.Keys)
            {
                var version = Versions.FirstOrDefault(r => r.VersionId == versionId);
                if (version == null)
                    continue;
                var node = Nodes.FirstOrDefault(r => r.NodeId == version.NodeId);
                if (node == null)
                    continue;
                var builder = buildersByVersionId[versionId];
                builder.SetCoreAttributes(node.NodeId, node.NodeTypeId, node.ContentListId, node.ContentListTypeId,
                    node.CreatingInProgress, node.IsDeleted, node.ParentNodeId, node.Name, node.DisplayName, node.Path,
                    node.Index, node.Locked, node.LockedById, node.ETag, node.LockType, node.LockTimeout, node.LockDate,
                    node.LockToken, node.LastLockUpdate, versionId, version.Version, version.CreationDate,
                    version.CreatedById, version.ModificationDate, version.ModifiedById, node.IsSystem, node.OwnerId,
                    node.SavingState, version.ChangedData, node.NodeCreationDate, node.NodeCreatedById,
                    node.NodeModificationDate, node.NodeModifiedById, node.NodeTimestamp, version.VersionTimestamp);
            }
            foreach (var builder in buildersByVersionId.Values)
                builder.Finish();
        }

        protected internal override System.Data.DataSet LoadSchema()
        {
            var xml = new XmlDocument();
            xml.LoadXml(TestSchema);
            return SchemaRoot.BuildDataSetFromXml(xml);
        }

        #region NOT IMPLEMENTED
        protected internal override Stream LoadStream(int versionId, int propertyTypeId)
        {
            throw new NotImplementedException();
        }

        protected internal override string LoadTextPropertyValue(int versionId, int propertyTypeId)
        {
            throw new NotImplementedException();
        }

        protected internal override System.Collections.Generic.Dictionary<int, string> LoadTextPropertyValues(int versionId, int[] propertyTypeIds)
        {
            throw new NotImplementedException();
        }

        protected internal override DataOperationResult MoveNodeTree(int sourceNodeId, int targetNodeId, long sourceTimestamp = 0, long targetTimestamp = 0)
        {
            throw new NotImplementedException();
        }

        protected override int NodeCount(string path)
        {
            throw new NotImplementedException();
        }

        protected override bool NodeExistsInDatabase(string path)
        {
            throw new NotImplementedException();
        }

        protected internal override IEnumerable<int> QueryNodesByPath(string pathStart, bool orderByPath)
        {
            throw new NotImplementedException();
        }

        protected internal override IEnumerable<int> QueryNodesByReferenceAndType(string referenceName, int referredNodeId, int[] allowedTypeIds)
        {
            throw new NotImplementedException();
        }

        protected internal override IEnumerable<int> QueryNodesByType(int[] typeIds)
        {
            throw new NotImplementedException();
        }

        protected internal override IEnumerable<int> QueryNodesByTypeAndPath(int[] nodeTypeIds, string[] pathStart, bool orderByPath)
        {
            throw new NotImplementedException();
        }

        protected internal override IEnumerable<int> QueryNodesByTypeAndPath(int[] nodeTypeIds, string pathStart, bool orderByPath)
        {
            throw new NotImplementedException();
        }

        protected internal override IEnumerable<int> QueryNodesByTypeAndPathAndName(int[] nodeTypeIds, string[] pathStart, bool orderByPath, string name)
        {
            throw new NotImplementedException();
        }

        protected internal override IEnumerable<int> QueryNodesByTypeAndPathAndName(int[] nodeTypeIds, string pathStart, bool orderByPath, string name)
        {
            throw new NotImplementedException();
        }

        protected internal override IEnumerable<int> QueryNodesByTypeAndPathAndProperty(int[] nodeTypeIds, string pathStart, bool orderByPath, List<QueryPropertyData> properties)
        {
            throw new NotImplementedException();
        }

        protected override IndexBackup RecoverIndexBackup(string backupFilePath)
        {
            throw new NotImplementedException();
        }

        protected internal override void ReleaseTreeLock(int[] lockIds)
        {
            throw new NotImplementedException();
        }

        protected internal override void Reset()
        {
            throw new NotImplementedException();
        }

        protected internal override void SetActiveBackup(IndexBackup backup, IndexBackup lastBackup)
        {
            throw new NotImplementedException();
        }

        protected internal override string StartChunk(int versionId, int propertyTypeId, long fullSize)
        {
            throw new NotImplementedException();
        }

        protected internal override void StoreBackupStream(string backupFilePath, IndexBackup backup, IndexBackupProgress progress)
        {
            throw new NotImplementedException();
        }

        protected internal override void UpdateIndexDocument(int versionId, byte[] indexDocumentBytes)
        {
            throw new NotImplementedException();
        }
        #endregion

        protected internal override void UpdateIndexDocument(NodeData nodeData, byte[] indexDocumentBytes)
        {
            var versionRow = Versions.FirstOrDefault(r => r.VersionId == nodeData.VersionId);
            if (versionRow == null)
                return;
            versionRow.IndexDocument = indexDocumentBytes;
        }

        #region NOT IMPLEMENTED

        protected override int VersionCount(string path)
        {
            throw new NotImplementedException();
        }

        protected internal override void WriteChunk(int versionId, string token, byte[] buffer, long offset, long fullSize)
        {
            throw new NotImplementedException();
        }

        protected internal override AuditLogEntry[] LoadLastAuditLogEntries(int count)
        {
            throw new NotImplementedException();
        }

        #endregion

        /* ====================================================================================== */

        internal IEnumerable<StoredSecurityEntity> GetSecurityEntities()
        {
            return Nodes.Select(n => new StoredSecurityEntity
            {
                Id = n.NodeId,
                nullableOwnerId = n.OwnerId,
                nullableParentId = n.ParentNodeId
            }).ToArray();
        }

        internal static int LastNodeId
        {
            get { return Nodes.Max(n => n.NodeId); }
        }

        /* ====================================================================================== Database */

        private static readonly List<VersionRecord> Versions;
        private static readonly List<NodeRecord> Nodes;
        private static readonly List<IndexingActivityRecord> IndexingActivity = new List<IndexingActivityRecord>();

        static InMemoryDataProvider()
        {
            // SELECT NodeId, COALESCE(ParentNodeId, 0) ParentNodeId, NodeTypeId, LastMajorVersionId, LastMinorVersionId, [Index], IsSystem, Name, COALESCE(DisplayName, '""""'), [Path] FROM Nodes
            var skip = _initialNodes.StartsWith("NodeId") ? 1 : 0;
            Nodes = _initialNodes.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Skip(skip)
                .Select(l =>
                {
                    var record = l.Split('\t');
                    return new NodeRecord
                    {
                        NodeId = int.Parse(record[0]),
                        ParentNodeId = int.Parse(record[1]),
                        NodeTypeId = int.Parse(record[2]),
                        LastMajorVersionId = int.Parse(record[3]),
                        LastMinorVersionId = int.Parse(record[4]),
                        Index = int.Parse(record[5]),
                        IsSystem = record[6] == "1",
                        Name = record[7],
                        DisplayName = record[8] == "\"\"\"\"" ? null : record[8],
                        Path = record[9],
                        NodeCreatedById = 1,
                        NodeModifiedById = 1,
                        OwnerId = 1
                    };
                }).ToList();

            Versions = Nodes.Select(n => new VersionRecord
            {
                VersionId = n.LastMajorVersionId,
                NodeId = n.NodeId,
                Version = VersionNumber.Parse("1.0.A"),
                CreatedById = 1,
                ModifiedById = 1
            }).ToList();
        }

        #region Implementation classes

        private class InMemoryNodeWriter : INodeWriter
        {
            public void Open()
            {
                // do nothing
            }
            public void Close()
            {
                // do nothing
            }
            public void InsertNodeAndVersionRows(NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId)
            {
                var newNodeId = Nodes.Max(r => r.NodeId) + 1;
                var newVersionId = Versions.Max(r => r.NodeId) + 1;
                lastMinorVersionId = newVersionId;
                lastMajorVersionId = nodeData.Version.IsMajor ? newVersionId : 0;
                var nodeTimeStamp = 0L; //TODO:! InMemoryDataProvider: timestamp not supported
                var versionTimestamp = 0L; //TODO:! InMemoryDataProvider: timestamp not supported
                Nodes.Add(new NodeRecord
                {
                    NodeId = newNodeId,
                    NodeTypeId = nodeData.NodeTypeId,
                    ContentListTypeId = nodeData.ContentListTypeId,
                    ContentListId = nodeData.ContentListId,
                    CreatingInProgress = nodeData.CreatingInProgress,
                    IsDeleted = nodeData.IsDeleted,
                    ParentNodeId = nodeData.ParentId,
                    Name = nodeData.Name,
                    DisplayName = nodeData.DisplayName,
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
                    LastMinorVersionId = lastMinorVersionId,
                    LastMajorVersionId = lastMajorVersionId,
                    NodeCreationDate = nodeData.CreationDate,
                    NodeCreatedById = nodeData.CreatedById,
                    NodeModificationDate = nodeData.ModificationDate,
                    NodeModifiedById = nodeData.ModifiedById,
                    IsSystem = nodeData.IsSystem,
                    OwnerId = nodeData.OwnerId,
                    SavingState = nodeData.SavingState,
                    NodeTimestamp = nodeTimeStamp
                });
                Versions.Add(new VersionRecord
                {
                    VersionId = newVersionId,
                    NodeId = newNodeId,
                    Version = nodeData.Version,
                    CreationDate = nodeData.VersionCreationDate,
                    CreatedById = nodeData.VersionCreatedById,
                    ModificationDate = nodeData.VersionModificationDate,
                    ModifiedById = nodeData.VersionModifiedById,
                    ChangedData = nodeData.ChangedData,
                    VersionTimestamp = versionTimestamp
                });
                nodeData.Id = newNodeId;
                nodeData.VersionId = newVersionId;
                nodeData.NodeTimestamp = nodeTimeStamp;
                nodeData.VersionTimestamp = versionTimestamp;
            }
            public void UpdateSubTreePath(string oldPath, string newPath)
            {
                throw new NotImplementedException();
            }
            public void UpdateNodeRow(NodeData nodeData)
            {
                throw new NotImplementedException();
            }
            public void UpdateVersionRow(NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId)
            {
                throw new NotImplementedException();
            }
            public void CopyAndUpdateVersion(NodeData nodeData, int previousVersionId, out int lastMajorVersionId,
                out int lastMinorVersionId)
            {
                throw new NotImplementedException();
            }
            public void CopyAndUpdateVersion(NodeData nodeData, int previousVersionId, int destinationVersionId, out int lastMajorVersionId,
                out int lastMinorVersionId)
            {
                throw new NotImplementedException();
            }
            public void SaveStringProperty(int versionId, PropertyType propertyType, string value)
            {
                //TODO:! InMemoryDataProvider: dynamic property not supported
            }
            public void SaveDateTimeProperty(int versionId, PropertyType propertyType, DateTime value)
            {
                //TODO:! InMemoryDataProvider: dynamic property not supported
            }
            public void SaveIntProperty(int versionId, PropertyType propertyType, int value)
            {
                //TODO:! InMemoryDataProvider: dynamic property not supported
            }
            public void SaveCurrencyProperty(int versionId, PropertyType propertyType, decimal value)
            {
                //TODO:! InMemoryDataProvider: dynamic property not supported
            }

            public void SaveTextProperty(int versionId, PropertyType propertyType, bool isLoaded, string value)
            {
                //TODO:! InMemoryDataProvider: dynamic property not supported
            }
            public void SaveReferenceProperty(int versionId, PropertyType propertyType, IEnumerable<int> value)
            {
                //TODO:! InMemoryDataProvider: dynamic property not supported
            }
            public void InsertBinaryProperty(BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode)
            {
                //TODO:! InMemoryDataProvider: dynamic property not supported
            }
            public void UpdateBinaryProperty(BinaryDataValue value)
            {
                //TODO:! InMemoryDataProvider: dynamic property not supported
            }
            public void DeleteBinaryProperty(int versionId, PropertyType propertyType)
            {
                //TODO:! InMemoryDataProvider: dynamic property not supported
            }
        }

        #endregion
        #region Data classes
        private class NodeRecord
        {
            public int NodeId;
            public int NodeTypeId;
            public int ContentListTypeId;
            public int ContentListId;
            public bool CreatingInProgress;
            public bool IsDeleted;
            //public bool IsInherited; 
            public int ParentNodeId;
            public string Name;
            public string DisplayName;
            public string Path;
            public int Index;
            public bool Locked;
            public int LockedById;
            public string ETag;
            public int LockType;
            public int LockTimeout;
            public DateTime LockDate;
            public string LockToken;
            public DateTime LastLockUpdate;
            public int LastMinorVersionId;
            public int LastMajorVersionId;
            public DateTime NodeCreationDate;
            public int NodeCreatedById;
            public DateTime NodeModificationDate;
            public int NodeModifiedById;
            public bool IsSystem;
            public int OwnerId;
            public ContentSavingState SavingState;
            public long NodeTimestamp;
        }
        private class VersionRecord
        {
            public int VersionId;
            public int NodeId;
            public VersionNumber Version;
            public DateTime CreationDate;
            public int CreatedById;
            public DateTime ModificationDate;
            public int ModifiedById;
            public byte[] IndexDocument;
            public IEnumerable<ChangedData> ChangedData;
            public long VersionTimestamp;
        }
        private class IndexingActivityRecord
        {
            public int IndexingActivityId;
            public string ActivityType;
            public DateTime CreationDate;
            public int NodeId;
            public int VersionId;
            public bool? SingleVersion;
            public bool? MoveOrRename;
            public bool? IsLastDraftValue;
            public string Path;
            public string Extension;
        }
        #endregion
    }
}
