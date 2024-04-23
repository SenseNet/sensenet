using System;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Search.Querying;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.SqlClient;

internal class SqlQueryEngine : IMetaQueryEngine
{
    //TODO: Part of 'CQL to SQL compiler' for future use.
    [Obsolete("Use async version instead", true)]
    public QueryResult<int> TryExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context)
    {
        //var queryInfo = SnQueryClassifier.Classify(query, context.AllVersions);
        //if (SnLucToSqlCompiler.TryCompile(QueryInfo.Query.QueryTree, QueryInfo.Top, QueryInfo.Skip,
        //    QueryInfo.SortFields, QueryInfo.CountOnly, out _sqlQueryText, out _sqlParameters))

        return null; // means: cannot execute
    }

    //TODO: Part of 'CQL to SQL compiler' for future use.
    [Obsolete("Use async version instead", true)]
    public QueryResult<string> TryExecuteQueryAndProject(SnQuery query, IPermissionFilter filter,
        IQueryContext context)
    {
        return null; // means: cannot execute
    }

    public Task<QueryResult<int>> TryExecuteQueryAsync(SnQuery query, IPermissionFilter filter, IQueryContext context, CancellationToken cancel)
    {
        //var queryInfo = SnQueryClassifier.Classify(query, context.AllVersions);
        //if (SnLucToSqlCompiler.TryCompile(QueryInfo.Query.QueryTree, QueryInfo.Top, QueryInfo.Skip,
        //    QueryInfo.SortFields, QueryInfo.CountOnly, out _sqlQueryText, out _sqlParameters))

        return Task.FromResult((QueryResult<int>) null);
    }

    public Task<QueryResult<string>> TryExecuteQueryAndProjectAsync(SnQuery query, IPermissionFilter filter, IQueryContext context,
        CancellationToken cancel)
    {
        return Task.FromResult((QueryResult<string>)null);
    }
}