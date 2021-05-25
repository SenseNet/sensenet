using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Search.Indexing.Activities;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.Testing;

namespace SenseNet.IntegrationTests.TestCases
{
    public class CentralizedIndexingTestCases : TestCaseBase
    {
        // ReSharper disable once InconsistentNaming
        private static DataProvider DP => Providers.Instance.DataProvider;

        public async Task Indexing_Centralized_RegisterAndReload()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                // Ensure an existing activity to get right last activity id.
                await DP.RegisterIndexingActivityAsync(
                    CreateActivity(IndexingActivityType.AddDocument, "/Root/42", 42, 42, 99999999, null),
                    CancellationToken.None);
                var lastActivityIdBefore = await DP.GetLastIndexingActivityIdAsync(CancellationToken.None);
                await DP.DeleteAllIndexingActivitiesAsync(CancellationToken.None);

                var timeAtStart = DateTime.UtcNow;

                // avoid rounding errors: reloaded datetime can be less than timeAtStart
                // if rounding in sql converts the input to smaller value.
                Thread.Sleep(10);

                var activities = new[]
                {
                    CreateActivity(IndexingActivityType.AddDocument, "/Root/42", 42, 42, 99999999, null),
                    CreateActivity(IndexingActivityType.UpdateDocument, "/Root/43", 43, 43, 99999999, null),
                    CreateTreeActivity(IndexingActivityType.AddTree, "/Root/44", 44, null),
                    CreateTreeActivity(IndexingActivityType.RemoveTree, "/Root/45", 45, null),
                    CreateActivity(IndexingActivityType.Rebuild, "/Root/46", 46, 46, 99999999, null),
                };

                foreach (var activity in activities)
                    await DP.RegisterIndexingActivityAsync(activity, CancellationToken.None);

                // avoid rounding errors: reloaded datetime can be greater than timeAtEnd
                // if rounding in sql converts the input to greater value.
                Thread.Sleep(10);

                var timeAtEnd = DateTime.UtcNow;

                var lastActivityIdAfter = await DP.GetLastIndexingActivityIdAsync(CancellationToken.None);
                Assert.AreEqual(lastActivityIdBefore + 5, lastActivityIdAfter);

                var factory = new IndexingActivityFactory();

                // ---- simulating system start
                var unprocessedActivities = await DP.LoadIndexingActivitiesAsync(lastActivityIdBefore + 1,
                    lastActivityIdAfter, 1000, true, factory, CancellationToken.None);
                Assert.AreEqual(5, unprocessedActivities.Length);
                Assert.AreEqual(
                    $"{IndexingActivityType.AddDocument}, {IndexingActivityType.UpdateDocument}, {IndexingActivityType.AddTree}, {IndexingActivityType.RemoveTree}, {IndexingActivityType.Rebuild}",
                    string.Join(", ", unprocessedActivities.Select(x => x.ActivityType.ToString()).ToArray()));
                Assert.AreEqual(
                    "42, 43, 44, 45, 46",
                    string.Join(", ", unprocessedActivities.Select(x => x.NodeId.ToString()).ToArray()));
                Assert.AreEqual(
                    "42, 43, 0, 0, 46",
                    string.Join(", ", unprocessedActivities.Select(x => x.VersionId.ToString()).ToArray()));
                Assert.AreEqual(
                    "/root/42, /root/43, /root/44, /root/45, /root/46",
                    string.Join(", ", unprocessedActivities.Select(x => x.Path)));

                for (int i = 0; i < unprocessedActivities.Length; i++)
                {
                    Assert.AreEqual(lastActivityIdBefore + i + 1, unprocessedActivities[i].Id);
                    Assert.IsTrue(timeAtStart <= unprocessedActivities[i].CreationDate &&
                                  unprocessedActivities[i].CreationDate <= timeAtEnd);
                    Assert.IsTrue(unprocessedActivities[i].IsUnprocessedActivity);
                    Assert.AreEqual(IndexingActivityRunningState.Waiting, unprocessedActivities[i].RunningState);
                    Assert.IsNull(unprocessedActivities[i].LockTime);
                }

