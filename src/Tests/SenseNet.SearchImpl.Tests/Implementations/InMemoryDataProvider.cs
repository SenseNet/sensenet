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

        protected override int ContentListStartPage
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

        public override void RegisterIndexingActivity(IIndexingActivity activity)
        {
            throw new NotImplementedException();
        }

        #endregion

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

        protected override int AcquireTreeLock(string path)
        {
            throw new NotImplementedException();
        }

        protected override void CheckScriptInternal(string commandText)
        {
            throw new NotImplementedException();
        }

        protected override void CommitChunk(int versionId, int propertyTypeId, string token, long fullSize, ContentRepository.Storage.BinaryDataValue source = null)
        {
            throw new NotImplementedException();
        }

        protected override void CopyFromStream(int versionId, string token, Stream input)
        {
            throw new NotImplementedException();
        }

        protected override IndexBackup CreateBackup(int backupNumber)
        {
            throw new NotImplementedException();
        }

        protected override ContentRepository.Storage.Data.IDataProcedure CreateDataProcedureInternal(string commandText, ContentRepository.Storage.Data.ConnectionInfo connectionInfo)
        {
            throw new NotImplementedException();
        }

        protected override ContentRepository.Storage.Data.IDataProcedure CreateDataProcedureInternal(string commandText, string connectionName = null, ContentRepository.Storage.Data.InitialCatalog initialCatalog = 0)
        {
            throw new NotImplementedException();
        }

        protected override INodeWriter CreateNodeWriter()
        {
            throw new NotImplementedException();
        }

        protected override System.Data.IDbDataParameter CreateParameterInternal()
        {
            throw new NotImplementedException();
        }

        protected override SchemaWriter CreateSchemaWriter()
        {
            throw new NotImplementedException();
        }

        protected override DataOperationResult DeleteNodeTree(int nodeId)
        {
            throw new NotImplementedException();
        }

        protected override DataOperationResult DeleteNodeTreePsychical(int nodeId, long timestamp)
        {
            throw new NotImplementedException();
        }

        protected override void DeleteVersion(int versionId, NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            throw new NotImplementedException();
        }

        protected override string GetAppModelScriptPrivate(IEnumerable<string> paths, bool all, bool resolveChildren)
        {
            throw new NotImplementedException();
        }

        protected override ContentRepository.Storage.Data.BlobStorageContext GetBlobStorageContext(int fileId, bool clearStream = false, int versionId = 0, int propertyTypeId = 0)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<int> GetChildrenIdentfiers(int nodeId)
        {
            throw new NotImplementedException();
        }

        protected override List<ContentListType> GetContentListTypesInTree(string path)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<int> GetIdsOfNodesThatDoNotHaveIndexDocument(int fromId, int toId)
        {
            throw new NotImplementedException();
        }

        protected override IndexDocumentData GetIndexDocumentDataFromReader(System.Data.Common.DbDataReader reader)
        {
            throw new NotImplementedException();
        }

        protected override Guid GetLastIndexBackupNumber()
        {
            throw new NotImplementedException();
        }

        protected override NodeHead.NodeVersion[] GetNodeVersions(int nodeId)
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

        protected override long GetTreeSize(string path, bool includeChildren)
        {
            throw new NotImplementedException();
        }

        protected override VersionNumber[] GetVersionNumbers(string path)
        {
            throw new NotImplementedException();
        }

        protected override VersionNumber[] GetVersionNumbers(int nodeId)
        {
            throw new NotImplementedException();
        }

        protected override bool HasChild(int nodeId)
        {
            throw new NotImplementedException();
        }

        protected override void InitializeForTestsPrivate()
        {
            throw new NotImplementedException();
        }

        protected override void InstallDefaultSecurityStructure()
        {
            throw new NotImplementedException();
        }

        protected override int InstanceCount(int[] nodeTypeIds)
        {
            throw new NotImplementedException();
        }

        protected override bool IsCacheableText(string text)
        {
            throw new NotImplementedException();
        }

        protected override bool IsTreeLocked(string path)
        {
            throw new NotImplementedException();
        }

        protected override void KeepOnlyLastIndexBackup()
        {
            throw new NotImplementedException();
        }

        protected override Dictionary<int, string> LoadAllTreeLocks()
        {
            throw new NotImplementedException();
        }

        protected override ContentRepository.Storage.Data.BinaryCacheEntity LoadBinaryCacheEntity(int nodeVersionId, int propertyTypeId)
        {
            throw new NotImplementedException();
        }

        protected override byte[] LoadBinaryFragment(int fileId, long position, int count)
        {
            throw new NotImplementedException();
        }

        protected override ContentRepository.Storage.BinaryDataValue LoadBinaryPropertyValue(int versionId, int propertyTypeId)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<NodeType> LoadChildTypesToAllow(int sourceNodeId)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<IndexDocumentData> LoadIndexDocumentByVersionId(IEnumerable<int> versionId)
        {
            throw new NotImplementedException();
        }

        protected override IndexDocumentData LoadIndexDocumentByVersionId(int versionId)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<IndexDocumentData> LoadIndexDocumentsByPath(string path, int[] excludedNodeTypes)
        {
            throw new NotImplementedException();
        }

        protected override IndexBackup LoadLastBackup()
        {
            throw new NotImplementedException();
        }

        #endregion

        protected override NodeHead LoadNodeHead(int nodeId)
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

        protected override NodeHead LoadNodeHead(string path)
        {
            throw new NotImplementedException();
        }

        protected override NodeHead LoadNodeHeadByVersionId(int versionId)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<NodeHead> LoadNodeHeads(IEnumerable<int> heads)
        {
            throw new NotImplementedException();
        }

        #endregion

        protected override void LoadNodes(Dictionary<int, NodeBuilder> buildersByVersionId)
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

        protected override System.Data.DataSet LoadSchema()
        {
            var xml = new XmlDocument();
            xml.LoadXml(TestSchema);
            return SchemaRoot.BuildDataSetFromXml(xml);
        }

        #region NOT IMPLEMENTED
        protected override Stream LoadStream(int versionId, int propertyTypeId)
        {
            throw new NotImplementedException();
        }

        protected override string LoadTextPropertyValue(int versionId, int propertyTypeId)
        {
            throw new NotImplementedException();
        }

        protected override Dictionary<int, string> LoadTextPropertyValues(int versionId, int[] propertyTypeIds)
        {
            throw new NotImplementedException();
        }

        protected override DataOperationResult MoveNodeTree(int sourceNodeId, int targetNodeId, long sourceTimestamp = 0, long targetTimestamp = 0)
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

        protected override IEnumerable<int> QueryNodesByPath(string pathStart, bool orderByPath)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<int> QueryNodesByReferenceAndType(string referenceName, int referredNodeId, int[] allowedTypeIds)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<int> QueryNodesByType(int[] typeIds)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<int> QueryNodesByTypeAndPath(int[] nodeTypeIds, string[] pathStart, bool orderByPath)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<int> QueryNodesByTypeAndPath(int[] nodeTypeIds, string pathStart, bool orderByPath)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<int> QueryNodesByTypeAndPathAndName(int[] nodeTypeIds, string[] pathStart, bool orderByPath, string name)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<int> QueryNodesByTypeAndPathAndName(int[] nodeTypeIds, string pathStart, bool orderByPath, string name)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<int> QueryNodesByTypeAndPathAndProperty(int[] nodeTypeIds, string pathStart, bool orderByPath, List<QueryPropertyData> properties)
        {
            throw new NotImplementedException();
        }

        protected override IndexBackup RecoverIndexBackup(string backupFilePath)
        {
            throw new NotImplementedException();
        }

        protected override void ReleaseTreeLock(int[] lockIds)
        {
            throw new NotImplementedException();
        }

        protected override void Reset()
        {
            throw new NotImplementedException();
        }

        protected override void SetActiveBackup(IndexBackup backup, IndexBackup lastBackup)
        {
            throw new NotImplementedException();
        }

        protected override string StartChunk(int versionId, int propertyTypeId, long fullSize)
        {
            throw new NotImplementedException();
        }

        protected override void StoreBackupStream(string backupFilePath, IndexBackup backup, IndexBackupProgress progress)
        {
            throw new NotImplementedException();
        }

        protected override void UpdateIndexDocument(int versionId, byte[] indexDocumentBytes)
        {
            throw new NotImplementedException();
        }

        protected override void UpdateIndexDocument(NodeData nodeData, byte[] indexDocumentBytes)
        {
            throw new NotImplementedException();
        }

        protected override int VersionCount(string path)
        {
            throw new NotImplementedException();
        }

        protected override void WriteChunk(int versionId, string token, byte[] buffer, long offset, long fullSize)
        {
            throw new NotImplementedException();
        }

        protected override AuditLogEntry[] LoadLastAuditLogEntries(int count)
        {
            throw new NotImplementedException();
        }
        #endregion

        /* ====================================================================================== */

        #region CONSTANTS

        private static readonly string TestSchema = @"<?xml version='1.0' encoding='utf-8' ?>
