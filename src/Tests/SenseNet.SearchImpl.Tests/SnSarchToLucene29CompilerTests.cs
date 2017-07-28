using System;
using Lucene.Net.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;
using SenseNet.Search.Lucene29;
using SenseNet.Search.Parser;
using SenseNet.Search.Tests.Implementations;
using SenseNet.SearchImpl.Tests.Implementations;

namespace SenseNet.SearchImpl.Tests
{
    [TestClass]
    public class SnSarchToLucene29CompilerTests
    {
        [TestMethod]
        public void SnSarchToLucene29_Tests1()
        {
            var queryText = "asdf";
            var expected = "_Text:asdf";

            StorageContext.Search.ContentRepository =
                new TestSearchEngineSupport(new Dictionary<string, IPerFieldIndexingInfo>
                {
                    {"_Text", new TestPerfieldIndexingInfo_string()},
                });
            

            var queryContext = new TestQueryContext(QuerySettings.Default, 0, null);
            var parser = new CqlParser();
            var snQuery = parser.Parse(queryText, queryContext);

            var compiler = new Lucene29Compiler();
            var lucQuery = compiler.Compile(snQuery);

            var lqVisitor = new LucQueryToStringVisitor();
            lqVisitor.Visit(lucQuery.Query);
            var actual = lqVisitor.ToString();

            Assert.AreEqual(expected, actual);
        }
    }
}