                // ---- simulating runtime maintenance
                var loadedActivities = await DP.LoadIndexingActivitiesAsync(lastActivityIdBefore + 1,
                    lastActivityIdAfter, 1000, false, factory, CancellationToken.None);
                Assert.AreEqual(5, loadedActivities.Length);
                Assert.AreEqual(
                    $"{IndexingActivityType.AddDocument}, {IndexingActivityType.UpdateDocument}, {IndexingActivityType.AddTree}, {IndexingActivityType.RemoveTree}, {IndexingActivityType.Rebuild}",
                    string.Join(", ", loadedActivities.Select(x => x.ActivityType.ToString()).ToArray()));
                Assert.AreEqual(
                    "42, 43, 44, 45, 46",
                    string.Join(", ", loadedActivities.Select(x => x.NodeId.ToString()).ToArray()));
                Assert.AreEqual(
                    "42, 43, 0, 0, 46",
                    string.Join(", ", loadedActivities.Select(x => x.VersionId.ToString()).ToArray()));
                Assert.AreEqual(
                    "/root/42, /root/43, /root/44, /root/45, /root/46",
                    string.Join(", ", loadedActivities.Select(x => x.Path)));

                for (int i = 0; i < loadedActivities.Length; i++)
                {
                    Assert.AreEqual(lastActivityIdBefore + i + 1, loadedActivities[i].Id);
                    Assert.IsTrue(timeAtStart <= loadedActivities[i].CreationDate &&
                                  loadedActivities[i].CreationDate <= timeAtEnd);
                    Assert.IsFalse(loadedActivities[i].IsUnprocessedActivity);
                    Assert.AreEqual(IndexingActivityRunningState.Waiting, unprocessedActivities[i].RunningState);
                    Assert.IsNull(unprocessedActivities[i].LockTime);
                }

