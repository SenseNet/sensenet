using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Search.Parser;
using SenseNet.Search.Tests.Implementations;

namespace SenseNet.Search.Tests
{
    [TestClass]
    public class SnQueryTests
    {
        [TestMethod]
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

        [TestMethod]
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
    }
}
