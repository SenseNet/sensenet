using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
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

        [TestMethod]
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
        [TestMethod]
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
        [TestMethod]
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
        [TestMethod]
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
        [TestMethod]
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

        //protected string ArrayToString(int[] array)
        //{
        //    return string.Join(",", array.Select(x => x.ToString()));
        //}
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
