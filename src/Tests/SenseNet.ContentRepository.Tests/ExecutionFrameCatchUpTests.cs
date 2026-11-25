using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Search.Indexing.Activities;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;
using SenseNet.Tests.Core;
using ISnTracer = SenseNet.Diagnostics.ISnTracer;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ExecutionFrameCatchUpTests : TestBase
    {
        [TestMethod]
        public void ExecutionFrameCatchUp_FormatsIdRanges()
        {
            Assert.AreEqual("[]", ExecutionFrameIndexCatchUpProvider.FormatIdRanges([]));
            Assert.AreEqual("[100, 101, 105-107, 110, 113, 120-141]", ExecutionFrameIndexCatchUpProvider.FormatIdRanges(
                [100, 101, 105, 106, 107, 110, 113, .. Enumerable.Range(120, 22)]));
            Assert.AreEqual("[2147483645-2147483647]", ExecutionFrameIndexCatchUpProvider.FormatIdRanges(
                [int.MaxValue - 2, int.MaxValue - 1, int.MaxValue]));
        }

        [TestMethod, TestCategory("IR")]
        public async STT.Task ExecutionFrameCatchUp_AggregatesActivitiesBeforeSplittingDocumentWrites()
        {
            const int repeatedActivityCount = 250;
            const int deletedVersionCount = 200;
            var cancel = CancellationToken.None;
            RepositoryFixture fixture = null;
            IndexingActivityStatus checkpoint = null;
            var coalescedFrom = 0;
            var coalescedThrough = 0;
            var skippedId = 0;
            var updatedVersionId = 0;

            await Test(async () =>
            {
                fixture = new RepositoryFixture();
                var folder = new SystemFolder(Repository.Root) { Name = "frame-trace-target" };
                await folder.SaveAsync(cancel);
                updatedVersionId = folder.VersionId;
                checkpoint = new IndexingActivityStatus
                {
                    LastActivityId = await fixture.DataProvider.GetLastIndexingActivityIdAsync(cancel)
                };

                var indexManager = Providers.Instance.IndexManager;
                async STT.Task<int> RegisterUpdateAsync(int[] deletedVersions = null)
                {
                    var activity = new UpdateDocumentActivity
                    {
                        ActivityType = IndexingActivityType.UpdateDocument,
                        NodeId = folder.Id,
                        VersionId = folder.VersionId,
                        Path = folder.Path,
                        Versioning = new VersioningInfo { Delete = deletedVersions, Reindex = [] }
                    };
                    await indexManager.RegisterActivityAsync(activity, cancel);
                    return activity.Id;
                }

                await RegisterUpdateAsync();
                coalescedFrom = await RegisterUpdateAsync();
                for (var i = 1; i < repeatedActivityCount; i++)
                    coalescedThrough = await RegisterUpdateAsync();
                await RegisterUpdateAsync(Enumerable.Range(int.MaxValue - deletedVersionCount + 1, deletedVersionCount).ToArray());
                var restore = new RestoreActivity { ActivityType = IndexingActivityType.Restore, Path = folder.Path };
                await indexManager.RegisterActivityAsync(restore, cancel);
                skippedId = restore.Id;
            });

            await fixture.SearchEngine.IndexingEngine.WriteActivityStatusToIndexAsync(checkpoint, cancel);
            var tracer = new CatchUpTracer();
            Test2(services => services.AddSenseNetExecutionFrameIndexCatchUpProvider().AddSingleton<ISnTracer>(tracer),
                builder =>
                {
                    fixture.Configure(builder);
                    builder.UseTraceCategories("Index");
                }, () =>
                {
                    var activityLog = tracer.Lines.Single(line => line.Contains("Skipped activities"));
                    StringAssert.Contains(activityLog, $"Skipped activities [{skippedId}].");
                    StringAssert.Contains(activityLog, $"Coalesced activities [{coalescedFrom}-{coalescedThrough}].");
                    var writeLogs = tracer.Lines.Where(line => line.Contains("applied an index batch to the session")).ToArray();
                    Assert.AreEqual(2, writeLogs.Length);
                    Assert.AreEqual(1, writeLogs.Count(line => line.Contains($"Upsert by VersionId [{updatedVersionId}].")));
                    Assert.IsTrue(writeLogs.Any(line => line.Contains($"{int.MaxValue}]")));
                });
        }

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

            await Test2(services => services.AddSenseNetExecutionFrameIndexCatchUpProvider(), fixture.Configure, async () =>
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

            await Test2(services => services.AddSenseNetLegacyIndexCatchUpProvider(), fixture.Configure, async () =>
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

            Test2(services => services.AddSenseNetExecutionFrameIndexCatchUpProvider(), fixture.Configure, () =>
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
                Test2(services => services.AddSenseNetExecutionFrameIndexCatchUpProvider(), fixture.Configure,
                    () => Assert.Fail("Startup should fail on the damaged version.")));

            var indexingEngine = fixture.SearchEngine.IndexingEngine;
            Assert.AreSame(checkpoint, fixture.SearchEngine.Index);
            Assert.IsNotNull(indexingEngine.GetIndexDocumentByVersionId(deletedVersion));
            Assert.IsNull(indexingEngine.GetIndexDocumentByVersionId(damagedVersion));
            var actual = await indexingEngine.ReadActivityStatusFromIndexAsync(CancellationToken.None);
            Assert.AreEqual(checkpointStatus.LastActivityId, actual.LastActivityId);
            CollectionAssert.AreEqual(checkpointStatus.Gaps, actual.Gaps);
        }

        [TestMethod, TestCategory("IR")]
        [DataRow(true, true)]
        [DataRow(false, true)]
        [DataRow(false, false)]
        public async STT.Task ExecutionFrameCatchUp_StartupAfterFullReindex(bool emptyIndex, bool resetCheckpoint)
        {
            var cancel = CancellationToken.None;
            RepositoryFixture fixture = null;
            var versionId = 0;
            var lastAllocatedActivityId = 0;

            await Test(async () =>
            {
                fixture = new RepositoryFixture();
                var folder = new SystemFolder(Repository.Root) { Name = "frame-empty-index" };
                await folder.SaveAsync(cancel);
                versionId = folder.VersionId;
                lastAllocatedActivityId = await fixture.DataProvider.GetLastIndexingActivityIdAsync(cancel);
                Assert.IsTrue(lastAllocatedActivityId > 0);
                await Providers.Instance.SearchManager.GetIndexPopulator().ClearAndPopulateAllAsync(cancel);
                Assert.AreEqual(0, fixture.DataProvider.DB.IndexingActivities.Count);
                Assert.AreEqual(lastAllocatedActivityId, await fixture.DataProvider.GetLastIndexingActivityIdAsync(cancel));
            });

            var engine = fixture.SearchEngine.IndexingEngine;
            if (emptyIndex)
                await engine.ClearIndexAsync(cancel);
            if (resetCheckpoint)
                await engine.WriteActivityStatusToIndexAsync(IndexingActivityStatus.Startup, cancel);

            void ConfigureServices(IServiceCollection services) => services.AddSenseNetExecutionFrameIndexCatchUpProvider();

            async STT.Task AssertIndexAsync()
            {
                Assert.IsNotNull(engine.GetIndexDocumentByVersionId(versionId));
                var status = await engine.ReadActivityStatusFromIndexAsync(cancel);
                Assert.AreEqual(lastAllocatedActivityId, status.LastActivityId);
                CollectionAssert.AreEqual(Array.Empty<int>(), status.Gaps);
            }

            await Test2(ConfigureServices, fixture.Configure, AssertIndexAsync);
            await Test2(ConfigureServices, fixture.Configure, AssertIndexAsync);
        }

        [TestMethod, TestCategory("IR")]
        public async STT.Task ExecutionFrameCatchUp_SkipsMissingActivityAndReplaysItsNeighbors()
        {
            var cancel = CancellationToken.None;
            RepositoryFixture fixture = null;
            var precedingVersionId = 0;
            var followingVersionId = 0;
            var precedingActivityId = 0;
            var missingActivityId = 0;
            var lastActivityId = 0;

            await Test(async () =>
            {
                fixture = new RepositoryFixture();
                var preceding = new SystemFolder(Repository.Root) { Name = "frame-before-missing-activity" };
                await preceding.SaveAsync(cancel);
                precedingVersionId = preceding.VersionId;

                var missing = new RestoreActivity { ActivityType = IndexingActivityType.Restore, Path = Repository.RootPath };
                await Providers.Instance.IndexManager.RegisterActivityAsync(missing, cancel);
                missingActivityId = missing.Id;

                var following = new SystemFolder(Repository.Root) { Name = "frame-after-missing-activity" };
                await following.SaveAsync(cancel);
                followingVersionId = following.VersionId;

                var activityTable = fixture.DataProvider.DB.IndexingActivities;
                precedingActivityId = activityTable.Single(activity => activity.VersionId == precedingVersionId).IndexingActivityId;
                lastActivityId = await fixture.DataProvider.GetLastIndexingActivityIdAsync(cancel);
                activityTable.Remove(activityTable.Single(activity => activity.IndexingActivityId == missingActivityId));
            });

            var engine = fixture.SearchEngine.IndexingEngine;
            await engine.WriteIndexAsync(
                [new SnTerm(IndexFieldName.VersionId, precedingVersionId), new SnTerm(IndexFieldName.VersionId, followingVersionId)],
                null, null, cancel);
            await engine.WriteActivityStatusToIndexAsync(new IndexingActivityStatus
            {
                LastActivityId = missingActivityId,
                Gaps = [precedingActivityId, missingActivityId]
            }, cancel);

            var tracer = new CatchUpTracer();
            var dataProvider = new CountingActivityDataProvider();
            await Test2(services => services.AddSenseNetExecutionFrameIndexCatchUpProvider()
                    .AddSingleton<DataProvider>(dataProvider).AddSingleton<ISnTracer>(tracer),
                builder =>
                {
                    fixture.Configure(builder);
                    builder.UseTraceCategories("Index");
                }, async () =>
                {
                    Assert.IsNotNull(engine.GetIndexDocumentByVersionId(precedingVersionId));
                    Assert.IsNotNull(engine.GetIndexDocumentByVersionId(followingVersionId));
                    var status = await engine.ReadActivityStatusFromIndexAsync(cancel);
                    Assert.AreEqual(lastActivityId, status.LastActivityId);
                    CollectionAssert.AreEqual(Array.Empty<int>(), status.Gaps);
                    Assert.AreEqual(1, dataProvider.MetadataPageReads);
                    Assert.IsTrue(tracer.Lines.Any(line => line.Contains($"Skipped missing activities [{missingActivityId}].")));
                });
        }

        private sealed class CountingActivityDataProvider : InMemoryDataProvider
        {
            public int MetadataPageReads { get; private set; }

            public override STT.Task<IndexingActivityMetadata[]> LoadIndexingActivityMetadataAsync(
                int fromId, int toId, int count, CancellationToken cancellationToken)
            {
                MetadataPageReads++;
                return base.LoadIndexingActivityMetadataAsync(fromId, toId, count, cancellationToken);
            }
        }

        [TestMethod, TestCategory("IR")]
        public async STT.Task ExecutionFrameCatchUp_LogsAnUpToDateIndex()
        {
            RepositoryFixture fixture = null;
            var lastActivityId = 0;
            var cancel = CancellationToken.None;

            await Test(async () =>
            {
                fixture = new RepositoryFixture();
                lastActivityId = await fixture.DataProvider.GetLastIndexingActivityIdAsync(cancel);
                await fixture.SearchEngine.IndexingEngine.WriteActivityStatusToIndexAsync(
                    new IndexingActivityStatus { LastActivityId = lastActivityId }, cancel);
            });

            var logger = new CatchUpLogger();
            Test2(services => services.AddSenseNetExecutionFrameIndexCatchUpProvider()
                    .AddSingleton<ILogger<ExecutionFrameIndexCatchUpProvider>>(logger), fixture.Configure,
                () => Assert.AreEqual(
                    $"Execution frame catch-up was not needed. LastActivityId={lastActivityId}, Gaps=[].", logger.Message));
        }

        private sealed class CatchUpTracer : ISnTracer
        {
            public ConcurrentQueue<string> Lines { get; } = new();
            public void Write(string line) => Lines.Enqueue(line);
            public void Flush() { }
        }

        private sealed class CatchUpLogger : ILogger<ExecutionFrameIndexCatchUpProvider>
        {
            public string Message { get; private set; }

            public IDisposable BeginScope<TState>(TState state) => null;
            public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Information;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                if (IsEnabled(logLevel))
                    Message = formatter(state, exception);
            }
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
