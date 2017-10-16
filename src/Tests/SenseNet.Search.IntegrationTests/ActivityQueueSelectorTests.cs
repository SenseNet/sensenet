using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Tests;
using SenseNet.ContentRepository.Tests.Implementations;
using SenseNet.Search.Indexing;
using SenseNet.Search.Indexing.Activities;
using SenseNet.SearchImpl.Tests.Implementations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.IntegrationTests
{
    [TestClass]
    public class ActivityQueueSelectorTests : TestBase
    {
        private class IndexingEngineForActivityQueueSelectorTests : IIndexingEngine
        {
            public bool Running { get; private set; }

            private bool _centralized;
            public bool WorksAsCentralizedIndex => _centralized;

            public IndexingEngineForActivityQueueSelectorTests(bool centralized)
            {
                _centralized = centralized;
            }

            public void ClearIndex()
            {
                throw new NotImplementedException();
            }

            public IndexingActivityStatus ReadActivityStatusFromIndex()
            {
                return IndexingActivityStatus.Startup;
            }

            public void ShutDown()
            {
                Running = false;
            }

            public void Start(TextWriter consoleOut)
            {
                IndexingActivityQueue.Startup(consoleOut);
                Running = true;
            }

            public void WriteActivityStatusToIndex(IndexingActivityStatus state)
            {
            }

            public void WriteIndex(IEnumerable<SnTerm> deletions, IEnumerable<DocumentUpdate> updates, IEnumerable<IndexDocument> addition)
            {
                var distributed = Environment.StackTrace.Contains(typeof(IndexingActivityQueue).FullName) ? "DISTRIBUTED" : "";
                var centralized = Environment.StackTrace.Contains(typeof(CentralizedIndexingActivityQueue).FullName) ? "CENTRALIZED" : "";
                _log.Append($"{centralized}{distributed}. deletions: {deletions?.Count() ?? 0}, updates: {updates?.Count() ?? 0}, addition: {addition?.Count() ?? 0}");
            }

            StringBuilder _log = new StringBuilder();
            public string GetLog()
            {
                return _log.ToString();
            }
        }

        private class QueryEngineForActivityQueueSelectorTests : IQueryEngine
        {
            public IQueryResult<int> ExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context)
            {
                throw new NotImplementedException();
            }

            public IQueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter filter, IQueryContext context)
            {
                throw new NotImplementedException();
            }
        }

        private class SearchEngineForActivityQueueSelectorTests : ISearchEngine
        {
            public IIndexingEngine IndexingEngine { get; }

            public IQueryEngine QueryEngine { get; }

            public SearchEngineForActivityQueueSelectorTests(bool centralized)
            {
                IndexingEngine = new IndexingEngineForActivityQueueSelectorTests(centralized);
                QueryEngine = new QueryEngineForActivityQueueSelectorTests();
            }

            public IDictionary<string, IndexFieldAnalyzer> GetAnalyzers()
            {
                throw new NotImplementedException();
            }

            public void SetIndexingInfo(IDictionary<string, IPerFieldIndexingInfo> indexingInfo)
            {
            }

            public string GetIndexingLog()
            {
                return ((IndexingEngineForActivityQueueSelectorTests)IndexingEngine).GetLog();
            }
        }

        [TestMethod, TestCategory("IR")]
        public void Indexing_Distributed()
        {
            var searchEngine = new SearchEngineForActivityQueueSelectorTests(false);
            ActivityQueueSelectorTest(searchEngine, s =>
            {
                var nodeName = "Indexing_Distributed";
                var node = new SystemFolder(Repository.Root) { Name = nodeName };
                using (new SystemAccount())
                    node.Save();

                Assert.AreEqual($"DISTRIBUTED. deletions: 0, updates: 0, addition: 1", searchEngine.GetIndexingLog());
            });
        }
        [TestMethod, TestCategory("IR")]
        public void Indexing_Centralized()
        {
            var searchEngine = new SearchEngineForActivityQueueSelectorTests(true);
            ActivityQueueSelectorTest(searchEngine, s =>
            {
                var nodeName = "Indexing_Centralized";
                var node = new SystemFolder(Repository.Root) { Name = nodeName };
                using (new SystemAccount())
                    node.Save();

                Assert.AreEqual($"CENTRALIZED. deletions: 0, updates: 0, addition: 1", searchEngine.GetIndexingLog());
            });
        }

        //[TestMethod, TestCategory("IR")]
        //public void IndexingSql_SaveNode()
        //{
        //    var result = IndexingSqlTest(s =>
        //    {
        //        var searchEngine = SearchManager.SearchEngine;
        //        var originalStatus = searchEngine.IndexingEngine.ReadActivityStatusFromIndex();

        //        var node = new SystemFolder(Repository.Root) { Name = "IndexingSql_SaveNode" };
        //        using (new SystemAccount())
        //            node.Save();

        //        var updatedStatus = searchEngine.IndexingEngine.ReadActivityStatusFromIndex();

        //        return new Tuple<IndexingActivityStatus, IndexingActivityStatus>(originalStatus, updatedStatus);
        //    });

        //    Assert.AreEqual(result.Item1.LastActivityId + 1, result.Item2.LastActivityId);
        //}

        /* ============================================================================================== */

        protected void ActivityQueueSelectorTest(ISearchEngine searchEngine, Action<string> callback, [CallerMemberName]string memberName = "")
        {
            var dataProvider = new InMemoryDataProvider();
            var securityDataProvider = GetSecurityDataProvider(dataProvider);
            var indexFolderName = "Test_" + memberName;

            Configuration.Indexing.IsOuterSearchEngineEnabled = true;
            CommonComponents.TransactionFactory = dataProvider;
            DistributedApplication.Cache.Reset();

            var indxManConsole = new StringWriter();
            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(dataProvider)
                .UseAccessProvider(new DesktopAccessProvider())
                .UsePermissionFilterFactory(new EverythingAllowedPermissionFilterFactory())
                .UseSearchEngine(searchEngine)
                .UseSecurityDataProvider(securityDataProvider)
                .UseCacheProvider(new EmptyCache())
                .UseTraceCategories(new[] { "ContentOperation", "Event", "Repository", "IndexQueue", "Index", "Query" })
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(SettingsCache))
                .StartWorkflowEngine(false);

            repoBuilder.Console = indxManConsole;

            using (Repository.Start(repoBuilder))
            {
                using (ContentRepository.Tests.Tools.Swindle(typeof(SearchManager), "ContentRepository", new SearchEngineSupport()))
                {
                    callback(indxManConsole.ToString());
                }
            }
        }

        private IndexingActivityBase CreateActivity(IndexingActivityType type, string path, int nodeId, int versionId, long versionTimestamp, VersioningInfo versioningInfo)
        {
            var activity = (IndexingActivityBase)IndexingActivityFactory.Instance.CreateActivity(type);
            activity.Path = path.ToLowerInvariant();
            activity.NodeId = nodeId;
            activity.VersionId = versionId;
            activity.VersionTimestamp = versionTimestamp;

            var lucDocAct = activity as DocumentIndexingActivity;
            if (lucDocAct != null)
                lucDocAct.IndexDocumentData = new IndexDocumentData(null, new byte[0]); ;

            var documentActivity = activity as DocumentIndexingActivity;
            if (documentActivity != null)
                documentActivity.Versioning = versioningInfo;

            return activity;
        }
    }
}
