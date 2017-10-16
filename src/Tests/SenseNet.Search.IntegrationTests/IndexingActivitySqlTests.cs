using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Tests;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search.Indexing;
using SenseNet.Search.Indexing.Activities;
using SenseNet.ContentRepository.Storage;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

namespace SenseNet.Search.IntegrationTests
{
    [TestClass]
    public class IndexingActivitySqlTests : TestBase
    {
        private static string _connectionString = @"Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=sn7tests;Data Source=.\SQL2016";

        [ClassInitialize]
        public static void InitializeClass(TestContext context)
        {
            CleanupIndexingActivitiesTable();
        }
        public static void CleanupIndexingActivitiesTable()
        {
            var connectionStringBackup = SenseNet.Configuration.ConnectionStrings.ConnectionString;
            SenseNet.Configuration.ConnectionStrings.ConnectionString = _connectionString;
            try
            {
                DataProvider.Current.DeleteAllIndexingActivities();
            }
            finally
            {
                SenseNet.Configuration.ConnectionStrings.ConnectionString = connectionStringBackup;
            }
        }

        [TestMethod, TestCategory("IR")]
        public void Indexing_Centralized_Sql_RegisterAndReload()
        {
            var connectionStringBackup = SenseNet.Configuration.ConnectionStrings.ConnectionString;
            SenseNet.Configuration.ConnectionStrings.ConnectionString = _connectionString;
            try
            {
                var lastActivityIdBefore = DataProvider.Current.GetLastActivityId();

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
                    DataProvider.Current.RegisterIndexingActivity(activity);

                // avoid rounding errors: reloaded datetime can be greater than timeAtEnd
                // if rounding in sql converts the input to greater value.
                Thread.Sleep(10);

                var timeAtEnd = DateTime.UtcNow;

                var lastActivityIdAfter = DataProvider.Current.GetLastActivityId();
                Assert.AreEqual(lastActivityIdBefore + 5, lastActivityIdAfter);

                var factory = new IndexingActivityFactory();

                // ---- simulating system start
                var unprocessedActivities = DataProvider.Current.LoadIndexingActivities(lastActivityIdBefore + 1, lastActivityIdAfter, 1000, true, factory);
                Assert.AreEqual(5, unprocessedActivities.Length);
                Assert.AreEqual(
                    $"{IndexingActivityType.AddDocument}, {IndexingActivityType.UpdateDocument}, {IndexingActivityType.AddTree}, {IndexingActivityType.RemoveTree}, {IndexingActivityType.Rebuild}",
                    string.Join(", ", unprocessedActivities.Select(x => x.ActivityType.ToString()).ToArray()));
                Assert.AreEqual(
                    $"42, 43, 44, 45, 46",
                    string.Join(", ", unprocessedActivities.Select(x => x.NodeId.ToString()).ToArray()));
                Assert.AreEqual(
                    $"42, 43, 0, 0, 46",
                    string.Join(", ", unprocessedActivities.Select(x => x.VersionId.ToString()).ToArray()));
                Assert.AreEqual(
                    $"/root/42, /root/43, /root/44, /root/45, /root/46",
                    string.Join(", ", unprocessedActivities.Select(x => x.Path)));

                for (int i = 0; i < unprocessedActivities.Length; i++)
                {
                    Assert.AreEqual(lastActivityIdBefore + i + 1, unprocessedActivities[i].Id);
                    Assert.IsTrue(timeAtStart <= unprocessedActivities[i].CreationDate && unprocessedActivities[i].CreationDate <= timeAtEnd);
                    Assert.IsTrue(unprocessedActivities[i].IsUnprocessedActivity);
                    Assert.AreEqual(IndexingActivityRunningState.Waiting, unprocessedActivities[i].RunningState);
                    Assert.IsNull(unprocessedActivities[i].StartDate);
                }

                // ---- simulating runtime maintenance
                var loadedActivities = DataProvider.Current.LoadIndexingActivities(lastActivityIdBefore + 1, lastActivityIdAfter, 1000, false, factory);
                Assert.AreEqual(5, loadedActivities.Length);
                Assert.AreEqual(
                    $"{IndexingActivityType.AddDocument}, {IndexingActivityType.UpdateDocument}, {IndexingActivityType.AddTree}, {IndexingActivityType.RemoveTree}, {IndexingActivityType.Rebuild}",
                    string.Join(", ", loadedActivities.Select(x => x.ActivityType.ToString()).ToArray()));
                Assert.AreEqual(
                    $"42, 43, 44, 45, 46",
                    string.Join(", ", loadedActivities.Select(x => x.NodeId.ToString()).ToArray()));
                Assert.AreEqual(
                    $"42, 43, 0, 0, 46",
                    string.Join(", ", loadedActivities.Select(x => x.VersionId.ToString()).ToArray()));
                Assert.AreEqual(
                    $"/root/42, /root/43, /root/44, /root/45, /root/46",
                    string.Join(", ", loadedActivities.Select(x => x.Path)));

                for (int i = 0; i < loadedActivities.Length; i++)
                {
                    Assert.AreEqual(lastActivityIdBefore + i + 1, loadedActivities[i].Id);
                    Assert.IsTrue(timeAtStart <= loadedActivities[i].CreationDate && loadedActivities[i].CreationDate <= timeAtEnd);
                    Assert.IsFalse(loadedActivities[i].IsUnprocessedActivity);
                    Assert.AreEqual(IndexingActivityRunningState.Waiting, unprocessedActivities[i].RunningState);
                    Assert.IsNull(unprocessedActivities[i].StartDate);
                }

                var gaps = new[] { lastActivityIdBefore + 1, lastActivityIdBefore + 2 };
                // ---- simulating system start
                var unprocessedActivitiesFromGaps = DataProvider.Current.LoadIndexingActivities(gaps, true, factory);
                Assert.AreEqual(
                   $"{lastActivityIdBefore + 1}, {lastActivityIdBefore + 2}",
                   string.Join(", ", unprocessedActivitiesFromGaps.Select(x => x.Id.ToString()).ToArray()));
                Assert.AreEqual(
                   $"True, True",
                   string.Join(", ", unprocessedActivitiesFromGaps.Select(x => x.IsUnprocessedActivity.ToString()).ToArray()));
                // ---- simulating runtime maintenance
                var loadedActivitiesFromGaps = DataProvider.Current.LoadIndexingActivities(gaps, false, factory);
                Assert.AreEqual(
                   $"{lastActivityIdBefore + 1}, {lastActivityIdBefore + 2}",
                   string.Join(", ", loadedActivitiesFromGaps.Select(x => x.Id.ToString()).ToArray()));
                Assert.AreEqual(
                   $"False, False",
                   string.Join(", ", loadedActivitiesFromGaps.Select(x => x.IsUnprocessedActivity.ToString()).ToArray()));
            }
            finally
            {
                SenseNet.Configuration.ConnectionStrings.ConnectionString = connectionStringBackup;
            }
        }

