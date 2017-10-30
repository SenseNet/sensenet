using SenseNet.Search;
using SenseNet.Search.Querying;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    internal class SqlQueryEngine : IMetaQueryEngine
    {
        //TODO: Part of 'CQL to SQL compiler' for future use.
        public IQueryResult<int> TryExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context)
        {
            //var queryInfo = SnQueryClassifier.Classify(query, context.AllVersions);
            //if (SnLucToSqlCompiler.TryCompile(QueryInfo.Query.QueryTree, QueryInfo.Top, QueryInfo.Skip,
            //    QueryInfo.SortFields, QueryInfo.CountOnly, out _sqlQueryText, out _sqlParameters))

            return null; // means: cannot execute
        }

        //TODO: Part of 'CQL to SQL compiler' for future use.
        public IQueryResult<string> TryExecuteQueryAndProject(SnQuery query, IPermissionFilter filter,
            IQueryContext context)
        {
            return null; // means: cannot execute
        }

    }
}
