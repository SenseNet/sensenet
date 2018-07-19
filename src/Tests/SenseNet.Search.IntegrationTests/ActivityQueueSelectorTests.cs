using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Search.Indexing.Activities;
using SenseNet.Search.Querying;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;

namespace SenseNet.Search.IntegrationTests
{
    [TestClass]
    public class ActivityQueueSelectorTests : TestBase
    {
        private class IndexingEngineForActivityQueueSelectorTests : IIndexingEngine
        {
            public bool Running { get; private set; }

            public bool IndexIsCentralized { get; }

            public IndexingEngineForActivityQueueSelectorTests(bool centralized)
            {
                IndexIsCentralized = centralized;
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
                Running = true;
            }

            public void WriteActivityStatusToIndex(IndexingActivityStatus state)
            {
            }

            [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
            public void WriteIndex(IEnumerable<SnTerm> deletions, IEnumerable<DocumentUpdate> updates, IEnumerable<IndexDocument> additions)
            {
                var distributed = Environment.StackTrace.Contains(typeof(DistributedIndexingActivityQueue).FullName) ? "DISTRIBUTED" : "";
                var centralized = Environment.StackTrace.Contains(typeof(CentralizedIndexingActivityQueue).FullName) ? "CENTRALIZED" : "";
                _log.AppendLine($"{centralized}{distributed}. deletions: {deletions?.Count() ?? 0}, updates: {updates?.Count() ?? 0}, addition: {additions?.Count() ?? 0}");
            }

            readonly StringBuilder _log = new StringBuilder();
            public string GetLog()
            {
                return _log.ToString();
            }
        }

        private class QueryEngineForActivityQueueSelectorTests : IQueryEngine
        {
            public QueryResult<int> ExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context)
            {
                throw new NotImplementedException();
            }

            public QueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter filter, IQueryContext context)
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
        public void Indexing_ActivitySelector_Distributed()
        {
            var searchEngine = new SearchEngineForActivityQueueSelectorTests(false);
            ActivityQueueSelectorTest(searchEngine, null, s =>
            {
                var nodeName = "Indexing_Distributed";
                var node = new SystemFolder(Repository.Root) { Name = nodeName };
                using (new SystemAccount())
                    node.Save();

                Assert.AreEqual("DISTRIBUTED. deletions: 0, updates: 0, addition: 1\r\n", searchEngine.GetIndexingLog());
            });
        }
        [TestMethod, TestCategory("IR")]
        public void Indexing_ActivitySelector_Centralized()
        {
            var searchEngine = new SearchEngineForActivityQueueSelectorTests(true);
            ActivityQueueSelectorTest(searchEngine, null, s =>
            {
                var nodeName = "Indexing_Centralized";
                var node = new SystemFolder(Repository.Root) { Name = nodeName };
                using (new SystemAccount())
                    node.Save();

                Assert.AreEqual("CENTRALIZED. deletions: 0, updates: 0, addition: 1\r\n", searchEngine.GetIndexingLog());
            });
        }

        [TestMethod, TestCategory("IR")]
        public void Indexing_Centralized_InMemory_ExecuteUnprocessed()
        {
            var dataProvider = new InMemoryDataProvider();
            var searchEngine = new SearchEngineForActivityQueueSelectorTests(false);
            var nodeId = 0;
            var versionId = 0;
            var path = string.Empty;
            ActivityQueueSelectorTest(searchEngine, dataProvider, s =>
            {
                // create a valid version
                var nodeName = "Indexing_Centralized_InMemory_ExecuteUnprocessed";
                var node = new SystemFolder(Repository.Root) { Name = nodeName };
                using (new SystemAccount())
                    node.Save();
                nodeId = node.Id;
                versionId = node.VersionId;
                path = node.Path;
            });

            DataProvider.Current.DeleteAllIndexingActivities();
            RegisterActivity(IndexingActivityType.AddDocument, IndexingActivityRunningState.Waiting, nodeId, versionId, path);
            RegisterActivity(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, nodeId, versionId, path);
            RegisterActivity(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, nodeId, versionId, path);

            SnTrace.Test.Write("Indexing_Centralized_InMemory_ExecuteUnprocessed ACTION");

            searchEngine = new SearchEngineForActivityQueueSelectorTests(true);
            ActivityQueueSelectorTest(searchEngine, dataProvider, s =>
            {
                var log = searchEngine.GetIndexingLog();
                Assert.AreEqual(
                    "CENTRALIZED. deletions: 0, updates: 0, addition: 1\r\n" +
                    "CENTRALIZED. deletions: 0, updates: 1, addition: 0\r\n" +
                    "CENTRALIZED. deletions: 0, updates: 1, addition: 0\r\n", log);
            });
        }

