using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Search.Indexing.Activities;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;
using SenseNet.Testing;
using SenseNet.Tests.Core;
using SenseNet.Tests.Core.Implementations;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Search.Tests
{
    [TestClass]
    public class ActivityQueueSelectorTests : TestBase
    {
        private IDataStore DataStore => Providers.Instance.DataStore;

        private class IndexingEngineForActivityQueueSelectorTests : IIndexingEngine
        {
            public IndexingEngineForActivityQueueSelectorTests(bool centralized)
            {
                IndexIsCentralized = centralized;
            }

            readonly StringBuilder _log = new StringBuilder();
            public string GetLog()
            {
                return _log.ToString();
            }

            public void ClearLog()
            {
                _log.Clear();
            }

            public bool Running { get; private set; }
            public bool IndexIsCentralized { get; }
            public Task StartAsync(TextWriter consoleOut, CancellationToken cancellationToken)
            {
                Running = true;
                return Task.CompletedTask;
            }
            public Task ShutDownAsync(CancellationToken cancellationToken)
            {
                Running = false;
                return Task.CompletedTask;
            }
            public Task<BackupResponse> BackupAsync(string target, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
            public Task<BackupResponse> QueryBackupAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
            public Task<BackupResponse> CancelBackupAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
            public Task ClearIndexAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
            public Task<IndexingActivityStatus> ReadActivityStatusFromIndexAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(IndexingActivityStatus.Startup);
            }
            public Task WriteActivityStatusToIndexAsync(IndexingActivityStatus state, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
            [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
            public Task WriteIndexAsync(IEnumerable<SnTerm> deletions, IEnumerable<DocumentUpdate> updates, IEnumerable<IndexDocument> additions,
                CancellationToken cancellationToken)
            {
                var distributed = Environment.StackTrace.Contains(typeof(DistributedIndexingActivityQueue).FullName) ? "DISTRIBUTED" : "";
                var centralized = Environment.StackTrace.Contains(typeof(CentralizedIndexingActivityQueue).FullName) ? "CENTRALIZED" : "";
                _log.AppendLine($"{centralized}{distributed}. deletions: {deletions?.Count() ?? 0}, updates: {updates?.Count() ?? 0}, addition: {additions?.Count() ?? 0}");
                return Task.CompletedTask;
            }
        }

        private class QueryEngineForActivityQueueSelectorTests : IQueryEngine
        {
            public QueryResult<int> ExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context)
            {
                return QueryResult<int>.Empty;
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

            public void ClearIndexingLog()
            {
                ((IndexingEngineForActivityQueueSelectorTests)IndexingEngine).ClearLog();
            }
        }

        [TestMethod, TestCategory("IR")]
        public void Indexing_ActivitySelector_Distributed()
        {
            var searchEngine = new SearchEngineForActivityQueueSelectorTests(false);
            var indxManConsole = new StringWriter();
            Test(builder =>
            {
                Configuration.Indexing.IsOuterSearchEngineEnabled = true;
                builder.UseSearchManager(new SearchManager(DataStore));
                builder.UseIndexManager(new IndexManager(DataStore, Providers.Instance.SearchManager));
                builder.UseIndexPopulator(new DocumentPopulator(DataStore, Providers.Instance.IndexManager));
                builder.UseSearchEngine(searchEngine);
                builder.SetConsole(indxManConsole);
            }, () =>
            {
                searchEngine.ClearIndexingLog();

                var nodeName = "Indexing_Distributed";
                var node = new SystemFolder(Repository.Root) {Name = nodeName};
                node.Save();
                Assert.AreEqual("DISTRIBUTED. deletions: 0, updates: 0, addition: 1\r\n",
                    searchEngine.GetIndexingLog());
            });
        }
        [TestMethod, TestCategory("IR")]
        public void Indexing_ActivitySelector_Centralized()
        {
            var searchEngine = new SearchEngineForActivityQueueSelectorTests(true);
            var indxManConsole = new StringWriter();
            Test(builder =>
            {
                Configuration.Indexing.IsOuterSearchEngineEnabled = true;
                builder.UseSearchManager(new SearchManager(DataStore));
                builder.UseIndexManager(new IndexManager(DataStore, Providers.Instance.SearchManager));
                builder.UseIndexPopulator(new DocumentPopulator(DataStore, Providers.Instance.IndexManager));
                builder.UseSearchEngine(searchEngine);
                builder.SetConsole(indxManConsole);
            }, () =>
            {
                searchEngine.ClearIndexingLog();
                
                var nodeName = "Indexing_Centralized";
                var node = new SystemFolder(Repository.Root) { Name = nodeName };
                node.Save();
                Assert.AreEqual("CENTRALIZED. deletions: 0, updates: 0, addition: 1\r\n", searchEngine.GetIndexingLog());
            });
        }

        //TODO: Indexing_ActivitySelector_Centralized_InMemory_ExecuteUnprocessed is inactivated
        //[TestMethod, TestCategory("IR")]
        public void Indexing_ActivitySelector_Centralized_InMemory_ExecuteUnprocessed()
        {
            // This test calls the "Test" method twice but the first run's database (and index)
            // is not passed well to the second run.
            Assert.Inconclusive();

            var searchEngine = new SearchEngineForActivityQueueSelectorTests(false);
            var nodeId = 0;
            var versionId = 0;
            var path = string.Empty;
            var indxManConsole = new StringWriter();
            Test(builder =>
            {
                Configuration.Indexing.IsOuterSearchEngineEnabled = true;
                builder.UseSearchManager(new SearchManager(DataStore));
                builder.UseIndexManager(new IndexManager(DataStore, Providers.Instance.SearchManager));
                builder.UseIndexPopulator(new DocumentPopulator(DataStore, Providers.Instance.IndexManager));
                builder.UseSearchEngine(searchEngine);
                builder.SetConsole(indxManConsole);
            }, () =>
            {
                // create a valid version
                var nodeName = "Indexing_Centralized_InMemory_ExecuteUnprocessed";
                var node = new SystemFolder(Repository.Root) { Name = nodeName };
                node.Save();
                nodeId = node.Id;
                versionId = node.VersionId;
                path = node.Path;
            });

            SnTrace.Test.Write("Indexing_Centralized_InMemory_ExecuteUnprocessed ACTION");
            searchEngine = new SearchEngineForActivityQueueSelectorTests(true);

            var dp2 = Providers.Instance.DataProvider;
            Test(builder =>
            {
                Providers.Instance.DataProvider = dp2;

                DataStore.DataProvider.DeleteAllIndexingActivitiesAsync(CancellationToken.None).GetAwaiter().GetResult();
                RegisterActivity(IndexingActivityType.AddDocument, IndexingActivityRunningState.Waiting, nodeId, versionId, path);
                RegisterActivity(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, nodeId, versionId, path);
                RegisterActivity(IndexingActivityType.UpdateDocument, IndexingActivityRunningState.Waiting, nodeId, versionId, path);

                Configuration.Indexing.IsOuterSearchEngineEnabled = true;
                builder.UseSearchManager(new SearchManager(DataStore));
                builder.UseIndexManager(new IndexManager(DataStore, Providers.Instance.SearchManager));
                builder.UseIndexPopulator(new DocumentPopulator(DataStore, Providers.Instance.IndexManager));
                builder.UseSearchEngine(searchEngine);
                builder.SetConsole(indxManConsole);
                builder.UseInitialData(null);
            }, () =>
            {
                var log = searchEngine.GetIndexingLog();
                Assert.AreEqual(
                    "CENTRALIZED. deletions: 0, updates: 0, addition: 1\r\n" +
                    "CENTRALIZED. deletions: 0, updates: 1, addition: 0\r\n" +
                    "CENTRALIZED. deletions: 0, updates: 1, addition: 0\r\n", log);
            });
        }

        /* ============================================================================================== */

        private void RegisterActivity(IndexingActivityType type, IndexingActivityRunningState state, int nodeId, int versionId, string path)
        {
            IndexingActivityBase activity;
            if (type == IndexingActivityType.AddTree || type == IndexingActivityType.RemoveTree)
                activity = CreateTreeActivity(type, path, nodeId);
            else
                activity = CreateActivity(type, path, nodeId, versionId, 9999);
            activity.RunningState = state;

            DataStore.DataProvider.RegisterIndexingActivityAsync(activity, CancellationToken.None).GetAwaiter().GetResult();
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
                    //Reindex = type == IndexingActivityType.AddDocument ? new int[0] : new[] { versionId },
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
