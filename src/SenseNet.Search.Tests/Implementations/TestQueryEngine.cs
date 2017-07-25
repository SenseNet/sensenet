using System.Collections.Generic;

namespace SenseNet.Search.Tests.Implementations
{
    internal class TestQueryEngine : IQueryEngine
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
}
