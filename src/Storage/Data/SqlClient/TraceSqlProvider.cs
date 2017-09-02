using SenseNet.ContentRepository.Storage.Schema;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    /// <summary>
    /// For debugging purposes. Goal: tracing all SQL accesses.
    /// In the development time it is easier to follow what is not overridden yet if the no-sql-access-methods are overridden too.
    /// </summary>
    internal class TraceSqlProvider : SqlProvider, IDisposable
    {
        public void Dispose()
        {
        }

        private void WriteLog(MethodBase methodBase, params object[] prms)
        {
            var log = new StringBuilder("@#$Test> SQLACCESS: ");
            log.Append(methodBase.Name).Append("(");
            ParameterInfo[] prmInfos = methodBase.GetParameters();
            for (int i = 0; i < prmInfos.Length; i++)
            {
                if (i > 0)
                    log.Append(", ");

                log.Append(prmInfos[i].Name).Append("=<");
                if (i < prms.Length)
                    log.Append(FormatValue(prms[i]));
                log.Append(">");
            }
            log.Append(");").Append("\r\n");

            Debug.WriteLine(log);
        }
        private string FormatValue(object value)
        {
            var intArray = value as IEnumerable<int>;
            if (intArray != null)
                return FormatIntArray(intArray);
            if (value == null)
                return "<null>";
            return value.ToString();
        }
        internal static string FormatIntArray(IEnumerable<int> items)
        {
            var enumerable = items as int[] ?? items.ToArray();
            return string.Join(", ", enumerable.Take(20)) + (enumerable.Count() > 20 ? "..." : string.Empty);
        }

        protected override bool NodeExistsInDatabase(string path)
        {
            WriteLog(MethodBase.GetCurrentMethod(), path);
            return base.NodeExistsInDatabase(path);
        }
        public override string GetNameOfLastNodeWithNameBase(int parentId, string namebase, string extension)
        {
            WriteLog(MethodBase.GetCurrentMethod(), parentId, namebase, extension);
            return base.GetNameOfLastNodeWithNameBase(parentId, namebase, extension);
        }
        protected internal override System.Data.DataSet LoadSchema()
        {
            WriteLog(MethodBase.GetCurrentMethod());
            return base.LoadSchema();
        }
        public override void AssertSchemaTimestampAndWriteModificationDate(long timestamp)
        {
            WriteLog(MethodBase.GetCurrentMethod(), timestamp);
            base.AssertSchemaTimestampAndWriteModificationDate(timestamp);
        }

        protected internal override INodeWriter CreateNodeWriter()
        {
            // no sql access
            return new TraceNodeWriter();
        }
        protected internal override VersionNumber[] GetVersionNumbers(int nodeId)
        {
            WriteLog(MethodBase.GetCurrentMethod(), nodeId);
            return base.GetVersionNumbers(nodeId);
        }
        protected internal override VersionNumber[] GetVersionNumbers(string path)
        {
            WriteLog(MethodBase.GetCurrentMethod(), path);
            return base.GetVersionNumbers(path);
        }
        protected internal override void LoadNodes(Dictionary<int, NodeBuilder> buildersByVersionId)
        {
            WriteLog(MethodBase.GetCurrentMethod(), buildersByVersionId);
            base.LoadNodes(buildersByVersionId);
        }

        protected internal override string LoadTextPropertyValue(int versionId, int propertyTypeId)
        {
            WriteLog(MethodBase.GetCurrentMethod(), versionId, propertyTypeId);
            return base.LoadTextPropertyValue(versionId, propertyTypeId);
        }
        protected internal override Dictionary<int, string> LoadTextPropertyValues(int versionId, int[] propertyTypeIds)
        {
            WriteLog(MethodBase.GetCurrentMethod(), versionId, propertyTypeIds);
            return base.LoadTextPropertyValues(versionId, propertyTypeIds);
        }
        protected internal override BinaryDataValue LoadBinaryPropertyValue(int versionId, int propertyTypeId)
        {
            WriteLog(MethodBase.GetCurrentMethod(), versionId, propertyTypeId);
            return base.LoadBinaryPropertyValue(versionId, propertyTypeId);
        }
        protected internal override BinaryCacheEntity LoadBinaryCacheEntity(int nodeVersionId, int propertyTypeId)
        {
            WriteLog(MethodBase.GetCurrentMethod(), nodeVersionId, propertyTypeId);
            return base.LoadBinaryCacheEntity(nodeVersionId, propertyTypeId);
        }
        protected internal override byte[] LoadBinaryFragment(int fileId, long position, int count)
        {
            WriteLog(MethodBase.GetCurrentMethod(), fileId, position, count);
            return base.LoadBinaryFragment(fileId, position, count);
        }
        protected internal override IEnumerable<NodeType> LoadChildTypesToAllow(int sourceNodeId)
        {
            WriteLog(MethodBase.GetCurrentMethod(), sourceNodeId);
            return base.LoadChildTypesToAllow(sourceNodeId);
        }
        protected internal override DataOperationResult MoveNodeTree(int sourceNodeId, int targetNodeId, long sourceTimestamp = 0, long targetTimestamp = 0)
        {
            WriteLog(MethodBase.GetCurrentMethod(), sourceNodeId, targetNodeId, sourceTimestamp, targetTimestamp);
            return base.MoveNodeTree(sourceNodeId, targetNodeId, sourceTimestamp, targetTimestamp);
        }
        protected internal override DataOperationResult DeleteNodeTree(int nodeId)
        {
            WriteLog(MethodBase.GetCurrentMethod(), nodeId);
            return base.DeleteNodeTree(nodeId);
        }
        protected internal override DataOperationResult DeleteNodeTreePsychical(int nodeId, long timestamp = 0)
        {
            WriteLog(MethodBase.GetCurrentMethod(), nodeId, timestamp);
            return base.DeleteNodeTreePsychical(nodeId, timestamp);
        }
        protected internal override bool HasChild(int nodeId)
        {
            WriteLog(MethodBase.GetCurrentMethod(), nodeId);
            return base.HasChild(nodeId);
        }
        protected internal override void DeleteVersion(int versionId, NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            base.DeleteVersion(versionId, nodeData, out lastMajorVersionId, out lastMinorVersionId);
            WriteLog(MethodBase.GetCurrentMethod(), versionId, nodeData, lastMajorVersionId, lastMinorVersionId);
        }
        protected override string GetAppModelScriptPrivate(IEnumerable<string> paths, bool all, bool resolveChildren)
        {
            // no sql access
            return base.GetAppModelScriptPrivate(paths, all, resolveChildren);
        }
        protected internal override NodeHead LoadNodeHead(string path)
        {
            WriteLog(MethodBase.GetCurrentMethod(), path);
            return base.LoadNodeHead(path);
        }
        protected internal override NodeHead LoadNodeHead(int nodeId)
        {
            WriteLog(MethodBase.GetCurrentMethod(), nodeId);
            return base.LoadNodeHead(nodeId);
        }
        protected internal override NodeHead LoadNodeHeadByVersionId(int versionId)
        {
            WriteLog(MethodBase.GetCurrentMethod(), versionId);
            return base.LoadNodeHeadByVersionId(versionId);
        }
        protected internal override IEnumerable<NodeHead> LoadNodeHeads(IEnumerable<int> heads)
        {
            WriteLog(MethodBase.GetCurrentMethod(), heads);
            return base.LoadNodeHeads(heads);
        }
        protected internal override NodeHead.NodeVersion[] GetNodeVersions(int nodeId)
        {
            WriteLog(MethodBase.GetCurrentMethod(), nodeId);
            return base.GetNodeVersions(nodeId);
        }
        protected internal override long GetTreeSize(string path, bool includeChildren)
        {
            WriteLog(MethodBase.GetCurrentMethod(), path, includeChildren);
            return base.GetTreeSize(path, includeChildren);
        }
        protected override int NodeCount(string path)
        {
            WriteLog(MethodBase.GetCurrentMethod(), path);
            return base.NodeCount(path);
        }
        protected override int VersionCount(string path)
        {
            WriteLog(MethodBase.GetCurrentMethod(), path);
            return base.VersionCount(path);
        }
        protected internal override void UpdateIndexDocument(NodeData nodeData, byte[] indexDocumentBytes)
        {
            WriteLog(MethodBase.GetCurrentMethod(), nodeData, indexDocumentBytes);
            base.UpdateIndexDocument(nodeData, indexDocumentBytes);
        }
        protected internal override void UpdateIndexDocument(int versionId, byte[] indexDocumentBytes)
        {
            WriteLog(MethodBase.GetCurrentMethod(), versionId, indexDocumentBytes);
            base.UpdateIndexDocument(versionId, indexDocumentBytes);
        }
        protected internal override IndexDocumentData LoadIndexDocumentByVersionId(int versionId)
        {
            WriteLog(MethodBase.GetCurrentMethod(), versionId);
            return base.LoadIndexDocumentByVersionId(versionId);
        }
        protected internal override IEnumerable<IndexDocumentData> LoadIndexDocumentByVersionId(IEnumerable<int> versionId)
        {
            WriteLog(MethodBase.GetCurrentMethod(), versionId);
            return base.LoadIndexDocumentByVersionId(versionId);
        }
        protected internal override IEnumerable<int> GetIdsOfNodesThatDoNotHaveIndexDocument(int fromId, int toId)
        {
            WriteLog(MethodBase.GetCurrentMethod());
            return base.GetIdsOfNodesThatDoNotHaveIndexDocument(fromId, toId);
        }
        public override int GetLastActivityId()
        {
            WriteLog(MethodBase.GetCurrentMethod());
            return base.GetLastActivityId();
        }
        public override IDataProcedure GetTimestampDataForOneNodeIntegrityCheck(string path, int[] excludedNodeTypeIds)
        {
            WriteLog(MethodBase.GetCurrentMethod(), path);
            return base.GetTimestampDataForOneNodeIntegrityCheck(path, excludedNodeTypeIds);
        }
        public override IDataProcedure GetTimestampDataForRecursiveIntegrityCheck(string path, int[] excludedNodeTypeIds)
        {
            WriteLog(MethodBase.GetCurrentMethod(), path);
            return base.GetTimestampDataForRecursiveIntegrityCheck(path, excludedNodeTypeIds);
        }
        public override IEnumerable<string> GetScriptsForDatabaseBackup()
        {
            WriteLog(MethodBase.GetCurrentMethod());
            return base.GetScriptsForDatabaseBackup();
        }
        protected internal override List<ContentListType> GetContentListTypesInTree(string path)
        {
            WriteLog(MethodBase.GetCurrentMethod(), path);
            return base.GetContentListTypesInTree(path);
        }
        protected internal override IEnumerable<int> GetChildrenIdentfiers(int nodeId)
        {
            WriteLog(MethodBase.GetCurrentMethod(), nodeId);
            return base.GetChildrenIdentfiers(nodeId);
        }
        protected internal override int InstanceCount(int[] nodeTypeIds)
        {
            WriteLog(MethodBase.GetCurrentMethod(), nodeTypeIds);
            return base.InstanceCount(nodeTypeIds);
        }
        protected internal override IEnumerable<int> QueryNodesByPath(string pathStart, bool orderByPath)
        {
            WriteLog(MethodBase.GetCurrentMethod(), pathStart, orderByPath);
            return base.QueryNodesByPath(pathStart, orderByPath);
        }
        protected internal override IEnumerable<int> QueryNodesByType(int[] typeIds)
        {
            WriteLog(MethodBase.GetCurrentMethod(), typeIds);
            return base.QueryNodesByType(typeIds);
        }
        protected internal override IEnumerable<int> QueryNodesByTypeAndPath(int[] nodeTypeIds, string pathStart, bool orderByPath)
        {
            WriteLog(MethodBase.GetCurrentMethod(), nodeTypeIds, pathStart, orderByPath);
            return base.QueryNodesByTypeAndPath(nodeTypeIds, pathStart, orderByPath);
        }
        protected internal override IEnumerable<int> QueryNodesByTypeAndPath(int[] nodeTypeIds, string[] pathStart, bool orderByPath)
        {
            WriteLog(MethodBase.GetCurrentMethod(), nodeTypeIds, "[\"" + String.Join("\", \"", pathStart) + "\"]", orderByPath);
            return base.QueryNodesByTypeAndPath(nodeTypeIds, pathStart, orderByPath);
        }
        protected internal override IEnumerable<int> QueryNodesByTypeAndPathAndName(int[] nodeTypeIds, string pathStart, bool orderByPath, string name)
        {
            WriteLog(MethodBase.GetCurrentMethod(), nodeTypeIds, pathStart, orderByPath, name);
            return base.QueryNodesByTypeAndPathAndName(nodeTypeIds, pathStart, orderByPath, name);
        }
        protected internal override IEnumerable<int> QueryNodesByTypeAndPathAndName(int[] nodeTypeIds, string[] pathStart, bool orderByPath, string name)
        {
            WriteLog(MethodBase.GetCurrentMethod(), nodeTypeIds, pathStart == null ? "[null]" : "[\"" + String.Join("\", \"", pathStart) + "\"]", orderByPath, name);
            return base.QueryNodesByTypeAndPathAndName(nodeTypeIds, pathStart, orderByPath, name);
        }
        protected internal override IEnumerable<int> QueryNodesByTypeAndPathAndProperty(int[] nodeTypeIds, string pathStart, bool orderByPath, List<QueryPropertyData> properties)
        {
            WriteLog(MethodBase.GetCurrentMethod(), nodeTypeIds, pathStart, orderByPath, properties);
            return base.QueryNodesByTypeAndPathAndProperty(nodeTypeIds, pathStart, orderByPath, properties);
        }
        protected internal override IEnumerable<int> QueryNodesByReferenceAndType(string referenceName, int referredNodeId, int[] allowedTypeIds)
        {
            WriteLog(MethodBase.GetCurrentMethod(), referenceName, referredNodeId, allowedTypeIds);
            return base.QueryNodesByReferenceAndType(referenceName, referredNodeId, allowedTypeIds);
        }
        protected internal override void CommitChunk(int versionId, int propertyTypeId, string token, long fullSize, BinaryDataValue source = null)
        {
            WriteLog(MethodBase.GetCurrentMethod(), versionId, propertyTypeId, token, fullSize, source);
            base.CommitChunk(versionId, propertyTypeId, token, fullSize, source);
        }
        public override void DeletePackage(Package package)
        {
            WriteLog(MethodBase.GetCurrentMethod(), package);
            base.DeletePackage(package);
        }
        public override void DeleteAllPackages()
        {
            WriteLog(MethodBase.GetCurrentMethod());
            base.DeleteAllPackages();
        }
        protected override void InitializeForTestsPrivate()
        {
            WriteLog(MethodBase.GetCurrentMethod());
            base.InitializeForTestsPrivate();
        }
        protected internal override bool IsFilestreamEnabled()
        {
            WriteLog(MethodBase.GetCurrentMethod());
            return base.IsFilestreamEnabled();
        }
        public override bool IsPackageExist(string componentId, PackageType packageType, Version version)
        {
            WriteLog(MethodBase.GetCurrentMethod(), componentId, packageType, version);
            return base.IsPackageExist(componentId, packageType, version);
        }
        public override IEnumerable<ComponentInfo> LoadInstalledComponents()
        {
            WriteLog(MethodBase.GetCurrentMethod());
            return base.LoadInstalledComponents();
        }
        public override IEnumerable<Package> LoadInstalledPackages()
        {
            WriteLog(MethodBase.GetCurrentMethod());
            return base.LoadInstalledPackages();
        }
        public override void SavePackage(Package package)
        {
            WriteLog(MethodBase.GetCurrentMethod(), package);
            base.SavePackage(package);
        }
        protected internal override string StartChunk(int versionId, int propertyTypeId, long fullSize)
        {
            WriteLog(MethodBase.GetCurrentMethod(), versionId, propertyTypeId);
            return base.StartChunk(versionId, propertyTypeId, fullSize);
        }
        public override void UpdatePackage(Package package)
        {
            WriteLog(MethodBase.GetCurrentMethod(), package);
            base.UpdatePackage(package);
        }
        protected internal override void WriteChunk(int versionId, string token, byte[] buffer, long offset, long fullSize)
        {
            WriteLog(MethodBase.GetCurrentMethod(), token, buffer, offset, fullSize);
            base.WriteChunk(versionId, token, buffer, offset, fullSize);
        }

        protected internal override int AcquireTreeLock(string path)
        {
            WriteLog(MethodBase.GetCurrentMethod(), path);
            return base.AcquireTreeLock(path);
        }
        protected internal override bool IsTreeLocked(string path)
        {
            WriteLog(MethodBase.GetCurrentMethod(), path);
            return base.IsTreeLocked(path);
        }
        protected internal override void ReleaseTreeLock(int[] lockIds)
        {
            WriteLog(MethodBase.GetCurrentMethod(), lockIds);
            base.ReleaseTreeLock(lockIds);
        }
    }

    internal class TraceNodeWriter : INodeWriter
    {
        private SqlNodeWriter _writer = new SqlNodeWriter();

        private void WriteLog(string text)
        {
            Debug.WriteLine("@#$Test> SQLACCESS: NODEWRITER: " + text);
        }
        private void WriteLog(MethodBase methodBase, params object[] prms)
        {
            var log = new StringBuilder("@#$Test> SQLACCESS: NODEWRITER: ");
            log.Append(methodBase.Name).Append("(");
            ParameterInfo[] prmInfos = methodBase.GetParameters();
            for (int i = 0; i < prmInfos.Length; i++)
            {
                if (i > 0)
                    log.Append(", ");
                log.Append(prmInfos[i].Name).Append("=<");
                log.Append(FormatValue(prms[i]));
                log.Append(">");
            }
            log.Append(");");
            Debug.WriteLine(log);
        }
        private string FormatValue(object value)
        {
            var intArray = value as IEnumerable<int>;
            if (intArray != null)
                return TraceSqlProvider.FormatIntArray(intArray);
            if (value == null)
                return "<null>";
            var nodeData = value as NodeData;
            if(nodeData != null)
                return FormatNodeData(nodeData);
            return value.ToString();
        }
        private string FormatNodeData(NodeData nodeData)
        {
            return String.Format("NodeData: {0}, '{1}'", nodeData.Id, nodeData.Path);
        }

        private int _stringProperties;
        private int _dateTimeProperties;
        private int _intProperties;
        private int _currencyProperties;
        private List<int> _pages;

        private int _textPropertyPayload;
        private List<int> _textProperties;

        private void AddPage(PropertyType propertyType, int pageSize)
        {
            var page = propertyType.Mapping / pageSize;
            if (!_pages.Contains(page))
                _pages.Add(page);
        }
        private void AddTextProperty(PropertyType propertyType, string value)
        {
            _textProperties.Add(propertyType.Id);
            if (!string.IsNullOrEmpty(value))
                _textPropertyPayload += value.Length;
        }

        public void Open()
        {
            _stringProperties = 0;
            _dateTimeProperties = 0;
            _intProperties = 0;
            _currencyProperties = 0;
            _pages = new List<int>();

            _textPropertyPayload = 0;
            _textProperties = new List<int>();

            _writer.Open();
        }
        public void Close()
        {
            if (_pages.Count > 0)
                WriteLog(String.Format("Write flatroperties. Strings: {0}, Dates: {1}, Ints: {2}, Numbers: {3}. Affected pages: {4}",
                    _stringProperties, _dateTimeProperties, _intProperties, _currencyProperties, String.Join(", ", _pages)));
            if (_textProperties.Count > 0)
                WriteLog(String.Format("Write textproperties. Payload: {0}, PropertyTypes: {1}", _textPropertyPayload, String.Join(", ", _textProperties)));
            _writer.Close();
        }
        public void InsertNodeAndVersionRows(NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            _writer.InsertNodeAndVersionRows(nodeData, out lastMajorVersionId, out lastMinorVersionId);
            WriteLog(MethodBase.GetCurrentMethod(), nodeData, lastMajorVersionId, lastMinorVersionId);
        }
        public void UpdateSubTreePath(string oldPath, string newPath)
        {
            WriteLog(MethodBase.GetCurrentMethod(), oldPath, newPath);
            _writer.UpdateSubTreePath(oldPath, newPath);
        }
        public void UpdateNodeRow(NodeData nodeData)
        {
            WriteLog(MethodBase.GetCurrentMethod(), nodeData);
            _writer.UpdateNodeRow(nodeData);
        }
        public void UpdateVersionRow(NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            _writer.UpdateVersionRow(nodeData, out lastMajorVersionId, out lastMinorVersionId);
            WriteLog(MethodBase.GetCurrentMethod(), nodeData, lastMajorVersionId, lastMinorVersionId);
        }
        public void CopyAndUpdateVersion(NodeData nodeData, int previousVersionId, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            _writer.CopyAndUpdateVersion(nodeData, previousVersionId, out lastMajorVersionId, out lastMinorVersionId);
            // WriteLog(MethodBase.GetCurrentMethod(), nodeData, previousVersionId, lastMajorVersionId, lastMinorVersionId);
        }
        public void CopyAndUpdateVersion(NodeData nodeData, int previousVersionId, int destinationVersionId, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            _writer.CopyAndUpdateVersion(nodeData, previousVersionId, destinationVersionId, out lastMajorVersionId, out lastMinorVersionId);
            WriteLog(MethodBase.GetCurrentMethod(), nodeData, previousVersionId, destinationVersionId, lastMajorVersionId, lastMinorVersionId);
        }
        public void SaveStringProperty(int versionId, PropertyType propertyType, string value)
        {
            _stringProperties++;
            AddPage(propertyType, SqlProvider.StringPageSize);
            _writer.SaveStringProperty(versionId, propertyType, value);
        }
        public void SaveDateTimeProperty(int versionId, PropertyType propertyType, DateTime value)
        {
            _dateTimeProperties++;
            AddPage(propertyType, SqlProvider.DateTimePageSize);
            _writer.SaveDateTimeProperty(versionId, propertyType, value);
        }
        public void SaveIntProperty(int versionId, PropertyType propertyType, int value)
        {
            _intProperties++;
            AddPage(propertyType, SqlProvider.IntPageSize);
            _writer.SaveIntProperty(versionId, propertyType, value);
        }
        public void SaveCurrencyProperty(int versionId, PropertyType propertyType, decimal value)
        {
            _currencyProperties++;
            AddPage(propertyType, SqlProvider.CurrencyPageSize);
            _writer.SaveCurrencyProperty(versionId, propertyType, value);
        }
        public void SaveTextProperty(int versionId, PropertyType propertyType, bool isLoaded, string value)
        {
            AddTextProperty(propertyType, value);
            _writer.SaveTextProperty(versionId, propertyType, isLoaded, value);
        }
        public void SaveReferenceProperty(int versionId, PropertyType propertyType, IEnumerable<int> value)
        {
            WriteLog(MethodBase.GetCurrentMethod(), versionId, propertyType, value);
            _writer.SaveReferenceProperty(versionId, propertyType, value);
        }
        public void InsertBinaryProperty(BinaryDataValue value, int versionId, int propertyTypeId, bool isNewNode)
        {
            WriteLog(MethodBase.GetCurrentMethod(), value, versionId, propertyTypeId, isNewNode);
            _writer.InsertBinaryProperty(value, versionId, propertyTypeId, isNewNode);
        }
        public void UpdateBinaryProperty(BinaryDataValue value)
        {
            WriteLog(MethodBase.GetCurrentMethod(), value);
            _writer.UpdateBinaryProperty(value);
        }
        public void DeleteBinaryProperty(int versionId, PropertyType propertyType)
        {
            WriteLog(MethodBase.GetCurrentMethod(), versionId, propertyType);
            _writer.DeleteBinaryProperty(versionId, propertyType);
        }
    }

}
