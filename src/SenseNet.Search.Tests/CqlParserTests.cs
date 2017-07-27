using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Search.Parser;
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
        public void AggregateSettingsTest()
        {
            var indexingInfo = new Dictionary<string, IPerFieldIndexingInfo>
            {
                {"Id", new TestPerfieldIndexingInfo_int() }
            };
            var settings = new List<Tuple<QuerySettings, string, int, int>>
            {
                Tuple.Create(new QuerySettings {Top = 10}, "", 10, 0),
                Tuple.Create(new QuerySettings {Top = 10}, " .TOP:0", 10, 0),
                Tuple.Create(new QuerySettings {Top = 0}, " .TOP:10", 10, 0),
                Tuple.Create(new QuerySettings {Top = 5}, " .TOP:10", 5, 0),
                Tuple.Create(new QuerySettings {Top = 10}, " .TOP:5", 5, 0),
                Tuple.Create(new QuerySettings {Skip = 0}, "", int.MaxValue, 1),
                Tuple.Create(new QuerySettings {Skip = 0}, " .SKIP:1", int.MaxValue, 1),
                Tuple.Create(new QuerySettings {Skip = 1}, " .SKIP:0", int.MaxValue, 1),
                Tuple.Create(new QuerySettings {Skip = 10}, " .SKIP:5", int.MaxValue, 10),
                Tuple.Create(new QuerySettings {Skip = 5}, " .SKIP:10", int.MaxValue, 10),
                Tuple.Create(new QuerySettings {EnableAutofilters = FilterStatus.Default}, "", int.MaxValue, 0),
                Tuple.Create(new QuerySettings {EnableAutofilters = FilterStatus.Enabled}, "", int.MaxValue, 0)
            };

            var parser = new CqlParser();
            var queryText = "+Id:<1000";
            foreach (var setting in settings)
            {
                var queryContext = new TestQueryContext(setting.Item1, 0, indexingInfo);
                var inputQueryText = queryText + setting.Item2;    
                var expectedResultText = queryText;

                var snQuery = parser.Parse(inputQueryText, queryContext);

                var visitor = new SnQueryToStringVisitor();
                visitor.Visit(snQuery.QueryTree);
                var actualResultText = visitor.Output;

                Assert.AreEqual(expectedResultText, actualResultText);
                Assert.AreEqual(setting.Item3, snQuery.Top);
                Assert.AreEqual(setting.Item4, snQuery.Skip);
            }
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
            var queryText = "+Id:<1000 +Name:Admin* +(Field1:value1 Field2:value2)";
            var expected = "Field1, Field2, Id, Name";

            var snQuery = parser.Parse(queryText, queryContext);

            var actual = string.Join(", ", snQuery.UsedFieldNames.OrderBy(x => x).ToArray());
            Assert.AreEqual(expected, actual);
        }

    }
}
