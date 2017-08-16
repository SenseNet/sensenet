using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Lucene29;
using SenseNet.SearchImpl.Tests.Implementations;

namespace SenseNet.SearchImpl.Tests
{
    [TestClass]
    public class IndexingTests
    {
        //public static void xxx()
        //{
        //    Repository.Start(new RepositoryStartSettings
        //    {
        //        StartLuceneManager = startLuceneManager,
        //        PluginsPath = PluginsPath ?? context.SandboxPath,
        //        IndexPath = null,
        //        RestoreIndex = RestoreIndex,
        //        BackupIndexAtTheEnd = BackupIndexAtTheEnd,
        //        StartWorkflowEngine = StartWorkflowEngine
        //    });
        //}

        [TestMethod]
        public void Indexing_IndexDocumentAndActivityConsistencyAfterCreate()
        {
            var result = InMemoryDataProviderTests.Test(() =>
            {
                // create a test node unter the root.
                var n = new SystemFolder(Node.LoadNode(Identifiers.PortalRootId))
                {
                    Name = "Node1",
                    DisplayName = "Node 1"
                };
                foreach (var observer in NodeObserver.GetObserverTypes())
                    n.DisableObserver(observer);
                n.Save();

                // reload the newly created.
                n = Node.Load<SystemFolder>(n.Id);

                // load the pre-converted index document
                var db = DataProvider.Current;
                var indexDocument = db.LoadIndexDocumentByVersionId(n.VersionId);

                // load last indexing activity
                var activityId = db.GetLastActivityId();
                var activity =
                    db.LoadIndexingActivities(activityId, activityId, 1, false, IndexingActivityFactory.Instance)
                    .FirstOrDefault();

                return new Tuple<Node, IndexDocumentData, IIndexingActivity, TestIndex>(n, indexDocument, activity, GetTestIndex());
            });

            var node = result.Item1;
            var indexDoc = result.Item2;
            var lastActivity = result.Item3;
            var index = result.Item4;

            // check the index document head consistency
            Assert.IsNotNull(indexDoc);
            Assert.AreEqual(indexDoc.Path, node.Path);
            Assert.AreEqual(indexDoc.NodeId, node.Id);
            Assert.AreEqual(indexDoc.NodeTypeId, node.NodeTypeId);
            Assert.AreEqual(indexDoc.ParentId, node.ParentId);
            Assert.AreEqual(indexDoc.VersionId, node.VersionId);

            // check the activity
            Assert.IsNotNull(lastActivity);
            Assert.AreEqual(IndexingActivityType.AddDocument, lastActivity.ActivityType);

            var history = IndexingActivityHistory.GetHistory();
            Assert.AreEqual(1, history.RecentLength);
            var item = history.Recent[0];
            Assert.AreEqual(IndexingActivityType.AddDocument.ToString(), item.TypeName);
            Assert.AreEqual(null, item.Error);

            var hit1 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.Name, "node1"));
            var hit2 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.DisplayName, "node 1"));
            var hit3 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.NodeId, node.Id));
            var hit4 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.VersionId, node.VersionId));

            Assert.IsNotNull(hit1);
            Assert.IsNotNull(hit2);
            Assert.IsNotNull(hit3);
            Assert.IsNotNull(hit4);
        }

        private TestIndex GetTestIndex()
        {
            var indexManagerAcc = new PrivateType(typeof(IndexManager));
            var factory = (TestIndexingEngineFactory) indexManagerAcc.GetStaticField("_indexingEngineFactory");
            return factory.Instance.Index;
        }
    }
}