        [TestMethod, TestCategory("IR")]
        public void Indexing_Centralized_Sql_UpdateStateToDone()
        {
            var connectionStringBackup = SenseNet.Configuration.ConnectionStrings.ConnectionString;
            SenseNet.Configuration.ConnectionStrings.ConnectionString = _connectionString;
            try
            {
                var lastActivityIdBefore = DataProvider.Current.GetLastActivityId();
                DataProvider.Current.RegisterIndexingActivity(
                    CreateActivity(
                        IndexingActivityType.AddDocument, "/Root/Indexing_Centralized_Sql_UpdateStateToDone", 42, 42, 99999999, null));
                var lastActivityIdAfter = DataProvider.Current.GetLastActivityId();

                var factory = new IndexingActivityFactory();
                var loadedActivities = DataProvider.Current.LoadIndexingActivities(lastActivityIdBefore + 1, lastActivityIdAfter, 1000, false, factory).ToArray();
                if (loadedActivities.Length != 1)
                    Assert.Inconclusive("Successful test needs one IndexingActivity.");
                var loadedActivity = loadedActivities[0];
                if (loadedActivity == null)
                    Assert.Inconclusive("Successful test needs the existing IndexingActivity.");
                if (loadedActivity.RunningState != IndexingActivityRunningState.Waiting)
                    Assert.Inconclusive("Successful test needs the requirement: ActivityState is IndexingActivityState.Waiting.");

                // action
                DataProvider.Current.UpdateIndexingActivityRunningState(loadedActivity.Id, IndexingActivityRunningState.Done);

                // check
                var lastActivityIdAfterUpdate = DataProvider.Current.GetLastActivityId();
                Assert.AreEqual(lastActivityIdAfter, lastActivityIdAfterUpdate);

                loadedActivity = DataProvider.Current.LoadIndexingActivities(lastActivityIdBefore + 1, lastActivityIdAfter, 1000, false, factory).First();
                Assert.AreEqual(IndexingActivityRunningState.Done, loadedActivity.RunningState);
            }
            finally
            {
                SenseNet.Configuration.ConnectionStrings.ConnectionString = connectionStringBackup;
            }
        }

