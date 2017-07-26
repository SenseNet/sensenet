using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Search.Parser;
using SenseNet.Search.Parser.Predicates;
using SenseNet.Search.Tests.Implementations;

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
            var queryText = "asdf";
            var expectedResult = "_Text:asdf";

            var snQuery = parser.Parse(queryText, queryContext);

            var visitor = new SnQueryToStringVisitor();
            visitor.Visit(snQuery.QueryTree);
            var actualResult = visitor.Output;

            Assert.AreEqual(expectedResult, actualResult);
        }
        [TestMethod]
        public void CqlParser_2()
        {
            var indexingInfo = new Dictionary<string, IPerFieldIndexingInfo>
            {
                {"Id", new TestPerfieldIndexingInfo_int() },
                {"Name", new TestPerfieldIndexingInfo_string() }
            };
            var queryContext = new TestQueryContext(QuerySettings.AdminSettings, 0, indexingInfo);
            var parser = new CqlParser();
            var queryText = "+Id:<1000 +Name:Admin*";
            var expectedResult = "+Id:<1000 +Name:admin*";

            var snQuery = parser.Parse(queryText, queryContext);

            var visitor = new SnQueryToStringVisitor();
            visitor.Visit(snQuery.QueryTree);
            var actualResult = visitor.Output;

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void CqlParser_UsedFieldNames()
        {
            var indexingInfo = new Dictionary<string, IPerFieldIndexingInfo>
            {
                {"Id", new TestPerfieldIndexingInfo_int() },
                {"Name", new TestPerfieldIndexingInfo_string() },
                {"Field1", new TestPerfieldIndexingInfo_string() },
                {"Field2", new TestPerfieldIndexingInfo_string() }
            };
            var queryContext = new TestQueryContext(QuerySettings.AdminSettings, 0, indexingInfo);
            var parser = new CqlParser();
            var queryText = "+Id:<1000 +Name:Admin* +(Field1:value1 Field2:value2) +(Field1:asdf)";
            var expected = "Field1, Field2, Id, Name";

            var snQuery = parser.Parse(queryText, queryContext);

            var actual = string.Join(", ", snQuery.UsedFieldNames.OrderBy(x => x).ToArray());
            Assert.AreEqual(expected, actual);
        }

    }
}
