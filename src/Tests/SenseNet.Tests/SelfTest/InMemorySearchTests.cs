using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;
using SenseNet.Security.Data;
using SenseNet.Tests.Implementations;

namespace SenseNet.Tests.SelfTest
{
    [TestClass]
    public class InMemorySearchTests : TestBase
    {
        [TestMethod, TestCategory("IR")]
        public void InMemSearch_Indexing_Create()
        {
            Node node;
            Test(() =>
            {
                // create a test node under the root.
                node = new SystemFolder(Node.LoadNode(Identifiers.PortalRootId))
                {
                    Name = "Node1",
                    DisplayName = "Node 1",
                    Index = 42
                };

                // ACTION
                node.Save();

                // reload the newly created.
                node = Node.Load<SystemFolder>(node.Id);

                // load the pre-converted index document and  last indexing activity
                IndexDocumentData indexDoc;
                IIndexingActivity lastActivity;
                if (DataStore.Enabled)
                {
                    indexDoc = DataStore.LoadIndexDocumentByVersionIdAsync(node.VersionId).Result;
                    var activityId = DataStore.GetLastIndexingActivityIdAsync().Result;
                    lastActivity =
                        DataStore.LoadIndexingActivitiesAsync(activityId, activityId, 1, false, IndexingActivityFactory.Instance)
                            .Result.FirstOrDefault();
                }
                else
                {
                    indexDoc = DataProvider.Instance.LoadIndexDocumentByVersionId(node.VersionId); //DB:ok
                    var activityId = DataProvider.Instance.GetLastIndexingActivityId();
                    lastActivity =
                        DataProvider.Instance.LoadIndexingActivities(activityId, activityId, 1, false, IndexingActivityFactory.Instance)
                            .FirstOrDefault();
                }

                var index = GetTestIndex();

                // check the index document head consistency
                Assert.IsNotNull(indexDoc);
                Assert.AreEqual(node.Path, indexDoc.Path);
                Assert.AreEqual(node.Id, indexDoc.NodeId);
                Assert.AreEqual(node.NodeTypeId, indexDoc.NodeTypeId);
                Assert.AreEqual(node.ParentId, indexDoc.ParentId);
                Assert.AreEqual(node.VersionId, indexDoc.VersionId);

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
            });
        }

        [TestMethod, TestCategory("IR")]
        public void InMemSearch_Indexing_Update()
        {
            Node node;
            Test(() =>
            {
                // create a test node under the root.
                node = new SystemFolder(Node.LoadNode(Identifiers.PortalRootId))
                {
                    Name = "Node1",
                    DisplayName = "Node 1",
                    Index = 42
                };

                node.Save();
                // reload the newly created.
                node = Node.Load<SystemFolder>(node.Id);

                // ACTION
                node.DisplayName = "Node 2";
                node.Index = 43;
                node.Save();

                // reload the updated.
                node = Node.Load<SystemFolder>(node.Id);

                //// load the pre-converted index document
                //var db = DataProvider.Instance; //DB:??test??
                //var indexDoc = db.LoadIndexDocumentByVersionId(node.VersionId);

                //// load last indexing activity
                //var activityId = db.GetLastIndexingActivityId();
                //var lastActivity =
                //    db.LoadIndexingActivities(activityId, activityId, 1, false, IndexingActivityFactory.Instance)
                //        .FirstOrDefault();
                // load the pre-converted index document and  last indexing activity
                IndexDocumentData indexDoc;
                IIndexingActivity lastActivity;
                if (DataStore.Enabled)
                {
                    indexDoc = DataStore.LoadIndexDocumentByVersionIdAsync(node.VersionId).Result;
                    var activityId = DataStore.GetLastIndexingActivityIdAsync().Result;
                    lastActivity =
                        DataStore.LoadIndexingActivitiesAsync(activityId, activityId, 1, false, IndexingActivityFactory.Instance)
                            .Result.FirstOrDefault();
                }
                else
                {
                    indexDoc = DataProvider.Instance.LoadIndexDocumentByVersionId(node.VersionId); //DB:ok
                    var activityId = DataProvider.Instance.GetLastIndexingActivityId();
                    lastActivity =
                        DataProvider.Instance.LoadIndexingActivities(activityId, activityId, 1, false, IndexingActivityFactory.Instance)
                            .FirstOrDefault();
                }

                var index = GetTestIndex();

                // check the index document head consistency
                Assert.IsNotNull(indexDoc);
                Assert.AreEqual(node.Path, indexDoc.Path);
                Assert.AreEqual(node.Id, indexDoc.NodeId);
                Assert.AreEqual(node.NodeTypeId, indexDoc.NodeTypeId);
                Assert.AreEqual(node.ParentId, indexDoc.ParentId);
                Assert.AreEqual(node.VersionId, indexDoc.VersionId);

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
            });
        }

