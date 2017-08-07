using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Search.Indexing;
using SenseNet.SearchImpl.Tests.Implementations;

namespace SenseNet.SearchImpl.Tests
{
    [TestClass]
    public class IndexingTests
    {
        [TestMethod]
        public void Indexing_IndexDocumentAndActivityConsistencyAfterCreate()
        {
            var result = InMemoryDataProviderTests.Test(() =>
            {
                // create a test node unter the root.
                var n = new TestNode(Node.LoadNode(Identifiers.PortalRootId))
                {
                    Name = "Node1",
                    DisplayName = "Node 1"
                };
                foreach (var observer in NodeObserver.GetObserverTypes())
                    n.DisableObserver(observer);
                n.Save();

                // reload the newly created.
                n = Node.Load<TestNode>(n.Id);

                // load the pre-converted index document
                var db = DataProvider.Current;
                var indexDocument = db.LoadIndexDocumentByVersionId(n.VersionId);

                // load last indexing activity
                var activityId = db.GetLastActivityId();
                var activity =
                    db.LoadIndexingActivities(activityId, activityId, 1, false, IndexingActivityFactory.Instance)
                    .FirstOrDefault();

                return new Tuple<Node, IndexDocumentData, IIndexingActivity>(n, indexDocument, activity);
            });

            var node = result.Item1;
            var indexDoc = result.Item2;
            var lastActivity = result.Item3;

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
        }

    }
}
