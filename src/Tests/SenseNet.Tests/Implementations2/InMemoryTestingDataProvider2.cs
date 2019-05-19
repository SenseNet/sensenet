using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using SenseNet.ContentRepository.Schema;
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

        public int GetLastNodeId()
        {
            return DB.Nodes.Count == 0 ? 0 : DB.Nodes.Max(n => n.NodeId);
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

        public virtual string TestMethodThatIsNotInterfaceMember(string input)
        {
            return input + input;
        }
    }
}
