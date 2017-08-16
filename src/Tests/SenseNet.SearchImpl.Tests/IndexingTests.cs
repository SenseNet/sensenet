using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Lucene29;
using SenseNet.SearchImpl.Tests.Implementations;

namespace SenseNet.SearchImpl.Tests
{
    [TestClass]
    public class IndexingTests : TestBase
    {
        [TestMethod]
        public void Indexing_Create()
        {
            Node node;
            var result = Test(() =>
            {
                // create a test node unter the root.
                node = new SystemFolder(Node.LoadNode(Identifiers.PortalRootId))
                {
                    Name = "Node1",
                    DisplayName = "Node 1",
                    Index = 42
                };
                foreach (var observer in NodeObserver.GetObserverTypes())
                    node.DisableObserver(observer);

                // ACTION
                node.Save();

                // reload the newly created.
                node = Node.Load<SystemFolder>(node.Id);

                // load the pre-converted index document
                var db = DataProvider.Current;
                var indexDocument = db.LoadIndexDocumentByVersionId(node.VersionId);

                // load last indexing activity
                var activityId = db.GetLastActivityId();
                var activity =
                    db.LoadIndexingActivities(activityId, activityId, 1, false, IndexingActivityFactory.Instance)
                    .FirstOrDefault();

                return new Tuple<Node, IndexDocumentData, IIndexingActivity, TestIndex>(node, indexDocument, activity, GetTestIndex());
            });

            node = result.Item1;
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
            var hit5 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.Index, node.Index));

            Assert.IsNotNull(hit1);
            Assert.IsNotNull(hit2);
            Assert.IsNotNull(hit3);
            Assert.IsNotNull(hit4);
            Assert.IsNotNull(hit5);
        }
        [TestMethod]
        public void Indexing_Update()
        {
            Node node;
            var result = Test(() =>
            {
                // create a test node unter the root.
                node = new SystemFolder(Node.LoadNode(Identifiers.PortalRootId))
                {
                    Name = "Node1",
                    DisplayName = "Node 1"
                };
                foreach (var observer in NodeObserver.GetObserverTypes())
                    node.DisableObserver(observer);
                node.Save();
                // reload the newly created.
                node = Node.Load<SystemFolder>(node.Id);

                // ACTION
                node.DisplayName = "Node 2";
                node.Index = 43;
                foreach (var observer in NodeObserver.GetObserverTypes())
                    node.DisableObserver(observer);
                node.Save();

                // reload the updated.
                node = Node.Load<SystemFolder>(node.Id);

                // load the pre-converted index document
                var db = DataProvider.Current;
                var indexDocument = db.LoadIndexDocumentByVersionId(node.VersionId);

                // load last indexing activity
                var activityId = db.GetLastActivityId();
                var activity =
                    db.LoadIndexingActivities(activityId, activityId, 1, false, IndexingActivityFactory.Instance)
                    .FirstOrDefault();

                return new Tuple<Node, IndexDocumentData, IIndexingActivity, TestIndex>(node, indexDocument, activity, GetTestIndex());
            });

            node = result.Item1;
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
            Assert.AreEqual(IndexingActivityType.UpdateDocument, lastActivity.ActivityType);

            var history = IndexingActivityHistory.GetHistory();
            Assert.AreEqual(2, history.RecentLength);
            var item = history.Recent[0];
            Assert.AreEqual(IndexingActivityType.AddDocument.ToString(), item.TypeName);
            Assert.AreEqual(null, item.Error);
            item = history.Recent[1];
            Assert.AreEqual(IndexingActivityType.UpdateDocument.ToString(), item.TypeName);
            Assert.AreEqual(null, item.Error);

            var hit1 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.Name, "node1"));
            var hit2 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.DisplayName, "node 1"));
            var hit3 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.DisplayName, "node 2"));
            var hit4 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.NodeId, node.Id));
            var hit5 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.VersionId, node.VersionId));
            var hit6 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.Index, 42));
            var hit7 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.Index, 43));

            Assert.IsNotNull(hit1);
            Assert.IsNull(hit2);
            Assert.IsNotNull(hit3);
            Assert.IsNotNull(hit4);
            Assert.IsNotNull(hit5);
            Assert.IsNull(hit6);
            Assert.IsNotNull(hit7);
        }
        [TestMethod]
        public void Indexing_Delete()
        {
            Node node;
            var result = Test(() =>
            {
                // create a test node unter the root.
                node = new SystemFolder(Node.LoadNode(Identifiers.PortalRootId))
                {
                    Name = "Node1",
                    DisplayName = "Node 1"
                };
                foreach (var observer in NodeObserver.GetObserverTypes())
                    node.DisableObserver(observer);
                node.Save();
                // reload the newly created.
                node = Node.Load<SystemFolder>(node.Id);

                // ACTION
                foreach (var observer in NodeObserver.GetObserverTypes())
                    node.DisableObserver(observer);
                node.ForceDelete();

                // load last indexing activity
                var db = DataProvider.Current;
                var activityId = db.GetLastActivityId();
                var activity =
                    db.LoadIndexingActivities(activityId, activityId, 1, false, IndexingActivityFactory.Instance)
                    .FirstOrDefault();

                return new Tuple<Node, IIndexingActivity, TestIndex>(node, activity, GetTestIndex());
            });

            node = result.Item1;
            var lastActivity = result.Item2;
            var index = result.Item3;

            // check the activity
            Assert.IsNotNull(lastActivity);
            Assert.AreEqual(IndexingActivityType.RemoveTree, lastActivity.ActivityType);

            var history = IndexingActivityHistory.GetHistory();
            Assert.AreEqual(2, history.RecentLength);
            var item = history.Recent[0];
            Assert.AreEqual(IndexingActivityType.AddDocument.ToString(), item.TypeName);
            Assert.AreEqual(null, item.Error);
            item = history.Recent[1];
            Assert.AreEqual(IndexingActivityType.RemoveTree.ToString(), item.TypeName);
            Assert.AreEqual(null, item.Error);

            var hit1 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.Name, "node1"));
            var hit2 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.DisplayName, "node 1"));
            var hit3 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.NodeId, node.Id));
            var hit4 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.VersionId, node.VersionId));
            var hit5 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.Index, 42));

            Assert.IsNull(hit1);
            Assert.IsNull(hit2);
            Assert.IsNull(hit3);
            Assert.IsNull(hit4);
            Assert.IsNull(hit5);
        }

        /* ============================================================================ */

        private TestIndex GetTestIndex()
        {
            var indexManagerAcc = new PrivateType(typeof(IndexManager));
            var factory = (TestIndexingEngineFactory) indexManagerAcc.GetStaticField("_indexingEngineFactory");
            return factory.Instance.Index;
        }
    }
}
