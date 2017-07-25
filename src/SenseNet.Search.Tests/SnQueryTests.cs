using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Search.Parser;

namespace SenseNet.Search.Tests
{
    [TestClass]
    public class SnQueryTests
    {
        #region INFRASTRUCTURE

        private class TestQueryContext : IQueryContext
        {
            private IDictionary<string, IPerFieldIndexingInfo> _indexingInfo;

            public QuerySettings Settings { get; }
            public int UserId { get; }
            public IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
            {
                return _indexingInfo[fieldName];
            }

            public TestQueryContext(QuerySettings settings, int userId, IDictionary<string, IPerFieldIndexingInfo> indexingInfo)
            {
                Settings = settings;
                UserId = userId;
                _indexingInfo = indexingInfo;
            }
        }

        private class TestQueryEngineSelector : IQueryEngineSelector
        {
            public TestQueryEngine QueryEngine { get; set; }
            public IQueryEngine Select(SnQuery query, QuerySettings settings)
            {
                return QueryEngine;
            }
        }

        private class TestQueryEngine : IQueryEngine
        {
            private readonly IDictionary<string, IQueryResult<int>> _intResults;
            private readonly IDictionary<string, IQueryResult<string>> _stringResults;

            public TestQueryEngine(IDictionary<string, IQueryResult<int>> intResults, IDictionary<string, IQueryResult<string>> stringResults)
            {
                _intResults = intResults;
                _stringResults = stringResults;
            }

            public IQueryResult<int> ExecuteQuery(SnQuery query, IPermissionFilter filter)
            {
                IQueryResult<int> result;
                if (_intResults.TryGetValue(query.Querytext, out result))
                    return result;
                return QueryResult<int>.Empty;
            }

            public IQueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter permissionFilter)
            {
                return _stringResults[query.Querytext];
            }
        }

        private class SnQueryLegoBricks
        {
            public IPermissionFilterFactory PermissionFilterFactory;
            public IQueryEngineSelector QueryEngineSelector;
            public IQueryParserFactory QueryParserFactory;
        }

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

        #endregion

        [TestMethod]
        public void SnQuery_IntResult()
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
        public void SnQuery_StringResult()
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

    }
}
