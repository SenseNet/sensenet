using System.Collections.Generic;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Tests.Implementations
{
    internal class TestQueryEngine : IQueryEngine
    {
        private readonly IDictionary<string, QueryResult<int>> _intResults;
        private readonly IDictionary<string, QueryResult<string>> _stringResults;

        public TestQueryEngine(IDictionary<string, QueryResult<int>> intResults, IDictionary<string, QueryResult<string>> stringResults)
        {
            _intResults = intResults;
            _stringResults = stringResults;
        }

        public QueryResult<int> ExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context)
        {
            if (_intResults.TryGetValue(query.Querytext, out var result))
                return result;
            return QueryResult<int>.Empty;
        }

        public QueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter filter, IQueryContext context)
        {
            return _stringResults[query.Querytext];
        }
    }
}
