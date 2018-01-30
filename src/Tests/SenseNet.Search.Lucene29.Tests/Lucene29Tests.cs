using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search.Indexing;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;

namespace SenseNet.Search.Lucene29.Tests
{
    /// <summary>
    /// Test indexing engine implementation for throwing an exception during testing.
    /// </summary>
    internal class Lucene29LocalIndexingEngineFailStartup : Lucene29LocalIndexingEngine
    {
        protected override void Startup(TextWriter consoleOut)
        {
            base.Startup(consoleOut);

            throw new InvalidOperationException("Exception thrown for testing purposes, ignore it.");
        }
    }

    [TestClass]
    public class Lucene29Tests : TestBase
    {
        [TestMethod, TestCategory("IR, L29")]
        public void L29_BasicConditions()
        {
            var result =
                L29Test(s =>
                        new Tuple<IIndexingEngine, string>(IndexManager.IndexingEngine, s));
            var engine = result.Item1;
            var console = result.Item2;
            Assert.AreEqual(typeof(Lucene29LocalIndexingEngine).FullName, engine.GetType().FullName);

            var indxDir =((Lucene29LocalIndexingEngine)engine).IndexDirectory.CurrentDirectory;
            Assert.IsNotNull(indxDir);
            Assert.IsTrue(indxDir.Contains(MethodBase.GetCurrentMethod().Name));
            Assert.IsTrue(console.Contains(indxDir));
        }

        [TestMethod, TestCategory("IR, L29")]
        public void L29_ClearAndPopulateAll()
        {
            var sb = new StringBuilder();
            IIndexingActivity[] activities;
            var result = L29Test(s =>
            {
                SaveInitialIndexDocuments();

                var paths = new List<string>();
                var populator = SearchManager.GetIndexPopulator();
                populator.NodeIndexed += (sender, e) => { paths.Add(e.Path); };

                // ACTION
                using (var console = new StringWriter(sb))
                    populator.ClearAndPopulateAll(console);

                // load last indexing activity
                var db = DataProvider.Current;
                var activityId = db.GetLastIndexingActivityId();
                activities = db.LoadIndexingActivities(1, activityId, 10000, false, IndexingActivityFactory.Instance);

                GetAllIdValuesFromIndex(out var nodeIds, out var versionIds);
                return new[]
                {
                    activities.Length,
                    DataProvider.GetNodeCount(),
                    DataProvider.GetVersionCount(),
                    nodeIds.Length,
                    versionIds.Length,
                    paths.Count
                };
            });
            var activityCount = result[0];
            var nodeCount = result[1];
            var versionCount = result[2];
            var nodeIdTermCount = result[3];
            var versionIdTermCount = result[4];
            var pathCount = result[5];

            Assert.AreEqual(0, activityCount);
            Assert.AreEqual(nodeCount, nodeIdTermCount);
            Assert.AreEqual(versionCount, versionIdTermCount);
            Assert.AreEqual(versionCount, pathCount);
        }

        [TestMethod, TestCategory("IR, L29")]
        public void L29_Query()
        {
            QueryResult queryResult1, queryResult2;
            var result =
                L29Test(console =>
                {
                    var indexPopulator = SearchManager.GetIndexPopulator();

                    var root = Repository.Root;
                    indexPopulator.RebuildIndex(root, false, IndexRebuildLevel.DatabaseAndIndex);
                    var admin = User.Administrator;
                    indexPopulator.RebuildIndex(admin, false, IndexRebuildLevel.DatabaseAndIndex);

                    queryResult1 = CreateSafeContentQuery("Id:1").Execute();
                    queryResult2 = CreateSafeContentQuery("Id:2 .COUNTONLY").Execute();

                    return new Tuple<IIndexingEngine, IUser, QueryResult, QueryResult, string>(
                        IndexManager.IndexingEngine, User.Current,
                        queryResult1, queryResult2, console);
                });

            var engine = result.Item1;
            var user = result.Item2;
            queryResult1 = result.Item3;
            queryResult2 = result.Item4;

            Assert.AreEqual(typeof(Lucene29LocalIndexingEngine).FullName, engine.GetType().FullName);
            var indxDir = ((Lucene29LocalIndexingEngine)engine).IndexDirectory.CurrentDirectory;
            Assert.IsNotNull(indxDir);
            Assert.AreEqual(1, user.Id);
            Assert.AreEqual(1, queryResult1.Count);
            Assert.AreEqual(1, queryResult1.Identifiers.FirstOrDefault());
            Assert.AreEqual(1, queryResult2.Count);
            Assert.AreEqual(0, queryResult2.Identifiers.FirstOrDefault());
        }

