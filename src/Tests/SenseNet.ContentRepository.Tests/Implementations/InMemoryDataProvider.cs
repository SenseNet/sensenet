using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Security;

namespace SenseNet.ContentRepository.Tests.Implementations
{
    public partial class InMemoryDataProvider : DataProvider
    {
        public override IMetaQueryEngine MetaQueryEngine { get { return null; } }

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
            //UNDONE:|||| TEST: AssertSchemaTimestampAndWriteModificationDate is not implemented in the InMemoryDataProvider
            // do nothimg
        }
        #endregion

        public override ITransactionProvider CreateTransaction()
        {
            return new TestTransaction();
        }

        public override void DeleteAllIndexingActivities()
        {
            _db.IndexingActivity.Clear();
        }

        #region NOT IMPLEMENTED

        public override void DeleteAllPackages()
        {
            throw new NotImplementedException();
        }

        public override void DeletePackage(Package package)
        {
            throw new NotImplementedException();
        }

        #endregion

        public override int GetLastActivityId()
        {
            return _db.IndexingActivity.Count == 0 ? 0 : _db.IndexingActivity.Max(r => r.IndexingActivityId);
        }

        #region NOT IMPLEMENTED

        public override string GetNameOfLastNodeWithNameBase(int parentId, string namebase, string extension)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<string> GetScriptsForDatabaseBackup()
        {
            throw new NotImplementedException();
        }

        public override IDataProcedure GetTimestampDataForOneNodeIntegrityCheck(string path, int[] excludedNodeTypeIds)
        {
            throw new NotImplementedException();
        }

        public override IDataProcedure GetTimestampDataForRecursiveIntegrityCheck(string path, int[] excludedNodeTypeIds)
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

        #endregion

        public override IIndexingActivity[] LoadIndexingActivities(int fromId, int toId, int count, bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory)
        {
            var result = new List<IIndexingActivity>();

            var activities = _db.IndexingActivity.Where(r => r.IndexingActivityId >= fromId && r.IndexingActivityId <= toId).Take(count).ToArray();
            foreach (var activityRecord in activities)
            {
                var nodeRecord = _db.Nodes.FirstOrDefault(r => r.NodeId == activityRecord.NodeId);
                var versionRecord = _db.Versions.FirstOrDefault(r => r.VersionId == activityRecord.VersionId);
                var activity = activityFactory.CreateActivity(activityRecord.ActivityType);

                activity.Id = activityRecord.IndexingActivityId;
                activity.ActivityType = activityRecord.ActivityType;
                activity.NodeId = activityRecord.NodeId;
                activity.VersionId = activityRecord.VersionId;
                activity.SingleVersion = activityRecord.SingleVersion;
                activity.MoveOrRename = activityRecord.MoveOrRename;
                activity.Path = activityRecord.Path;
                activity.FromDatabase = true;
                activity.IsUnprocessedActivity = executingUnprocessedActivities;
                activity.Extension = activityRecord.Extension;

                if (versionRecord?.IndexDocument != null && nodeRecord != null)
                {
                    activity.IndexDocumentData = new IndexDocumentData(null, versionRecord.IndexDocument)
                    {
                        NodeTypeId = nodeRecord.NodeTypeId,
                        VersionId = activity.VersionId,
                        NodeId = activity.NodeId,
                        ParentId = nodeRecord.ParentNodeId,
                        Path = activity.Path,
                        IsSystem = nodeRecord.IsSystem,
                        IsLastDraft = nodeRecord.LastMinorVersionId == activity.VersionId,
                        IsLastPublic = versionRecord.Version.Status == VersionStatus.Approved && nodeRecord.LastMajorVersionId == activity.VersionId
                        //NodeTimestamp = nodeTimeStamp,
                        //VersionTimestamp = versionTimestamp,
                    };
                }

                result.Add(activity);
            }
            return result.ToArray();
        }

        #region NOT IMPLEMENTED

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
            var newId = _db.IndexingActivity.Count == 0 ? 1 : _db.IndexingActivity.Max(r => r.IndexingActivityId) + 1;

