using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Search.Querying;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Versioning;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;
using SenseNet.Security;
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
        // ReSharper disable once InconsistentNaming
        private DataProvider CCDP => TDP.CreateCannotCommitDataProvider(DP);

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
            await DpMoveTest(async (source, target) =>
            {
                var sourceTimestampBefore = source.NodeTimestamp;

                // ACTION: Node.Move(source.Path, target.Path);
                var srcNodeHeadData = source.Data.GetNodeHeadData();
                await DP.MoveNodeAsync(srcNodeHeadData, target.Id, CancellationToken.None);

                // ASSERT
                Assert.AreNotEqual(sourceTimestampBefore, srcNodeHeadData.Timestamp);

                //There are further asserts in the caller. See the DpMoveTest method.
            });
        }
        public async Task DP_Move_DataStore_NodeHead()
        {
            await DpMoveTest(async (source, target) =>
            {
                var sourceTimestampBefore = source.NodeTimestamp;

                // ACTION
                var sourceNodeHead = NodeHead.Get(source.Id);
                await DataStore.MoveNodeAsync(sourceNodeHead, target.Id, CancellationToken.None);

                // ASSERT
                Assert.AreNotEqual(sourceTimestampBefore, sourceNodeHead.Timestamp);

                //There are further asserts in the caller. See the DpMoveTest method.
            });
        }
        public async Task DP_Move_DataStore_NodeData()
        {
            await DpMoveTest(async (source, target) =>
            {
                var sourceTimestampBefore = source.NodeTimestamp;
                source.Index++; // ensure private source.Data

                // ACTION
                await DataStore.MoveNodeAsync(source.Data, target.Id, CancellationToken.None);

                // ASSERT
                // timestamp is changed because the source.Data is private
                Assert.AreNotEqual(sourceTimestampBefore, source.NodeTimestamp);

                //There are further asserts in the caller. See the DpMoveTest method.
            });
        }
        private async Task DpMoveTest(Func<Node, Node, Task> callback)
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

        public async Task DP_RefreshCacheAfterSave()
        {
            await IntegrationTestAsync(() =>
            {
                var root = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };

                // ACTION-1: Create
                root.Save();
                var nodeTimestamp1 = root.NodeTimestamp;
                var versionTimestamp1 = root.VersionTimestamp;

                // ASSERT-1: NodeData is in cache after creation
                var cacheKey1 = DataStore.GenerateNodeDataVersionIdCacheKey(root.VersionId);
                var item1 = Cache.Get(cacheKey1);
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
                var item2 = Cache.Get(cacheKey2);
                Assert.IsNotNull(item2);
                var cachedNodeData2 = item2 as NodeData;
                Assert.IsNotNull(cachedNodeData2);
                Assert.AreEqual(nodeTimestamp2, cachedNodeData2.NodeTimestamp);
                Assert.AreEqual(versionTimestamp2, cachedNodeData2.VersionTimestamp);

                return Task.FromResult(true);
            });
        }

        public async Task DP_LazyLoadedBigText()
        {
            await IntegrationTestAsync(async () =>
            {
                var nearlyLongText = new string('a', DataStore.TextAlternationSizeLimit - 10);
                var longText = new string('c', DataStore.TextAlternationSizeLimit + 10);
                var descriptionPropertyType = ActiveSchema.PropertyTypes["Description"];

                // ACTION-1a: Creation with text that shorter than the magic limit
                var root = new SystemFolder(Repository.Root)
                { Name = Guid.NewGuid().ToString(), Description = nearlyLongText };
                root.Save();
                // ACTION-1b: Load the node
                var loaded = (await DP.LoadNodesAsync(new[] { root.VersionId }, CancellationToken.None)).First();
                var longTextProps = loaded.GetDynamicData(false).LongTextProperties;
                var longTextPropType = longTextProps.First().Key;

                // ASSERT-1
                Assert.AreEqual("Description", longTextPropType.Name);

                // ACTION-2a: Update text property value in the database over the magic limit
                await TDP.UpdateDynamicPropertyAsync(loaded.VersionId, "Description", longText);
                // ACTION-2b: Load the node
                loaded = (await DP.LoadNodesAsync(new[] { root.VersionId }, CancellationToken.None)).First();
                longTextProps = loaded.GetDynamicData(false).LongTextProperties;

                // ASSERT-2
                Assert.AreEqual(0, longTextProps.Count);

                // ACTION-3: Load the property value
                Cache.Reset();
                root = Node.Load<SystemFolder>(root.Id);
                var lazyLoadedDescription = root.Description; // Loads the property value

                // ASSERT-3
                Assert.AreEqual(longText, lazyLoadedDescription);
            });
        }
        public async Task DP_LazyLoadedBigTextVsCache()
        {
            await IntegrationTestAsync(() =>
            {
                var nearlyLongText1 = new string('a', DataStore.TextAlternationSizeLimit - 10);
                var nearlyLongText2 = new string('b', DataStore.TextAlternationSizeLimit - 10);
                var longText = new string('c', DataStore.TextAlternationSizeLimit + 10);
                var descriptionPropertyType = ActiveSchema.PropertyTypes["Description"];

                // ACTION-1: Creation with text that shorter than the magic limit
                var root = new SystemFolder(Repository.Root)
                {
                    Name = Guid.NewGuid().ToString(),
                    Description = nearlyLongText1
                };
                root.Save();
                var cacheKey = DataStore.GenerateNodeDataVersionIdCacheKey(root.VersionId);

                // ASSERT-1: text property is in cache
                var cachedNodeData = (NodeData)Cache.Get(cacheKey);
                Assert.IsTrue(cachedNodeData.IsShared);
                var longTextProperties = cachedNodeData.GetDynamicData(false).LongTextProperties;
                Assert.IsTrue(longTextProperties.ContainsKey(descriptionPropertyType));
                Assert.AreEqual(nearlyLongText1, (string)longTextProperties[descriptionPropertyType]);

                // ACTION-2: Update with text that shorter than the magic limit
                root = Node.Load<SystemFolder>(root.Id);
                root.Description = nearlyLongText2;
                root.Save();

                // ASSERT-2: text property is in cache
                cachedNodeData = (NodeData)Cache.Get(cacheKey);
                Assert.IsTrue(cachedNodeData.IsShared);
                longTextProperties = cachedNodeData.GetDynamicData(false).LongTextProperties;
                Assert.IsTrue(longTextProperties.ContainsKey(descriptionPropertyType));
                Assert.AreEqual(nearlyLongText2, (string)longTextProperties[descriptionPropertyType]);

                // ACTION-3: Update with text that longer than the magic limit
                root = Node.Load<SystemFolder>(root.Id);
                root.Description = longText;
                root.Save();

                // ASSERT-3: text property is not in the cache
                cachedNodeData = (NodeData)Cache.Get(cacheKey);
                Assert.IsTrue(cachedNodeData.IsShared);
                longTextProperties = cachedNodeData.GetDynamicData(false).LongTextProperties;
                Assert.IsFalse(longTextProperties.ContainsKey(descriptionPropertyType));

                // ACTION-4: Load the text property
                var loadedValue = root.Description;

                // ASSERT-4: Property is loaded and is in cache
                Assert.AreEqual(longText, loadedValue);
                cachedNodeData = (NodeData)Cache.Get(cacheKey);
                Assert.IsTrue(cachedNodeData.IsShared);
                longTextProperties = cachedNodeData.GetDynamicData(false).LongTextProperties;
                Assert.IsTrue(longTextProperties.ContainsKey(descriptionPropertyType));

                return Task.CompletedTask;
            });
        }

        public async Task DP_LoadChildTypesToAllow()
        {
            await IntegrationTestAsync(async () =>
            {
                // Create a small subtree
                var root = CreateTestRoot();
                var site1 = new Workspace(root) { Name = "Site1" }; site1.Save();
                site1.AllowChildTypes(new[] { "Task" }); site1.Save();
                site1 = Node.Load<Workspace>(site1.Id);
                var folder1 = new Folder(site1) { Name = "Folder1" }; folder1.Save();
                var folder2 = new Folder(folder1) { Name = "Folder2" }; folder2.Save();
                var folder3 = new Folder(folder1) { Name = "Folder3" }; folder3.Save();
                var task1 = new ContentRepository.Task(folder3) { Name = "Task1" }; task1.Save();
                var doclib1 = new ContentList(folder3, "DocumentLibrary") { Name = "Doclib1" }; doclib1.Save();
                var file1 = new File(doclib1) { Name = "File1" }; file1.Save();
                var systemFolder1 = new SystemFolder(doclib1) { Name = "SystemFolder1" }; systemFolder1.Save();
                var file2 = new File(systemFolder1) { Name = "File2" }; file2.Save();
                var memoList1 = new ContentList(folder1, "MemoList") { Name = "MemoList1" }; memoList1.Save();
                var site2 = new Workspace(root) { Name = "Site2" }; site2.Save();

                // ACTION
                var types = await DataStore.LoadChildTypesToAllowAsync(folder1.Id, CancellationToken.None);

                // ASSERT
                var names = string.Join(", ", types.Select(x => x.Name).OrderBy(x => x));
                Assert.AreEqual("DocumentLibrary, Folder, MemoList, Task", names);
            });
        }
        public async Task DP_ContentListTypesInTree()
        {
            await IntegrationTestAsync(async () =>
            {
                // ALIGN-1
                ActiveSchema.Reset();
                var contentLlistTypeCountBefore = ActiveSchema.ContentListTypes.Count;
                var root = CreateTestRoot();

                // ACTION-1
                var result1 = await DP.GetContentListTypesInTreeAsync(root.Path, CancellationToken.None);

                // ASSERT-1
                Assert.IsNotNull(result1);
                Assert.AreEqual(0, result1.Count);
                Assert.AreEqual(contentLlistTypeCountBefore, ActiveSchema.ContentListTypes.Count);

                // ALIGN-2
                // Creation
                var node = new ContentList(root) { Name = "Survey-1" };
                node.Save();

                // ACTION-2
                var result2 = await DP.GetContentListTypesInTreeAsync(root.Path, CancellationToken.None);

                // ASSERT
                Assert.AreEqual(contentLlistTypeCountBefore + 1, ActiveSchema.ContentListTypes.Count);
                Assert.IsNotNull(result2);
                Assert.AreEqual(1, result2.Count);
                Assert.AreEqual(ActiveSchema.ContentListTypes.Last().Id, result2[0].Id);
            });
        }

        public async Task DP_ForceDelete()
        {
            await IntegrationTestAsync(async () =>
            {
                var countsBefore = await GetDbObjectCountsAsync(null, DP, TDP);

                // Create a small subtree
                var root = CreateTestRoot();
                var f1 = new SystemFolder(root) { Name = "F1" };
                f1.Save();
                var f2 = new File(root) { Name = "F2" };
                f2.Binary.SetStream(RepositoryTools.GetStreamFromString("filecontent"));
                f2.Save();
                var f3 = new SystemFolder(f1) { Name = "F3" };
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
                var countsAfter = await GetDbObjectCountsAsync(null, DP, TDP);
                Assert.AreEqual(countsBefore.AllCountsExceptFiles, countsAfter.AllCountsExceptFiles);
            });
        }
        public async Task DP_DeleteDeleted()
        {
            await IntegrationTestAsync(async () =>
            {
                var folder = new SystemFolder(Repository.Root) { Name = "Folder1" };
                folder.Save();
                folder = Node.Load<SystemFolder>(folder.Id);
                var nodeHeadData = folder.Data.GetNodeHeadData();
                Node.ForceDelete(folder.Path);

                // ACTION
                await DP.DeleteNodeAsync(nodeHeadData, CancellationToken.None);

                // ASSERT
                // Expectation: no exception was thrown
            });
        }

        public async Task DP_GetVersionNumbers()
        {
            await IntegrationTestAsync(() =>
            {
                var folderB = new SystemFolder(Repository.Root)
                {
                    Name = Guid.NewGuid().ToString(),
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
                var allVersinsById = Node.GetVersionNumbers(folderB.Id);
                var allVersinsByPath = Node.GetVersionNumbers(folderB.Path);

                // Check
                var expected = new[] { "V0.1.D", "V0.2.D", "V0.3.D", "V1.0.A", "V1.1.D",
                        "V1.2.D", "V2.0.A", "V2.1.D", "V2.2.D", "V3.0.A" }
                    .Select(VersionNumber.Parse).ToArray();
                AssertSequenceEqual(expected, allVersinsById);
                AssertSequenceEqual(expected, allVersinsByPath);

                return Task.CompletedTask;
            });
        }
        public async Task DP_GetVersionNumbers_MissingNode()
        {
            await IntegrationTestAsync(async () =>
            {
                // ACTION
                var result = await DP.GetVersionNumbersAsync("/Root/Deleted", CancellationToken.None);

                // ASSERT
                Assert.IsFalse(result.Any());
            });
        }

        public async Task DP_LoadBinaryPropertyValues()
        {
            await IntegrationTestAsync(async () =>
            {
                ContentTypeInstaller.InstallContentType(@"<?xml version=""1.0"" encoding=""utf-8""?>
<ContentType name=""File2"" parentType=""File"" handler=""SenseNet.ContentRepository.File"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
  <Fields>
    <Field name=""Binary2"" type=""Binary"">
      <DisplayName>$Ctd-File,Binary-DisplayName</DisplayName>
      <Description>$Ctd-File,Binary-Description</Description>
      <Indexing>
        <Analyzer>Standard</Analyzer>
      </Indexing>
      <Configuration>
        <VisibleBrowse>Show</VisibleBrowse>
        <VisibleEdit>Hide</VisibleEdit>
        <VisibleNew>Hide</VisibleNew>
      </Configuration>
    </Field>
  </Fields>
</ContentType>");

                var root = CreateFolder(Repository.Root, "TestRoot");
                var file = new File(root, "File2") { Name = "File-1.txt" };
                file.Binary.SetStream(RepositoryTools.GetStreamFromString("File content."));
                var binary2 = new BinaryData {FileName = "File-1.SecondaryStream", ContentType = "text2/plain2"};
                binary2.SetStream(RepositoryTools.GetStreamFromString("File secondary content."));
                file.SetProperty("Binary2", binary2);
                file.Save();

                var versionId = file.VersionId;
                var fileId = file.Binary.FileId;
                var propertyTypeId = file.Binary.PropertyType.Id;
                var propertyTypeId2 = file.PropertyTypes.Single(x => x.Name == "Binary2").Id;

                // ACTION-1: Load existing
                var result = await DP.LoadBinaryPropertyValueAsync(versionId, propertyTypeId, CancellationToken.None);
                var result2 = await DP.LoadBinaryPropertyValueAsync(versionId, propertyTypeId2, CancellationToken.None);
                // ASSERT-1
                Assert.IsNotNull(result);
                Assert.AreEqual("File-1", result.FileName.FileNameWithoutExtension);
                Assert.AreEqual("txt", result.FileName.Extension);
                Assert.AreEqual(3L + "File content.".Length, result.Size); // +UTF-8 BOM
                Assert.AreEqual("text/plain", result.ContentType);
                Assert.IsNotNull(result2);
                Assert.AreEqual("File-1", result2.FileName.FileNameWithoutExtension);
                Assert.AreEqual("SecondaryStream", result2.FileName.Extension);
                Assert.AreEqual(3L + "File secondary content.".Length, result2.Size); // +UTF-8 BOM
                Assert.AreEqual("text2/plain2", result2.ContentType);

                // ACTION-2: Missing Binary
                result = await DP.LoadBinaryPropertyValueAsync(versionId, 999999, CancellationToken.None);
                // ASSERT-2 (not loaded and no exception was thrown)
                Assert.IsNull(result);

                // ACTION-3: Staging
                await TDP.SetFileStagingAsync(fileId, true);
                result = await DP.LoadBinaryPropertyValueAsync(versionId, propertyTypeId, CancellationToken.None);
                // ASSERT-3 (not loaded and no exception was thrown)
                Assert.IsNull(result);

                // ACTION-4: Missing File (inconsistent but need to be handled)
                await TDP.DeleteFileAsync(fileId);

                result = await DP.LoadBinaryPropertyValueAsync(versionId, propertyTypeId, CancellationToken.None);
                // ASSERT-4 (not loaded and no exception was thrown)
                Assert.IsNull(result);
            });
        }

        public async Task DP_NodeEnumerator()
        {
            await IntegrationTestAsync(() =>
            {
                // Create a small subtree
                var root = CreateTestRoot();
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
                Assert.AreEqual(root.Name + ", F1, F3, F5, F6, F4, F2", names);

                return Task.CompletedTask;
            });
        }

        public async Task DP_NameSuffix()
        {
            await IntegrationTestAsync(() =>
            {
                // Create a small subtree
                var root = CreateTestRoot();
                var f1 = new SystemFolder(root) { Name = "folder(42)" }; f1.Save();

                // ACTION
                var newName = ContentNamingProvider.IncrementNameSuffixToLastName("folder(11)", f1.ParentId);

                // ASSERT
                Assert.AreEqual("folder(43)", newName);

                return Task.CompletedTask;
            });
        }

        public async Task DP_TreeSize_Root()
        {
            await IntegrationTestAsync(async () =>
            {
                // ACTION
                var size = await DP.GetTreeSizeAsync("/Root", true, CancellationToken.None);

                // ASSERT
                var expectedSize = (long) await TDP.GetAllFileSize();
                Assert.AreEqual(expectedSize, size);
            });
        }
        public async Task DP_TreeSize_Subtree()
        {
            await IntegrationTestAsync(async () =>
            {
                // ACTION
                var size = await DP.GetTreeSizeAsync("/Root/System/Schema/ContentTypes/GenericContent/Folder", true, CancellationToken.None);

                // ASSERT
                var expectedSize = (long)await TDP.GetAllFileSizeInSubtree("/Root/System/Schema/ContentTypes/GenericContent/Folder");
                Assert.AreEqual(expectedSize, size);
            });
        }
        public async Task DP_TreeSize_Item()
        {
            await IntegrationTestAsync(async () =>
            {
                // ACTION
                var size = await DP.GetTreeSizeAsync("/Root/System/Schema/ContentTypes/GenericContent/Folder", false, CancellationToken.None);

                // ASSERT
                var expectedSize = (long)await TDP.GetFileSize("/Root/System/Schema/ContentTypes/GenericContent/Folder");
                Assert.AreEqual(expectedSize, size);
            });
        }

        /* ================================================================================================== ShortText escape */

        //UNDONE:<?:IntT:!!! Only MsSql serializes the dynamic properties
        //public async Task DP_ShortText_Escape()
        //{
        //    await IntegrationTestAsync(() =>
        //    {
        //        var properties = new PropertyType[]
        //        {
        //            PropertyType.GetByName("Domain"),
        //            PropertyType.GetByName("FullName"),
        //            PropertyType.GetByName("Email"),
        //            PropertyType.GetByName("LoginName"),
        //        };
        //        var inputValues = new[]
        //        {
        //            "Domain1",
        //            "LastName\tFirstName",
        //            "a@b.c \\ d@e.f",
        //            "asdf\\\r\nqwer",
        //        };
        //        var data = new Dictionary<PropertyType, object>();
        //        for (int i = 0; i < inputValues.Length; i++)
        //        {
        //            data.Add(properties[i], inputValues[i]);
        //        }


        //        // ACTION
        //        var serialized = DP.SerializeDynamicProperties(data);
        //        var deserialized = DP.DeserializeDynamicProperties(serialized);

        //        // ASSERT
        //        var keys = deserialized.Keys.ToArray();
        //        var values = deserialized.Values.ToArray();
        //        for (int i = 0; i < inputValues.Length; i++)
        //        {
        //            Assert.AreEqual(properties[i], keys[i]);
        //            Assert.AreEqual(inputValues[i], values[i]);
        //        }

        //        return Task.CompletedTask;
        //    });
        //}

        /* ================================================================================================== NodeQuery */

        public async Task DP_NodeQuery_InstanceCount()
        {
            await IntegrationTestAsync(async () =>
            {
                var expectedFolderCount = CreateSafeContentQuery("+Type:Folder .COUNTONLY").Execute().Count;
                var expectedSystemFolderCount = CreateSafeContentQuery("+Type:SystemFolder .COUNTONLY").Execute().Count;
                var expectedAggregated = expectedFolderCount + expectedSystemFolderCount;

                var folderTypeTypeId = ActiveSchema.NodeTypes["Folder"].Id;
                var systemFolderTypeTypeId = ActiveSchema.NodeTypes["SystemFolder"].Id;

                // ACTION-1
                var actualFolderCount1 = await DP.InstanceCountAsync(new[] { folderTypeTypeId }, CancellationToken.None);
                var actualSystemFolderCount1 = await DP.InstanceCountAsync(new[] { systemFolderTypeTypeId }, CancellationToken.None);
                var actualAggregated1 = await DP.InstanceCountAsync(new[] { folderTypeTypeId, systemFolderTypeTypeId }, CancellationToken.None);

                // ASSERT
                Assert.AreEqual(expectedFolderCount, actualFolderCount1);
                Assert.AreEqual(expectedSystemFolderCount, actualSystemFolderCount1);
                Assert.AreEqual(expectedAggregated, actualAggregated1);

                // add a systemFolder to check inheritance in counts
                var folder = new SystemFolder(Repository.Root) { Name = "Folder-1" };
                folder.Save();

                // ACTION-1
                var actualFolderCount2 = await DP.InstanceCountAsync(new[] { folderTypeTypeId }, CancellationToken.None);
                var actualSystemFolderCount2 = await DP.InstanceCountAsync(new[] { systemFolderTypeTypeId }, CancellationToken.None);
                var actualAggregated2 = await DP.InstanceCountAsync(new[] { folderTypeTypeId, systemFolderTypeTypeId }, CancellationToken.None);

                // ASSERT
                Assert.AreEqual(expectedFolderCount, actualFolderCount2);
                Assert.AreEqual(expectedSystemFolderCount + 1, actualSystemFolderCount2);
                Assert.AreEqual(expectedAggregated + 1, actualAggregated2);

            });
        }
        public async Task DP_NodeQuery_ChildrenIdentifiers()
        {
            await IntegrationTestAsync(async () =>
            {
                var expected = CreateSafeContentQuery("+InFolder:/Root").Execute().Identifiers;

                // ACTION
                var result = await DP.GetChildrenIdentifiersAsync(Repository.Root.Id, CancellationToken.None);

                // ASSERT
                AssertSequenceEqual(expected.OrderBy(x => x), result.OrderBy(x => x));
            });
        }

        public async Task DP_NodeQuery_QueryNodesByTypeAndPathAndName()
        {
            await IntegrationTestAsync(async () =>
            {
                var r = new SystemFolder(Repository.Root) { Name = "R" }; r.Save();
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

                // ACTION-1 (type: 1, path: 1, name: -)
                var nodeTypeIds = new[] { typeF };
                var pathStart = new[] { "/Root/R/A" };
                var result = await DP.QueryNodesByTypeAndPathAndNameAsync(nodeTypeIds, pathStart, true, null, CancellationToken.None);
                // ASSERT-1
                var expected = CreateSafeContentQuery("+Type:Folder +InTree:'/Root/R/A' .SORT:Path .AUTOFILTERS:OFF")
                    .Execute().Identifiers.Skip(1).ToArray();
                Assert.AreEqual(3, expected.Length);
                AssertSequenceEqual(expected, result);

                // ACTION-2 (type: 2, path: 1, name: -)
                nodeTypeIds = new[] { typeF, typeS };
                pathStart = new[] { "/Root/R/A" };
                result = await DP.QueryNodesByTypeAndPathAndNameAsync(nodeTypeIds, pathStart, true, null, CancellationToken.None);
                // ASSERT-2
                expected = CreateSafeContentQuery("+Type:(Folder SystemFolder) +InTree:/Root/R/A .SORT:Path .AUTOFILTERS:OFF")
                    .Execute().Identifiers.Skip(1).ToArray();
                Assert.AreEqual(6, expected.Length);
                AssertSequenceEqual(expected, result);

                // ACTION-3 (type: 1, path: 2, name: -)
                nodeTypeIds = new[] { typeF };
                pathStart = new[] { "/Root/R/A", "/Root/R/B" };
                result = await DP.QueryNodesByTypeAndPathAndNameAsync(nodeTypeIds, pathStart, true, null, CancellationToken.None);
                // ASSERT-3
                expected = CreateSafeContentQuery("+Type:Folder +InTree:/Root/R/A .SORT:Path .AUTOFILTERS:OFF")
                    .Execute().Identifiers.Skip(1)
                    .Union(CreateSafeContentQuery("+Type:Folder +InTree:/Root/R/B .SORT:Path .AUTOFILTERS:OFF")
                        .Execute().Identifiers.Skip(1)).ToArray();
                Assert.AreEqual(6, expected.Length);
                AssertSequenceEqual(expected, result);

                // ACTION-4 (type: -, path: 1, name: A)
                pathStart = new[] { "/Root/R" };
                result = await DP.QueryNodesByTypeAndPathAndNameAsync(null, pathStart, true, "A", CancellationToken.None);
                // ASSERT-4
                expected = CreateSafeContentQuery("+Name:A +InTree:/Root/R .SORT:Path .AUTOFILTERS:OFF").Execute().Identifiers.ToArray();
                Assert.AreEqual(7, expected.Length);
                AssertSequenceEqual(expected, result);
            });
        }
        public async Task DP_NodeQuery_QueryNodesByTypeAndPathAndProperty()
        {
            var contentType1 = "TestContent1";
            var contentType2 = "TestContent2";
            var ctd1 = $"<ContentType name='{contentType1}' parentType='SystemFolder'" + $@"
             handler='SenseNet.ContentRepository.SystemFolder'
             xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <AllowedChildTypes>Page,Folder,{contentType1},{contentType2}</AllowedChildTypes>
  <Fields>
    <Field name='Str' type='ShortText'/>
    <Field name='Int' type='Integer'/>
  </Fields>
</ContentType>
";
            var ctd2 = $"<ContentType name='{contentType2}' parentType='SystemFolder'" + $@"
             handler='SenseNet.ContentRepository.SystemFolder'
             xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <AllowedChildTypes>Page,Folder,{contentType1},{contentType2}</AllowedChildTypes>
  <Fields>
    <Field name='Str' type='ShortText'/>
    <Field name='Int' type='Integer'/>
  </Fields>
</ContentType>
";
            SystemFolder root = null;
            await IntegrationTestAsync(async () =>
            {
                ContentTypeInstaller.InstallContentType(ctd1, ctd2);
                var unused = ContentType.GetByName(contentType1); // preload schema

                root = new SystemFolder(Repository.Root) {Name = "R"};
                root.Save();
                var ra = new GenericContent(root, contentType1) {Name = "A", ["Int"] = 42, ["Str"] = "str1"};
                ra.Save();
                var raf = new GenericContent(ra, contentType1) {Name = "F"};
                raf.Save();
                var rafa = new GenericContent(raf, contentType1) {Name = "A", ["Int"] = 42, ["Str"] = "str1"};
                rafa.Save();
                var rafb = new GenericContent(raf, contentType1) {Name = "B", ["Int"] = 43, ["Str"] = "str2"};
                rafb.Save();
                var ras = new GenericContent(ra, contentType2) {Name = "S"};
                ras.Save();
                var rasa = new GenericContent(ras, contentType2) {Name = "A", ["Int"] = 42, ["Str"] = "str1"};
                rasa.Save();
                var rasb = new GenericContent(ras, contentType2) {Name = "B", ["Int"] = 43, ["Str"] = "str2"};
                rasb.Save();
                var rb = new GenericContent(root, contentType1) {Name = "B", ["Int"] = 43, ["Str"] = "str2"};
                rb.Save();
                var rbf = new GenericContent(rb, contentType1) {Name = "F"};
                rbf.Save();
                var rbfa = new GenericContent(rbf, contentType1) {Name = "A", ["Int"] = 42, ["Str"] = "str1"};
                rbfa.Save();
                var rbfb = new GenericContent(rbf, contentType1) {Name = "B", ["Int"] = 43, ["Str"] = "str2"};
                rbfb.Save();
                var rbs = new GenericContent(rb, contentType2) {Name = "S"};
                rbs.Save();
                var rbsa = new GenericContent(rbs, contentType2) {Name = "A", ["Int"] = 42, ["Str"] = "str1"};
                rbsa.Save();
                var rbsb = new GenericContent(rbs, contentType2) {Name = "B", ["Int"] = 43, ["Str"] = "str2"};
                rbsb.Save();
                var rc = new GenericContent(root, contentType1) {Name = "C"};
                rc.Save();
                var rcf = new GenericContent(rc, contentType1) {Name = "F"};
                rcf.Save();
                var rcfa = new GenericContent(rcf, contentType1) {Name = "A", ["Int"] = 42, ["Str"] = "str1"};
                rcfa.Save();
                var rcfb = new GenericContent(rcf, contentType1) {Name = "B", ["Int"] = 43, ["Str"] = "str2"};
                rcfb.Save();
                var rcs = new GenericContent(rc, contentType2) {Name = "S"};
                rcs.Save();
                var rcsa = new GenericContent(rcs, contentType2) {Name = "A", ["Int"] = 42, ["Str"] = "str1"};
                rcsa.Save();
                var rcsb = new GenericContent(rcs, contentType2) {Name = "B", ["Int"] = 43, ["Str"] = "str2"};
                rcsb.Save();

                var type1 = ActiveSchema.NodeTypes[contentType1].Id;
                var type2 = ActiveSchema.NodeTypes[contentType2].Id;
                var property1 = new List<QueryPropertyData>
                    {new QueryPropertyData {PropertyName = "Int", QueryOperator = Operator.Equal, Value = 42}};
                var property2 = new List<QueryPropertyData>
                    {new QueryPropertyData {PropertyName = "Str", QueryOperator = Operator.Equal, Value = "str1"}};

                // ACTION-1 (type: 1, path: 1, prop: -)
                var result = await DP.QueryNodesByTypeAndPathAndPropertyAsync(new[] {type1}, "/Root/R/A", true, null,
                    CancellationToken.None);
                // ASSERT-1
                // Skip(1) because the NodeQuery does not contain the subtree root.
                var expected = CreateSafeContentQuery($"+Type:{contentType1} +InTree:/Root/R/A .SORT:Path")
                    .Execute().Identifiers.Skip(1).ToArray();
                Assert.AreEqual(3, expected.Length);
                AssertSequenceEqual(expected, result);

                // ACTION-2 (type: 2, path: 1, prop: -)
                result = await DP.QueryNodesByTypeAndPathAndPropertyAsync(new[] {type1, type2}, "/Root/R/A", true, null,
                    CancellationToken.None);
                // ASSERT-2
                // Skip(1) because the NodeQuery does not contain the subtree root.
                expected = CreateSafeContentQuery($"+Type:({contentType1} {contentType2}) +InTree:/Root/R/A .SORT:Path")
                    .Execute().Identifiers.Skip(1).ToArray();
                Assert.AreEqual(6, expected.Length);
                AssertSequenceEqual(expected, result);

                // ACTION-3 (type: 1, path: 1, prop: Int:42)
                result = await DP.QueryNodesByTypeAndPathAndPropertyAsync(new[] {type1}, "/Root/R/A", true, property1,
                    CancellationToken.None);
                // ASSERT-3
                // Skip(1) because the NodeQuery does not contain the subtree root.
                expected = CreateSafeContentQuery($"+Int:42 +InTree:/Root/R/A +Type:({contentType1}).SORT:Path")
                    .Execute().Identifiers.Skip(1).ToArray();
                Assert.AreEqual(1, expected.Length);
                AssertSequenceEqual(expected, result);

                // ACTION-4 (type: -, path: 1,  prop: Int:42)
                result = await DP.QueryNodesByTypeAndPathAndPropertyAsync(null, "/Root/R", true, property1,
                    CancellationToken.None);
                // ASSERT-4
                // Skip(1) is unnecessary because the subtree root is not a query hit.
                expected = CreateSafeContentQuery("+Int:42 +InTree:/Root/R .SORT:Path").Execute().Identifiers.ToArray();
                Assert.AreEqual(7, expected.Length);
                AssertSequenceEqual(expected, result);

                // ACTION-5 (type: 1, path: 1, prop: Str:"str1")
                result = await DP.QueryNodesByTypeAndPathAndPropertyAsync(new[] {type1}, "/Root/R/A", true, property2,
                    CancellationToken.None);
                // ASSERT-5
                // Skip(1) because the NodeQuery does not contain the subtree root.
                expected = CreateSafeContentQuery($"+Str:str1 +InTree:/Root/R/A +Type:({contentType1}).SORT:Path")
                    .Execute().Identifiers.Skip(1).ToArray();
                Assert.AreEqual(1, expected.Length);
                AssertSequenceEqual(expected, result);

                // ACTION-6 (type: -, path: 1,  prop: Str:"str2")
                result = await DP.QueryNodesByTypeAndPathAndPropertyAsync(null, "/Root/R", true, property2,
                    CancellationToken.None);
                // ASSERT-6
                // Skip(1) is unnecessary because the subtree root is not a query hit.
                expected = CreateSafeContentQuery("+Str:str1 +InTree:/Root/R .SORT:Path").Execute().Identifiers
                    .ToArray();
                Assert.AreEqual(7, expected.Length);
                AssertSequenceEqual(expected, result);
            });
        }
        public async Task DP_NodeQuery_QueryNodesByReferenceAndType()
        {
            var contentType1 = "TestContent1";
            var contentType2 = "TestContent2";
            var ctd1 = $"<ContentType name='{contentType1}' parentType='SystemFolder'" + $@"
             handler='SenseNet.ContentRepository.SystemFolder'
             xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <AllowedChildTypes>Page,Folder,{contentType1},{contentType2}</AllowedChildTypes>
  <Fields>
    <Field name='Ref' type='Reference'/>
  </Fields>
</ContentType>
";
            var ctd2 = $"<ContentType name='{contentType2}' parentType='SystemFolder'" + $@"
             handler='SenseNet.ContentRepository.SystemFolder'
             xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <AllowedChildTypes>Page,Folder,{contentType1},{contentType2}</AllowedChildTypes>
  <Fields>
    <Field name='Ref' type='Reference'/>
  </Fields>
</ContentType>
";
            SystemFolder root = null;
            await IntegrationTestAsync(async () =>
            {
                ContentTypeInstaller.InstallContentType(ctd1, ctd2);
                var unused = ContentType.GetByName(contentType1); // preload schema

                root = new SystemFolder(Repository.Root) {Name = "TestRoot"};
                root.Save();
                var refs = new GenericContent(root, contentType1) {Name = "Refs"};
                refs.Save();
                var ref1 = new GenericContent(refs, contentType1) {Name = "R1"};
                ref1.Save();
                var ref2 = new GenericContent(refs, contentType2) {Name = "R2"};
                ref2.Save();

                var r1 = new NodeList<Node>(new[] {ref1.Id});
                var r2 = new NodeList<Node>(new[] {ref2.Id});
                var n1 = new GenericContent(root, contentType1) {Name = "N1", ["Ref"] = r1};
                n1.Save();
                var n2 = new GenericContent(root, contentType1) {Name = "N2", ["Ref"] = r2};
                n2.Save();
                var n3 = new GenericContent(root, contentType2) {Name = "N3", ["Ref"] = r1};
                n3.Save();
                var n4 = new GenericContent(root, contentType2) {Name = "N4", ["Ref"] = r2};
                n4.Save();

                var type1 = ActiveSchema.NodeTypes[contentType1].Id;
                var type2 = ActiveSchema.NodeTypes[contentType2].Id;

                // ACTION-1 (type: T1, ref: R1)
                var result =
                    await DP.QueryNodesByReferenceAndTypeAsync("Ref", ref1.Id, new[] {type1}, CancellationToken.None);
                // ASSERT-1
                //((InMemorySearchEngine)Providers.Instance.SearchEngine).Index.Save("D:\\index-asdf.txt");
                var expected = CreateSafeContentQuery($"+Type:{contentType1} +Ref:{ref1.Id} .SORT:Id")
                    .Execute().Identifiers.ToArray();
                Assert.AreEqual(1, expected.Length);
                AssertSequenceEqual(expected, result.OrderBy(x => x));

                // ACTION-2 (type: T1,T2, ref: R1)
                result = await DP.QueryNodesByReferenceAndTypeAsync("Ref", ref1.Id, new[] {type1, type2},
                    CancellationToken.None);
                // ASSERT-1
                expected = CreateSafeContentQuery($"+Type:({contentType1} {contentType2}) +Ref:{ref1.Id} .SORT:Id")
                    .Execute().Identifiers.ToArray();
                Assert.AreEqual(2, expected.Length);
                AssertSequenceEqual(expected, result.OrderBy(x => x));
            });
        }

        /* ================================================================================================== TreeLock */

        public async Task DP_LoadEntityTree()
        {
            await IntegrationTestAsync(async () =>
            {
                // ACTION
                var treeData = await DataStore.LoadEntityTreeAsync(CancellationToken.None);

                // ASSERT check the right ordering: every node follows it's parent node.
                var tree = new Dictionary<int, EntityTreeNodeData>();
                foreach (var node in treeData)
                {
                    if (node.ParentId != 0)
                        if (!tree.ContainsKey(node.ParentId))
                            Assert.Fail($"The parent is not yet loaded. Id: {node.Id}, ParentId: {node.ParentId}");
                    tree.Add(node.Id, node);
                }
            });
        }
        public async Task DP_TreeLock()
        {
            await IntegrationTestAsync(async () =>
            {
                var path = "/Root/Folder-1";
                var childPath = "/root/folder-1/folder-2";
                var anotherPath = "/Root/Folder-2";
                var timeLimit = DateTime.UtcNow.AddHours(-8.0);

                // Pre check: there is no lock
                var tlocks = await DP.LoadAllTreeLocksAsync(CancellationToken.None);
                Assert.AreEqual(0, tlocks.Count);

                // ACTION: create a lock
                var tlockId = await DP.AcquireTreeLockAsync(path, timeLimit, CancellationToken.None);

                // Check: there is one lock ant it matches
                tlocks = await DP.LoadAllTreeLocksAsync(CancellationToken.None);
                Assert.AreEqual(1, tlocks.Count);
                Assert.AreEqual(tlockId, tlocks.First().Key);
                Assert.AreEqual(path, tlocks.First().Value);

                // Check: path and subpath are locked
                Assert.IsTrue(await DP.IsTreeLockedAsync(path, timeLimit, CancellationToken.None));
                Assert.IsTrue(await DP.IsTreeLockedAsync(childPath, timeLimit, CancellationToken.None));

                // Check: outer path is not locked
                Assert.IsFalse(await DP.IsTreeLockedAsync(anotherPath, timeLimit, CancellationToken.None));

                // ACTION: try to create a lock fot a subpath
                var childLlockId = await DP.AcquireTreeLockAsync(childPath, timeLimit, CancellationToken.None);

                // Check: subPath cannot be locked
                Assert.AreEqual(0, childLlockId);

                // Check: there is still only one lock
                tlocks = await DP.LoadAllTreeLocksAsync(CancellationToken.None);
                Assert.AreEqual(1, tlocks.Count);
                Assert.AreEqual(tlockId, tlocks.First().Key);
                Assert.AreEqual(path, tlocks.First().Value);

                // ACTION: Release the lock
                await DP.ReleaseTreeLockAsync(new[] { tlockId }, CancellationToken.None);

                // Check: there is no lock
                tlocks = await DP.LoadAllTreeLocksAsync(CancellationToken.None);
                Assert.AreEqual(0, tlocks.Count);

                // Check: path and subpath are not locked
                Assert.IsFalse(await DP.IsTreeLockedAsync(path, timeLimit, CancellationToken.None));
                Assert.IsFalse(await DP.IsTreeLockedAsync(childPath, timeLimit, CancellationToken.None));

            });
        }

        /* ================================================================================================== IndexDocument */

        public async Task DP_LoadIndexDocuments()
        {
            const string fileContent = "File content.";
            const string testRootPath = "/Root/TestRoot";

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

            await IntegrationTestAsync(async () =>
            {
                var fileNodeType = ActiveSchema.NodeTypes["File"];
                var systemFolderType = ActiveSchema.NodeTypes["SystemFolder"];

                // ARRANGE
                CreateStructure();
                var testRoot = Node.Load<SystemFolder>(testRootPath);
                var testRootChildren = testRoot.Children.ToArray();

                // ACTION
                var oneVersion = await DP.LoadIndexDocumentsAsync(new[] { testRoot.VersionId }, CancellationToken.None);
                var moreVersions = (await DP.LoadIndexDocumentsAsync(testRootChildren.Select(x => x.VersionId).ToArray(), CancellationToken.None)).ToArray();
                var subTreeAll = DP.LoadIndexDocumentsAsync(testRootPath, new int[0]).ToArray();
                var onlyFiles = DP.LoadIndexDocumentsAsync(testRootPath, new[] { fileNodeType.Id }).ToArray();
                var onlySystemFolders = DP.LoadIndexDocumentsAsync(testRootPath, new[] { systemFolderType.Id }).ToArray();

                // ASSERT
                Assert.AreEqual(testRootPath, oneVersion.First().Path);
                Assert.AreEqual(3, moreVersions.Length);
                Assert.AreEqual(10, subTreeAll.Length);
                Assert.AreEqual(3, onlyFiles.Length);
                Assert.AreEqual(7, onlySystemFolders.Length);
            });
        }
        public async Task DP_SaveIndexDocumentById()
        {
            await IntegrationTestAsync(async () =>
            {
                var node = CreateTestRoot();
                var versionIds = new[] { node.VersionId };
                var loadResult = await DP.LoadIndexDocumentsAsync(versionIds, CancellationToken.None);
                var docData = loadResult.First();

                // ACTION
                docData.IndexDocument.Add(
                    new IndexField("TestField", "TestValue",
                        IndexingMode.Default, IndexStoringMode.Default, IndexTermVector.Default));
                await DP.SaveIndexDocumentAsync(node.VersionId, docData.IndexDocument.Serialize(), CancellationToken.None);

                // ASSERT (check additional field existence)
                loadResult = await DP.LoadIndexDocumentsAsync(versionIds, CancellationToken.None);
                docData = loadResult.First();
                var testField = docData.IndexDocument.FirstOrDefault(x => x.Name == "TestField");
                Assert.IsNotNull(testField);
                Assert.AreEqual("TestValue", testField.StringValue);
            });
        }

        /* ================================================================================================== IndexingActivities */

        public async Task DP_IA_GetLastIndexingActivityId()
        {
            await IndexingActivityTest(async (firstId, lastId) =>
            {
                var result = await DP.GetLastIndexingActivityIdAsync(CancellationToken.None);
                Assert.AreEqual(lastId, result);
            });
        }
        public async Task DP_IA_LoadIndexingActivities_Page()
        {
            await IndexingActivityTest(async (firstId, lastId) =>
            {
                var from = lastId - 10;
                var to = lastId;
                var count = 5;
                var factory = new TestIndexingActivityFactory();

                // ACTION
                var result = await DP.LoadIndexingActivitiesAsync(from, to, count, false, factory, CancellationToken.None);

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
        public async Task DP_IA_LoadIndexingActivities_PageUnprocessed()
        {
            await IndexingActivityTest(async (firstId, lastId) =>
            {
                var from = lastId - 10;
                var to = lastId;
                var count = 5;
                var factory = new TestIndexingActivityFactory();

                // ACTION
                var result = await DP.LoadIndexingActivitiesAsync(from, to, count, true, factory, CancellationToken.None);

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
        public async Task DP_IA_LoadIndexingActivities_Gaps()
        {
            await IndexingActivityTest(async (firstId, lastId) =>
            {
                var gaps = new[] { lastId - 10, lastId - 9, lastId - 5 };
                var factory = new TestIndexingActivityFactory();

                // ACTION
                var result = await DP.LoadIndexingActivitiesAsync(gaps, false, factory, CancellationToken.None);

                // ASSERT
                Assert.AreEqual(3, result.Length);
                Assert.AreEqual(100, result[0].NodeId);
                Assert.AreEqual(101, result[1].NodeId);
                Assert.AreEqual(105, result[2].NodeId);
            });
        }
        public async Task DP_IA_LoadIndexingActivities_Executable()
        {
            await IndexingActivityTest(async (firstId, lastId) =>
            {
                var factory = new TestIndexingActivityFactory();
                var timeout = 120;

                // ACTION
                var result = await DP.LoadExecutableIndexingActivitiesAsync(factory, 10, timeout, null, CancellationToken.None);
                Assert.IsNotNull(result.FinishedActivitiyIds);
                Assert.AreEqual(0, result.FinishedActivitiyIds.Length);

                // ASSERT
                var expectedIds = "45,46,50,51,52,53,55,100,101,102";
                Assert.AreEqual(expectedIds, string.Join(",", result.Activities.Select(x => x.NodeId.ToString())));
            });
        }
        public async Task DP_IA_LoadIndexingActivities_ExecutableAndFinished()
        {
            await IndexingActivityTest(async (firstId, lastId) =>
            {
                var factory = new TestIndexingActivityFactory();
                var timeout = 120;
                var waitingActivityIds = new[] { firstId, firstId + 1, firstId + 2, firstId + 3, firstId + 4, firstId + 5 };

                // ACTION-2
                var result = await DP.LoadExecutableIndexingActivitiesAsync(factory, 10, timeout, waitingActivityIds, CancellationToken.None);

                // ASSERT
                Assert.AreEqual(3, result.FinishedActivitiyIds.Length);
                Assert.AreEqual(firstId, result.FinishedActivitiyIds[0]);
                Assert.AreEqual(firstId + 1, result.FinishedActivitiyIds[1]);
                Assert.AreEqual(firstId + 2, result.FinishedActivitiyIds[2]);

                var expectedIds = "45,46,50,51,52,53,55,100,101,102";
                Assert.AreEqual(expectedIds, string.Join(",", result.Activities.Select(x => x.NodeId.ToString())));
            });
        }
        public async Task DP_IA_UpdateRunningState()
        {
            await IndexingActivityTest(async (firstId, lastId) =>
            {
                var gaps = new[] { lastId - 10, lastId - 9, lastId - 8 };
                var factory = new TestIndexingActivityFactory();
                var before = await DP.LoadIndexingActivitiesAsync(gaps, false, factory, CancellationToken.None);
                Assert.AreEqual(IndexingActivityRunningState.Waiting, before[0].RunningState);
                Assert.AreEqual(IndexingActivityRunningState.Waiting, before[1].RunningState);
                Assert.AreEqual(IndexingActivityRunningState.Waiting, before[2].RunningState);

                // ACTION
                await DP.UpdateIndexingActivityRunningStateAsync(gaps[0], IndexingActivityRunningState.Done, CancellationToken.None);
                await DP.UpdateIndexingActivityRunningStateAsync(gaps[1], IndexingActivityRunningState.Running, CancellationToken.None);

                // ASSERT
                var after = await DP.LoadIndexingActivitiesAsync(gaps, false, factory, CancellationToken.None);
                Assert.AreEqual(IndexingActivityRunningState.Done, after[0].RunningState);
                Assert.AreEqual(IndexingActivityRunningState.Running, after[1].RunningState);
                Assert.AreEqual(IndexingActivityRunningState.Waiting, after[2].RunningState);
            });
        }
        public async Task DP_IA_RefreshLockTime()
        {
            await IndexingActivityTest(async (firstId, lastId) =>
            {
                var startTime = DateTime.UtcNow;

                var activityIds = new[] { firstId + 5, firstId + 6 };
                var factory = new TestIndexingActivityFactory();
                var before = await DP.LoadIndexingActivitiesAsync(activityIds, false, factory, CancellationToken.None);
                Assert.AreEqual(47, before[0].NodeId);
                Assert.AreEqual(48, before[1].NodeId);

                // ACTION
                await DP.RefreshIndexingActivityLockTimeAsync(activityIds, CancellationToken.None);

                // ASSERT
                var after = await DP.LoadIndexingActivitiesAsync(activityIds, false, factory, CancellationToken.None);
                Assert.AreEqual(47, before[0].NodeId);
                Assert.AreEqual(48, before[1].NodeId);
                Assert.IsTrue(after[0].LockTime >= startTime);
                Assert.IsTrue(after[1].LockTime >= startTime);
            });
        }
        public async Task DP_IA_DeleteFinished()
        {
            await IndexingActivityTest(async (firstId, lastId) =>
            {
                // ACTION
                await DP.DeleteFinishedIndexingActivitiesAsync(CancellationToken.None);

                // ASSERT
                var result = await DP.LoadIndexingActivitiesAsync(firstId, lastId, 100, false, new TestIndexingActivityFactory(), CancellationToken.None);
                Assert.AreEqual(lastId - firstId - 2, result.Length);
                Assert.AreEqual(firstId + 3, result.First().Id);
            });
        }
        public async Task DP_IA_LoadFull()
        {
            await IntegrationTestAsync(async () =>
            {
                await DP.DeleteAllIndexingActivitiesAsync(CancellationToken.None);
                var node = CreateFolder(Repository.Root, "Folder-1");

                // ACTION
                var result = await DP.LoadIndexingActivitiesAsync(0, 1000, 1000, false, new TestIndexingActivityFactory(), CancellationToken.None);

                // ASSERT (IndexDocument loaded)
                Assert.AreEqual(1, result.Length);
                Assert.AreEqual(node.VersionId, result[0].IndexDocumentData.IndexDocument.VersionId);
            });
        }

        #region Tools for IndexingActivities
        private async Task IndexingActivityTest(Func<int, int, Task> callback)
        {
            await IntegrationTestAsync(async () =>
            {
                await DP.DeleteAllIndexingActivitiesAsync(CancellationToken.None);

                var now = DateTime.UtcNow;
                var firstId = await CreateActivityAsync(42, "/R/42", "Done");             // 1
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
                var lastId = await CreateActivityAsync(110, "/R/110", "Waiting");         // 27

                try
                {
                    await callback(firstId, lastId);
                }
                finally
                {
                    await DP.DeleteAllIndexingActivitiesAsync(CancellationToken.None);
                }
            });
        }
        private async Task<int> CreateActivityAsync(int nodeId, string path, string runningState, DateTime? lockTime = null)
        {
            var activity = new TestIndexingActivity(nodeId, path, runningState, lockTime);
            await DP.RegisterIndexingActivityAsync(activity, CancellationToken.None);
            return activity.Id;
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
                    (IndexingActivityRunningState)Enum.Parse(typeof(IndexingActivityRunningState), runningState, true);
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

        public async Task DP_CopyAndUpdateNode_Rename()
        {
            await IntegrationTestAsync(async () =>
            {
                var node = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString(), Index = 42 };
                node.Save();
                var childNode = CreateFolder(node, "Folder-2");
                var version1 = node.Version.ToString();
                var versionId1 = node.VersionId;
                node.CheckOut();
                var versionId2 = node.VersionId;
                var originalPath = node.Path;

                node = Node.Load<SystemFolder>(node.Id);
                node.Index++;
                var expectedName = "RENAMED-" + node.Name;
                node.Name = expectedName;
                node.Data.Path = RepositoryPath.Combine(node.ParentPath, node.Name); // ApplySettings
                node.Version = VersionNumber.Parse(version1); // ApplySettings
                var nodeData = node.Data;
                var nodeHeadData = nodeData.GetNodeHeadData();
                var versionData = nodeData.GetVersionData();
                var dynamicData = nodeData.GetDynamicData(false);
                var versionIdsToDelete = new[] { versionId2 };
                var expectedVersionId = versionId1;

                // ACTION: simulate a modification, rename and CheckIn on a checked-out, not-versioned node (V2.0.L -> V1.0.A).
                await DataStore.DataProvider
                    .CopyAndUpdateNodeAsync(nodeHeadData, versionData, dynamicData, versionIdsToDelete, CancellationToken.None,
                        expectedVersionId, originalPath);

                // ASSERT
                Cache.Reset();
                var reloaded = Node.Load<SystemFolder>(node.Id);
                Assert.AreEqual(expectedName, reloaded.Name);
                reloaded = Node.Load<SystemFolder>(childNode.Id);
                Assert.AreEqual($"/Root/{expectedName}/Folder-2", reloaded.Path);
            });
        }
        public async Task DP_LoadNodes()
        {
            await IntegrationTestAsync(async () =>
            {
                var expected = new[] { Repository.Root.VersionId, User.Administrator.VersionId };
                var versionIds = new[] { Repository.Root.VersionId, 999999, User.Administrator.VersionId };

                // ACTION
                var loadResult = await DP.LoadNodesAsync(versionIds, CancellationToken.None);

                // ASSERT
                var actual = loadResult.Select(x => x.VersionId);
                AssertSequenceEqual(expected.OrderBy(x => x), actual.OrderBy(x => x));
            });
        }
        public async Task DP_LoadNodeHeadByVersionId_Missing()
        {
            await IntegrationTestAsync(async () =>
            {
                // ACTION
                var result = await DP.LoadNodeHeadByVersionIdAsync(99999, CancellationToken.None);

                // ASSERT (returns null instead of throw any exception)
                Assert.IsNull(result);
            });
        }
        public async Task DP_NodeAndVersion_CountsAndTimestamps()
        {
            await IntegrationTestAsync(async () =>
            {
                // ACTIONS
                var allNodeCountBefore = await DP.GetNodeCountAsync(null, CancellationToken.None);
                var allVersionCountBefore = await DP.GetVersionCountAsync(null, CancellationToken.None);

                var node = CreateTestRoot();
                var child = CreateFolder(node, "Folder-2");
                child.CheckOut();

                var nodeCount = await DP.GetNodeCountAsync(node.Path, CancellationToken.None);
                var versionCount = await DP.GetVersionCountAsync(node.Path, CancellationToken.None);
                var allNodeCountAfter = await DP.GetNodeCountAsync(null, CancellationToken.None);
                var allVersionCountAfter = await DP.GetVersionCountAsync(null, CancellationToken.None);

                node = Node.Load<SystemFolder>(node.Id);
                var nodeTimeStamp = (await TDP.GetNodeHeadDataAsync(node.Id)).Timestamp;
                var versionTimeStamp = (await TDP.GetVersionDataAsync(node.VersionId)).Timestamp;

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

        public async Task DP_Error_InsertNode_AlreadyExists()
        {
            await IntegrationTestAsync(async () =>
            {
                var newNode = CreateTestRoot();
                try
                {
                    var node = new SystemFolder(Repository.Root) { Name = newNode.Name };
                    var nodeData = node.Data;
                    nodeData.Path = RepositoryPath.Combine(node.ParentPath, node.Name);
                    var nodeHeadData = nodeData.GetNodeHeadData();
                    var versionData = nodeData.GetVersionData();
                    var dynamicData = nodeData.GetDynamicData(false);

                    // ACTION
                    await DP.InsertNodeAsync(nodeHeadData, versionData, dynamicData, CancellationToken.None);
                    Assert.Fail("NodeAlreadyExistsException was not thrown.");
                }
                catch (NodeAlreadyExistsException)
                {
                    // ignored
                }
            });
        }

        public async Task DP_Error_UpdateNode_Deleted()
        {
            await IntegrationTestAsync(async () =>
            {
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
                    await DP.UpdateNodeAsync(nodeHeadData, versionData, dynamicData, versionIdsToDelete, CancellationToken.None);
                    Assert.Fail("ContentNotFoundException was not thrown.");
                }
                catch (ContentNotFoundException)
                {
                    // ignored
                }
            });
        }
        public async Task DP_Error_UpdateNode_MissingVersion()
        {
            await IntegrationTestAsync(async () =>
            {
                var newNode =
                    new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString(), Index = 42 };
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
                    await DP.UpdateNodeAsync(nodeHeadData, versionData, dynamicData, versionIdsToDelete, CancellationToken.None);
                    Assert.Fail("ContentNotFoundException was not thrown.");
                }
                catch (ContentNotFoundException)
                {
                    // ignored
                }
            });
        }
        public async Task DP_Error_UpdateNode_OutOfDate()
        {
            await IntegrationTestAsync(async () =>
            {
                var newNode =
                    new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString(), Index = 42 };
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
                    await DP.UpdateNodeAsync(nodeHeadData, versionData, dynamicData, versionIdsToDelete, CancellationToken.None);
                    Assert.Fail("NodeIsOutOfDateException was not thrown.");
                }
                catch (NodeIsOutOfDateException)
                {
                    // ignored
                }
            });
        }

        public async Task DP_Error_CopyAndUpdateNode_Deleted()
        {
            await IntegrationTestAsync(async () =>
            {
                var newNode = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString(), Index = 42 };
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
                    await DP.CopyAndUpdateNodeAsync(nodeHeadData, versionData, dynamicData, versionIdsToDelete, CancellationToken.None);
                    Assert.Fail("ContentNotFoundException was not thrown.");
                }
                catch (ContentNotFoundException)
                {
                    // ignored
                }
            });
        }
        public async Task DP_Error_CopyAndUpdateNode_MissingVersion()
        {
            await IntegrationTestAsync(async () =>
            {
                var newNode = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString(), Index = 42 };
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
                    await DP.CopyAndUpdateNodeAsync(nodeHeadData, versionData, dynamicData, versionIdsToDelete, CancellationToken.None);
                    Assert.Fail("ContentNotFoundException was not thrown.");
                }
                catch (ContentNotFoundException)
                {
                    // ignored
                }
            });
        }
        public async Task DP_Error_CopyAndUpdateNode_OutOfDate()
        {
            await IntegrationTestAsync(async () =>
            {
                var newNode = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString(), Index = 42 };
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
                    await DP.CopyAndUpdateNodeAsync(nodeHeadData, versionData, dynamicData, versionIdsToDelete, CancellationToken.None);
                    Assert.Fail("NodeIsOutOfDateException was not thrown.");
                }
                catch (NodeIsOutOfDateException)
                {
                    // ignored
                }
            });
        }

        public async Task DP_Error_UpdateNodeHead_Deleted()
        {
            await IntegrationTestAsync(async () =>
            {
                var newNode = CreateTestRoot();

                try
                {
                    var node = Node.Load<SystemFolder>(newNode.Id);
                    var nodeData = node.Data;
                    var nodeHeadData = nodeData.GetNodeHeadData();
                    var versionIdsToDelete = new int[0];

                    // ACTION
                    nodeHeadData.NodeId = 999999;
                    await DP.UpdateNodeHeadAsync(nodeHeadData, versionIdsToDelete, CancellationToken.None);
                    Assert.Fail("ContentNotFoundException was not thrown.");
                }
                catch (ContentNotFoundException)
                {
                    // ignored
                }
            });
        }
        public async Task DP_Error_UpdateNodeHead_OutOfDate()
        {
            await IntegrationTestAsync(async () =>
            {
                var newNode = CreateTestRoot();

                try
                {
                    var node = Node.Load<SystemFolder>(newNode.Id);
                    var nodeData = node.Data;
                    var nodeHeadData = nodeData.GetNodeHeadData();
                    var versionIdsToDelete = new int[0];

                    // ACTION
                    nodeHeadData.Timestamp++;
                    await DP.UpdateNodeHeadAsync(nodeHeadData, versionIdsToDelete, CancellationToken.None);
                    Assert.Fail("NodeIsOutOfDateException was not thrown.");
                }
                catch (NodeIsOutOfDateException)
                {
                    // ignored
                }
            });
        }

        public async Task DP_Error_DeleteNode()
        {
            await IntegrationTestAsync(async () =>
            {
                // Create a small subtree
                var root = CreateTestRoot();

                try
                {
                    var node = Node.Load<SystemFolder>(root.Id);
                    var nodeHeadData = node.Data.GetNodeHeadData();

                    // ACTION
                    nodeHeadData.Timestamp++;
                    await DP.DeleteNodeAsync(nodeHeadData, CancellationToken.None);
                    Assert.Fail("NodeIsOutOfDateException was not thrown.");
                }
                catch (NodeIsOutOfDateException)
                {
                    // ignored
                }
            });
        }

        public async Task DP_Error_MoveNode_MissingSource()
        {
            await IntegrationTestAsync(async () =>
            {
                var root = CreateTestRoot();
                var source = new SystemFolder(root) { Name = "Source" }; source.Save();
                var target = new SystemFolder(root) { Name = "Target" }; target.Save();

                try
                {
                    var node = Node.Load<SystemFolder>(source.Id);
                    var nodeHeadData = node.Data.GetNodeHeadData();

                    // ACTION
                    nodeHeadData.NodeId = 999999;
                    await DP.MoveNodeAsync(nodeHeadData, target.Id, CancellationToken.None);
                    Assert.Fail("ContentNotFoundException was not thrown.");
                }
                catch (ContentNotFoundException)
                {
                    // ignored
                }
            });
        }
        public async Task DP_Error_MoveNode_MissingTarget()
        {
            await IntegrationTestAsync(async () =>
            {
                var root = CreateTestRoot();
                var source = new SystemFolder(root) { Name = "Source" }; source.Save();
                var target = new SystemFolder(root) { Name = "Target" }; target.Save();

                try
                {
                    var node = Node.Load<SystemFolder>(source.Id);
                    var nodeHeadData = node.Data.GetNodeHeadData();

                    // ACTION
                    await DP.MoveNodeAsync(nodeHeadData, 999999, CancellationToken.None);
                    Assert.Fail("ContentNotFoundException was not thrown.");
                }
                catch (ContentNotFoundException)
                {
                    // ignored
                }
            });
        }
        public async Task DP_Error_MoveNode_OutOfDate()
        {
            await IntegrationTestAsync(async () =>
            {
                var root = CreateTestRoot();
                var source = new SystemFolder(root) { Name = "Source" }; source.Save();
                var target = new SystemFolder(root) { Name = "Target" }; target.Save();

                try
                {
                    var node = Node.Load<SystemFolder>(source.Id);
                    var nodeHeadData = node.Data.GetNodeHeadData();

                    // ACTION
                    nodeHeadData.Timestamp++;
                    await DP.MoveNodeAsync(nodeHeadData, target.Id, CancellationToken.None);
                    Assert.Fail("NodeIsOutOfDateException was not thrown.");
                }
                catch (NodeIsOutOfDateException)
                {
                    // ignored
                }
            });
        }

        public async Task DP_Error_QueryNodesByReferenceAndTypeAsync()
        {
            await IntegrationTestAsync(async () =>
            {
                try
                {
                    await DP.QueryNodesByReferenceAndTypeAsync(null, 1, new[] { 1 }, CancellationToken.None);
                }
                catch (ArgumentNullException)
                {
                    // ignored
                }

                try
                {
                    await DP.QueryNodesByReferenceAndTypeAsync("", 1, new[] { 1 }, CancellationToken.None);
                }
                catch (ArgumentException e)
                {
                    Assert.IsTrue(e.Message.Contains("cannot be empty"));
                }

                try
                {
                    await DP.QueryNodesByReferenceAndTypeAsync("PropertyNameThatCertainlyDoesNotExist", 1, new[] { 1 }, CancellationToken.None);
                }
                catch (ArgumentException e)
                {
                    Assert.IsTrue(e.Message.Contains("not found"));
                }
            });
        }

        /* ================================================================================================== Transaction */

        public async Task DP_Transaction_InsertNode()
        {
            await IntegrationTestAsync(async () =>
            {
                var countsBefore = (await GetDbObjectCountsAsync(null, DP, TDP)).AllCounts;
                string errorMessage = null;

                // ACTION
                try
                {
                    var newNode = new SystemFolder(Repository.Root)
                    {
                        Name = Guid.NewGuid().ToString(),
                        Description = "Description-1",
                        Index = 42
                    };
                    var nodeData = newNode.Data;
                    var nodeHeadData = nodeData.GetNodeHeadData();
                    var versionData = nodeData.GetVersionData();
                    var dynamicData = nodeData.GetDynamicData(false);
                    // Call low level API
                    await CCDP.InsertNodeAsync(nodeHeadData, versionData, dynamicData, CancellationToken.None);
                }
                catch (Exception e)
                {
                    while (e.InnerException != null)
                        e = e.InnerException;
                    errorMessage = e.Message;
                }

                // ASSERT (special error was thrown)
                Assert.AreEqual("This transaction cannot commit anything.", errorMessage);
                // ASSERT (all operation need to be rolled back)
                var countsAfter = (await GetDbObjectCountsAsync(null, DP, TDP)).AllCounts;
                Assert.AreEqual(countsBefore, countsAfter);
            });
        }
        public async Task DP_Transaction_UpdateNode()
        {
            await IntegrationTestAsync(async () =>
            {
                var newNode =
                    new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString(), Description = "Description-1", Index = 42 };
                newNode.Save();
                var nodeTimeStampBefore = newNode.NodeTimestamp;
                var versionTimeStampBefore = newNode.VersionTimestamp;
                string errorMessage = null;

                // ACTION
                try
                {
                    var node = Node.Load<SystemFolder>(newNode.Id);
                    node.Index++;
                    node.Description = "Description-MODIFIED";
                    var nodeData = node.Data;
                    var nodeHeadData = nodeData.GetNodeHeadData();
                    var versionData = nodeData.GetVersionData();
                    var dynamicData = nodeData.GetDynamicData(false);
                    var versionIdsToDelete = new int[0];
                    // Call low level API
                    await CCDP.UpdateNodeAsync(nodeHeadData, versionData, dynamicData, versionIdsToDelete, CancellationToken.None);
                }
                catch (Exception e)
                {
                    while (e.InnerException != null)
                        e = e.InnerException;
                    errorMessage = e.Message;
                }

                // ASSERT (special error was thrown)
                Assert.AreEqual("This transaction cannot commit anything.", errorMessage);
                // ASSERT (all operation need to be rolled back)
                Cache.Reset();
                var reloaded = Node.Load<SystemFolder>(newNode.Id);
                var nodeTimeStampAfter = reloaded.NodeTimestamp;
                var versionTimeStampAfter = reloaded.VersionTimestamp;
                Assert.AreEqual(nodeTimeStampBefore, nodeTimeStampAfter);
                Assert.AreEqual(versionTimeStampBefore, versionTimeStampAfter);
                Assert.AreEqual(42, reloaded.Index);
                Assert.AreEqual("Description-1", reloaded.Description);
            });
        }
        public async Task DP_Transaction_CopyAndUpdateNode()
        {
            await IntegrationTestAsync(async () =>
            {
                var newNode =
                    new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString(), Description = "Description-1", Index = 42 };
                newNode.Save();
                var version1 = newNode.Version.ToString();
                var versionId1 = newNode.VersionId;
                newNode.CheckOut();
                var version2 = newNode.Version.ToString();
                var versionId2 = newNode.VersionId;
                var countsBefore = await GetDbObjectCountsAsync(null, DP, TDP);
                string errorMessage = null;

                // ACTION: simulate a modification and CheckIn on a checked-out, not-versioned node (V2.0.L -> V1.0.A).
                try
                {
                    var node = Node.Load<SystemFolder>(newNode.Id);
                    node.Index++;
                    node.Description = "Description-MODIFIED";
                    node.Version = VersionNumber.Parse(version1); // ApplySettings
                    var nodeData = node.Data;
                    var nodeHeadData = nodeData.GetNodeHeadData();
                    var versionData = nodeData.GetVersionData();
                    var dynamicData = nodeData.GetDynamicData(false);
                    var versionIdsToDelete = new[] { versionId2 };
                    var expectedVersionId = versionId1;
                    // Call low level API
                    await CCDP.CopyAndUpdateNodeAsync(nodeHeadData, versionData, dynamicData, versionIdsToDelete, CancellationToken.None, expectedVersionId);
                }
                catch (Exception e)
                {
                    while (e.InnerException != null)
                        e = e.InnerException;
                    errorMessage = e.Message;
                }

                // ASSERT (special error was thrown)
                Assert.AreEqual("This transaction cannot commit anything.", errorMessage);
                // ASSERT (all operation need to be rolled back)
                var countsAfter = await GetDbObjectCountsAsync(null, DP, TDP);
                Cache.Reset();
                var reloaded = Node.Load<SystemFolder>(newNode.Id);
                Assert.AreEqual(countsBefore.AllCounts, countsAfter.AllCounts);
                Assert.AreEqual(version2, reloaded.Version.ToString());
                Assert.AreEqual(versionId2, reloaded.VersionId);
            });
        }
        public async Task DP_Transaction_UpdateNodeHead()
        {
            await IntegrationTestAsync(async () =>
            {
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
                var countsBefore = await GetDbObjectCountsAsync(null, DP, TDP);
                string errorMessage = null;

                // ACTION: simulate a modification and UndoCheckout on a checked-out, not-versioned node (V2.0.L -> V1.0.A).
                try
                {
                    var node = Node.Load<SystemFolder>(newNode.Id);
                    var nodeData = node.Data;
                    var nodeHeadData = nodeData.GetNodeHeadData();
                    var versionData = nodeData.GetVersionData();
                    var dynamicData = nodeData.GetDynamicData(false);
                    var versionIdsToDelete = new[] { versionId2 };
                    var currentVersionId = newNode.VersionId;
                    var expectedVersionId = versionId1;
                    // Call low level API
                    await CCDP.UpdateNodeHeadAsync(nodeHeadData, versionIdsToDelete, CancellationToken.None);
                }
                catch (Exception e)
                {
                    while (e.InnerException != null)
                        e = e.InnerException;
                    errorMessage = e.Message;
                }

                // ASSERT (special error was thrown)
                Assert.AreEqual("This transaction cannot commit anything.", errorMessage);
                // ASSERT (all operation need to be rolled back)
                var countsAfter = await GetDbObjectCountsAsync(null, DP, TDP);
                Cache.Reset();
                var reloaded = Node.Load<SystemFolder>(newNode.Id);
                Assert.AreEqual(countsBefore.AllCounts, countsAfter.AllCounts);
                Assert.AreEqual(version2, reloaded.Version.ToString());
                Assert.AreEqual(versionId2, reloaded.VersionId);
            });
        }
        public async Task DP_Transaction_MoveNode()
        {
            await IntegrationTestAsync(async () =>
            {
                // Create a small subtree
                var rootName = Guid.NewGuid().ToString();
                var root = new SystemFolder(Repository.Root) { Name = rootName }; root.Save();
                var source = new SystemFolder(root) { Name = "Source" }; source.Save();
                var target = new SystemFolder(root) { Name = "Target" }; target.Save();
                var f1 = new SystemFolder(source) { Name = "F1" }; f1.Save();
                var f2 = new SystemFolder(source) { Name = "F2" }; f2.Save();
                var f3 = new SystemFolder(f1) { Name = "F3" }; f3.Save();
                var f4 = new SystemFolder(f1) { Name = "F4" }; f4.Save();
                string errorMessage = null;

                // ACTION
                try
                {
                    var node = Node.Load<SystemFolder>(source.Id);
                    var nodeData = node.Data;
                    var nodeHeadData = nodeData.GetNodeHeadData();
                    // Call low level API
                    await CCDP.MoveNodeAsync(nodeHeadData, target.Id, CancellationToken.None);
                }
                catch (Exception e)
                {
                    while (e.InnerException != null)
                        e = e.InnerException;
                    errorMessage = e.Message;
                }

                // ASSERT (special error was thrown)
                Assert.AreEqual("This transaction cannot commit anything.", errorMessage);
                // ASSERT
                Cache.Reset();
                target = Node.Load<SystemFolder>(target.Id);
                source = Node.Load<SystemFolder>(source.Id);
                f1 = Node.Load<SystemFolder>(f1.Id);
                f2 = Node.Load<SystemFolder>(f2.Id);
                f3 = Node.Load<SystemFolder>(f3.Id);
                f4 = Node.Load<SystemFolder>(f4.Id);
                Assert.AreEqual($"/Root/{rootName}", root.Path);
                Assert.AreEqual($"/Root/{rootName}/Target", target.Path);
                Assert.AreEqual($"/Root/{rootName}/Source", source.Path);
                Assert.AreEqual($"/Root/{rootName}/Source/F1", f1.Path);
                Assert.AreEqual($"/Root/{rootName}/Source/F2", f2.Path);
                Assert.AreEqual($"/Root/{rootName}/Source/F1/F3", f3.Path);
                Assert.AreEqual($"/Root/{rootName}/Source/F1/F4", f4.Path);
            });
        }
        public async Task DP_Transaction_RenameNode()
        {
            await IntegrationTestAsync(async () =>
            {
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
                string errorMessage = null;

                // ACTION: rename root
                try
                {
                    var node = Node.Load<SystemFolder>(root.Id);
                    var originalPath = node.Path;
                    node.Name = "X";
                    node.Data.Path = node.ParentPath + "/X"; // illegal operation but this test requires
                    var nodeData = node.Data;
                    var nodeHeadData = nodeData.GetNodeHeadData();
                    var versionData = nodeData.GetVersionData();
                    var dynamicData = nodeData.GetDynamicData(false);
                    var versionIdsToDelete = new int[0];
                    // Call low level API
                    await CCDP.UpdateNodeAsync(nodeHeadData, versionData, dynamicData, versionIdsToDelete, CancellationToken.None, originalPath);
                }
                catch (Exception e)
                {
                    while (e.InnerException != null)
                        e = e.InnerException;
                    errorMessage = e.Message;
                }

                // ASSERT (special error was thrown)
                Assert.AreEqual("This transaction cannot commit anything.", errorMessage);

                // ASSERT (all operation need to be rolled back)
                var paths = (new[] { f1, f11, f12, f2, f21, f22 })
                    .Select(x => Node.Load<SystemFolder>(x.Id).Path.Replace("/Root/", ""))
                    .ToArray();
                AssertSequenceEqual(expectedPaths, paths);
            });
        }
        public async Task DP_Transaction_DeleteNode()
        {
            await IntegrationTestAsync(async () =>
            {
                // Create a small subtree
                var root = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString(), Description = "Test root" };
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

                var countsBefore = (await GetDbObjectCountsAsync(null, DP, TDP)).AllCounts;
                string errorMessage = null;

                // ACTION
                try
                {
                    var node = Node.Load<SystemFolder>(root.Id);
                    var nodeData = node.Data;
                    var nodeHeadData = nodeData.GetNodeHeadData();
                    // Call low level API
                    await CCDP.DeleteNodeAsync(nodeHeadData, CancellationToken.None);
                }
                catch (Exception e)
                {
                    while (e.InnerException != null)
                        e = e.InnerException;
                    errorMessage = e.Message;
                }

                // ASSERT (special error was thrown)
                Assert.AreEqual("This transaction cannot commit anything.", errorMessage);

                // ASSERT
                Assert.IsNotNull(Node.Load<SystemFolder>(root.Id));
                Assert.IsNotNull(Node.Load<SystemFolder>(f1.Id));
                Assert.IsNotNull(Node.Load<File>(f2.Id));
                Assert.IsNotNull(Node.Load<SystemFolder>(f3.Id));
                Assert.IsNotNull(Node.Load<File>(f4.Id));
                var countsAfter = (await GetDbObjectCountsAsync(null, DP, TDP)).AllCounts;
                Assert.AreEqual(countsBefore, countsAfter);
            });
        }

        /* ================================================================================================== Schema */

        public async Task DP_Schema_ExclusiveUpdate()
        {
            await IntegrationTestAsync(async () =>
            {
                await TDP.EnsureOneUnlockedSchemaLockAsync();

                var ed = new SchemaEditor();
                ed.Load();
                var timestampBefore = ed.SchemaTimestamp;

                // ACTION: try to start update with wrong timestamp
                try
                {
                    var unused = await DP.StartSchemaUpdateAsync(timestampBefore - 1, CancellationToken.None);
                    Assert.Fail("Expected DataException was not thrown.");
                }
                catch (DataException e)
                {
                    Assert.AreEqual("Storage schema is out of date.", e.Message);
                }

                // ACTION: start update normally
                var @lock = await DP.StartSchemaUpdateAsync(timestampBefore, CancellationToken.None);

                // ACTION: try to start update again
                try
                {
                    var unused = await DP.StartSchemaUpdateAsync(timestampBefore, CancellationToken.None);
                    Assert.Fail("Expected DataException was not thrown.");
                }
                catch (DataException e)
                {
                    Assert.AreEqual("Schema is locked by someone else.", e.Message);
                }

                // ACTION: try to finish with invalid @lock
                try
                {
                    var unused = await DP.FinishSchemaUpdateAsync("wrong-lock", CancellationToken.None);
                    Assert.Fail("Expected DataException was not thrown.");
                }
                catch (DataException e)
                {
                    Assert.AreEqual("Schema is locked by someone else.", e.Message);
                }

                // ACTION: finish normally
                timestampBefore = await DP.FinishSchemaUpdateAsync(@lock, CancellationToken.None);

                // ASSERT: start update is allowed again
                @lock = await DP.StartSchemaUpdateAsync(timestampBefore, CancellationToken.None);
                // cleanup
                var timestampAfter = await DP.FinishSchemaUpdateAsync(@lock, CancellationToken.None);
                // Bonus assert: change detection
                Assert.AreNotEqual(timestampBefore, timestampAfter);
            });
        }

        /* ================================================================================================== TOOLS */

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
        private File CreateFile(Node parent, string name, string fileContent)
        {
            var file = new File(parent) { Name = name };
            file.Binary.SetStream(RepositoryTools.GetStreamFromString(fileContent));
            file.Save();
            return file;
        }

        private async Task<(int Nodes, int Versions, int Binaries, int Files, int LongTexts, string AllCounts, string AllCountsExceptFiles)> GetDbObjectCountsAsync(string path, DataProvider DP, ITestingDataProviderExtension tdp)
        {
            var nodes = await DP.GetNodeCountAsync(path, CancellationToken.None);
            var versions = await DP.GetVersionCountAsync(path, CancellationToken.None);
            var binaries = await TDP.GetBinaryPropertyCountAsync(path);
            var files = await TDP.GetFileCountAsync(path);
            var longTexts = await TDP.GetLongTextCountAsync(path);
            var all = $"{nodes},{versions},{binaries},{files},{longTexts}";
            var allExceptFiles = $"{nodes},{versions},{binaries},{longTexts}";

            var result = (Nodes: nodes, Versions: versions, Binaries: binaries, Files: files, LongTexts: longTexts, AllCounts: all, AllCountsExceptFiles: allExceptFiles);
            return await Task.FromResult(result);
        }

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
