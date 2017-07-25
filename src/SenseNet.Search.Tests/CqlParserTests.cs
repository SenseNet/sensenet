using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Search.Parser;

namespace SenseNet.Search.Tests
{
    [TestClass]
    public class CqlParserTests
    {
        [TestMethod]
        public void CqlParser_1()
        {
            var queryContext = new TestQueryContext(QuerySettings.AdminSettings, 0, null);
            var parser = new CqlParser();
            var snQuery = parser.Parse("asdf", queryContext);
            Assert.Inconclusive();
        }
    }
}
