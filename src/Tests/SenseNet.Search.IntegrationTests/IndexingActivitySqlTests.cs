using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Tests;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search.Indexing;
using SenseNet.Search.Indexing.Activities;
using SenseNet.ContentRepository.Storage;
using System.Linq;
using System.Threading;

namespace SenseNet.Search.IntegrationTests
{
    [TestClass]
    public class IndexingActivitySqlTests : TestBase
    {
        private static string _connectionString = @"Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=sn7tests;Data Source=.\SQL2016";

        [ClassInitialize]
        public static void CleanupDatabase(TestContext context)
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

        [TestMethod]
        public void IndexingSql_RegisterAndReload()
        {
            var connectionStringBackup = SenseNet.Configuration.ConnectionStrings.ConnectionString;
            SenseNet.Configuration.ConnectionStrings.ConnectionString = _connectionString;
            try
            {
                var lastActivityIdBefore = DataProvider.Current.GetLastActivityId();

                var timeAtAtStart = DateTime.UtcNow;

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
                    Assert.IsTrue(timeAtAtStart <= unprocessedActivities[i].CreationDate && unprocessedActivities[i].CreationDate <= timeAtEnd);
                    Assert.IsTrue(unprocessedActivities[i].IsUnprocessedActivity);
                    Assert.AreEqual(IndexingActivityState.Waiting, unprocessedActivities[i].ActivityState);
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
                    Assert.IsTrue(timeAtAtStart <= loadedActivities[i].CreationDate && loadedActivities[i].CreationDate <= timeAtEnd);
                    Assert.IsFalse(loadedActivities[i].IsUnprocessedActivity);
                    Assert.AreEqual(IndexingActivityState.Waiting, unprocessedActivities[i].ActivityState);
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
