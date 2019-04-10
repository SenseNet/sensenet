using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Tests.Implementations;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Portal;
using SenseNet.Portal.Virtualization;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class DataProviderTests : TestBase
    {
        private string _initialPropertyTypePath = @"D:\propertyTypes.txt";
        private string _initialNodeTypePath = @"D:\nodeTypes.txt";
        private string _initialNodesPath = @"D:\nodes.txt";
        private string _initialVersionsPath = @"D:\versions.txt";
        private string _initialDynamicDataPath = @"D:\dynamicData.txt";

        //[TestMethod]
        public void InitialData_Create()
        {
            DPTest(() =>
            {
                using (var ntw = new StreamWriter(_initialPropertyTypePath, false))
                using (var ptw = new StreamWriter(_initialNodeTypePath, false))
                using (var nw = new StreamWriter(_initialNodesPath, false))
                using (var vw = new StreamWriter(_initialVersionsPath, false))
                using (var dw = new StreamWriter(_initialDynamicDataPath, false))
                    InitialData.Save(ptw, ntw, nw, vw, dw, null, 
                        ()=> ((InMemoryDataProvider)DataProvider.Current).DB.Nodes.Select(x => x.NodeId));
            });
            Assert.Inconclusive();
        }
        //[TestMethod]
        public void InitialData_LoadStream()
        {
            DPTest(() =>
            {
                // see FileSystemReaderBlobProvider.GetStreamForRead and ParseData

                DataStore.Enabled = true;

                var node = Node.LoadNode("/Root/System/Settings/Logging.settings");
                string fileContent;
                using (var stream = node.GetBinary("Binary").GetStream())
                using (var reader = new StreamReader(stream))
                    fileContent = reader.ReadToEnd();
                Assert.IsTrue(fileContent.Length > 0);
            });
        }


        // The prefix DP_AB_ means: DataProvider A-B comparative test when A is the 
        //     old in-memory DataProvider implementation and B is the new one.

        //[TestMethod]
        //public void DP_AB_Schema_Save()
        //{
        //    DPTest(() =>
        //    {
        //        var storedSchema = GetStoredSchema();
        //        Assert.AreEqual(0L, storedSchema.Timestamp);
        //        Assert.IsNull(storedSchema.PropertyTypes);
        //        Assert.IsNull(storedSchema.NodeTypes);
        //        Assert.IsNull(storedSchema.ContentListTypes);

        //        var ed = new SchemaEditor();
        //        ed.Load();
        //        var xml = new XmlDocument();
        //        xml.LoadXml(ed.ToXml());

        //        DataStore.Enabled = true;
        //        var ed2 = new SchemaEditor();
        //        ed2.Load(xml);
        //        ed2.Register();

        //        storedSchema = GetStoredSchema();

        //        Assert.IsTrue(0L < storedSchema.Timestamp);
        //        Assert.AreEqual(ActiveSchema.PropertyTypes.Count, storedSchema.PropertyTypes.Count);
        //        Assert.AreEqual(ActiveSchema.NodeTypes.Count, storedSchema.NodeTypes.Count);
        //        Assert.AreEqual(ActiveSchema.ContentListTypes.Count, storedSchema.ContentListTypes.Count);
        //        //UNDONE:DB ----Deep check: storedSchema
        //    });
        //}
        //private RepositorySchemaData GetStoredSchema()
        //{
        //    return ((InMemoryDataProvider2) Providers.Instance.DataProvider2).DB.Schema;
        //}

        [TestMethod]
        public void DP_AB_Create()
        {
            // TESTED: DataProvider2: InsertNodeAsync(NodeData nodeData, NodeSaveSettings settings);

            DPTest(() =>
            {
                // ACTION-A
                DataStore.SnapshotsEnabled = true;
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Save();
                
                // ACTION-B
                DataStore.Enabled = true;
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Save();

                var nodeDataBeforeA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && !x.IsDp2).Snapshot;
                var nodeDataBeforeB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && x.IsDp2).Snapshot;
                var nodeDataAfterA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && !x.IsDp2).Snapshot;
                var nodeDataAfterB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && x.IsDp2).Snapshot;
                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);
                CheckDynamicDataByVersionId(folderA.VersionId);
            });
        }
        [TestMethod]
        public void DP_AB_Create_TextProperty()
        {
            // TESTED: DataProvider2: InsertNodeAsync(NodeData nodeData, NodeSaveSettings settings);

            DPTest(() =>
            {
                var description = "text property value.";

                // ACTION-A
                DataStore.SnapshotsEnabled = true;
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Description = description;
                folderA.Save();

                // ACTION-B
                DataStore.Enabled = true;
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Description = description;
                folderB.Save();

                var nodeDataBeforeA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && !x.IsDp2).Snapshot;
                var nodeDataBeforeB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && x.IsDp2).Snapshot;
                var nodeDataAfterA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && !x.IsDp2).Snapshot;
                var nodeDataAfterB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && x.IsDp2).Snapshot;
                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);
                CheckDynamicDataByVersionId(folderA.VersionId);
            });
        }
        [TestMethod]
        public void DP_AB_CreateFile()
        {
            // TESTED: DataProvider2: InsertNodeAsync(NodeData nodeData, NodeSaveSettings settings);

            DPTest(() =>
            {
                var filecontent = "File content.";

                // ACTION-A
                DataStore.SnapshotsEnabled = false;
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Save();
                var fileA = new File(folderA) { Name = "File1" };
                fileA.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent));
                DataStore.SnapshotsEnabled = true;
                fileA.Save();
                DataStore.SnapshotsEnabled = false;
                DistributedApplication.Cache.Reset();
                fileA = Node.Load<File>(fileA.Id);
                var reloadedFileContentA = RepositoryTools.GetStreamString(fileA.Binary.GetStream());

                // ACTION-B
                DataStore.Enabled = true;
                DataStore.SnapshotsEnabled = false;
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Save();
                var fileB = new File(folderB) { Name = "File1" };
                fileB.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent));
                DataStore.SnapshotsEnabled = true;
                fileB.Save();
                var fileBId = fileB.Id;
                DataStore.SnapshotsEnabled = false;
                DistributedApplication.Cache.Reset();
                fileB = Node.Load<File>(fileBId);
                var reloadedFileContentB = RepositoryTools.GetStreamString(fileB.Binary.GetStream());

                // ASSERT
                var nodeDataBeforeA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && !x.IsDp2).Snapshot;
                var nodeDataBeforeB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && x.IsDp2).Snapshot;
                var nodeDataAfterA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && !x.IsDp2).Snapshot;
                var nodeDataAfterB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && x.IsDp2).Snapshot;
                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);

                CheckDynamicDataByVersionId(fileA.VersionId);

                Assert.AreEqual(filecontent, reloadedFileContentA);
                Assert.AreEqual(filecontent, reloadedFileContentB);
            });
        }

        [TestMethod]
        public void DP_AB_Update()
        {
            // TESTED: DataProvider2: UpdateNodeAsync(NodeData nodeData, NodeSaveSettings settings, IEnumerable<int> versionIdsToDelete)

            DPTest(() =>
            {
                // PROVIDER-A
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Save();
                folderA = Node.Load<SystemFolder>(folderA.Id);
                folderA.Index++;
                DataStore.SnapshotsEnabled = true;
                folderA.Save();
                DataStore.SnapshotsEnabled = false;

                // PROVIDER-B
                DataStore.Enabled = true;
                DistributedApplication.Cache.Reset();
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Save();
                folderB = Node.Load<SystemFolder>(folderB.Id);
                folderB.Index++;
                DataStore.SnapshotsEnabled = true;
                folderB.Save();
                DataStore.SnapshotsEnabled = false;

                // Check
                var nodeDataBeforeA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && !x.IsDp2).Snapshot;
                var nodeDataBeforeB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && x.IsDp2).Snapshot;
                var nodeDataAfterA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && !x.IsDp2).Snapshot;
                var nodeDataAfterB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && x.IsDp2).Snapshot;

                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);
                Assert.IsTrue(nodeDataBeforeB.NodeTimestamp < nodeDataAfterB.NodeTimestamp);
                Assert.IsTrue(nodeDataBeforeB.VersionTimestamp < nodeDataAfterB.VersionTimestamp);
                CheckDynamicDataByVersionId(folderA.VersionId);
            });
        }
        [TestMethod]
        public void DP_AB_UpdateFile_SameVersion()
        {
            // TESTED: DataProvider2: UpdateNodeAsync(NodeData nodeData, NodeSaveSettings settings, IEnumerable<int> versionIdsToDelete)

            DPTest(() =>
            {
                var filecontent1 = "1111 File content 1.";
                var filecontent2 = "2222 File content 2.";

                //// ACTION-A
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Save();
                var fileA = new File(folderA) { Name = "File1" };
                fileA.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent1));
                fileA.Save();
                fileA = Node.Load<File>(fileA.Id);
                var binaryA = fileA.Binary;
                binaryA.SetStream(RepositoryTools.GetStreamFromString(filecontent2));
                fileA.Binary = binaryA;
                DataStore.SnapshotsEnabled = true;
                fileA.Save();
                DataStore.SnapshotsEnabled = false;
                DistributedApplication.Cache.Reset();
                fileA = Node.Load<File>(fileA.Id);
                var reloadedFileContentA = RepositoryTools.GetStreamString(fileA.Binary.GetStream());

                // ACTION-B
                DataStore.Enabled = true;
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Save();
                var fileB = new File(folderB) { Name = "File1" };
                fileB.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent1));
                fileB.Save();
                fileB = Node.Load<File>(fileB.Id);
                var binaryB = fileB.Binary;
                binaryB.SetStream(RepositoryTools.GetStreamFromString(filecontent2));
                fileB.Binary = binaryB;
                DataStore.SnapshotsEnabled = true;
                fileB.Save();
                DataStore.SnapshotsEnabled = false;
                DistributedApplication.Cache.Reset();
                fileB = Node.Load<File>(fileB.Id);
                var reloadedFileContentB = RepositoryTools.GetStreamString(fileB.Binary.GetStream());

                // ASSERT
                var nodeDataBeforeA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && !x.IsDp2).Snapshot;
                var nodeDataBeforeB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && x.IsDp2).Snapshot;
                var nodeDataAfterA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && !x.IsDp2).Snapshot;
                var nodeDataAfterB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && x.IsDp2).Snapshot;
                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);

                CheckDynamicDataByVersionId(fileA.VersionId);

                Assert.AreEqual(filecontent2, reloadedFileContentA);
                Assert.AreEqual(filecontent2, reloadedFileContentB);
            });
        }
        [TestMethod]
        public void DP_AB_UpdateFile_NewVersion()
        {
            DPTest(() =>
            {
                var filecontent1 = "1111 File content 1.";
                var filecontent2 = "2222 File content 2.";

                //// ACTION-A
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Save();
                var fileA = new File(folderA) { Name = "File1",VersioningMode = VersioningType.MajorAndMinor };
                fileA.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent1));
                fileA.Save();
                fileA = Node.Load<File>(fileA.Id);
                var binaryA = fileA.Binary;
                binaryA.SetStream(RepositoryTools.GetStreamFromString(filecontent2));
                fileA.Binary = binaryA;
                DataStore.SnapshotsEnabled = true;
                fileA.Save();
                DataStore.SnapshotsEnabled = false;
                DistributedApplication.Cache.Reset();
                fileA = Node.Load<File>(fileA.Id);
                var reloadedFileContentA = RepositoryTools.GetStreamString(fileA.Binary.GetStream());

                // ACTION-B
                DataStore.Enabled = true;
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Save();
                var fileB = new File(folderB) { Name = "File1", VersioningMode = VersioningType.MajorAndMinor};
                fileB.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent1));
                fileB.Save();
                fileB = Node.Load<File>(fileB.Id);
                var binaryB = fileB.Binary;
                binaryB.SetStream(RepositoryTools.GetStreamFromString(filecontent2));
                fileB.Binary = binaryB;
                DataStore.SnapshotsEnabled = true;
                fileB.Save();
                DataStore.SnapshotsEnabled = false;
                DistributedApplication.Cache.Reset();
                fileB = Node.Load<File>(fileB.Id);
                var reloadedFileContentB = RepositoryTools.GetStreamString(fileB.Binary.GetStream());

                // ASSERT
                var nodeDataBeforeA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && !x.IsDp2).Snapshot;
                var nodeDataBeforeB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && x.IsDp2).Snapshot;
                var nodeDataAfterA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && !x.IsDp2).Snapshot;
                var nodeDataAfterB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && x.IsDp2).Snapshot;
                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);

                CheckDynamicDataByVersionId(fileA.VersionId);

                Assert.AreEqual(filecontent2, reloadedFileContentA);
                Assert.AreEqual(filecontent2, reloadedFileContentB);
            });
        }
        [TestMethod]
        public void DP_AB_UpdateFile_ExpectedVersion()
        {
            DPTest(() =>
            {
                var filecontent1 = "1111 File content 1.";
                var filecontent2 = "2222 File content 2.";

                //// ACTION-A
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Save();
                var fileA = new File(folderA) { Name = "File1" };
                fileA.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent1));
                fileA.Save();
                fileA.CheckOut();
                fileA = Node.Load<File>(fileA.Id);
                var binaryA = fileA.Binary;
                binaryA.SetStream(RepositoryTools.GetStreamFromString(filecontent2));
                fileA.Binary = binaryA;
                fileA.Save();
                DataStore.SnapshotsEnabled = true;
                fileA.CheckIn();
                DataStore.SnapshotsEnabled = false;
                DistributedApplication.Cache.Reset();
                fileA = Node.Load<File>(fileA.Id);
                var reloadedFileContentA = RepositoryTools.GetStreamString(fileA.Binary.GetStream());

                // ACTION-B
                DataStore.Enabled = true;
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Save();
                var fileB = new File(folderB) { Name = "File1" };
                fileB.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent1));
                fileB.Save();
                fileB.CheckOut();
                fileB = Node.Load<File>(fileB.Id);
                var binaryB = fileB.Binary;
                binaryB.SetStream(RepositoryTools.GetStreamFromString(filecontent2));
                fileB.Binary = binaryB;
                fileB.Save();
                DataStore.SnapshotsEnabled = true;
                fileB.CheckIn();
                DataStore.SnapshotsEnabled = false;
                DistributedApplication.Cache.Reset();
                fileB = Node.Load<File>(fileB.Id);
                var reloadedFileContentB = RepositoryTools.GetStreamString(fileB.Binary.GetStream());

                // ASSERT
                var nodeDataBeforeA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && !x.IsDp2).Snapshot;
                var nodeDataBeforeB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && x.IsDp2).Snapshot;
                var nodeDataAfterA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && !x.IsDp2).Snapshot;
                var nodeDataAfterB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && x.IsDp2).Snapshot;
                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);

                CheckDynamicDataByVersionId(fileA.VersionId);

                Assert.AreEqual(filecontent2, reloadedFileContentA);
                Assert.AreEqual(filecontent2, reloadedFileContentB);
            });
        }
        [TestMethod]
        public void DP_AB_Update_HeadOnly()
        {
            DPTest(() =>
            {
                var filecontent1 = "1111 File content 1.";
                var filecontent2 = "2222 File content 2.";

                // ACTION-A
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Save();
                var fileA = new File(folderA) { Name = "File1" };
                fileA.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent1));
                fileA.Save();
                fileA.CheckOut();
                fileA = Node.Load<File>(fileA.Id);
                var binaryA = fileA.Binary;
                binaryA.SetStream(RepositoryTools.GetStreamFromString(filecontent2));
                fileA.Binary = binaryA;
                fileA.Save();
                DataStore.SnapshotsEnabled = true;
                fileA.UndoCheckOut();
                DataStore.SnapshotsEnabled = false;
                DistributedApplication.Cache.Reset();
                fileA = Node.Load<File>(fileA.Id);
                var reloadedFileContentA = RepositoryTools.GetStreamString(fileA.Binary.GetStream());

                // ACTION-B
                DataStore.Enabled = true;
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Save();
                var fileB = new File(folderB) { Name = "File1" };
                fileB.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent1));
                fileB.Save();
                fileB.CheckOut();
                fileB = Node.Load<File>(fileB.Id);
                var binaryB = fileB.Binary;
                binaryB.SetStream(RepositoryTools.GetStreamFromString(filecontent2));
                fileB.Binary = binaryB;
                fileB.Save();
                DataStore.SnapshotsEnabled = true;
                fileB.UndoCheckOut();
                DataStore.SnapshotsEnabled = false;
                DistributedApplication.Cache.Reset();
                fileB = Node.Load<File>(fileB.Id);
                var reloadedFileContentB = RepositoryTools.GetStreamString(fileB.Binary.GetStream());

                // ASSERT
                var nodeDataBeforeA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && !x.IsDp2).Snapshot;
                var nodeDataBeforeB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeBefore" && x.IsDp2).Snapshot;
                var nodeDataAfterA = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && !x.IsDp2).Snapshot;
                var nodeDataAfterB = (NodeData)DataStore.Snapshots.First(x => x.Name == "SaveNodeAfter" && x.IsDp2).Snapshot;
                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);

                CheckDynamicDataByVersionId(fileA.VersionId);

                Assert.AreEqual(filecontent1, reloadedFileContentA);
                Assert.AreEqual(filecontent1, reloadedFileContentB);
            });
        }

        [TestMethod]
        public void DP_HandleAllDynamicProps()
        {
            var contentTypeName = "TestContent";
            var ctd = $"<ContentType name='{contentTypeName}' parentType='GenericContent'" + @"
             handler='SenseNet.ContentRepository.GenericContent'
             xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <Fields>
    <Field name='ShortText1' type='ShortText'/>
    <Field name='LongText1' type='LongText'/>
    <Field name='Integer1' type='Integer'/>
    <Field name='Number1' type='Number'/>
    <Field name='DateTime1' type='DateTime'/>
    <Field name='Reference1' type='Reference'/>
  </Fields>
</ContentType>
";
            DPTest(() =>
            {
                try
                {
                    ContentTypeInstaller.InstallContentType(ctd);
                    var unused = ContentType.GetByName(contentTypeName); // preload schema
                    DataStore.Enabled = true;

                    var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                    folderB.Save();

                    var db = GetDb();

                    // ACTION-1 CREATE
                    // Create all kind of dynamic properties
                    var nodeB = new GenericContent(folderB, contentTypeName)
                    {
                        Name = $"{contentTypeName}1",
                        ["ShortText1"] = "ShortText value 1",
                        ["LongText1"] = "LongText value 1",
                        ["Integer1"] = 42,
                        ["Number1"] = 42.56m,
                        ["DateTime1"] = new DateTime(1111, 11, 11)
                    };
                    nodeB.AddReference("Reference1", Repository.Root);
                    nodeB.AddReference("Reference1", folderB);
                    nodeB.Save();

                    // ASSERT-1
                    var storedProps = db.Versions[nodeB.VersionId].DynamicProperties;
                    Assert.AreEqual("ShortText value 1", storedProps["ShortText1"]);
                    Assert.AreEqual("LongText value 1", storedProps["LongText1"]);
                    Assert.AreEqual(42, storedProps["Integer1"]);
                    Assert.AreEqual(42.56m, storedProps["Number1"]);
                    Assert.AreEqual(new DateTime(1111, 11, 11), storedProps["DateTime1"]);
                    Assert.AreEqual($"{Repository.Root.Id},{folderB.Id}", ArrayToString((int[])storedProps["Reference1"]));

                    // ACTION-2 UPDATE-1
                    nodeB = Node.Load<GenericContent>(nodeB.Id);
                    // Update all kind of dynamic properties
                    nodeB["ShortText1"] = "ShortText value 2";
                    nodeB["LongText1"] = "LongText value 2";
                    nodeB["Integer1"] = 43;
                    nodeB["Number1"] = 42.099m;
                    nodeB["DateTime1"] = new DateTime(1111, 11, 22);
                    nodeB.RemoveReference("Reference1", Repository.Root);
                    nodeB.Save();

                    // ASSERT-2
                    storedProps = db.Versions[nodeB.VersionId].DynamicProperties;
                    Assert.AreEqual("ShortText value 2", storedProps["ShortText1"]);
                    Assert.AreEqual("LongText value 2", storedProps["LongText1"]);
                    Assert.AreEqual(43, storedProps["Integer1"]);
                    Assert.AreEqual(42.099m, storedProps["Number1"]);
                    Assert.AreEqual(new DateTime(1111, 11, 22), storedProps["DateTime1"]);
                    Assert.AreEqual($"{folderB.Id}", ArrayToString((int[])storedProps["Reference1"]));

                    // ACTION-3 UPDATE-2
                    nodeB = Node.Load<GenericContent>(nodeB.Id);
                    // Remove existing references
                    nodeB.RemoveReference("Reference1", folderB);
                    nodeB.Save();

                    // ASSERT-3
                    storedProps = db.Versions[nodeB.VersionId].DynamicProperties;
                    Assert.AreEqual("ShortText value 2", storedProps["ShortText1"]);
                    Assert.AreEqual("LongText value 2", storedProps["LongText1"]);
                    Assert.AreEqual(43, storedProps["Integer1"]);
                    Assert.AreEqual(42.099m, storedProps["Number1"]);
                    Assert.AreEqual(new DateTime(1111, 11, 22), storedProps["DateTime1"]);
                    Assert.IsFalse(storedProps.ContainsKey("Reference1"));
                }
                finally
                {
                    DataStore.Enabled = false;
                    ContentTypeInstaller.RemoveContentType(contentTypeName);
                }
            });
        }

        [TestMethod]
        public void DP_Rename()
        {
            DPTest(() =>
            {
                // Create a small subtree
                DataStore.Enabled = true;
                var root = new SystemFolder(Repository.Root) { Name = "TestRoot" }; root.Save();
                var f1 = new SystemFolder(root) { Name = "F1" }; f1.Save();
                var f2 = new SystemFolder(root) { Name = "F2" }; f2.Save();
                var f3 = new SystemFolder(f1) { Name = "F3" }; f3.Save();
                var f4 = new SystemFolder(f1) { Name = "F4" }; f4.Save();

                // ACTION: Rename root
                root = Node.Load<SystemFolder>(root.Id);
                root.Name = "RENAMED";
                root.Save();

                // ASSERT
                f1 = Node.Load<SystemFolder>(f1.Id);
                f2 = Node.Load<SystemFolder>(f2.Id);
                f3 = Node.Load<SystemFolder>(f3.Id);
                f4 = Node.Load<SystemFolder>(f4.Id);
                Assert.AreEqual("/Root/RENAMED", root.Path);
                Assert.AreEqual("/Root/RENAMED/F1", f1.Path);
                Assert.AreEqual("/Root/RENAMED/F2", f2.Path);
                Assert.AreEqual("/Root/RENAMED/F1/F3", f3.Path);
                Assert.AreEqual("/Root/RENAMED/F1/F4", f4.Path);
            });
        }

        //UNDONE:DB TEST: DP_AB_Create and Rollback
        //UNDONE:DB TEST: DP_AB_Update and Rollback

        /* ================================================================================================== */

        private InMemoryDataBase2 GetDb()
        {
            return ((InMemoryDataProvider2)Providers.Instance.DataProvider2).DB;
        }
        private string ArrayToString(int[] array) //UNDONE:DB --------Move to TestBase
        {
            return string.Join(",", array.Select(x => x.ToString()));
        }

        private void CheckDynamicDataByVersionId(int versionId)
        {
            DataStore.SnapshotsEnabled = false;
            DataStore.Snapshots.Clear();

            DataStore.Enabled = false;
            DistributedApplication.Cache.Reset();
            var nodeA = Node.LoadNodeByVersionId(versionId);
            var unused1 = nodeA.PropertyTypes.Select(p => $"{p.Name}:{nodeA[p]}").ToArray();

            DataStore.Enabled = true;
            DistributedApplication.Cache.Reset();
            var nodeB = Node.LoadNodeByVersionId(versionId);
            var unused2 = nodeB.PropertyTypes.Select(p => $"{p.Name}:{nodeB[p]}").ToArray();

            DataProviderChecker.Assert_AreEqual(nodeA.Data, nodeB.Data);
        }

        private void DPTest(Action callback)
        {
            DataStore.Enabled = false;
            DataStore.SnapshotsEnabled = false;

            DistributedApplication.Cache.Reset();
            ContentTypeManager.Reset();
            var portalContextAcc = new PrivateType(typeof(PortalContext));
            portalContextAcc.SetStaticField("_sites", new Dictionary<string, Site>());

            var builder = CreateRepositoryBuilderForTest();

            Indexing.IsOuterSearchEngineEnabled = true;

            var dp2 = new InMemoryDataProvider2();
            Providers.Instance.DataProvider2 = dp2;
            Providers.Instance.BlobMetaDataProvider2 = new InMemoryBlobStorageMetaDataProvider2(dp2);

            using (Repository.Start(builder))
            {
                DataStore.InstallDataPackage(GetInitialStructure());
                new SnMaintenance().Shutdown();
                using (new SystemAccount())
                    callback();
            }
        }

        private InitialData _initialData;
        private InitialData GetInitialStructure()
        {
            if (_initialData == null)
            {
                using (var ptr = new StreamReader(@"D:\propertyTypes.txt"))
                using (var ntr = new StreamReader(@"D:\nodeTypes.txt"))
                using (var nr = new StreamReader(@"D:\nodes.txt"))
                using (var vr = new StreamReader(@"D:\versions.txt"))
                using (var dr = new StreamReader(@"D:\dynamicData.txt"))
                    _initialData = InitialData.Load(ptr, ntr, nr, vr, dr);
            }
            return _initialData;
        }
    }
}
