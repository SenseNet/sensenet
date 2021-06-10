using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SenseNet.Configuration;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;
using SenseNet.Testing;

namespace SenseNet.Tests.Core.Implementations
{
    /// <summary>
    /// In-memory implementation of the <see cref="ITestingDataProviderExtension"/> interface.
    /// It requires the main data provider to be an <see cref="InMemoryDataProvider"/>.
    /// </summary>
    public class InMemoryTestingDataProvider : ITestingDataProviderExtension
    {
        private DataProvider _mainProvider;
        public DataProvider MainProvider => _mainProvider ?? (_mainProvider = Providers.Instance.DataProvider);

        // ReSharper disable once InconsistentNaming
        public InMemoryDataBase DB => ((InMemoryDataProvider)MainProvider).DB;

        public void InitializeForTests()
        {
            // do nothing
        }

        public string GetSecurityControlStringForTests()
        {
            throw new NotImplementedException();
        }

        public int GetPermissionLogEntriesCountAfterMoment(DateTime moment)
        {
            return DB.LogEntries.Count(x => x.Title == "PermissionChanged" && x.LogDate >= moment);
        }

        public AuditLogEntry[] LoadLastAuditLogEntries(int count)
        {
            return DB.LogEntries
                .OrderByDescending(e => e.LogId)
                .Take(count)
                .OrderBy(e => e.LogId)
                .Select(e => new AuditLogEntry
                {
                    Id = e.LogId,
                    EventId = e.EventId,
                    Title = e.Title,
                    ContentId = e.ContentId,
                    ContentPath = e.ContentPath,
                    UserName = e.UserName,
                    LogDate = new DateTime(e.LogDate.Ticks),
                    Message = e.Message,
                    FormattedMessage = e.FormattedMessage
                })
                .ToArray();
        }

        public void CheckScript(string commandText)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IndexIntegrityCheckerItem> GetTimestampDataForOneNodeIntegrityCheck(string path, int[] excludedNodeTypeIds)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IndexIntegrityCheckerItem> GetTimestampDataForRecursiveIntegrityCheck(string path, int[] excludedNodeTypeIds)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetLastNodeIdAsync()
        {
            return Task.FromResult(DB.Nodes.Count == 0 ? 0 : DB.Nodes.Max(n => n.NodeId));
        }

        public void SetContentHandler(string contentTypeName, string handler)
        {
            var fileRecord = DB.Files.First(ff =>
                ff.Extension == "ContentType" && ff.FileNameWithoutExtension == contentTypeName);

            // change handler in the blob
            EditFileStream(fileRecord, xDoc =>
            {
                // ReSharper disable once PossibleNullReferenceException
                xDoc.DocumentElement.Attributes["handler"].Value = handler;
            });

            // change handler in the preloaded xml schema
            var schema = DB.Schema;
            var nodeType = schema.NodeTypes.FirstOrDefault(x => x.Name == contentTypeName);
            if (nodeType != null)
                nodeType.ClassName = handler;
        }
        private static void EditFileStream(FileDoc fileRecord, Action<XmlDocument> action)
        {
            var blobStorage = Providers.Instance.BlobStorage;
            var ctx = blobStorage.GetBlobStorageContextAsync(fileRecord.FileId, CancellationToken.None)
                .GetAwaiter().GetResult();
            var blobProvider = ctx.Provider;

            var gcXmlDoc = new XmlDocument();
            using (var xmlReaderStream = blobProvider.GetStreamForRead(ctx))
                gcXmlDoc.Load(xmlReaderStream);

            action(gcXmlDoc);

            var ctdString = gcXmlDoc.OuterXml;

            var blobProvider2 = blobStorage.GetProvider(ctdString.Length);
            var ctx2 = new BlobStorageContext(blobProvider)
            {
                VersionId = ctx.VersionId,
                PropertyTypeId = ctx.PropertyTypeId,
                Length = ctdString.Length
            };
            blobProvider2.AllocateAsync(ctx2, CancellationToken.None).GetAwaiter().GetResult();

            using (var xmlWriterStream = blobProvider2.GetStreamForWrite(ctx2))
            {
                xmlWriterStream.Write(Encoding.UTF8.GetBytes(ctdString));
                xmlWriterStream.Flush();
                fileRecord.Size = xmlWriterStream.Length;
            }

            fileRecord.BlobProvider = blobProvider2.GetType().FullName;
            fileRecord.BlobProviderData = BlobStorageContext.SerializeBlobProviderData(ctx2.BlobProviderData);
        }

