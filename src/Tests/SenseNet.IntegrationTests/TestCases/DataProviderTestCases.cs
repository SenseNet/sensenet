using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Versioning;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.Tests.Core;
using SenseNet.Tests.Core.Implementations;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.IntegrationTests.TestCases
{
    public class DataProviderTestCases : TestCaseBase
    {
        // ReSharper disable once InconsistentNaming
        protected DataProvider DP => DataStore.DataProvider;
        // ReSharper disable once InconsistentNaming
        protected ITestingDataProviderExtension TDP => DataStore.GetDataProviderExtension<ITestingDataProviderExtension>();

        /* ================================================================================================== */

        public async Task DP_InsertNode()
        {
            await IntegrationTestAsync(async () =>
            {
                var root = CreateTestRoot();

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
                await DP.InsertNodeAsync(nodeHeadData, versionData, dynamicData, CancellationToken.None);

                // ASSERT
                Assert.IsTrue(nodeHeadData.NodeId > 0);
                Assert.IsTrue(nodeHeadData.Timestamp > 0);
                Assert.IsTrue(versionData.VersionId > 0);
                Assert.IsTrue(versionData.Timestamp > 0);
                Assert.IsTrue(binaryProperty.Id > 0);
                Assert.IsTrue(binaryProperty.FileId > 0);
                Assert.IsTrue(nodeHeadData.LastMajorVersionId == versionData.VersionId);
                Assert.IsTrue(nodeHeadData.LastMajorVersionId == nodeHeadData.LastMinorVersionId);

                Cache.Reset();
                var loaded = Node.Load<File>(nodeHeadData.NodeId);
                Assert.IsNotNull(loaded);
                Assert.AreEqual("File1", loaded.Name);
                Assert.AreEqual(nodeHeadData.Path, loaded.Path);
                Assert.AreEqual(42, loaded.Index);
                Assert.AreEqual("File1 Content", RepositoryTools.GetStreamString(loaded.Binary.GetStream()));

                foreach (var propType in loaded.Data.PropertyTypes)
                    loaded.Data.GetDynamicRawData(propType);
                NodeDataChecker.Assert_DynamicPropertiesAreEqualExceptBinaries(nodeData, loaded.Data);

            });
        }
        public async Task DP_Update()
        {
            await IntegrationTestAsync(async () =>
            {
                var root = CreateTestRoot();

                var created = new File(root) { Name = "File1", Index = 42 };
                created.Binary.SetStream(RepositoryTools.GetStreamFromString("File1 Content"));
                created.Save();

                // Update a file but do not save
                var updated = Node.Load<File>(created.Id);
                updated.Index = 142;
                updated.Binary.SetStream(RepositoryTools.GetStreamFromString("File1 Content UPDATED"));
                var nodeData = updated.Data;
                GenerateTestData(nodeData);

                // ACTION
                var nodeHeadData = nodeData.GetNodeHeadData();
                var versionData = nodeData.GetVersionData();
                var dynamicData = nodeData.GetDynamicData(false);
                var versionIdsToDelete = new int[0];
                //var binaryProperty = dynamicData.BinaryProperties.First().Value;
                await DP.UpdateNodeAsync(nodeHeadData, versionData, dynamicData, versionIdsToDelete, CancellationToken.None);

                // ASSERT
                Assert.IsTrue(nodeHeadData.Timestamp > created.NodeTimestamp);

                Cache.Reset();
                var loaded = Node.Load<File>(nodeHeadData.NodeId);
                Assert.IsNotNull(loaded);
                Assert.AreEqual("File1", loaded.Name);
                Assert.AreEqual(nodeHeadData.Path, loaded.Path);
                Assert.AreEqual(142, loaded.Index);
                Assert.AreEqual("File1 Content UPDATED", RepositoryTools.GetStreamString(loaded.Binary.GetStream()));

                foreach (var propType in loaded.Data.PropertyTypes)
                    loaded.Data.GetDynamicRawData(propType);
                NodeDataChecker.Assert_DynamicPropertiesAreEqualExceptBinaries(nodeData, loaded.Data);
            });
        }
        public async Task DP_CopyAndUpdate_NewVersion()
        {
            await IntegrationTestAsync(async () =>
            {
                var root = CreateTestRoot();
                var refNode = new Application(root) { Name = "SampleApp" };
                refNode.Save();
                var created = new File(root) { Name = "File1", VersioningMode = VersioningType.MajorAndMinor, BrowseApplication = refNode };
                created.Binary.SetStream(RepositoryTools.GetStreamFromString("File1 Content"));
                created.Save();

                // Update a file but do not save
                var updated = Node.Load<File>(created.Id);
                var binary = updated.Binary;
                binary.SetStream(RepositoryTools.GetStreamFromString("File1 Content UPDATED"));
                updated.Binary = binary;
                var nodeData = updated.Data;

                // Patch version because the NodeSaveSetting logic is skipped.
                nodeData.Version = VersionNumber.Parse("V0.2.D");

                // Update dynamic properties
                GenerateTestData(nodeData, "BrowseApplication");
                var versionIdBefore = nodeData.VersionId;
                var modificationDateBefore = nodeData.ModificationDate;
                var nodeTimestampBefore = nodeData.NodeTimestamp;

                // ACTION
                nodeData.ModificationDate = DateTime.UtcNow;
                var nodeHeadData = nodeData.GetNodeHeadData();
                var versionData = nodeData.GetVersionData();
                var dynamicData = nodeData.GetDynamicData(false);
                var versionIdsToDelete = new int[0];
                await DP.CopyAndUpdateNodeAsync(nodeHeadData, versionData, dynamicData, versionIdsToDelete, CancellationToken.None);

                // ASSERT
                Assert.AreNotEqual(versionIdBefore, versionData.VersionId);

                Cache.Reset();
                var loaded = Node.Load<File>(nodeHeadData.NodeId);
                Assert.IsNotNull(loaded);
                Assert.AreEqual("File1", loaded.Name);
                Assert.AreEqual(nodeHeadData.Path, loaded.Path);
                Assert.AreNotEqual(nodeTimestampBefore, loaded.NodeTimestamp);
                Assert.AreNotEqual(modificationDateBefore, loaded.ModificationDate);
                Assert.AreEqual("File1 Content UPDATED", RepositoryTools.GetStreamString(loaded.Binary.GetStream()));
                Assert.AreEqual(refNode.Id, loaded.BrowseApplication.Id);

                foreach (var propType in loaded.Data.PropertyTypes)
                    loaded.Data.GetDynamicRawData(propType);
                NodeDataChecker.Assert_DynamicPropertiesAreEqualExceptBinaries(nodeData, loaded.Data, "BrowseApplication");
            });
        }
        public async Task DP_CopyAndUpdate_ExpectedVersion()
        {
            await IntegrationTestAsync(async () =>
            {
                var root = CreateTestRoot();
                var refNode = Node.LoadNode("/Root/(apps)/File/GetPreviewsFolder");
                var created = new File(root) { Name = "File1", BrowseApplication = refNode };
                created.Binary.SetStream(RepositoryTools.GetStreamFromString("File1 Content"));
                created.Save();
                var versionIdBefore = created.VersionId;

                created.CheckOut();

                // Update a file but do not save
                var updated = Node.Load<File>(created.Id);
                var binary = updated.Binary;
                binary.SetStream(RepositoryTools.GetStreamFromString("File1 Content UPDATED"));
                updated.Binary = binary;
                var nodeData = updated.Data;

                // Patch version because the NodeSaveSetting logic is skipped.
                nodeData.Version = VersionNumber.Parse("V1.0.A");

                // Update dynamic properties
                GenerateTestData(nodeData, "BrowseApplication");
                var modificationDateBefore = nodeData.ModificationDate;
                var nodeTimestampBefore = nodeData.NodeTimestamp;

                // ACTION
                nodeData.ModificationDate = DateTime.UtcNow;
                var nodeHeadData = nodeData.GetNodeHeadData();
                var versionData = nodeData.GetVersionData();
                var dynamicData = nodeData.GetDynamicData(false);
                var versionIdsToDelete = new [] { versionData.VersionId };
                var expectedVersionId = versionIdBefore;
                await DP.CopyAndUpdateNodeAsync(nodeHeadData, versionData, dynamicData, versionIdsToDelete, CancellationToken.None, expectedVersionId);

                // ASSERT
                Assert.AreEqual(versionIdBefore, versionData.VersionId);

                Cache.Reset();
                var loaded = Node.Load<File>(nodeHeadData.NodeId);
                Assert.IsNotNull(loaded);
                Assert.AreEqual("File1", loaded.Name);
                Assert.AreEqual(nodeHeadData.Path, loaded.Path);
                Assert.AreNotEqual(nodeTimestampBefore, loaded.NodeTimestamp);
                Assert.AreNotEqual(modificationDateBefore, loaded.ModificationDate);
                Assert.AreEqual(VersionNumber.Parse("V1.0.A"), loaded.Version);
                Assert.AreEqual(versionIdBefore, loaded.VersionId);
                Assert.AreEqual("File1 Content UPDATED", RepositoryTools.GetStreamString(loaded.Binary.GetStream()));

                foreach (var propType in loaded.Data.PropertyTypes)
                    loaded.Data.GetDynamicRawData(propType);
                NodeDataChecker.Assert_DynamicPropertiesAreEqualExceptBinaries(nodeData, loaded.Data, "BrowseApplication");
            });
        }
        public async Task DP_UpdateNodeHead()
        {
            await IntegrationTestAsync(async () =>
            {
                // Create a file under the test root
                var root = CreateTestRoot();
                var created = new File(root) { Name = "File1" };
                created.Binary.SetStream(RepositoryTools.GetStreamFromString("File1 Content"));
                created.Save();

                // Memorize final expectations
                var expectedVersion = created.Version;
                var expectedVersionId = created.VersionId;
                var createdHead = NodeHead.Get(created.Id);
                var expectedLastMajor = createdHead.LastMajorVersionId;
                var expectedLastMinor = createdHead.LastMinorVersionId;

                // Make a new version.
                created.CheckOut();

                // Modify the new version.
                var checkedOut = Node.Load<File>(created.Id);
                var binary = checkedOut.Binary;
                binary.SetStream(RepositoryTools.GetStreamFromString("File1 Content UPDATED"));
                checkedOut.Binary = binary;
                checkedOut.Save();

                // PREPARE THE LAST ACTION: simulate UndoCheckOut
                var modified = Node.Load<File>(created.Id);
                var oldTimestamp = modified.NodeTimestamp;
                // Get the editable NodeData
                modified.Index = modified.Index;
                var nodeData = modified.Data;

                nodeData.LastLockUpdate = DP.DateTimeMinValue;
                nodeData.LockDate = DP.DateTimeMinValue;
                nodeData.Locked = false;
                nodeData.LockedById = 0;
                nodeData.ModificationDate = DateTime.UtcNow;
                var nodeHeadData = nodeData.GetNodeHeadData();
                var deletedVersionId = nodeData.VersionId;
                var versionIdsToDelete = new [] { deletedVersionId };

                // ACTION: Simulate UndoCheckOut
                await DP.UpdateNodeHeadAsync(nodeHeadData, versionIdsToDelete, CancellationToken.None);

                // ASSERT: the original state is restored after the UndoCheckOut operation
                Assert.IsTrue(oldTimestamp < nodeHeadData.Timestamp);
                Cache.Reset();
                var reloaded = Node.Load<File>(created.Id);
                Assert.AreEqual(expectedVersion, reloaded.Version);
                Assert.AreEqual(expectedVersionId, reloaded.VersionId);
                var reloadedHead = NodeHead.Get(created.Id);
                Assert.AreEqual(expectedLastMajor, reloadedHead.LastMajorVersionId);
                Assert.AreEqual(expectedLastMinor, reloadedHead.LastMinorVersionId);
                Assert.AreEqual("File1 Content", RepositoryTools.GetStreamString(reloaded.Binary.GetStream()));
                Assert.IsNull(Node.LoadNodeByVersionId(deletedVersionId));
            });
        }

        public async Task DP_HandleAllDynamicProps()
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
            await IntegrationTestAsync(async () =>
            {
                Node node = null;
                try
                {
                    ContentTypeInstaller.InstallContentType(ctd);
                    var unused = ContentType.GetByName(contentTypeName); // preload schema

                    var root = new SystemFolder(Repository.Root) { Name = "TestRoot" };
                    root.Save();

                    // ACTION-1 CREATE
                    // Create all kind of dynamic properties
                    node = new GenericContent(root, contentTypeName)
                    {
                        Name = $"{contentTypeName}1",
                        ["ShortText1"] = "ShortText value 1",
                        ["LongText1"] = "LongText value 1",
                        ["Integer1"] = 42,
                        ["Number1"] = 42.56m,
                        ["DateTime1"] = new DateTime(1111, 11, 11)
                    };
                    node.AddReference("Reference1", Repository.Root);
                    node.AddReference("Reference1", root);
                    node.Save();

                    // ASSERT-1
                    Assert.AreEqual("ShortText value 1", await TDP.GetPropertyValueAsync(node.VersionId, "ShortText1"));
                    Assert.AreEqual("LongText value 1", await TDP.GetPropertyValueAsync(node.VersionId, "LongText1"));
                    Assert.AreEqual(42, await TDP.GetPropertyValueAsync(node.VersionId, "Integer1"));
                    Assert.AreEqual(42.56m, await TDP.GetPropertyValueAsync(node.VersionId, "Number1"));
                    Assert.AreEqual(new DateTime(1111, 11, 11), await TDP.GetPropertyValueAsync(node.VersionId, "DateTime1"));
                    Assert.AreEqual($"{Repository.Root.Id},{root.Id}", ArrayToString((int[])await TDP.GetPropertyValueAsync(node.VersionId, "Reference1")));

                    // ACTION-2 UPDATE-1
                    node = Node.Load<GenericContent>(node.Id);
                    // Update all kind of dynamic properties
                    node["ShortText1"] = "ShortText value 2";
                    node["LongText1"] = "LongText value 2";
                    node["Integer1"] = 43;
                    node["Number1"] = 42.099m;
                    node["DateTime1"] = new DateTime(1111, 11, 22);
                    node.RemoveReference("Reference1", Repository.Root);
                    node.Save();

                    // ASSERT-2
                    Assert.AreEqual("ShortText value 2", await TDP.GetPropertyValueAsync(node.VersionId, "ShortText1"));
                    Assert.AreEqual("LongText value 2", await TDP.GetPropertyValueAsync(node.VersionId, "LongText1"));
                    Assert.AreEqual(43, await TDP.GetPropertyValueAsync(node.VersionId, "Integer1"));
                    Assert.AreEqual(42.099m, await TDP.GetPropertyValueAsync(node.VersionId, "Number1"));
                    Assert.AreEqual(new DateTime(1111, 11, 22), await TDP.GetPropertyValueAsync(node.VersionId, "DateTime1"));
                    Assert.AreEqual($"{root.Id}", ArrayToString((int[])await TDP.GetPropertyValueAsync(node.VersionId, "Reference1")));

                    // ACTION-3 UPDATE-2
                    node = Node.Load<GenericContent>(node.Id);
                    // Remove existing references
                    node.RemoveReference("Reference1", root);
                    node.Save();

                    // ASSERT-3
                    Assert.AreEqual("ShortText value 2", await TDP.GetPropertyValueAsync(node.VersionId, "ShortText1"));
                    Assert.AreEqual("LongText value 2", await TDP.GetPropertyValueAsync(node.VersionId, "LongText1"));
                    Assert.AreEqual(43, await TDP.GetPropertyValueAsync(node.VersionId, "Integer1"));
                    Assert.AreEqual(42.099m, await TDP.GetPropertyValueAsync(node.VersionId, "Number1"));
                    Assert.AreEqual(new DateTime(1111, 11, 22), await TDP.GetPropertyValueAsync(node.VersionId, "DateTime1"));
                    Assert.IsNull(await TDP.GetPropertyValueAsync(node.VersionId, "Reference1"));
                }
                finally
                {
                    node?.ForceDelete();
                    ContentTypeInstaller.RemoveContentType(contentTypeName);
                }
            });
        }

        public async Task DP_Rename()
        {
            await IntegrationTestAsync(async () =>
            {
                // Create a small subtree
                var root = CreateTestRoot();
                var f1 = new SystemFolder(root) { Name = "F1" }; f1.Save();
                var f2 = new SystemFolder(root) { Name = "F2" }; f2.Save();
                var f3 = new SystemFolder(f1) { Name = "F3" }; f3.Save();
                var f4 = new SystemFolder(f1) { Name = "F4" }; f4.Save();

                // ACTION: Rename root
                root = Node.Load<SystemFolder>(root.Id);
                var originalPath = root.Path;
                var newName = Guid.NewGuid() + "-RENAMED";
                root.Name = newName;
                var nodeData = root.Data;
                nodeData.Path = RepositoryPath.Combine(root.ParentPath, root.Name); // ApplySettings
                var nodeHeadData = nodeData.GetNodeHeadData();
                var versionData = nodeData.GetVersionData();
                var dynamicData = nodeData.GetDynamicData(false);
                var versionIdsToDelete = new int[0];
                await DP.UpdateNodeAsync(nodeHeadData, versionData, dynamicData, versionIdsToDelete, CancellationToken.None, originalPath);

                // ASSERT
                Cache.Reset();
                f1 = Node.Load<SystemFolder>(f1.Id);
                f2 = Node.Load<SystemFolder>(f2.Id);
                f3 = Node.Load<SystemFolder>(f3.Id);
                f4 = Node.Load<SystemFolder>(f4.Id);
                Assert.AreEqual("/Root/" + newName, root.Path);
                Assert.AreEqual("/Root/" + newName + "/F1", f1.Path);
                Assert.AreEqual("/Root/" + newName + "/F2", f2.Path);
                Assert.AreEqual("/Root/" + newName + "/F1/F3", f3.Path);
                Assert.AreEqual("/Root/" + newName + "/F1/F4", f4.Path);
            });
        }

        public async Task DP_LoadChildren()
        {
            await IntegrationTestAsync(async () =>
            {
                Cache.Reset();
                var loaded = Repository.Root.Children.Select(x => x.Id.ToString()).ToArray();

                int[] expected = await TDP.GetChildNodeIdsByParentNodeIdAsync(Repository.Root.Id);

                Assert.AreEqual(string.Join(",", expected), string.Join(",", loaded));
            });
        }

        public async Task DP_Move()
        {
            await MoveTest(async (source, target) =>
            {
                var sourceTimestampBefore = source.NodeTimestamp;

                // ACTION: Node.Move(source.Path, target.Path);
                var srcNodeHeadData = source.Data.GetNodeHeadData();
                await DP.MoveNodeAsync(srcNodeHeadData, target.Id, CancellationToken.None);

                // ASSERT
                Assert.AreNotEqual(sourceTimestampBefore, srcNodeHeadData.Timestamp);

                //There are further asserts in the caller. See the MoveTest method.
            });
        }
        public async Task DP_Move_DataStore_NodeHead()
        {
            await MoveTest(async (source, target) =>
            {
                var sourceTimestampBefore = source.NodeTimestamp;

                // ACTION
                var sourceNodeHead = NodeHead.Get(source.Id);
                await DataStore.MoveNodeAsync(sourceNodeHead, target.Id, CancellationToken.None);

                // ASSERT
                Assert.AreNotEqual(sourceTimestampBefore, sourceNodeHead.Timestamp);

                //There are further asserts in the caller. See the MoveTest method.
            });
        }
        public async Task DP_Move_DataStore_NodeData()
        {
            await MoveTest(async (source, target) =>
            {
                var sourceTimestampBefore = source.NodeTimestamp;
                source.Index++; // ensure private source.Data

                // ACTION
                await DataStore.MoveNodeAsync(source.Data, target.Id, CancellationToken.None);

                // ASSERT
                // timestamp is changed because the source.Data is private
                Assert.AreNotEqual(sourceTimestampBefore, source.NodeTimestamp);

                //There are further asserts in the caller. See the MoveTest method.
            });
        }
        private async Task MoveTest(Func<Node, Node, Task> callback)
        {
            await IntegrationTestAsync(async () =>
            {
                // Create a small subtree
                var root = CreateTestRoot();
                var rootPath = root.Path;
                var source = new SystemFolder(root) { Name = "Source" }; source.Save();
                var target = new SystemFolder(root) { Name = "Target" }; target.Save();
                var f1 = new SystemFolder(source) { Name = "F1" }; f1.Save();
                var f2 = new SystemFolder(source) { Name = "F2" }; f2.Save();
                var f3 = new SystemFolder(f1) { Name = "F3" }; f3.Save();
                var f4 = new SystemFolder(f1) { Name = "F4" }; f4.Save();

                // ACTION: Node.Move(source.Path, target.Path);
                await callback(source, target);

                // ASSERT
                Cache.Reset(); // simulates PathDependency operation (see the Node.MoveTo method).
                target = Node.Load<SystemFolder>(target.Id);
                source = Node.Load<SystemFolder>(source.Id);
                f1 = Node.Load<SystemFolder>(f1.Id);
                f2 = Node.Load<SystemFolder>(f2.Id);
                f3 = Node.Load<SystemFolder>(f3.Id);
                f4 = Node.Load<SystemFolder>(f4.Id);
                Assert.AreEqual(rootPath, root.Path);
                Assert.AreEqual(rootPath + "/Target", target.Path);
                Assert.AreEqual(rootPath + "/Target/Source", source.Path);
                Assert.AreEqual(rootPath + "/Target/Source/F1", f1.Path);
                Assert.AreEqual(rootPath + "/Target/Source/F2", f2.Path);
                Assert.AreEqual(rootPath + "/Target/Source/F1/F3", f3.Path);
                Assert.AreEqual(rootPath + "/Target/Source/F1/F4", f4.Path);
            });
        }

        /* ================================================================================================== */

        private void GenerateTestData(NodeData nodeData, params string[] excludedProperties)
        {
            foreach (var propType in nodeData.PropertyTypes.Where(x => !excludedProperties.Contains(x.Name)))
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
                case DataType.Reference: return new List<int> { Rng(), Rng() };
                // ReSharper disable once RedundantCaseLabel
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

        private SystemFolder CreateTestRoot()
        {
            return CreateFolder(Repository.Root, "TestRoot" + Guid.NewGuid());
        }
        private SystemFolder CreateFolder(Node parent, string name = null)
        {
            var folder = new SystemFolder(parent) { Name = name ?? Guid.NewGuid().ToString() };
            folder.Save();
            return folder;
        }
        //private File CreateFile(Node parent, string name, string fileContent)
        //{
        //    var file = new File(parent) { Name = name };
        //    file.Binary.SetStream(RepositoryTools.GetStreamFromString(fileContent));
        //    file.Save();
        //    return file;
        //}

        //private async Task<(int Nodes, int Versions, int Binaries, int Files, int LongTexts, string AllCounts, string AllCountsExceptFiles)> GetDbObjectCountsAsync(string path, DataProvider DP, ITestingDataProviderExtension tdp)
        //{
        //    var nodes = await DP.GetNodeCountAsync(path, CancellationToken.None);
        //    var versions = await DP.GetVersionCountAsync(path, CancellationToken.None);
        //    var binaries = await TDP.GetBinaryPropertyCountAsync(path);
        //    var files = await TDP.GetFileCountAsync(path);
        //    var longTexts = await TDP.GetLongTextCountAsync(path);
        //    var all = $"{nodes},{versions},{binaries},{files},{longTexts}";
        //    var allExceptFiles = $"{nodes},{versions},{binaries},{longTexts}";

        //    var result = (Nodes: nodes, Versions: versions, Binaries: binaries, Files: files, LongTexts: longTexts, AllCounts: all, AllCountsExceptFiles: allExceptFiles);
        //    return await Task.FromResult(result);
        //}

        //private async Task<object> ExecuteScalarAsync(string sql)
        //{
        //    using (var ctx = DP.CreateDataContext(CancellationToken.None))
        //        return await ctx.ExecuteScalarAsync(sql);
        //}

        protected string ArrayToString(int[] array)
        {
            return string.Join(",", array.Select(x => x.ToString()));
        }
        //protected string ArrayToString(List<int> array)
        //{
        //    return string.Join(",", array.Select(x => x.ToString()));
        //}
        //protected string ArrayToString(IEnumerable<object> array)
        //{
        //    return string.Join(",", array.Select(x => x.ToString()));
        //}
    }
}
