using System.Collections.Generic;

namespace SenseNet.Search.Tests.Implementations
{
    public class TestQueryEngine : IQueryEngine
    {
        private readonly IDictionary<string, IQueryResult<int>> _intResults;
        private readonly IDictionary<string, IQueryResult<string>> _stringResults;

        public TestQueryEngine(IDictionary<string, IQueryResult<int>> intResults, IDictionary<string, IQueryResult<string>> stringResults)
        {
            _intResults = intResults;
            _stringResults = stringResults;
        }

        public IQueryResult<int> ExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context)
        {
            IQueryResult<int> result;
            if (_intResults.TryGetValue(query.Querytext, out result))
                return result;
            return QueryResult<int>.Empty;
        }

        public IQueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter filter, IQueryContext context)
        {
            return _stringResults[query.Querytext];
        }
    }
}