        public void AddField(string contentTypeName, string fieldName, string fieldType = null, string fieldHandler = null)
        {
            var fileRecord = DB.Files.First(ff =>
                ff.Extension == "ContentType" && ff.FileNameWithoutExtension == contentTypeName);

            // change field in the blob
            EditFileStream(fileRecord, xDoc =>
            {
                var fieldNode = xDoc.CreateElement("Field", ContentType.ContentDefinitionXmlNamespace);
                fieldNode.SetAttribute("name", fieldName);

                if (!string.IsNullOrEmpty(fieldType))
                    fieldNode.SetAttribute("type", fieldType);
                if (!string.IsNullOrEmpty(fieldHandler))
                    fieldNode.SetAttribute("handler", fieldHandler);

                // ReSharper disable once PossibleNullReferenceException
                var fields = xDoc.DocumentElement["Fields"];
                fields?.AppendChild(fieldNode);
            });

            var schema = DB.Schema;
            var propTypeId = schema.PropertyTypes.Max(x => x.Id) + 1;

            schema.PropertyTypes.Add(new PropertyTypeData
            {
                Id = propTypeId,
                DataType = DataType.String,
                Name = fieldName,
                Mapping = 100
            });

            var typeNode = schema.NodeTypes.FirstOrDefault(x => x.Name == contentTypeName);
            if (!typeNode?.Properties.Contains(fieldName) == true)
                typeNode.Properties.Add(fieldName);
        }

        public Task<int[]> GetChildNodeIdsByParentNodeIdAsync(int parentNodeId)
        {
            var result = DB.Nodes
                .Where(x => x.ParentNodeId == parentNodeId)
                .Select(x => x.NodeId)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task<NodeHeadData> GetNodeHeadDataAsync(int nodeId)
        {
            lock (DB)
            {
                var nodeDoc = DB.Nodes.FirstOrDefault(x => x.NodeId == nodeId);
                if (nodeDoc == null)
                    return Task.FromResult((NodeHeadData)null);


                var result = new NodeHeadData
                {
                    NodeId = nodeDoc.NodeId,
                    NodeTypeId = nodeDoc.NodeTypeId,
                    ContentListTypeId = nodeDoc.ContentListTypeId,
                    ContentListId = nodeDoc.ContentListId,
                    CreatingInProgress = nodeDoc.CreatingInProgress,
                    IsDeleted = nodeDoc.IsDeleted,
                    ParentNodeId = nodeDoc.ParentNodeId,
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
                    LastMinorVersionId = nodeDoc.LastMinorVersionId,
                    LastMajorVersionId = nodeDoc.LastMajorVersionId,
                    CreationDate = nodeDoc.CreationDate,
                    CreatedById = nodeDoc.CreatedById,
                    ModificationDate = nodeDoc.ModificationDate,
                    ModifiedById = nodeDoc.ModifiedById,
                    IsSystem = nodeDoc.IsSystem,
                    OwnerId = nodeDoc.OwnerId,
                    SavingState = nodeDoc.SavingState,
                    Timestamp = nodeDoc.Timestamp,
                };

                return Task.FromResult(result);
            }
        }

        public Task<VersionData> GetVersionDataAsync(int versionId)
        {
            lock (DB)
            {
                var versionDoc = DB.Versions.FirstOrDefault(x => x.VersionId == versionId);
                if (versionDoc == null)
                    return Task.FromResult((VersionData) null);

                var result = new VersionData
                {
                    VersionId = versionDoc.VersionId,
                    NodeId = versionDoc.NodeId,
                    Version = versionDoc.Version,
                    CreationDate = versionDoc.CreationDate,
                    CreatedById = versionDoc.CreatedById,
                    ModificationDate = versionDoc.ModificationDate,
                    ModifiedById = versionDoc.ModifiedById,
                    ChangedData = versionDoc.ChangedData,
                    Timestamp = versionDoc.Timestamp,
                };
                return Task.FromResult(result);
            }
        }

        public Task<int> GetBinaryPropertyCountAsync(string path)
        {
            lock (DB)
            {
                if (string.IsNullOrEmpty(path) || path == RepositoryPath.PathSeparator)
                    return Task.FromResult(DB.BinaryProperties.Count);

                var result = from b in DB.BinaryProperties
                    join v in DB.Versions on b.VersionId equals v.VersionId
                    join n in DB.Nodes on v.NodeId equals n.NodeId
                    where n.Path.StartsWith(
                              path + RepositoryPath.PathSeparator, StringComparison.InvariantCultureIgnoreCase)
                          || n.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase)
                    select b.BinaryPropertyId;

                return Task.FromResult(result.Count());
            }
        }

