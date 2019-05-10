using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Search.Indexing.Activities;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Diagnostics;
using SenseNet.Portal;
using SenseNet.Portal.OData.Metadata;
using SenseNet.Portal.Virtualization;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;
using SenseNet.Tests.Implementations2;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class DataProviderTests : TestBase
    {
        // The prefix DP_AB_ means: DataProvider A-B comparative test when A is the 
        //     old in-memory DataProvider implementation and B is the new one.

        [TestMethod]
        public async STT.Task DP_InsertNode()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;
                var dp = DataStore.DataProvider;

                var root = CreateFolder(Repository.Root, "TestRoot");

                // Create a file but do not save.
                var created = new File(root) { Name = "File1", Index = 42, Description = "File1 Description" };
                created.Binary.SetStream(RepositoryTools.GetStreamFromString("File1 Content"));
                var nodeData = created.Data;
                nodeData.Path = RepositoryPath.Combine(created.ParentPath, created.Name);
                GenerateTestData(nodeData);


                // ACTION
                var nodeHeadData = nodeData.GetNodeHeadData();
                var versionData = nodeData.GetVersionData();
                var dynamicData = nodeData.GetDynamicData(false);
                var binaryProperty = dynamicData.BinaryProperties.First().Value;
                await dp.InsertNodeAsync(nodeHeadData, versionData, dynamicData);

                // ASSERT
                Assert.IsTrue(nodeHeadData.NodeId > 0);
                Assert.IsTrue(nodeHeadData.Timestamp > 0);
                Assert.IsTrue(versionData.VersionId > 0);
                Assert.IsTrue(versionData.Timestamp > 0);
                Assert.IsTrue(binaryProperty.Id > 0);
                Assert.IsTrue(binaryProperty.FileId > 0);
                Assert.IsTrue(nodeHeadData.LastMajorVersionId == versionData.VersionId);
                Assert.IsTrue(nodeHeadData.LastMajorVersionId == nodeHeadData.LastMinorVersionId);

                DistributedApplication.Cache.Reset();
                var loaded = Node.Load<File>(nodeHeadData.NodeId);
                Assert.IsNotNull(loaded);
                Assert.AreEqual("File1", loaded.Name);
                Assert.AreEqual(nodeHeadData.Path, loaded.Path);
                Assert.AreEqual(42, loaded.Index);
                Assert.AreEqual("File1 Content", RepositoryTools.GetStreamString(loaded.Binary.GetStream()));

                foreach (var propType in loaded.Data.PropertyTypes)
                    loaded.Data.GetDynamicRawData(propType);
                DataProviderChecker.Assert_DynamicPropertiesAreEqualExceptBinaries(nodeData, loaded.Data);

            });
        }
        private void GenerateTestData(NodeData nodeData)
        {
            foreach (var propType in nodeData.PropertyTypes)
            {
                var data = GetTestData(propType);
                if (data != null)
                    nodeData.SetDynamicRawData(propType, data);
            }
        }
        private object GetTestData(PropertyType propType)
        {
            if (propType.Name == "AspectData")
                return "<AspectData />";
            switch (propType.DataType)
            {
                case DataType.String: return "String " + Guid.NewGuid();
                case DataType.Text: return "Text value" + Guid.NewGuid();
                case DataType.Int: return Rng();
                case DataType.Currency: return (decimal)Rng();
                case DataType.DateTime: return DateTime.UtcNow;
                case DataType.Reference: return new[] {1, 2};
                case DataType.Binary:
                default:
                    return null;
            }
        }
        private Random _random = new Random();
        private int Rng()
        {
            return _random.Next(1, int.MaxValue);
        }

        [TestMethod]
        public void DP_AB_Create_TextProperty()
        {
            // TESTED: DataProvider2: InsertNodeAsync(NodeData nodeData, NodeSaveSettings settings);

            DPTest(() =>
            {
                var description = "text property value.";

                // ACTION-A
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Description = description;
                var nodeDataBeforeA = folderA.Data.Clone();
                folderA.Save();
                var nodeDataAfterA = folderA.Data.Clone();

                // ACTION-B
                DataStore.Enabled = true;
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Description = description;
                var nodeDataBeforeB = folderB.Data.Clone();
                folderB.Save();
                var nodeDataAfterB = folderB.Data.Clone();

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
                var folderA = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderA.Save();
                var fileA = new File(folderA) { Name = "File1" };
                fileA.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent));
                var nodeDataBeforeA = fileA.Data.Clone();
                fileA.Save();
                var nodeDataAfterA = fileA.Data.Clone();
                DistributedApplication.Cache.Reset();
                fileA = Node.Load<File>(fileA.Id);
                var reloadedFileContentA = RepositoryTools.GetStreamString(fileA.Binary.GetStream());

                // ACTION-B
                DataStore.Enabled = true;
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Save();
                var fileB = new File(folderB) { Name = "File1" };
                fileB.Binary.SetStream(RepositoryTools.GetStreamFromString(filecontent));
                var nodeDataBeforeB = fileB.Data.Clone();
                fileB.Save();
                var nodeDataAfterB = fileB.Data.Clone();
                var fileBId = fileB.Id;
                DistributedApplication.Cache.Reset();
                fileB = Node.Load<File>(fileBId);
                var reloadedFileContentB = RepositoryTools.GetStreamString(fileB.Binary.GetStream());

                // ASSERT
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
                var nodeDataBeforeA = folderA.Data.Clone();
                folderA.Save();
                var nodeDataAfterA = folderA.Data.Clone();

                // PROVIDER-B
                DataStore.Enabled = true;
                DistributedApplication.Cache.Reset();
                var folderB = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folderB.Save();
                folderB = Node.Load<SystemFolder>(folderB.Id);
                folderB.Index++;
                var nodeDataBeforeB = folderB.Data.Clone();
                folderB.Save();
                var nodeDataAfterB = folderB.Data.Clone();

                // Check
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
                var nodeDataBeforeA = fileA.Data.Clone();
                fileA.Save();
                var nodeDataAfterA = fileA.Data.Clone();
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
                var nodeDataBeforeB = fileB.Data.Clone();
                fileB.Save();
                var nodeDataAfterB = fileB.Data.Clone();
                DistributedApplication.Cache.Reset();
                fileB = Node.Load<File>(fileB.Id);
                var reloadedFileContentB = RepositoryTools.GetStreamString(fileB.Binary.GetStream());

                // ASSERT
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
                var nodeDataBeforeA = fileA.Data.Clone();
                fileA.Save();
                var nodeDataAfterA = fileA.Data.Clone();
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
                var nodeDataBeforeB = fileB.Data.Clone();
                fileB.Save();
                var nodeDataAfterB = fileB.Data.Clone();
                DistributedApplication.Cache.Reset();
                fileB = Node.Load<File>(fileB.Id);
                var reloadedFileContentB = RepositoryTools.GetStreamString(fileB.Binary.GetStream());

                // ASSERT
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
                var nodeDataBeforeA = fileA.Data.Clone();
                fileA.CheckIn();
                var nodeDataAfterA = fileA.Data.Clone();
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
                var nodeDataBeforeB = fileB.Data.Clone();
                fileB.CheckIn();
                var nodeDataAfterB = fileB.Data.Clone();
                DistributedApplication.Cache.Reset();
                fileB = Node.Load<File>(fileB.Id);
                var reloadedFileContentB = RepositoryTools.GetStreamString(fileB.Binary.GetStream());

                // ASSERT
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
                var nodeDataBeforeA = fileA.Data.Clone();
                fileA.UndoCheckOut();
                PreloadAllProperties(fileA);
                var nodeDataAfterA = fileA.Data.Clone();
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
                var nodeDataBeforeB = fileB.Data.Clone();
                fileB.UndoCheckOut();
                PreloadAllProperties(fileB);
                var nodeDataAfterB = fileB.Data.Clone();
                DistributedApplication.Cache.Reset();
                fileB = Node.Load<File>(fileB.Id);
                var reloadedFileContentB = RepositoryTools.GetStreamString(fileB.Binary.GetStream());

                // ASSERT
                DataProviderChecker.Assert_AreEqual(nodeDataBeforeA, nodeDataBeforeB);
                DataProviderChecker.Assert_AreEqual(nodeDataAfterA, nodeDataAfterB);

                CheckDynamicDataByVersionId(fileA.VersionId);

                Assert.AreEqual(filecontent1, reloadedFileContentA);
                Assert.AreEqual(filecontent1, reloadedFileContentB);
            });
        }
        private void PreloadAllProperties(Node node)
        {
            var data = node.Data;
            var _ = node.Data.PropertyTypes.Select(p => data.GetDynamicRawData(p)).ToArray();
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
                    var longText1PropertyType = ActiveSchema.PropertyTypes["LongText1"];
                    var storedProps = db.Versions.First(x => x.VersionId == nodeB.VersionId).DynamicProperties;
                    var storedTextProp = db.LongTextProperties.First(x => x.VersionId == nodeB.VersionId &&
                                                                          x.PropertyTypeId == longText1PropertyType.Id);
                    Assert.AreEqual("ShortText value 1", storedProps["ShortText1"]);
                    Assert.AreEqual("LongText value 1", storedTextProp.Value);
                    Assert.AreEqual(42, storedProps["Integer1"]);
                    Assert.AreEqual(42.56m, storedProps["Number1"]);
                    Assert.AreEqual(new DateTime(1111, 11, 11), storedProps["DateTime1"]);
                    Assert.AreEqual($"{Repository.Root.Id},{folderB.Id}", ArrayToString((List<int>)storedProps["Reference1"]));

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
                    storedProps = db.Versions.First(x => x.VersionId == nodeB.VersionId).DynamicProperties;
                    storedTextProp = db.LongTextProperties.First(x => x.VersionId == nodeB.VersionId &&
                                                                          x.PropertyTypeId == longText1PropertyType.Id);
                    Assert.AreEqual("ShortText value 2", storedProps["ShortText1"]);
                    Assert.AreEqual("LongText value 2", storedTextProp.Value);
                    Assert.AreEqual(43, storedProps["Integer1"]);
                    Assert.AreEqual(42.099m, storedProps["Number1"]);
                    Assert.AreEqual(new DateTime(1111, 11, 22), storedProps["DateTime1"]);
                    Assert.AreEqual($"{folderB.Id}", ArrayToString((List<int>)storedProps["Reference1"]));

                    // ACTION-3 UPDATE-2
                    nodeB = Node.Load<GenericContent>(nodeB.Id);
                    // Remove existing references
                    nodeB.RemoveReference("Reference1", folderB);
                    nodeB.Save();

                    // ASSERT-3
                    storedProps = db.Versions.First(x => x.VersionId == nodeB.VersionId).DynamicProperties;
                    storedTextProp = db.LongTextProperties.First(x => x.VersionId == nodeB.VersionId &&
                                                                      x.PropertyTypeId == longText1PropertyType.Id);
                    Assert.AreEqual("ShortText value 2", storedProps["ShortText1"]);
                    Assert.AreEqual("LongText value 2", storedTextProp.Value);
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
                DataStore.Enabled = true;

                // Create a small subtree
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

        [TestMethod]
        public void DP_LoadChildren()
        {
            DPTest(() =>
            {
                DistributedApplication.Cache.Reset();
                var loadedA = Repository.Root.Children.Select(x=>x.Id.ToString()).ToArray();
                DataStore.Enabled = true;
                DistributedApplication.Cache.Reset();
                var loadedB = Repository.Root.Children.Select(x => x.Id.ToString()).ToArray();

                Assert.AreEqual(string.Join(",", loadedA), string.Join(",", loadedB));
            });
        }

        [TestMethod]
        public void DP_Move()
        {
            DPTest(() =>
            {
                DataStore.Enabled = true;

                // Create a small subtree
                var root = new SystemFolder(Repository.Root) { Name = "TestRoot" }; root.Save();
                var source = new SystemFolder(root) { Name = "Source" }; source.Save();
                var target = new SystemFolder(root) { Name = "Target" }; target.Save();
                var f1 = new SystemFolder(source) { Name = "F1" }; f1.Save();
                var f2 = new SystemFolder(source) { Name = "F2" }; f2.Save();
                var f3 = new SystemFolder(f1) { Name = "F3" }; f3.Save();
                var f4 = new SystemFolder(f1) { Name = "F4" }; f4.Save();

                // ACTION
                Node.Move(source.Path, target.Path);

                // ASSERT
                target = Node.Load<SystemFolder>(target.Id);
                source = Node.Load<SystemFolder>(source.Id);
                f1 = Node.Load<SystemFolder>(f1.Id);
                f2 = Node.Load<SystemFolder>(f2.Id);
                f3 = Node.Load<SystemFolder>(f3.Id);
                f4 = Node.Load<SystemFolder>(f4.Id);
                Assert.AreEqual("/Root/TestRoot", root.Path);
                Assert.AreEqual("/Root/TestRoot/Target", target.Path);
                Assert.AreEqual("/Root/TestRoot/Target/Source", source.Path);
                Assert.AreEqual("/Root/TestRoot/Target/Source/F1", f1.Path);
                Assert.AreEqual("/Root/TestRoot/Target/Source/F2", f2.Path);
                Assert.AreEqual("/Root/TestRoot/Target/Source/F1/F3", f3.Path);
                Assert.AreEqual("/Root/TestRoot/Target/Source/F1/F4", f4.Path);
            });
        }

        [TestMethod]
        public void DP_RefreshCacheAfterSave()
        {
            DPTest(() =>
            {
                DataStore.Enabled = true;

                var root = new SystemFolder(Repository.Root) { Name = "TestRoot" };

                // ACTION-1: Create
                root.Save();
                var nodeTimestamp1 = root.NodeTimestamp;
                var versionTimestamp1 = root.VersionTimestamp;

                // ASSERT-1: NodeData is in cache after creation
                var cacheKey1 = DataStore.GenerateNodeDataVersionIdCacheKey(root.VersionId);
                var item1 = DistributedApplication.Cache[cacheKey1];
                Assert.IsNotNull(item1);
                var cachedNodeData1 = item1 as NodeData;
                Assert.IsNotNull(cachedNodeData1);
                Assert.AreEqual(nodeTimestamp1, cachedNodeData1.NodeTimestamp);
                Assert.AreEqual(versionTimestamp1, cachedNodeData1.VersionTimestamp);

                // ACTION-2: Update
                root.Index++;
                root.Save();
                var nodeTimestamp2 = root.NodeTimestamp;
                var versionTimestamp2 = root.VersionTimestamp;

                // ASSERT-2: NodeData is refreshed in the cache after update,
                Assert.AreNotEqual(nodeTimestamp1, nodeTimestamp2);
                Assert.AreNotEqual(versionTimestamp1, versionTimestamp2);
                var cacheKey2 = DataStore.GenerateNodeDataVersionIdCacheKey(root.VersionId);
                if (cacheKey1 != cacheKey2)
                    Assert.Inconclusive("The test is invalid because the cache keys are not equal.");
                var item2 = DistributedApplication.Cache[cacheKey2];
                Assert.IsNotNull(item2);
                var cachedNodeData2 = item2 as NodeData;
                Assert.IsNotNull(cachedNodeData2);
                Assert.AreEqual(nodeTimestamp2, cachedNodeData2.NodeTimestamp);
                Assert.AreEqual(versionTimestamp2, cachedNodeData2.VersionTimestamp);
            });
        }

        [TestMethod]
        public void DP_LazyLoadedBigText()
        {
            DPTest(() =>
            {
                DataStore.Enabled = true;
                var nearlyLongText = new string('a', InMemoryDataProvider2.TextAlternationSizeLimit - 10);
                var longText = new string('c', InMemoryDataProvider2.TextAlternationSizeLimit + 10);
                var descriptionPropertyType = ActiveSchema.PropertyTypes["Description"];

                // ACTION-1a: Creation with text that shorter than the magic limit
                var root = new SystemFolder(Repository.Root) { Name = "TestRoot", Description = nearlyLongText };
                root.Save();
                // ACTION-1b: Load the node
                var loaded = DataStore.DataProvider.LoadNodesAsync(new[] {root.VersionId}).Result.First();
                var longTextProps = loaded.GetDynamicData(false).LongTextProperties;

                // ASSERT-1
                Assert.AreEqual("Description", longTextProps.Keys.First().Name);

                // ACTION-2a: Update text property value over the magic limit
                var doc = ((InMemoryDataProvider2) DataStore.DataProvider).DB.LongTextProperties //UNDONE:DB@@@@ Use abstract approach
                    .First(x => x.Value == nearlyLongText);
                doc.Value = longText;
                doc.Length = longText.Length;
                // ACTION-2b: Load the node
                loaded = DataStore.DataProvider.LoadNodesAsync(new[] { root.VersionId }).Result.First();
                longTextProps = loaded.GetDynamicData(false).LongTextProperties;

                // ASSERT-2
                Assert.AreEqual(0, longTextProps.Count);

                // ACTION-3: Load the property value
                DistributedApplication.Cache.Reset();
                root = Node.Load<SystemFolder>(root.Id);
                var lazyLoadedDescription = root.Description; // Loads the property value

                // ASSERT-3
                Assert.AreEqual(longText, lazyLoadedDescription);
            });
        }
        [TestMethod]
        public void DP_LazyLoadedBigTextVsCache()
        {
            DPTest(() =>
            {
                DataStore.Enabled = true;
                var nearlyLongText1 = new string('a', InMemoryDataProvider2.TextAlternationSizeLimit - 10);
                var nearlyLongText2 = new string('b', InMemoryDataProvider2.TextAlternationSizeLimit - 10);
                var longText = new string('c', InMemoryDataProvider2.TextAlternationSizeLimit + 10);
                var descriptionPropertyType = ActiveSchema.PropertyTypes["Description"];

                // ACTION-1: Creation with text that shorter than the magic limit
                var root = new SystemFolder(Repository.Root) { Name = "TestRoot", Description = nearlyLongText1 };
                root.Save();
                var cacheKey = DataStore.GenerateNodeDataVersionIdCacheKey(root.VersionId);

                // ASSERT-1: text property is in cache
                var cachedNodeData = (NodeData)DistributedApplication.Cache[cacheKey];
                Assert.IsTrue(cachedNodeData.IsShared);
                var longTextProperties = cachedNodeData.GetDynamicData(false).LongTextProperties;
                Assert.IsTrue(longTextProperties.ContainsKey(descriptionPropertyType));
                Assert.AreEqual(nearlyLongText1, (string)longTextProperties[descriptionPropertyType]);

                // ACTION-2: Update with text that shorter than the magic limit
                root = Node.Load<SystemFolder>(root.Id);
                root.Description = nearlyLongText2;
                root.Save();

                // ASSERT-2: text property is in cache
                cachedNodeData = (NodeData)DistributedApplication.Cache[cacheKey];
                Assert.IsTrue(cachedNodeData.IsShared);
                longTextProperties = cachedNodeData.GetDynamicData(false).LongTextProperties;
                Assert.IsTrue(longTextProperties.ContainsKey(descriptionPropertyType));
                Assert.AreEqual(nearlyLongText2, (string)longTextProperties[descriptionPropertyType]);

                // ACTION-3: Update with text that longer than the magic limit
                root = Node.Load<SystemFolder>(root.Id);
                root.Description = longText;
                root.Save();

                // ASSERT-3: text property is not in the cache
                cachedNodeData = (NodeData)DistributedApplication.Cache[cacheKey];
                Assert.IsTrue(cachedNodeData.IsShared);
                longTextProperties = cachedNodeData.GetDynamicData(false).LongTextProperties;
                Assert.IsFalse(longTextProperties.ContainsKey(descriptionPropertyType));

                // ACTION-4: Load the text property
                var loadedValue = root.Description;

                // ASSERT-4: Property is loaded and is in cache
                Assert.AreEqual(longText, loadedValue);
                cachedNodeData = (NodeData)DistributedApplication.Cache[cacheKey];
                Assert.IsTrue(cachedNodeData.IsShared);
                longTextProperties = cachedNodeData.GetDynamicData(false).LongTextProperties;
                Assert.IsTrue(longTextProperties.ContainsKey(descriptionPropertyType));
            });
        }

        [TestMethod]
        public void DP_LoadChildTypesToAllow()
        {
            DPTest(() =>
            {
                DataStore.Enabled = true;

                // Create a small subtree
                var root = new SystemFolder(Repository.Root) { Name = "TestRoot" }; root.Save();
                var site1 = new Site(root) { Name = "Site1" }; site1.Save();
                site1.AllowChildTypes(new[] { "Task" }); site1.Save();
                site1 = Node.Load<Site>(site1.Id);
                var folder1 = new Folder(site1) { Name = "Folder1" }; folder1.Save();
                var folder2 = new Folder(folder1) { Name = "Folder2" }; folder2.Save();
                var folder3 = new Folder(folder1) { Name = "Folder3" }; folder3.Save();
                var task1 = new Task(folder3) { Name = "Task1" }; task1.Save();
                var doclib1 = new ContentList(folder3, "DocumentLibrary") { Name = "Doclib1" }; doclib1.Save();
                var file1 = new File(doclib1) { Name = "File1" }; file1.Save();
                var systemFolder1 = new SystemFolder(doclib1) { Name = "SystemFolder1" }; systemFolder1.Save();
                var file2 = new File(systemFolder1) { Name = "File2" }; file2.Save();
                var memoList1 = new ContentList(folder1, "MemoList") { Name = "MemoList1" }; memoList1.Save();
                var site2 = new Site(root) { Name = "Site2" }; site2.Save();

                // ACTION
                var types = DataStore.LoadChildTypesToAllowAsync(folder1.Id).Result;

                // ASSERT
                var names = string.Join(", ", types.Select(x => x.Name).OrderBy(x => x));
                Assert.AreEqual("DocumentLibrary, Folder, MemoList, Task", names);
            });
        }
        [TestMethod]
        public async STT.Task DP_ContentListTypesInTree()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;
                ActiveSchema.Reset();

                // Precheck-1
                Assert.AreEqual(0, ActiveSchema.ContentListTypes.Count);

                // Creation
                var root = CreateFolder(Repository.Root, "TestRoot");
                var node = new ContentList(root) {Name = "Survey-1"};
                node.Save();

                // Precheck-2
                Assert.AreEqual(1, ActiveSchema.ContentListTypes.Count);

                // ACTION
                var result = await DataStore.DataProvider.GetContentListTypesInTreeAsync(root.Path);

                // ASSERT
                Assert.IsNotNull(result);
                Assert.AreEqual(1, result.Count);
                Assert.AreEqual(ActiveSchema.ContentListTypes[0].Id, result[0].Id);
            });
        }

        [TestMethod]
        public void DP_ForceDelete()
        {
            DPTest(() =>
            {
                DataStore.Enabled = true;

                var db = GetDb();
                var nodeCount = db.Nodes.Count;
                var versionCount = db.Versions.Count;
                var binPropCount = db.BinaryProperties.Count;
                var fileCount = db.Files.Count;

                // Create a small subtree
                var root = new SystemFolder(Repository.Root) {Name = "TestRoot"};
                root.Save();
                var f1 = new SystemFolder(root) {Name = "F1"};
                f1.Save();
                var f2 = new File(root) { Name = "F2" };
                f2.Binary.SetStream(RepositoryTools.GetStreamFromString("filecontent"));
                f2.Save();
                var f3 = new SystemFolder(f1) {Name = "F3"};
                f3.Save();
                var f4 = new File(root) { Name = "F4" };
                f4.Binary.SetStream(RepositoryTools.GetStreamFromString("filecontent"));
                f4.Save();

                // ACTION
                Node.ForceDelete(root.Path);

                // ASSERT
                Assert.IsNull(Node.Load<SystemFolder>(root.Id));
                Assert.IsNull(Node.Load<SystemFolder>(f1.Id));
                Assert.IsNull(Node.Load<File>(f2.Id));
                Assert.IsNull(Node.Load<SystemFolder>(f3.Id));
                Assert.IsNull(Node.Load<File>(f4.Id));
                Assert.AreEqual(nodeCount, db.Nodes.Count);
                Assert.AreEqual(versionCount, db.Versions.Count);
                Assert.AreEqual(binPropCount, db.BinaryProperties.Count);
                Assert.AreEqual(fileCount, db.Files.Count);
            });
        }
        [TestMethod]
        public void DP_DeleteDeleted()
        {
            DPTest(() =>
            {
                DataStore.Enabled = true;

                var folder = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folder.Save();
                folder = Node.Load<SystemFolder>(folder.Id);
                var nodeHeadData = folder.Data.GetNodeHeadData();
                Node.ForceDelete(folder.Path);

                // ACTION
                DataStore.DataProvider.DeleteNodeAsync(nodeHeadData);

                // ASSERT
                // Expectation: no exception was thrown
            });
        }

        [TestMethod]
        public void DP_GetVersionNumbers()
        {
            DPTest(() =>
            {
                DataStore.Enabled = false;

                // Old dataprovider
                var folderA = new SystemFolder(Repository.Root)
                {
                    Name = "Folder1",
                    VersioningMode = VersioningType.MajorAndMinor
                };
                folderA.Save();
                for (int j = 0; j < 3; j++)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        folderA.CheckOut();
                        folderA.Index++;
                        folderA.Save();
                        folderA.CheckIn();
                    }
                    folderA.Publish();
                }
                var allVersinsA1 = Node.GetVersionNumbers(folderA.Id);
                var allVersinsA2 = Node.GetVersionNumbers(folderA.Path);

                // New dataprovider
                DataStore.Enabled = true;

                var folderB = new SystemFolder(Repository.Root)
                {
                    Name = "Folder1",
                    VersioningMode = VersioningType.MajorAndMinor
                };
                folderB.Save();
                for (int j = 0; j < 3; j++)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        folderB.CheckOut();
                        folderB.Index++;
                        folderB.Save();
                        folderB.CheckIn();
                    }
                    folderB.Publish();
                }
                var allVersinsB1 = Node.GetVersionNumbers(folderA.Id);
                var allVersinsB2 = Node.GetVersionNumbers(folderA.Path);

                // Check
                Assert.AreEqual(10, allVersinsA1.Count);
                AssertSequenceEqual(allVersinsA1, allVersinsA2);
                AssertSequenceEqual(allVersinsA1, allVersinsB1);
                AssertSequenceEqual(allVersinsA2, allVersinsB2);
            });
        }
        [TestMethod]
        public async STT.Task DP_GetVersionNumbers_MissingNode()
        {
            await Test(async () =>
            {
                DataStore.Enabled = false;

                // ACTION
                var result = await DataStore.DataProvider.GetVersionNumbersAsync("/Root/Deleted");

                // ASSERT
                Assert.IsFalse(result.Any());
            });
        }

        [TestMethod]
        public async STT.Task DP_LoadBinaryPropertyValues()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;

                var root = CreateFolder(Repository.Root, "TestRoot");
                var file = CreateFile(root, "File-1", "File content.");

                var versionId = file.VersionId;
                var fileId = file.Binary.FileId;
                var propertyTypeId = file.Binary.PropertyType.Id;

                // ACTION-1: Load existing
                var result = await DataStore.DataProvider.LoadBinaryPropertyValueAsync(versionId, propertyTypeId);
                // ASSERT-1
                Assert.IsNotNull(result);

                // ACTION-2: Missing Binary
                result = await DataStore.DataProvider.LoadBinaryPropertyValueAsync(versionId, 999999);
                // ASSERT-2 (not loaded and no exceptin was thrown)
                Assert.IsNull(result);

                // ACTION-3: Staging
                await DataStore.DataProvider.SetFileStagingAsync(fileId, true);
                result = await DataStore.DataProvider.LoadBinaryPropertyValueAsync(versionId, propertyTypeId);
                // ASSERT-3 (not loaded and no exceptin was thrown)
                Assert.IsNull(result);

                // ACTION-4: Missing File (inconsistent but need to be handled)
                await DataStore.DataProvider.DeleteFileAsync(fileId);
                result = await DataStore.DataProvider.LoadBinaryPropertyValueAsync(versionId, propertyTypeId);
                // ASSERT-4 (not loaded and no exceptin was thrown)
                Assert.IsNull(result);
            });
        }


        [TestMethod]
        public void DP_NodeEnumerator()
        {
            DPTest(() =>
            {
                DataStore.Enabled = true;

                // Create a small subtree
                var root = new SystemFolder(Repository.Root) { Name = "TestRoot" }; root.Save();
                var f1 = new SystemFolder(root) { Name = "F1" }; f1.Save();
                var f2 = new SystemFolder(root) { Name = "F2" }; f2.Save();
                var f3 = new SystemFolder(f1) { Name = "F3" }; f3.Save();
                var f4 = new SystemFolder(f1) { Name = "F4" }; f4.Save();
                var f5 = new SystemFolder(f3) { Name = "F5" }; f5.Save();
                var f6 = new SystemFolder(f3) { Name = "F6" }; f6.Save();

                // ACTION
                // Use ExecutionHint.ForceRelationalEngine for a valid dataprovider test case.
                var result = NodeEnumerator.GetNodes(root.Path, ExecutionHint.ForceRelationalEngine);

                // ASSERT
                var names = string.Join(", ", result.Select(n => n.Name));
                // preorder tree-walking
                Assert.AreEqual("TestRoot, F1, F3, F5, F6, F4, F2", names);
            });
        }

        [TestMethod]
        public void DP_NameSuffix()
        {
            DPTest(() =>
            {
                DataStore.Enabled = true;

                // Create a small subtree
                var root = new SystemFolder(Repository.Root) { Name = "TestRoot" }; root.Save();
                var f1 = new SystemFolder(root) { Name = "folder(42)" }; f1.Save();

                // ACTION
                var newName = ContentNamingProvider.IncrementNameSuffixToLastName("folder(11)", f1.ParentId);

                // ASSERT
                Assert.AreEqual("folder(43)", newName);
            });
        }

        [TestMethod]
        public void DP_AB_TreeSize()
        {
            var fileContent = "File content.";
            void CreateStructure()
            {
                var root = CreateFolder(Repository.Root, "TestRoot");
                var file1 = CreateFile(root, "File1", fileContent);
                var file2 = CreateFile(root, "File2", fileContent);
                var folder1 = CreateFolder(root, "Folder1");
                var file3 = CreateFile(folder1, "File3", fileContent);
                var file4 = CreateFile(folder1, "File4", fileContent);
                file4.CheckOut();
            }

            DPTest(() =>
            {
                var testRootPath = "/Root/TestRoot";

                // ARRANGE-A
                DataStore.Enabled = false;
                CreateStructure();

                // ACTION-A
                var folderSizeA = Node.GetTreeSize(testRootPath, false);
                var treeSizeA = Node.GetTreeSize(testRootPath, true);

                // ARRANGE-B
                DataStore.Enabled = true;
                CreateStructure();

                // ACTION-B
                var folderSizeB = Node.GetTreeSize(testRootPath, false);
                var treeSizeB = Node.GetTreeSize(testRootPath, true);

                // ASSERT
                Assert.AreEqual(2 * (fileContent.Length + 3), folderSizeA);
                Assert.AreEqual(5 * (fileContent.Length + 3), treeSizeA);
                Assert.AreEqual(folderSizeA, folderSizeB);
                Assert.AreEqual(treeSizeA, treeSizeB);
            });
        }
        private SystemFolder CreateFolder(Node parent, string name)
        {
            var folder = new SystemFolder(parent) { Name = name };
            folder.Save();
            return folder;
        }
        private File CreateFile(Node parent, string name, string fileContent)
        {
            var file = new File(parent) { Name = name };
            file.Binary.SetStream(RepositoryTools.GetStreamFromString(fileContent));
            file.Save();
            return file;
        }

        /* ================================================================================================== NodeQuery */

        [TestMethod]
        public async STT.Task DP_NodeQuery_InstanceCount()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;

                var dp = DataStore.DataProvider;
