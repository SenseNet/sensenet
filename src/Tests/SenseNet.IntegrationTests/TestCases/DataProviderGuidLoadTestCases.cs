using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Versioning;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.IntegrationTests.TestCases
{
    public partial class DataProviderTestCases
    {
        public Task DP_GuidLoads_Versions()
        {
            return IntegrationTestAsync(async () =>
            {
                var cancel = CancellationToken.None;
                var node = new File(CreateTestRoot())
                {
                    Name = "GuidVersions.txt",
                    VersioningMode = VersioningType.MajorAndMinor
                };
                node.Binary.SetStream(RepositoryTools.GetStreamFromString("published"));
                await node.SaveAsync(cancel);
                await node.PublishAsync(cancel);
                var publishedVersion = node.Version;
                var publishedVersionId = node.VersionId;
                var publishedVersionGuid = node.VersionGuid;

                await node.CheckOutAsync(cancel);
                node.Binary.SetStream(RepositoryTools.GetStreamFromString("draft"));
                await node.SaveAsync(SavingMode.KeepVersionAndLock, cancel);
                await node.CheckInAsync(cancel);
                Assert.AreNotEqual(publishedVersionId, node.VersionId);
                Assert.AreNotEqual(publishedVersionGuid, node.VersionGuid);

                Assert.AreEqual(node.Id, await DP.GetNodeIdByGuidAsync(node.NodeGuid, cancel));
                Assert.AreEqual(publishedVersionId, await DP.GetVersionIdByGuidAsync(publishedVersionGuid, cancel));
                Assert.AreEqual(node.VersionId, await DP.GetVersionIdByGuidAsync(node.VersionGuid, cancel));

                // Exercise both cold and warm caches, including all seven public entry points.
                Cache.Reset();
                for (var i = 0; i < 2; i++)
                {
                    AssertGuidLoadedNode(node.Id, node.VersionId, node.NodeGuid, node.VersionGuid,
                        await Node.LoadNodeAsync(node.NodeGuid, cancel));
                    AssertGuidLoadedNode(node.Id, node.VersionId, node.NodeGuid, node.VersionGuid,
                        await Node.LoadAsync<File>(node.NodeGuid, cancel));
                    AssertGuidLoadedNode(node.Id, publishedVersionId, node.NodeGuid, publishedVersionGuid,
                        await Node.LoadNodeAsync(node.NodeGuid, publishedVersion, cancel));
                    AssertGuidLoadedNode(node.Id, publishedVersionId, node.NodeGuid, publishedVersionGuid,
                        await Node.LoadAsync<File>(node.NodeGuid, publishedVersion, cancel));
                    AssertGuidLoadedNode(node.Id, publishedVersionId, node.NodeGuid, publishedVersionGuid,
                        await Node.LoadNodeByVersionGuidAsync(publishedVersionGuid, cancel));
                    AssertGuidLoadedNode(node.Id, node.VersionId, node.NodeGuid, node.VersionGuid,
                        await Node.LoadNodeByVersionGuidAsync(node.VersionGuid, cancel));

                    foreach (var version in new[]
                    {
                        VersionNumber.LastAccessible, VersionNumber.LastMajor, VersionNumber.LastMinor,
                        VersionNumber.Header, VersionNumber.LastFinalized
                    })
                    {
                        var byId = await Node.LoadNodeAsync(node.Id, version, cancel);
                        var byGuid = await Node.LoadNodeAsync(node.NodeGuid, version, cancel);
                        AssertGuidLoadedNode(byId.Id, byId.VersionId, byId.NodeGuid, byId.VersionGuid, byGuid);
                        Assert.AreEqual(byId.IsHeadOnly, byGuid.IsHeadOnly);
                    }

                    var currentHead = await NodeHead.GetAsync(node.Id, cancel);
                    foreach (var head in new[]
                    {
                        await NodeHead.GetAsync(node.NodeGuid, cancel),
                        await NodeHead.GetByVersionGuidAsync(publishedVersionGuid, cancel),
                        await NodeHead.GetByVersionGuidAsync(node.VersionGuid, cancel)
                    })
                    {
                        Assert.IsNotNull(head);
                        Assert.AreEqual(currentHead.Id, head.Id);
                        Assert.AreEqual(currentHead.Path, head.Path);
                        Assert.AreEqual(currentHead.LastMajorVersionId, head.LastMajorVersionId);
                        Assert.AreEqual(currentHead.LastMinorVersionId, head.LastMinorVersionId);
                    }
                }

                Assert.IsNull(await Node.LoadNodeAsync(node.NodeGuid, new VersionNumber(9999, 0), cancel));
                Assert.IsNull(await Node.LoadAsync<File>(node.NodeGuid, new VersionNumber(9999, 0), cancel));
                await Assert.ThrowsExceptionAsync<InvalidCastException>(() => Node.LoadAsync<User>(node.NodeGuid, cancel));
                await Assert.ThrowsExceptionAsync<InvalidCastException>(() =>
                    Node.LoadAsync<User>(node.NodeGuid, publishedVersion, cancel));
            });
        }

        public Task DP_GuidLoads_Missing()
        {
            return IntegrationTestAsync(async () =>
            {
                var node = CreateTestRoot();
                foreach (var guid in new[] { Guid.Empty, Guid.NewGuid() })
                {
                    Assert.AreEqual(0, await DP.GetNodeIdByGuidAsync(guid, CancellationToken.None));
                    Assert.AreEqual(0, await DP.GetVersionIdByGuidAsync(guid, CancellationToken.None));
                    await AssertMissingNodeGuidAsync(guid);
                    await AssertMissingVersionGuidAsync(guid);
                }

                // Node GUIDs and version GUIDs belong to separate identifier domains.
                Assert.AreEqual(0, await DP.GetNodeIdByGuidAsync(node.VersionGuid, CancellationToken.None));
                Assert.AreEqual(0, await DP.GetVersionIdByGuidAsync(node.NodeGuid, CancellationToken.None));
                await AssertMissingNodeGuidAsync(node.VersionGuid);
                await AssertMissingVersionGuidAsync(node.NodeGuid);
            });
        }

        public Task DP_GuidLoads_Lifecycle()
        {
            return IntegrationTestAsync(async () =>
            {
                var cancel = CancellationToken.None;
                var root = CreateTestRoot();
                var node = new SystemFolder(root) { Name = "Original", VersioningMode = VersioningType.None };
                await node.SaveAsync(cancel);
                var nodeGuid = node.NodeGuid;
                var versionGuid = node.VersionGuid;
                await Node.LoadNodeAsync(nodeGuid, cancel);
                await NodeHead.GetAsync(nodeGuid, cancel);

                // Checkin without versioning removes the temporary checked-out version row.
                await node.CheckOutAsync(cancel);
                var removedVersionGuid = node.VersionGuid;
                Assert.IsNotNull(await Node.LoadNodeByVersionGuidAsync(removedVersionGuid, cancel));
                await node.CheckInAsync(cancel);
                await AssertMissingVersionGuidAsync(removedVersionGuid);

                node.Name = "Renamed";
                await node.SaveAsync(cancel);
                var target = CreateFolder(root, "Target");
                node.MoveTo(target);
                Assert.AreEqual(node.Path, (await Node.LoadNodeAsync(nodeGuid, cancel)).Path);
                Assert.AreEqual(node.Path, (await NodeHead.GetAsync(nodeGuid, cancel)).Path);
                Assert.AreEqual(node.Path, (await NodeHead.GetByVersionGuidAsync(versionGuid, cancel)).Path);

                var copy = node.CopyToAndGetCopy(target, "Copy");
                AssertGuidLoadedNode(copy.Id, copy.VersionId, copy.NodeGuid, copy.VersionGuid,
                    await Node.LoadNodeAsync(copy.NodeGuid, cancel));
                AssertGuidLoadedNode(node.Id, node.VersionId, nodeGuid, versionGuid,
                    await Node.LoadNodeByVersionGuidAsync(versionGuid, cancel));

                await node.ForceDeleteAsync(cancel);
                await AssertMissingNodeGuidAsync(nodeGuid);
                await AssertMissingVersionGuidAsync(versionGuid);
                Assert.IsNotNull(await Node.LoadNodeAsync(copy.NodeGuid, cancel));
            });
        }

        public Task DP_GuidLoads_Cancellation()
        {
            return IntegrationTestAsync(async () =>
            {
                var node = CreateTestRoot();
                // Populate the caches before cancellation.
                await Node.LoadNodeAsync(node.NodeGuid, CancellationToken.None);
                await NodeHead.GetByVersionGuidAsync(node.VersionGuid, CancellationToken.None);
                using var source = new CancellationTokenSource();
                source.Cancel();
                var cancel = source.Token;
                foreach (var guid in new[] { node.NodeGuid, node.VersionGuid, Guid.Empty, Guid.NewGuid() })
                {
                    await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => DP.GetNodeIdByGuidAsync(guid, cancel));
                    await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => DP.GetVersionIdByGuidAsync(guid, cancel));
                    await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => Node.LoadNodeAsync(guid, cancel));
                    await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => Node.LoadNodeAsync(guid, node.Version, cancel));
                    await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => Node.LoadAsync<SystemFolder>(guid, cancel));
                    await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => Node.LoadAsync<SystemFolder>(guid, node.Version, cancel));
                    await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => Node.LoadNodeByVersionGuidAsync(guid, cancel));
                    await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => NodeHead.GetAsync(guid, cancel));
                    await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => NodeHead.GetByVersionGuidAsync(guid, cancel));
                }
            });
        }

        public Task DP_GuidLoads_Permissions()
        {
            return IntegrationTestAsync(async () =>
            {
                var cancel = CancellationToken.None;
                var node = CreateTestRoot();
                var visitor = User.Visitor;
                await Providers.Instance.SecurityHandler.CreateAclEditor()
                    .Allow(node.Id, visitor.Id, false, PermissionType.Open)
                    .Deny(node.Id, visitor.Id, false, PermissionType.RecallOldVersion)
                    .ApplyAsync(cancel);
                var originalUser = User.Current;
                try
                {
                    User.Current = visitor;
                    var byId = await Node.LoadNodeAsync(node.Id, cancel);
                    AssertGuidLoadedNode(byId.Id, byId.VersionId, byId.NodeGuid, byId.VersionGuid,
                        await Node.LoadNodeAsync(node.NodeGuid, cancel));
                    await Assert.ThrowsExceptionAsync<SenseNetSecurityException>(() =>
                        Node.LoadNodeByVersionIdAsync(node.VersionId, cancel));
                    await Assert.ThrowsExceptionAsync<SenseNetSecurityException>(() =>
                        Node.LoadNodeByVersionGuidAsync(node.VersionGuid, cancel));
                }
                finally
                {
                    User.Current = originalUser;
                }

                await Providers.Instance.SecurityHandler.CreateAclEditor()
                    .Deny(node.Id, visitor.Id, false, PermissionType.See)
                    .ApplyAsync(cancel);
                try
                {
                    User.Current = visitor;
                    await Assert.ThrowsExceptionAsync<SenseNetSecurityException>(() => Node.LoadNodeAsync(node.Id, cancel));
                    await Assert.ThrowsExceptionAsync<SenseNetSecurityException>(() => Node.LoadNodeAsync(node.NodeGuid, cancel));
                    await Assert.ThrowsExceptionAsync<SenseNetSecurityException>(() => Node.LoadAsync<SystemFolder>(node.NodeGuid, cancel));
                    await Assert.ThrowsExceptionAsync<SenseNetSecurityException>(() =>
                        Node.LoadNodeAsync(node.NodeGuid, node.Version, cancel));
                    await Assert.ThrowsExceptionAsync<SenseNetSecurityException>(() =>
                        Node.LoadAsync<SystemFolder>(node.NodeGuid, node.Version, cancel));
                }
                finally
                {
                    User.Current = originalUser;
                }
            });
        }

        private static void AssertGuidLoadedNode(int nodeId, int versionId, Guid nodeGuid, Guid versionGuid, Node actual)
        {
            Assert.IsNotNull(actual);
            Assert.AreEqual(nodeId, actual.Id);
            Assert.AreEqual(versionId, actual.VersionId);
            Assert.AreEqual(nodeGuid, actual.NodeGuid);
            Assert.AreEqual(versionGuid, actual.VersionGuid);
        }

        private static async Task AssertMissingNodeGuidAsync(Guid guid)
        {
            var cancel = CancellationToken.None;
            Assert.IsNull(await Node.LoadNodeAsync(guid, cancel));
            Assert.IsNull(await Node.LoadNodeAsync(guid, VersionNumber.LastMajor, cancel));
            Assert.IsNull(await Node.LoadNodeAsync(guid, VersionNumber.LastFinalized, cancel));
            Assert.IsNull(await Node.LoadAsync<SystemFolder>(guid, cancel));
            Assert.IsNull(await Node.LoadAsync<SystemFolder>(guid, VersionNumber.LastMajor, cancel));
            Assert.IsNull(await NodeHead.GetAsync(guid, cancel));
        }

        private static async Task AssertMissingVersionGuidAsync(Guid guid)
        {
            Assert.IsNull(await Node.LoadNodeByVersionGuidAsync(guid, CancellationToken.None));
            Assert.IsNull(await NodeHead.GetByVersionGuidAsync(guid, CancellationToken.None));
        }
    }
}
