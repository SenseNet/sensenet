using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Search.Parser;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    internal class SqlQueryEngine : IMetaQueryEngine
    {
        //UNDONE: SQL: Develop SqlQueryEngine.TryExecuteQuery
        public IQueryResult<int> TryExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context)
        {
            //var queryInfo = SnQueryClassifier.Classify(query, context.AllVersions);
            //if (SnLucToSqlCompiler.TryCompile(QueryInfo.Query.QueryTree, QueryInfo.Top, QueryInfo.Skip,
            //    QueryInfo.SortFields, QueryInfo.CountOnly, out _sqlQueryText, out _sqlParameters))

            return null; // means: cannot execute
        }

        //UNDONE: SQL: Develop SqlQueryEngine.TryExecuteQueryAndProject
        public IQueryResult<string> TryExecuteQueryAndProject(SnQuery query, IPermissionFilter filter,
            IQueryContext context)
        {
            return null; // means: cannot execute
        }

    }
}
