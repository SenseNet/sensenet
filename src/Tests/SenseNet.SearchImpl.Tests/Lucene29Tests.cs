using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Lucene29;
using SenseNet.SearchImpl.Tests.Implementations;

namespace SenseNet.SearchImpl.Tests
{
    [TestClass]
    public class Lucene29Tests : TestBase
    {
        [TestMethod, TestCategory("IR"), Timeout(20*1000)]
        public void L29_BasicConditions()
        {
            var result =
                L29Test(s =>
                        new Tuple<IIndexingEngine, string, string>(IndexManager.IndexingEngine,
                            IndexDirectory.CurrentDirectory, s));
            var engine = result.Item1;
            var indxDir = result.Item2;
            var console = result.Item3;

            Assert.AreEqual(typeof(Lucene29IndexingEngine).FullName, engine.GetType().FullName);
            Assert.IsNotNull(indxDir);
        }

        [TestMethod, TestCategory("IR"), Timeout(40*1000)]
        public void L29_ClearAndPopulateAll()
        {
            var sb = new StringBuilder();
            IIndexingActivity[] activities;
            var result = L29Test(s =>
            {
                SaveInitialIndexDocuments();

                var paths = new List<string>();
                var populator = StorageContext.Search.SearchEngine.GetPopulator();
                populator.NodeIndexed += (sender, e) => { paths.Add(e.Path); };

                // ACTION
                using (var console = new StringWriter(sb))
                    populator.ClearAndPopulateAll(console);

                // load last indexing activity
                var db = DataProvider.Current;
                var activityId = db.GetLastActivityId();
                activities = db.LoadIndexingActivities(1, activityId, 10000, false, IndexingActivityFactory.Instance);

                int[] nodeIds, versionIds;
                GetAllIdValuesFromIndex(out nodeIds, out versionIds);
                return new[]
                {
                    activities.Length,
                    DataProvider.GetNodeCount(),
                    DataProvider.GetVersionCount(),
                    nodeIds.Length,
                    versionIds.Length
                };
            });
            var activityCount = result[0];
            var nodeCount = result[1];
            var versionCount = result[2];
            var nodeIdTermCount = result[3];
            var versionIdTermCount = result[4];

            Assert.AreEqual(0, activityCount);
            Assert.AreEqual(nodeCount, nodeIdTermCount);
            Assert.AreEqual(versionCount, versionIdTermCount);
        }

        // =======================================================================================

        protected T L29Test<T>(Func<string, T> callback)
        {
            var dataProvider = new InMemoryDataProvider();
            var securityDataProvider = GetSecurityDataProvider(dataProvider);

            Indexing.IsOuterSearchEngineEnabled = true;
            CommonComponents.TransactionFactory = dataProvider;
            DistributedApplication.Cache.Reset();

            var indxManConsole = new StringWriter();
            var repoBuilder = new RepositoryBuilder()
                .UseDataProvider(dataProvider)
                .UseAccessProvider(new DesktopAccessProvider())
                .UseSearchEngine(new Lucene29SearchEngine())
                .UseSecurityDataProvider(securityDataProvider)
                .StartWorkflowEngine(false);

            repoBuilder.Console = indxManConsole;

            using (Repository.Start(repoBuilder))
            {
                IndexDirectory.Reset();

                using (Tools.Swindle(typeof (StorageContext.Search), "ContentRepository", new SearchEngineSupport()))
                using (new SystemAccount())
                {
                    EnsureEmptyIndexDirectory();

                    try
                    {
                        var result = callback(indxManConsole.ToString());
                        return result;
                    }
                    finally
                    {
                        DeleteIndexDirectories();
                    }
                }
            }
        }

        private void GetAllIdValuesFromIndex(out int[] nodeIds, out int[] versionIds)
        {
            var nodeIdList = new List<int>();
            var versionIdLists = new List<int>();
            using (var rf = IndexReaderFrame.GetReaderFrame())
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
            var path = StorageContext.Search.IndexDirectoryPath;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            IndexDirectory.CreateNew();
            //IndexManager.ClearIndex();
        }

        public void DeleteIndexDirectories()
        {
            var path = StorageContext.Search.IndexDirectoryPath;
            foreach (var indexDir in Directory.GetDirectories(path))
            {
                try
                {
                    Directory.Delete(indexDir, true);
                }
                catch (Exception e)
                {
                }
            }
            foreach (var file in Directory.GetFiles(path))
            {
                try
                {
                    System.IO.File.Delete(file);
                }
                catch (Exception e)
                {
                }
            }
        }


    }
}
