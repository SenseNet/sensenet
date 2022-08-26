using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        public Task<QueryResult<int>> ExecuteQueryAsync(SnQuery query, IPermissionFilter filter, IQueryContext context, CancellationToken cancel)
        {
            return Task.FromResult(ExecuteQuery(query, filter, context));
        }

        public Task<QueryResult<string>> ExecuteQueryAndProjectAsync(SnQuery query, IPermissionFilter filter, IQueryContext context,
            CancellationToken cancel)
        {
            return Task.FromResult(ExecuteQueryAndProject(query, filter, context));
        }
    }
}
