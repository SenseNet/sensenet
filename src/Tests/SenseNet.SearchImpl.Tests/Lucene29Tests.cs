using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Lucene29;

namespace SenseNet.SearchImpl.Tests
{
    [TestClass]
    public class Lucene29Tests : TestBase
    {
        [TestMethod]
        public void L29_BasicConditions()
        {
            var result = L29Test((s) => new Tuple<IIndexingEngine, string, string>(IndexManager.IndexingEngine, IndexDirectory.CurrentDirectory, s));
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

            var result = L29Test<string>((s) =>
            {
                return IndexDirectory.CurrentDirectory;
            });
        }
    }
}