        [TestMethod, TestCategory("IR, L29")]
        public void L29_Query_TopSkipResultCount()
        {
            L29Test(console =>
            {
                var indexPopulator = SearchManager.GetIndexPopulator();

                var root = Repository.Root;
                indexPopulator.RebuildIndex(root, true, IndexRebuildLevel.DatabaseAndIndex);

                var queryResult = CreateSafeContentQuery("Id:>1 .TOP:10 .SKIP:20 .AUTOFILTERS:OFF").Execute();
                var identifiers = queryResult.Identifiers.ToArray();
                Assert.IsTrue(identifiers.Length > 0);
                Assert.IsTrue(queryResult.Count > identifiers.Length);

                return true;
            });
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            DeleteIndexDirectories();
        }

        [TestMethod, TestCategory("IR, L29")]
        public void L29_SaveAndQuery()
        {
            QueryResult queryResultBefore, queryResultAfter;
            var result =
                L29Test(console =>
                {
                    var indexPopulator = SearchManager.GetIndexPopulator();

                    var root = Repository.Root;
                    indexPopulator.RebuildIndex(root, false, IndexRebuildLevel.DatabaseAndIndex);
                    var admin = User.Administrator;
                    indexPopulator.RebuildIndex(admin, false, IndexRebuildLevel.DatabaseAndIndex);

                    var nodeName = "NodeForL29_SaveAndQuery";

                    queryResultBefore = CreateSafeContentQuery($"Name:{nodeName}").Execute();

                    var node = new SystemFolder(root) {Name = nodeName};
                    using (new SystemAccount())
                        node.Save();

                    queryResultAfter = CreateSafeContentQuery($"Name:{nodeName}").Execute();

                    return new Tuple<QueryResult, QueryResult, int, string>(
                        queryResultBefore, queryResultAfter, node.Id, console);
                });

            queryResultBefore = result.Item1;
            queryResultAfter = result.Item2;
            var nodeId = result.Item3;

            Assert.AreEqual(0, queryResultBefore.Count);
            Assert.AreEqual(1, queryResultAfter.Count);
            Assert.IsTrue(nodeId > 0);
            Assert.AreEqual(nodeId, queryResultAfter.Identifiers.FirstOrDefault());
        }

        //[TestMethod, TestCategory("IR, L29")]
        //public void L29_StartUpFail()
        //{
        //    Assert.Inconclusive("Currently the write.lock cleanup does not work correctly in a test environment.");

        //    var dataProvider = new InMemoryDataProvider();
        //    var securityDataProvider = GetSecurityDataProvider(dataProvider);

        //    // Search engine that contains an indexing engine that will throw 
        //    // an exception during startup to test index directory cleanup.
        //    var searchEngine = new Lucene29SearchEngine
        //    {
        //        IndexingEngine = new Lucene29LocalIndexingEngineFailStartup()
        //    };

        //    Indexing.IsOuterSearchEngineEnabled = true;
        //    CommonComponents.TransactionFactory = dataProvider;
        //    DistributedApplication.Cache.Reset();

