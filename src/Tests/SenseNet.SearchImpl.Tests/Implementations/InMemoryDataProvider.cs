using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;

namespace SenseNet.SearchImpl.Tests.Implementations
{
    internal class InMemoryDataProvider : DataProvider
    {
        #region NOT IMPLEMENTED

        public override Dictionary<DataType, int> ContentListMappingOffsets
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
            //UNDONE:!!!!! RegisterIndexingActivity or not
            // do nothing
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
            return false;
        }

        #region NOT IMPLEMENTED

        protected override void KeepOnlyLastIndexBackup()
        {
            throw new NotImplementedException();
        }

        protected internal override Dictionary<int, string> LoadAllTreeLocks()
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
            return null;
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

        protected internal override IndexDocumentData LoadIndexDocumentByVersionId(int versionId)
        {
            throw new NotImplementedException();
        }

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
            return _nodes
                .Where(r => r.NodeId == nodeId)
                .Select(r => new NodeHead(r.NodeId, r.Name, r.DisplayName, r.Path, r.ParentNodeId,
                    r.NodeTypeId, r.ContentListTypeId, r.ContentListId, r.NodeCreationDate,
                    r.NodeModificationDate, r.LastMinorVersionId, r.LastMajorVersionId, r.OwnerId,
                    r.NodeCreatedById, r.NodeModifiedById, r.Index, r.LockedById, r.NodeTimestamp))
                .FirstOrDefault();
        }

        #region NOT IMPLEMENTED

        protected internal override NodeHead LoadNodeHead(string path)
        {
            throw new NotImplementedException();
        }

        protected internal override NodeHead LoadNodeHeadByVersionId(int versionId)
        {
            throw new NotImplementedException();
        }

        protected internal override IEnumerable<NodeHead> LoadNodeHeads(IEnumerable<int> heads)
        {
            throw new NotImplementedException();
        }

        #endregion

        protected internal override void LoadNodes(Dictionary<int, NodeBuilder> buildersByVersionId)
        {
            foreach (var versionId in buildersByVersionId.Keys)
            {
                var version = _versions.FirstOrDefault(r => r.VersionId == versionId);
                if (version == null)
                    continue;
                var node = _nodes.FirstOrDefault(r => r.NodeId == version.NodeId);
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

        protected internal override Dictionary<int, string> LoadTextPropertyValues(int versionId, int[] propertyTypeIds)
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
            var versionRow = _versions.FirstOrDefault(r => r.VersionId == nodeData.VersionId);
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

        #region implementation classes

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
                var newNodeId = _nodes.Max(r => r.NodeId) + 1;
                var newVersionId = _versions.Max(r => r.NodeId) + 1;
                lastMinorVersionId = newVersionId;
                lastMajorVersionId = nodeData.Version.IsMajor ? newVersionId : 0;
                var nodeTimeStamp = 0L; //TODO:! InMemoryDataProvider: timestamp not supported
                var versionTimestamp = 0L; //TODO:! InMemoryDataProvider: timestamp not supported
                _nodes.Add(new NodeRecord
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
                _versions.Add(new VersionRecord
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
        #endregion

        #region SCHEMA

        private static readonly string TestSchema = @"<?xml version='1.0' encoding='utf-8' ?>
<StorageSchema xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/Storage/Schema'>
	<UsedPropertyTypes>
		<PropertyType itemID='1' name='Binary' dataType='Binary' mapping='0' />
		<PropertyType itemID='2' name='VersioningMode' dataType='Int' mapping='0' />
		<PropertyType itemID='3' name='Description' dataType='Text' mapping='0' />
		<PropertyType itemID='4' name='Hidden' dataType='Int' mapping='1' />
		<PropertyType itemID='5' name='InheritableVersioningMode' dataType='Int' mapping='2' />
		<PropertyType itemID='6' name='ApprovingMode' dataType='Int' mapping='3' />
		<PropertyType itemID='7' name='InheritableApprovingMode' dataType='Int' mapping='4' />
		<PropertyType itemID='8' name='AllowedChildTypes' dataType='Text' mapping='1' />
		<PropertyType itemID='9' name='TrashDisabled' dataType='Int' mapping='5' />
		<PropertyType itemID='10' name='EnableLifespan' dataType='Int' mapping='6' />
		<PropertyType itemID='11' name='ValidFrom' dataType='DateTime' mapping='0' />
		<PropertyType itemID='12' name='ValidTill' dataType='DateTime' mapping='1' />
		<PropertyType itemID='13' name='Aspects' dataType='Reference' mapping='0' />
		<PropertyType itemID='14' name='AspectData' dataType='Text' mapping='2' />
		<PropertyType itemID='15' name='BrowseApplication' dataType='Reference' mapping='1' />
		<PropertyType itemID='16' name='ExtensionData' dataType='Text' mapping='3' />
		<PropertyType itemID='17' name='IsTaggable' dataType='Int' mapping='7' />
		<PropertyType itemID='18' name='Tags' dataType='Text' mapping='4' />
		<PropertyType itemID='19' name='IsRateable' dataType='Int' mapping='8' />
		<PropertyType itemID='20' name='RateStr' dataType='String' mapping='0' />
		<PropertyType itemID='21' name='RateAvg' dataType='Currency' mapping='0' />
		<PropertyType itemID='22' name='RateCount' dataType='Int' mapping='9' />
		<PropertyType itemID='23' name='CheckInComments' dataType='Text' mapping='5' />
		<PropertyType itemID='24' name='RejectReason' dataType='Text' mapping='6' />
		<PropertyType itemID='25' name='SyncGuid' dataType='String' mapping='1' />
		<PropertyType itemID='26' name='LastSync' dataType='DateTime' mapping='2' />
		<PropertyType itemID='27' name='Watermark' dataType='String' mapping='2' />
		<PropertyType itemID='28' name='PageCount' dataType='Int' mapping='10' />
		<PropertyType itemID='29' name='MimeType' dataType='String' mapping='3' />
		<PropertyType itemID='30' name='Shapes' dataType='Text' mapping='7' />
		<PropertyType itemID='31' name='PageAttributes' dataType='Text' mapping='8' />
		<PropertyType itemID='32' name='GlobalOnly' dataType='Int' mapping='11' />
		<PropertyType itemID='33' name='AppName' dataType='String' mapping='4' />
		<PropertyType itemID='34' name='Disabled' dataType='Int' mapping='12' />
		<PropertyType itemID='35' name='IsModal' dataType='Int' mapping='13' />
		<PropertyType itemID='36' name='Clear' dataType='Int' mapping='14' />
		<PropertyType itemID='37' name='Scenario' dataType='String' mapping='5' />
		<PropertyType itemID='38' name='ActionTypeName' dataType='String' mapping='6' />
		<PropertyType itemID='39' name='StyleHint' dataType='String' mapping='7' />
		<PropertyType itemID='40' name='RequiredPermissions' dataType='String' mapping='8' />
		<PropertyType itemID='41' name='DeepPermissionCheck' dataType='Int' mapping='15' />
		<PropertyType itemID='42' name='IncludeBackUrl' dataType='String' mapping='9' />
		<PropertyType itemID='43' name='CacheControl' dataType='String' mapping='10' />
		<PropertyType itemID='44' name='MaxAge' dataType='String' mapping='11' />
		<PropertyType itemID='45' name='CustomUrlParameters' dataType='String' mapping='12' />
		<PropertyType itemID='46' name='StoredIcon' dataType='String' mapping='13' />
		<PropertyType itemID='47' name='WorkflowStatus' dataType='String' mapping='14' />
		<PropertyType itemID='48' name='WorkflowDefinitionVersion' dataType='String' mapping='15' />
		<PropertyType itemID='49' name='WorkflowInstanceGuid' dataType='String' mapping='16' />
		<PropertyType itemID='50' name='RelatedContent' dataType='Reference' mapping='2' />
		<PropertyType itemID='51' name='RelatedContentTimestamp' dataType='Currency' mapping='1' />
		<PropertyType itemID='52' name='SystemMessages' dataType='Text' mapping='9' />
		<PropertyType itemID='53' name='AllowManualStart' dataType='Int' mapping='16' />
		<PropertyType itemID='54' name='AutostartOnPublished' dataType='Int' mapping='17' />
		<PropertyType itemID='55' name='AutostartOnCreated' dataType='Int' mapping='18' />
		<PropertyType itemID='56' name='AutostartOnChanged' dataType='Int' mapping='19' />
		<PropertyType itemID='57' name='ContentWorkflow' dataType='Int' mapping='20' />
		<PropertyType itemID='58' name='AbortOnRelatedContentChange' dataType='Int' mapping='21' />
		<PropertyType itemID='59' name='OwnerSiteUrl' dataType='String' mapping='17' />
		<PropertyType itemID='60' name='FirstLevelTimeFrame' dataType='String' mapping='18' />
		<PropertyType itemID='61' name='SecondLevelTimeFrame' dataType='String' mapping='19' />
		<PropertyType itemID='62' name='FirstLevelApprover' dataType='Reference' mapping='3' />
		<PropertyType itemID='63' name='SecondLevelApprover' dataType='Reference' mapping='4' />
		<PropertyType itemID='64' name='WaitForAll' dataType='Int' mapping='22' />
		<PropertyType itemID='65' name='StartDate' dataType='DateTime' mapping='3' />
		<PropertyType itemID='66' name='DueDate' dataType='DateTime' mapping='4' />
		<PropertyType itemID='67' name='AssignedTo' dataType='Reference' mapping='5' />
		<PropertyType itemID='68' name='Priority' dataType='String' mapping='20' />
		<PropertyType itemID='69' name='Status' dataType='String' mapping='21' />
		<PropertyType itemID='70' name='TaskCompletion' dataType='Int' mapping='23' />
		<PropertyType itemID='71' name='Comment' dataType='String' mapping='22' />
		<PropertyType itemID='72' name='Result' dataType='String' mapping='23' />
		<PropertyType itemID='73' name='ContentToApprove' dataType='Reference' mapping='6' />
		<PropertyType itemID='74' name='ReviewDate' dataType='DateTime' mapping='5' />
		<PropertyType itemID='75' name='ArchiveDate' dataType='DateTime' mapping='6' />
		<PropertyType itemID='76' name='Subtitle' dataType='String' mapping='24' />
		<PropertyType itemID='77' name='Lead' dataType='Text' mapping='10' />
		<PropertyType itemID='78' name='Body' dataType='Text' mapping='11' />
		<PropertyType itemID='79' name='Pinned' dataType='Int' mapping='24' />
		<PropertyType itemID='80' name='Keywords' dataType='Text' mapping='12' />
		<PropertyType itemID='81' name='Author' dataType='String' mapping='25' />
		<PropertyType itemID='82' name='ImageRef' dataType='Reference' mapping='7' />
		<PropertyType itemID='83' name='ImageData' dataType='Binary' mapping='1' />
		<PropertyType itemID='84' name='ContentListBindings' dataType='Text' mapping='13' />
		<PropertyType itemID='85' name='ContentListDefinition' dataType='Text' mapping='14' />
		<PropertyType itemID='86' name='DefaultView' dataType='String' mapping='26' />
		<PropertyType itemID='87' name='AvailableViews' dataType='Reference' mapping='8' />
		<PropertyType itemID='88' name='AvailableContentTypeFields' dataType='Reference' mapping='9' />
		<PropertyType itemID='89' name='ListEmail' dataType='String' mapping='27' />
		<PropertyType itemID='90' name='ExchangeSubscriptionId' dataType='String' mapping='28' />
		<PropertyType itemID='91' name='OverwriteFiles' dataType='Int' mapping='25' />
		<PropertyType itemID='92' name='GroupAttachments' dataType='String' mapping='29' />
		<PropertyType itemID='93' name='SaveOriginalEmail' dataType='Int' mapping='26' />
		<PropertyType itemID='94' name='IncomingEmailWorkflow' dataType='Reference' mapping='10' />
		<PropertyType itemID='95' name='OnlyFromLocalGroups' dataType='Int' mapping='27' />
		<PropertyType itemID='96' name='InboxFolder' dataType='String' mapping='30' />
		<PropertyType itemID='97' name='OwnerWhenVisitor' dataType='Reference' mapping='11' />
		<PropertyType itemID='98' name='AspectDefinition' dataType='Text' mapping='15' />
		<PropertyType itemID='99' name='FieldSettingContents' dataType='Reference' mapping='12' />
		<PropertyType itemID='100' name='IsActive' dataType='Int' mapping='28' />
		<PropertyType itemID='101' name='IsWallContainer' dataType='Int' mapping='29' />
		<PropertyType itemID='102' name='WorkspaceSkin' dataType='Reference' mapping='13' />
		<PropertyType itemID='103' name='Manager' dataType='Reference' mapping='14' />
		<PropertyType itemID='104' name='Deadline' dataType='DateTime' mapping='7' />
		<PropertyType itemID='105' name='IsCritical' dataType='Int' mapping='30' />
		<PropertyType itemID='106' name='ShowAvatar' dataType='Int' mapping='31' />
		<PropertyType itemID='107' name='PublishedOn' dataType='DateTime' mapping='8' />
		<PropertyType itemID='108' name='LeadingText' dataType='Text' mapping='16' />
		<PropertyType itemID='109' name='BodyText' dataType='Text' mapping='17' />
		<PropertyType itemID='110' name='IsPublished' dataType='Int' mapping='32' />
		<PropertyType itemID='111' name='RegistrationForm' dataType='Reference' mapping='15' />
		<PropertyType itemID='112' name='Location' dataType='String' mapping='31' />
		<PropertyType itemID='113' name='EndDate' dataType='DateTime' mapping='9' />
		<PropertyType itemID='114' name='AllDay' dataType='Int' mapping='33' />
		<PropertyType itemID='115' name='EventUrl' dataType='String' mapping='32' />
		<PropertyType itemID='116' name='RequiresRegistration' dataType='Int' mapping='34' />
		<PropertyType itemID='117' name='OwnerEmail' dataType='String' mapping='33' />
		<PropertyType itemID='118' name='NotificationMode' dataType='String' mapping='34' />
		<PropertyType itemID='119' name='EmailTemplate' dataType='Text' mapping='18' />
		<PropertyType itemID='120' name='EmailTemplateSubmitter' dataType='Text' mapping='19' />
		<PropertyType itemID='121' name='EmailFrom' dataType='String' mapping='35' />
		<PropertyType itemID='122' name='EmailFromSubmitter' dataType='String' mapping='36' />
		<PropertyType itemID='123' name='EmailField' dataType='String' mapping='37' />
		<PropertyType itemID='124' name='MaxParticipants' dataType='Int' mapping='35' />
		<PropertyType itemID='125' name='EventType' dataType='String' mapping='38' />
		<PropertyType itemID='126' name='Make' dataType='String' mapping='39' />
		<PropertyType itemID='127' name='Model' dataType='String' mapping='40' />
		<PropertyType itemID='128' name='Style' dataType='String' mapping='41' />
		<PropertyType itemID='129' name='StartingDate' dataType='DateTime' mapping='10' />
		<PropertyType itemID='130' name='Color' dataType='String' mapping='42' />
		<PropertyType itemID='131' name='EngineSize' dataType='String' mapping='43' />
		<PropertyType itemID='132' name='Power' dataType='String' mapping='44' />
		<PropertyType itemID='133' name='Price' dataType='Currency' mapping='2' />
		<PropertyType itemID='134' name='Confirmed' dataType='Int' mapping='36' />
		<PropertyType itemID='135' name='Link' dataType='Reference' mapping='16' />
		<PropertyType itemID='136' name='Query' dataType='Text' mapping='20' />
		<PropertyType itemID='137' name='EnableAutofilters' dataType='String' mapping='45' />
		<PropertyType itemID='138' name='EnableLifespanFilter' dataType='String' mapping='46' />
		<PropertyType itemID='139' name='SelectionMode' dataType='String' mapping='47' />
		<PropertyType itemID='140' name='OrderingMode' dataType='String' mapping='48' />
		<PropertyType itemID='141' name='ContractId' dataType='String' mapping='49' />
		<PropertyType itemID='142' name='Project' dataType='Reference' mapping='17' />
		<PropertyType itemID='143' name='Language' dataType='String' mapping='50' />
		<PropertyType itemID='144' name='Responsee' dataType='Reference' mapping='18' />
		<PropertyType itemID='145' name='Lawyer' dataType='String' mapping='51' />
		<PropertyType itemID='146' name='RelatedDocs' dataType='Reference' mapping='19' />
		<PropertyType itemID='147' name='WorkflowsRunning' dataType='Int' mapping='37' />
		<PropertyType itemID='148' name='UserAgentPattern' dataType='String' mapping='52' />
		<PropertyType itemID='149' name='StartIndex' dataType='Int' mapping='38' />
		<PropertyType itemID='150' name='ContentVersion' dataType='String' mapping='53' />
		<PropertyType itemID='151' name='From' dataType='String' mapping='54' />
		<PropertyType itemID='152' name='Sent' dataType='DateTime' mapping='11' />
		<PropertyType itemID='153' name='RegistrationFolder' dataType='Reference' mapping='20' />
		<PropertyType itemID='154' name='EmailList' dataType='Text' mapping='21' />
		<PropertyType itemID='155' name='TitleSubmitter' dataType='String' mapping='55' />
		<PropertyType itemID='156' name='AfterSubmitText' dataType='Text' mapping='22' />
		<PropertyType itemID='157' name='Email' dataType='String' mapping='56' />
		<PropertyType itemID='158' name='GuestNumber' dataType='Int' mapping='39' />
		<PropertyType itemID='159' name='Amount' dataType='Currency' mapping='3' />
		<PropertyType itemID='160' name='Date' dataType='DateTime' mapping='12' />
		<PropertyType itemID='161' name='CEO' dataType='Reference' mapping='21' />
		<PropertyType itemID='162' name='BudgetLimit' dataType='Int' mapping='40' />
		<PropertyType itemID='163' name='FinanceEmail' dataType='String' mapping='57' />
		<PropertyType itemID='164' name='Reason' dataType='Text' mapping='23' />
		<PropertyType itemID='165' name='ExpenseClaim' dataType='Reference' mapping='22' />
		<PropertyType itemID='166' name='Sum' dataType='Int' mapping='41' />
		<PropertyType itemID='167' name='EmailForPassword' dataType='String' mapping='58' />
		<PropertyType itemID='168' name='ReplyTo' dataType='Reference' mapping='23' />
		<PropertyType itemID='169' name='PostedBy' dataType='Reference' mapping='24' />
		<PropertyType itemID='170' name='SerialNo' dataType='Int' mapping='42' />
		<PropertyType itemID='171' name='ClassName' dataType='String' mapping='59' />
		<PropertyType itemID='172' name='MethodName' dataType='String' mapping='60' />
		<PropertyType itemID='173' name='Parameters' dataType='Text' mapping='24' />
		<PropertyType itemID='174' name='ListHidden' dataType='Int' mapping='43' />
		<PropertyType itemID='175' name='SiteUrl' dataType='String' mapping='61' />
		<PropertyType itemID='176' name='Members' dataType='Reference' mapping='25' />
		<PropertyType itemID='177' name='HTMLFragment' dataType='Text' mapping='25' />
		<PropertyType itemID='178' name='A' dataType='Int' mapping='44' />
		<PropertyType itemID='179' name='B' dataType='Int' mapping='45' />
		<PropertyType itemID='180' name='StatusCode' dataType='String' mapping='62' />
		<PropertyType itemID='181' name='RedirectUrl' dataType='String' mapping='63' />
		<PropertyType itemID='182' name='Width' dataType='Int' mapping='46' />
		<PropertyType itemID='183' name='Height' dataType='Int' mapping='47' />
		<PropertyType itemID='184' name='DateTaken' dataType='DateTime' mapping='13' />
		<PropertyType itemID='185' name='CoverImage' dataType='Reference' mapping='26' />
		<PropertyType itemID='186' name='ImageType' dataType='String' mapping='64' />
		<PropertyType itemID='187' name='ImageFieldName' dataType='String' mapping='65' />
		<PropertyType itemID='188' name='Stretch' dataType='Int' mapping='48' />
		<PropertyType itemID='189' name='OutputFormat' dataType='String' mapping='66' />
		<PropertyType itemID='190' name='SmoothingMode' dataType='String' mapping='67' />
		<PropertyType itemID='191' name='InterpolationMode' dataType='String' mapping='68' />
		<PropertyType itemID='192' name='PixelOffsetMode' dataType='String' mapping='69' />
		<PropertyType itemID='193' name='ResizeTypeMode' dataType='String' mapping='70' />
		<PropertyType itemID='194' name='CropVAlign' dataType='String' mapping='71' />
		<PropertyType itemID='195' name='CropHAlign' dataType='String' mapping='72' />
		<PropertyType itemID='196' name='KPIData' dataType='Text' mapping='26' />
		<PropertyType itemID='197' name='Url' dataType='String' mapping='73' />
		<PropertyType itemID='198' name='Template' dataType='Reference' mapping='27' />
		<PropertyType itemID='199' name='FilterXml' dataType='Text' mapping='27' />
		<PropertyType itemID='200' name='QueryTop' dataType='Int' mapping='49' />
		<PropertyType itemID='201' name='QuerySkip' dataType='Int' mapping='50' />
		<PropertyType itemID='202' name='Icon' dataType='String' mapping='74' />
		<PropertyType itemID='203' name='Columns' dataType='Text' mapping='28' />
		<PropertyType itemID='204' name='SortBy' dataType='String' mapping='75' />
		<PropertyType itemID='205' name='GroupBy' dataType='String' mapping='76' />
		<PropertyType itemID='206' name='Flat' dataType='Int' mapping='51' />
		<PropertyType itemID='207' name='MainScenario' dataType='String' mapping='77' />
		<PropertyType itemID='208' name='MemoType' dataType='String' mapping='78' />
		<PropertyType itemID='209' name='SeeAlso' dataType='Reference' mapping='28' />
		<PropertyType itemID='210' name='Subject' dataType='String' mapping='79' />
		<PropertyType itemID='211' name='SenderAddress' dataType='String' mapping='80' />
		<PropertyType itemID='212' name='CompanyName' dataType='String' mapping='81' />
		<PropertyType itemID='213' name='OrderFormId' dataType='String' mapping='82' />
		<PropertyType itemID='214' name='CompanySeat' dataType='Text' mapping='29' />
		<PropertyType itemID='215' name='RepresentedBy' dataType='String' mapping='83' />
		<PropertyType itemID='216' name='ContactEmailAddress' dataType='String' mapping='84' />
		<PropertyType itemID='217' name='ContactPhoneNr' dataType='String' mapping='85' />
		<PropertyType itemID='218' name='MetaTitle' dataType='String' mapping='86' />
		<PropertyType itemID='219' name='MetaDescription' dataType='Text' mapping='30' />
		<PropertyType itemID='220' name='MetaAuthors' dataType='String' mapping='87' />
		<PropertyType itemID='221' name='CustomMeta' dataType='Text' mapping='31' />
		<PropertyType itemID='222' name='PageTemplateNode' dataType='Reference' mapping='29' />
		<PropertyType itemID='223' name='PersonalizationSettings' dataType='Binary' mapping='2' />
		<PropertyType itemID='224' name='TemporaryPortletInfo' dataType='Text' mapping='32' />
		<PropertyType itemID='225' name='TextExtract' dataType='Text' mapping='33' />
		<PropertyType itemID='226' name='SmartUrl' dataType='String' mapping='88' />
		<PropertyType itemID='227' name='PageSkin' dataType='Reference' mapping='30' />
		<PropertyType itemID='228' name='HasTemporaryPortletInfo' dataType='Int' mapping='52' />
		<PropertyType itemID='229' name='IsExternal' dataType='Int' mapping='53' />
		<PropertyType itemID='230' name='OuterUrl' dataType='String' mapping='89' />
		<PropertyType itemID='231' name='PageId' dataType='String' mapping='90' />
		<PropertyType itemID='232' name='NodeName' dataType='String' mapping='91' />
		<PropertyType itemID='233' name='MasterPageNode' dataType='Reference' mapping='31' />
		<PropertyType itemID='234' name='TypeName' dataType='String' mapping='92' />
		<PropertyType itemID='235' name='JournalId' dataType='Int' mapping='54' />
		<PropertyType itemID='236' name='PostType' dataType='Int' mapping='55' />
		<PropertyType itemID='237' name='SharedContent' dataType='Reference' mapping='32' />
		<PropertyType itemID='238' name='PostDetails' dataType='Text' mapping='34' />
		<PropertyType itemID='239' name='Completion' dataType='Currency' mapping='4' />
		<PropertyType itemID='240' name='SecurityGroups' dataType='Reference' mapping='33' />
		<PropertyType itemID='241' name='DefaultDomainPath' dataType='Reference' mapping='34' />
		<PropertyType itemID='242' name='UserTypeName' dataType='String' mapping='93' />
		<PropertyType itemID='243' name='DuplicateErrorMessage' dataType='Text' mapping='35' />
		<PropertyType itemID='244' name='IsBodyHtml' dataType='Int' mapping='56' />
		<PropertyType itemID='245' name='ActivationEnabled' dataType='Int' mapping='57' />
		<PropertyType itemID='246' name='ActivationEmailTemplate' dataType='Text' mapping='36' />
		<PropertyType itemID='247' name='ActivationSuccessTemplate' dataType='Text' mapping='37' />
		<PropertyType itemID='248' name='AlreadyActivatedMessage' dataType='Text' mapping='38' />
		<PropertyType itemID='249' name='MailSubjectTemplate' dataType='Text' mapping='39' />
		<PropertyType itemID='250' name='MailFrom' dataType='String' mapping='94' />
		<PropertyType itemID='251' name='AdminEmailAddress' dataType='String' mapping='95' />
		<PropertyType itemID='252' name='RegistrationSuccessTemplate' dataType='Text' mapping='40' />
		<PropertyType itemID='253' name='ResetPasswordTemplate' dataType='Text' mapping='41' />
		<PropertyType itemID='254' name='ResetPasswordSubjectTemplate' dataType='String' mapping='96' />
		<PropertyType itemID='255' name='ResetPasswordSuccessfulTemplate' dataType='Text' mapping='42' />
		<PropertyType itemID='256' name='ChangePasswordUserInterfacePath' dataType='String' mapping='97' />
		<PropertyType itemID='257' name='ChangePasswordSuccessfulMessage' dataType='String' mapping='98' />
		<PropertyType itemID='258' name='ForgottenPasswordUserInterfacePath' dataType='String' mapping='99' />
		<PropertyType itemID='259' name='NewRegistrationContentView' dataType='String' mapping='100' />
		<PropertyType itemID='260' name='EditProfileContentView' dataType='String' mapping='101' />
		<PropertyType itemID='261' name='AutoGeneratePassword' dataType='Int' mapping='58' />
		<PropertyType itemID='262' name='DisableCreatedUser' dataType='Int' mapping='59' />
		<PropertyType itemID='263' name='IsUniqueEmail' dataType='Int' mapping='60' />
		<PropertyType itemID='264' name='AutomaticLogon' dataType='Int' mapping='61' />
		<PropertyType itemID='265' name='ChangePasswordPagePath' dataType='Reference' mapping='35' />
		<PropertyType itemID='266' name='ChangePasswordRestrictedText' dataType='Text' mapping='43' />
		<PropertyType itemID='267' name='AlreadyRegisteredUserMessage' dataType='Text' mapping='44' />
		<PropertyType itemID='268' name='UpdateProfileSuccessTemplate' dataType='Text' mapping='45' />
		<PropertyType itemID='269' name='EmailNotValid' dataType='Text' mapping='46' />
		<PropertyType itemID='270' name='NoEmailGiven' dataType='Text' mapping='47' />
		<PropertyType itemID='271' name='ActivateByAdmin' dataType='Int' mapping='62' />
		<PropertyType itemID='272' name='ActivateEmailSubject' dataType='String' mapping='102' />
		<PropertyType itemID='273' name='ActivateEmailTemplate' dataType='Text' mapping='48' />
		<PropertyType itemID='274' name='ActivateAdmins' dataType='Reference' mapping='36' />
		<PropertyType itemID='275' name='Enabled' dataType='Int' mapping='63' />
		<PropertyType itemID='276' name='Domain' dataType='String' mapping='103' />
		<PropertyType itemID='277' name='FullName' dataType='String' mapping='104' />
		<PropertyType itemID='278' name='OldPasswords' dataType='Text' mapping='49' />
		<PropertyType itemID='279' name='PasswordHash' dataType='String' mapping='105' />
		<PropertyType itemID='280' name='LoginName' dataType='String' mapping='106' />
		<PropertyType itemID='281' name='FollowedWorkspaces' dataType='Reference' mapping='37' />
		<PropertyType itemID='282' name='JobTitle' dataType='String' mapping='107' />
		<PropertyType itemID='283' name='Captcha' dataType='String' mapping='108' />
		<PropertyType itemID='284' name='Department' dataType='String' mapping='109' />
		<PropertyType itemID='285' name='Languages' dataType='String' mapping='110' />
		<PropertyType itemID='286' name='Phone' dataType='String' mapping='111' />
		<PropertyType itemID='287' name='Gender' dataType='String' mapping='112' />
		<PropertyType itemID='288' name='MaritalStatus' dataType='String' mapping='113' />
		<PropertyType itemID='289' name='BirthDate' dataType='DateTime' mapping='14' />
		<PropertyType itemID='290' name='Education' dataType='Text' mapping='50' />
		<PropertyType itemID='291' name='TwitterAccount' dataType='String' mapping='114' />
		<PropertyType itemID='292' name='FacebookURL' dataType='String' mapping='115' />
		<PropertyType itemID='293' name='LinkedInURL' dataType='String' mapping='116' />
		<PropertyType itemID='294' name='ResetKey' dataType='String' mapping='117' />
		<PropertyType itemID='295' name='ActivationId' dataType='String' mapping='118' />
		<PropertyType itemID='296' name='Activated' dataType='Int' mapping='64' />
		<PropertyType itemID='297' name='SecurityQuestion' dataType='String' mapping='119' />
		<PropertyType itemID='298' name='SecurityAnswer' dataType='String' mapping='120' />
		<PropertyType itemID='299' name='UserName' dataType='String' mapping='121' />
		<PropertyType itemID='300' name='RegistrationType' dataType='String' mapping='122' />
		<PropertyType itemID='301' name='Downloads' dataType='Currency' mapping='5' />
		<PropertyType itemID='302' name='Customer' dataType='Text' mapping='51' />
		<PropertyType itemID='303' name='ExpectedRevenue' dataType='Currency' mapping='6' />
		<PropertyType itemID='304' name='ChanceOfWinning' dataType='Currency' mapping='7' />
		<PropertyType itemID='305' name='Contacts' dataType='Text' mapping='52' />
		<PropertyType itemID='306' name='Notes' dataType='Text' mapping='53' />
		<PropertyType itemID='307' name='ContractSigned' dataType='Int' mapping='65' />
		<PropertyType itemID='308' name='ContractSignedDate' dataType='DateTime' mapping='15' />
		<PropertyType itemID='309' name='PendingUserLang' dataType='String' mapping='123' />
		<PropertyType itemID='310' name='EnableClientBasedCulture' dataType='Int' mapping='66' />
		<PropertyType itemID='311' name='EnableUserBasedCulture' dataType='Int' mapping='67' />
		<PropertyType itemID='312' name='UrlList' dataType='Text' mapping='54' />
		<PropertyType itemID='313' name='StartPage' dataType='Reference' mapping='38' />
		<PropertyType itemID='314' name='LoginPage' dataType='Reference' mapping='39' />
		<PropertyType itemID='315' name='SiteSkin' dataType='Reference' mapping='40' />
		<PropertyType itemID='316' name='DenyCrossSiteAccess' dataType='Int' mapping='68' />
		<PropertyType itemID='317' name='NewSkin' dataType='Int' mapping='69' />
		<PropertyType itemID='318' name='Background' dataType='Reference' mapping='41' />
		<PropertyType itemID='319' name='YouTubeBackground' dataType='String' mapping='124' />
		<PropertyType itemID='320' name='VerticalAlignment' dataType='String' mapping='125' />
		<PropertyType itemID='321' name='HorizontalAlignment' dataType='String' mapping='126' />
		<PropertyType itemID='322' name='OuterCallToActionButton' dataType='String' mapping='127' />
		<PropertyType itemID='323' name='InnerCallToActionButton' dataType='Text' mapping='55' />
		<PropertyType itemID='324' name='ContentPath' dataType='String' mapping='128' />
		<PropertyType itemID='325' name='UserPath' dataType='String' mapping='129' />
		<PropertyType itemID='326' name='UserEmail' dataType='String' mapping='130' />
		<PropertyType itemID='327' name='UserId' dataType='Currency' mapping='8' />
		<PropertyType itemID='328' name='Frequency' dataType='String' mapping='131' />
		<PropertyType itemID='329' name='LandingPage' dataType='Reference' mapping='42' />
		<PropertyType itemID='330' name='PageContentView' dataType='Reference' mapping='43' />
		<PropertyType itemID='331' name='InvalidSurveyPage' dataType='Reference' mapping='44' />
		<PropertyType itemID='332' name='MailTemplatePage' dataType='Reference' mapping='45' />
		<PropertyType itemID='333' name='EnableMoreFilling' dataType='Int' mapping='70' />
		<PropertyType itemID='334' name='EnableNotificationMail' dataType='Int' mapping='71' />
		<PropertyType itemID='335' name='Evaluators' dataType='Reference' mapping='46' />
		<PropertyType itemID='336' name='EvaluatedBy' dataType='Reference' mapping='47' />
		<PropertyType itemID='337' name='EvaluatedAt' dataType='DateTime' mapping='16' />
		<PropertyType itemID='338' name='Evaluation' dataType='Text' mapping='56' />
		<PropertyType itemID='339' name='MailSubject' dataType='String' mapping='132' />
		<PropertyType itemID='340' name='AdminEmailTemplate' dataType='Text' mapping='57' />
		<PropertyType itemID='341' name='SubmitterEmailTemplate' dataType='Text' mapping='58' />
		<PropertyType itemID='342' name='OnlySingleResponse' dataType='Int' mapping='72' />
		<PropertyType itemID='343' name='RawJson' dataType='Text' mapping='59' />
		<PropertyType itemID='344' name='IntroText' dataType='Text' mapping='60' />
		<PropertyType itemID='345' name='OutroText' dataType='Text' mapping='61' />
		<PropertyType itemID='346' name='OutroRedirectLink' dataType='Reference' mapping='48' />
		<PropertyType itemID='347' name='Description2' dataType='String' mapping='133' />
		<PropertyType itemID='348' name='BlackListPath' dataType='Text' mapping='62' />
		<PropertyType itemID='349' name='KeepUntil' dataType='DateTime' mapping='17' />
		<PropertyType itemID='350' name='OriginalPath' dataType='String' mapping='134' />
		<PropertyType itemID='351' name='WorkspaceId' dataType='Int' mapping='73' />
		<PropertyType itemID='352' name='WorkspaceRelativePath' dataType='String' mapping='135' />
		<PropertyType itemID='353' name='MinRetentionTime' dataType='Int' mapping='74' />
		<PropertyType itemID='354' name='SizeQuota' dataType='Int' mapping='75' />
		<PropertyType itemID='355' name='BagCapacity' dataType='Int' mapping='76' />
		<PropertyType itemID='356' name='Search' dataType='String' mapping='136' />
		<PropertyType itemID='357' name='IsResultVisibleBefore' dataType='Int' mapping='77' />
		<PropertyType itemID='358' name='ResultPageContentView' dataType='Reference' mapping='49' />
		<PropertyType itemID='359' name='VotingPageContentView' dataType='Reference' mapping='50' />
		<PropertyType itemID='360' name='CannotSeeResultContentView' dataType='Reference' mapping='51' />
		<PropertyType itemID='361' name='LandingPageContentView' dataType='Reference' mapping='52' />
		<PropertyType itemID='362' name='RelatedImage' dataType='Reference' mapping='53' />
		<PropertyType itemID='363' name='Header' dataType='Text' mapping='63' />
		<PropertyType itemID='364' name='Details' dataType='String' mapping='137' />
		<PropertyType itemID='365' name='ContentLanguage' dataType='String' mapping='138' />
		<PropertyType itemID='366' name='WikiArticleText' dataType='Text' mapping='64' />
		<PropertyType itemID='367' name='ReferencedWikiTitles' dataType='Text' mapping='65' />
		<PropertyType itemID='368' name='DeleteInstanceAfterFinished' dataType='String' mapping='139' />
		<PropertyType itemID='369' name='AssignableToContentList' dataType='Int' mapping='78' />
		<PropertyType itemID='370' name='Cacheable' dataType='Int' mapping='79' />
		<PropertyType itemID='371' name='CacheableForLoggedInUser' dataType='Int' mapping='80' />
		<PropertyType itemID='372' name='CacheByHost' dataType='Int' mapping='81' />
		<PropertyType itemID='373' name='CacheByPath' dataType='Int' mapping='82' />
		<PropertyType itemID='374' name='CacheByParams' dataType='Int' mapping='83' />
		<PropertyType itemID='375' name='CacheByLanguage' dataType='Int' mapping='84' />
		<PropertyType itemID='376' name='SlidingExpirationMinutes' dataType='Int' mapping='85' />
		<PropertyType itemID='377' name='AbsoluteExpiration' dataType='Int' mapping='86' />
		<PropertyType itemID='378' name='CustomCacheKey' dataType='String' mapping='140' />
		<PropertyType itemID='379' name='OmitXmlDeclaration' dataType='Int' mapping='87' />
		<PropertyType itemID='380' name='ResponseEncoding' dataType='String' mapping='141' />
		<PropertyType itemID='381' name='WithChildren' dataType='Int' mapping='88' />
	</UsedPropertyTypes>
	<NodeTypeHierarchy>
		<NodeType itemID='11' name='JournalNode' className='SenseNet.Portal.Workspaces.JournalNode' />
		<NodeType itemID='10' name='GenericContent' className='SenseNet.ContentRepository.GenericContent'>
			<PropertyType name='VersioningMode' />
			<PropertyType name='Description' />
			<PropertyType name='Hidden' />
			<PropertyType name='InheritableVersioningMode' />
			<PropertyType name='ApprovingMode' />
			<PropertyType name='InheritableApprovingMode' />
			<PropertyType name='AllowedChildTypes' />
			<PropertyType name='TrashDisabled' />
			<PropertyType name='EnableLifespan' />
			<PropertyType name='ValidFrom' />
			<PropertyType name='ValidTill' />
			<PropertyType name='Aspects' />
			<PropertyType name='AspectData' />
			<PropertyType name='BrowseApplication' />
			<PropertyType name='ExtensionData' />
			<PropertyType name='IsTaggable' />
			<PropertyType name='Tags' />
			<PropertyType name='IsRateable' />
			<PropertyType name='RateStr' />
			<PropertyType name='RateAvg' />
			<PropertyType name='RateCount' />
			<PropertyType name='CheckInComments' />
			<PropertyType name='RejectReason' />
			<NodeType itemID='3' name='User' className='SenseNet.ContentRepository.User'>
				<PropertyType name='VersioningMode' />
				<PropertyType name='Description' />
				<PropertyType name='Hidden' />
				<PropertyType name='InheritableVersioningMode' />
				<PropertyType name='ApprovingMode' />
				<PropertyType name='InheritableApprovingMode' />
				<PropertyType name='AllowedChildTypes' />
				<PropertyType name='TrashDisabled' />
				<PropertyType name='EnableLifespan' />
				<PropertyType name='ValidFrom' />
				<PropertyType name='ValidTill' />
				<PropertyType name='Aspects' />
				<PropertyType name='AspectData' />
				<PropertyType name='BrowseApplication' />
				<PropertyType name='SyncGuid' />
				<PropertyType name='LastSync' />
				<PropertyType name='ImageRef' />
				<PropertyType name='ImageData' />
				<PropertyType name='Manager' />
				<PropertyType name='Language' />
				<PropertyType name='Email' />
				<PropertyType name='Enabled' />
				<PropertyType name='Domain' />
				<PropertyType name='FullName' />
				<PropertyType name='OldPasswords' />
				<PropertyType name='PasswordHash' />
				<PropertyType name='LoginName' />
				<PropertyType name='FollowedWorkspaces' />
				<PropertyType name='JobTitle' />
				<PropertyType name='Captcha' />
				<PropertyType name='Department' />
				<PropertyType name='Languages' />
				<PropertyType name='Phone' />
				<PropertyType name='Gender' />
				<PropertyType name='MaritalStatus' />
				<PropertyType name='BirthDate' />
				<PropertyType name='Education' />
				<PropertyType name='TwitterAccount' />
				<PropertyType name='FacebookURL' />
				<PropertyType name='LinkedInURL' />
				<NodeType itemID='112' name='RegisteredUser' className='SenseNet.ContentRepository.User'>
					<PropertyType name='ResetKey' />
					<PropertyType name='ActivationId' />
					<PropertyType name='Activated' />
					<PropertyType name='SecurityQuestion' />
					<PropertyType name='SecurityAnswer' />
				</NodeType>
			</NodeType>
			<NodeType itemID='2' name='Group' className='SenseNet.ContentRepository.Group'>
				<PropertyType name='VersioningMode' />
				<PropertyType name='Description' />
				<PropertyType name='Hidden' />
				<PropertyType name='InheritableVersioningMode' />
				<PropertyType name='ApprovingMode' />
				<PropertyType name='InheritableApprovingMode' />
				<PropertyType name='AllowedChildTypes' />
				<PropertyType name='TrashDisabled' />
				<PropertyType name='EnableLifespan' />
				<PropertyType name='ValidFrom' />
				<PropertyType name='ValidTill' />
				<PropertyType name='Aspects' />
				<PropertyType name='AspectData' />
				<PropertyType name='BrowseApplication' />
				<PropertyType name='SyncGuid' />
				<PropertyType name='LastSync' />
				<PropertyType name='Members' />
			</NodeType>
			<NodeType itemID='1' name='Folder' className='SenseNet.ContentRepository.Folder'>
				<PropertyType name='VersioningMode' />
				<PropertyType name='Description' />
				<PropertyType name='Hidden' />
				<PropertyType name='InheritableVersioningMode' />
				<PropertyType name='ApprovingMode' />
				<PropertyType name='InheritableApprovingMode' />
				<PropertyType name='TrashDisabled' />
				<PropertyType name='EnableLifespan' />
				<PropertyType name='ValidFrom' />
				<PropertyType name='ValidTill' />
				<PropertyType name='Aspects' />
				<PropertyType name='AspectData' />
				<PropertyType name='BrowseApplication' />
				<NodeType itemID='48' name='TrashBag' className='SenseNet.ContentRepository.TrashBag'>
					<PropertyType name='Link' />
					<PropertyType name='KeepUntil' />
					<PropertyType name='OriginalPath' />
					<PropertyType name='WorkspaceId' />
					<PropertyType name='WorkspaceRelativePath' />
				</NodeType>
				<NodeType itemID='47' name='Sites' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='46' name='SalesWorkspaceFolder' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='45' name='RuntimeContentContainer' className='SenseNet.ContentRepository.RuntimeContentContainer' />
				<NodeType itemID='44' name='ProjectWorkspaceFolder' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='43' name='Profiles' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='42' name='ProfileDomain' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='41' name='Posts' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='40' name='PortletCategory' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='39' name='OtherWorkspaceFolder' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='38' name='KPIDatasources' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='37' name='KPIDatasource' className='SenseNet.ContentRepository.KPIDatasource'>
					<PropertyType name='KPIData' />
				</NodeType>
				<NodeType itemID='36' name='FieldControlTemplates' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='35' name='ExpenseClaim' className='SenseNet.ContentRepository.ExpenseClaim' />
				<NodeType itemID='34' name='Email' className='SenseNet.ContentRepository.Folder'>
					<PropertyType name='Body' />
					<PropertyType name='From' />
					<PropertyType name='Sent' />
				</NodeType>
				<NodeType itemID='33' name='DocumentWorkspaceFolder' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='32' name='DiscussionForum' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='31' name='Device' className='SenseNet.ApplicationModel.Device'>
					<PropertyType name='UserAgentPattern' />
				</NodeType>
				<NodeType itemID='30' name='ContentViews' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='29' name='SmartFolder' className='SenseNet.ContentRepository.SmartFolder'>
					<PropertyType name='Query' />
					<PropertyType name='EnableAutofilters' />
					<PropertyType name='EnableLifespanFilter' />
					<NodeType itemID='129' name='ContentRotator' className='SenseNet.ContentRepository.ContentRotator'>
						<PropertyType name='SelectionMode' />
						<PropertyType name='OrderingMode' />
					</NodeType>
				</NodeType>
				<NodeType itemID='28' name='Workspace' className='SenseNet.ContentRepository.Workspaces.Workspace'>
					<PropertyType name='IsActive' />
					<PropertyType name='IsWallContainer' />
					<PropertyType name='WorkspaceSkin' />
					<PropertyType name='Manager' />
					<PropertyType name='Deadline' />
					<PropertyType name='IsCritical' />
					<NodeType itemID='128' name='Wiki' className='SenseNet.ContentRepository.Workspaces.Workspace' />
					<NodeType itemID='127' name='UserProfile' className='SenseNet.ContentRepository.UserProfile' />
					<NodeType itemID='126' name='TrashBin' className='SenseNet.ContentRepository.TrashBin'>
						<PropertyType name='MinRetentionTime' />
						<PropertyType name='SizeQuota' />
						<PropertyType name='BagCapacity' />
					</NodeType>
					<NodeType itemID='125' name='TeamWorkspace' className='SenseNet.ContentRepository.Workspaces.Workspace' />
					<NodeType itemID='124' name='Site' className='SenseNet.Portal.Site'>
						<PropertyType name='Language' />
						<PropertyType name='PendingUserLang' />
						<PropertyType name='EnableClientBasedCulture' />
						<PropertyType name='EnableUserBasedCulture' />
						<PropertyType name='UrlList' />
						<PropertyType name='StartPage' />
						<PropertyType name='LoginPage' />
						<PropertyType name='SiteSkin' />
						<PropertyType name='DenyCrossSiteAccess' />
					</NodeType>
					<NodeType itemID='123' name='SalesWorkspace' className='SenseNet.ContentRepository.Workspaces.Workspace'>
						<PropertyType name='StartDate' />
						<PropertyType name='Completion' />
						<PropertyType name='Customer' />
						<PropertyType name='ExpectedRevenue' />
						<PropertyType name='ChanceOfWinning' />
						<PropertyType name='Contacts' />
						<PropertyType name='Notes' />
						<PropertyType name='ContractSigned' />
						<PropertyType name='ContractSignedDate' />
					</NodeType>
					<NodeType itemID='122' name='ProjectWorkspace' className='SenseNet.ContentRepository.Workspaces.Workspace'>
						<PropertyType name='Completion' />
					</NodeType>
					<NodeType itemID='121' name='DocumentWorkspace' className='SenseNet.ContentRepository.Workspaces.Workspace' />
					<NodeType itemID='120' name='Blog' className='SenseNet.ContentRepository.Workspaces.Workspace'>
						<PropertyType name='ShowAvatar' />
					</NodeType>
				</NodeType>
				<NodeType itemID='27' name='ContentList' className='SenseNet.ContentRepository.ContentList'>
					<PropertyType name='ContentListBindings' />
					<PropertyType name='ContentListDefinition' />
					<PropertyType name='DefaultView' />
					<PropertyType name='AvailableViews' />
					<PropertyType name='AvailableContentTypeFields' />
					<PropertyType name='ListEmail' />
					<PropertyType name='ExchangeSubscriptionId' />
					<PropertyType name='OverwriteFiles' />
					<PropertyType name='GroupAttachments' />
					<PropertyType name='SaveOriginalEmail' />
					<PropertyType name='IncomingEmailWorkflow' />
					<PropertyType name='OnlyFromLocalGroups' />
					<PropertyType name='InboxFolder' />
					<PropertyType name='OwnerWhenVisitor' />
					<NodeType itemID='119' name='Library' className='SenseNet.ContentRepository.ContentList'>
						<NodeType itemID='159' name='ImageLibrary' className='SenseNet.ContentRepository.ContentList'>
							<PropertyType name='CoverImage' />
						</NodeType>
						<NodeType itemID='158' name='DocumentLibrary' className='SenseNet.ContentRepository.ContentList' />
					</NodeType>
					<NodeType itemID='118' name='ItemList' className='SenseNet.ContentRepository.ContentList'>
						<NodeType itemID='157' name='TaskList' className='SenseNet.ContentRepository.ContentList' />
						<NodeType itemID='156' name='SurveyList' className='SenseNet.Portal.Portlets.ContentHandlers.SurveyList'>
							<PropertyType name='EmailFrom' />
							<PropertyType name='EmailField' />
							<PropertyType name='EmailList' />
							<PropertyType name='LandingPage' />
							<PropertyType name='EnableNotificationMail' />
							<PropertyType name='MailSubject' />
							<PropertyType name='AdminEmailTemplate' />
							<PropertyType name='SubmitterEmailTemplate' />
							<PropertyType name='OnlySingleResponse' />
							<PropertyType name='RawJson' />
							<PropertyType name='IntroText' />
							<PropertyType name='OutroText' />
							<PropertyType name='OutroRedirectLink' />
						</NodeType>
						<NodeType itemID='155' name='Survey' className='SenseNet.ContentRepository.Survey'>
							<PropertyType name='SenderAddress' />
							<PropertyType name='LandingPage' />
							<PropertyType name='PageContentView' />
							<PropertyType name='InvalidSurveyPage' />
							<PropertyType name='MailTemplatePage' />
							<PropertyType name='EnableMoreFilling' />
							<PropertyType name='EnableNotificationMail' />
							<PropertyType name='Evaluators' />
							<NodeType itemID='165' name='Voting' className='SenseNet.ContentRepository.Voting'>
								<PropertyType name='IsResultVisibleBefore' />
								<PropertyType name='ResultPageContentView' />
								<PropertyType name='VotingPageContentView' />
								<PropertyType name='CannotSeeResultContentView' />
								<PropertyType name='LandingPageContentView' />
							</NodeType>
						</NodeType>
						<NodeType itemID='154' name='MemoList' className='SenseNet.ContentRepository.ContentList' />
						<NodeType itemID='153' name='LinkList' className='SenseNet.ContentRepository.ContentList' />
						<NodeType itemID='152' name='ForumTopic' className='SenseNet.ContentRepository.ContentList' />
						<NodeType itemID='151' name='Form' className='SenseNet.Portal.Portlets.ContentHandlers.Form'>
							<PropertyType name='EmailTemplate' />
							<PropertyType name='EmailTemplateSubmitter' />
							<PropertyType name='EmailFrom' />
							<PropertyType name='EmailFromSubmitter' />
							<PropertyType name='EmailField' />
							<PropertyType name='EmailList' />
							<PropertyType name='TitleSubmitter' />
							<PropertyType name='AfterSubmitText' />
							<NodeType itemID='164' name='EventRegistrationForm' className='SenseNet.Portal.Portlets.ContentHandlers.Form' />
						</NodeType>
						<NodeType itemID='150' name='EventList' className='SenseNet.ContentRepository.ContentList'>
							<PropertyType name='RegistrationFolder' />
						</NodeType>
						<NodeType itemID='149' name='CustomList' className='SenseNet.ContentRepository.ContentList' />
					</NodeType>
					<NodeType itemID='117' name='Aspect' className='SenseNet.ContentRepository.Aspect'>
						<PropertyType name='AspectDefinition' />
						<PropertyType name='FieldSettingContents' />
					</NodeType>
				</NodeType>
				<NodeType itemID='26' name='ArticleSection' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='25' name='ADFolder' className='SenseNet.ContentRepository.Security.ADSync.ADFolder'>
					<PropertyType name='SyncGuid' />
					<PropertyType name='LastSync' />
				</NodeType>
				<NodeType itemID='8' name='OrganizationalUnit' className='SenseNet.ContentRepository.OrganizationalUnit'>
					<PropertyType name='SyncGuid' />
					<PropertyType name='LastSync' />
				</NodeType>
				<NodeType itemID='7' name='Domain' className='SenseNet.ContentRepository.Domain'>
					<PropertyType name='SyncGuid' />
					<PropertyType name='LastSync' />
				</NodeType>
				<NodeType itemID='6' name='Domains' className='SenseNet.ContentRepository.Folder' />
				<NodeType itemID='5' name='SystemFolder' className='SenseNet.ContentRepository.SystemFolder'>
					<NodeType itemID='168' name='TestNode' className='SenseNet.SearchImpl.Tests.Implementations.TestNode' />
					<NodeType itemID='116' name='Skins' className='SenseNet.ContentRepository.SystemFolder' />
					<NodeType itemID='115' name='Skin' className='SenseNet.ContentRepository.SystemFolder'>
						<PropertyType name='NewSkin' />
					</NodeType>
					<NodeType itemID='114' name='Resources' className='SenseNet.ContentRepository.SystemFolder' />
					<NodeType itemID='113' name='Portlets' className='SenseNet.ContentRepository.SystemFolder' />
				</NodeType>
				<NodeType itemID='4' name='PortalRoot' className='SenseNet.ContentRepository.PortalRoot'>
					<PropertyType name='VersioningMode' />
					<PropertyType name='Description' />
					<PropertyType name='Hidden' />
					<PropertyType name='InheritableVersioningMode' />
					<PropertyType name='ApprovingMode' />
					<PropertyType name='InheritableApprovingMode' />
					<PropertyType name='TrashDisabled' />
					<PropertyType name='EnableLifespan' />
					<PropertyType name='ValidFrom' />
					<PropertyType name='ValidTill' />
					<PropertyType name='Aspects' />
					<PropertyType name='AspectData' />
					<PropertyType name='BrowseApplication' />
				</NodeType>
			</NodeType>
			<NodeType itemID='24' name='WikiArticle' className='SenseNet.Portal.WikiArticle'>
				<PropertyType name='WikiArticleText' />
				<PropertyType name='ReferencedWikiTitles' />
			</NodeType>
			<NodeType itemID='23' name='UserSearch' className='SenseNet.ContentRepository.GenericContent'>
				<PropertyType name='Search' />
			</NodeType>
			<NodeType itemID='22' name='Tag' className='SenseNet.ContentRepository.GenericContent'>
				<PropertyType name='Description2' />
				<PropertyType name='BlackListPath' />
			</NodeType>
			<NodeType itemID='21' name='Subscription' className='SenseNet.ContentRepository.GenericContent'>
				<PropertyType name='IsActive' />
				<PropertyType name='Language' />
				<PropertyType name='UserName' />
				<PropertyType name='ContentPath' />
				<PropertyType name='UserPath' />
				<PropertyType name='UserEmail' />
				<PropertyType name='UserId' />
				<PropertyType name='Frequency' />
			</NodeType>
			<NodeType itemID='20' name='Query' className='SenseNet.ContentRepository.QueryContent'>
				<PropertyType name='Query' />
			</NodeType>
			<NodeType itemID='19' name='PublicRegistrationConfig' className='SenseNet.ContentRepository.GenericContent'>
				<PropertyType name='SecurityGroups' />
				<PropertyType name='DefaultDomainPath' />
				<PropertyType name='UserTypeName' />
				<PropertyType name='DuplicateErrorMessage' />
				<PropertyType name='IsBodyHtml' />
				<PropertyType name='ActivationEnabled' />
				<PropertyType name='ActivationEmailTemplate' />
				<PropertyType name='ActivationSuccessTemplate' />
				<PropertyType name='AlreadyActivatedMessage' />
				<PropertyType name='MailSubjectTemplate' />
				<PropertyType name='MailFrom' />
				<PropertyType name='AdminEmailAddress' />
				<PropertyType name='RegistrationSuccessTemplate' />
				<PropertyType name='ResetPasswordTemplate' />
				<PropertyType name='ResetPasswordSubjectTemplate' />
				<PropertyType name='ResetPasswordSuccessfulTemplate' />
				<PropertyType name='ChangePasswordUserInterfacePath' />
				<PropertyType name='ChangePasswordSuccessfulMessage' />
				<PropertyType name='ForgottenPasswordUserInterfacePath' />
				<PropertyType name='NewRegistrationContentView' />
				<PropertyType name='EditProfileContentView' />
				<PropertyType name='AutoGeneratePassword' />
				<PropertyType name='DisableCreatedUser' />
				<PropertyType name='IsUniqueEmail' />
				<PropertyType name='AutomaticLogon' />
				<PropertyType name='ChangePasswordPagePath' />
				<PropertyType name='ChangePasswordRestrictedText' />
				<PropertyType name='AlreadyRegisteredUserMessage' />
				<PropertyType name='UpdateProfileSuccessTemplate' />
				<PropertyType name='EmailNotValid' />
				<PropertyType name='NoEmailGiven' />
				<PropertyType name='ActivateByAdmin' />
				<PropertyType name='ActivateEmailSubject' />
				<PropertyType name='ActivateEmailTemplate' />
				<PropertyType name='ActivateAdmins' />
			</NodeType>
			<NodeType itemID='18' name='NotificationConfig' className='SenseNet.Messaging.NotificationConfig'>
				<PropertyType name='Body' />
				<PropertyType name='Subject' />
				<PropertyType name='SenderAddress' />
			</NodeType>
			<NodeType itemID='17' name='ContentLink' className='SenseNet.ContentRepository.ContentLink'>
				<PropertyType name='Link' />
			</NodeType>
			<NodeType itemID='16' name='FieldSettingContent' className='SenseNet.ContentRepository.Schema.FieldSettingContent'>
				<NodeType itemID='111' name='XmlFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				<NodeType itemID='110' name='ReferenceFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				<NodeType itemID='109' name='PageBreakFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				<NodeType itemID='108' name='NullFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				<NodeType itemID='107' name='IntegerFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				<NodeType itemID='106' name='HyperLinkFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				<NodeType itemID='105' name='DateTimeFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				<NodeType itemID='104' name='NumberFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent'>
					<NodeType itemID='148' name='CurrencyFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
				</NodeType>
				<NodeType itemID='103' name='TextFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent'>
					<NodeType itemID='147' name='LongTextFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
					<NodeType itemID='146' name='ShortTextFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent'>
						<NodeType itemID='163' name='PasswordFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
						<NodeType itemID='162' name='ChoiceFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent'>
							<NodeType itemID='167' name='YesNoFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
							<NodeType itemID='166' name='PermissionChoiceFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
						</NodeType>
					</NodeType>
				</NodeType>
				<NodeType itemID='102' name='BinaryFieldSetting' className='SenseNet.ContentRepository.Schema.FieldSettingContent' />
			</NodeType>
			<NodeType itemID='15' name='ListItem' className='SenseNet.ContentRepository.GenericContent'>
				<NodeType itemID='101' name='VotingItem' className='SenseNet.ContentRepository.VotingItem' />
				<NodeType itemID='100' name='SurveyListItem' className='SenseNet.ContentRepository.GenericContent' />
				<NodeType itemID='99' name='SurveyItem' className='SenseNet.ContentRepository.SurveyItem'>
					<PropertyType name='EvaluatedBy' />
					<PropertyType name='EvaluatedAt' />
					<PropertyType name='Evaluation' />
				</NodeType>
				<NodeType itemID='98' name='SliderItem' className='SenseNet.ContentRepository.GenericContent'>
					<PropertyType name='Background' />
					<PropertyType name='YouTubeBackground' />
					<PropertyType name='VerticalAlignment' />
					<PropertyType name='HorizontalAlignment' />
					<PropertyType name='OuterCallToActionButton' />
					<PropertyType name='InnerCallToActionButton' />
				</NodeType>
				<NodeType itemID='97' name='Post' className='SenseNet.ContentRepository.GenericContent'>
					<PropertyType name='JournalId' />
					<PropertyType name='PostType' />
					<PropertyType name='SharedContent' />
					<PropertyType name='PostDetails' />
				</NodeType>
				<NodeType itemID='96' name='Portlet' className='SenseNet.ContentRepository.GenericContent'>
					<PropertyType name='ImageRef' />
					<PropertyType name='ImageData' />
					<PropertyType name='TypeName' />
				</NodeType>
				<NodeType itemID='95' name='Memo' className='SenseNet.ContentRepository.GenericContent'>
					<PropertyType name='Date' />
					<PropertyType name='MemoType' />
					<PropertyType name='SeeAlso' />
				</NodeType>
				<NodeType itemID='94' name='Link' className='SenseNet.ContentRepository.GenericContent'>
					<PropertyType name='Url' />
				</NodeType>
				<NodeType itemID='93' name='Like' className='SenseNet.ContentRepository.GenericContent' />
				<NodeType itemID='92' name='ForumEntry' className='SenseNet.Portal.DiscussionForum.ForumEntry'>
					<PropertyType name='ReplyTo' />
					<PropertyType name='PostedBy' />
					<PropertyType name='SerialNo' />
				</NodeType>
				<NodeType itemID='91' name='ExpenseClaimItem' className='SenseNet.ContentRepository.ExpenseClaimItem'>
					<PropertyType name='ImageRef' />
					<PropertyType name='ImageData' />
					<PropertyType name='Amount' />
					<PropertyType name='Date' />
				</NodeType>
				<NodeType itemID='90' name='FormItem' className='SenseNet.Portal.Portlets.ContentHandlers.FormItem'>
					<NodeType itemID='145' name='EventRegistrationFormItem' className='SenseNet.Portal.Portlets.ContentHandlers.EventRegistrationFormItem'>
						<PropertyType name='Email' />
						<PropertyType name='GuestNumber' />
					</NodeType>
				</NodeType>
				<NodeType itemID='89' name='CustomListItem' className='SenseNet.ContentRepository.GenericContent'>
					<PropertyType name='WorkflowsRunning' />
				</NodeType>
				<NodeType itemID='88' name='ConfirmationItem' className='SenseNet.ContentRepository.GenericContent'>
					<PropertyType name='Confirmed' />
				</NodeType>
				<NodeType itemID='87' name='Comment' className='SenseNet.ContentRepository.GenericContent' />
				<NodeType itemID='86' name='Car' className='SenseNet.ContentRepository.GenericContent'>
					<PropertyType name='Make' />
					<PropertyType name='Model' />
					<PropertyType name='Style' />
					<PropertyType name='StartingDate' />
					<PropertyType name='Color' />
					<PropertyType name='EngineSize' />
					<PropertyType name='Power' />
					<PropertyType name='Price' />
				</NodeType>
				<NodeType itemID='85' name='CalendarEvent' className='SenseNet.ContentRepository.CalendarEvent'>
					<PropertyType name='StartDate' />
					<PropertyType name='Lead' />
					<PropertyType name='RegistrationForm' />
					<PropertyType name='Location' />
					<PropertyType name='EndDate' />
					<PropertyType name='AllDay' />
					<PropertyType name='EventUrl' />
					<PropertyType name='RequiresRegistration' />
					<PropertyType name='OwnerEmail' />
					<PropertyType name='NotificationMode' />
					<PropertyType name='EmailTemplate' />
					<PropertyType name='EmailTemplateSubmitter' />
					<PropertyType name='EmailFrom' />
					<PropertyType name='EmailFromSubmitter' />
					<PropertyType name='EmailField' />
					<PropertyType name='MaxParticipants' />
					<PropertyType name='EventType' />
				</NodeType>
				<NodeType itemID='84' name='BlogPost' className='SenseNet.Portal.BlogPost'>
					<PropertyType name='PublishedOn' />
					<PropertyType name='LeadingText' />
					<PropertyType name='BodyText' />
					<PropertyType name='IsPublished' />
				</NodeType>
				<NodeType itemID='83' name='WebContent' className='SenseNet.ContentRepository.GenericContent'>
					<PropertyType name='ReviewDate' />
					<PropertyType name='ArchiveDate' />
					<NodeType itemID='144' name='WebContentDemo' className='SenseNet.ContentRepository.GenericContent'>
						<PropertyType name='Subtitle' />
						<PropertyType name='Body' />
						<PropertyType name='Keywords' />
						<PropertyType name='Author' />
						<PropertyType name='RelatedImage' />
						<PropertyType name='Header' />
						<PropertyType name='Details' />
						<PropertyType name='ContentLanguage' />
					</NodeType>
					<NodeType itemID='143' name='HTMLContent' className='SenseNet.ContentRepository.GenericContent'>
						<PropertyType name='HTMLFragment' />
					</NodeType>
					<NodeType itemID='142' name='Article' className='SenseNet.ContentRepository.GenericContent'>
						<PropertyType name='Subtitle' />
						<PropertyType name='Lead' />
						<PropertyType name='Body' />
						<PropertyType name='Pinned' />
						<PropertyType name='Keywords' />
						<PropertyType name='Author' />
						<PropertyType name='ImageRef' />
						<PropertyType name='ImageData' />
					</NodeType>
				</NodeType>
				<NodeType itemID='82' name='Task' className='SenseNet.ContentRepository.Task'>
					<PropertyType name='StartDate' />
					<PropertyType name='DueDate' />
					<PropertyType name='AssignedTo' />
					<PropertyType name='Priority' />
					<PropertyType name='Status' />
					<PropertyType name='TaskCompletion' />
					<NodeType itemID='141' name='ApprovalWorkflowTask' className='SenseNet.ContentRepository.Task'>
						<PropertyType name='Comment' />
						<PropertyType name='Result' />
						<PropertyType name='ContentToApprove' />
						<NodeType itemID='161' name='ExpenseClaimWorkflowTask' className='SenseNet.ContentRepository.Task'>
							<PropertyType name='Reason' />
							<PropertyType name='ExpenseClaim' />
							<PropertyType name='Sum' />
						</NodeType>
					</NodeType>
				</NodeType>
			</NodeType>
			<NodeType itemID='14' name='Workflow' className='SenseNet.Workflow.WorkflowHandlerBase'>
				<PropertyType name='WorkflowStatus' />
				<PropertyType name='WorkflowDefinitionVersion' />
				<PropertyType name='WorkflowInstanceGuid' />
				<PropertyType name='RelatedContent' />
				<PropertyType name='RelatedContentTimestamp' />
				<PropertyType name='SystemMessages' />
				<PropertyType name='AllowManualStart' />
				<PropertyType name='AutostartOnPublished' />
				<PropertyType name='AutostartOnCreated' />
				<PropertyType name='AutostartOnChanged' />
				<PropertyType name='ContentWorkflow' />
				<PropertyType name='AbortOnRelatedContentChange' />
				<PropertyType name='OwnerSiteUrl' />
				<NodeType itemID='81' name='RegistrationWorkflow' className='SenseNet.Workflow.RegistrationWorkflow'>
					<PropertyType name='Email' />
					<PropertyType name='FullName' />
					<PropertyType name='PasswordHash' />
					<PropertyType name='UserName' />
					<PropertyType name='RegistrationType' />
				</NodeType>
				<NodeType itemID='80' name='MailProcessorWorkflow' className='SenseNet.Workflow.WorkflowHandlerBase' />
				<NodeType itemID='79' name='ForgottenPasswordWorkflow' className='SenseNet.Workflow.WorkflowHandlerBase'>
					<PropertyType name='EmailForPassword' />
				</NodeType>
				<NodeType itemID='78' name='ExpenseClaimWorkflow' className='SenseNet.Workflow.WorkflowHandlerBase'>
					<PropertyType name='CEO' />
					<PropertyType name='BudgetLimit' />
					<PropertyType name='FinanceEmail' />
				</NodeType>
				<NodeType itemID='77' name='DocumentPreviewWorkflow' className='SenseNet.Workflow.WorkflowHandlerBase'>
					<PropertyType name='StartIndex' />
					<PropertyType name='ContentVersion' />
				</NodeType>
				<NodeType itemID='76' name='ApprovalWorkflow' className='SenseNet.Workflow.ApprovalWorkflow'>
					<PropertyType name='FirstLevelTimeFrame' />
					<PropertyType name='SecondLevelTimeFrame' />
					<PropertyType name='FirstLevelApprover' />
					<PropertyType name='SecondLevelApprover' />
					<PropertyType name='WaitForAll' />
				</NodeType>
			</NodeType>
			<NodeType itemID='13' name='Application' className='SenseNet.ApplicationModel.Application'>
				<PropertyType name='AppName' />
				<PropertyType name='Disabled' />
				<PropertyType name='IsModal' />
				<PropertyType name='Clear' />
				<PropertyType name='Scenario' />
				<PropertyType name='ActionTypeName' />
				<PropertyType name='StyleHint' />
				<PropertyType name='RequiredPermissions' />
				<PropertyType name='DeepPermissionCheck' />
				<PropertyType name='IncludeBackUrl' />
				<PropertyType name='CacheControl' />
				<PropertyType name='MaxAge' />
				<PropertyType name='CustomUrlParameters' />
				<PropertyType name='StoredIcon' />
				<NodeType itemID='75' name='XsltApplication' className='SenseNet.Portal.Handlers.XsltApplication'>
					<PropertyType name='Binary' />
					<PropertyType name='MimeType' />
					<PropertyType name='Cacheable' />
					<PropertyType name='CacheableForLoggedInUser' />
					<PropertyType name='CacheByHost' />
					<PropertyType name='CacheByPath' />
					<PropertyType name='CacheByParams' />
					<PropertyType name='CacheByLanguage' />
					<PropertyType name='SlidingExpirationMinutes' />
					<PropertyType name='AbsoluteExpiration' />
					<PropertyType name='CustomCacheKey' />
					<PropertyType name='OmitXmlDeclaration' />
					<PropertyType name='ResponseEncoding' />
					<PropertyType name='WithChildren' />
				</NodeType>
				<NodeType itemID='74' name='WebServiceApplication' className='SenseNet.ApplicationModel.Application'>
					<PropertyType name='Binary' />
				</NodeType>
				<NodeType itemID='73' name='RssApplication' className='SenseNet.Services.RssApplication' />
				<NodeType itemID='72' name='Webform' className='SenseNet.ApplicationModel.Application'>
					<PropertyType name='Binary' />
					<NodeType itemID='140' name='Page' className='SenseNet.Portal.Page'>
						<PropertyType name='Comment' />
						<PropertyType name='Keywords' />
						<PropertyType name='MetaTitle' />
						<PropertyType name='MetaDescription' />
						<PropertyType name='MetaAuthors' />
						<PropertyType name='CustomMeta' />
						<PropertyType name='PageTemplateNode' />
						<PropertyType name='PersonalizationSettings' />
						<PropertyType name='TemporaryPortletInfo' />
						<PropertyType name='TextExtract' />
						<PropertyType name='SmartUrl' />
						<PropertyType name='PageSkin' />
						<PropertyType name='HasTemporaryPortletInfo' />
						<PropertyType name='IsExternal' />
						<PropertyType name='OuterUrl' />
						<PropertyType name='PageId' />
						<PropertyType name='NodeName' />
					</NodeType>
				</NodeType>
				<NodeType itemID='71' name='ImgResizeApplication' className='SenseNet.Portal.ApplicationModel.ImgResizeApplication'>
					<PropertyType name='Width' />
					<PropertyType name='Height' />
					<PropertyType name='ImageType' />
					<PropertyType name='ImageFieldName' />
					<PropertyType name='Stretch' />
					<PropertyType name='OutputFormat' />
					<PropertyType name='SmoothingMode' />
					<PropertyType name='InterpolationMode' />
					<PropertyType name='PixelOffsetMode' />
					<PropertyType name='ResizeTypeMode' />
					<PropertyType name='CropVAlign' />
					<PropertyType name='CropHAlign' />
				</NodeType>
				<NodeType itemID='70' name='HttpStatusApplication' className='SenseNet.Portal.AppModel.HttpStatusApplication'>
					<PropertyType name='StatusCode' />
					<PropertyType name='RedirectUrl' />
				</NodeType>
				<NodeType itemID='69' name='HttpHandlerApplication' className='SenseNet.Portal.Handlers.HttpHandlerApplication' />
				<NodeType itemID='68' name='HttpEndpointDemoContent' className='SenseNet.ContentRepository.HttpEndpointDemoContent'>
					<PropertyType name='A' />
					<PropertyType name='B' />
				</NodeType>
				<NodeType itemID='67' name='GoogleSitemap' className='SenseNet.ApplicationModel.Application'>
					<PropertyType name='Query' />
					<PropertyType name='ListHidden' />
					<PropertyType name='SiteUrl' />
				</NodeType>
				<NodeType itemID='66' name='GenericODataApplication' className='SenseNet.Portal.ApplicationModel.GenericODataApplication'>
					<PropertyType name='ClassName' />
					<PropertyType name='MethodName' />
					<PropertyType name='Parameters' />
				</NodeType>
				<NodeType itemID='65' name='ExportToCsvApplication' className='SenseNet.Services.ExportToCsvApplication' />
				<NodeType itemID='64' name='CaptchaImageApplication' className='SenseNet.Portal.UI.Controls.Captcha.CaptchaImageApplication' />
				<NodeType itemID='63' name='BackupIndexHandler' className='SenseNet.Portal.Handlers.BackupIndexHandler' />
				<NodeType itemID='62' name='ApplicationOverride' className='SenseNet.ApplicationModel.Application' />
			</NodeType>
			<NodeType itemID='12' name='File' className='SenseNet.ContentRepository.File'>
				<PropertyType name='Binary' />
				<PropertyType name='Watermark' />
				<PropertyType name='PageCount' />
				<PropertyType name='MimeType' />
				<PropertyType name='Shapes' />
				<PropertyType name='PageAttributes' />
				<NodeType itemID='61' name='WorkflowDefinition' className='SenseNet.Workflow.WorkflowDefinitionHandler'>
					<PropertyType name='ContentWorkflow' />
					<PropertyType name='AbortOnRelatedContentChange' />
					<PropertyType name='DeleteInstanceAfterFinished' />
					<PropertyType name='AssignableToContentList' />
				</NodeType>
				<NodeType itemID='60' name='Video' className='SenseNet.ContentRepository.File'>
					<PropertyType name='Keywords' />
				</NodeType>
				<NodeType itemID='59' name='OrderForm' className='SenseNet.ContentRepository.File'>
					<PropertyType name='CompanyName' />
					<PropertyType name='OrderFormId' />
					<PropertyType name='CompanySeat' />
					<PropertyType name='RepresentedBy' />
					<PropertyType name='ContactEmailAddress' />
					<PropertyType name='ContactPhoneNr' />
				</NodeType>
				<NodeType itemID='58' name='UserControl' className='SenseNet.ContentRepository.File'>
					<NodeType itemID='139' name='ViewBase' className='SenseNet.Portal.UI.ContentListViews.Handlers.ViewBase'>
						<PropertyType name='EnableAutofilters' />
						<PropertyType name='EnableLifespanFilter' />
						<PropertyType name='Template' />
						<PropertyType name='FilterXml' />
						<PropertyType name='QueryTop' />
						<PropertyType name='QuerySkip' />
						<PropertyType name='Icon' />
						<NodeType itemID='160' name='ListView' className='SenseNet.Portal.UI.ContentListViews.Handlers.ListView'>
							<PropertyType name='Columns' />
							<PropertyType name='SortBy' />
							<PropertyType name='GroupBy' />
							<PropertyType name='Flat' />
							<PropertyType name='MainScenario' />
						</NodeType>
					</NodeType>
				</NodeType>
				<NodeType itemID='57' name='Image' className='SenseNet.ContentRepository.Image'>
					<PropertyType name='Keywords' />
					<PropertyType name='Width' />
					<PropertyType name='Height' />
					<PropertyType name='DateTaken' />
					<NodeType itemID='138' name='PreviewImage' className='SenseNet.ContentRepository.Image' />
				</NodeType>
				<NodeType itemID='56' name='HtmlTemplate' className='SenseNet.Portal.UI.HtmlTemplate' />
				<NodeType itemID='55' name='FieldControlTemplate' className='SenseNet.ContentRepository.File' />
				<NodeType itemID='54' name='ExecutableFile' className='SenseNet.ContentRepository.File' />
				<NodeType itemID='53' name='DynamicJsonContent' className='SenseNet.Portal.Handlers.DynamicJsonContent' />
				<NodeType itemID='52' name='Contract' className='SenseNet.ContentRepository.File'>
					<PropertyType name='Keywords' />
					<PropertyType name='ContractId' />
					<PropertyType name='Project' />
					<PropertyType name='Language' />
					<PropertyType name='Responsee' />
					<PropertyType name='Lawyer' />
					<PropertyType name='RelatedDocs' />
				</NodeType>
				<NodeType itemID='51' name='ContentView' className='SenseNet.ContentRepository.File' />
				<NodeType itemID='50' name='SystemFile' className='SenseNet.ContentRepository.File'>
					<NodeType itemID='137' name='Resource' className='SenseNet.ContentRepository.i18n.Resource'>
						<PropertyType name='Downloads' />
					</NodeType>
					<NodeType itemID='136' name='PageTemplate' className='SenseNet.Portal.PageTemplate'>
						<PropertyType name='MasterPageNode' />
					</NodeType>
					<NodeType itemID='135' name='MasterPage' className='SenseNet.Portal.MasterPage' />
					<NodeType itemID='134' name='ApplicationCacheFile' className='SenseNet.ContentRepository.ApplicationCacheFile' />
				</NodeType>
				<NodeType itemID='49' name='Settings' className='SenseNet.ContentRepository.Settings'>
					<PropertyType name='GlobalOnly' />
					<NodeType itemID='133' name='PortalSettings' className='SenseNet.Portal.PortalSettings' />
					<NodeType itemID='132' name='LoggingSettings' className='SenseNet.ContentRepository.LoggingSettings' />
					<NodeType itemID='131' name='IndexingSettings' className='SenseNet.Search.IndexingSettings' />
					<NodeType itemID='130' name='ADSettings' className='SenseNet.ContentRepository.Security.ADSync.ADSettings' />
				</NodeType>
			</NodeType>
		</NodeType>
		<NodeType itemID='9' name='ContentType' className='SenseNet.ContentRepository.Schema.ContentType'>
			<PropertyType name='Binary' />
		</NodeType>
	</NodeTypeHierarchy>
</StorageSchema>
";
        #endregion
        #region NODES
        private static List<NodeRecord> _nodes = new List<NodeRecord>
        {
            new NodeRecord
            {
                Path = "/Root/IMS/BuiltIn/Portal/Admin",
                Name = "Admin",
                DisplayName = null,
                NodeId = 1,
                ParentNodeId = 5,
                LastMajorVersionId = 1,
                LastMinorVersionId = 1,
                NodeTypeId = 3,
                ContentListTypeId = 0,
                ContentListId = 0,
                CreatingInProgress = false,
                IsDeleted = false,
                Index = 1,
                Locked = false,
                LockedById = 0,
                ETag = null,
                LockType = 0,
                LockTimeout = 0,
                LockDate = DateTime.MinValue,
                LockToken = "",
                LastLockUpdate = DateTime.MinValue,
                NodeCreationDate = DateTime.MinValue,
                NodeCreatedById = 1,
                NodeModificationDate = DateTime.MinValue,
                NodeModifiedById = 1,
                IsSystem = false,
                OwnerId = 1,
                SavingState = ContentSavingState.Finalized,
                NodeTimestamp = 0L
            },
            new NodeRecord
            {
                Path = "/Root",
                Name = "Root",
                DisplayName = null,
                NodeId = 2,
                ParentNodeId = 0,
                LastMajorVersionId = 2,
                LastMinorVersionId = 2,
                NodeTypeId = 4,
                ContentListTypeId = 0,
                ContentListId = 0,
                CreatingInProgress = false,
                IsDeleted = false,
                Index = 1,
                Locked = false,
                LockedById = 0,
                ETag = null,
                LockType = 0,
                LockTimeout = 0,
                LockDate = DateTime.MinValue,
                LockToken = "",
                LastLockUpdate = DateTime.MinValue,
                NodeCreationDate = DateTime.MinValue,
                NodeCreatedById = 1,
                NodeModificationDate = DateTime.MinValue,
                NodeModifiedById = 1,
                IsSystem = false,
                OwnerId = 1,
                SavingState = ContentSavingState.Finalized,
                NodeTimestamp = 0L
            }
        };
        #endregion
        #region VERSIONS
        private static List<VersionRecord> _versions = new List<VersionRecord>
        {
            new VersionRecord
            {
                VersionId = 1,
                NodeId = 1,
                Version = VersionNumber.Parse("1.0.A"),
                CreationDate = DateTime.MinValue,
                CreatedById = 1,
                ModificationDate = DateTime.MinValue,
                ModifiedById = 1,
                ChangedData = null,
                VersionTimestamp = 0L
            },
            new VersionRecord
            {
                VersionId = 2,
                NodeId = 2,
                Version = VersionNumber.Parse("1.0.A"),
                CreationDate = DateTime.MinValue,
                CreatedById = 1,
                ModificationDate = DateTime.MinValue,
                ModifiedById = 1,
                ChangedData = null,
                VersionTimestamp = 0L
            }
        };

        #endregion
    }
}