                var gaps = new[] { lastActivityIdBefore + 1, lastActivityIdBefore + 2 };
                // ---- simulating system start
                var unprocessedActivitiesFromGaps = await DP.LoadIndexingActivitiesAsync(gaps, true, factory, CancellationToken.None);
                Assert.AreEqual(
                    $"{lastActivityIdBefore + 1}, {lastActivityIdBefore + 2}",
                    string.Join(", ", unprocessedActivitiesFromGaps.Select(x => x.Id.ToString()).ToArray()));
                Assert.AreEqual(
                    "True, True",
                    string.Join(", ",
                        unprocessedActivitiesFromGaps.Select(x => x.IsUnprocessedActivity.ToString()).ToArray()));
                // ---- simulating runtime maintenance
                var loadedActivitiesFromGaps = await DP.LoadIndexingActivitiesAsync(gaps, false, factory, CancellationToken.None);
                Assert.AreEqual(
                    $"{lastActivityIdBefore + 1}, {lastActivityIdBefore + 2}",
                    string.Join(", ", loadedActivitiesFromGaps.Select(x => x.Id.ToString()).ToArray()));
                Assert.AreEqual(
                    "False, False",
                    string.Join(", ",
                        loadedActivitiesFromGaps.Select(x => x.IsUnprocessedActivity.ToString()).ToArray()));
            });
        }

        public async Task Indexing_Centralized_UpdateStateToDone()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await DP.DeleteAllIndexingActivitiesAsync(CancellationToken.None);
                var lastActivityIdBefore = await DP.GetLastIndexingActivityIdAsync(CancellationToken.None);
                await DP.RegisterIndexingActivityAsync(
                    CreateActivity(
                        IndexingActivityType.AddDocument, "/Root/Indexing_Centralized_UpdateStateToDone", 42, 42, 99999999, null), CancellationToken.None);
                var lastActivityIdAfter = await DP.GetLastIndexingActivityIdAsync(CancellationToken.None);

                var factory = new IndexingActivityFactory();
                var loadedActivities = await DP.LoadIndexingActivitiesAsync(lastActivityIdBefore + 1, lastActivityIdAfter, 1000, false, factory, CancellationToken.None);
                if (loadedActivities.Length != 1)
                    Assert.Inconclusive("Successful test needs one IndexingActivity.");
                var loadedActivity = loadedActivities[0];
                if (loadedActivity == null)
                    Assert.Inconclusive("Successful test needs the existing IndexingActivity.");
                if (loadedActivity.RunningState != IndexingActivityRunningState.Waiting)
                    Assert.Inconclusive("Successful test needs the requirement: ActivityState is IndexingActivityState.Waiting.");

                // action
                await DP.UpdateIndexingActivityRunningStateAsync(loadedActivity.Id, IndexingActivityRunningState.Done, CancellationToken.None);

                // check
                var lastActivityIdAfterUpdate = await DP.GetLastIndexingActivityIdAsync(CancellationToken.None);
                Assert.AreEqual(lastActivityIdAfter, lastActivityIdAfterUpdate);

                loadedActivity = (await DP.LoadIndexingActivitiesAsync(lastActivityIdBefore + 1, lastActivityIdAfter, 1000, false, factory, CancellationToken.None)).First();
                Assert.AreEqual(IndexingActivityRunningState.Done, loadedActivity.RunningState);
            });
        }

        public async Task Indexing_Centralized_Allocate01_SelectWaiting()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await DP.DeleteAllIndexingActivitiesAsync(CancellationToken.None);
                var start = new[]
                {
                    await RegisterActivityAsync(IndexingActivityType.AddDocument, IndexingActivityRunningState.Done, 1, 1, "/Root/Path1"),
                    await RegisterActivityAsync(IndexingActivityType.AddDocument, IndexingActivityRunningState.Running, 2, 2, "/Root/Path2"),
                    await RegisterActivityAsync(IndexingActivityType.AddDocument, IndexingActivityRunningState.Waiting, 3, 3, "/Root/Path3"),
                };

                var loaded = await DP.LoadExecutableIndexingActivitiesAsync(new IndexingActivityFactory(), 10, 60, new int[0], CancellationToken.None);

                var activities = loaded.Activities;
                Assert.AreEqual(1, activities.Length);
                Assert.AreEqual(start[2].Id, activities[0].Id);
            });
        }
        public async Task Indexing_Centralized_Allocate02_IdDependency()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await DP.DeleteAllIndexingActivitiesAsync(CancellationToken.None);
                var start = new[]
                {
                    await RegisterActivityAsync(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Waiting, 1, 1, "/Root/Path1"),
                    await RegisterActivityAsync(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 1, 1, "/Root/Path1"),
                    await RegisterActivityAsync(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 1, 1, "/Root/Path1"),
                };

                var loaded = await DP.LoadExecutableIndexingActivitiesAsync(new IndexingActivityFactory(), 10, 60, new int[0], CancellationToken.None);

                var activities = loaded.Activities;
                Assert.AreEqual(1, activities.Length);
                Assert.AreEqual(start[0].Id, activities[0].Id);
            });
        }
        public async Task Indexing_Centralized_Allocate02_IdDependency_VersionId0()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await DP.DeleteAllIndexingActivitiesAsync(CancellationToken.None);
                var start = new[]
                {
                    await RegisterActivityAsync(IndexingActivityType.Rebuild, IndexingActivityRunningState.Waiting, 1, 0, "/Root/Path1"),
                    await RegisterActivityAsync(IndexingActivityType.Rebuild, IndexingActivityRunningState.Waiting, 2, 0, "/Root/Path2"),
                    await RegisterActivityAsync(IndexingActivityType.Rebuild, IndexingActivityRunningState.Waiting, 3, 0, "/Root/Path3"),
                };

                var loaded = await DP.LoadExecutableIndexingActivitiesAsync(new IndexingActivityFactory(), 10, 60, new int[0], CancellationToken.None);

                var activities = loaded.Activities;
                Assert.AreEqual(3, activities.Length);
                Assert.AreEqual(start[0].Id, activities[0].Id);
                Assert.AreEqual(start[1].Id, activities[1].Id);
                Assert.AreEqual(start[2].Id, activities[2].Id);
            });
        }
        public async Task Indexing_Centralized_Allocate03_InactiveDependency()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await DP.DeleteAllIndexingActivitiesAsync(CancellationToken.None);
                var start = new[]
                {
                    await RegisterActivityAsync(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Done,    1, 1, "/Root/Path1"),
                    await RegisterActivityAsync(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Done,    2, 2, "/Root/Path2"),
                    await RegisterActivityAsync(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 2, 2, "/Root/Path2"),
                    await RegisterActivityAsync(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 2, 2, "/Root/Path2"),
                    await RegisterActivityAsync(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 2, 2, "/Root/Path2"),
                };

                var loaded = await DP.LoadExecutableIndexingActivitiesAsync(new IndexingActivityFactory(), 10, 60, new int[0], CancellationToken.None);

                var activities = loaded.Activities;
                Assert.AreEqual(1, activities.Length);
                Assert.AreEqual(start[2].Id, activities[0].Id);
            });
        }
        public async Task Indexing_Centralized_Allocate04_SelectMore()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await DP.DeleteAllIndexingActivitiesAsync(CancellationToken.None);
                var start = new[]
                {
                    await RegisterActivityAsync(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Done,    1, 1, "/Root/Path1"), //   0
                    await RegisterActivityAsync(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Done,    2, 2, "/Root/Path2"), //   1
                    await RegisterActivityAsync(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 2, 2, "/Root/Path2"), // 2
                    await RegisterActivityAsync(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 2, 2, "/Root/Path2"), //   3
                    await RegisterActivityAsync(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 2, 2, "/Root/Path2"), //   4
                    await RegisterActivityAsync(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Waiting, 3, 3, "/Root/Path3"), // 5
                    await RegisterActivityAsync(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 3, 3, "/Root/Path3"), //   6
                    await RegisterActivityAsync(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Waiting, 4, 4, "/Root/Path4"), // 7
                    await RegisterActivityAsync(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 4, 4, "/Root/Path4"), //   8
                };

                var loaded = await DP.LoadExecutableIndexingActivitiesAsync(new IndexingActivityFactory(), 10, 60, new int[0], CancellationToken.None);

                var activities = loaded.Activities;
                Assert.AreEqual(3, activities.Length);
                Assert.AreEqual(start[2].Id, activities[0].Id);
                Assert.AreEqual(start[5].Id, activities[1].Id);
                Assert.AreEqual(start[7].Id, activities[2].Id);
            });
        }
        public async Task Indexing_Centralized_Allocate05_PathDependency()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await DP.DeleteAllIndexingActivitiesAsync(CancellationToken.None);
                var start = new[]
                {
                    await RegisterActivityAsync(IndexingActivityType.AddTree,        IndexingActivityRunningState.Waiting, 1, 1, "/Root/Path1"),     // 0
                    await RegisterActivityAsync(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Waiting, 2, 2, "/Root/Path2"),     // 1
                    await RegisterActivityAsync(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 3, 3, "/Root/Path1/aaa"), //   2
                    await RegisterActivityAsync(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 4, 4, "/Root/Path1/bbb"), //   3
                    await RegisterActivityAsync(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 2, 2, "/Root/Path2"),     //   4
                    await RegisterActivityAsync(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Waiting, 5, 5, "/Root/Path3/xxx"), // 5
                    await RegisterActivityAsync(IndexingActivityType.AddTree,        IndexingActivityRunningState.Waiting, 6, 6, "/Root/Path3"),     //   6
                    await RegisterActivityAsync(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Waiting, 7, 7, "/Root/Path4"),     // 7
                };

                var loaded = await DP.LoadExecutableIndexingActivitiesAsync(new IndexingActivityFactory(), 10, 60, new int[0], CancellationToken.None);

                var activities = loaded.Activities;
                Assert.AreEqual(4, activities.Length);
                Assert.AreEqual(start[0].Id, activities[0].Id);
                Assert.AreEqual(start[1].Id, activities[1].Id);
                Assert.AreEqual(start[5].Id, activities[2].Id);
                Assert.AreEqual(start[7].Id, activities[3].Id);
            });
        }
        public async Task Indexing_Centralized_Allocate06_Timeout()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await DP.DeleteAllIndexingActivitiesAsync(CancellationToken.None);
                var start = new[]
                {
                    await RegisterActivityAsync(IndexingActivityType.AddDocument, IndexingActivityRunningState.Done, DateTime.UtcNow.AddSeconds(-75), 1, 1, "/Root/Path1"),        //   0
                    await RegisterActivityAsync(IndexingActivityType.AddDocument, IndexingActivityRunningState.Done, DateTime.UtcNow.AddSeconds(-65), 2, 2, "/Root/Path2"),        //   1
                    await RegisterActivityAsync(IndexingActivityType.AddDocument, IndexingActivityRunningState.Done, DateTime.UtcNow.AddSeconds(-55), 3, 3, "/Root/Path3"),        //   2
                    await RegisterActivityAsync(IndexingActivityType.AddDocument, IndexingActivityRunningState.Done, DateTime.UtcNow,                 4, 4, "/Root/Path4"),        //   3

                    await RegisterActivityAsync(IndexingActivityType.AddDocument, IndexingActivityRunningState.Running, DateTime.UtcNow.AddSeconds(-75), 5, 5, "/Root/Path5"),     //   4
                    await RegisterActivityAsync(IndexingActivityType.AddDocument, IndexingActivityRunningState.Running, DateTime.UtcNow.AddSeconds(-65), 6, 6, "/Root/Path6"),     //   5
                    await RegisterActivityAsync(IndexingActivityType.AddDocument, IndexingActivityRunningState.Running, DateTime.UtcNow.AddSeconds(-55), 7, 7, "/Root/Path7"),     // 6
                    await RegisterActivityAsync(IndexingActivityType.AddDocument, IndexingActivityRunningState.Running, DateTime.UtcNow,                 8, 8, "/Root/Path8"),     // 7

                    await RegisterActivityAsync(IndexingActivityType.AddDocument, IndexingActivityRunningState.Waiting, DateTime.UtcNow.AddSeconds(-75), 10, 10, "/Root/Path10"),   // 8
                    await RegisterActivityAsync(IndexingActivityType.AddDocument, IndexingActivityRunningState.Waiting, DateTime.UtcNow.AddSeconds(-65), 11, 11, "/Root/Path11"),   // 9
                    await RegisterActivityAsync(IndexingActivityType.AddDocument, IndexingActivityRunningState.Waiting, DateTime.UtcNow.AddSeconds(-55), 12, 12, "/Root/Path12"),   // 10
                    await RegisterActivityAsync(IndexingActivityType.AddDocument, IndexingActivityRunningState.Waiting, DateTime.UtcNow,                 13, 13, "/Root/Path13"),   // 11
                };

                var loaded = await DP.LoadExecutableIndexingActivitiesAsync(new IndexingActivityFactory(), 10, 60, new int[0], CancellationToken.None);

                var activities = loaded.Activities;
                Assert.AreEqual(6, activities.Length);
                Assert.AreEqual(start[4].Id, activities[0].Id);
                Assert.AreEqual(start[5].Id, activities[1].Id);
                Assert.AreEqual(start[8].Id, activities[2].Id);
                Assert.AreEqual(start[9].Id, activities[3].Id);
                Assert.AreEqual(start[10].Id, activities[4].Id);
                Assert.AreEqual(start[11].Id, activities[5].Id);
            });
        }
        public async Task Indexing_Centralized_Allocate07_MaxRecords()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await DP.DeleteAllIndexingActivitiesAsync(CancellationToken.None);
                var start = new IIndexingActivity[15];
                for (int i = 1; i <= start.Length; i++)
                {
                    start[i - 1] = await RegisterActivityAsync(IndexingActivityType.AddDocument, IndexingActivityRunningState.Waiting, i, i, $"/Root/Path0{i}");
                }

                var loaded = await DP.LoadExecutableIndexingActivitiesAsync(new IndexingActivityFactory(), 10, 60, new int[0], CancellationToken.None);

                var activities = loaded.Activities;
                Assert.AreEqual(10, activities.Length);
                for (var i = 0; i < 10; i++)
                    Assert.AreEqual(start[i].Id, activities[i].Id);
            });
        }
        public async Task Indexing_Centralized_Allocate08_StateUpdated()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await DP.DeleteAllIndexingActivitiesAsync(CancellationToken.None);
                var start = new[]
                {
                    await RegisterActivityAsync(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Waiting, 1, 1, "/Root/Path1"),
                    await RegisterActivityAsync(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 1, 1, "/Root/Path1"),
                    await RegisterActivityAsync(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Waiting, 2, 2, "/Root/Path2"),
                    await RegisterActivityAsync(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 2, 2, "/Root/Path2"),
                };

                var loaded = await DP.LoadExecutableIndexingActivitiesAsync(new IndexingActivityFactory(), 10, 60, new int[0], CancellationToken.None);

                var activities = loaded.Activities;
                Assert.AreEqual(2, activities.Length);
                Assert.AreEqual(start[0].Id, activities[0].Id);
                Assert.AreEqual(start[2].Id, activities[1].Id);
                Assert.AreEqual(IndexingActivityRunningState.Running, activities[0].RunningState);
                Assert.AreEqual(IndexingActivityRunningState.Running, activities[1].RunningState);
            });
        }

        public async Task Indexing_Centralized_AllocateAndState()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await DP.DeleteAllIndexingActivitiesAsync(CancellationToken.None);
                var start = new[]
                {
                    await RegisterActivityAsync(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Done, 1, 1, "/Root/Path1"),
                    await RegisterActivityAsync(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 1, 1, "/Root/Path1"),
                    await RegisterActivityAsync(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Running, 2, 2, "/Root/Path2"),
                    await RegisterActivityAsync(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 2, 2, "/Root/Path2"),
                    await RegisterActivityAsync(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Waiting, 3, 3, "/Root/Path3"),
                    await RegisterActivityAsync(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 3, 3, "/Root/Path3"),
                };

                var waitingIds = new[] { start[0].Id, start[2].Id };

                var loaded = await DP.LoadExecutableIndexingActivitiesAsync(new IndexingActivityFactory(), 10, 60, waitingIds, CancellationToken.None);

                var finishetIds = loaded.FinishedActivitiyIds;
                Assert.AreEqual(1, finishetIds.Length);
                Assert.AreEqual(start[0].Id, finishetIds[0]);

                var activities = loaded.Activities;
                Assert.AreEqual(2, activities.Length);
                Assert.AreEqual(start[1].Id, activities[0].Id);
                Assert.AreEqual(start[4].Id, activities[1].Id);
                Assert.AreEqual(IndexingActivityRunningState.Running, activities[0].RunningState);
                Assert.AreEqual(IndexingActivityRunningState.Running, activities[1].RunningState);
            });
        }
        public async Task Indexing_Centralized_RefreshLock()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await DP.DeleteAllIndexingActivitiesAsync(CancellationToken.None);
                var now = DateTime.UtcNow;
                var time1 = now.AddSeconds(-60 * 6);
                var time2 = now.AddSeconds(-60 * 5);
                var time3 = now.AddSeconds(-60 * 4);
                var time4 = now.AddSeconds(-60 * 3);
                var start = new[]
                {
                    await RegisterActivityAsync(IndexingActivityType.Rebuild, IndexingActivityRunningState.Waiting, time1, 1, 1, "/Root/Path1"),
                    await RegisterActivityAsync(IndexingActivityType.Rebuild, IndexingActivityRunningState.Waiting, time2, 2, 2, "/Root/Path2"),
                    await RegisterActivityAsync(IndexingActivityType.Rebuild, IndexingActivityRunningState.Waiting, time3, 3, 3, "/Root/Path3"),
                    await RegisterActivityAsync(IndexingActivityType.Rebuild, IndexingActivityRunningState.Waiting, time4, 4, 4, "/Root/Path4"),
                };
                var startIds = start.Select(x => x.Id).ToArray();

                var waitingIds = new[] { start[1].Id, start[2].Id };
                await DP.RefreshIndexingActivityLockTimeAsync(waitingIds, CancellationToken.None);

                var activities = await DP.LoadIndexingActivitiesAsync(startIds, false, IndexingActivityFactory.Instance, CancellationToken.None);

                Assert.AreEqual(4, activities.Length);
                Assert.IsTrue(time1 == activities[0].LockTime);
                Assert.IsTrue(now <= activities[1].LockTime);
                Assert.IsTrue(now <= activities[2].LockTime);
                Assert.IsTrue(time4 == activities[3].LockTime);
            });
        }
        public async Task Indexing_Centralized_DeleteFinished()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await DP.DeleteAllIndexingActivitiesAsync(CancellationToken.None);
                var start = new[]
                {
                    await RegisterActivityAsync(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Done,    1, 1, "/Root/Path1"),
                    await RegisterActivityAsync(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Done,    2, 2, "/Root/Path2"),
                    await RegisterActivityAsync(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Running, 3, 3, "/Root/Path3"),
                    await RegisterActivityAsync(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 3, 3, "/Root/Path3"),
                    await RegisterActivityAsync(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Done,    4, 4, "/Root/Path4"),
                    await RegisterActivityAsync(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Waiting, 5, 5, "/Root/Path5"),
                };

                await DP.DeleteFinishedIndexingActivitiesAsync(CancellationToken.None);

                var loaded = await DP.LoadIndexingActivitiesAsync(0, int.MaxValue, 9999, false, IndexingActivityFactory.Instance, CancellationToken.None);

                Assert.AreEqual(3, loaded.Length);
                Assert.AreEqual(start[2].Id, loaded[0].Id);
                Assert.AreEqual(start[3].Id, loaded[1].Id);
                Assert.AreEqual(start[5].Id, loaded[2].Id);
            });
        }

        /* ====================================================================================== */

        private async Task<IIndexingActivity> RegisterActivityAsync(IndexingActivityType type, IndexingActivityRunningState state, int nodeId, int versionId, string path)
        {
            IndexingActivityBase activity;
            if (type == IndexingActivityType.AddTree || type == IndexingActivityType.RemoveTree)
                activity = CreateTreeActivity(type, path, nodeId, null);
            else
                activity = CreateActivity(type, path, nodeId, versionId, 9999, null);
            activity.RunningState = state;

            await DP.RegisterIndexingActivityAsync(activity, CancellationToken.None);

            return activity;
        }
        private async Task<IIndexingActivity> RegisterActivityAsync(IndexingActivityType type, IndexingActivityRunningState state, DateTime lockTime, int nodeId, int versionId, string path)
        {
            IndexingActivityBase activity;
            if (type == IndexingActivityType.AddTree || type == IndexingActivityType.RemoveTree)
                activity = CreateTreeActivity(type, path, nodeId, null);
            else
                activity = CreateActivity(type, path, nodeId, versionId, 9999, null);

            activity.RunningState = state;
            activity.LockTime = lockTime;

            await DP.RegisterIndexingActivityAsync(activity, CancellationToken.None);

            return activity;
        }

        private static IndexingActivityBase CreateActivity(IndexingActivityType type, string path, int nodeId, int versionId, long versionTimestamp, IndexDocumentData indexDocumentData)
        {
            return DocumentPopulator.CreateActivity(type, path, nodeId, versionId, versionTimestamp, null, indexDocumentData);
        }
        private static IndexingActivityBase CreateTreeActivity(IndexingActivityType type, string path, int nodeId, IndexDocumentData indexDocumentData)
        {
            return DocumentPopulator.CreateTreeActivity(type, path, nodeId, indexDocumentData);
        }
    }
}