        //    var indxManConsole = new StringWriter();
        //    var repoBuilder = new RepositoryBuilder()
        //        .UseDataProvider(dataProvider)
        //        .UseAccessProvider(new DesktopAccessProvider())
        //        .UsePermissionFilterFactory(new EverythingAllowedPermissionFilterFactory())
        //        .UseSearchEngine(searchEngine)
        //        .UseSecurityDataProvider(securityDataProvider)
        //        .UseCacheProvider(new EmptyCache())
        //        .StartWorkflowEngine(false)
        //        .UseTraceCategories(new [] { "Test", "Event", "Repository", "System" });

        //    repoBuilder.Console = indxManConsole;

        //    try
        //    {
        //        using (Repository.Start(repoBuilder))
        //        {
        //            // Although the repo start process fails, the next startup
        //            // should clean the lock file from the index directory.
        //        }
        //    }
        //    catch (InvalidOperationException)
        //    {
        //        // expected
        //    }

        //    // revert to a regular search engine that does not throw an exception
        //    repoBuilder.UseSearchEngine(new Lucene29SearchEngine());

        //    var originalTimeout = Indexing.IndexLockFileWaitForRemovedTimeout;

        //    try
        //    {
        //        // remove lock file after 5 seconds
        //        Indexing.IndexLockFileWaitForRemovedTimeout = 5;

        //        // Start the repo again to check if indexmanager is able to start again correctly.
        //        using (Repository.Start(repoBuilder))
        //        {

        //        }
        //    }
        //    finally
        //    {
        //        Indexing.IndexLockFileWaitForRemovedTimeout = originalTimeout;
        //    }
        //}

        [TestMethod, TestCategory("IR, L29")]
        public void L29_SwitchOffRunningState()
        {
            var dataProvider = new InMemoryDataProvider();
            var securityDataProvider = GetSecurityDataProvider(dataProvider);
            var indexingEngine = new Lucene29LocalIndexingEngine();

            var searchEngine = new Lucene29SearchEngine
            {
                IndexingEngine = indexingEngine
            };

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
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Event", "Repository", "System");

            repoBuilder.Console = indxManConsole;

            try
            {
                using (Repository.Start(repoBuilder))
                {
                    // Switch off the running flag. The shutdown mechanism
                    // should still clean up the index directory.
                    indexingEngine.Running = false;
                }
            }
            catch (InvalidOperationException)
            {
                // expected
            }

            // Start the repo again to check if indexmanager is able to start again correctly.
            using (Repository.Start(repoBuilder))
            {

            }
        }

        [TestMethod, TestCategory("IR, L29")]
        public void L29_NamedIndexDirectory()
        {
            var folderName = "Test_" + MethodBase.GetCurrentMethod().Name;

            var dataProvider = new InMemoryDataProvider();
            var indexingEngine = new Lucene29LocalIndexingEngine(new IndexDirectory(folderName));
            var searchEngine = new Lucene29SearchEngine
            {
                IndexingEngine = indexingEngine
            };

            Configuration.Indexing.IsOuterSearchEngineEnabled = true;
            CommonComponents.TransactionFactory = dataProvider;
            DistributedApplication.Cache.Reset();

            var indxManConsole = new StringWriter();
            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(dataProvider)
                .UseAccessProvider(new DesktopAccessProvider())
                .UsePermissionFilterFactory(new EverythingAllowedPermissionFilterFactory())
                .UseSearchEngine(searchEngine)
                .UseSecurityDataProvider(GetSecurityDataProvider(dataProvider))
                .UseCacheProvider(new EmptyCache())
                .StartWorkflowEngine(false)
                .UseTraceCategories("Test", "Event", "Repository", "System");

            repoBuilder.Console = indxManConsole;

            using (Repository.Start(repoBuilder))
            {
                var expectedPath = Path.Combine(SearchManager.IndexDirectoryPath, folderName);

                Assert.AreEqual(expectedPath,
                    indexingEngine.IndexDirectory.CurrentDirectory);
                Assert.AreEqual(Path.Combine(expectedPath, "write.lock"),
                    indexingEngine.IndexDirectory.IndexLockFilePath);
                Assert.IsTrue(System.IO.File.Exists(indexingEngine.IndexDirectory.IndexLockFilePath));
            }
        }

