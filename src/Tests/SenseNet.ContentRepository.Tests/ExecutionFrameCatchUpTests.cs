using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;
using SenseNet.Tests.Core;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ExecutionFrameCatchUpTests : TestBase
    {
        [TestMethod, TestCategory("IR")]
        public async STT.Task ExecutionFrameCatchUp_ReconcilesRemoveTreeGapAndReplaysCompletedActivities()
        {
            RepositoryFixture fixture = null;
            var oldVersion = 0;
            var movedVersion = 0;
            var laterVersion = 0;
            var lastActivityId = 0;
            var path = Repository.RootPath + "/frame-gap-target";

            await Test(async () =>
            {
                fixture = new RepositoryFixture();
                var old = new SystemFolder(Repository.Root) { Name = "frame-gap-target" };
                await old.SaveAsync(CancellationToken.None);
                oldVersion = old.VersionId;
                var stale = await Providers.Instance.DataStore.LoadIndexDocumentsAsync(
                    new[] { oldVersion }, CancellationToken.None);
                var oldDocument = Providers.Instance.IndexManager.CompleteIndexDocument(stale.Single());

                var source = new SystemFolder(Repository.Root) { Name = "frame-gap-source" };
                await source.SaveAsync(CancellationToken.None);
                var moved = new SystemFolder(source) { Name = old.Name };
                await moved.SaveAsync(CancellationToken.None);
                movedVersion = moved.VersionId;

                await old.ForceDeleteAsync(CancellationToken.None);
                moved.MoveTo(Repository.Root);
                var later = new SystemFolder(Repository.Root) { Name = "frame-gap-later" };
                await later.SaveAsync(CancellationToken.None);
                laterVersion = later.VersionId;

                var activities = fixture.DataProvider.DB.IndexingActivities;
                var gap = activities.Single(a => a.ActivityType == IndexingActivityType.RemoveTree &&
                    string.Equals(a.Path, path, StringComparison.OrdinalIgnoreCase)).IndexingActivityId;
                Assert.IsTrue(activities.Any(a => a.ActivityType == IndexingActivityType.AddTree && a.IndexingActivityId > gap &&
                    string.Equals(a.Path, path, StringComparison.OrdinalIgnoreCase)));
                lastActivityId = activities.Max(a => a.IndexingActivityId);

                var engine = fixture.SearchEngine.IndexingEngine;
                await engine.WriteIndexAsync(new[] { new SnTerm(IndexFieldName.VersionId, laterVersion) },
                    null, new[] { oldDocument }, CancellationToken.None);
                await engine.WriteActivityStatusToIndexAsync(
                    new IndexingActivityStatus { LastActivityId = lastActivityId, Gaps = new[] { gap } }, CancellationToken.None);
            });

            await Test2(services => services.UseExecutionFrameIndexCatchUp(), fixture.Configure, async () =>
            {
                var engine = fixture.SearchEngine.IndexingEngine;
                var state = await engine.ReadActivityStatusFromIndexAsync(CancellationToken.None);
                Assert.AreEqual(lastActivityId, state.LastActivityId);
                CollectionAssert.AreEqual(Array.Empty<int>(), state.Gaps);
                Assert.IsNull(engine.GetIndexDocumentByVersionId(oldVersion));
                Assert.AreEqual(path.ToLowerInvariant(), engine.GetIndexDocumentByVersionId(movedVersion)["Path"]);
                Assert.IsNotNull(engine.GetIndexDocumentByVersionId(laterVersion));

                var online = new SystemFolder(Repository.Root) { Name = "frame-after-startup" };
                await online.SaveAsync(CancellationToken.None);
                await Providers.Instance.IndexManager.CommitAsync(CancellationToken.None);
                Assert.IsNotNull(engine.GetIndexDocumentByVersionId(online.VersionId));
                lastActivityId = fixture.DataProvider.DB.IndexingActivities.Max(activity => activity.IndexingActivityId);
            });

            await Test2(services => services.UseLegacyIndexCatchUp(), fixture.Configure, async () =>
            {
                var state = await fixture.SearchEngine.IndexingEngine.ReadActivityStatusFromIndexAsync(CancellationToken.None);
                Assert.AreEqual(lastActivityId, state.LastActivityId);
                CollectionAssert.AreEqual(Array.Empty<int>(), state.Gaps);
            });
        }

        [TestMethod, TestCategory("IR")]
        public async STT.Task ExecutionFrameCatchUp_DeletesWithdrawnVersionAndRestoresCurrentFlags()
        {
            RepositoryFixture fixture = null;
            InMemoryIndex checkpoint = null;
            IndexingActivityStatus checkpointStatus = null;
            var withdrawnVersion = 0;
            var publicVersion = 0;
            var cancel = CancellationToken.None;

            await Test(async () =>
            {
                fixture = new RepositoryFixture();
                var parent = new SystemFolder(Repository.Root) { Name = "frame-versioning" };
                await parent.SaveAsync(cancel);
                var file = new File(parent)
                {
                    Name = "frame-withdrawn.txt",
                    VersioningMode = VersioningType.None,
                    ApprovingMode = ApprovingType.False
                };
                file.Binary.SetStream(RepositoryTools.GetStreamFromString("original"));
                file.Binary.FileName = file.Name;
                await file.SaveAsync(cancel);
                publicVersion = file.VersionId;
                await file.CheckOutAsync(cancel);
                withdrawnVersion = file.VersionId;
                Assert.AreNotEqual(publicVersion, withdrawnVersion);

                var engine = fixture.SearchEngine.IndexingEngine;
                await Providers.Instance.IndexManager.CommitAsync(cancel);
                checkpointStatus = await engine.ReadActivityStatusFromIndexAsync(cancel);
                checkpoint = fixture.SearchEngine.Index.Clone();
                Assert.IsNotNull(engine.GetIndexDocumentByVersionId(withdrawnVersion));

                file.Description = "changed in the checked-out version";
                await file.SaveAsync(cancel);
                await file.UndoCheckOutAsync(cancel);
                Assert.IsTrue(fixture.DataProvider.DB.IndexingActivities.Any(activity =>
                    activity.IndexingActivityId > checkpointStatus.LastActivityId &&
                    activity.ActivityType == IndexingActivityType.UpdateDocument &&
                    activity.VersionId == withdrawnVersion));
                Assert.IsFalse(fixture.DataProvider.DB.Versions.Any(version => version.VersionId == withdrawnVersion));
            });

            fixture.SearchEngine.Index = checkpoint;
            await fixture.SearchEngine.IndexingEngine.WriteActivityStatusToIndexAsync(checkpointStatus, cancel);

            Test2(services => services.UseExecutionFrameIndexCatchUp(), fixture.Configure, () =>
            {
                var engine = fixture.SearchEngine.IndexingEngine;
                Assert.IsNull(engine.GetIndexDocumentByVersionId(withdrawnVersion));
                var document = engine.GetIndexDocumentByVersionId(publicVersion);
                Assert.AreEqual(IndexValue.Yes, document[IndexFieldName.IsLastPublic]);
                Assert.AreEqual(IndexValue.Yes, document[IndexFieldName.IsLastDraft]);
            });
        }

        [TestMethod, TestCategory("IR")]
        public async STT.Task ExecutionFrameCatchUp_FailureDiscardsEarlierFrameWrites()
        {
            RepositoryFixture fixture = null;
            InMemoryIndex checkpoint = null;
            IndexingActivityStatus checkpointStatus = null;
            var deletedVersion = 0;
            var damagedVersion = 0;

            await Test(async () =>
            {
                fixture = new RepositoryFixture();
                var old = new SystemFolder(Repository.Root) { Name = "frame-abort-target" };
                await old.SaveAsync(CancellationToken.None);
                deletedVersion = old.VersionId;

                await Providers.Instance.IndexManager.CommitAsync(CancellationToken.None);
                var engine = fixture.SearchEngine.IndexingEngine;
                checkpointStatus = await engine.ReadActivityStatusFromIndexAsync(CancellationToken.None);
                checkpoint = fixture.SearchEngine.Index.Clone();

                await old.ForceDeleteAsync(CancellationToken.None);
                var damaged = new SystemFolder(Repository.Root) { Name = "frame-abort-damaged" };
                await damaged.SaveAsync(CancellationToken.None);
                damagedVersion = damaged.VersionId;
            });

            fixture.DataProvider.DB.Versions.Single(version => version.VersionId == damagedVersion).IndexDocument = string.Empty;
            fixture.SearchEngine.Index = checkpoint;
            await fixture.SearchEngine.IndexingEngine.WriteActivityStatusToIndexAsync(checkpointStatus, CancellationToken.None);

            Assert.ThrowsException<AggregateException>(() =>
                Test2(services => services.UseExecutionFrameIndexCatchUp(), fixture.Configure,
                    () => Assert.Fail("Startup should fail on the damaged version.")));

            var indexingEngine = fixture.SearchEngine.IndexingEngine;
            Assert.AreSame(checkpoint, fixture.SearchEngine.Index);
            Assert.IsNotNull(indexingEngine.GetIndexDocumentByVersionId(deletedVersion));
            Assert.IsNull(indexingEngine.GetIndexDocumentByVersionId(damagedVersion));
            var actual = await indexingEngine.ReadActivityStatusFromIndexAsync(CancellationToken.None);
            Assert.AreEqual(checkpointStatus.LastActivityId, actual.LastActivityId);
            CollectionAssert.AreEqual(checkpointStatus.Gaps, actual.Gaps);
        }

        private sealed class RepositoryFixture
        {
            public InMemoryDataProvider DataProvider { get; } = (InMemoryDataProvider)Providers.Instance.DataStore.DataProvider;
            public InMemorySearchEngine SearchEngine { get; } = (InMemorySearchEngine)Providers.Instance.SearchManager.SearchEngine;
            private InMemoryBlobProvider BlobProvider { get; } =
                (InMemoryBlobProvider)Providers.Instance.BlobProviders[typeof(InMemoryBlobProvider).FullName];

            public void Configure(RepositoryBuilder builder)
            {
                ((InMemoryDataProvider)Providers.Instance.DataProvider).DB = DataProvider.DB;
                builder.UseInitialData(null).UseSearchEngine(SearchEngine).AddBlobProvider(BlobProvider);
            }
        }
    }
}
