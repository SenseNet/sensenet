using System;
using System.Linq;
using Lucene.Net.Search;
using SenseNet.Search.Parser;

namespace SenseNet.Search.Lucene29
{
    internal class Lucene29QueryEngine : IQueryEngine
    {
        public IQueryResult<int> ExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context)
        {
            var lucQuery = Compile(query, context);

            //UNDONE:!!!!!!!!!!!!! Classify query
            //return lucQuery.Execute(filter);

            throw new NotImplementedException(); //UNDONE:! not implemented: Execute
        }

        public IQueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter filter, IQueryContext context)
        {
            var lucQuery = Compile(query, context);

            //UNDONE:!!!!!!!!!!!!! Classify query
            //return lucQuery.ExecuteAndProject(filter);

            throw new NotImplementedException(); //UNDONE:! not implemented: ExecuteAndProject
        }

        /* ============================================================================================= */

        private LucQuery Compile(SnQuery query, IQueryContext context)
        {
            var analyzer = Lucene29IndexingEngine.GetAnalyzer();
            var visitor = new SnQueryToLucQueryVisitor(analyzer, context);
            visitor.Visit(query.QueryTree);

            var result = LucQuery.Create(visitor.Result);
            result.Skip = query.Skip;
            result.Top = query.Top;
            result.SortFields = query.Sort.Select(s => LucQuery.CreateSortField(s.FieldName, s.Reverse)).ToArray();
            result.EnableAutofilters = query.EnableAutofilters;
            result.EnableLifespanFilter = query.EnableLifespanFilter;
            result.QueryExecutionMode = query.QueryExecutionMode;
            result.CountOnly = query.CountOnly;
            result.CountAllPages = query.CountAllPages;
            result.QueryInfo = query.QueryInfo;

            //UNDONE:!!!!!! FieldLevel
            //result.FieldLevel = ;

            return result;
        }
    }
}
