using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;
using SenseNet.Tests.Implementations;

namespace SenseNet.Tests.Implementations2 //UNDONE:DB -------CLEANUP: move to SenseNet.Tests.Implementations
{
    public class InMemoryTestingDataProvider2 : ITestingDataProviderExtension
    {
        private DataProvider2 _mainProvider; //DB:ok
        public DataProvider2 MainProvider => _mainProvider ?? (_mainProvider = DataStore.DataProvider); //DB:ok

        // ReSharper disable once InconsistentNaming
        public InMemoryDataBase2 DB => ((InMemoryDataProvider2)MainProvider).DB;

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
                ff.Extension == ".ContentType" && ff.FileNameWithoutExtension == contentTypeName);

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
            var ctx = BlobStorage.GetBlobStorageContext(fileRecord.FileId);
            var blobProvider = ctx.Provider;

            var gcXmlDoc = new XmlDocument();
            using (var xmlReaderStream = blobProvider.GetStreamForRead(ctx))
                gcXmlDoc.Load(xmlReaderStream);

            action(gcXmlDoc);

            var ctdString = gcXmlDoc.OuterXml;

            var blobProvider2 = BlobStorage.GetProvider(ctdString.Length);
            var ctx2 = new BlobStorageContext(blobProvider)
            {
                VersionId = ctx.VersionId,
                PropertyTypeId = ctx.PropertyTypeId,
                Length = ctdString.Length
            };
            blobProvider2.Allocate(ctx2);

            using (var xmlWriterStream = blobProvider2.GetStreamForWrite(ctx2))
            using (var writer = new StreamWriter(xmlWriterStream))
            {
                writer.Write(ctdString);
                writer.Flush();
                fileRecord.Size = xmlWriterStream.Length;
            }
            fileRecord.BlobProvider = blobProvider2.GetType().FullName;
            fileRecord.BlobProviderData = BlobStorageContext.SerializeBlobProviderData(ctx2.BlobProviderData);
        }

        public void AddField(string contentTypeName, string fieldName, string fieldType = null, string fieldHandler = null)
        {
            var fileRecord = DB.Files.First(ff =>
                ff.Extension == ".ContentType" && ff.FileNameWithoutExtension == contentTypeName);

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

        public Task<object> GetPropertyValueAsync(int versionId, string name)
        {
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
                        case DataType.Reference:
                            version.DynamicProperties.TryGetValue(name, out result);
                            break;
                        case DataType.Text:
                            result = DB.LongTextProperties
                                .FirstOrDefault(x => x.VersionId == versionId && x.PropertyTypeId == pt.Id)?.Value;
                            break;
                        case DataType.Binary:
                            result = BlobStorage.LoadBinaryProperty(versionId, pt.Id);
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
                        case DataType.Reference:
                            version.DynamicProperties[name] = value;
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
            throw new NotImplementedException();
        }

        public virtual string TestMethodThatIsNotInterfaceMember(string input)
        {
            return input + input;
        }
    }
}