            _db.IndexingActivity.Add(new IndexingActivityRecord
            {
                IndexingActivityId = newId,
                ActivityType = activity.ActivityType,
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
        #endregion


        #region // ====================================================== Tree lock

        protected internal override int AcquireTreeLock(string path)
        {
            var parentChain = GetParentChain(path);
            var timeMin = GetObsoleteLimitTime();

            if (_db.TreeLocks
                .Any(t => t.LockedAt > timeMin &&
                          (parentChain.Contains(t.Path) ||
                           t.Path.StartsWith(path + "/", StringComparison.InvariantCultureIgnoreCase))))
            return 0;

            var newTreeLockId = _db.TreeLocks.Count == 0 ? 1 : _db.TreeLocks.Max(t => t.TreeLockId) + 1;
            _db.TreeLocks.Add(new TreeLockRow
            {
                TreeLockId = newTreeLockId,
                Path = path,
                LockedAt = DateTime.Now
            });

            return newTreeLockId;
        }
        protected internal override bool IsTreeLocked(string path)
        {
            var parentChain = GetParentChain(path);
            var timeMin = GetObsoleteLimitTime();

            return _db.TreeLocks
                .Any(t => t.LockedAt > timeMin &&
                          (parentChain.Contains(t.Path) ||
                           t.Path.StartsWith(path + "/", StringComparison.InvariantCultureIgnoreCase)));
        }
        protected internal override System.Collections.Generic.Dictionary<int, string> LoadAllTreeLocks()
        {
            return _db.TreeLocks.ToDictionary(t => t.TreeLockId, t => t.Path);
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

        #endregion


        #region NOT IMPLEMENTED

        protected internal override void CheckScriptInternal(string commandText)
        {
            throw new NotImplementedException();
        }

        protected internal override void CommitChunk(int versionId, int propertyTypeId, string token, long fullSize, BinaryDataValue source = null)
        {
            throw new NotImplementedException();
        }

        protected internal override void CopyFromStream(int versionId, string token, Stream input)
        {
            throw new NotImplementedException();
        }

        protected internal override IDataProcedure CreateDataProcedureInternal(string commandText, ConnectionInfo connectionInfo)
        {
            throw new NotImplementedException();
        }

        protected internal override IDataProcedure CreateDataProcedureInternal(string commandText, string connectionName = null, InitialCatalog initialCatalog = 0)
        {
            throw new NotImplementedException();
        }

        protected internal override INodeWriter CreateNodeWriter()
        {
            return new InMemoryNodeWriter(_db);
        }

        protected override IDbDataParameter CreateParameterInternal()
        {
            throw new NotImplementedException();
        }

        protected internal override SchemaWriter CreateSchemaWriter()
        {
            return new InMemorySchemaWriter(_db.Schema);
        }

        protected internal override DataOperationResult DeleteNodeTree(int nodeId)
        {
            throw new NotImplementedException();
        }
        #endregion

        protected internal override DataOperationResult DeleteNodeTreePsychical(int nodeId, long timestamp)
        {
            var nodeRec = _db.Nodes.FirstOrDefault(n => n.NodeId == nodeId);
            if(nodeRec == null)
                return DataOperationResult.Successful;

            var path = nodeRec.Path;
            var nodeIds = _db.Nodes
                .Where(n => n.Path.StartsWith(path, StringComparison.InvariantCultureIgnoreCase))
                .Select(n => n.NodeId)
                .ToArray();
            var versionIds = _db.Versions
                .Where(v => nodeIds.Contains(v.VersionId))
                .Select(v => v.VersionId)
                .ToArray();
            var fileIds = _db.BinaryProperties
                .Where(b => versionIds.Contains(b.VersionId))
                .Select(b => b.FileId)
                .ToArray();

            _db.BinaryProperties.RemoveAll(r => versionIds.Contains(r.VersionId));
            _db.Files.RemoveAll(r => fileIds.Contains(r.FileId));
            _db.TextProperties.RemoveAll(r => versionIds.Contains(r.VersionId));
            _db.Versions.RemoveAll(r => versionIds.Contains(r.VersionId));
            _db.Nodes.RemoveAll(r => nodeIds.Contains(r.NodeId));

            return DataOperationResult.Successful;
        }

        #region NOT IMPLEMENTED

        protected internal override void DeleteVersion(int versionId, NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            throw new NotImplementedException();
        }

        protected override string GetAppModelScriptPrivate(IEnumerable<string> paths, bool all, bool resolveChildren)
        {
            throw new NotImplementedException();
        }

        protected internal override BlobStorageContext GetBlobStorageContext(int fileId, bool clearStream = false, int versionId = 0, int propertyTypeId = 0)
        {
            throw new NotImplementedException();
        }

        protected internal override IEnumerable<int> GetChildrenIdentfiers(int nodeId)
        {
            throw new NotImplementedException();
        }
        #endregion

        protected internal override List<ContentListType> GetContentListTypesInTree(string path)
        {
            return _db.Nodes
                .Where(n => n.ContentListId == 0 && n.ContentListTypeId != 0 &&
                        n.Path.StartsWith(path, StringComparison.InvariantCultureIgnoreCase))
                .Select(n => NodeTypeManager.Current.ContentListTypes.GetItemById(n.ContentListTypeId))
                .ToList();
        }

        protected internal override IEnumerable<int> GetIdsOfNodesThatDoNotHaveIndexDocument(int fromId, int toId)
        {
            return _db.Versions.Where(v => v.IndexDocument == null).Select(v => v.NodeId).ToArray();
        }

        #region NOT IMPLEMENTED

        protected internal override IndexDocumentData GetIndexDocumentDataFromReader(DbDataReader reader)
        {
            throw new NotImplementedException();
        }
        #endregion

        protected internal override NodeHead.NodeVersion[] GetNodeVersions(int nodeId)
        {
            return _db.Versions
                .Where(v => v.NodeId == nodeId)
                .Select(v =>new NodeHead.NodeVersion(v.Version, v.VersionId))
                .ToArray();
        }

        #region NOT IMPLEMENTED

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
        #endregion

        protected internal override bool IsCacheableText(string text)
        {
            return false;
        }

        protected internal override BinaryCacheEntity LoadBinaryCacheEntity(int nodeVersionId, int propertyTypeId)
        {

            // SELECT F.Size, B.BinaryPropertyId, F.FileId, F.BlobProvider, F.BlobProviderData,
            //        CASE WHEN F.Size < 1048576 THEN F.Stream ELSE null END AS Stream
            // FROM dbo.BinaryProperties B
            //     JOIN Files F ON B.FileId = F.FileId
            // WHERE B.VersionId = @VersionId AND B.PropertyTypeId = @PropertyTypeId AND F.Staging IS NULL";

            var binRec = _db.BinaryProperties
                .FirstOrDefault(r => r.VersionId == nodeVersionId && r.PropertyTypeId == propertyTypeId);
            if (binRec == null)
                return null;
            var fileRec = _db.Files.FirstOrDefault(f => f.FileId == binRec.FileId);
            if (fileRec == null)
                return null;

            var length = fileRec.Size;
            var binaryPropertyId = binRec.BinaryPropertyId;
            var fileId = binRec.FileId;

            // To avoid accessing to blob provider, read data here, else set rawData to null
            byte[] rawData = fileRec.Stream;

            var provider = BlobStorageBase.GetProvider(null);
            var context = new BlobStorageContext(provider) { VersionId = nodeVersionId, PropertyTypeId = propertyTypeId, FileId = fileId, Length = length, UseFileStream = false };
            if (provider == BlobStorageBase.BuiltInProvider)
                context.BlobProviderData = new BuiltinBlobProviderData { FileStreamData = null };

            return new BinaryCacheEntity
            {
                Length = length,
                RawData = rawData,
                BinaryPropertyId = binaryPropertyId,
                FileId = fileId,
                Context = context
            };
        }

        #region NOT IMPLEMENTED

        protected internal override byte[] LoadBinaryFragment(int fileId, long position, int count)
        {
            throw new NotImplementedException();
        }

        #endregion

        protected internal override BinaryDataValue LoadBinaryPropertyValue(int versionId, int propertyTypeId)
        {
            //ALTER PROCEDURE [dbo].[proc_BinaryProperty_LoadValue]
            //	@VersionId int,
            //	@PropertyTypeId int
            //AS
            //BEGIN
            //	SELECT B.BinaryPropertyId, B.VersionId, B.PropertyTypeId, F.FileId, F.ContentType, F.FileNameWithoutExtension,
            //		F.Extension, F.[Size], F.[Checksum], NULL AS Stream, 0 AS Loaded, F.[Timestamp], F.[BlobProvider], F.[BlobProviderData] 
            //	FROM dbo.BinaryProperties B
            //		JOIN dbo.Files F ON B.FileId = F.FileId
            //	WHERE VersionId = @VersionId AND PropertyTypeId = @PropertyTypeId AND Staging IS NULL
            //END

            var binRec = _db.BinaryProperties
                .FirstOrDefault(r => r.VersionId == versionId && r.PropertyTypeId == propertyTypeId);
            if (binRec == null)
                return null;
            var fileRec = _db.Files.FirstOrDefault(f => f.FileId == binRec.FileId);
            if (fileRec == null)
                return null;

            string ext = fileRec.Extension ?? string.Empty;
            if (ext.Length != 0)
                ext = ext.Remove(0, 1); // Remove dot from the start if extension is not empty
            string fn = fileRec.FileNameWithoutExtension ?? string.Empty;

            return new BinaryDataValue
            {
                Id = binRec.BinaryPropertyId,
                FileId = binRec.FileId,
                Checksum = null, // not supported
                FileName = new BinaryFileName(fn, ext),
                ContentType = fileRec.ContentType,
                Size = fileRec.Size,
                Timestamp = 0L // not supported
            };
        }

        #region NOT IMPLEMENTED

        protected internal override IEnumerable<NodeType> LoadChildTypesToAllow(int sourceNodeId)
        {
            throw new NotImplementedException();
        }
        #endregion

        protected internal override IEnumerable<IndexDocumentData> LoadIndexDocumentByVersionId(IEnumerable<int> versionId)
        {
            return versionId.Select(LoadIndexDocumentByVersionId).Where(i => i != null).ToArray();
        }

        protected internal override IndexDocumentData LoadIndexDocumentByVersionId(int versionId)
        {
            var version = _db.Versions.FirstOrDefault(v => v.VersionId == versionId);
            if (version == null)
                return null;
            var node = _db.Nodes.FirstOrDefault(n => n.NodeId == version.NodeId);
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
                IsLastPublic = approved && isLastMajor
                //NodeTimestamp = node.Timestamp,
                //VersionTimestamp = version.Timestamp,
            };
        }
        private IndexDocumentData CreateIndexDocumentData(NodeRecord node, VersionRecord version)
        {
            var approved = version.Version.Status == VersionStatus.Approved;
            var isLastMajor = node.LastMajorVersionId == version.VersionId;

            var bytes = version.IndexDocument ?? new byte[0];

            return new IndexDocumentData(null, bytes)
            {
                NodeTypeId = node.NodeTypeId,
                VersionId = version.VersionId,
                NodeId = node.NodeId,
                ParentId = node.ParentNodeId,
                Path = node.Path,
                IsSystem = node.IsSystem,
                IsLastDraft = node.LastMinorVersionId == version.VersionId,
                IsLastPublic = approved && isLastMajor
                //NodeTimestamp = node.Timestamp,
                //VersionTimestamp = version.Timestamp,
            };
        }

        protected internal override IEnumerable<IndexDocumentData> LoadIndexDocumentsByPath(string path, int[] excludedNodeTypes)
        {
            var result = new List<IndexDocumentData>();
            var pathExt = path + "/";

            foreach (var node in _db.Nodes.Where(n => n.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase) ||
                                                      n.Path.StartsWith(pathExt, StringComparison.InvariantCultureIgnoreCase)).ToArray())
                foreach (var version in _db.Versions.Where(v => v.NodeId == node.NodeId).ToArray())
                    result.Add(CreateIndexDocumentData(node, version));

            return result;
        }

        protected internal override NodeHead LoadNodeHead(int nodeId)
        {
            return CreateNodeHead(_db.Nodes.FirstOrDefault(r => r.NodeId == nodeId));
        }
        protected internal override NodeHead LoadNodeHead(string path)
        {
            return
                CreateNodeHead(
                    _db.Nodes.FirstOrDefault(r => string.Equals(r.Path, path, StringComparison.InvariantCultureIgnoreCase)));
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

        protected internal override NodeHead LoadNodeHeadByVersionId(int versionId)
        {
            var version = _db.Versions.FirstOrDefault(v => v.VersionId == versionId);
            if (version == null)
                return null;
            return LoadNodeHead(version.NodeId);
        }

        protected internal override IEnumerable<NodeHead> LoadNodeHeads(IEnumerable<int> heads)
        {
            return _db.Nodes.Where(n => heads.Contains(n.NodeId)).Select(CreateNodeHead);
        }

        protected internal override void LoadNodes(System.Collections.Generic.Dictionary<int, NodeBuilder> buildersByVersionId)
        {
            foreach (var versionId in buildersByVersionId.Keys)
            {
                var version = _db.Versions.FirstOrDefault(r => r.VersionId == versionId);
                if (version == null)
                    continue;
                var node = _db.Nodes.FirstOrDefault(r => r.NodeId == version.NodeId);
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

                foreach (var flatPropertRow in _db.FlatProperties.Where(r => r.VersionId == versionId))
                {
                    foreach (PropertyType pt in builder.Token.AllPropertyTypes)
                    {
                        object value;
                        if(GetDataSlot(flatPropertRow, flatPropertRow.Page, pt, out value))
                            builder.AddDynamicProperty(pt, value);
                    }
                }

                var referenceCollector = new Dictionary<int, List<int>>();
                foreach (var row in _db.ReferenceProperties.Where(r => r.VersionId == versionId))
                {
                    List<int> refList;
                    if (!referenceCollector.TryGetValue(row.PropertyTypeId, out refList))
                        referenceCollector.Add(row.PropertyTypeId, refList = new List<int>());
                    refList.Add(row.ReferredNodeId);
                }
                foreach (var item in referenceCollector)
                    builder.AddDynamicProperty(item.Key, item.Value);
            }

            foreach (var builder in buildersByVersionId.Values)
                builder.Finish();
        }
        private static bool GetDataSlot(FlatPropertyRow row, int queryedPage, PropertyType pt, out object value)
        {
            value = null;
            int pageSize;
            switch (pt.DataType)
            {
                case DataType.String:
                    pageSize = SqlProvider.StringPageSize;
                    break;
                case DataType.Int:
                    pageSize = SqlProvider.IntPageSize;
                    break;
                case DataType.DateTime:
                    pageSize = SqlProvider.DateTimePageSize;
                    break;
                case DataType.Currency:
                    pageSize = SqlProvider.CurrencyPageSize;
                    break;
                default:
                    return false;
            }

            var mappingIndex = pt.Mapping;
            var page = mappingIndex / pageSize;
            if (page != queryedPage)
                return false;

            int index = mappingIndex - (page * pageSize);

            switch (pt.DataType)
            {
                case DataType.String: value = row.Strings[index]; return true;
                case DataType.Int: value = row.Integers[index]; return true;
                case DataType.Currency: value = row.Decimals[index]; return true;
                case DataType.DateTime: value = row.Datetimes[index]; return true;
            }

            return false;
        }


        protected internal override DataSet LoadSchema()
        {
            return SchemaRoot.BuildDataSetFromXml(_db.Schema);
        }

        #region NOT IMPLEMENTED

        [Obsolete("Use GetStream method on a BinaryData instance instead.", true)]
        protected internal override Stream LoadStream(int versionId, int propertyTypeId)
        {
            throw new NotImplementedException();
        }

        protected internal override string LoadTextPropertyValue(int versionId, int propertyTypeId)
        {
            throw new NotImplementedException();
        }
        #endregion
        protected internal override System.Collections.Generic.Dictionary<int, string> LoadTextPropertyValues(int versionId, int[] propertyTypeIds)
        {
            return _db.TextProperties
                .Where(t => t.VersionId == versionId && propertyTypeIds.Contains(t.PropertyTypeId))
                .ToDictionary(t => t.PropertyTypeId, t => t.Value);
        }

        #region NOT IMPLEMENTED

        protected internal override DataOperationResult MoveNodeTree(int sourceNodeId, int targetNodeId, long sourceTimestamp = 0, long targetTimestamp = 0)
        {
            throw new NotImplementedException();
        }
        #endregion 

        protected override int NodeCount(string path)
        {
            return (string.IsNullOrEmpty(path) || path == RepositoryPath.PathSeparator)
                ? _db.Nodes.Count
                : _db.Nodes.Count(
                    n =>
                        n.Path.StartsWith(path + RepositoryPath.PathSeparator,
                            StringComparison.InvariantCultureIgnoreCase)
                        || n.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase));
        }

        #region NOT IMPLEMENTED

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
        #endregion

        protected internal override IEnumerable<int> QueryNodesByType(int[] typeIds)
        {
            return QueryNodesByTypeAndPath(typeIds, new string[0], false);
        }

        protected internal override IEnumerable<int> QueryNodesByTypeAndPath(int[] nodeTypeIds, string[] pathStart, bool orderByPath)
        {
            return QueryNodesByTypeAndPathAndName(nodeTypeIds, pathStart, orderByPath, null);
        }

        protected internal override IEnumerable<int> QueryNodesByTypeAndPath(int[] nodeTypeIds, string pathStart, bool orderByPath)
        {
            return QueryNodesByTypeAndPathAndName(nodeTypeIds, new[] { pathStart }, orderByPath, null);
        }

        protected internal override IEnumerable<int> QueryNodesByTypeAndPathAndName(int[] nodeTypeIds, string[] pathStart, bool orderByPath, string name)
        {
            var nodes = _db.Nodes;
            if (nodeTypeIds != null)
                nodes = nodes
                    .Where(n => nodeTypeIds.Contains(n.NodeTypeId))
                    .ToList();

            if (name != null)
                nodes = nodes
                    .Where(n => n.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();

            if (pathStart != null)
                nodes = nodes
                    .Where(n => pathStart.Any(p => n.Path.StartsWith(p, StringComparison.InvariantCultureIgnoreCase)))
                    .ToList();

            if (orderByPath)
                nodes = nodes
                    .OrderBy(n => n.Path)
                    .ToList();

            var ids = nodes.Select(n => n.NodeId);
            return ids.ToArray();
        }

        #region NOT IMPLEMENTED

        protected internal override IEnumerable<int> QueryNodesByTypeAndPathAndName(int[] nodeTypeIds, string pathStart, bool orderByPath, string name)
        {
            throw new NotImplementedException();
        }

        protected internal override IEnumerable<int> QueryNodesByTypeAndPathAndProperty(int[] nodeTypeIds, string pathStart, bool orderByPath, List<QueryPropertyData> properties)
        {
            throw new NotImplementedException();
        }
        #endregion

        protected internal override void ReleaseTreeLock(int[] lockIds)
        {
            foreach (var item in _db.TreeLocks.Where(t => lockIds.Contains(t.TreeLockId)).ToArray())
                _db.TreeLocks.Remove(item);
        }

        #region NOT IMPLEMENTED

        protected internal override void Reset()
        {
            // read the same method of the SqlProvider
        }

        protected internal override string StartChunk(int versionId, int propertyTypeId, long fullSize)
        {
            throw new NotImplementedException();
        }
        #endregion

        protected internal override void UpdateIndexDocument(int versionId, byte[] indexDocumentBytes)
        {
            var versionRow = _db.Versions.FirstOrDefault(r => r.VersionId == versionId);
            if (versionRow == null)
                return;
            versionRow.IndexDocument = indexDocumentBytes;
        }

        protected internal override void UpdateIndexDocument(NodeData nodeData, byte[] indexDocumentBytes)
        {
            var versionRow = _db.Versions.FirstOrDefault(r => r.VersionId == nodeData.VersionId);
            if (versionRow == null)
                return;
            versionRow.IndexDocument = indexDocumentBytes;
        }

        protected override int VersionCount(string path)
        {
            if (string.IsNullOrEmpty(path) || path == RepositoryPath.PathSeparator)
                return _db.Versions.Count;

            var count = _db.Nodes.Join(_db.Versions, n => n.NodeId, v => v.NodeId,
                    (node, version) => new {Node = node, Version = version})
                .Count(
                    x =>
                        x.Node.Path.StartsWith(path + RepositoryPath.PathSeparator,
                            StringComparison.InvariantCultureIgnoreCase)
                        || x.Node.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase));
            return count;
        }

        #region NOT IMPLEMENTED

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

        public IEnumerable<StoredSecurityEntity> GetSecurityEntities()
        {
            return _db.Nodes.Select(n => new StoredSecurityEntity
            {
                Id = n.NodeId,
                IsInherited = true,
                nullableOwnerId = n.OwnerId,
                nullableParentId = n.ParentNodeId
            }).ToArray();
        }

        public int LastNodeId
        {
            get { return _db.Nodes.Max(n => n.NodeId); }
        }

        /* ====================================================================================== Database */

        #region CREATION

        // Preloade CTD bytes by name
        private static readonly System.Collections.Generic.Dictionary<string, byte[]> ContentTypeBytes;
        static InMemoryDataProvider()
        {
            // Preload CTD bytes from disk to avoid heavy IO charging

            ContentTypeBytes = new System.Collections.Generic.Dictionary<string, byte[]>();

            var ctdDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\nuget\snadmin\install-services\import\System\Schema\ContentTypes"));

            foreach (var ctdPath in Directory.GetFiles(ctdDirectory, "*.xml"))
            {
                var stream = new MemoryStream();
                byte[] bytes;
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                using (var reader = new StreamReader(ctdPath))
                {
                    writer.Write(reader.ReadToEnd());
                    writer.Flush();
                    var buffer = stream.GetBuffer();
                    bytes = new byte[stream.Length];
                    Array.Copy(buffer, bytes, bytes.Length);
                }
                ContentTypeBytes.Add(Path.GetFileNameWithoutExtension(ctdPath).ToLowerInvariant(), bytes);
            }
        }

        private static byte[] GetContentTypeBytes(string name)
        {
            name = name.ToLowerInvariant();
            if (!ContentTypeBytes.ContainsKey(name))
            {
                name = name + "ctd";
                if (!ContentTypeBytes.ContainsKey(name))
                    return new byte[0];
            }
            return ContentTypeBytes[name];
        }

        private readonly Database _db;

        public InMemoryDataProvider() //UNDONE:|||||||| REFACTOR: REFACTOR split to private methods
        {
            _db = new Database();

            _db.Schema = new XmlDocument();
            _db.Schema.LoadXml(_initialSchema);

            var skip = _initialNodes.StartsWith("NodeId") ? 1 : 0;
            _db.Nodes = _initialNodes.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
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

            // -------------------------------------------------------------------------------------

            _db.Versions = _db.Nodes.Select(n => new VersionRecord
            {
                VersionId = n.LastMajorVersionId,
                NodeId = n.NodeId,
                Version = VersionNumber.Parse("1.0.A"),
                CreatedById = 1,
                ModifiedById = 1
            }).ToList();

            // -------------------------------------------------------------------------------------

            skip = _initialBinaryProperties.StartsWith("BinaryPropertyId") ? 1 : 0;
            _db.BinaryProperties = _initialBinaryProperties.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Skip(skip)
                .Select(l =>
                {
                    var record = l.Split('\t');
                    return new BinaryPropertyRecord
                    {
                        BinaryPropertyId= int.Parse(record[0]),
                        VersionId = int.Parse(record[1]),
                        PropertyTypeId = int.Parse(record[2]),
                        FileId = int.Parse(record[3])
                    };
                }).ToList();

            // -------------------------------------------------------------------------------------

            skip = _initialFiles.StartsWith("FileId") ? 1 : 0;
            _db.Files = _initialFiles.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Skip(skip)
                .Select(l =>
                {
                    var record = l.Split('\t');
                    var name = record[2];
                    var ext = record[3];

                    var bytes = ext == ".ContentType" ? GetContentTypeBytes(name) : new byte[0];

                    return new FileRecord
                    {
                        FileId = int.Parse(record[0]),
                        ContentType = record[1],
                        FileNameWithoutExtension = name,
                        Extension = ext,
                        Size = bytes.LongLength,
                        Stream = bytes
                    };
                }).ToList();

            // -------------------------------------------------------------------------------------

            skip = _initialTextProperties.StartsWith("TextPropertyNVarcharId") ? 1 : 0;
            _db.TextProperties = _initialTextProperties.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Skip(skip)
                .Select(l =>
                {
                    var record = l.Split('\t');
                    var value = record[3].Replace(@"\n", Environment.NewLine).Replace(@"\t", "\t");
                    return new TextPropertyRecord
                    {
                        TextPropertyNVarcharId = int.Parse(record[0]),
                        VersionId = int.Parse(record[1]),
                        PropertyTypeId = int.Parse(record[2]),
                        Value = value
                    };
                }).ToList();

            // -------------------------------------------------------------------------------------

            // nvarchars
            skip = _initialFlatPropertiesNvarchar.StartsWith("Id") ? 1 : 0;
            var flatRows = _initialFlatPropertiesNvarchar.Split("\r\n".ToCharArray(),
                    StringSplitOptions.RemoveEmptyEntries)
                .Skip(skip)
                .Select(l =>
                {
                    var record = l.Split('\t');
                    return new FlatPropertyRow
                    {
                        Id = int.Parse(record[0]),
                        VersionId = int.Parse(record[1]),
                        Page = int.Parse(record[2]),
                        Strings = record.Skip(3).ToArray(),
                    };
                }).ToDictionary(x => x.Id, x => x);

            // integers
            skip = _initialFlatPropertiesInt.StartsWith("Id") ? 1 : 0;
            foreach (var item in _initialFlatPropertiesInt.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Skip(skip))
            {
                var record = item.Split('\t');
                var id = int.Parse(record[0]);
                var intValues = record.Skip(3).Select(x => x == "NULL" ? (int?)null : int.Parse(x)).ToArray();

                FlatPropertyRow row;
                if (!flatRows.TryGetValue(id, out row))
                {
                    row = new FlatPropertyRow
                    {
                        Id = int.Parse(record[0]),
                        VersionId = int.Parse(record[1]),
                        Page = int.Parse(record[2]),
                    };
                    flatRows.Add(row.Id, row);
                }
                row.Integers = intValues;
            }

            // datetimes
            skip = _initialFlatPropertiesDatetime.StartsWith("Id") ? 1 : 0;
            foreach (var item in _initialFlatPropertiesDatetime.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Skip(skip))
            {
                var record = item.Split('\t');
                var id = int.Parse(record[0]);
                var dateTimeValues = record.Skip(3).Select(x => x == "NULL" ? (DateTime?)null : DateTime.Parse(x)).ToArray();

                FlatPropertyRow row;
                if (!flatRows.TryGetValue(id, out row))
                {
                    row = new FlatPropertyRow
                    {
                        Id = int.Parse(record[0]),
                        VersionId = int.Parse(record[1]),
                        Page = int.Parse(record[2]),
                    };
                    flatRows.Add(row.Id, row);
                }
                row.Datetimes = dateTimeValues;
            }

            // decimals
            skip = _initialFlatPropertiesDecimal.StartsWith("Id") ? 1 : 0;
            foreach (var item in _initialFlatPropertiesDecimal.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Skip(skip))
            {
                var record = item.Split('\t');
                var id = int.Parse(record[0]);
                var decimalValues = record.Skip(3).Select(x => x == "NULL" ? (decimal?)null : decimal.Parse(x, CultureInfo.InvariantCulture)).ToArray();

                FlatPropertyRow row;
                if (!flatRows.TryGetValue(id, out row))
                {
                    row = new FlatPropertyRow
                    {
                        Id = int.Parse(record[0]),
                        VersionId = int.Parse(record[1]),
                        Page = int.Parse(record[2]),
                    };
                    flatRows.Add(row.Id, row);
                }
                row.Decimals = decimalValues;
            }

            _db.FlatProperties = flatRows.Values.ToList();

            // -------------------------------------------------------------------------------------

            skip = _initialReferenceProperties.StartsWith("ReferencePropertyId") ? 1 : 0;
            _db.ReferenceProperties = _initialReferenceProperties.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Skip(skip)
                .Select(l =>
                {
                    var record = l.Split('\t');
                    return new ReferencePropertyRow
                    {
                        ReferencePropertyId = int.Parse(record[0]),
                        VersionId = int.Parse(record[1]),
                        PropertyTypeId = int.Parse(record[2]),
                        ReferredNodeId = int.Parse(record[3])
                    };
                }).ToList();

        }

        #endregion

        #region Implementation classes

        private class TestTransaction : ITransactionProvider
        {
            private static long _lastId;

            public TestTransaction()
            {
                Id = Interlocked.Increment(ref _lastId);
            }

            public void Dispose()
            {
                // do nothing
            }

            public long Id { get; }
            public DateTime Started { get; private set; }
            public IsolationLevel IsolationLevel { get; private set; }
            public void Begin(IsolationLevel isolationLevel)
            {
                IsolationLevel = isolationLevel;
                Started = DateTime.Now;
            }

            public void Begin(IsolationLevel isolationLevel, TimeSpan timeout)
            {
                IsolationLevel = isolationLevel;
                Started = DateTime.Now;
            }

            public void Commit()
            {
                // do nothing
            }

            public void Rollback()
            {
                throw new NotSupportedException();
            }
        }
        private class Database
        {
            public XmlDocument Schema;

            public List<NodeRecord> Nodes;
            public List<VersionRecord> Versions;
            public List<BinaryPropertyRecord> BinaryProperties;
            public List<FileRecord> Files;
            public List<TextPropertyRecord> TextProperties;
            public List<FlatPropertyRow> FlatProperties;
            public List<ReferencePropertyRow> ReferenceProperties;
            public List<IndexingActivityRecord> IndexingActivity = new List<IndexingActivityRecord>();
            public List<TreeLockRow> TreeLocks = new List<TreeLockRow>();
        }
        private class InMemoryNodeWriter : INodeWriter
        {
            private readonly Database _db;

            public InMemoryNodeWriter(Database db)
            {
                _db = db;
            }

            public void Open()
            {
                // do nothing
            }
            public void Close()
            {
                // do nothing
            }

            /*============================================================================ Node Insert/Update */

            public void InsertNodeAndVersionRows(NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId)
            {
                var newNodeId = _db.Nodes.Max(r => r.NodeId) + 1;
                var newVersionId = _db.Versions.Max(r => r.NodeId) + 1;
                lastMinorVersionId = newVersionId;
                lastMajorVersionId = nodeData.Version.IsMajor ? newVersionId : 0;
                var nodeTimeStamp = 0L; //TODO: InMemoryDataProvider: timestamp not supported
                var versionTimestamp = 0L; //TODO: InMemoryDataProvider: timestamp not supported
                _db.Nodes.Add(new NodeRecord
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
                _db.Versions.Add(new VersionRecord
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
                var oldPathExt = oldPath + "/";
                var newPathExt = newPath + "/";
                foreach (var nodeRow in _db.Nodes.Where(n => n.Path.StartsWith(oldPathExt)))
                    nodeRow.Path = nodeRow.Path.Replace(oldPathExt, newPathExt);
            }
            public void UpdateNodeRow(NodeData nodeData)
            {
                var nodeId = nodeData.Id;

                var nodeRec = _db.Nodes.FirstOrDefault(n => n.NodeId == nodeId);
                if (nodeRec == null)
                    throw new InvalidOperationException("Node not found. NodeId:" + nodeId);
                var parentRec = _db.Nodes.FirstOrDefault(n => n.NodeId == nodeRec.ParentNodeId);

                nodeRec.NodeTypeId = nodeData.NodeTypeId;
                nodeRec.ContentListTypeId = nodeData.ContentListTypeId;
                nodeRec.ContentListId = nodeData.ContentListId;
                nodeRec.CreatingInProgress = nodeData.CreatingInProgress;
                nodeRec.IsDeleted = nodeData.IsDeleted;
                nodeRec.ParentNodeId = nodeData.ParentId;
                nodeRec.Name = nodeData.Name;
                nodeRec.DisplayName = nodeData.DisplayName;
                nodeRec.Path = $"{parentRec?.Path ?? ""}/{nodeRec.Name}";
                nodeRec.Index = nodeData.Index;
                nodeRec.Locked = nodeData.Locked;
                nodeRec.LockedById = nodeData.LockedById;
                nodeRec.ETag = nodeData.ETag;
                nodeRec.LockType = nodeData.LockType;
                nodeRec.LockTimeout = nodeData.LockTimeout;
                nodeRec.LockDate = nodeData.LockDate;
                nodeRec.LockToken = nodeData.LockToken;
                nodeRec.LastLockUpdate = nodeData.LastLockUpdate;
                nodeRec.NodeCreationDate = nodeData.CreationDate;
                nodeRec.NodeCreatedById = nodeData.CreatedById;
                nodeRec.NodeModificationDate = nodeData.ModificationDate;
                nodeRec.NodeModifiedById = nodeData.ModifiedById;
                nodeRec.IsSystem = nodeData.IsSystem;
                nodeRec.OwnerId = nodeData.OwnerId;
                nodeRec.SavingState = nodeData.SavingState;
            }

            /*============================================================================ Version Insert/Update */

            public void UpdateVersionRow(NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId)
            {
                var versionId = nodeData.VersionId;
                var nodeId = nodeData.Id;

                var versionRec = _db.Versions.FirstOrDefault(v => v.VersionId == versionId);
                if (versionRec == null)
                    throw new InvalidOperationException("Version not found. VersionId:" + versionId);

                versionRec.NodeId = nodeId;
                versionRec.Version = nodeData.Version;
                versionRec.CreationDate = nodeData.VersionCreationDate;
                versionRec.CreatedById = nodeData.VersionCreatedById;
                versionRec.ModificationDate = nodeData.VersionModificationDate;
                versionRec.ModifiedById = nodeData.VersionModifiedById;
                versionRec.ChangedData = nodeData.ChangedData;

                var nodeRec = _db.Nodes.FirstOrDefault(n => n.NodeId == nodeId);
                if(nodeRec == null)
                    throw new InvalidOperationException("Node not found. NodeId:" + nodeId);

                if (nodeData.IsPropertyChanged("Version"))
                {
                    nodeRec.LastMinorVersionId = _db.Versions
                        .Where(v => v.NodeId == nodeId)
                        .OrderByDescending(v => v.Version.Major)
                        .ThenByDescending(v => v.Version.Minor)
                        .First().VersionId;
                    nodeRec.LastMajorVersionId = _db.Versions
                        .Where(v => v.NodeId == nodeId && v.Version.Minor == 0 && v.Version.Status == VersionStatus.Approved)
                        .OrderByDescending(v => v.Version.Major)
                        .ThenByDescending(v => v.Version.Minor)
                        .First().VersionId;
                }

                lastMajorVersionId = nodeRec.LastMajorVersionId;
                lastMinorVersionId = nodeRec.LastMinorVersionId;
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

            // ============================================================================ Property Insert/Update

            public void SaveStringProperty(int versionId, PropertyType propertyType, string value)
            {
                int mapping;
                GetFlatPropertyRow(versionId, propertyType.Mapping, SqlProvider.StringPageSize, out mapping).Strings[mapping] = value;
            }

            public void SaveDateTimeProperty(int versionId, PropertyType propertyType, DateTime value)
            {
                int mapping;
                GetFlatPropertyRow(versionId, propertyType.Mapping, SqlProvider.DateTimePageSize, out mapping).Datetimes[mapping] = value;
            }
            public void SaveIntProperty(int versionId, PropertyType propertyType, int value)
            {
                int mapping;
                GetFlatPropertyRow(versionId, propertyType.Mapping, SqlProvider.IntPageSize, out mapping).Integers[mapping] = value;
            }
            public void SaveCurrencyProperty(int versionId, PropertyType propertyType, decimal value)
            {
                int mapping;
                GetFlatPropertyRow(versionId, propertyType.Mapping, SqlProvider.CurrencyPageSize, out mapping).Decimals[mapping] = value;
            }
            private FlatPropertyRow GetFlatPropertyRow(int versionId, int mapping, int pageSize, out int propertyIndex)
            {
                var page = mapping / pageSize;
                propertyIndex = mapping % pageSize;
                var row = _db.FlatProperties.FirstOrDefault(r => r.VersionId == versionId && r.Page == page);
                if (row == null)
                {
                    var id = _db.FlatProperties.Max(r => r.Id) + 1;
                    row = new FlatPropertyRow { Id = id, VersionId = versionId, Page = page };
                    _db.FlatProperties.Add(row);
                }
                return row;
            }

            public void SaveTextProperty(int versionId, PropertyType propertyType, bool isLoaded, string value)
            {
                var row =
                    _db.TextProperties.FirstOrDefault(
                        r => r.VersionId == versionId && r.PropertyTypeId == propertyType.Id);
                if (row == null)
                    _db.TextProperties.Add(row = new TextPropertyRecord
                    {
                        TextPropertyNVarcharId = _db.TextProperties.Max(r => r.TextPropertyNVarcharId) + 1,
                        VersionId = versionId,
                        PropertyTypeId = propertyType.Id
                    });
                row.Value = value;
            }

            public void SaveReferenceProperty(int versionId, PropertyType propertyType, IEnumerable<int> value)
            {
                var rows =
                    _db.ReferenceProperties.Where(r => r.VersionId == versionId && r.PropertyTypeId == propertyType.Id)
                        .ToArray();
                foreach (var row in rows)
                    _db.ReferenceProperties.Remove(row);

                foreach(var referredNodeId in value.Distinct())
                    _db.ReferenceProperties.Add(new ReferencePropertyRow
                    {
                        ReferencePropertyId = _db.ReferenceProperties.Max(x=>x.ReferencePropertyId) + 1,
                        VersionId = versionId,
                        PropertyTypeId = propertyType.Id,
                        ReferredNodeId = referredNodeId
                    });
            }

            public void InsertBinaryProperty(BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode)
            {
                if (isNewNode)
                    _db.BinaryProperties.RemoveAll(r => r.VersionId == versionId && r.PropertyTypeId == propertyTypeId);

                var fileId = value.FileId;
                if (fileId == 0 || value.Stream != null)
                {
                    fileId = _db.Files.Max(r => r.FileId) + 1;
                    _db.Files.Add(new FileRecord
                    {
                        FileId = fileId,
                        ContentType = value.ContentType,
                        FileNameWithoutExtension = value.FileName.FileNameWithoutExtension,
                        Extension = value.FileName.Extension,
                        Size = value.Stream?.Length ?? 0L,
                        Stream = GetBytes(value.Stream)
                    });
                    value.FileId = fileId;
                }

                var binaryPropertyId = _db.BinaryProperties.Max(r => r.BinaryPropertyId) + 1;
                _db.BinaryProperties.Add(new BinaryPropertyRecord
                {
                    BinaryPropertyId = binaryPropertyId,
                    VersionId = versionId,
                    PropertyTypeId = propertyTypeId,
                    FileId = fileId
                });
                value.Id = binaryPropertyId;
            }
            private byte[] GetBytes(Stream stream)
            {
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                return buffer;
            }

            public void UpdateBinaryProperty(BinaryDataValue value)
            {
                throw new NotImplementedException();
            }
            public void DeleteBinaryProperty(int versionId, PropertyType propertyType)
            {
                throw new NotImplementedException();
            }
        }
        private class InMemorySchemaWriter : SchemaWriter
        {
            private XmlDocument _schemaXml;
            private string _xmlNamespace = "http://schemas.sensenet.com/SenseNet/ContentRepository/Storage/Schema";
            private XmlNamespaceManager _nsmgr;

            public InMemorySchemaWriter(XmlDocument schemaXml)
            {
                _schemaXml = schemaXml;
                _nsmgr = new XmlNamespaceManager(_schemaXml.NameTable);
                _nsmgr.AddNamespace("x", _xmlNamespace);
            }

            public override void Open()
            {
                // do nothing
            }
            public override void Close()
            {
                // do nothing
            }

            public override void CreatePropertyType(string name, DataType dataType, int mapping, bool isContentListProperty)
            {
                if(isContentListProperty)
                    throw new NotImplementedException(); //TODO: ContentListProperty creating is not implemented.

                // ReSharper disable once AssignNullToNotNullAttribute
                var ids =
                    _schemaXml.SelectNodes("/x:StorageSchema/x:UsedPropertyTypes/x:PropertyType/@itemID", _nsmgr)
                        .OfType<XmlAttribute>().Select(a=>int.Parse(a.Value)).ToArray();
                var id = ids.Max() + 1;

                var element = _schemaXml.CreateElement("PropertyType", _xmlNamespace);
                element.SetAttribute("itemID", id.ToString());
                element.SetAttribute("name", name);
                element.SetAttribute("dataType", dataType.ToString());
                element.SetAttribute("mapping", mapping.ToString());

                var parentElement = (XmlElement)_schemaXml.SelectSingleNode("/x:StorageSchema/x:UsedPropertyTypes", _nsmgr);
                parentElement.AppendChild(element);
            }
            public override void DeletePropertyType(PropertyType propertyType)
            {
                throw new NotImplementedException();
            }

            public override void CreateContentListType(string name)
            {
                throw new NotImplementedException();
            }
            public override void DeleteContentListType(ContentListType contentListType)
            {
                throw new NotImplementedException();
            }

            public override void CreateNodeType(NodeType parent, string name, string className)
            {
                var ids =
                    _schemaXml.SelectNodes("//x:NodeType/@itemID", _nsmgr)
                        .OfType<XmlAttribute>().Select(a => int.Parse(a.Value)).ToArray();
                var id = ids.Max() + 1;

                var element = _schemaXml.CreateElement("NodeType", _xmlNamespace);
                element.SetAttribute("itemID", id.ToString());
                element.SetAttribute("name", name);
                element.SetAttribute("className", className);

                var parentElement = (XmlElement)_schemaXml.SelectSingleNode($"//x:NodeType[@itemID = '{parent.Id}']", _nsmgr);
                parentElement.AppendChild(element);
            }
            public override void ModifyNodeType(NodeType nodeType, NodeType parent, string className)
            {
                throw new NotImplementedException();
            }
            public override void DeleteNodeType(NodeType nodeType)
            {
                throw new NotImplementedException();
            }

            public override void AddPropertyTypeToPropertySet(PropertyType propertyType, PropertySet owner, bool isDeclared)
            {
                if (!isDeclared)
                    return;

                var element = _schemaXml.CreateElement("PropertyType", _xmlNamespace);
                element.SetAttribute("name", propertyType.Name);

                var parentElement = (XmlElement)_schemaXml.SelectSingleNode($"//x:NodeType[@name = '{owner.Name}']", _nsmgr);
                if (parentElement == null)
                    throw new NotImplementedException(); //TODO: ContentList property adding is not implemented.
                parentElement.AppendChild(element);
            }
            public override void RemovePropertyTypeFromPropertySet(PropertyType propertyType, PropertySet owner)
            {
                throw new NotImplementedException();
            }
            public override void UpdatePropertyTypeDeclarationState(PropertyType propertyType, NodeType owner, bool isDeclared)
            {
                throw new NotImplementedException();
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
        private class BinaryPropertyRecord
        {
            public int BinaryPropertyId;
            public int VersionId;
            public int PropertyTypeId;
            public int FileId;
        }
        private class FileRecord
        {
            public int FileId;
            public string ContentType;
            public string FileNameWithoutExtension;
            public string Extension;
            public long Size;
            public byte[] Stream;
        }
        private class TextPropertyRecord
        {
            public int TextPropertyNVarcharId;
            public int VersionId;
            public int PropertyTypeId;
            public string Value;
        }
        private class FlatPropertyRow
        {
            public int Id;
            public int VersionId;
            public int Page;
            public string[] Strings = new string[80];
            public int?[] Integers = new int?[40];
            public DateTime?[] Datetimes = new DateTime?[25];
            public decimal?[] Decimals = new decimal?[15];
        }
        private class ReferencePropertyRow
        {
            public int ReferencePropertyId;
            public int VersionId;
            public int PropertyTypeId;
            public int ReferredNodeId;
        }
        private class IndexingActivityRecord
        {
            public int IndexingActivityId;
            public IndexingActivityType ActivityType;
            public DateTime CreationDate;
            public int NodeId;
            public int VersionId;
            public bool? SingleVersion;
            public bool? MoveOrRename;
            public bool? IsLastDraftValue;
            public string Path;
            public string Extension;
        }
        private class TreeLockRow
        {
            public int TreeLockId;
            public string Path;
            public DateTime LockedAt;
        }
        #endregion
    }
}