        [TestMethod, TestCategory("IR, L29")]
        public void L29_ActivityStatus_WithoutSave()
        {
            var newStatus = new IndexingActivityStatus
            {
                LastActivityId = 33,
                Gaps = new[] { 5, 6, 7 }
            };

            var result = L29Test(s =>
            {
                var searchEngine = SearchManager.SearchEngine;
                var originalStatus = searchEngine.IndexingEngine.ReadActivityStatusFromIndex();

                searchEngine.IndexingEngine.WriteActivityStatusToIndex(newStatus);

                var updatedStatus = searchEngine.IndexingEngine.ReadActivityStatusFromIndex();

                return new Tuple<IndexingActivityStatus, IndexingActivityStatus>(originalStatus, updatedStatus);
            });

            var resultStatus = new IndexingActivityStatus()
            {
                LastActivityId = result.Item2.LastActivityId,
                Gaps = result.Item2.Gaps
            };

            Assert.AreEqual(result.Item1.LastActivityId, 0);
            Assert.AreEqual(result.Item1.Gaps.Length, 0);
            Assert.AreEqual(newStatus.ToString(), resultStatus.ToString());
        }
        [TestMethod, TestCategory("IR, L29")]
        public void L29_ActivityStatus_WithSave()
        {
            var result = L29Test(s =>
            {
                var searchEngine = SearchManager.SearchEngine;
                var originalStatus = searchEngine.IndexingEngine.ReadActivityStatusFromIndex();

                var node = new SystemFolder(Repository.Root) { Name = "L29_ActivityStatus_WithSave" };
                using (new SystemAccount())
                    node.Save();

                var updatedStatus = searchEngine.IndexingEngine.ReadActivityStatusFromIndex();

                return new Tuple<IndexingActivityStatus, IndexingActivityStatus>(originalStatus, updatedStatus);
            });

            Assert.AreEqual(result.Item1.LastActivityId + 1, result.Item2.LastActivityId);
        }

        [TestMethod, TestCategory("IR, L29")]
        public void L29_Analyzers()
        {
            L29Test(s =>
            {
                var searchEngine = SearchManager.SearchEngine;
                //var analyzers = searchEngine.GetAnalyzers();
                var indexingEngine = (Lucene29LocalIndexingEngine) searchEngine.IndexingEngine;

                var masterAnalyzerAcc = new PrivateObject(indexingEngine.GetAnalyzer());

                Assert.AreEqual(typeof(StandardAnalyzer).FullName, ((Analyzer)masterAnalyzerAcc.Invoke("GetAnalyzer", IndexFieldName.AllText)).GetType().FullName);
                Assert.AreEqual(typeof(KeywordAnalyzer).FullName, ((Analyzer)masterAnalyzerAcc.Invoke("GetAnalyzer", IndexFieldName.NodeId)).GetType().FullName);
                Assert.AreEqual(typeof(KeywordAnalyzer).FullName, ((Analyzer)masterAnalyzerAcc.Invoke("GetAnalyzer", IndexFieldName.Name)).GetType().FullName);
                Assert.AreEqual(typeof(StandardAnalyzer).FullName, ((Analyzer)masterAnalyzerAcc.Invoke("GetAnalyzer", "Description")).GetType().FullName);
                Assert.AreEqual(typeof(StandardAnalyzer).FullName, ((Analyzer)masterAnalyzerAcc.Invoke("GetAnalyzer", "Binary")).GetType().FullName);
                Assert.AreEqual(typeof(KeywordAnalyzer).FullName, ((Analyzer)masterAnalyzerAcc.Invoke("GetAnalyzer", "FakeField")).GetType().FullName);

                return 0;
            });
        }

        /* ======================================================================================= */

