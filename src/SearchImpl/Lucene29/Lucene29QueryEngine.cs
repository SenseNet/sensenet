using System;
using System.Linq;
using SenseNet.ContentRepository.Storage.Search;

namespace SenseNet.Search.Lucene29
{
    internal class Lucene29QueryEngine : IQueryEngine
    {
        public IQueryResult<int> ExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context)
        {
            var lucQuery = Compile(query, context);

            var lucQueryResult = lucQuery.Execute(filter, context);
            var hits = lucQueryResult?.Select(x => x.NodeId).ToArray() ?? new int[0];

            return new QueryResult<int>(hits, lucQuery.TotalCount);
        }

        public IQueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter filter, IQueryContext context)
        {
            var lucQuery = Compile(query, context);

            var projection = query.Projection ?? IndexFieldName.NodeId;
            var indexFieldHandler = context.GetPerFieldIndexingInfo(projection).IndexFieldHandler as IIndexValueConverter;
            var converter = indexFieldHandler == null ? DefaultConverter : indexFieldHandler.GetBack; 
            var lucQueryResult = lucQuery.Execute(filter, context);
            var hits = lucQueryResult?
                           .Select(x => x[projection, false])
                           .Where(r => !string.IsNullOrEmpty(r))
                           .Select(q => converter(q).ToString())
                           .ToArray()
                       ?? new string[0];

            return new QueryResult<string>(hits, lucQuery.TotalCount);
        }

        /* ============================================================================================= */

        private static readonly Func<string, object> DefaultConverter = s => s;

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

            return result;
        }
    }
}
