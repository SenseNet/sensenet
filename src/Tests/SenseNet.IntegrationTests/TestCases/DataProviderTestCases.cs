using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Versioning;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.Search.Querying;
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
                var root = CreateFolder(Repository.Root, "TestRoot");
                var file = CreateFile(root, "File-1.txt", "File content.");

                var versionId = file.VersionId;
                var fileId = file.Binary.FileId;
                var propertyTypeId = file.Binary.PropertyType.Id;

                // ACTION-1: Load existing
                var result = await DP.LoadBinaryPropertyValueAsync(versionId, propertyTypeId, CancellationToken.None);
                // ASSERT-1
                //UNDONE:<?:IntT:!!! Fix fileext: mssql returns: "File1..txt"
                Assert.IsNotNull(result);
                Assert.AreEqual("File-1", result.FileName.FileNameWithoutExtension);
                Assert.AreEqual("txt", result.FileName.Extension);
                Assert.AreEqual(3L + "File content.".Length, result.Size); // +UTF-8 BOM
                Assert.AreEqual("text/plain", result.ContentType);

                // ACTION-2: Missing Binary
                result = await DP.LoadBinaryPropertyValueAsync(versionId, 999999, CancellationToken.None);
                // ASSERT-2 (not loaded and no exceptin was thrown)
                Assert.IsNull(result);

                // ACTION-3: Staging
                await TDP.SetFileStagingAsync(fileId, true);
                result = await DP.LoadBinaryPropertyValueAsync(versionId, propertyTypeId, CancellationToken.None);
                // ASSERT-3 (not loaded and no exceptin was thrown)
                Assert.IsNull(result);

                // ACTION-4: Missing File (inconsistent but need to be handled)
                await TDP.DeleteFileAsync(fileId);

                result = await DP.LoadBinaryPropertyValueAsync(versionId, propertyTypeId, CancellationToken.None);
                // ASSERT-4 (not loaded and no exceptin was thrown)
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
