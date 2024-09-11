using System;
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

        [Obsolete("Use async version instead", true)]
        public QueryResult<int> ExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context)
        {
            return ExecuteQueryAsync(query, filter, context, CancellationToken.None).GetAwaiter().GetResult();
        }

        [Obsolete("Use async version instead", true)]
        public QueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter filter, IQueryContext context)
        {
            return _stringResults[query.Querytext];
        }

        public Task<QueryResult<int>> ExecuteQueryAsync(SnQuery query, IPermissionFilter filter, IQueryContext context, CancellationToken cancel)
        {
            if (_intResults.TryGetValue(query.Querytext, out var result))
                return Task.FromResult(result);
            return Task.FromResult(QueryResult<int>.Empty);
        }

        public Task<QueryResult<string>> ExecuteQueryAndProjectAsync(SnQuery query, IPermissionFilter filter, IQueryContext context,
            CancellationToken cancel)
        {
            return Task.FromResult(_stringResults[query.Querytext]);
        }
    }
}