        [TestMethod, TestCategory("IR")]
        public void InMemSearch_Indexing_Delete()
        {
            Node node1, node2;

            Test(() =>
            {
                // create node#1 under the root.
                node1 = new SystemFolder(Node.LoadNode(Identifiers.PortalRootId))
                {
                    Name = "Node1",
                    DisplayName = "Node 1",
                    Index = 42
                };

                node1.Save();

                // create node#2 under the root.
                node2 = new SystemFolder(Node.LoadNode(Identifiers.PortalRootId))
                {
                    Name = "Node2",
                    DisplayName = "Node 2",
                    Index = 43
                };

                node2.Save();

                // reload.
                node1 = Node.Load<SystemFolder>(node1.Id);

                // ACTION
                node1.ForceDelete();

                // load last indexing activity
                IIndexingActivity lastActivity;
                if (DataStore.Enabled)
                {
                    var activityId = DataStore.GetLastIndexingActivityIdAsync().Result;
                    lastActivity = DataStore.LoadIndexingActivitiesAsync(activityId, activityId, 1, false, IndexingActivityFactory.Instance)
                        .Result.FirstOrDefault();
                }
                else
                {
                    var db = DataProvider.Instance; //DB:ok
                    var activityId = db.GetLastIndexingActivityId();
                    lastActivity = db.LoadIndexingActivities(activityId, activityId, 1, false, IndexingActivityFactory.Instance)
                            .FirstOrDefault();
                }

                // ASSERT
                var index = GetTestIndex();

                // check the activity
                Assert.IsNotNull(lastActivity);
                Assert.AreEqual(IndexingActivityType.RemoveTree, lastActivity.ActivityType);

                var history = IndexingActivityHistory.GetHistory();
                Assert.AreEqual(3, history.RecentLength);
                var item = history.Recent[0];
                Assert.AreEqual(IndexingActivityType.AddDocument.ToString(), item.TypeName);
                Assert.AreEqual(null, item.Error);
                item = history.Recent[1];
                Assert.AreEqual(IndexingActivityType.AddDocument.ToString(), item.TypeName);
                Assert.AreEqual(null, item.Error);
                item = history.Recent[2];
                Assert.AreEqual(IndexingActivityType.RemoveTree.ToString(), item.TypeName);
                Assert.AreEqual(null, item.Error);

                var hit1 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.Name, "node1"));
                var hit2 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.DisplayName, "node 1"));
                var hit3 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.NodeId, node1.Id));
                var hit4 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.VersionId, node1.VersionId));
                var hit5 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.Index, 42));
                var hit6 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.Name, "node2"));
                var hit7 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.DisplayName, "node 2"));
                var hit8 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.NodeId, node2.Id));
                var hit9 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.VersionId, node2.VersionId));
                var hit10 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.Index, 43));

                Assert.IsNull(hit1);
                Assert.IsNull(hit2);
                Assert.IsNull(hit3);
                Assert.IsNull(hit4);
                Assert.IsNull(hit5);
                Assert.IsNotNull(hit6);
                Assert.IsNotNull(hit7);
                Assert.IsNotNull(hit8);
                Assert.IsNotNull(hit9);
                Assert.IsNotNull(hit10);
            });
        }

        [TestMethod, TestCategory("IR")]
        public void InMemSearch_Indexing_Rename()
        {
            Node node1; // /Root/Node1
            Node node2; // /Root/Node1/Node2
            Node node3; // /Root/Node1/Node2/Node3
            Node node4; // /Root/Node1/Node4
            Node node5; // /Root/Node5
            Node node6; // /Root/Node5/Node6
            Node[] nodes;

            Test(builder => { builder.UseCacheProvider(new EmptyCache()); }, () =>
            {
                // create initial structure.
                var root = Node.LoadNode(Identifiers.PortalRootId);
                node1 = new SystemFolder(root) {Name = "Node1"};  node1.Save();
                node2 = new SystemFolder(node1) {Name = "Node2"}; node2.Save();
                node3 = new SystemFolder(node2) {Name = "Node3"}; node3.Save();
                node4 = new SystemFolder(node1) {Name = "Node4"}; node4.Save();
                node5 = new SystemFolder(root) {Name = "Node5"};  node5.Save();
                node6 = new SystemFolder(node5) {Name = "Node6"}; node6.Save();

                // ACTION
                node1 = Node.LoadNode(node1.Id);
                node1.Name = "Node1Renamed";
                node1.Save();

                // reload the newly created.
                nodes = new[]
                {
                    Node.LoadNode(node1.Id),
                    Node.LoadNode(node2.Id),
                    Node.LoadNode(node3.Id),
                    Node.LoadNode(node4.Id),
                    Node.LoadNode(node5.Id),
                    Node.LoadNode(node6.Id),
                };

                //return new Tuple<Node[], IndexingActivityHistory, InMemoryIndex>(nodes,
                //    IndexingActivityHistory.GetHistory(), GetTestIndex());
                var history = IndexingActivityHistory.GetHistory();
                var index = GetTestIndex();


                // check paths
                Assert.AreEqual("/Root/Node1Renamed", nodes[0].Path);
                Assert.AreEqual("/Root/Node1Renamed/Node2", nodes[1].Path);
                Assert.AreEqual("/Root/Node1Renamed/Node2/Node3", nodes[2].Path);
                Assert.AreEqual("/Root/Node1Renamed/Node4", nodes[3].Path);
                Assert.AreEqual("/Root/Node5", nodes[4].Path);
                Assert.AreEqual("/Root/Node5/Node6", nodes[5].Path);

                // check indexing activities and errors
                Assert.AreEqual(8, history.RecentLength);
                Assert.AreEqual(IndexingActivityType.AddDocument.ToString(), history.Recent[0].TypeName);
                Assert.AreEqual(IndexingActivityType.AddDocument.ToString(), history.Recent[1].TypeName);
                Assert.AreEqual(IndexingActivityType.AddDocument.ToString(), history.Recent[2].TypeName);
                Assert.AreEqual(IndexingActivityType.AddDocument.ToString(), history.Recent[3].TypeName);
                Assert.AreEqual(IndexingActivityType.AddDocument.ToString(), history.Recent[4].TypeName);
                Assert.AreEqual(IndexingActivityType.AddDocument.ToString(), history.Recent[5].TypeName);
                Assert.AreEqual(IndexingActivityType.RemoveTree.ToString(), history.Recent[6].TypeName);
                Assert.AreEqual(IndexingActivityType.AddTree.ToString(), history.Recent[7].TypeName);
                Assert.AreEqual(null, history.Recent[0].Error);
                Assert.AreEqual(null, history.Recent[1].Error);
                Assert.AreEqual(null, history.Recent[2].Error);
                Assert.AreEqual(null, history.Recent[3].Error);
                Assert.AreEqual(null, history.Recent[4].Error);
                Assert.AreEqual(null, history.Recent[5].Error);
                Assert.AreEqual(null, history.Recent[6].Error);
                Assert.AreEqual(null, history.Recent[7].Error);

                // check name terms in index
                var hit1 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.Name, "node1"));
                var hit2 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.Name, "node1renamed"));
                var hit3 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.Name, "node2"));
                var hit4 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.Name, "node3"));
                var hit5 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.Name, "node4"));
                var hit6 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.Name, "node5"));
                var hit7 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.Name, "node6"));
                Assert.IsNull(hit1);
                Assert.IsNotNull(hit2);
                Assert.IsNotNull(hit3);
                Assert.IsNotNull(hit4);
                Assert.IsNotNull(hit5);
                Assert.IsNotNull(hit6);
                Assert.IsNotNull(hit7);

                // check old subtree existence
                var node1Tree = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.InTree, "/root/node1"));
                Assert.IsNull(node1Tree);

                // check renamed tree
                var renamedTree =
                    index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.InTree, "/root/node1renamed")).ToArray();
                Assert.IsNotNull(renamedTree);
                Assert.AreEqual(4, renamedTree.Length);
                // check node ids in renamed tree
                var ids = string.Join(", ", renamedTree
                    .Select(x => x.Item2.First(y => y.Name == IndexFieldName.NodeId).IntegerValue)
                    .OrderBy(z => z).Select(z => z.ToString()).ToArray());
                var expectedIds = string.Join(", ", new[] {nodes[0].Id, nodes[1].Id, nodes[2].Id, nodes[3].Id}
                    .OrderBy(z => z).Select(z => z.ToString()).ToArray());
                Assert.AreEqual(expectedIds, ids);
                // check paths ids in renamed tree
                var paths = string.Join(", ", renamedTree
                    .Select(x => x.Item2.First(y => y.Name == IndexFieldName.Path).StringValue)
                    .OrderBy(z => z).ToArray());
                var expectedPaths = string.Join(", ",
                    new[] {nodes[0].Path, nodes[1].Path, nodes[2].Path, nodes[3].Path}
                        .OrderBy(z => z).ToArray());
                Assert.AreEqual(expectedPaths.ToLowerInvariant(), paths);

                // check untouched tree
                var node5Tree = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.InTree, "/root/node5"))
                    .ToArray();
                Assert.IsNotNull(node5Tree);
                Assert.AreEqual(2, node5Tree.Length);
                // check node ids in untouched tree
                ids = string.Join(", ", node5Tree
                    .Select(x => x.Item2.First(y => y.Name == IndexFieldName.NodeId).IntegerValue)
                    .OrderBy(z => z).Select(z => z.ToString()).ToArray());
                expectedIds = string.Join(", ", new[] {nodes[4].Id, nodes[5].Id}
                    .OrderBy(z => z).Select(z => z.ToString()).ToArray());
                Assert.AreEqual(expectedIds, ids);
                // check paths ids in untouched tree
                paths = string.Join(", ", node5Tree
                    .Select(x => x.Item2.First(y => y.Name == IndexFieldName.Path).StringValue)
                    .OrderBy(z => z).ToArray());
                expectedPaths = string.Join(", ", new[] {nodes[4].Path, nodes[5].Path}
                    .OrderBy(z => z).ToArray());
                Assert.AreEqual(expectedPaths.ToLowerInvariant(), paths);
            });
        }


        [TestMethod, TestCategory("IR")]
        public void InMemSearch_Indexing_AddTextEctract()
        {
            Node node;
            var additionalText = "additionaltext";

            Test(() =>
            {
                // create a test node under the root.
                node = new SystemFolder(Node.LoadNode(Identifiers.PortalRootId))
                {
                    Name = "Node1",
                    DisplayName = "Node 1",
                    Index = 42
                };

                node.Save();

                // ACTION
                IndexingTools.AddTextExtract(node.VersionId, additionalText);

                node = Node.Load<SystemFolder>(node.Id);

                // load the pre-converted index document
                var indexDoc = DataStore.Enabled
                    ? DataStore.LoadIndexDocumentByVersionIdAsync(node.VersionId).Result
                    : DataProvider.Instance.LoadIndexDocumentByVersionId(node.VersionId);

                // check the index document head consistency
                Assert.IsNotNull(indexDoc);
                Assert.AreEqual(node.VersionId, indexDoc.VersionId);
                Assert.IsTrue(indexDoc.IndexDocument.GetStringValue(IndexFieldName.AllText).Contains(additionalText));

                // check executed activities
                var history = IndexingActivityHistory.GetHistory();
                Assert.AreEqual(2, history.RecentLength);
                var item = history.Recent[0];
                Assert.AreEqual(IndexingActivityType.AddDocument.ToString(), item.TypeName);
                Assert.AreEqual(null, item.Error);
                item = history.Recent[1];
                Assert.AreEqual(IndexingActivityType.Rebuild.ToString(), item.TypeName);
                Assert.AreEqual(null, item.Error);

                // check index
                var index = GetTestIndex();

                var hit1 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.Name, "node1"));
                var hit2 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.VersionId, node.VersionId));
                var hit3 = index.GetStoredFieldsByTerm(new SnTerm(IndexFieldName.AllText, additionalText));

                Assert.IsNotNull(hit1);
                Assert.IsNotNull(hit2);
                Assert.IsNotNull(hit3);
            });
        }

        [TestMethod, TestCategory("IR")]
        public void InMemSearch_Indexing_ClearAndPopulateAll()
        {
            var sb = new StringBuilder();
            IIndexingActivity[] activities;
            Test(() =>
            {
                SaveInitialIndexDocuments();

                // ACTION
                using (var console = new StringWriter(sb))
                    SearchManager.GetIndexPopulator().ClearAndPopulateAll(console);

                // load last indexing activity
                int nodeCount, versionCount;
                if (DataStore.Enabled)
                {
                    var activityId = DataStore.GetLastIndexingActivityIdAsync().Result;
                    activities = DataStore.LoadIndexingActivitiesAsync(1, activityId, 10000, false, IndexingActivityFactory.Instance).Result;
                    nodeCount = DataStore.GetVersionCountAsync().Result;
                    versionCount = DataStore.GetVersionCountAsync().Result;
                }
                else
                {
                    var db = DataProvider.Instance; //DB:ok
                    var activityId = db.GetLastIndexingActivityId();
                    activities = db.LoadIndexingActivities(1, activityId, 10000, false, IndexingActivityFactory.Instance);
                    nodeCount = DataProvider.GetNodeCount(); //DB:??test??
                    versionCount = DataProvider.GetVersionCount(); //DB:??test??
                }


                var index = GetTestIndex();
                var nodeCountInDb = nodeCount;
                var versionCountInDb = versionCount;

                // check activities
                Assert.IsNotNull(activities);
                Assert.AreEqual(0, activities.Length);

                var historyItems = IndexingActivityHistory.GetHistory().Recent;
                Assert.AreEqual(0, historyItems.Length);

                var nodeCountInIndex = index.GetTermCount(IndexFieldName.NodeId);
                var versionCountInIndex = index.GetTermCount(IndexFieldName.VersionId);

                Assert.AreEqual(nodeCountInDb, nodeCountInIndex);
                Assert.AreEqual(versionCountInDb, versionCountInIndex);
            });
        }

        /* ============================================================================ */

        [TestMethod, TestCategory("IR")]
        public void InMemSearch_Query_1Term1Hit()
        {
            Node node;
            Test(() =>
            {
                // create a test node under the root.
                node = new SystemFolder(Node.LoadNode(Identifiers.PortalRootId))
                {
                    Name = "Node1",
                    DisplayName = "Node 1",
                    Index = 42
                };

                node.Save();

                // ACTION
                var qresult = ContentQuery.Query(SafeQueries.Name, QuerySettings.AdminSettings, "Node1");

                // ASSERT
                var nodeId = node.Id;
                var nodeIds = qresult.Identifiers.ToArray();
                var nodes = qresult.Nodes.ToArray();

                Assert.IsTrue(nodeId > 0);

                Assert.AreEqual(1, nodeIds.Length);
                Assert.AreEqual(nodeId, nodeIds[0]);

                Assert.AreEqual(1, nodes.Length);
                Assert.AreEqual(nodeId, nodes[0].Id);
            });
        }

        [TestMethod, TestCategory("IR")]
        public void InMemSearch_Query_1TermMoreHit1Order()
        {
            var createNode = new Func<Node, string, int, Node>((parent, name, index) =>
            {
                var node = new SystemFolder(parent) {Name = name, Index = index};
                node.Save();
                return node;
            });

            Test(() =>
            {
                // create a test structure:
                //    Root
                //      F1
                //        Node1 (Index=42)
                //      F2
                //        Node1 (Index=41)
                //      F3
                //        Node1 (Index=43)
                var root = Node.LoadNode(Identifiers.PortalRootId);

                var f1 = createNode(root, "F1", 0);
                var node1 = createNode(f1, "Node1", 42);
                var f2 = createNode(root, "F2", 0);
                var node2 = createNode(f2, "Node1", 41);
                var f3 = createNode(root, "F3", 0);
                var node3 = createNode(f3, "Node1", 43);

                // ACTION
                var settings = QuerySettings.AdminSettings;
                settings.Sort = new[] { new SortInfo(IndexFieldName.Index) };
                var qresult = ContentQuery.Query(SafeQueries.Name, settings, "Node1");

                // ASSERT
                var nodeIds = qresult.Identifiers.ToArray();
                var nodes = qresult.Nodes.ToArray();

                var expectedNodeIds = $"{node2.Id}, {node1.Id}, {node3.Id}";
                var actualNodeIds = string.Join(", ", nodeIds);
                Assert.AreEqual(expectedNodeIds, actualNodeIds);

                var expectedPaths = "/Root/F2/Node1, /Root/F1/Node1, /Root/F3/Node1";
                var actualPaths = string.Join(", ", nodes.Select(n => n.Path));
                Assert.AreEqual(expectedPaths, actualPaths);
            });
        }

        [TestMethod, TestCategory("IR")]
        public void InMemSearch_Query_1TermMoreHit2Order()
        {
            var createNode = new Func<Node, string, string, int, Node>((parent, name, displayName, index) =>
            {
                var node = new SystemFolder(parent) { Name = name, DisplayName = displayName, Index = index };
                node.Save();
                return node;
            });

            Test(() =>
            {
                // create a test structure:
                var root = Node.LoadNode(Identifiers.PortalRootId);
                var f1 = createNode(root, "F1", "F1", 0);
                createNode(f1, "N1", "D2", 2);
                createNode(f1, "N2", "D3", 2);
                createNode(f1, "N3", "D2", 3);
                createNode(f1, "N4", "D3", 3);
                createNode(f1, "N5", "D1", 2);
                createNode(f1, "N6", "D3", 1);
                createNode(f1, "N7", "D2", 1);
                createNode(f1, "N8", "D1", 1);

                // ACTION
                var settings = QuerySettings.AdminSettings;
                settings.Sort = new[] {new SortInfo(IndexFieldName.DisplayName), new SortInfo(IndexFieldName.Index, true) };
                var qresult = ContentQuery.Query(SafeQueries.OneTerm, settings, "ParentId", f1.Id.ToString());

                // ASSERT
                var nodes = qresult.Nodes.ToArray();

                var expectedNames = "N5, N8, N3, N1, N7, N4, N2, N6";
                var actualNames = string.Join(", ", nodes.Select(n => n.Name));
                Assert.AreEqual(expectedNames, actualNames);
            });
        }

        [TestMethod, TestCategory("IR")]
        public void InMemSearch_Query_PrefixOrSuffix()
        {
            var createNode = new Func<Node, string, Node>((parent, name) =>
            {
                var node = new SystemFolder(parent) { Name = name };
                node.Save();
                return node;
            });

            Test(() =>
            {
                // create a test structure:
                var root = Node.LoadNode(Identifiers.PortalRootId);
                var f1 = createNode(root, "F1");
                createNode(f1, "Aa11");
                createNode(f1, "Bb11");
                createNode(f1, "Cc11");
                createNode(f1, "Aa22");
                createNode(f1, "Bb22");
                createNode(f1, "Cc22");
                createNode(f1, "Aa33");
                createNode(f1, "Bb33");
                createNode(f1, "Cc33");

                // ACTION
                var settings = QuerySettings.AdminSettings;
                settings.Sort = new[] { new SortInfo(IndexFieldName.Name) };
                var qresult1 = ContentQuery.Query(SafeQueries.OneTerm, settings, "Name", "Bb*");
                var qresult2 = ContentQuery.Query(SafeQueries.OneTerm, settings, "Name", "*33");

                var nodes1 = qresult1.Nodes.ToArray();
                var nodes2 = qresult2.Nodes.ToArray();
                Assert.AreEqual("Bb11, Bb22, Bb33", string.Join(", ", nodes1.Select(n => n.Name)));
                Assert.AreEqual("Aa33, Bb33, Cc33", string.Join(", ", nodes2.Select(n => n.Name)));
            });
        }

        [TestMethod, TestCategory("IR")]
        public void InMemSearch_Query_PrefixAndSuffixOrMiddle()
        {
            var createNode = new Func<Node, string, Node>((parent, name) =>
            {
                var node = new SystemFolder(parent) { Name = name };
                node.Save();
                return node;
            });

            Test(() =>
            {
                // create a test structure:
                var root = Node.LoadNode(Identifiers.PortalRootId);
                var f1 = createNode(root, "F1");
                createNode(f1, "AAxx11");
                createNode(f1, "AAxx22");
                createNode(f1, "AAyy11");
                createNode(f1, "AAyy22");
                createNode(f1, "BBxx11");
                createNode(f1, "BBxx22");
                createNode(f1, "BByy11");
                createNode(f1, "BByy22");

                // ACTION
                var settings = QuerySettings.AdminSettings;
                settings.Sort = new[] { new SortInfo(IndexFieldName.Name) };
                var qresult1 = ContentQuery.Query(SafeQueries.OneTerm, settings, "Name", "AA*22");
                var qresult2 = ContentQuery.Query(SafeQueries.OneTerm, settings, "Name", "*yy*");

                var nodes1 = qresult1.Nodes.ToArray();
                var nodes2 = qresult2.Nodes.ToArray();

                Assert.AreEqual("AAxx22, AAyy22", string.Join(", ", nodes1.Select(n => n.Name)));
                Assert.AreEqual("AAyy11, AAyy22, BByy11, BByy22", string.Join(", ", nodes2.Select(n => n.Name)));
            });
        }


        [TestMethod, TestCategory("IR")]
        public void InMemSearch_Query_Range()
        {
            var createNode = new Func<Node, string, Node>((parent, name) =>
            {
                var node = new SystemFolder(parent) { Name = name };
                node.Save();
                return node;
            });
            var executeQuery = new Func<string, string>((query) =>
            {
                return string.Join(", ", CreateSafeContentQuery(query).Execute().Nodes
                    .Where(n => n.Name.StartsWith("Nn")).Select(n => n.Name).ToArray());
            });

            Test(() =>
            {
                // create a test structure:
                var root = Node.LoadNode(Identifiers.PortalRootId);
                createNode(root, "Nn0");
                createNode(root, "Nn1");
                createNode(root, "Nn2");
                createNode(root, "Nn3");
                createNode(root, "Nn4");
                createNode(root, "Nn5");
                createNode(root, "Nn6");
                createNode(root, "Nn7");
                createNode(root, "Nn8");
                createNode(root, "Nn9");

                // ACTION
                var settings = QuerySettings.AdminSettings;
                settings.Sort = new[] { new SortInfo(IndexFieldName.Name) };

                Assert.AreEqual("Nn5, Nn6, Nn7, Nn8, Nn9",      executeQuery("+Name:>Nn4  +InFolder:/Root"));
                Assert.AreEqual("Nn4, Nn5, Nn6, Nn7, Nn8, Nn9", executeQuery("+Name:>=Nn4 +InFolder:/Root"));
                Assert.AreEqual("Nn0, Nn1, Nn2, Nn3",           executeQuery("+Name:<Nn4  +InFolder:/Root"));
                Assert.AreEqual("Nn0, Nn1, Nn2, Nn3, Nn4",      executeQuery("+Name:<=Nn4 +InFolder:/Root"));
                Assert.AreEqual("Nn2, Nn3, Nn4, Nn5, Nn6, Nn7", executeQuery("+Name:[Nn2 TO Nn7] +InFolder:/Root"));
                Assert.AreEqual("Nn2, Nn3, Nn4, Nn5, Nn6",      executeQuery("+Name:[Nn2 TO Nn7} +InFolder:/Root"));
                Assert.AreEqual("Nn3, Nn4, Nn5, Nn6, Nn7",      executeQuery("+Name:{Nn2 TO Nn7] +InFolder:/Root"));
                Assert.AreEqual("Nn3, Nn4, Nn5, Nn6",           executeQuery("+Name:{Nn2 TO Nn7} +InFolder:/Root"));
            });
        }
        [TestMethod, TestCategory("IR")]
        public void InMemSearch_Query_Range_Int()
        {
            var createNode = new Func<Node, int, Node>((parent, index) =>
            {
                var node = new SystemFolder(parent) { Name = $"Nn{index}", Index = index };
                node.Save();
                return node;
            });
            var executeQuery = new Func<string, string>((query) =>
            {
                return string.Join(", ", CreateSafeContentQuery(query).Execute().Nodes
                    .Where(n => n.Name.StartsWith("Nn")).Select(n => n.Index).OrderBy(i => i).ToArray());
            });

            Test(() =>
            {
                // create a test structure:
                var root = Node.LoadNode(Identifiers.PortalRootId);
                for (var i = 0; i < 100; i++)
                    createNode(root, i);

                // ACTION
                var settings = QuerySettings.AdminSettings;
                settings.Sort = new[] { new SortInfo(IndexFieldName.Name) };

                Assert.AreEqual("5, 6, 7, 8, 9", executeQuery("+Index:>4 +Index:<10"));
                Assert.AreEqual("4, 5, 6, 7, 8, 9", executeQuery("+Index:>=4 +Index:<10"));
                Assert.AreEqual("0, 1, 2, 3", executeQuery("+Index:<4  +InFolder:/Root"));
                Assert.AreEqual("0, 1, 2, 3, 4", executeQuery("+Index:<=4 +InFolder:/Root"));
                Assert.AreEqual("2, 3, 4, 5, 6, 7", executeQuery("+Index:[2 TO 7] +InFolder:/Root"));
                Assert.AreEqual("2, 3, 4, 5, 6", executeQuery("+Index:[2 TO 7} +InFolder:/Root"));
                Assert.AreEqual("3, 4, 5, 6, 7", executeQuery("+Index:{2 TO 7] +InFolder:/Root"));
                Assert.AreEqual("3, 4, 5, 6", executeQuery("+Index:{2 TO 7} +InFolder:/Root"));
            });
        }

        [TestMethod]
        public void InMemSearch_Converter_IntToString()
        {
            var input = new[] { int.MinValue, int.MinValue + 1, -124, -123, -12, -2, -1, 0, 1, 2, 12, 123, 124, int.MaxValue - 1, int.MaxValue };
            var output = input.Select(InMemoryIndex.IntToString).ToArray();

            for (var i = 1; i < output.Length; i++)
                Assert.IsTrue(string.CompareOrdinal(output[i - 1], output[i]) < 0, $"Result of String.CompareOrdinal({output[i - 1]}, {output[i]}) is {string.CompareOrdinal(output[i - 1], output[i])}");
        }

        [TestMethod]
        public void InMemSearch_Converter_LongToString()
        {
            var input = new[] { long.MinValue, long.MinValue + 1L, -124L, -123L, -12L, -2L, -1L, 0L, 1L, 2L, 12L, 123L, 124L, long.MaxValue - 1L, long.MaxValue };
            var output = input.Select(InMemoryIndex.LongToString).ToArray();

            for (var i = 1; i < output.Length; i++)
                Assert.IsTrue(string.CompareOrdinal(output[i - 1], output[i]) < 0, $"Result of String.CompareOrdinal({output[i - 1]}, {output[i]}) is {string.CompareOrdinal(output[i - 1], output[i])}");
        }

        [TestMethod, TestCategory("IR")]
        public void InMemSearch_Query_2TermsBool()
        {
            var createNode = new Func<Node, string, int, Node>((parent, name, index) =>
            {
                var node = new SystemFolder(parent) { Name = name, Index = index };
                node.Save();
                return node;
            });

            Test(() =>
            {
                // create a test structure:
                var root = Node.LoadNode(Identifiers.PortalRootId);
                createNode(root, "Xx0", 0);
                createNode(root, "Xx1", 111);
                createNode(root, "Xx2", 222);
                createNode(root, "Xx3", 333);
                createNode(root, "Yy0", 0);
                createNode(root, "Yy1", 111);
                createNode(root, "Yy2", 222);
                createNode(root, "Yy3", 333);

                // ACTION
                var settings = QuerySettings.AdminSettings;
                settings.Sort = new[] { new SortInfo(IndexFieldName.Name) };
                var result = new[]
                {
                    string.Join(", ", ContentQuery.Query(SafeQueries.TwoTermsShouldShould, settings, "Name", "Xx*", "Index", 111).Nodes.Select(n => n.Name).ToArray()),
                    string.Join(", ", ContentQuery.Query(SafeQueries.TwoTermsMustMust, settings, "Name", "Xx*", "Index", 111).Nodes.Select(n => n.Name).ToArray()),
                    string.Join(", ", ContentQuery.Query(SafeQueries.TwoTermsMustNot, settings, "Name", "Xx*", "Index", 111).Nodes.Select(n => n.Name).ToArray()),
                };

                Assert.AreEqual("Xx0, Xx1, Xx2, Xx3, Yy1", result[0]);
                Assert.AreEqual("Xx1", result[1]);
                Assert.AreEqual("Xx0, Xx2, Xx3", result[2]);
            });

        }

        [TestMethod, TestCategory("IR")]
        public void InMemSearch_Query_MultiLevelBool()
        {
            var createNode = new Func<Node, string, int, Node>((parent, name, index) =>
            {
                var node = new SystemFolder(parent) { Name = name, Index = index };
                node.Save();
                return node;
            });

            Test(() =>
            {
                // create a test structure:
                var root = Node.LoadNode(Identifiers.PortalRootId);
                createNode(root, "Aa0", 0);
                createNode(root, "Aa1", 11);
                createNode(root, "Aa2", 22);
                createNode(root, "Aa3", 33);
                createNode(root, "Bb0", 0);
                createNode(root, "Bb1", 11);
                createNode(root, "Bb2", 22);
                createNode(root, "Bb3", 33);

                // ACTION
                var settings = QuerySettings.AdminSettings;
                settings.Sort = new[] { new SortInfo(IndexFieldName.Name) };
                var result = new[]
                {
                    //  (+Name:A* +Index:1) (+Name:B* +Index:2) --> A1, B2
                    string.Join(", ", ContentQuery.Query(SafeQueries.MultiLevelBool1, settings, "Name", "Aa*", "Index", 11, "Name", "Bb*", "Index", 22).Nodes.Select(n => n.Name).ToArray()),
                    //  +(Name:A* Index:1) +(Name:B* Index:2) --> +(A0, A1, A2, A3, B1) +(B0, B1, B2, B3, A2) --> A2, B1
                    string.Join(", ", ContentQuery.Query(SafeQueries.MultiLevelBool2, settings, "Name", "Aa*", "Index", 11, "Name", "Bb*", "Index", 22).Nodes.Select(n => n.Name).ToArray()),
                };

                Assert.AreEqual("Aa1, Bb2", result[0]);
                Assert.AreEqual("Aa2, Bb1", result[1]);
            });
        }


        [TestMethod, TestCategory("IR")]
        public void InMemSearch_Query_Recursive_One()
        {
            var mock = new Dictionary<string, string[]>
            {
                {"Name:'MyDocument.doc' .SELECT:OwnerId", new [] {"1", "3", "7"}}
            };
            var log = new List<string>();
            QueryResult result = null;

            Test(builder => { builder.UseSearchEngine(new SearchEngineForNestedQueryTests(mock, log)); }, () =>
            {
                var qtext = "Id:{{Name:'MyDocument.doc' .SELECT:OwnerId}}";
                var cquery = ContentQuery.CreateQuery(qtext, QuerySettings.AdminSettings);
                var cqueryAcc = new PrivateObject(cquery);
                cqueryAcc.SetFieldOrProperty("IsSafe", true);
                result = cquery.Execute();
            });

            Assert.AreEqual(42, result.Identifiers.First());
            Assert.AreEqual(42, result.Count);

            Assert.AreEqual(2, log.Count);
            Assert.AreEqual("Name:'MyDocument.doc' .SELECT:OwnerId", log[0]);
            Assert.AreEqual("Id:(1 3 7)", log[1]);
        }

        [TestMethod, TestCategory("IR")]
        public void InMemSearch_Query_Recursive_Three()
        {
            var qtext = "+F4:{{F2:{{F1:v1 .SELECT:P1}} F3:v4 .SELECT:P2}} +F5:{{F6:v6 .SELECT:P6}}";
            var mock = new Dictionary<string, string[]>
            {
                {"F1:v1 .SELECT:P1", new [] {"v1a", "v1b", "v1c"}},
                {"F2:(v1a v1b v1c) F3:v4 .SELECT:P2", new [] {"v2a", "v2b", "v2c"}},
                {"F6:v6 .SELECT:P6", new [] {"v3a", "v3b", "v3c"}}
            };
            var indexingInfo = new Dictionary<string, IPerFieldIndexingInfo>
            {
                //{"_Text", new TestPerfieldIndexingInfoString()},
                {"F1", new TestPerfieldIndexingInfoString()},
                {"F2", new TestPerfieldIndexingInfoString()},
                {"F3", new TestPerfieldIndexingInfoString()},
                {"F4", new TestPerfieldIndexingInfoString()},
                {"F5", new TestPerfieldIndexingInfoString()},
                {"F6", new TestPerfieldIndexingInfoString()},
            };

            var log = new List<string>();
            QueryResult result = null;

            Test(builder => { builder.UseSearchEngine(new SearchEngineForNestedQueryTests(mock, log)); }, () =>
            {
                using (Tools.Swindle(typeof(SearchManager), "_searchEngineSupport", new TestSearchEngineSupport(indexingInfo)))
                {
                    var cquery = ContentQuery.CreateQuery(qtext, QuerySettings.AdminSettings);
                    var cqueryAcc = new PrivateObject(cquery);
                    cqueryAcc.SetFieldOrProperty("IsSafe", true);
                    result = cquery.Execute();
                }
            });

            Assert.AreEqual(42, result.Identifiers.First());
            Assert.AreEqual(42, result.Count);

            Assert.AreEqual(4, log.Count);
            Assert.AreEqual("F1:v1 .SELECT:P1", log[0]);
            Assert.AreEqual("F2:(v1a v1b v1c) F3:v4 .SELECT:P2", log[1]);
            Assert.AreEqual("F6:v6 .SELECT:P6", log[2]);
            Assert.AreEqual("+F4:(v2a v2b v2c) +F5:(v3a v3b v3c)", log[3]);
        }

        [TestMethod, TestCategory("IR")]
        public void InMemSearch_Query_Recursive_Resolve()
        {
            var qtext = "+F4:{{F2:{{F1:v1 .SELECT:P1}} F3:v4 .SELECT:P2}} +F5:{{F6:v6 .SELECT:P6}}";
            var expected = "+F4:(v2a v2b v2c) +F5:(v3a v3b v3c)";

            var mock = new Dictionary<string, string[]>
            {
                {"F1:v1 .SELECT:P1", new [] {"v1a", "v1b", "v1c"}},
                {"F2:(v1a v1b v1c) F3:v4 .SELECT:P2", new [] {"v2a", "v2b", "v2c"}},
                {"F6:v6 .SELECT:P6", new [] {"v3a", "v3b", "v3c"}}
            };
            var indexingInfo = new Dictionary<string, IPerFieldIndexingInfo>
            {
                //{"_Text", new TestPerfieldIndexingInfoString()},
                {"F1", new TestPerfieldIndexingInfoString()},
                {"F2", new TestPerfieldIndexingInfoString()},
                {"F3", new TestPerfieldIndexingInfoString()},
                {"F4", new TestPerfieldIndexingInfoString()},
                {"F5", new TestPerfieldIndexingInfoString()},
                {"F6", new TestPerfieldIndexingInfoString()},
            };

            var log = new List<string>();
            string resolved = null;
            Test(builder => { builder.UseSearchEngine(new SearchEngineForNestedQueryTests(mock, log)); }, () =>
            {
                using (Tools.Swindle(typeof(SearchManager), "_searchEngineSupport", new TestSearchEngineSupport(indexingInfo)))
                    resolved = ContentQuery.ResolveInnerQueries(qtext, QuerySettings.AdminSettings);
            });

            Assert.AreEqual(expected, resolved);

            Assert.AreEqual(3, log.Count);
            Assert.AreEqual("F1:v1 .SELECT:P1", log[0]);
            Assert.AreEqual("F2:(v1a v1b v1c) F3:v4 .SELECT:P2", log[1]);
            Assert.AreEqual("F6:v6 .SELECT:P6", log[2]);
        }

        /* ============================================================================ */

        [TestMethod, TestCategory("IR")]
        public void InMemSearch_ActivityStatus_WithoutRepository()
        {
            var newStatus = new IndexingActivityStatus
            {
                LastActivityId = 33,
                Gaps = new[] { 5, 6, 7 }
            };

            var searchEngine = new InMemorySearchEngine(GetInitialIndex());
            var originalStatus = searchEngine.IndexingEngine.ReadActivityStatusFromIndex();

            searchEngine.IndexingEngine.WriteActivityStatusToIndex(newStatus);

            var updatedStatus = searchEngine.IndexingEngine.ReadActivityStatusFromIndex();
            var resultStatus = new IndexingActivityStatus()
            {
                LastActivityId = updatedStatus.LastActivityId,
                Gaps = updatedStatus.Gaps
            };

            Assert.AreEqual(originalStatus.LastActivityId, 0);
            Assert.AreEqual(originalStatus.Gaps.Length, 0);
            Assert.AreEqual(newStatus.ToString(), resultStatus.ToString());
        }

        [TestMethod, TestCategory("IR")]
        public void InMemSearch_ActivityStatus_WithRepository()
        {
            var newStatus = new IndexingActivityStatus
            {
                LastActivityId = 33,
                Gaps = new[] { 5, 6, 7 }
            };

            Test(() =>
            {
                var searchEngine = SearchManager.SearchEngine;
                var originalStatus = searchEngine.IndexingEngine.ReadActivityStatusFromIndex();
                searchEngine.IndexingEngine.WriteActivityStatusToIndex(newStatus);

                var updatedStatus = searchEngine.IndexingEngine.ReadActivityStatusFromIndex();

                Assert.AreEqual(0, originalStatus.LastActivityId);
                Assert.AreEqual(0, originalStatus.Gaps.Length);
                Assert.AreEqual(newStatus.ToString(), updatedStatus.ToString());
            });

        }

        /* ============================================================================ */

        private InMemoryIndex GetTestIndex()
        {
            return ((InMemorySearchEngine) SearchManager.SearchEngine).Index;
        }

        private class SearchEngineForNestedQueryTests : ISearchEngine
        {
            private class QueryEngineForNestedQueryTests : IQueryEngine
            {
                private readonly Dictionary<string, string[]> _mockResultsPerQueries;
                private readonly List<string> _log;
                public QueryEngineForNestedQueryTests(Dictionary<string, string[]> mockResultsPerQueries, List<string> log)
                {
                    _mockResultsPerQueries = mockResultsPerQueries;
                    _log = log;
                }

                public QueryResult<int> ExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context)
                {
                    _log.Add(query.Querytext);
                    return new QueryResult<int>(new [] {42}, 42);
                }
                public QueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter filter, IQueryContext context)
                {
                    var strings = _mockResultsPerQueries[query.Querytext];
                    _log.Add(query.Querytext);
                    return new QueryResult<string>(strings, strings.Length);
                }
            }

            private class IndexingEngineForNestedQueryTests : IIndexingEngine
            {
                public bool Running { get; private set; }

                public bool IndexIsCentralized => false;

                public void Start(TextWriter consoleOut)
                {
                    Running = true;
                }
                public void ShutDown()
                {
                    Running = false;
                }
                public void ClearIndex()
                {
                    throw new NotImplementedException();
                }
                public IndexingActivityStatus ReadActivityStatusFromIndex()
                {
                    return IndexingActivityStatus.Startup;
                }
                public void WriteActivityStatusToIndex(IndexingActivityStatus state)
                {
                    throw new NotImplementedException();
                }
                public void WriteIndex(IEnumerable<SnTerm> deletions, IEnumerable<DocumentUpdate> updates, IEnumerable<IndexDocument> additions)
                {
                    throw new NotImplementedException();
                }
            }

            private readonly Dictionary<string, string[]> _mockResultsPerQueries;
            private readonly List<string> _log;
            // ReSharper disable once NotAccessedField.Local
            private IDictionary<string, IPerFieldIndexingInfo> _perFieldIndexingInfos;

            public SearchEngineForNestedQueryTests(Dictionary<string, string[]> mockResultsPerQueries, List<string> log)
            {
                _mockResultsPerQueries = mockResultsPerQueries;
                _log = log;
            }

            public IIndexingEngine IndexingEngine => new IndexingEngineForNestedQueryTests();
            public IQueryEngine QueryEngine => new QueryEngineForNestedQueryTests(_mockResultsPerQueries, _log);
            public IDictionary<string, IndexFieldAnalyzer> GetAnalyzers()
            {
                throw new NotImplementedException();
            }
            public void SetIndexingInfo(IDictionary<string, IPerFieldIndexingInfo> indexingInfo)
            {
                _perFieldIndexingInfos = indexingInfo;
            }
        }
    }
}