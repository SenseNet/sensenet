using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;
using SenseNet.Search.Tests.Implementations;
using SenseNet.SearchImpl.Tests.Implementations;

namespace SenseNet.SearchImpl.Tests
{
    [TestClass]
    public class IndexingTests
    {
        [TestMethod]
        public void Indexing_0()
        {
            var indexingInfo = new Lucene.Net.Support.Dictionary<string, IPerFieldIndexingInfo>
            {
                {"_Text", new TestPerfieldIndexingInfoString()},
                {"Id", new TestPerfieldIndexingInfoInt()},
                {"Name", new TestPerfieldIndexingInfoString()},
            };

            StorageContext.Search.ContentRepository = new TestSearchEngineSupport(indexingInfo);
            var storageContextAcc = new PrivateType(typeof(StorageContext));
            var storageContextInstance = storageContextAcc.GetStaticFieldOrProperty("Instance");
            var storageContextInstanceAcc = new PrivateObject(storageContextInstance);
            storageContextInstanceAcc.SetField("_searchEngine", new TestSearchEngine());

            var dataProvider = new InMemoryDataProvider();
            using(Tools.Swindle(typeof(AccessProvider), "_current", new DesktopAccessProvider()))
            using (Tools.Swindle(typeof(DataProvider), "_current", dataProvider))
            {
                var node = new TestNode(null)
                {
                    Name = "Node1",
                    DisplayName = "Node 1"
                };
                node.Save();
            }
        }
    }
}