        [TestMethod, TestCategory("IR")]
        public void Indexing_Centralized_Sql_Allocate01_SelectWaiting()
        {
            var connectionStringBackup = SenseNet.Configuration.ConnectionStrings.ConnectionString;
            SenseNet.Configuration.ConnectionStrings.ConnectionString = _connectionString;
            try
            {
                CleanupIndexingActivitiesTable();
                var start = new[]
                {
                    RegisterActivity(IndexingActivityType.AddDocument, IndexingActivityRunningState.Done, 1, 1, "/Root/Path1"),
                    RegisterActivity(IndexingActivityType.AddDocument, IndexingActivityRunningState.Running, 2, 2, "/Root/Path2"),
                    RegisterActivity(IndexingActivityType.AddDocument, IndexingActivityRunningState.Waiting, 3, 3, "/Root/Path3"),
                };

                var allocated =  DataProvider.Current.StartIndexingActivities(new IndexingActivityFactory(), 10, 60);

                Assert.AreEqual(1, allocated.Length);
                Assert.AreEqual(start[2].Id, allocated[0].Id);
            }
            finally
            {
                SenseNet.Configuration.ConnectionStrings.ConnectionString = connectionStringBackup;
            }
        }
        [TestMethod, TestCategory("IR")]
        public void Indexing_Centralized_Sql_Allocate02_IdDependency()
        {
            var connectionStringBackup = SenseNet.Configuration.ConnectionStrings.ConnectionString;
            SenseNet.Configuration.ConnectionStrings.ConnectionString = _connectionString;
            try
            {
                CleanupIndexingActivitiesTable();
                var start = new[]
                {
                    RegisterActivity(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Waiting, 1, 1, "/Root/Path1"),
                    RegisterActivity(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 1, 1, "/Root/Path1"),
                    RegisterActivity(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 1, 1, "/Root/Path1"),
                };

                var allocated =  DataProvider.Current.StartIndexingActivities(new IndexingActivityFactory(), 10, 60);

                Assert.AreEqual(1, allocated.Length);
                Assert.AreEqual(start[0].Id, allocated[0].Id);
            }
            finally
            {
                SenseNet.Configuration.ConnectionStrings.ConnectionString = connectionStringBackup;
            }
        }
        [TestMethod, TestCategory("IR")]
        public void Indexing_Centralized_Sql_Allocate03_InactiveDependency()
        {
            var connectionStringBackup = SenseNet.Configuration.ConnectionStrings.ConnectionString;
            SenseNet.Configuration.ConnectionStrings.ConnectionString = _connectionString;
            try
            {
                CleanupIndexingActivitiesTable();
                var start = new[]
                {
                    RegisterActivity(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Done,    1, 1, "/Root/Path1"),
                    RegisterActivity(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Done,    2, 2, "/Root/Path2"),
                    RegisterActivity(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 2, 2, "/Root/Path2"),
                    RegisterActivity(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 2, 2, "/Root/Path2"),
                    RegisterActivity(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 2, 2, "/Root/Path2"),
                };

                var allocated =  DataProvider.Current.StartIndexingActivities(new IndexingActivityFactory(), 10, 60);

                Assert.AreEqual(1, allocated.Length);
                Assert.AreEqual(start[2].Id, allocated[0].Id);
            }
            finally
            {
                SenseNet.Configuration.ConnectionStrings.ConnectionString = connectionStringBackup;
            }
        }
        [TestMethod, TestCategory("IR")]
        public void Indexing_Centralized_Sql_Allocate04_SelectMore()
        {
            var connectionStringBackup = SenseNet.Configuration.ConnectionStrings.ConnectionString;
            SenseNet.Configuration.ConnectionStrings.ConnectionString = _connectionString;
            try
            {
                CleanupIndexingActivitiesTable();
                var start = new[]
                {
                    RegisterActivity(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Done,    1, 1, "/Root/Path1"), //   0
                    RegisterActivity(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Done,    2, 2, "/Root/Path2"), //   1
                    RegisterActivity(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 2, 2, "/Root/Path2"), // 2
                    RegisterActivity(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 2, 2, "/Root/Path2"), //   3
                    RegisterActivity(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 2, 2, "/Root/Path2"), //   4
                    RegisterActivity(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Waiting, 3, 3, "/Root/Path3"), // 5
                    RegisterActivity(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 3, 3, "/Root/Path3"), //   6
                    RegisterActivity(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Waiting, 4, 4, "/Root/Path4"), // 7
                    RegisterActivity(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 4, 4, "/Root/Path4"), //   8
                };

                var allocated =  DataProvider.Current.StartIndexingActivities(new IndexingActivityFactory(), 10, 60);

                Assert.AreEqual(3, allocated.Length);
                Assert.AreEqual(start[2].Id, allocated[0].Id);
                Assert.AreEqual(start[5].Id, allocated[1].Id);
                Assert.AreEqual(start[7].Id, allocated[2].Id);
            }
            finally
            {
                SenseNet.Configuration.ConnectionStrings.ConnectionString = connectionStringBackup;
            }
        }
        [TestMethod, TestCategory("IR")]
        public void Indexing_Centralized_Sql_Allocate05_PathDependency()
        {
            var connectionStringBackup = SenseNet.Configuration.ConnectionStrings.ConnectionString;
            SenseNet.Configuration.ConnectionStrings.ConnectionString = _connectionString;
            try
            {
                CleanupIndexingActivitiesTable();
                var start = new[]
                {
                    RegisterActivity(IndexingActivityType.AddTree,        IndexingActivityRunningState.Waiting, 1, 1, "/Root/Path1"),     // 0
                    RegisterActivity(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Waiting, 2, 2, "/Root/Path2"),     // 1
                    RegisterActivity(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 3, 3, "/Root/Path1/aaa"), //   2
                    RegisterActivity(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 4, 4, "/Root/Path1/bbb"), //   3
                    RegisterActivity(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 2, 2, "/Root/Path2"),     //   4
                    RegisterActivity(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Waiting, 5, 5, "/Root/Path3/xxx"), // 5
                    RegisterActivity(IndexingActivityType.AddTree,        IndexingActivityRunningState.Waiting, 6, 6, "/Root/Path3"),     //   6
                    RegisterActivity(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Waiting, 7, 7, "/Root/Path4"),     // 7
                };

                var allocated =  DataProvider.Current.StartIndexingActivities(new IndexingActivityFactory(), 10, 60);

                Assert.AreEqual(4, allocated.Length);
                Assert.AreEqual(start[0].Id, allocated[0].Id);
                Assert.AreEqual(start[1].Id, allocated[1].Id);
                Assert.AreEqual(start[5].Id, allocated[2].Id);
                Assert.AreEqual(start[7].Id, allocated[3].Id);
            }
            finally
            {
                SenseNet.Configuration.ConnectionStrings.ConnectionString = connectionStringBackup;
            }
        }
        [TestMethod, TestCategory("IR")]
        public void Indexing_Centralized_Sql_Allocate06_Timeout()
        {
            var connectionStringBackup = SenseNet.Configuration.ConnectionStrings.ConnectionString;
            SenseNet.Configuration.ConnectionStrings.ConnectionString = _connectionString;
            try
            {
                CleanupIndexingActivitiesTable();
                var start = new[]
                {
                    RegisterActivity(IndexingActivityType.AddDocument, IndexingActivityRunningState.Done, DateTime.UtcNow.AddSeconds(-75), 1, 1, "/Root/Path1"),        //   0
                    RegisterActivity(IndexingActivityType.AddDocument, IndexingActivityRunningState.Done, DateTime.UtcNow.AddSeconds(-65), 2, 2, "/Root/Path2"),        //   1
                    RegisterActivity(IndexingActivityType.AddDocument, IndexingActivityRunningState.Done, DateTime.UtcNow.AddSeconds(-55), 3, 3, "/Root/Path3"),        //   2
                    RegisterActivity(IndexingActivityType.AddDocument, IndexingActivityRunningState.Done, DateTime.UtcNow,                 4, 4, "/Root/Path4"),        //   3

                    RegisterActivity(IndexingActivityType.AddDocument, IndexingActivityRunningState.Running, DateTime.UtcNow.AddSeconds(-75), 5, 5, "/Root/Path5"),     //   4
                    RegisterActivity(IndexingActivityType.AddDocument, IndexingActivityRunningState.Running, DateTime.UtcNow.AddSeconds(-65), 6, 6, "/Root/Path6"),     //   5
                    RegisterActivity(IndexingActivityType.AddDocument, IndexingActivityRunningState.Running, DateTime.UtcNow.AddSeconds(-55), 7, 7, "/Root/Path7"),     // 6
                    RegisterActivity(IndexingActivityType.AddDocument, IndexingActivityRunningState.Running, DateTime.UtcNow,                 8, 8, "/Root/Path8"),     // 7

                    RegisterActivity(IndexingActivityType.AddDocument, IndexingActivityRunningState.Waiting, DateTime.UtcNow.AddSeconds(-75), 10, 10, "/Root/Path10"),   // 8
                    RegisterActivity(IndexingActivityType.AddDocument, IndexingActivityRunningState.Waiting, DateTime.UtcNow.AddSeconds(-65), 11, 11, "/Root/Path11"),   // 9
                    RegisterActivity(IndexingActivityType.AddDocument, IndexingActivityRunningState.Waiting, DateTime.UtcNow.AddSeconds(-55), 12, 12, "/Root/Path12"),   // 10
                    RegisterActivity(IndexingActivityType.AddDocument, IndexingActivityRunningState.Waiting, DateTime.UtcNow,                 13, 13, "/Root/Path13"),   // 11
                };

                var allocated =  DataProvider.Current.StartIndexingActivities(new IndexingActivityFactory(), 10, 60);

                Assert.AreEqual(6, allocated.Length);
                Assert.AreEqual(start[4].Id, allocated[0].Id);
                Assert.AreEqual(start[5].Id, allocated[1].Id);
                Assert.AreEqual(start[8].Id, allocated[2].Id);
                Assert.AreEqual(start[9].Id, allocated[3].Id);
                Assert.AreEqual(start[10].Id, allocated[4].Id);
                Assert.AreEqual(start[11].Id, allocated[5].Id);
            }
            finally
            {
                SenseNet.Configuration.ConnectionStrings.ConnectionString = connectionStringBackup;
            }
        }
        [TestMethod, TestCategory("IR")]
        public void Indexing_Centralized_Sql_Allocate07_MaxRecords()
        {
            var connectionStringBackup = SenseNet.Configuration.ConnectionStrings.ConnectionString;
            SenseNet.Configuration.ConnectionStrings.ConnectionString = _connectionString;
            try
            {
                CleanupIndexingActivitiesTable();
                var start = new IIndexingActivity[15];
                for (int i = 1; i <= start.Length; i++)
                {
                    start[i - 1] = RegisterActivity(IndexingActivityType.AddDocument, IndexingActivityRunningState.Waiting, i, i, $"/Root/Path0{i}");
                }

                var allocated =  DataProvider.Current.StartIndexingActivities(new IndexingActivityFactory(), 10, 60);

                Assert.AreEqual(10, allocated.Length);
                for (var i = 0; i < 10; i++)
                    Assert.AreEqual(start[i].Id, allocated[i].Id);
            }
            finally
            {
                SenseNet.Configuration.ConnectionStrings.ConnectionString = connectionStringBackup;
            }
        }
        [TestMethod, TestCategory("IR")]
        public void Indexing_Centralized_Sql_Allocate08_StateUpdated()
        {
            var connectionStringBackup = SenseNet.Configuration.ConnectionStrings.ConnectionString;
            SenseNet.Configuration.ConnectionStrings.ConnectionString = _connectionString;
            try
            {
                CleanupIndexingActivitiesTable();
                var start = new[]
                {
                    RegisterActivity(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Waiting, 1, 1, "/Root/Path1"),
                    RegisterActivity(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 1, 1, "/Root/Path1"),
                    RegisterActivity(IndexingActivityType.AddDocument,    IndexingActivityRunningState.Waiting, 2, 2, "/Root/Path2"),
                    RegisterActivity(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, 2, 2, "/Root/Path2"),         
                };

                var allocated =  DataProvider.Current.StartIndexingActivities(new IndexingActivityFactory(), 10, 60);

                Assert.AreEqual(2, allocated.Length);
                Assert.AreEqual(start[0].Id, allocated[0].Id);
                Assert.AreEqual(start[2].Id, allocated[1].Id);
                Assert.AreEqual(IndexingActivityRunningState.Running, allocated[0].RunningState);
                Assert.AreEqual(IndexingActivityRunningState.Running, allocated[1].RunningState);
            }
            finally
            {
                SenseNet.Configuration.ConnectionStrings.ConnectionString = connectionStringBackup;
            }
        }

