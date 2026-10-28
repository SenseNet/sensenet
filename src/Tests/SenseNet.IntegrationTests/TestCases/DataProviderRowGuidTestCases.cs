using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Versioning;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.IntegrationTests.TestCases
{
    public partial class DataProviderTestCases
    {
        public Task DP_RowGuids_NodeLifecycle()
        {
            return IntegrationTestAsync(async () =>
            {
                var root = CreateTestRoot();
                var node = new SystemFolder(root) { Name = "RowGuids", VersioningMode = VersioningType.None };
                Assert.AreEqual(Guid.Empty, node.NodeGuid);
                Assert.AreEqual(Guid.Empty, node.VersionGuid);
                await node.SaveAsync(CancellationToken.None);

                var nodeGuid = node.NodeGuid;
                var versionGuid = node.VersionGuid;
                var versionId = node.VersionId;
                Assert.AreNotEqual(Guid.Empty, nodeGuid);
                Assert.AreNotEqual(Guid.Empty, versionGuid);
                Assert.AreNotEqual(nodeGuid, versionGuid);
                AssertRowGuidProperties(node, nodeGuid, versionGuid);
                Assert.IsFalse(node.IsModified);

                var cached = Node.Load<SystemFolder>(node.Id);
                AssertRowGuidProperties(cached, nodeGuid, versionGuid);
                cached.Index++;
                AssertRowGuidProperties(cached, nodeGuid, versionGuid);
                Assert.IsFalse(cached.Data.GetChangedValues().Any(x => x.Name == "NodeGuid" || x.Name == "VersionGuid"));
                cached.Name = "RowGuidsRenamed";
                await cached.SaveAsync(CancellationToken.None);
                Assert.AreEqual(versionId, cached.VersionId);
                AssertRowGuidProperties(cached, nodeGuid, versionGuid);
                Assert.IsFalse(cached.IsModified);
                await AssertStoredRowGuidsAsync(cached.Id, versionId, nodeGuid, versionGuid);

                // Checkout creates a row. Checkin without versioning overwrites the original row.
                await cached.CheckOutAsync(CancellationToken.None);
                var checkedOutVersionGuid = cached.VersionGuid;
                Assert.AreNotEqual(versionId, cached.VersionId);
                Assert.AreNotEqual(versionGuid, checkedOutVersionGuid);
                AssertRowGuidProperties(cached, nodeGuid, checkedOutVersionGuid);
                await AssertStoredRowGuidsAsync(cached.Id, versionId, nodeGuid, versionGuid);
                await cached.CheckInAsync(CancellationToken.None);
                Assert.AreEqual(versionId, cached.VersionId);
                AssertRowGuidProperties(cached, nodeGuid, versionGuid);

                var target = CreateFolder(root, "Target");
                var copy = cached.CopyToAndGetCopy(target, "Copy");
                Assert.AreNotEqual(nodeGuid, copy.NodeGuid);
                Assert.AreNotEqual(versionGuid, copy.VersionGuid);
                await AssertStoredRowGuidsAsync(copy.Id, copy.VersionId, copy.NodeGuid, copy.VersionGuid);
                cached.MoveTo(target);
                await AssertStoredRowGuidsAsync(cached.Id, versionId, nodeGuid, versionGuid);

                Cache.Reset();
                var reloaded = Node.LoadNode(cached.Id);
                AssertRowGuidProperties(reloaded, nodeGuid, versionGuid);
                Assert.IsFalse(reloaded.IsModified);
            });
        }

        public Task DP_RowGuids_ProviderWrites(bool emptyInput)
        {
            return IntegrationTestAsync(async () =>
            {
                var root = CreateTestRoot();
                var node = new SystemFolder(root) { Name = "DirectInsert" };
                node.Data.Path = RepositoryPath.Combine(root.Path, node.Name);
                var head = node.Data.GetNodeHeadData();
                var version = node.Data.GetVersionData();
                var inputNodeGuid = emptyInput ? Guid.Empty : Guid.NewGuid();
                var inputVersionGuid = emptyInput ? Guid.Empty : Guid.NewGuid();
                head.RowGuid = inputNodeGuid;
                version.RowGuid = inputVersionGuid;
                await DP.InsertNodeAsync(head, version, node.Data.GetDynamicData(false), CancellationToken.None);
                var nodeGuid = head.RowGuid;
                var versionGuid = version.RowGuid;
                var originalVersionId = version.VersionId;
                var originalVersion = version.Version;
                Assert.AreNotEqual(Guid.Empty, nodeGuid);
                Assert.AreNotEqual(Guid.Empty, versionGuid);
                Assert.AreNotEqual(inputNodeGuid, nodeGuid);
                Assert.AreNotEqual(inputVersionGuid, versionGuid);
                await AssertStoredRowGuidsAsync(head.NodeId, originalVersionId, nodeGuid, versionGuid);

                var data = (await DP.LoadNodesAsync(new[] { originalVersionId }, CancellationToken.None)).Single();
                head = data.GetNodeHeadData();
                version = data.GetVersionData();
                head.RowGuid = inputNodeGuid;
                version.RowGuid = inputVersionGuid;
                head.Index++;
                await DP.UpdateNodeAsync(head, version, data.GetDynamicData(false), Array.Empty<int>(), CancellationToken.None);
                Assert.AreEqual(nodeGuid, head.RowGuid);
                Assert.AreEqual(versionGuid, version.RowGuid);
                await AssertStoredRowGuidsAsync(head.NodeId, originalVersionId, nodeGuid, versionGuid);

                head.RowGuid = inputNodeGuid;
                head.Index++;
                await DP.UpdateNodeHeadAsync(head, Array.Empty<int>(), CancellationToken.None);
                Assert.AreEqual(nodeGuid, head.RowGuid);
                await AssertStoredRowGuidsAsync(head.NodeId, originalVersionId, nodeGuid, versionGuid);

                // A new version ignores both the source GUID and any supplied DTO GUID.
                head.RowGuid = inputNodeGuid;
                version.RowGuid = inputVersionGuid;
                version.Version = VersionNumber.Parse("V2.0.L");
                await DP.CopyAndUpdateNodeAsync(head, version, data.GetDynamicData(false), Array.Empty<int>(), CancellationToken.None);
                var newVersionId = version.VersionId;
                var newVersionGuid = version.RowGuid;
                Assert.AreNotEqual(originalVersionId, newVersionId);
                Assert.AreNotEqual(versionGuid, newVersionGuid);
                Assert.AreNotEqual(inputVersionGuid, newVersionGuid);
                Assert.AreEqual(nodeGuid, head.RowGuid);
                await AssertStoredRowGuidsAsync(head.NodeId, originalVersionId, nodeGuid, versionGuid);
                await AssertStoredRowGuidsAsync(head.NodeId, newVersionId, nodeGuid, newVersionGuid);

                // The existing destination keeps its own GUID, not the source version's GUID.
                data = (await DP.LoadNodesAsync(new[] { newVersionId }, CancellationToken.None)).Single();
                head = data.GetNodeHeadData();
                version = data.GetVersionData();
                head.RowGuid = inputNodeGuid;
                version.RowGuid = inputVersionGuid;
                version.Version = originalVersion;
                await DP.CopyAndUpdateNodeAsync(head, version, data.GetDynamicData(false), Array.Empty<int>(),
                    CancellationToken.None, originalVersionId);
                Assert.AreEqual(nodeGuid, head.RowGuid);
                Assert.AreEqual(originalVersionId, version.VersionId);
                Assert.AreEqual(versionGuid, version.RowGuid);
                await AssertStoredRowGuidsAsync(head.NodeId, originalVersionId, nodeGuid, versionGuid);
                await AssertStoredRowGuidsAsync(head.NodeId, newVersionId, nodeGuid, newVersionGuid);

                var loaded = (await DP.LoadNodesAsync(new[] { newVersionId, root.VersionId, originalVersionId },
                    CancellationToken.None)).ToDictionary(x => x.VersionId);
                Assert.AreEqual(newVersionGuid, loaded[newVersionId].VersionGuid);
                Assert.AreEqual(versionGuid, loaded[originalVersionId].VersionGuid);
                Assert.AreEqual(root.NodeGuid, loaded[root.VersionId].NodeGuid);
            });
        }

        public Task DP_RowGuids_InitialDataAndAuxiliaryWrites()
        {
            return IntegrationTestAsync(async () =>
            {
                foreach (var initialNode in new Node[] { Repository.Root, User.Administrator })
                    await AssertStoredRowGuidsAsync(initialNode.Id, initialNode.VersionId,
                        initialNode.NodeGuid, initialNode.VersionGuid);

                var node = CreateTestRoot();
                var nodeGuid = node.NodeGuid;
                var versionGuid = node.VersionGuid;
                await DP.AddChangedDataAsync(node.VersionId,
                    new ChangedData { Name = "Index", Original = "0", Value = "1" }, CancellationToken.None);
                await AssertStoredRowGuidsAsync(node.Id, node.VersionId, nodeGuid, versionGuid);
                var document = (await DP.LoadIndexDocumentsAsync(new[] { node.VersionId }, CancellationToken.None)).Single();
                await DP.SaveIndexDocumentAsync(node.VersionId, document.IndexDocument.Serialize(), CancellationToken.None);
                await AssertStoredRowGuidsAsync(node.Id, node.VersionId, nodeGuid, versionGuid);
            });
        }

        public Task DP_RowGuids_MissingDestination()
        {
            return IntegrationTestAsync(async () =>
            {
                var node = CreateTestRoot();
                var data = node.Data;
                await Assert.ThrowsExceptionAsync<ContentNotFoundException>(() => DP.CopyAndUpdateNodeAsync(
                    data.GetNodeHeadData(), data.GetVersionData(), data.GetDynamicData(false), Array.Empty<int>(),
                    CancellationToken.None, int.MaxValue));
                await AssertStoredRowGuidsAsync(node.Id, node.VersionId, node.NodeGuid, node.VersionGuid);
                Assert.IsFalse((await DP.LoadNodesAsync(new[] { int.MaxValue }, CancellationToken.None)).Any());
            });
        }

        public Task DP_RowGuids_Rollback(string operation)
        {
            return IntegrationTestAsync(async () =>
            {
                var node = CreateTestRoot();
                var originalVersionId = node.VersionId;
                var originalVersionGuid = node.VersionGuid;
                var originalVersion = node.Version;
                await node.CheckOutAsync(CancellationToken.None);
                var nodeGuid = node.NodeGuid;
                var versionGuid = node.VersionGuid;
                var versionId = node.VersionId;
                var data = node.Data;
                var head = data.GetNodeHeadData();
                var version = data.GetVersionData();
                head.RowGuid = Guid.NewGuid();
                version.RowGuid = Guid.NewGuid();
                head.Index++;
                var cannotCommit = CCDP;
                Exception error = null;
                try
                {
                    switch (operation)
                    {
                        case "update":
                            await cannotCommit.UpdateNodeAsync(head, version, data.GetDynamicData(false),
                                Array.Empty<int>(), CancellationToken.None);
                            break;
                        case "head":
                            await cannotCommit.UpdateNodeHeadAsync(head, Array.Empty<int>(), CancellationToken.None);
                            break;
                        case "new-version":
                            version.Version = VersionNumber.Parse("V3.0.L");
                            await cannotCommit.CopyAndUpdateNodeAsync(head, version, data.GetDynamicData(false),
                                Array.Empty<int>(), CancellationToken.None);
                            break;
                        case "overwrite":
                            version.Version = originalVersion;
                            await cannotCommit.CopyAndUpdateNodeAsync(head, version, data.GetDynamicData(false),
                                new[] { versionId }, CancellationToken.None, originalVersionId);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(operation));
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                Assert.IsNotNull(error, "The transaction must fail.");
                while (error.InnerException != null)
                    error = error.InnerException;
                Assert.AreEqual("This transaction cannot commit anything.", error.Message);
                AssertRowGuidProperties(node, nodeGuid, versionGuid);
                await AssertStoredRowGuidsAsync(node.Id, versionId, nodeGuid, versionGuid);
                await AssertStoredRowGuidsAsync(node.Id, originalVersionId, nodeGuid, originalVersionGuid);
                if (operation == "new-version")
                    Assert.IsFalse((await DP.LoadNodesAsync(new[] { version.VersionId }, CancellationToken.None)).Any());
                Cache.Reset();
                AssertRowGuidProperties(Node.LoadNode(node.Id), nodeGuid, versionGuid);
            });
        }

        private static void AssertRowGuidProperties(Node node, Guid nodeGuid, Guid versionGuid)
        {
            Assert.AreEqual(nodeGuid, node.NodeGuid);
            Assert.AreEqual(versionGuid, node.VersionGuid);
            Assert.AreEqual(nodeGuid, node.Data.NodeGuid);
            Assert.AreEqual(versionGuid, node.Data.VersionGuid);
            Assert.AreEqual(nodeGuid, node["NodeGuid"]);
            Assert.AreEqual(versionGuid, node["VersionGuid"]);
            Assert.AreEqual(nodeGuid, node.GetStoredValue("NodeGuid"));
            Assert.AreEqual(versionGuid, node.GetStoredValue("VersionGuid"));
            Assert.IsTrue(node.HasProperty("NodeGuid"));
            Assert.IsTrue(node.HasProperty("VersionGuid"));
            Assert.ThrowsException<InvalidOperationException>(() => node["NodeGuid"] = Guid.NewGuid());
            Assert.ThrowsException<InvalidOperationException>(() => node["VersionGuid"] = Guid.NewGuid());
            Assert.AreEqual(nodeGuid.ToString("D"), node.Data.GetAllValues()["NodeGuid"]);
            Assert.AreEqual(versionGuid.ToString("D"), node.Data.GetAllValues()["VersionGuid"]);
        }

        private async Task AssertStoredRowGuidsAsync(int nodeId, int versionId, Guid nodeGuid, Guid versionGuid)
        {
            Assert.AreNotEqual(Guid.Empty, nodeGuid);
            Assert.AreNotEqual(Guid.Empty, versionGuid);
            // The testing adapter reads the physical SQL rows / in-memory documents directly.
            Assert.AreEqual(nodeGuid, (await TDP.GetNodeHeadDataAsync(nodeId)).RowGuid);
            Assert.AreEqual(versionGuid, (await TDP.GetVersionDataAsync(versionId)).RowGuid);
            var loaded = (await DP.LoadNodesAsync(new[] { versionId }, CancellationToken.None)).Single();
            Assert.AreEqual(nodeId, loaded.Id);
            Assert.AreEqual(nodeGuid, loaded.NodeGuid);
            Assert.AreEqual(versionGuid, loaded.VersionGuid);
        }
    }
}