        public Task<int> GetFileCountAsync(string path)
        {
            lock (DB)
            {
                if (string.IsNullOrEmpty(path) || path == RepositoryPath.PathSeparator)
                    return Task.FromResult(DB.Files.Count);

                var result = from b in DB.BinaryProperties
                    join f in DB.Files on b.FileId equals f.FileId
                    join v in DB.Versions on b.VersionId equals v.VersionId
                    join n in DB.Nodes on v.NodeId equals n.NodeId
                    where n.Path.StartsWith(
                              path + RepositoryPath.PathSeparator, StringComparison.InvariantCultureIgnoreCase)
                          || n.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase)
                    select b.BinaryPropertyId;

                return Task.FromResult(result.Count());
            }
        }

        public Task<int> GetLongTextCountAsync(string path)
        {
            lock (DB)
            {
                if (string.IsNullOrEmpty(path) || path == RepositoryPath.PathSeparator)
                    return Task.FromResult(DB.LongTextProperties.Count);

                var result = from l in DB.LongTextProperties
                    join v in DB.Versions on l.VersionId equals v.VersionId
                    join n in DB.Nodes on v.NodeId equals n.NodeId
                    where n.Path.StartsWith(
                              path + RepositoryPath.PathSeparator, StringComparison.InvariantCultureIgnoreCase)
                          || n.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase)
                    select l.LongTextPropertyId;

                return Task.FromResult(result.Count());
            }
        }

        public Task<long> GetAllFileSize()
        {
            var result = DB.Files.Sum(f => f.Size);
            return Task.FromResult(result);
        }
        public async Task<long> GetAllFileSizeInSubtree(string path)
        {
            var nodeIds = DB.Nodes
                .Where(n => n.Path.StartsWith(path, StringComparison.OrdinalIgnoreCase))
                .Select(n => n.NodeId)
                .ToArray();
            return await GetFileSizesByNodeIds(nodeIds);
        }
        public async Task<long> GetFileSize(string path)
        {
            var nodeId = DB.Nodes.FirstOrDefault(n => n.Path.Equals(path, StringComparison.OrdinalIgnoreCase))?.NodeId ?? 0;
            if (nodeId == 0)
                return 0L;
            return await GetFileSizesByNodeIds(new[] {nodeId});
        }
        private Task<long> GetFileSizesByNodeIds(int[] nodeIds)
        {
            //var nodeId = DB.Nodes.First(n => n.Path.Equals(path, StringComparison.OrdinalIgnoreCase)).NodeId;

            var versionIds = DB.Versions
                .Where(v => nodeIds.Contains(v.NodeId))
                .Select(v => v.VersionId)
                .ToArray();
            var fileIds = DB.BinaryProperties
                .Where(b => versionIds.Contains(b.VersionId))
                .Select(b => b.FileId)
                .ToArray();
            var result = DB.Files
                .Where(f => fileIds.Contains(f.FileId))
                .Sum(f => f.Size);

            return Task.FromResult(result);
        }


        public Task<object> GetPropertyValueAsync(int versionId, string name)
        {
            var blobStorage = Providers.Instance.BlobStorage;
            var pt = ActiveSchema.PropertyTypes[name];
            object result = null;
            lock (DB)
            {
                var version = DB.Versions.FirstOrDefault(x => x.VersionId == versionId);
                if (version != null)
                {
                    switch (pt.DataType)
                    {
                        case DataType.String:
                        case DataType.Int:
                        case DataType.Currency:
                        case DataType.DateTime:
                            version.DynamicProperties.TryGetValue(name, out result);
                            break;
                        case DataType.Reference:
                            result = DB.ReferenceProperties
                                .FirstOrDefault(x => x.VersionId == versionId && x.PropertyTypeId == pt.Id)?.Value.ToArray();
                            break;
                        case DataType.Text:
                            result = DB.LongTextProperties
                                .FirstOrDefault(x => x.VersionId == versionId && x.PropertyTypeId == pt.Id)?.Value;
                            break;
                        case DataType.Binary:
                            result = blobStorage.LoadBinaryPropertyAsync(versionId, pt.Id, null).GetAwaiter().GetResult();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            return Task.FromResult(result);
        }

        public Task UpdateDynamicPropertyAsync(int versionId, string name, object value)
        {
            var pt = ActiveSchema.PropertyTypes[name];
            lock (DB)
            {
                var version = DB.Versions.FirstOrDefault(x => x.VersionId == versionId);
                if (version != null)
                {
                    switch (pt.DataType)
                    {
                        case DataType.String:
                        case DataType.Int:
                        case DataType.Currency:
                        case DataType.DateTime:
                            version.DynamicProperties[name] = value;
                            break;
                        case DataType.Reference:
                            if (!(value is List<int> listValue))
                                throw new ArgumentException($"The value is {value.GetType().Name}. Expected: List<int>");
                            var existingRef = DB.ReferenceProperties
                                .FirstOrDefault(x => x.VersionId == versionId && x.PropertyTypeId == pt.Id);
                            if (existingRef == null)
                                throw new ApplicationException($"The property does not exist: {name}.");
                            existingRef.Value = listValue;
                            break;
                        case DataType.Text:
                            if (!(value is string stringValue))
                                throw new ArgumentException($"The value is {value.GetType().Name}. Expected: string");
                            var existing = DB.LongTextProperties
                                .FirstOrDefault(x => x.VersionId == versionId && x.PropertyTypeId == pt.Id);
                            if (existing == null)
                                throw new ApplicationException($"The property does not exist: {name}.");
                            existing.Length = stringValue.Length;
                            existing.Value = stringValue;
                            break;
                        case DataType.Binary:
                            throw new NotImplementedException();
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            return Task.CompletedTask;
        }

        public Task SetFileStagingAsync(int fileId, bool staging)
        {
            lock (DB)
            {
                var fileDoc = DB.Files.FirstOrDefault(x => x.FileId == fileId);
                if (fileDoc != null)
                    fileDoc.Staging = staging;
                return Task.CompletedTask;
            }
        }

        public Task DeleteFileAsync(int fileId)
        {
            var fileDoc = DB.Files.FirstOrDefault(x => x.FileId == fileId);
            DB.Files.Remove(fileDoc);
            return Task.CompletedTask;
        }

        public Task EnsureOneUnlockedSchemaLockAsync()
        {
            //DB.SchemaLock = Guid.NewGuid().ToString();
            return Task.CompletedTask;
        }

        private DataCollection<SharedLockDoc> GetSharedLocks()
        {
            return ((InMemoryDataProvider)Providers.Instance.DataProvider).DB.GetCollection<SharedLockDoc>();
        }
        public DateTime GetSharedLockCreationDate(int nodeId)
        {
            var sharedLockRow = GetSharedLocks().First(x => x.ContentId == nodeId);
            return sharedLockRow.CreationDate;
        }
        public void SetSharedLockCreationDate(int nodeId, DateTime value)
        {
            var sharedLockRow = GetSharedLocks().First(x => x.ContentId == nodeId);
            sharedLockRow.CreationDate = value;
        }

        public DataProvider CreateCannotCommitDataProvider(DataProvider mainDataProvider)
        {
            var inMemDp = (InMemoryDataProvider) mainDataProvider;
            inMemDp.DB.TransactionFactory = new InMemoryCannotCommitTransactionFactory();
            return inMemDp;
        }

        public Task DeleteAllStatisticalDataRecordsAsync(IStatisticalDataProvider dataProvider)
        {
            var dp = (InMemoryStatisticalDataProvider) dataProvider;
            var dpAcc = new ObjectAccessor(dp);
            var storage = (List<StatisticalDataRecord>)dpAcc.GetFieldOrProperty("Storage");
            storage.Clear();
            return Task.CompletedTask;
        }
        public Task<IEnumerable<IStatisticalDataRecord>> LoadAllStatisticalDataRecords(IStatisticalDataProvider dataProvider)
        {
            var dp = (InMemoryStatisticalDataProvider)dataProvider;
            var dpAcc = new ObjectAccessor(dp);
            var storage = (List<StatisticalDataRecord>)dpAcc.GetFieldOrProperty("Storage");
            return Task.FromResult((IEnumerable<IStatisticalDataRecord>)storage);
        }

        #region CreateCannotCommitDataProvider classes
        private class InMemoryCannotCommitTransactionFactory : ITransactionFactory
        {
            public InMemoryTransaction CreateTransaction(InMemoryDataBase db)
            {
                return new InMemoryCannotCommitTransaction(db);
            }
        }

        public class InMemoryCannotCommitTransaction : InMemoryTransaction
        {
            public InMemoryCannotCommitTransaction(InMemoryDataBase db) : base(db) { }
            public override void Commit()
            {
                throw new NotSupportedException("This transaction cannot commit anything.");
            }
        }
        #endregion

        public virtual string TestMethodThatIsNotInterfaceMember(string input)
        {
            return input + input;
        }
    }
}
