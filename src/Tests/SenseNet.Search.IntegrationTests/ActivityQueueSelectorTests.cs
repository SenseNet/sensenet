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
        }

        [TestMethod, TestCategory("IR")]
        public void Indexing_Distributed()
        {
            var searchEngine = new SearchEngineForActivityQueueSelectorTests(false);
            var result = ActivityQueueSelectorTest(searchEngine, s =>
            {
                var nodeName = "Indexing_Distributed";
                var node = new SystemFolder(Repository.Root) { Name = nodeName };
                using (new SystemAccount())
                    node.Save();

                return true;
            });
        }
        [TestMethod, TestCategory("IR")]
        public void Indexing_Centralized()
        {
            var searchEngine = new SearchEngineForActivityQueueSelectorTests(true);
            var result = ActivityQueueSelectorTest(searchEngine, s =>
            {
                var versioningInfo = new VersioningInfo { LastDraftVersionId = 42, LastPublicVersionId = 42, Delete = new int[0], Reindex = new int[0] };
                var activity = CreateActivity(IndexingActivityType.AddDocument, "/Root/Path1", 42, 42, 424242, versioningInfo);
                activity.Id = 1;

                IndexManager.ExecuteActivity(activity);

                return true;
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

        protected T ActivityQueueSelectorTest<T>(ISearchEngine searchEngine, Func<string, T> callback, [CallerMemberName]string memberName = "")
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

            T result;
            using (Repository.Start(repoBuilder))
            {
                using (ContentRepository.Tests.Tools.Swindle(typeof(SearchManager), "ContentRepository", new SearchEngineSupport()))
                {
                    result = callback(indxManConsole.ToString());
                }
            }

            return result;
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
