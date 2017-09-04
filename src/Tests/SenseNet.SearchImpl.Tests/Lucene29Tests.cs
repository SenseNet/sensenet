using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Index;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        [TestMethod]
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

        [TestMethod]
        public void L29_ClearAndPopulateAll()
        {
            Assert.Inconclusive();

            var console = new StringWriter();

            var result =
                L29Test(s =>
                {
                    StorageContext.Search.SearchEngine.GetPopulator().ClearAndPopulateAll(console);
                    return new Tuple<string, string>(IndexDirectory.CurrentDirectory, s);
                });
            var indx = result.Item1;

            Assert.IsNotNull(indx);
        }

        // =======================================================================================

        protected T L29Test<T>(Func<string, T> callback)
        {
            TypeHandler.Initialize(new Dictionary<Type, Type[]>
            {
                {typeof(ElevatedModificationVisibilityRule), new[] {typeof(SnElevatedModificationVisibilityRule)}}
            });

            var dataProvider = new InMemoryDataProvider();
            StartSecurity(dataProvider);

            DistributedApplication.Cache.Reset();
            IndexDirectory.Reset();

            var indxManConsole = new StringWriter();

            using (new Tools.SearchEngineSwindler(new TestSearchEngine())) //UNDONE: change to final (Lucene29SearchEngine)
            using (Tools.Swindle(typeof(StorageContext.Search), "ContentRepository", new SearchEngineSupport()))
            using (Tools.Swindle(typeof(AccessProvider), "_current", new DesktopAccessProvider()))
            using (Tools.Swindle(typeof(DataProvider), "_current", dataProvider))
            using (new SystemAccount())
            {
                CommonComponents.TransactionFactory = dataProvider;
                EnsureEmptyIndexDirectory();

                var factory = new DefaultIndexingEngineFactory(new Lucene29IndexingEngine(TimeSpan.FromSeconds(30)));
                IndexManager.Start(factory, indxManConsole);

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
