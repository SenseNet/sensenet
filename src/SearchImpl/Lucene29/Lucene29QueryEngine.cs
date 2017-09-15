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
            return Execute(lucQuery, filter);
        }
        public IQueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter filter, IQueryContext context)
        {
            var lucQuery = Compile(query, context);
            return ExecuteAndProject(lucQuery, filter);
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

            //UNDONE:!!!!!!!!!!!!!!!!!!!
            //result.QueryInfo = query.QueryInfo;
            
            //UNDONE:!!!!!! Set CountAllPages, FieldLevel
            //result.CountAllPages = 0;
            //result.FieldLevel = 0;

            return result;
        }
        private QueryInfo Classify(LucQuery query, IQueryContext context)
        {
            var queryInfo = QueryClassifier.Classify(query, context.AllVersions);
            return queryInfo;
        }

        private IQueryResult<int> Execute(LucQuery lucQuery, IPermissionFilter filter)
        {
            //UNDONE:!!!!!!!!!!!!!!!!!!!
            //return lucQuery.Execute(filter);
            throw new NotImplementedException(); //UNDONE:! not implemented: Execute
        }
        private IQueryResult<string> ExecuteAndProject(LucQuery lucQuery, IPermissionFilter filter)
        {
            //UNDONE:!!!!!!!!!!!!!!!!!!!
            //return lucQuery.ExecuteAndProject(filter);
            throw new NotImplementedException(); //UNDONE:! not implemented: ExecuteAndProject
        }
    }
}
