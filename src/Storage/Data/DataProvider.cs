using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Data.Common;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Search.Internal;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Storage.Data
{
    public abstract class DataProvider : ITransactionFactory, IPackageStorageProvider
    {

        //////////////////////////////////////// Static Access ////////////////////////////////////////

        public static DataProvider Current => Providers.Instance.DataProvider;

        // ====================================================== Query support

        public abstract IMetaQueryEngine MetaQueryEngine { get; }

        //////////////////////////////////////// For tests ////////////////////////////////////////

        internal static void InitializeForTests()
        {
            Current.InitializeForTestsPrivate();
        }
        protected abstract void InitializeForTestsPrivate();

        public static string GetSecurityControlStringForTests()
        {
            return Current.GetSecurityControlStringForTestsInternal();
        }
        protected abstract string GetSecurityControlStringForTestsInternal();

        public static int GetPermissionLogEntriesCountAfterMoment(DateTime moment)
        {
            return Current.GetPermissionLogEntriesCountAfterMomentInternal(moment);
        }
        protected abstract int GetPermissionLogEntriesCountAfterMomentInternal(DateTime moment);

        protected internal abstract AuditLogEntry[] LoadLastAuditLogEntries(int count);


        //////////////////////////////////////// Generic Datalayer Logic ////////////////////////////////////////

        internal static bool NodeExists(string path)
        {
            return Current.NodeExistsInDatabase(path);
        }
        protected abstract bool NodeExistsInDatabase(string path);
        public abstract string GetNameOfLastNodeWithNameBase(int parentId, string namebase, string extension);

        internal void LoadNodeData(IEnumerable<NodeToken> tokens)
        {
            var buildersByVersionId = new Dictionary<int, NodeBuilder>();
            foreach (NodeToken token in tokens)
            {
                if (token.VersionId == 0)
                    throw new NotSupportedException("Cannot load a node if the versionId is 0.");
                if (!buildersByVersionId.ContainsKey(token.VersionId))
                    buildersByVersionId.Add(token.VersionId, new NodeBuilder(token));
            }
            if (buildersByVersionId.Count != 0)
                LoadNodes(buildersByVersionId);
        }

        internal void SaveNodeData(NodeData nodeData, NodeSaveSettings settings, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            if (nodeData == null)
                throw new ArgumentNullException("nodeData");

            lastMajorVersionId = 0;
            lastMinorVersionId = 0;
            
            bool isNewNode = nodeData.Id == 0; // shortcut
            string path;

            if (nodeData.Id != Identifiers.PortalRootId)
            {
                var parent = NodeHead.Get(nodeData.ParentId);
                if (parent == null)
                    throw new ContentNotFoundException(nodeData.ParentId.ToString());

                path = RepositoryPath.Combine(parent.Path, nodeData.Name);
            }
            else
            {
                path = Identifiers.RootPath;
            }

            Node.AssertPath(path);

            nodeData.Path = path;

            var writer = this.CreateNodeWriter();
            try
            {
                var savingAlgorithm = settings.GetSavingAlgorithm();

                writer.Open();
                if (settings.NeedToSaveData)
                {
                    SaveNodeBaseData(nodeData, savingAlgorithm, writer, settings, out lastMajorVersionId, out lastMinorVersionId);

                    BenchmarkCounter.IncrementBy(BenchmarkCounter.CounterName.SaveNodeBaseData, nodeData.SavingTimer.ElapsedTicks);
                    nodeData.SavingTimer.Restart();

                    if (!isNewNode && nodeData.PathChanged && nodeData.SharedData != null)
                        writer.UpdateSubTreePath(nodeData.SharedData.Path, nodeData.Path);
                    SaveNodeProperties(nodeData, savingAlgorithm, writer, isNewNode);
                }
                else
                {
                    writer.UpdateNodeRow(nodeData);
                }
                writer.Close();

                BenchmarkCounter.IncrementBy(BenchmarkCounter.CounterName.SaveNodeFlatProperties, nodeData.SavingTimer.ElapsedTicks);
                nodeData.SavingTimer.Restart();

                foreach (var versionId in settings.DeletableVersionIds)
                    DeleteVersion(versionId, nodeData, out lastMajorVersionId, out lastMinorVersionId);
            }
            catch // rethrow
            {
                if (isNewNode)
                {
                    // Failed save: set NodeId back to 0
                    nodeData.Id = 0;
                }

                throw;
            }
        }
        private static void SaveNodeBaseData(NodeData nodeData, SavingAlgorithm savingAlgorithm, INodeWriter writer, NodeSaveSettings settings, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            switch (savingAlgorithm)
            {
                case SavingAlgorithm.CreateNewNode:
                    writer.InsertNodeAndVersionRows(nodeData, out lastMajorVersionId, out lastMinorVersionId);
                    break;
                case SavingAlgorithm.UpdateSameVersion:
                    writer.UpdateNodeRow(nodeData);
                    writer.UpdateVersionRow(nodeData, out lastMajorVersionId, out lastMinorVersionId);
                    break;
                case SavingAlgorithm.CopyToNewVersionAndUpdate:
                    writer.UpdateNodeRow(nodeData);
                    writer.CopyAndUpdateVersion(nodeData, settings.CurrentVersionId, out lastMajorVersionId, out lastMinorVersionId);
                    break;
                case SavingAlgorithm.CopyToSpecifiedVersionAndUpdate:
                    writer.UpdateNodeRow(nodeData);
                    writer.CopyAndUpdateVersion(nodeData, settings.CurrentVersionId, settings.ExpectedVersionId, out lastMajorVersionId, out lastMinorVersionId);
                    break;
                default:
                    throw new SnNotSupportedException("Unknown SavingAlgorithm: " + savingAlgorithm);
            }
        }
        private static void SaveNodeProperties(NodeData nodeData, SavingAlgorithm savingAlgorithm, INodeWriter writer, bool isNewNode)
        {
            int versionId = nodeData.VersionId;
            foreach (var propertyType in nodeData.PropertyTypes)
            {
                var slotValue = nodeData.GetDynamicRawData(propertyType) ?? propertyType.DefaultValue;
                bool isModified = nodeData.IsModified(propertyType);

                if (!isModified && !isNewNode)
                    continue;

                switch (propertyType.DataType)
                {
                    case DataType.String:
                        writer.SaveStringProperty(versionId, propertyType, (string)slotValue);
                        break;
                    case DataType.DateTime:
                        writer.SaveDateTimeProperty(versionId, propertyType, (DateTime)slotValue);
                        break;
                    case DataType.Int:
                        writer.SaveIntProperty(versionId, propertyType, (int)slotValue);
                        break;
                    case DataType.Currency:
                        writer.SaveCurrencyProperty(versionId, propertyType, (decimal)slotValue);
                        break;
                    case DataType.Text:
                        writer.SaveTextProperty(versionId, propertyType, true, (string)slotValue);//TODO: ?? isLoaded property handling
                        BenchmarkCounter.IncrementBy(BenchmarkCounter.CounterName.SaveNodeTextProperties, nodeData.SavingTimer.ElapsedTicks);
                        nodeData.SavingTimer.Restart();
                        break;
                    case DataType.Reference:
                        var ids = (IEnumerable<int>)slotValue;
                        if (!isNewNode || (ids != null && ids.Count() > 0))
                        {
                            var ids1 = ids.Distinct().ToList();
                            if (ids1.Count != ids.Count())
                                nodeData.SetDynamicRawData(propertyType, ids1);
                            writer.SaveReferenceProperty(versionId, propertyType, ids1);
                        }
                        BenchmarkCounter.IncrementBy(BenchmarkCounter.CounterName.SaveNodeReferenceProperties, nodeData.SavingTimer.ElapsedTicks);
                        nodeData.SavingTimer.Restart();
                        break;
                    case DataType.Binary:
                        var binValue = (BinaryDataValue)slotValue;

                        if (binValue == null || binValue.IsEmpty)
                        {
                            writer.DeleteBinaryProperty(versionId, propertyType);
                        }
                        else if (binValue.Id == 0 || savingAlgorithm != SavingAlgorithm.UpdateSameVersion)
                        {
                            writer.InsertBinaryProperty(binValue, versionId, propertyType.Id, isNewNode);
                        }
                        else
                        {
                            writer.UpdateBinaryProperty(binValue);
                        }

                        BenchmarkCounter.IncrementBy(BenchmarkCounter.CounterName.SaveNodeBinaries, nodeData.SavingTimer.ElapsedTicks);
                        nodeData.SavingTimer.Restart();
                        break;
                    default:
                        throw new NotSupportedException(propertyType.DataType.ToString());
                }
            }
        }

        public abstract DateTime RoundDateTime(DateTime d);

        //////////////////////////////////////// Abstract Schema Members ////////////////////////////////////////

        protected internal abstract DataSet LoadSchema();
        protected internal abstract void Reset();
        protected internal abstract SchemaWriter CreateSchemaWriter();
        public abstract Dictionary<DataType, int> ContentListMappingOffsets { get; }
        protected internal abstract int ContentListStartPage { get; }
        public static PropertyMapping GetPropertyMapping(PropertyType propType)
        {
            return Current.GetPropertyMappingInternal(propType);
        }
        protected abstract PropertyMapping GetPropertyMappingInternal(PropertyType propType);
        public abstract void AssertSchemaTimestampAndWriteModificationDate(long timestamp);

        //////////////////////////////////////// Abstract Node Members ////////////////////////////////////////

        public abstract int PathMaxLength { get; }
        public abstract DateTime DateTimeMinValue { get; }
        public abstract DateTime DateTimeMaxValue { get; }
        public abstract decimal DecimalMinValue { get; }
        public abstract decimal DecimalMaxValue { get; }

        public abstract ITransactionProvider CreateTransaction();
        protected internal abstract INodeWriter CreateNodeWriter();

        protected internal abstract VersionNumber[] GetVersionNumbers(int nodeId);
        protected internal abstract VersionNumber[] GetVersionNumbers(string path);

        // Load Nodes, Binary

        protected internal abstract void LoadNodes(Dictionary<int, NodeBuilder> buildersByVersionId);

        protected internal abstract bool IsCacheableText(string text);
        protected internal abstract string LoadTextPropertyValue(int versionId, int propertyTypeId);
        protected internal abstract Dictionary<int, string> LoadTextPropertyValues(int versionId, int[] propertyTypeIds);
        protected internal abstract BinaryDataValue LoadBinaryPropertyValue(int versionId, int propertyTypeId);

        [Obsolete("Use GetStream method on a BinaryData instance instead.", true)]
        protected internal abstract Stream LoadStream(int versionId, int propertyTypeId);

        // BIN2
        protected internal abstract BinaryCacheEntity LoadBinaryCacheEntity(int nodeVersionId, int propertyTypeId);
        protected internal abstract byte[] LoadBinaryFragment(int fileId, long position, int count);

        protected internal abstract BlobStorageContext GetBlobStorageContext(int fileId, bool clearStream = false, int versionId = 0, int propertyTypeId = 0);

        protected internal virtual bool IsFilestreamEnabled()
        {
            return false;
        }

        // =============================================== Chunk upload

        [Obsolete("Use the another overload with three parameter.", true)]
        protected internal virtual string StartChunk(int versionId, int propertyTypeId) { throw new NotSupportedException(); }
        protected internal abstract string StartChunk(int versionId, int propertyTypeId, long fullSize);

        protected internal abstract void WriteChunk(int versionId, string token, byte[] buffer, long offset, long fullSize);

        protected internal abstract void CopyFromStream(int versionId, string token, Stream input);

        protected internal abstract void CommitChunk(int versionId, int propertyTypeId, string token, long fullSize, BinaryDataValue source = null);

        /////////////// Operations

        internal void MoveNode(int sourceNodeId, int targetNodeId, long sourceTimestamp, long targetTimestamp)
        {
            DataOperationResult result = MoveNodeTree(sourceNodeId, targetNodeId, sourceTimestamp, targetTimestamp);
            if (result == DataOperationResult.Successful)
                return;
            DataOperationException exc = new DataOperationException(result);
            exc.Data.Add("SourceNodeId", sourceNodeId);
            exc.Data.Add("TargetNodeId", targetNodeId);
            throw exc;
        }
        internal void DeleteNode(int nodeId)
        {
            DataOperationResult result = DeleteNodeTree(nodeId);
            if (result == DataOperationResult.Successful)
                return;
            DataOperationException exc = new DataOperationException(result);
            exc.Data.Add("NodeId", nodeId);
            throw exc;
        }
        internal void DeleteNodePsychical(int nodeId, long timestamp)
        {
            DataOperationResult result = DeleteNodeTreePsychical(nodeId, timestamp);
            if (result == DataOperationResult.Successful)
                return;
            DataOperationException exc = new DataOperationException(result);
            exc.Data.Add("NodeId", nodeId);
            throw exc;
        }
        protected internal abstract IEnumerable<NodeType> LoadChildTypesToAllow(int sourceNodeId);
        protected internal abstract DataOperationResult MoveNodeTree(int sourceNodeId, int targetNodeId, long sourceTimestamp = 0, long targetTimestamp = 0);
        protected internal abstract DataOperationResult DeleteNodeTree(int nodeId);
        protected internal abstract DataOperationResult DeleteNodeTreePsychical(int nodeId, long timestamp);
        protected internal abstract bool HasChild(int nodeId);
        protected internal abstract void DeleteVersion(int versionId, NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId);

        /////////////// Security

        protected internal abstract void InstallDefaultSecurityStructure();

        // ====================================================== AppModel script generator

        public static string GetAppModelScript(IEnumerable<string> paths, bool resolveAll, bool resolveChildren)
        {
            return Current.GetAppModelScriptPrivate(paths, resolveAll, resolveChildren);
        }
        protected abstract string GetAppModelScriptPrivate(IEnumerable<string> paths, bool all, bool resolveChildren);

        // ====================================================== Custom database script support

        public static IDataProcedure CreateDataProcedure(string commandText, string connectionName = null, InitialCatalog initialCatalog = InitialCatalog.Initial)
        {
            return Current.CreateDataProcedureInternal(commandText, connectionName, initialCatalog);
        }
        public static IDataProcedure CreateDataProcedure(string commandText, ConnectionInfo connectionInfo)
        {
            return Current.CreateDataProcedureInternal(commandText, connectionInfo);
        }
        public static IDbDataParameter CreateParameter()
        {
            return Current.CreateParameterInternal();
        }
        protected internal abstract IDataProcedure CreateDataProcedureInternal(string commandText, string connectionName = null, InitialCatalog initialCatalog = InitialCatalog.Initial);
        protected internal abstract IDataProcedure CreateDataProcedureInternal(string commandText, ConnectionInfo connectionInfo);
        protected abstract IDbDataParameter CreateParameterInternal();

        public static void CheckScript(string commandText)
        {
            Current.CheckScriptInternal(commandText);
        }
        protected internal abstract void CheckScriptInternal(string commandText);

        // ====================================================== Tools

        protected void ReadNodeTokens(DbDataReader reader, List<NodeToken> targetList)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");
            if (targetList == null)
                throw new ArgumentNullException("targetList");
            while (reader.Read())
                targetList.Add(GetNodeTokenFromReader(reader));
        }
        private static NodeToken GetNodeTokenFromReader(DbDataReader reader)
        {
            int id = reader.GetInt32(reader.GetOrdinal("NodeId"));
            int versionId = reader.GetInt32(reader.GetOrdinal("VersionId"));
            int major = reader.GetInt16(reader.GetOrdinal("MajorNumber"));
            int minor = reader.GetInt16(reader.GetOrdinal("MinorNumber"));
            int status = reader.GetInt16(reader.GetOrdinal("Status"));
            int nodeTypeId = reader.GetInt32(reader.GetOrdinal("NodeTypeId"));
            int listId = TypeConverter.ToInt32(reader.GetValue(reader.GetOrdinal("ContentListId")));
            int listTypeId = TypeConverter.ToInt32(reader.GetValue(reader.GetOrdinal("ContentListTypeId")));
            VersionNumber versionNumber = new VersionNumber(major, minor, (VersionStatus)status);

            return new NodeToken(id, nodeTypeId, listId, listTypeId, versionId, versionNumber);
        }

        public void SaveTextProperty(int versionId, PropertyType propertyType, string value)
        {
            INodeWriter writer = this.CreateNodeWriter();
            writer.SaveTextProperty(versionId, propertyType, true, value);
            writer.Close();
        }

        protected internal abstract NodeHead LoadNodeHead(string path);
        protected internal abstract NodeHead LoadNodeHead(int nodeId);
        protected internal abstract NodeHead LoadNodeHeadByVersionId(int versionId);
        protected internal abstract IEnumerable<NodeHead> LoadNodeHeads(IEnumerable<int> heads);

        protected internal abstract NodeHead.NodeVersion[] GetNodeVersions(int nodeId);

        protected internal abstract long GetTreeSize(string path, bool includeChildren);

        public static int GetNodeCount()
        {
            return Current.NodeCount(null);
        }
        public static int GetNodeCount(string path)
        {
            return Current.NodeCount(path);
        }
        public static int GetVersionCount()
        {
            return Current.VersionCount(null);
        }
        public static int GetVersionCount(string path)
        {
            return Current.VersionCount(path);
        }
        protected abstract int NodeCount(string path);
        protected abstract int VersionCount(string path);

        // ====================================================== Index document save / load operations

        internal static void SaveIndexDocument(NodeData nodeData, byte[] indexDocumentBytes)
        {
            Current.UpdateIndexDocument(nodeData, indexDocumentBytes);
        }
        public static void SaveIndexDocument(int versionId, byte[] indexDocumentBytes)
        {
            Current.UpdateIndexDocument(versionId, indexDocumentBytes);
        }
        internal static IndexDocumentData LoadIndexDocument(int versionId)
        {
            return Current.LoadIndexDocumentByVersionId(versionId);
        }
        internal static IEnumerable<IndexDocumentData> LoadIndexDocument(IEnumerable<int> versionId)
        {
            return Current.LoadIndexDocumentByVersionId(versionId);
        }
        internal static IEnumerable<IndexDocumentData> LoadIndexDocument(string path, int[] excludedNodeTypes)
        {
            return Current.LoadIndexDocumentsByPath(path, excludedNodeTypes);
        }
        protected internal abstract IEnumerable<IndexDocumentData> LoadIndexDocumentsByPath(string path, int[] excludedNodeTypes);
        protected internal abstract void UpdateIndexDocument(NodeData nodeData, byte[] indexDocumentBytes);
        protected internal abstract void UpdateIndexDocument(int versionId, byte[] indexDocumentBytes);
        protected internal abstract IndexDocumentData LoadIndexDocumentByVersionId(int versionId);
        protected internal abstract IEnumerable<IndexDocumentData> LoadIndexDocumentByVersionId(IEnumerable<int> versionId);

        protected internal abstract IndexDocumentData GetIndexDocumentDataFromReader(DbDataReader reader);

        public static IEnumerable<int> LoadIdsOfNodesThatDoNotHaveIndexDocument(int fromId, int toId)
        {
            return Current.GetIdsOfNodesThatDoNotHaveIndexDocument(fromId, toId);
        }
        protected internal abstract IEnumerable<int> GetIdsOfNodesThatDoNotHaveIndexDocument(int fromId, int toId);

        // ====================================================== Indexing activity operations

        public abstract IIndexingActivity[] LoadIndexingActivities(int fromId, int toId, int count, bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory);
        public abstract IIndexingActivity[] LoadIndexingActivities(int[] gaps, bool executingUnprocessedActivities, IIndexingActivityFactory activityFactory);
        public abstract void RegisterIndexingActivity(IIndexingActivity activity);
        public abstract int GetLastActivityId();
        public abstract void DeleteAllIndexingActivities();

        // ====================================================== Checking  index integrity

        public abstract IDataProcedure GetTimestampDataForOneNodeIntegrityCheck(string path, int[] excludedNodeTypeIds);
        public abstract IDataProcedure GetTimestampDataForRecursiveIntegrityCheck(string path, int[] excludedNodeTypeIds);

        // ====================================================== Database backup / restore operations

        public abstract string DatabaseName { get; }
        public abstract IEnumerable<string> GetScriptsForDatabaseBackup();

        // ======================================================

        internal static long GetLongFromBytes(byte[] bytes)
        {
            var @long = 0L;
            for (int i = 0; i < bytes.Length; i++)
                @long = (@long << 8) + bytes[i];
            return @long;
        }
        internal static byte[] GetBytesFromLong(long @long)
        {
            var bytes = new byte[8];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[7 - i] = (byte)(@long & 0xFF);
                @long = @long >> 8;
            }
            return bytes;
        }

        protected internal abstract List<ContentListType> GetContentListTypesInTree(string path);

        // ====================================================== NodeQuery substitutions

        protected internal abstract IEnumerable<int> GetChildrenIdentfiers(int nodeId);
        protected internal abstract int InstanceCount(int[] nodeTypeIds);
        protected internal abstract IEnumerable<int> QueryNodesByPath(string pathStart, bool orderByPath);
        protected internal abstract IEnumerable<int> QueryNodesByType(int[] typeIds);
        protected internal abstract IEnumerable<int> QueryNodesByTypeAndPath(int[] nodeTypeIds, string pathStart, bool orderByPath);
        protected internal abstract IEnumerable<int> QueryNodesByTypeAndPath(int[] nodeTypeIds, string[] pathStart, bool orderByPath);
        protected internal abstract IEnumerable<int> QueryNodesByTypeAndPathAndName(int[] nodeTypeIds, string pathStart, bool orderByPath, string name);
        protected internal abstract IEnumerable<int> QueryNodesByTypeAndPathAndName(int[] nodeTypeIds, string[] pathStart, bool orderByPath, string name);
        protected internal abstract IEnumerable<int> QueryNodesByTypeAndPathAndProperty(int[] nodeTypeIds, string pathStart, bool orderByPath, List<QueryPropertyData> properties);
        protected internal abstract IEnumerable<int> QueryNodesByReferenceAndType(string referenceName, int referredNodeId, int[] allowedTypeIds);

        // ====================================================== Packaging: IPackageStorageProvider

        public abstract IDataProcedureFactory DataProcedureFactory { get; set; }

        public abstract IEnumerable<ComponentInfo> LoadInstalledComponents();
        public abstract IEnumerable<Package> LoadInstalledPackages();
        public abstract void SavePackage(Package package);
        public abstract void UpdatePackage(Package package);
        public abstract bool IsPackageExist(string componentId, PackageType packageType, Version version);
        public abstract void DeletePackage(Package package);
        public abstract void DeleteAllPackages();
        public abstract void LoadManifest(Package package);

        // ====================================================== Tree lock

        protected internal abstract int AcquireTreeLock(string path);
        protected internal abstract bool IsTreeLocked(string path);
        protected internal abstract void ReleaseTreeLock(int[] lockIds);
        protected internal abstract Dictionary<int, string> LoadAllTreeLocks();
    }
}
