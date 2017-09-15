using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Search.Parser;
using SenseNet.Search.Tests.Implementations;

namespace SenseNet.Search.Tests
{
    [TestClass]
    public class SnQueryTests
    {
        [TestMethod, TestCategory("IR")]
        public void SnQuery_Result_Int()
        {
            var intResults = new Dictionary<string, IQueryResult<int>> { { "asdf", new QueryResult<int>(new[] { 1, 2, 3 }, 4) } };
            var context = new TestQueryContext(QuerySettings.AdminSettings, 0, null, new TestQueryEngine(intResults, null));
            var queryText = "asdf";

            var result = SnQuery.Query(queryText, context);

            var expected = string.Join(", ", intResults[queryText].Hits.Select(x => x.ToString()).ToArray());
            var actual = string.Join(", ", result.Hits.Select(x => x.ToString()).ToArray());
            Assert.AreEqual(actual, expected);
            Assert.AreEqual(result.TotalCount, intResults[queryText].TotalCount);
        }

        [TestMethod, TestCategory("IR")]
        public void SnQuery_Result_String()
        {
            var stringResults = new Dictionary<string, IQueryResult<string>>{ {"asdf", new QueryResult<string>(new[] {"1", "2", "3"}, 4)} };
            var context = new TestQueryContext(QuerySettings.AdminSettings, 0, null, new TestQueryEngine(null, stringResults));
            var queryText = "asdf";

            var result = SnQuery.QueryAndProject(queryText, context);

            var expected = string.Join(", ", stringResults[queryText].Hits);
            var actual = string.Join(", ", result.Hits);
            Assert.AreEqual(actual, expected);
            Assert.AreEqual(result.TotalCount, stringResults[queryText].TotalCount);
        }

        [TestMethod, TestCategory("IR")]
        public void SnQuery_PrepareFilters_DisabledDisabled()
        {
            var queryText = "Name:MyDocument.doc";
            var result = CreateQueryAndPrepare(queryText, FilterStatus.Disabled, FilterStatus.Disabled);
            var query = result.Item1;
            var queryTextAfter = result.Item2;

            Assert.IsTrue(query.FiltersPrepared);
            Assert.AreEqual(queryText, queryTextAfter);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_PrepareFilters_DisabledEnabled()
        {
            var queryText = "Name:MyDocument.doc";

            var result = CreateQueryAndPrepare(queryText, FilterStatus.Disabled, FilterStatus.Enabled);
            var query = result.Item1;
            var queryTextAfter = result.Item2;

            Assert.IsTrue(query.FiltersPrepared);

            // get times from the query
            var regex = new Regex("\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}");
            var matches = regex.Matches(queryTextAfter);

            // check that the query contains 3 time and the first 2 are equal. 
            Assert.AreEqual(3, matches.Count);
            Assert.AreEqual(matches[0].Value, matches[1].Value);

            // check query time is close to now
            var queryTimeString = matches[0].Value;
            var queryTime = DateTime.Parse(queryTimeString);
            Assert.IsTrue(DateTime.Now - queryTime < TimeSpan.FromSeconds(10));

            // check global format of the query
            var expectedQueryText = $"+Name:MyDocument.doc +(EnableLifespan:no (+ValidFrom:<'{queryTimeString}' +(ValidTill:>'{queryTimeString}' ValidTill:'0001-01-01 00:00:00')))";
            Assert.AreEqual(expectedQueryText, queryTextAfter);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_PrepareFilters_EnabledDisabled()
        {
            var queryText = "Name:MyDocument.doc";

            var result = CreateQueryAndPrepare(queryText, FilterStatus.Enabled, FilterStatus.Disabled);
            var query = result.Item1;
            var queryTextAfter = result.Item2;

            Assert.IsTrue(query.FiltersPrepared);

            var expectedQueryText = "+Name:MyDocument.doc +IsSystemContent:no";
            Assert.AreEqual(expectedQueryText, queryTextAfter);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_PrepareFilters_EnabledEnabled()
        {
            var queryText = "Name:MyDocument.doc";

            var result = CreateQueryAndPrepare(queryText, FilterStatus.Enabled, FilterStatus.Enabled);
            var query = result.Item1;
            var queryTextAfter = result.Item2;

            Assert.IsTrue(query.FiltersPrepared);

            // get times from the query
            var regex = new Regex("\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}");
            var matches = regex.Matches(queryTextAfter);

            // check that the query contains 3 time and the first 2 are equal. 
            Assert.AreEqual(3, matches.Count);
            Assert.AreEqual(matches[0].Value, matches[1].Value);

            // check query time is close to now
            var queryTimeString = matches[0].Value;
            var queryTime = DateTime.Parse(queryTimeString);
            Assert.IsTrue(DateTime.Now-queryTime < TimeSpan.FromSeconds(10));

            // check global format of the query
            var expectedQueryText = $"+Name:MyDocument.doc +IsSystemContent:no +(EnableLifespan:no (+ValidFrom:<'{queryTimeString}' +(ValidTill:>'{queryTimeString}' ValidTill:'0001-01-01 00:00:00')))";
            Assert.AreEqual(expectedQueryText, queryTextAfter);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_PrepareFilters_DefaultDefault()

        {
            var queryText = "Name:MyDocument.doc";

            var result = CreateQueryAndPrepare(queryText, FilterStatus.Enabled, FilterStatus.Enabled);
            var query = result.Item1;
            var queryTextAfter = result.Item2;

            Assert.IsTrue(query.FiltersPrepared);

            // get times from the query
            var regex = new Regex("\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}");
            var matches = regex.Matches(queryTextAfter);

            // check that the query contains 3 time and the first 2 are equal. 
            Assert.AreEqual(3, matches.Count);
            Assert.AreEqual(matches[0].Value, matches[1].Value);

            // check query time is close to now
            var queryTimeString = matches[0].Value;
            var queryTime = DateTime.Parse(queryTimeString);
            Assert.IsTrue(DateTime.Now - queryTime < TimeSpan.FromSeconds(10));

            // check global format of the query
            var expectedQueryText = $"+Name:MyDocument.doc +IsSystemContent:no +(EnableLifespan:no (+ValidFrom:<'{queryTimeString}' +(ValidTill:>'{queryTimeString}' ValidTill:'0001-01-01 00:00:00')))";
            Assert.AreEqual(expectedQueryText, queryTextAfter);
        }

        private Tuple<SnQuery, string> CreateQueryAndPrepare(string queryText, FilterStatus autoFilters, FilterStatus lifespanFilter)
        {
            var parser = new CqlParser();
            var query = new SnQuery
            {
                Querytext = queryText,
                QueryTree = parser.Parse(queryText),
                EnableAutofilters = autoFilters,
                EnableLifespanFilter = lifespanFilter
            };
            SnQuery.PrepareQuery(query);

            var visitor = new SnQueryToStringVisitor();
            visitor.Visit(query.QueryTree);

            return new Tuple<SnQuery, string>(query, visitor.Output);
        }
    }
}
