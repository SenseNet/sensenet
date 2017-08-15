using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search.Parser;

namespace SenseNet.Search
{
    public partial class SnQuery
    {
        public static readonly string EmptyText = "$##$EMPTY$##$";
        public static readonly string EmptyInnerQueryText = "$##$EMPTYINNERQUERY$##$";
        public static readonly double DefaultSimilarity = 0.5d;
        public static readonly double DefaultFuzzyValue = 0.5d;
        public static readonly string NullReferenceValue = "null";

        private static IPermissionFilterFactory PermissionFilterFactory = new DefaultPermissionFilterFactory();
        private static IQueryEngineSelector QueryEngineSelector = new DefaultQueryEngineSelector();
        private static IQueryParserFactory QueryParserFactory = new DefaultQueryParserFactory();

        public static IQueryResult<int> Query(string queryText, IQueryContext context)
        {
            var query = QueryParserFactory.Create().Parse(queryText, context);
            var permissionFilter = PermissionFilterFactory.Create(context.UserId);
            var engine = QueryEngineSelector.Select(query, context.Settings);
            return engine.ExecuteQuery(query, permissionFilter);
        }
        public static IQueryResult<string> QueryAndProject(string queryText, IQueryContext context)
        {
            var query = QueryParserFactory.Create().Parse(queryText, context);
            var permissionFilter = PermissionFilterFactory.Create(context.UserId);
            var engine = QueryEngineSelector.Select(query, context.Settings);
            return engine.ExecuteQueryAndProject(query, permissionFilter);
        }
    }
}
