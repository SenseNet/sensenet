using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;

namespace SenseNet.Tests.Implementations
{
    public class InMemoryTestingDataProvider : ITestingDataProviderExtension
    {
        private DataProvider _mainProvider; //DB:ok
        public DataProvider MainProvider => _mainProvider ?? (_mainProvider = DataProvider.Instance); //DB:ok

        // ReSharper disable once InconsistentNaming
        public InMemoryDataProvider.Database DB => ((InMemoryDataProvider) MainProvider).DB;

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

        public virtual string TestMethodThatIsNotInterfaceMember(string input)
        {
            return input + input;
        }

        public void SetContentHandler(string contentTypeName, string handler)
        {
            var fileRecord = DB.Files.First(ff =>
                ff.Extension == ".ContentType" && ff.FileNameWithoutExtension == contentTypeName);

            // change handler in the blob
            EditFileStream(fileRecord, xDoc =>
            {
                xDoc.DocumentElement.Attributes["handler"].Value = handler;
            });

            // change handler in the preloaded xml schema
            var schema = DB.Schema;
            var nsmgr = new XmlNamespaceManager(schema.NameTable);
            nsmgr.AddNamespace("x", SchemaRoot.RepositoryStorageSchemaXmlNamespace);

            var classNameAttr = schema.SelectSingleNode($"/x:StorageSchema/x:NodeTypeHierarchy//x:NodeType[@name='{contentTypeName}']",
                nsmgr)?.Attributes?["className"];

            if (classNameAttr != null)
                classNameAttr.Value = handler;
            else
                throw new InvalidOperationException($"NodeType not found: {contentTypeName}");
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

                var fields = xDoc.DocumentElement["Fields"];
                fields.AppendChild(fieldNode);
            });

            // change field in the preloaded xml schema
            var schema = DB.Schema;
            var nsmgr = new XmlNamespaceManager(schema.NameTable);
            nsmgr.AddNamespace("x", SchemaRoot.RepositoryStorageSchemaXmlNamespace);

            var propsNode = schema.SelectSingleNode("/x:StorageSchema/x:UsedPropertyTypes", nsmgr);
            var propTypeId = propsNode.ChildNodes.Cast<XmlNode>().Select(pn => int.Parse(pn.Attributes["itemID"].Value))
                                 .Max() + 1;

            var propNode = schema.CreateElement("PropertyType", SchemaRoot.RepositoryStorageSchemaXmlNamespace);
            propNode.SetAttribute("itemID", propTypeId.ToString());
            propNode.SetAttribute("name", fieldName);
            propNode.SetAttribute("dataType", "String");
            propNode.SetAttribute("mapping", "100");

            propsNode.AppendChild(propNode);

            var typeNode = schema
                .SelectSingleNode($"/x:StorageSchema/x:NodeTypeHierarchy//x:NodeType[@name='{contentTypeName}']",
                    nsmgr);

            propNode = schema.CreateElement("PropertyType", SchemaRoot.RepositoryStorageSchemaXmlNamespace);
            propNode.SetAttribute("name", fieldName);

            typeNode.AppendChild(propNode);
        }

        private static void EditFileStream(InMemoryDataProvider.FileRecord fileRecord, Action<XmlDocument> action)
        {
            using (var xmlReaderStream = new MemoryStream(fileRecord.Stream))
            {
                var gcXmlDoc = new XmlDocument();
                gcXmlDoc.Load(xmlReaderStream);

                action(gcXmlDoc);

                var ctdString = gcXmlDoc.OuterXml;

                fileRecord.Stream = Encoding.UTF8.GetBytes(ctdString);
                fileRecord.Size = fileRecord.Stream.Length;
            }
        }
    }
}