        [TestMethod, TestCategory("IR, L29")]
        public void L29_IntegrityChecker()
        {
            var result = L29Test(s =>
            {
                SaveInitialIndexDocuments();
                var populator = SearchManager.GetIndexPopulator();
                populator.ClearAndPopulateAll();

                var node = new SystemFolder(Repository.Root) { Name = "L29_IntegrityChecker" };
                using (new SystemAccount())
                    node.Save();

                return new Tuple<object, Difference[]>(
                    IntegrityChecker.CheckIndexIntegrity("/Root", true),
                    IntegrityChecker.Check("/Root", true).ToArray());
            });
            //var checkerResult = result.Item1;
            var diffs = result.Item2;

            Assert.AreEqual(1, diffs.Length);
            Assert.AreEqual(IndexDifferenceKind.NotInDatabase, diffs[0].Kind);
            Assert.AreEqual(-1, diffs[0].NodeId);
            Assert.AreEqual(-1, diffs[0].VersionId);
            Assert.AreEqual(null, diffs[0].Path);
        }

        /* ======================================================================================= */

        protected T L29Test<T>(Func<string, T> callback, [CallerMemberName]string memberName = "")
        {
            var dataProvider = new InMemoryDataProvider();
            var securityDataProvider = GetSecurityDataProvider(dataProvider);
            var indexFolderName = $"Test_{memberName}_{Guid.NewGuid()}";
            var indexingEngine = new Lucene29LocalIndexingEngine(new IndexDirectory(indexFolderName));
            var searchEngine = new Lucene29SearchEngine
            {
                IndexingEngine = indexingEngine
            };

            Configuration.Indexing.IsOuterSearchEngineEnabled = true;
            CommonComponents.TransactionFactory = dataProvider;
            DistributedApplication.Cache.Reset();
            ContentTypeManager.Reset();

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
          .StartWorkflowEngine(false);

            repoBuilder.Console = indxManConsole;

            T result;
            using (Repository.Start(repoBuilder))
            {
                //IndexDirectory.CreateNew();
                //IndexDirectory.Reset();

                using (SenseNet.Tests.Tools.Swindle(typeof(SearchManager), "_searchEngineSupport", new SearchEngineSupport()))
                    //using (new SystemAccount())
                {
                    //EnsureEmptyIndexDirectory();

                    result = callback(indxManConsole.ToString());
                }
            }

            return result;
        }

        private void GetAllIdValuesFromIndex(out int[] nodeIds, out int[] versionIds)
        {
            var nodeIdList = new List<int>();
            var versionIdLists = new List<int>();
            using (var rf = Lucene29LocalIndexingEngine.GetReaderFrame())
            {
                var reader = rf.IndexReader;
                for (var d = 0; d < reader.NumDocs(); d++)
                {
                    var doc = reader.Document(d);

                    var nodeIdString = doc.Get(IndexFieldName.NodeId);
                    if (!string.IsNullOrEmpty(nodeIdString))
                        nodeIdList.Add(int.Parse(nodeIdString));

                    var versionIdString = doc.Get(IndexFieldName.VersionId);
                    if (!string.IsNullOrEmpty(versionIdString))
                        versionIdLists.Add(int.Parse(versionIdString));
                }
            }
            nodeIds = nodeIdList.ToArray();
            versionIds = versionIdLists.ToArray();
        }

        public void EnsureEmptyIndexDirectory()
        {
            var path = SearchManager.IndexDirectoryPath;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            //IndexDirectory.Instance.CreateNew();
            //IndexManager.ClearIndex();
        }

        public static void DeleteIndexDirectories()
        {
            var path = SearchManager.IndexDirectoryPath;
            foreach (var indexDir in Directory.GetDirectories(path))
            {
                try
                {
                    Directory.Delete(indexDir, true);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            foreach (var file in Directory.GetFiles(path))
            {
                try
                {
                    System.IO.File.Delete(file);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }
}
