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
        public void Search_Query_IntResult()
        {
            var intResults = new Dictionary<string, IQueryResult<int>> { { "asdf", new QueryResult<int>(new[] { 1, 2, 3 }, 4) } };
            var queryEngine = new TestQueryEngineSelector { QueryEngine = new TestQueryEngine(intResults, null) };
            var backup = SetupSnQuery(new SnQueryLegoBricks { QueryEngineSelector = queryEngine });
            var context = new TestQueryContext(QuerySettings.AdminSettings, 0, null);
            var queryText = "asdf";

            try
            {
                var result = SnQuery.Query(queryText, context);

                var expected = string.Join(", ", intResults[queryText].Hits.Select(x => x.ToString()).ToArray());
                var actual = string.Join(", ", result.Hits.Select(x => x.ToString()).ToArray());
                Assert.AreEqual(actual, expected);
                Assert.AreEqual(result.TotalCount, intResults[queryText].TotalCount);
            }
            finally
            {
                SetupSnQuery(backup);
            }
        }
        [TestMethod]
        public void Search_Query_StringResult()
        {
            var stringResults = new Dictionary<string, IQueryResult<string>> { { "asdf", new QueryResult<string>(new[] { "1", "2", "3" }, 4) } };
            var queryEngine = new TestQueryEngineSelector { QueryEngine = new TestQueryEngine(null, stringResults) };
            var backup = SetupSnQuery(new SnQueryLegoBricks { QueryEngineSelector = queryEngine });
            var context = new TestQueryContext(QuerySettings.AdminSettings, 0, null);
            var queryText = "asdf";

            try
            {
                var result = SnQuery.QueryAndProject(queryText, context);

                var expected = string.Join(", ", stringResults[queryText].Hits);
                var actual = string.Join(", ", result.Hits);
                Assert.AreEqual(actual, expected);
                Assert.AreEqual(result.TotalCount, stringResults[queryText].TotalCount);
            }
            finally
            {
                SetupSnQuery(backup);
            }
        }

        /* ====================================================================================== */

        private SnQueryLegoBricks SetupSnQuery(SnQueryLegoBricks bricks)
        {
            var backup = new SnQueryLegoBricks();
            var snQueryAcc = new PrivateType(typeof(SnQuery));

            if (bricks.QueryEngineSelector != null)
            {
                backup.QueryEngineSelector = (IQueryEngineSelector)snQueryAcc.GetStaticFieldOrProperty("QueryEngineSelector");
                snQueryAcc.SetStaticFieldOrProperty("QueryEngineSelector", bricks.QueryEngineSelector);
            }
            if (bricks.PermissionFilterFactory != null)
            {
                backup.PermissionFilterFactory = (IPermissionFilterFactory)snQueryAcc.GetStaticFieldOrProperty("PermissionFilterFactory");
                snQueryAcc.SetStaticFieldOrProperty("PermissionFilterFactory", bricks.PermissionFilterFactory);
            }
            if (bricks.QueryParserFactory != null)
            {
                backup.QueryParserFactory = (IQueryParserFactory)snQueryAcc.GetStaticFieldOrProperty("QueryParserFactory");
                snQueryAcc.SetStaticFieldOrProperty("QueryParserFactory", bricks.QueryParserFactory);
            }

            return backup;
        }

    }
}