        /* ====================================================================================== */

        private IIndexingActivity RegisterActivity(IndexingActivityType type, IndexingActivityRunningState state, int nodeId, int versionId, string path)
        {
            IndexingActivityBase activity;
            if (type == IndexingActivityType.AddTree || type == IndexingActivityType.RemoveTree)
                activity = CreateTreeActivity(type, path, nodeId, null);
            else
                activity = CreateActivity(type, path, nodeId, versionId, 9999, null);
            activity.RunningState = state;

            DataProvider.Current.RegisterIndexingActivity(activity);

            return activity;
        }
        private IIndexingActivity RegisterActivity(IndexingActivityType type, IndexingActivityRunningState state, DateTime startDate, int nodeId, int versionId, string path)
        {
            IndexingActivityBase activity;
            if (type == IndexingActivityType.AddTree || type == IndexingActivityType.RemoveTree)
                activity = CreateTreeActivity(type, path, nodeId, null);
            else
                activity = CreateActivity(type, path, nodeId, versionId, 9999, null);

            activity.RunningState = state;
            activity.StartDate = startDate;

            DataProvider.Current.RegisterIndexingActivity(activity);

            return activity;
        }

        private static IndexingActivityBase CreateActivity(IndexingActivityType type, string path, int nodeId, int versionId, long versionTimestamp, IndexDocumentData indexDocumentData)
        {
            var populatorAcc = new PrivateType(typeof(DocumentPopulator));
            var result = populatorAcc.InvokeStatic("CreateActivity", type, path, nodeId, versionId, versionTimestamp, null, indexDocumentData);
            return (IndexingActivityBase)result;
        }
        private static IndexingActivityBase CreateTreeActivity(IndexingActivityType type, string path, int nodeId, IndexDocumentData indexDocumentData)
        {
            var populatorAcc = new PrivateType(typeof(DocumentPopulator));
            var result = populatorAcc.InvokeStatic("CreateTreeActivity", type, path, nodeId, indexDocumentData);
            return (IndexingActivityBase)result;
        }
    }
}