<StorageSchema xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/Storage/Schema'>
	<UsedPropertyTypes>
		<PropertyType itemID='1' name='Binary' dataType='Binary' mapping='0' />
		<PropertyType itemID='2' name='VersioningMode' dataType='Int' mapping='0' />
		<PropertyType itemID='3' name='Make' dataType='String' mapping='0' />
		<PropertyType itemID='4' name='Model' dataType='String' mapping='1' />
		<PropertyType itemID='5' name='Style' dataType='String' mapping='2' />
		<PropertyType itemID='6' name='Color' dataType='String' mapping='3' />
		<PropertyType itemID='7' name='EngineSize' dataType='String' mapping='4' />
		<PropertyType itemID='8' name='Power' dataType='String' mapping='5' />
		<PropertyType itemID='9' name='Price' dataType='String' mapping='6' />
		<PropertyType itemID='10' name='Description' dataType='Text' mapping='0' />
		<PropertyType itemID='11' name='Enabled' dataType='Int' mapping='1' />
		<PropertyType itemID='12' name='Domain' dataType='String' mapping='7' />
		<PropertyType itemID='13' name='Email' dataType='String' mapping='8' />
		<PropertyType itemID='14' name='FullName' dataType='String' mapping='9' />
		<PropertyType itemID='15' name='PasswordHash' dataType='String' mapping='10' />
		<PropertyType itemID='16' name='Memberships' dataType='Binary' mapping='1' />
		<PropertyType itemID='17' name='PendingUserLang' dataType='String' mapping='11' />
		<PropertyType itemID='18' name='Language' dataType='Int' mapping='2' />
		<PropertyType itemID='19' name='Url' dataType='String' mapping='12' />
		<PropertyType itemID='20' name='AuthenticationType' dataType='String' mapping='13' />
		<PropertyType itemID='21' name='StartPage' dataType='String' mapping='14' />
		<PropertyType itemID='22' name='LoginPage' dataType='String' mapping='15' />
		<PropertyType itemID='23' name='StatisticalLog' dataType='Int' mapping='3' />
		<PropertyType itemID='24' name='AuditLog' dataType='Int' mapping='4' />
		<PropertyType itemID='26' name='PageNameInMenu' dataType='String' mapping='16' />
		<PropertyType itemID='27' name='Hidden' dataType='Int' mapping='6' />
		<PropertyType itemID='28' name='Keywords' dataType='String' mapping='17' />
		<PropertyType itemID='29' name='MetaDescription' dataType='String' mapping='18' />
		<PropertyType itemID='30' name='MetaTitle' dataType='String' mapping='19' />
		<PropertyType itemID='31' name='PageTemplateNode' dataType='Reference' mapping='0' />
		<PropertyType itemID='32' name='DefaultPortletSkin' dataType='String' mapping='20' />
		<PropertyType itemID='33' name='HiddenPageFrom' dataType='String' mapping='21' />
		<PropertyType itemID='34' name='Authors' dataType='String' mapping='22' />
		<PropertyType itemID='35' name='CustomMeta' dataType='String' mapping='23' />
		<PropertyType itemID='36' name='Comment' dataType='String' mapping='24' />
		<PropertyType itemID='37' name='PersonalizationSettings' dataType='Binary' mapping='2' />
		<PropertyType itemID='38' name='DisplayName' dataType='String' mapping='25' />
		<PropertyType itemID='39' name='Subtitle' dataType='String' mapping='26' />
		<PropertyType itemID='40' name='Header' dataType='Text' mapping='1' />
		<PropertyType itemID='41' name='Body' dataType='Text' mapping='2' />
		<PropertyType itemID='42' name='Links' dataType='Text' mapping='3' />
		<PropertyType itemID='43' name='ContentLanguage' dataType='String' mapping='27' />
		<PropertyType itemID='44' name='Author' dataType='String' mapping='28' />
		<PropertyType itemID='45' name='ContractId' dataType='String' mapping='29' />
		<PropertyType itemID='46' name='Project' dataType='String' mapping='30' />
		<PropertyType itemID='47' name='Responsee' dataType='String' mapping='31' />
		<PropertyType itemID='48' name='Lawyer' dataType='String' mapping='32' />
		<PropertyType itemID='49' name='MasterPageNode' dataType='Reference' mapping='1' />
		<PropertyType itemID='50' name='Members' dataType='Reference' mapping='2' />
		<PropertyType itemID='51' name='Manufacturer' dataType='String' mapping='33' />
		<PropertyType itemID='52' name='Driver' dataType='String' mapping='34' />
		<PropertyType itemID='53' name='InheritableVersioningMode' dataType='Int' mapping='35' />
		<PropertyType itemID='54' name='HasApproving' dataType='Int' mapping='36' />
	</UsedPropertyTypes>
	<NodeTypeHierarchy>
		<NodeType itemID='20' name='TestNode' className='SenseNet.SearchImpl.Tests.Implementations.TestNode'>
		</NodeType>
		<NodeType itemID='7' name='PersonalizationFile' className='SenseNet.ContentRepository.PersonalizationFile'>
			<PropertyType name='Binary' />
			<PropertyType name='VersioningMode' />
			<PropertyType name='InheritableVersioningMode' />
			<PropertyType name='HasApproving' />
		</NodeType>
		<NodeType itemID='5' name='GenericContent' className='SenseNet.ContentRepository.GenericContent'>
			<PropertyType name='VersioningMode' />
			<PropertyType name='InheritableVersioningMode' />
			<PropertyType name='HasApproving' />
			<NodeType itemID='3' name='User' className='SenseNet.ContentRepository.User'>
				<PropertyType name='VersioningMode' />
				<PropertyType name='Enabled' />
				<PropertyType name='Domain' />
				<PropertyType name='Email' />
				<PropertyType name='FullName' />
				<PropertyType name='PasswordHash' />
				<PropertyType name='Memberships' />
			</NodeType>
			<NodeType itemID='1' name='Folder' className='SenseNet.ContentRepository.Folder'>
				<PropertyType name='VersioningMode' />
				<NodeType itemID='16' name='Page' className='SenseNet.Portal.Page'>
					<PropertyType name='Binary' />
					<PropertyType name='PageNameInMenu' />
					<PropertyType name='Hidden' />
					<PropertyType name='Keywords' />
					<PropertyType name='MetaDescription' />
					<PropertyType name='MetaTitle' />
					<PropertyType name='PageTemplateNode' />
					<PropertyType name='DefaultPortletSkin' />
					<PropertyType name='HiddenPageFrom' />
					<PropertyType name='Authors' />
					<PropertyType name='CustomMeta' />
					<PropertyType name='Comment' />
					<PropertyType name='PersonalizationSettings' />
				</NodeType>
				<NodeType itemID='15' name='OrganizationalUnit' className='SenseNet.ContentRepository.OrganizationalUnit'>
				</NodeType>
				<NodeType itemID='14' name='Site' className='SenseNet.Portal.Site'>
					<PropertyType name='Description' />
					<PropertyType name='PendingUserLang' />
					<PropertyType name='Language' />
					<PropertyType name='Url' />
					<PropertyType name='AuthenticationType' />
					<PropertyType name='StartPage' />
					<PropertyType name='LoginPage' />
					<PropertyType name='StatisticalLog' />
					<PropertyType name='AuditLog' />
				</NodeType>
			</NodeType>
			<NodeType itemID='10' name='WebContentDemo' className='SenseNet.ContentRepository.GenericContent'>
				<PropertyType name='Keywords' />
				<PropertyType name='DisplayName' />
				<PropertyType name='Subtitle' />
				<PropertyType name='Header' />
				<PropertyType name='Body' />
				<PropertyType name='Links' />
				<PropertyType name='ContentLanguage' />
				<PropertyType name='Author' />
			</NodeType>
			<NodeType itemID='9' name='File' className='SenseNet.ContentRepository.File'>
				<PropertyType name='Binary' />
				<NodeType itemID='13' name='PageTemplate' className='SenseNet.Portal.PageTemplate'>
					<PropertyType name='MasterPageNode' />
				</NodeType>
				<NodeType itemID='12' name='Contract' className='SenseNet.ContentRepository.File'>
					<PropertyType name='Description' />
					<PropertyType name='Language' />
					<PropertyType name='Keywords' />
					<PropertyType name='ContractId' />
					<PropertyType name='Project' />
					<PropertyType name='Responsee' />
					<PropertyType name='Lawyer' />
				</NodeType>
				<NodeType itemID='11' name='MasterPage' className='SenseNet.Portal.MasterPage' />
			</NodeType>
			<NodeType itemID='8' name='Car' className='SenseNet.ContentRepository.GenericContent'>
				<PropertyType name='Make' />
				<PropertyType name='Model' />
				<PropertyType name='Style' />
				<PropertyType name='Color' />
				<PropertyType name='EngineSize' />
				<PropertyType name='Power' />
				<PropertyType name='Price' />
				<PropertyType name='Description' />
			</NodeType>
		</NodeType>
		<NodeType itemID='4' name='ContentType' className='SenseNet.ContentRepository.Schema.ContentType'>
			<PropertyType name='Binary' />
		</NodeType>
		<NodeType itemID='2' name='Group' className='SenseNet.ContentRepository.Group'>
			<PropertyType name='VersioningMode' />
			<PropertyType name='Members' />
		</NodeType>
	</NodeTypeHierarchy>
	<PermissionTypes>
		<PermissionType itemID='1' name='See' />
		<PermissionType itemID='2' name='Open' />
		<PermissionType itemID='3' name='OpenMinor' />
		<PermissionType itemID='4' name='Save' />
		<PermissionType itemID='5' name='Publish' />
		<PermissionType itemID='6' name='ForceCheckin' />
		<PermissionType itemID='7' name='AddNew' />
		<PermissionType itemID='8' name='Approve' />
		<PermissionType itemID='9' name='Delete' />
		<PermissionType itemID='10' name='RecallOldVersion' />
		<PermissionType itemID='11' name='DeleteOldVersion' />
		<PermissionType itemID='12' name='SeePermissions' />
		<PermissionType itemID='13' name='SetPermissions' />
		<PermissionType itemID='14' name='RunApplication' />
	</PermissionTypes>
</StorageSchema>
";

        //private static Dictionary<int, NodeHead> NodeHeadsById = new Dictionary<int, NodeHead>
        //{
        //    {
        //        1,
        //        new NodeHead(1, "Admin", "Admin", "/Root/IMS/BuiltIn/Portal/Admin", 5, 3, 0, 0, DateTime.MinValue,
        //            DateTime.MinValue, 1, 1, 1, 1, 1, 1, 0, 0L)
        //    }
        //};

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
            }
        };

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
            }
        };

        #endregion

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
            public IEnumerable<ChangedData> ChangedData;
            public long VersionTimestamp;
        }
    }
}