;
                var expectedFolderCount = CreateSafeContentQuery("+Type:Folder .COUNTONLY").Execute().Count;
                var expectedSystemFolderCount = CreateSafeContentQuery("+Type:SystemFolder .COUNTONLY").Execute().Count;
                var expectedAggregated = expectedFolderCount + expectedSystemFolderCount;

                var folderTypeTypeId = ActiveSchema.NodeTypes["Folder"].Id;
                var systemFolderTypeTypeId = ActiveSchema.NodeTypes["SystemFolder"].Id;

                // ACTION-1
                var actualFolderCount1 = await dp.InstanceCountAsync(new[] { folderTypeTypeId });
                var actualSystemFolderCount1 = await dp.InstanceCountAsync(new[] { systemFolderTypeTypeId });
                var actualAggregated1 = await dp.InstanceCountAsync(new[] { folderTypeTypeId, systemFolderTypeTypeId });

                // ASSERT
                Assert.AreEqual(expectedFolderCount, actualFolderCount1);
                Assert.AreEqual(expectedSystemFolderCount, actualSystemFolderCount1);
                Assert.AreEqual(expectedAggregated, actualAggregated1);

                // add a systemFolder to check inheritance in counts
                var folder = new SystemFolder(Repository.Root){Name ="Folder-1"};
                folder.Save();

                // ACTION-1
                var actualFolderCount2 = await dp.InstanceCountAsync(new[] { folderTypeTypeId });
                var actualSystemFolderCount2 = await dp.InstanceCountAsync(new[] { systemFolderTypeTypeId });
                var actualAggregated2 = await dp.InstanceCountAsync(new[] { folderTypeTypeId, systemFolderTypeTypeId });

                // ASSERT
                Assert.AreEqual(expectedFolderCount, actualFolderCount2);
                Assert.AreEqual(expectedSystemFolderCount + 1, actualSystemFolderCount2);
                Assert.AreEqual(expectedAggregated + 1, actualAggregated2);

            });
        }
        [TestMethod]
        public async STT.Task DP_NodeQuery_ChildrenIdentfiers()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;

                var expected = CreateSafeContentQuery("+InFolder:/Root").Execute().Identifiers;

                // ACTION
                var result = await DataStore.DataProvider.GetChildrenIdentfiersAsync(Repository.Root.Id);

                // ASSERT
                AssertSequenceEqual(expected.OrderBy(x => x), result.OrderBy(x => x));
            });
        }

        [TestMethod]
        public async STT.Task DP_NodeQuery_QueryNodesByTypeAndPathAndName()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;

                var r = new SystemFolder(Repository.Root){Name="R"}; r.Save();
                var ra = new Folder(r) { Name = "A" }; ra.Save();
                var raf = new Folder(ra) { Name = "F" }; raf.Save();
                var rafa = new Folder(raf) { Name = "A" }; rafa.Save();
                var rafb = new Folder(raf) { Name = "B" }; rafb.Save();
                var ras = new SystemFolder(ra) { Name = "S" }; ras.Save();
                var rasa = new SystemFolder(ras) { Name = "A" }; rasa.Save();
                var rasb = new SystemFolder(ras) { Name = "B" }; rasb.Save();
                var rb = new Folder(r) { Name = "B" }; rb.Save();
                var rbf = new Folder(rb) { Name = "F" }; rbf.Save();
                var rbfa = new Folder(rbf) { Name = "A" }; rbfa.Save();
                var rbfb = new Folder(rbf) { Name = "B" }; rbfb.Save();
                var rbs = new SystemFolder(rb) { Name = "S" }; rbs.Save();
                var rbsa = new SystemFolder(rbs) { Name = "A" }; rbsa.Save();
                var rbsb = new SystemFolder(rbs) { Name = "B" }; rbsb.Save();
                var rc = new Folder(r) { Name = "C" }; rc.Save();
                var rcf = new Folder(rc) { Name = "F" }; rcf.Save();
                var rcfa = new Folder(rcf) { Name = "A" }; rcfa.Save();
                var rcfb = new Folder(rcf) { Name = "B" }; rcfb.Save();
                var rcs = new SystemFolder(rc) { Name = "S" }; rcs.Save();
                var rcsa = new SystemFolder(rcs) { Name = "A" }; rcsa.Save();
                var rcsb = new SystemFolder(rcs) { Name = "B" }; rcsb.Save();

                var typeF = ActiveSchema.NodeTypes["Folder"].Id;
                var typeS = ActiveSchema.NodeTypes["SystemFolder"].Id;

                var dp = DataStore.DataProvider;

                // ACTION-1 (type: 1, path: 1, name: -)
                var nodeTypeIds = new[] { typeF };
                var pathStart = new[] { "/Root/R/A" };
                var result = await dp.QueryNodesByTypeAndPathAndNameAsync(nodeTypeIds, pathStart, true, null);
                // ASSERT-1
                var expected = CreateSafeContentQuery("+Type:Folder +InTree:/Root/R/A .SORT:Path")
                    .Execute().Identifiers.Skip(1).ToArray();
                Assert.AreEqual(3, expected.Length);
                AssertSequenceEqual(expected, result);

                // ACTION-2 (type: 2, path: 1, name: -)
                nodeTypeIds = new[] { typeF, typeS };
                pathStart = new[] { "/Root/R/A" };
                result = await dp.QueryNodesByTypeAndPathAndNameAsync(nodeTypeIds, pathStart, true, null);
                // ASSERT-2
                expected = CreateSafeContentQuery("+Type:(Folder SystemFolder) +InTree:/Root/R/A .SORT:Path")
                    .Execute().Identifiers.Skip(1).ToArray();
                Assert.AreEqual(6, expected.Length);
                AssertSequenceEqual(expected, result);

                // ACTION-3 (type: 1, path: 2, name: -)
                nodeTypeIds = new[] { typeF };
                pathStart = new[] { "/Root/R/A", "/Root/R/B" };
                result = await dp.QueryNodesByTypeAndPathAndNameAsync(nodeTypeIds, pathStart, true, null);
                // ASSERT-3
                expected = CreateSafeContentQuery("+Type:Folder +InTree:/Root/R/A .SORT:Path")
                    .Execute().Identifiers.Skip(1)
                    .Union(CreateSafeContentQuery("+Type:Folder +InTree:/Root/R/B .SORT:Path")
                        .Execute().Identifiers.Skip(1)).ToArray();
                Assert.AreEqual(6, expected.Length);
                AssertSequenceEqual(expected, result);

                // ACTION-4 (type: -, path: 1, name: A)
                pathStart = new[] { "/Root/R" };
                result = await dp.QueryNodesByTypeAndPathAndNameAsync(null, pathStart, true, "A");
                // ASSERT-4
                expected = CreateSafeContentQuery("+Name:A +InTree:/Root/R .SORT:Path").Execute().Identifiers.ToArray();
                Assert.AreEqual(7, expected.Length);
                AssertSequenceEqual(expected, result);
            });
        }
        [TestMethod]
        public async STT.Task DP_NodeQuery_QueryNodesByTypeAndPathAndProperty()
        {
            Assert.Inconclusive();
        }
        [TestMethod]
        public async STT.Task DP_NodeQuery_QueryNodesByReferenceAndType()
        {
            Assert.Inconclusive();
        }


        /* ================================================================================================== TreeLock */

        [TestMethod]
        public void DP_LoadEntityTree()
        {
            DPTest(() =>
            {
                // ACTION
                var treeData = DataStore.LoadEntityTreeAsync().Result;

                // ASSERT check the right ordering: every node follows it's parent node.
                var tree = new Dictionary<int, EntityTreeNodeData>();
                foreach (var node in treeData)
                {
                    if (node.ParentId != 0)
                        if(!tree.ContainsKey(node.ParentId))
                            Assert.Fail($"The parent is not yet loaded. Id: {node.Id}, ParentId: {node.ParentId}");
                    tree.Add(node.Id, node);
                }
            });
        }

        /* ================================================================================================== TreeLock */

        [TestMethod]
        public async STT.Task DP_TreeLock()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;

                var dp = DataStore.DataProvider;
                var path = "/Root/Folder-1";
                var childPath = "/root/folder-1/folder-2";
                var anotherPath = "/Root/Folder-2";

                // Pre check: there is no lock
                var tlocks = await dp.LoadAllTreeLocksAsync();
                Assert.AreEqual(0, tlocks.Count);

                // ACTION: create a lock
                var tlockId = await dp.AcquireTreeLockAsync(path);

                // Check: there is one lock ant it matches
                tlocks = await dp.LoadAllTreeLocksAsync();
                Assert.AreEqual(1, tlocks.Count);
                Assert.AreEqual(tlockId, tlocks.First().Key);
                Assert.AreEqual(path, tlocks.First().Value);

                // Check: path and subpath are locked
                Assert.IsTrue(await dp.IsTreeLockedAsync(path));
                Assert.IsTrue(await dp.IsTreeLockedAsync(childPath));

                // Check: outer path is not locked
                Assert.IsFalse(await dp.IsTreeLockedAsync(anotherPath));

                // ACTION: try to create a lock fot a subpath
                var childLlockId = await dp.AcquireTreeLockAsync(childPath);

                // Check: subPath cannot be locked
                Assert.AreEqual(0, childLlockId);

                // Check: there is still only one lock
                tlocks = await dp.LoadAllTreeLocksAsync();
                Assert.AreEqual(1, tlocks.Count);
                Assert.AreEqual(tlockId, tlocks.First().Key);
                Assert.AreEqual(path, tlocks.First().Value);

                // ACTION: Release the lock
                await dp.ReleaseTreeLockAsync(new[] {tlockId});

                // Check: there is no lock
                tlocks = await dp.LoadAllTreeLocksAsync();
                Assert.AreEqual(0, tlocks.Count);

                // Check: path and subpath are not locked
                Assert.IsFalse(await dp.IsTreeLockedAsync(path));
                Assert.IsFalse(await dp.IsTreeLockedAsync(childPath));

            });
        }

        /* ================================================================================================== IndexDocument */

        [TestMethod]
        public void DP_AB_LoadIndexDocuments()
        {
            var fileContent = "File content.";
            void CreateStructure()
            {
                var root = CreateFolder(Repository.Root, "TestRoot");
                var file1 = CreateFile(root, "File1", fileContent);
                var file2 = CreateFile(root, "File2", fileContent);
                var folder1 = CreateFolder(root, "Folder1");
                var file3 = CreateFile(folder1, "File3", fileContent);
                var file4 = CreateFile(folder1, "File4", fileContent);
                file4.CheckOut();
                var folder2 = CreateFolder(folder1, "Folder2");
                var fileA5 = CreateFile(folder2, "File5", fileContent);
                var fileA6 = CreateFile(folder2, "File6", fileContent);
            }

            DPTest(() =>
            {
                var testRootPath = "/Root/TestRoot";
                SystemFolder testRoot;
                var fileNodeType = ActiveSchema.NodeTypes["File"];
                var systemFolderType = ActiveSchema.NodeTypes["SystemFolder"];

                // ARRANGE-A
                DataStore.Enabled = false;
                CreateStructure();
                testRoot = Node.Load<SystemFolder>(testRootPath);
                var testRootChildren = testRoot.Children.ToArray();

                // ACTION-A
                // #1 One version
                var indxDataA1 = SearchManager.LoadIndexDocumentByVersionId(testRoot.VersionId);
                // #2 More versions
                var indxDataA2 = SearchManager.LoadIndexDocumentByVersionId(testRootChildren.Select(x => x.VersionId).ToArray()).ToArray();
                // #3 Subtree all
                var indxDataA3 = SearchManager.LoadIndexDocumentsByPath(testRootPath, new int[0]).ToArray();
                // #4 Only folders
                var indxDataA4 = SearchManager.LoadIndexDocumentsByPath(testRootPath, new[] { fileNodeType.Id }).ToArray();
                // #4 Only folders
                var indxDataA5 = SearchManager.LoadIndexDocumentsByPath(testRootPath, new[] { systemFolderType.Id }).ToArray();

                // ARRANGE-A
                DataStore.Enabled = true;
                CreateStructure();
                testRoot = Node.Load<SystemFolder>(testRootPath);
                testRootChildren = testRoot.Children.ToArray();

                // ACTION-A
                // #1 One version
                var indxDataB1 = SearchManager.LoadIndexDocumentByVersionId(testRoot.VersionId);
                // #2 More versions
                var indxDataB2 = SearchManager.LoadIndexDocumentByVersionId(testRootChildren.Select(x => x.VersionId).ToArray()).ToArray();
                // #3 Subtree all
                var indxDataB3 = SearchManager.LoadIndexDocumentsByPath(testRootPath, new int[0]).ToArray();
                // #4 Only folders
                var indxDataB4 = SearchManager.LoadIndexDocumentsByPath(testRootPath, new[] { fileNodeType.Id }).ToArray();
                // #4 Only folders
                var indxDataB5 = SearchManager.LoadIndexDocumentsByPath(testRootPath, new[] { systemFolderType.Id }).ToArray();

                // ASSERT
                // #1 One version
                Assert.AreEqual(testRootPath, indxDataA1.Path);
                Assert.AreEqual(indxDataA1.Path, indxDataB1.Path);
                // #2 More versions
                Assert.AreEqual(3, indxDataA2.Length);
                Assert.AreEqual(indxDataA2.Length, indxDataB2.Length);
                // #3 Subtree all
                Assert.AreEqual(10, indxDataA3.Length);
                Assert.AreEqual(indxDataA3.Length, indxDataB3.Length);
                // #4 Only folders
                Assert.AreEqual(3, indxDataA4.Length);
                Assert.AreEqual(indxDataA4.Length, indxDataB4.Length);
                // #4 Only files
                Assert.AreEqual(7, indxDataA5.Length);
                Assert.AreEqual(indxDataA5.Length, indxDataB5.Length);
            });
        }
        [TestMethod]
        public async STT.Task DP_SaveIndexDocumentById()
        {
            await Test( async () =>
            {
                DataStore.Enabled = true;
                var node = CreateFolder(Repository.Root, "Folder-1");
                var versionIds = new[] {node.VersionId};
                var loadResult = await DataStore.DataProvider.LoadIndexDocumentsAsync(versionIds);
                var docData = loadResult.First();

                // ACTION
                docData.IndexDocument.Add(
                    new IndexField("TestField", "TestValue",
                        IndexingMode.Default, IndexStoringMode.Default, IndexTermVector.Default));
                await DataStore.DataProvider.SaveIndexDocumentAsync(node.VersionId, docData.IndexDocument);

                // ASSERT (check additional field existence)
                loadResult = await DataStore.DataProvider.LoadIndexDocumentsAsync(versionIds);
                docData = loadResult.First();
                var testField = docData.IndexDocument.FirstOrDefault(x => x.Name == "TestField");
                Assert.IsNotNull(testField);
                Assert.AreEqual("TestValue", testField.StringValue);
            });
        }


        /* ================================================================================================== IndexingActivities */

        [TestMethod]
        public async STT.Task DP_IndexingActivity_GetLastIndexingActivityId()
        {
            await IndexingActivityTest(async (firstId, lastId) =>
            {
                var result = await DataStore.DataProvider.GetLastIndexingActivityIdAsync();
                Assert.AreEqual(lastId, result);
            });
        }
        [TestMethod]
        public async STT.Task DP_IndexingActivity_LoadIndexingActivities_Page()
        {
            await IndexingActivityTest(async (firstId, lastId) =>
            {
                var from = lastId - 10;
                var to = lastId;
                var count = 5;
                var factory = new TestIndexingActivityFactory();

                // ACTION
                var result = await DataStore.DataProvider.LoadIndexingActivitiesAsync(from, to, count, false, factory);

                // ASSERT
                Assert.AreEqual(5, result.Length);
                Assert.AreEqual(100, result[0].NodeId);
                Assert.AreEqual(101, result[1].NodeId);
                Assert.AreEqual(102, result[2].NodeId);
                Assert.AreEqual(103, result[3].NodeId);
                Assert.AreEqual(104, result[4].NodeId);
                Assert.IsFalse(result[0].IsUnprocessedActivity);
                Assert.IsFalse(result[1].IsUnprocessedActivity);
                Assert.IsFalse(result[2].IsUnprocessedActivity);
                Assert.IsFalse(result[3].IsUnprocessedActivity);
                Assert.IsFalse(result[4].IsUnprocessedActivity);
            });
        }
        [TestMethod]
        public async STT.Task DP_IndexingActivity_LoadIndexingActivities_PageUnprocessed()
        {
            await IndexingActivityTest(async (firstId, lastId) =>
            {
                var from = lastId - 10;
                var to = lastId;
                var count = 5;
                var factory = new TestIndexingActivityFactory();

                // ACTION
                var result = await DataStore.DataProvider.LoadIndexingActivitiesAsync(from, to, count, true, factory);

                // ASSERT
                Assert.AreEqual(5, result.Length);
                Assert.AreEqual(100, result[0].NodeId);
                Assert.AreEqual(101, result[1].NodeId);
                Assert.AreEqual(102, result[2].NodeId);
                Assert.AreEqual(103, result[3].NodeId);
                Assert.AreEqual(104, result[4].NodeId);
                Assert.IsTrue(result[0].IsUnprocessedActivity);
                Assert.IsTrue(result[1].IsUnprocessedActivity);
                Assert.IsTrue(result[2].IsUnprocessedActivity);
                Assert.IsTrue(result[3].IsUnprocessedActivity);
                Assert.IsTrue(result[4].IsUnprocessedActivity);
            });
        }
        [TestMethod]
        public async STT.Task DP_IndexingActivity_LoadIndexingActivities_Gaps()
        {
            await IndexingActivityTest(async (firstId, lastId) =>
            {
                var gaps = new[] {lastId - 10, lastId - 9, lastId - 5};
                var factory = new TestIndexingActivityFactory();

                // ACTION
                var result = await DataStore.DataProvider.LoadIndexingActivitiesAsync(gaps, false, factory);

                // ASSERT
                Assert.AreEqual(3, result.Length);
                Assert.AreEqual(100, result[0].NodeId);
                Assert.AreEqual(101, result[1].NodeId);
                Assert.AreEqual(105, result[2].NodeId);
            });
        }
        [TestMethod]
        public async STT.Task DP_IndexingActivity_LoadIndexingActivities_Executable()
        {
            await IndexingActivityTest(async (firstId, lastId) =>
            {
                var factory = new TestIndexingActivityFactory();
                var timeout = 120;
                var waitingActivityIds = new[] {firstId, firstId + 1, firstId + 2, firstId + 3, firstId + 4, firstId + 5 };

                // ACTION
                var result = await DataStore.DataProvider.LoadExecutableIndexingActivitiesAsync(factory, 10, timeout, waitingActivityIds);

                // ASSERT
                Assert.AreEqual(10, result.Activities.Length);
                Assert.AreEqual(45, result.Activities[0].NodeId);
                Assert.AreEqual(46, result.Activities[1].NodeId);
                Assert.AreEqual(50, result.Activities[2].NodeId);
                Assert.AreEqual(51, result.Activities[3].NodeId);
                Assert.AreEqual(52, result.Activities[4].NodeId);
                Assert.AreEqual(53, result.Activities[5].NodeId);
                Assert.AreEqual(55, result.Activities[6].NodeId);
                Assert.AreEqual(100, result.Activities[7].NodeId);
                Assert.AreEqual(101, result.Activities[8].NodeId);
                Assert.AreEqual(102, result.Activities[9].NodeId);

                Assert.AreEqual(3, result.FinishedActivitiyIds.Length);
                Assert.AreEqual(1, result.FinishedActivitiyIds[0]);
                Assert.AreEqual(2, result.FinishedActivitiyIds[1]);
                Assert.AreEqual(3, result.FinishedActivitiyIds[2]);
            });
        }
        [TestMethod]
        public async STT.Task DP_IndexingActivity_UpdateRunningState()
        {
            await IndexingActivityTest(async (firstId, lastId) =>
            {
                var gaps = new[] { lastId - 10, lastId - 9, lastId - 8 };
                var factory = new TestIndexingActivityFactory();
                var before = await DataStore.DataProvider.LoadIndexingActivitiesAsync(gaps, false, factory);
                Assert.AreEqual(IndexingActivityRunningState.Waiting, before[0].RunningState);
                Assert.AreEqual(IndexingActivityRunningState.Waiting, before[1].RunningState);
                Assert.AreEqual(IndexingActivityRunningState.Waiting, before[2].RunningState);

                // ACTION
                await DataStore.DataProvider.UpdateIndexingActivityRunningStateAsync(gaps[0], IndexingActivityRunningState.Done);
                await DataStore.DataProvider.UpdateIndexingActivityRunningStateAsync(gaps[1], IndexingActivityRunningState.Running);

                // ASSERT
                var after = await DataStore.DataProvider.LoadIndexingActivitiesAsync(gaps, false, factory);
                Assert.AreEqual(IndexingActivityRunningState.Done, after[0].RunningState);
                Assert.AreEqual(IndexingActivityRunningState.Running, after[1].RunningState);
                Assert.AreEqual(IndexingActivityRunningState.Waiting, after[2].RunningState);
            });
        }
        [TestMethod]
        public async STT.Task DP_IndexingActivity_RefreshLockTime()
        {
            await IndexingActivityTest(async (firstId, lastId) =>
            {
                var startTime = DateTime.UtcNow;

                var activityIds = new[] { firstId + 5, firstId + 6 };
                var factory = new TestIndexingActivityFactory();
                var before = await DataStore.DataProvider.LoadIndexingActivitiesAsync(activityIds, false, factory);
                Assert.AreEqual(47, before[0].NodeId);
                Assert.AreEqual(48, before[1].NodeId);

                // ACTION
                await DataStore.DataProvider.RefreshIndexingActivityLockTimeAsync(activityIds);

                // ASSERT
                var after = await DataStore.DataProvider.LoadIndexingActivitiesAsync(activityIds, false, factory);
                Assert.AreEqual(47, before[0].NodeId);
                Assert.AreEqual(48, before[1].NodeId);
                Assert.IsTrue(after[0].LockTime >= startTime);
                Assert.IsTrue(after[1].LockTime >= startTime);
            });
        }
        [TestMethod]
        public async STT.Task DP_IndexingActivity_DeleteFinished()
        {
            await IndexingActivityTest(async (firstId, lastId) =>
            {
                // ACTION
                await DataStore.DataProvider.DeleteFinishedIndexingActivitiesAsync();

                // ASSERT
                var result = await DataStore.DataProvider.LoadIndexingActivitiesAsync(firstId, lastId, 100, false, new TestIndexingActivityFactory());
                Assert.AreEqual(lastId - firstId - 2, result.Length);
                Assert.AreEqual(firstId + 3, result.First().Id);
            });
        }
        [TestMethod]
        public async STT.Task DP_IndexingActivity_LoadFull()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;

                await DataStore.DataProvider.DeleteAllIndexingActivitiesAsync();
                var node = CreateFolder(Repository.Root, "Folder-1");

                // ACTION
                var result = await DataStore.DataProvider.LoadIndexingActivitiesAsync(0,1000,1000,false, new TestIndexingActivityFactory());

                // ASSERT (IndexDocument loaded)
                Assert.AreEqual(1, result.Length);
                Assert.AreEqual(node.VersionId, result[0].IndexDocumentData.IndexDocument.VersionId);
            });
        }

        #region Tools for IndexingActivities
        private async STT.Task IndexingActivityTest(Func<int, int, STT.Task> callback)
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;

                var now = DateTime.UtcNow;
                var firstId = await CreateActivityAsync(42, "/R/42", "Done");                           // 1
                              await CreateActivityAsync(43, "/R/43", "Done");                           // 2
                              await CreateActivityAsync(44, "/R/44", "Done");                           // 3
                              await CreateActivityAsync(45, "/R/45", "Running", now.AddMinutes(-2.1));  // 4
                              await CreateActivityAsync(46, "/R/46", "Running", now.AddMinutes(-2.0));  // 5
                              await CreateActivityAsync(47, "/R/47", "Running", now.AddMinutes(-1.9));  // 6 skip
                              await CreateActivityAsync(48, "/R/48", "Running", now.AddMinutes(-1.8));  // 7 skip
                              await CreateActivityAsync(50, "/R/50", "Waiting");                        // 8
                              await CreateActivityAsync(51, "/R/51", "Waiting");                        // 9
                              await CreateActivityAsync(52, "/R/52", "Waiting");                        // 10
                              await CreateActivityAsync(52, "/R/525", "Waiting");                       // 11 skip
                              await CreateActivityAsync(53, "/R/A", "Waiting");                         // 12
                              await CreateActivityAsync(54, "/R/A/A", "Waiting");                       // 13 skip
                              await CreateActivityAsync(55, "/R/B/B", "Waiting");                       // 14
                              await CreateActivityAsync(56, "/R/B", "Waiting");                         // 15 skip
                              await CreateActivityAsync(57, "/R/B", "Waiting");                         // 16 skip
                              await CreateActivityAsync(100, "/R/100", "Waiting");                      // 17
                              await CreateActivityAsync(101, "/R/101", "Waiting");                      // 18
                              await CreateActivityAsync(102, "/R/102", "Waiting");                      // 19
                              await CreateActivityAsync(103, "/R/103", "Waiting");                      // 20
                              await CreateActivityAsync(104, "/R/104", "Waiting");                      // 21
                              await CreateActivityAsync(105, "/R/105", "Waiting");                      // 22
                              await CreateActivityAsync(106, "/R/106", "Waiting");                      // 23
                              await CreateActivityAsync(107, "/R/107", "Waiting");                      // 24
                              await CreateActivityAsync(108, "/R/108", "Waiting");                      // 25
                              await CreateActivityAsync(109, "/R/109", "Waiting");                      // 26
                var lastId =  await CreateActivityAsync(110, "/R/110", "Waiting");                      // 27

                await callback(firstId, lastId);
            });
        }
        private STT.Task<int> CreateActivityAsync(int nodeId, string path, string runningState, DateTime? lockTime = null)
        {
            var activity = new TestIndexingActivity(nodeId, path, runningState, lockTime);
            DataStore.DataProvider.RegisterIndexingActivityAsync(activity);
            return STT.Task.FromResult(activity.Id);
        }
        private class TestIndexingActivityFactory : IIndexingActivityFactory
        {
            public IIndexingActivity CreateActivity(IndexingActivityType activityType)
            {
                return new TestIndexingActivity();
            }
        }
        private class TestIndexingActivity : IIndexingActivity
        {
            public TestIndexingActivity() { }
            public TestIndexingActivity(int nodeId, string path, string runningState, DateTime? lockTime = null)
            {
                NodeId = nodeId;
                Path = path;
                RunningState =
                    (IndexingActivityRunningState) Enum.Parse(typeof(IndexingActivityRunningState), runningState, true);
                LockTime = lockTime;
            }
            public int Id { get; set; }
            public IndexingActivityType ActivityType { get; set; } = IndexingActivityType.AddDocument;
            public DateTime CreationDate { get; set; } = DateTime.UtcNow.AddMinutes(-2);
            public IndexingActivityRunningState RunningState { get; set; }
            public DateTime? LockTime { get; set; }
            public int NodeId { get; set; }
            public int VersionId { get; set; }
            public string Path { get; set; }
            public long? VersionTimestamp { get; set; }
            public IndexDocumentData IndexDocumentData { get; set; }
            public bool FromDatabase { get; set; }
            public bool IsUnprocessedActivity { get; set; }
            public string Extension { get; set; }
        }
        #endregion

        /* ================================================================================================== Nodes */

        [TestMethod]
        public async STT.Task DP_CopyAndUpdateNode_Rename()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;
                var node = new SystemFolder(Repository.Root) {Name = "Folder1", Index = 42};
                node.Save();
                var childNode = CreateFolder(node, "Folder-2");
                var version1 = node.Version.ToString();
                var versionId1 = node.VersionId;
                node.CheckOut();
                var versionId2 = node.VersionId;
                var originalPath = node.Path;

                node = Node.Load<SystemFolder>(node.Id);
                node.Index++;
                node.Name = "Folder1-RENAMED";
                node.Data.Path = RepositoryPath.Combine(node.ParentPath, node.Name); // ApplySettings
                node.Version = VersionNumber.Parse(version1); // ApplySettings
                var nodeData = node.Data;
                var nodeHeadData = nodeData.GetNodeHeadData();
                var versionData = nodeData.GetVersionData();
                var dynamicData = nodeData.GetDynamicData(false);
                var versionIdsToDelete = new[] {versionId2};
                var expectedVersionId = versionId1;

                // ACTION: simulate a modification, rename and CheckIn on a checked-out, not-versioned node (V2.0.L -> V1.0.A).
                await DataStore.DataProvider
                    .CopyAndUpdateNodeAsync(nodeHeadData, versionData, dynamicData, versionIdsToDelete,
                        expectedVersionId, originalPath);

                // ASSERT
                DistributedApplication.Cache.Reset();
                var reloaded = Node.Load<SystemFolder>(node.Id);
                Assert.AreEqual("Folder1-RENAMED", reloaded.Name);
                reloaded = Node.Load<SystemFolder>(childNode.Id);
                Assert.AreEqual("/Root/Folder1-RENAMED/Folder-2", reloaded.Path);
            });
        }

        [TestMethod]
        public async STT.Task DP_LoadNodes()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;

                var expected = new[] { Repository.Root.VersionId, User.Administrator.VersionId };
                var versionIds = new[] { Repository.Root.VersionId, 999999, User.Administrator.VersionId };

                // ACTION
                var loadResult = await DataStore.DataProvider.LoadNodesAsync(versionIds);

                // ASSERT
                var actual = loadResult.Select(x => x.VersionId);
                AssertSequenceEqual(expected, actual);
            });
        }

        [TestMethod]
        public async STT.Task DP_LoadNodeHeadByVersionId_Missing()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;

                // ACTION
                var result = await DataStore.DataProvider.LoadNodeHeadByVersionIdAsync(99999);

                // ASSERT (returns null instead of throw any exception)
                Assert.IsNull(result);
            });
        }

        [TestMethod]
        public async STT.Task DP_NodeAndVersion_CountsAndTimestamps()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;
                var dp = DataStore.DataProvider;

                // ACTIONS
                var allNodeCountBefore = await dp.GetNodeCountAsync(null);
                var allVersionCountBefore = await dp.GetVersionCountAsync(null);

                var node = CreateFolder(Repository.Root, "Folder-1");
                var child = CreateFolder(node, "Folder-2");
                child.CheckOut();

                var nodeCount = await dp.GetNodeCountAsync(node.Path);
                var versionCount = await dp.GetVersionCountAsync(node.Path);
                var allNodeCountAfter = await dp.GetNodeCountAsync(null);
                var allVersionCountAfter = await dp.GetVersionCountAsync(null);

                node = Node.Load<SystemFolder>(node.Id);
                var nodeTimeStamp = await dp.GetNodeTimestampAsync(node.Id);
                var versionTimeStamp = await dp.GetVersionTimestampAsync(node.VersionId);

                // ASSERTS
                Assert.AreEqual(allNodeCountBefore + 2, allNodeCountAfter);
                Assert.AreEqual(allVersionCountBefore + 3, allVersionCountAfter);
                Assert.AreEqual(2, nodeCount);
                Assert.AreEqual(3, versionCount);
                Assert.AreEqual(node.NodeTimestamp, nodeTimeStamp);
                Assert.AreEqual(node.VersionTimestamp, versionTimeStamp);
            });
        }

        /* ================================================================================================== Errors */

        [TestMethod]
        public async STT.Task DP_Error_InsertNode_AlreadyExists()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;
                var newNode = new SystemFolder(Repository.Root) { Name = "Folder1" };
                newNode.Save();

                try
                {
                    var node = new SystemFolder(Repository.Root) { Name = "Folder1" };
                    var nodeData = node.Data;
                    nodeData.Path = RepositoryPath.Combine(node.ParentPath, node.Name);
                    var nodeHeadData = nodeData.GetNodeHeadData();
                    var versionData = nodeData.GetVersionData();
                    var dynamicData = nodeData.GetDynamicData(false);

                    // ACTION
                    await DataStore.DataProvider.InsertNodeAsync(nodeHeadData, versionData, dynamicData);
                    Assert.Fail("NodeAlreadyExistsException was not thrown.");
                }
                catch (NodeAlreadyExistsException)
                {
                    // ignored
                }
            });
        }

        [TestMethod]
        public async STT.Task DP_Error_UpdateNode_Deleted()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;

                try
                {
                    var node = Node.LoadNode(Identifiers.PortalRootId);
                    node.Index++;
                    var nodeData = node.Data;
                    var nodeHeadData = nodeData.GetNodeHeadData();
                    var versionData = nodeData.GetVersionData();
                    var dynamicData = nodeData.GetDynamicData(false);
                    var versionIdsToDelete = new int[0];

                    // ACTION
                    nodeHeadData.NodeId = 99999;
                    await DataStore.DataProvider.UpdateNodeAsync(nodeHeadData, versionData, dynamicData, versionIdsToDelete);
                    Assert.Fail("ContentNotFoundException was not thrown.");
                }
                catch (ContentNotFoundException)
                {
                    // ignored
                }
            });
        }
        [TestMethod]
        public async STT.Task DP_Error_UpdateNode_MissingVersion()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;
                var newNode =
                    new SystemFolder(Repository.Root) { Name = "Folder1", Index = 42 };
                newNode.Save();

                try
                {
                    var node = Node.Load<SystemFolder>(newNode.Id);
                    node.Index++;
                    var nodeData = node.Data;
                    var nodeHeadData = nodeData.GetNodeHeadData();
                    var versionData = nodeData.GetVersionData();
                    var dynamicData = nodeData.GetDynamicData(false);
                    var versionIdsToDelete = new int[0];

                    // ACTION
                    versionData.VersionId = 99999;
                    await DataStore.DataProvider.UpdateNodeAsync(nodeHeadData, versionData, dynamicData, versionIdsToDelete);
                    Assert.Fail("ContentNotFoundException was not thrown.");
                }
                catch (ContentNotFoundException)
                {
                    // ignored
                }
            });
        }
        [TestMethod]
        public async STT.Task DP_Error_UpdateNode_OutOfDate()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;
                var newNode =
                    new SystemFolder(Repository.Root) { Name = "Folder1", Index = 42 };
                newNode.Save();

                try
                {
                    var node = Node.Load<SystemFolder>(newNode.Id);
                    node.Index++;
                    var nodeData = node.Data;
                    var nodeHeadData = nodeData.GetNodeHeadData();
                    var versionData = nodeData.GetVersionData();
                    var dynamicData = nodeData.GetDynamicData(false);
                    var versionIdsToDelete = new int[0];

                    // ACTION
                    nodeHeadData.Timestamp++;
                    await DataStore.DataProvider.UpdateNodeAsync(nodeHeadData, versionData, dynamicData, versionIdsToDelete);
                    Assert.Fail("NodeIsOutOfDateException was not thrown.");
                }
                catch (NodeIsOutOfDateException)
                {
                    // ignored
                }
            });
        }

        [TestMethod]
        public async STT.Task DP_Error_CopyAndUpdateNode_Deleted()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;
                var newNode = new SystemFolder(Repository.Root) { Name = "Folder1", Index = 42 };
                newNode.Save();

                try
                {
                    var node = Node.Load<SystemFolder>(newNode.Id);
                    node.Index++;
                    var nodeData = node.Data;
                    var nodeHeadData = nodeData.GetNodeHeadData();
                    var versionData = nodeData.GetVersionData();
                    var dynamicData = nodeData.GetDynamicData(false);
                    var versionIdsToDelete = new int[0];

                    // ACTION
                    nodeHeadData.NodeId = 99999;
                    await DataStore.DataProvider.CopyAndUpdateNodeAsync(nodeHeadData, versionData, dynamicData, versionIdsToDelete);
                    Assert.Fail("ContentNotFoundException was not thrown.");
                }
                catch (ContentNotFoundException)
                {
                    // ignored
                }
            });
        }
        [TestMethod]
        public async STT.Task DP_Error_CopyAndUpdateNode_MissingVersion()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;
                var newNode = new SystemFolder(Repository.Root) { Name = "Folder1", Index = 42 };
                newNode.Save();

                try
                {
                    var node = Node.Load<SystemFolder>(newNode.Id);
                    node.Index++;
                    var nodeData = node.Data;
                    var nodeHeadData = nodeData.GetNodeHeadData();
                    var versionData = nodeData.GetVersionData();
                    var dynamicData = nodeData.GetDynamicData(false);
                    var versionIdsToDelete = new int[0];

                    // ACTION
                    versionData.VersionId = 99999;
                    await DataStore.DataProvider.CopyAndUpdateNodeAsync(nodeHeadData, versionData, dynamicData, versionIdsToDelete);
                    Assert.Fail("ContentNotFoundException was not thrown.");
                }
                catch (ContentNotFoundException)
                {
                    // ignored
                }
            });
        }
        [TestMethod]
        public async STT.Task DP_Error_CopyAndUpdateNode_OutOfDate()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;
                var newNode = new SystemFolder(Repository.Root) { Name = "Folder1", Index = 42 };
                newNode.Save();

                try
                {
                    var node = Node.Load<SystemFolder>(newNode.Id);
                    node.Index++;
                    var nodeData = node.Data;
                    var nodeHeadData = nodeData.GetNodeHeadData();
                    var versionData = nodeData.GetVersionData();
                    var dynamicData = nodeData.GetDynamicData(false);
                    var versionIdsToDelete = new int[0];

                    // ACTION
                    nodeHeadData.Timestamp++;
                    await DataStore.DataProvider.CopyAndUpdateNodeAsync(nodeHeadData, versionData, dynamicData, versionIdsToDelete);
                    Assert.Fail("NodeIsOutOfDateException was not thrown.");
                }
                catch (NodeIsOutOfDateException)
                {
                    // ignored
                }
            });
        }

        [TestMethod]
        public async STT.Task DP_Error_UpdateNodeHead_Deleted()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;
                var newNode = new SystemFolder(Repository.Root) { Name = "Folder1" };
                newNode.Save();

                try
                {
                    var node = Node.Load<SystemFolder>(newNode.Id);
                    var nodeData = node.Data;
                    var nodeHeadData = nodeData.GetNodeHeadData();
                    var versionIdsToDelete = new int[0];

                    // ACTION
                    nodeHeadData.NodeId = 999999;
                    await DataStore.DataProvider.UpdateNodeHeadAsync(nodeHeadData, versionIdsToDelete);
                    Assert.Fail("ContentNotFoundException was not thrown.");
                }
                catch (ContentNotFoundException)
                {
                    // ignored
                }
            });
        }
        [TestMethod]
        public async STT.Task DP_Error_UpdateNodeHead_OutOfDate()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;
                var newNode = new SystemFolder(Repository.Root) { Name = "Folder1" };
                newNode.Save();

                try
                {
                    var node = Node.Load<SystemFolder>(newNode.Id);
                    var nodeData = node.Data;
                    var nodeHeadData = nodeData.GetNodeHeadData();
                    var versionIdsToDelete = new int[0];

                    // ACTION
                    nodeHeadData.Timestamp++;
                    await DataStore.DataProvider.UpdateNodeHeadAsync(nodeHeadData, versionIdsToDelete);
                    Assert.Fail("NodeIsOutOfDateException was not thrown.");
                }
                catch (NodeIsOutOfDateException)
                {
                    // ignored
                }
            });
        }

        [TestMethod]
        public async STT.Task DP_Error_DeleteNode()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;

                // Create a small subtree
                var root = new SystemFolder(Repository.Root) { Name = "TestRoot" };
                root.Save();

                try
                {
                    var node = Node.Load<SystemFolder>(root.Id);
                    var nodeHeadData = node.Data.GetNodeHeadData();

                    // ACTION
                    nodeHeadData.Timestamp++;
                    await DataStore.DataProvider.DeleteNodeAsync(nodeHeadData);
                    Assert.Fail("NodeIsOutOfDateException was not thrown.");
                }
                catch (NodeIsOutOfDateException)
                {
                    // ignored
                }
            });
        }

        [TestMethod]
        public async STT.Task DP_Error_MoveNode_MissingSource()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;

                var root = new SystemFolder(Repository.Root) { Name = "TestRoot" }; root.Save();
                var source = new SystemFolder(root) { Name = "Source" }; source.Save();
                var target = new SystemFolder(root) { Name = "Target" }; target.Save();

                try
                {
                    var node = Node.Load<SystemFolder>(source.Id);
                    var nodeHeadData = node.Data.GetNodeHeadData();

                    // ACTION
                    nodeHeadData.NodeId = 999999;
                    await DataStore.DataProvider.MoveNodeAsync(nodeHeadData, target.Id, target.NodeTimestamp);
                    Assert.Fail("ContentNotFoundException was not thrown.");
                }
                catch (ContentNotFoundException)
                {
                    // ignored
                }
            });
        }
        [TestMethod]
        public async STT.Task DP_Error_MoveNode_MissingTarget()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;

                var root = new SystemFolder(Repository.Root) { Name = "TestRoot" }; root.Save();
                var source = new SystemFolder(root) { Name = "Source" }; source.Save();
                var target = new SystemFolder(root) { Name = "Target" }; target.Save();

                try
                {
                    var node = Node.Load<SystemFolder>(source.Id);
                    var nodeHeadData = node.Data.GetNodeHeadData();

                    // ACTION
                    await DataStore.DataProvider.MoveNodeAsync(nodeHeadData, 999999, target.NodeTimestamp);
                    Assert.Fail("ContentNotFoundException was not thrown.");
                }
                catch (ContentNotFoundException)
                {
                    // ignored
                }
            });
        }
        [TestMethod]
        public async STT.Task DP_Error_MoveNode_OutOfDate()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;

                var root = new SystemFolder(Repository.Root) { Name = "TestRoot" }; root.Save();
                var source = new SystemFolder(root) { Name = "Source" }; source.Save();
                var target = new SystemFolder(root) { Name = "Target" }; target.Save();

                try
                {
                    var node = Node.Load<SystemFolder>(source.Id);
                    var nodeHeadData = node.Data.GetNodeHeadData();

                    // ACTION
                    nodeHeadData.Timestamp++;
                    await DataStore.DataProvider.MoveNodeAsync(nodeHeadData, target.Id, target.NodeTimestamp);
                    Assert.Fail("NodeIsOutOfDateException was not thrown.");
                }
                catch (NodeIsOutOfDateException)
                {
                    // ignored
                }
            });
        }

        /* ================================================================================================== Transaction */

        /// <summary>
        /// Designed for testing the Rollback opration of the transactionality.
        /// An instance of this class is almost like a NodeHeadData but throws an exception
        /// when the setter of the Timestamp property is called. This call probably is always after all database operation
        /// so using this object helps the testing of the full rolling-back operation.
        /// </summary>
        private class ErrorGenNodeHeadData : NodeHeadData
        {
            private long _timestamp;
            public override long Timestamp
            {
                get => _timestamp;
                set => throw new Exception("Something went wrong.");
            }

            public static NodeHeadData Create(NodeHeadData src)
            {
                return new ErrorGenNodeHeadData
                {
                    NodeId = src.NodeId,
                    NodeTypeId = src.NodeTypeId,
                    ContentListTypeId = src.ContentListTypeId,
                    ContentListId = src.ContentListId,
                    CreatingInProgress = src.CreatingInProgress,
                    IsDeleted = src.IsDeleted,
                    ParentNodeId = src.ParentNodeId,
                    Name = src.Name,
                    DisplayName = src.DisplayName,
                    Path = src.Path,
                    Index = src.Index,
                    Locked = src.Locked,
                    LockedById = src.LockedById,
                    ETag = src.ETag,
                    LockType = src.LockType,
                    LockTimeout = src.LockTimeout,
                    LockDate = src.LockDate,
                    LockToken = src.LockToken,
                    LastLockUpdate = src.LastLockUpdate,
                    LastMinorVersionId = src.LastMinorVersionId,
                    LastMajorVersionId = src.LastMajorVersionId,
                    CreationDate = src.CreationDate,
                    CreatedById = src.CreatedById,
                    ModificationDate = src.ModificationDate,
                    ModifiedById = src.ModifiedById,
                    IsSystem = src.IsSystem,
                    OwnerId = src.OwnerId,
                    SavingState = src.SavingState,
                    _timestamp = src.Timestamp
                };
            }
        }

        [TestMethod]
        public void DP_Transaction_InsertNode()
        {
            Test(() =>
            {
                DataStore.Enabled = true;
                var db = GetDb();
                var countsBefore = $"{db.Nodes.Count},{db.Versions.Count},{db.LongTextProperties.Count}";

                // ACTION
                try
                {
                    var newNode =
                        new SystemFolder(Repository.Root) {Name = "Folder1", Description = "Description-1", Index = 42};
                    var nodeData = newNode.Data;
                    var hackedNodeHeadData = ErrorGenNodeHeadData.Create(nodeData.GetNodeHeadData());
                    var versionData = nodeData.GetVersionData();
                    var dynamicData = nodeData.GetDynamicData(false);
                    // Call low level API
                    DataStore.DataProvider.InsertNodeAsync(hackedNodeHeadData, versionData, dynamicData).Wait();
                }
                catch (Exception)
                {
                    // ignored
                    // hackedNodeHeadData threw an exception when Timestamp's setter was called.
                }

                // ASSERT (all operation need to be rolled back)
                var countsAfter = $"{db.Nodes.Count},{db.Versions.Count},{db.LongTextProperties.Count}";

                Assert.AreEqual(countsBefore, countsAfter);
            });
        }
        [TestMethod]
        public void DP_Transaction_UpdateNode()
        {
            Test(() =>
            {
                DataStore.Enabled = true;
                var db = GetDb();
                var newNode =
                    new SystemFolder(Repository.Root) { Name = "Folder1", Description = "Description-1", Index = 42 };
                newNode.Save();
                var nodeTimeStampBefore = newNode.NodeTimestamp;
                var versionTimeStampBefore = newNode.VersionTimestamp;

                // ACTION
                try
                {
                    var node = Node.Load<SystemFolder>(newNode.Id);
                    node.Index++;
                    node.Description = "Description-MODIFIED";
                    var nodeData = node.Data;
                    var hackedNodeHeadData = ErrorGenNodeHeadData.Create(nodeData.GetNodeHeadData());
                    var versionData = nodeData.GetVersionData();
                    var dynamicData = nodeData.GetDynamicData(false);
                    var versionIdsToDelete = new int[0];
                    // Call low level API
                    DataStore.DataProvider
                        .UpdateNodeAsync(hackedNodeHeadData, versionData, dynamicData, versionIdsToDelete).Wait();
                }
                catch (Exception)
                {
                    // ignored
                    // hackedNodeHeadData threw an exception when Timestamp's setter was called.
                }

                // ASSERT (all operation need to be rolled back)
                DistributedApplication.Cache.Reset();
                var reloaded = Node.Load<SystemFolder>(newNode.Id);
                var nodeTimeStampAfter = reloaded.NodeTimestamp;
                var versionTimeStampAfter = reloaded.VersionTimestamp;
                Assert.AreEqual(nodeTimeStampBefore, nodeTimeStampAfter);
                Assert.AreEqual(versionTimeStampBefore, versionTimeStampAfter);
                Assert.AreEqual(42, reloaded.Index);
                Assert.AreEqual("Description-1", reloaded.Description);
            });
        }
        [TestMethod]
        public void DP_Transaction_CopyAndUpdateNode()
        {
            Test(() =>
            {
                DataStore.Enabled = true;
                var db = GetDb();
                var newNode =
                    new SystemFolder(Repository.Root) { Name = "Folder1", Description = "Description-1", Index = 42 };
                newNode.Save();
                var version1 = newNode.Version.ToString();
                var versionId1 = newNode.VersionId;
                newNode.CheckOut();
                var version2 = newNode.Version.ToString();
                var versionId2 = newNode.VersionId;
                var countsBefore = $"{db.Nodes.Count},{db.Versions.Count},{db.LongTextProperties.Count}";

                // ACTION: simulate a modification and CheckIn on a checked-out, not-versioned node (V2.0.L -> V1.0.A).
                try
                {
                    var node = Node.Load<SystemFolder>(newNode.Id);
                    node.Index++;
                    node.Description = "Description-MODIFIED";
                    node.Version = VersionNumber.Parse(version1); // ApplySettings
                    var nodeData = node.Data;
                    var hackedNodeHeadData = ErrorGenNodeHeadData.Create(nodeData.GetNodeHeadData());
                    var versionData = nodeData.GetVersionData();
                    var dynamicData = nodeData.GetDynamicData(false);
                    var versionIdsToDelete = new[] {versionId2};
                    var expectedVersionId = versionId1;
                    // Call low level API
                    DataStore.DataProvider
                        .CopyAndUpdateNodeAsync(hackedNodeHeadData, versionData, dynamicData, versionIdsToDelete, expectedVersionId).Wait();
                }
                catch (Exception)
                {
                    // ignored
                    // hackedNodeHeadData threw an exception when Timestamp's setter was called.
                }

                // ASSERT (all operation need to be rolled back)
                var countsAfter = $"{db.Nodes.Count},{db.Versions.Count},{db.LongTextProperties.Count}";
                DistributedApplication.Cache.Reset();
                var reloaded = Node.Load<SystemFolder>(newNode.Id);
                Assert.AreEqual(countsBefore, countsAfter);
                Assert.AreEqual(version2, reloaded.Version.ToString());
                Assert.AreEqual(versionId2, reloaded.VersionId);
            });
        }
        [TestMethod]
        public void DP_Transaction_UpdateNodeHead()
        {
            Test(() =>
            {
                DataStore.Enabled = true;
                var db = GetDb();
                var newNode =
                    new SystemFolder(Repository.Root) { Name = "Folder1", Description = "Description-1", Index = 42 };
                newNode.Save();
                var version1 = newNode.Version.ToString();
                var versionId1 = newNode.VersionId;
                newNode.CheckOut();
                var version2 = newNode.Version.ToString();
                var versionId2 = newNode.VersionId;
                newNode.Index++;
                newNode.Description = "Description-MODIFIED";
                newNode.Save();
                var countsBefore = $"{db.Nodes.Count},{db.Versions.Count},{db.LongTextProperties.Count}";

                // ACTION: simulate a modification and UndoCheckout on a checked-out, not-versioned node (V2.0.L -> V1.0.A).
                try
                {
                    var node = Node.Load<SystemFolder>(newNode.Id);
                    var nodeData = node.Data;
                    var hackedNodeHeadData = ErrorGenNodeHeadData.Create(nodeData.GetNodeHeadData());
                    var versionData = nodeData.GetVersionData();
                    var dynamicData = nodeData.GetDynamicData(false);
                    var versionIdsToDelete = new[] { versionId2 };
                    var currentVersionId = newNode.VersionId;
                    var expectedVersionId = versionId1;
                    // Call low level API
                    DataStore.DataProvider
                        .UpdateNodeHeadAsync(hackedNodeHeadData, versionIdsToDelete).Wait();
                }
                catch (Exception)
                {
                    // ignored
                    // hackedNodeHeadData threw an exception when Timestamp's setter was called.
                }

                // ASSERT (all operation need to be rolled back)
                var countsAfter = $"{db.Nodes.Count},{db.Versions.Count},{db.LongTextProperties.Count}";
                DistributedApplication.Cache.Reset();
                var reloaded = Node.Load<SystemFolder>(newNode.Id);
                Assert.AreEqual(countsBefore, countsAfter);
                Assert.AreEqual(version2, reloaded.Version.ToString());
                Assert.AreEqual(versionId2, reloaded.VersionId);
            });
        }
        [TestMethod]
        public void DP_Transaction_MoveNode()
        {
            DPTest(() =>
            {
                DataStore.Enabled = true;

                // Create a small subtree
                var root = new SystemFolder(Repository.Root) { Name = "TestRoot" }; root.Save();
                var source = new SystemFolder(root) { Name = "Source" }; source.Save();
                var target = new SystemFolder(root) { Name = "Target" }; target.Save();
                var f1 = new SystemFolder(source) { Name = "F1" }; f1.Save();
                var f2 = new SystemFolder(source) { Name = "F2" }; f2.Save();
                var f3 = new SystemFolder(f1) { Name = "F3" }; f3.Save();
                var f4 = new SystemFolder(f1) { Name = "F4" }; f4.Save();

                // ACTION
                try
                {
                    var node = Node.Load<SystemFolder>(source.Id);
                    var nodeData = node.Data;
                    var hackedNodeHeadData = ErrorGenNodeHeadData.Create(nodeData.GetNodeHeadData());
                    // Call low level API
                    DataStore.DataProvider
                        .MoveNodeAsync(hackedNodeHeadData, target.Id, target.NodeTimestamp).Wait();
                }
                catch (Exception)
                {
                    // ignored
                    // hackedNodeHeadData threw an exception when Timestamp's setter was called.
                }

                // ASSERT
                DistributedApplication.Cache.Reset();
                target = Node.Load<SystemFolder>(target.Id);
                source = Node.Load<SystemFolder>(source.Id);
                f1 = Node.Load<SystemFolder>(f1.Id);
                f2 = Node.Load<SystemFolder>(f2.Id);
                f3 = Node.Load<SystemFolder>(f3.Id);
                f4 = Node.Load<SystemFolder>(f4.Id);
                Assert.AreEqual("/Root/TestRoot", root.Path);
                Assert.AreEqual("/Root/TestRoot/Target", target.Path);
                Assert.AreEqual("/Root/TestRoot/Source", source.Path);
                Assert.AreEqual("/Root/TestRoot/Source/F1", f1.Path);
                Assert.AreEqual("/Root/TestRoot/Source/F2", f2.Path);
                Assert.AreEqual("/Root/TestRoot/Source/F1/F3", f3.Path);
                Assert.AreEqual("/Root/TestRoot/Source/F1/F4", f4.Path);
            });
        }
        [TestMethod]
        public void DP_Transaction_RenameNode()
        {
            Test(() =>
            {
                DataStore.Enabled = true;
                var db = GetDb();
                var root = CreateFolder(Repository.Root, "F");
                var f1 = CreateFolder(root, "F1");
                var f11 = CreateFolder(f1, "F11");
                var f12 = CreateFolder(f1, "F12");
                var f2 = CreateFolder(root, "F2");
                var f21 = CreateFolder(f2, "F21");
                var f22 = CreateFolder(f2, "F22");
                var expectedPaths = (new[] { f1, f11, f12, f2, f21, f22 })
                    .Select(x => Node.Load<SystemFolder>(x.Id).Path.Replace("/Root/", ""))
                    .ToArray();

                // ACTION: rename root
                try
                {
                    var node = Node.Load<SystemFolder>(root.Id);
                    var originalPath = node.Path;
                    node.Name = "X";
                    node.Data.Path = node.ParentPath + "/X"; // illegal operation but this test requires
                    var nodeData = node.Data;
                    var hackedNodeHeadData = ErrorGenNodeHeadData.Create(nodeData.GetNodeHeadData());
                    var versionData = nodeData.GetVersionData();
                    var dynamicData = nodeData.GetDynamicData(false);
                    var versionIdsToDelete = new int[0];
                    // Call low level API
                    DataStore.DataProvider
                        .UpdateNodeAsync(hackedNodeHeadData, versionData, dynamicData, versionIdsToDelete, originalPath).Wait();
                }
                catch (Exception)
                {
                    // ignored
                    // hackedNodeHeadData threw an exception when Timestamp's setter was called.
                }

                // ASSERT (all operation need to be rolled back)
                var paths = (new[] {f1, f11, f12, f2, f21, f22})
                    .Select(x => Node.Load<SystemFolder>(x.Id).Path.Replace("/Root/", ""))
                    .ToArray();
                AssertSequenceEqual(expectedPaths, paths);
            });
        }
        [TestMethod]
        public void DP_Transaction_DeleteNode()
        {
            DPTest(() =>
            {
                DataStore.Enabled = true;

                // Create a small subtree
                var root = new SystemFolder(Repository.Root) { Name = "TestRoot", Description = "Test root"};
                root.Save();
                var f1 = new SystemFolder(root) { Name = "F1", Description = "Folder-1" };
                f1.Save();
                var f2 = new File(root) { Name = "F2" };
                f2.Binary.SetStream(RepositoryTools.GetStreamFromString("filecontent"));
                f2.Save();
                var f3 = new SystemFolder(f1) { Name = "F3" };
                f3.Save();
                var f4 = new File(root) { Name = "F4" };
                f4.Binary.SetStream(RepositoryTools.GetStreamFromString("filecontent"));
                f4.Save();

                var db = GetDb();
                var nodeCount = db.Nodes.Count;
                var versionCount = db.Versions.Count;
                var binPropCount = db.BinaryProperties.Count;
                var fileCount = db.Files.Count;

                // ACTION
                try
                {
                    var node = Node.Load<SystemFolder>(root.Id);
                    var nodeData = node.Data;
                    var hackedNodeHeadData = ErrorGenNodeHeadData.Create(nodeData.GetNodeHeadData());
                    // Call low level API
                    DataStore.DataProvider
                        .DeleteNodeAsync(hackedNodeHeadData).Wait();
                }
                catch (Exception)
                {
                    // ignored
                    // hackedNodeHeadData threw an exception when Timestamp's setter was called.
                }

                // ASSERT
                Assert.IsNotNull(Node.Load<SystemFolder>(root.Id));
                Assert.IsNotNull(Node.Load<SystemFolder>(f1.Id));
                Assert.IsNotNull(Node.Load<File>(f2.Id));
                Assert.IsNotNull(Node.Load<SystemFolder>(f3.Id));
                Assert.IsNotNull(Node.Load<File>(f4.Id));
                Assert.AreEqual(nodeCount, db.Nodes.Count);
                Assert.AreEqual(versionCount, db.Versions.Count);
                Assert.AreEqual(binPropCount, db.BinaryProperties.Count);
                Assert.AreEqual(fileCount, db.Files.Count);
            });
        }

        /* ================================================================================================== Schema */

        [TestMethod]
        public async STT.Task DP_Schema_()
        {
            await Test(async () =>
            {
                DataStore.Enabled = true;

                var dp = DataStore.DataProvider;
                var ed = new SchemaEditor();
                ed.Load();
                var timestampBefore = ed.SchemaTimestamp;

                // ACTION: try to start update with wrong timestamp
                try
                {
                    var unused = await dp.StartSchemaUpdateAsync(timestampBefore - 1);
                    Assert.Fail("Expected DataException was not thrown.");
                }
                catch (DataException)
                {
                    // "Storage schema is out of date."
                    // ignored
                }

                // ACTION: start update normally
                var @lock = await dp.StartSchemaUpdateAsync(timestampBefore);

                // ACTION: try to start update again
                try
                {
                    var unused = await dp.StartSchemaUpdateAsync(timestampBefore);
                    Assert.Fail("Expected DataException was not thrown.");
                }
                catch (DataException)
                {
                    // "Schema is locked by someone else."
                    // ignored
                }

                // ACTION: try to finish with invalid @lock
                try
                {
                    var unused = await dp.FinishSchemaUpdateAsync("wrong-lock");
                    Assert.Fail("Expected DataException was not thrown.");
                }
                catch (DataException)
                {
                    // "Schema is locked by someone else."
                    // ignored
                }

                // ACTION: finish normally
                var unused1 = await dp.FinishSchemaUpdateAsync(@lock);

                // ASSERT: start update is allowed again
                @lock = await dp.StartSchemaUpdateAsync(timestampBefore);
                // cleanup
                var timestampAfter = await dp.FinishSchemaUpdateAsync(@lock);
                // Bonus assert: there is no any changes
                Assert.AreEqual(timestampBefore, timestampAfter);
            });
        }

        /* ================================================================================================== */

        private InMemoryDataBase2 GetDb()
        {
            return ((InMemoryDataProvider2)Providers.Instance.DataProvider2).DB;
        }

        private void CheckDynamicDataByVersionId(int versionId)
        {
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

            DistributedApplication.Cache.Reset();
            ContentTypeManager.Reset();
            var portalContextAcc = new PrivateType(typeof(PortalContext));
            portalContextAcc.SetStaticField("_sites", new Dictionary<string, Site>());

            var builder = CreateRepositoryBuilderForTest();
            PrepareRepository();

            Indexing.IsOuterSearchEngineEnabled = true;

            using (Repository.Start(builder))
            {
                new SnMaintenance().Shutdown();
                using (new SystemAccount())
                    callback();
            }
        }

    }
}