        /* ============================================================================================== */

        protected void ActivityQueueSelectorTest(ISearchEngine searchEngine, InMemoryDataProvider dataProvider, Action<string> callback, [CallerMemberName]string memberName = "")
        {
            if(dataProvider == null)
                dataProvider = new InMemoryDataProvider();
            var securityDataProvider = GetSecurityDataProvider(dataProvider);

            Configuration.Indexing.IsOuterSearchEngineEnabled = true;
            CommonComponents.TransactionFactory = dataProvider;
            DistributedApplication.Cache.Reset();

            var indxManConsole = new StringWriter();
            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(dataProvider)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dataProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseAccessProvider(new DesktopAccessProvider())
                .UsePermissionFilterFactory(new EverythingAllowedPermissionFilterFactory())
                .UseSearchEngine(searchEngine)
                .UseSecurityDataProvider(securityDataProvider)
                .UseCacheProvider(new EmptyCache())
                .UseTraceCategories("ContentOperation", "Event", "Repository", "IndexQueue", "Index", "Query")
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(SettingsCache))
                .StartWorkflowEngine(false)
                .SetConsole(indxManConsole);

            using (Repository.Start(repoBuilder))
            {
                using (Tests.Tools.Swindle(typeof(SearchManager), "_searchEngineSupport", new SearchEngineSupport()))
                {
                    callback(indxManConsole.ToString());
                }
            }
        }

        private void RegisterActivity(IndexingActivityType type, IndexingActivityRunningState state, int nodeId, int versionId, string path)
        {
            IndexingActivityBase activity;
            if (type == IndexingActivityType.AddTree || type == IndexingActivityType.RemoveTree)
                activity = CreateTreeActivity(type, path, nodeId);
            else
                activity = CreateActivity(type, path, nodeId, versionId, 9999);
            activity.RunningState = state;

            DataProvider.Current.RegisterIndexingActivity(activity);
        }
        private IndexingActivityBase CreateActivity(IndexingActivityType type, string path, int nodeId, int versionId, long versionTimestamp)
        {
            var activity = (IndexingActivityBase)IndexingActivityFactory.Instance.CreateActivity(type);
            activity.Path = path.ToLowerInvariant();
            activity.NodeId = nodeId;
            activity.VersionId = versionId;
            activity.VersionTimestamp = versionTimestamp;

            if (activity is DocumentIndexingActivity documentActivity)
            {
                documentActivity.SetDocument(new IndexDocument());
                documentActivity.Versioning = new VersioningInfo
                {
                    Delete = new int[0],
                    Reindex = new int[0],
                    LastDraftVersionId = versionId,
                    LastPublicVersionId = versionId
                };
            }

            return activity;
        }

        private IndexingActivityBase CreateTreeActivity(IndexingActivityType type, string path, int nodeId)
        {
            var activity = (IndexingActivityBase)IndexingActivityFactory.Instance.CreateActivity(type);
            activity.Path = path.ToLowerInvariant();
            activity.NodeId = nodeId;

            return activity;
        }
    }
}
