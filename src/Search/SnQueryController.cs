using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search.Parser;
using SenseNet.Search.Parser.Predicates;

namespace SenseNet.Search
{
    public partial class SnQuery
    {
        private static IPermissionFilterFactory PermissionFilterFactory = new DefaultPermissionFilterFactory();

        internal bool FiltersPrepared { get; private set; }

        public static IQueryResult<int> Query(string queryText, IQueryContext context)
        {
            var query = new CqlParser().Parse(queryText, context);
            var permissionFilter = PermissionFilterFactory.Create(context.UserId);
            PrepareQuery(query);
            var engine = SelectQueryEngine(query, context);
            return engine.ExecuteQuery(query, permissionFilter, context);
        }
        public static IQueryResult<string> QueryAndProject(string queryText, IQueryContext context)
        {
            var query = new CqlParser().Parse(queryText, context);
            var permissionFilter = PermissionFilterFactory.Create(context.UserId);
            PrepareQuery(query);
            var engine = SelectQueryEngine(query, context);
            return engine.ExecuteQueryAndProject(query, permissionFilter, context);
        }

        internal static void PrepareQuery(SnQuery query)
        {
            if (query.FiltersPrepared)
                return;

            var autoFiltersEnabled = IsAutofilterEnabled(query.EnableAutofilters);
            var lifespanFiltersEnabled = IsLifespanFilterEnabled(query.EnableLifespanFilter);

            if (autoFiltersEnabled || lifespanFiltersEnabled)
            {
                var topLevelPredicate = new LogicalPredicate();
                topLevelPredicate.Clauses.Add(new LogicalClause(query.QueryTree, Occurence.Must));

                if (autoFiltersEnabled)
                    topLevelPredicate.Clauses.Add(new LogicalClause(GetAutoFilterClause(), Occurence.Must));
                if (lifespanFiltersEnabled)
                    topLevelPredicate.Clauses.Add(new LogicalClause(GetLifespanFilterClause(), Occurence.Must));

                query.QueryTree = topLevelPredicate;
            }

            query.FiltersPrepared = true;
        }

        private static bool IsAutofilterEnabled(FilterStatus value)
        {
            switch (value)
            {
                case FilterStatus.Default:
                    return EnableAutofiltersDefaultValue == FilterStatus.Enabled;
                case FilterStatus.Enabled:
                    return true;
                case FilterStatus.Disabled:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
        private static bool IsLifespanFilterEnabled(FilterStatus value)
        {
            switch (value)
            {
                case FilterStatus.Default:
                    return EnableLifespanFilterDefaultValue == FilterStatus.Enabled;
                case FilterStatus.Enabled:
                    return true;
                case FilterStatus.Disabled:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        private static SnQueryPredicate _autoFilterClause;
        private static SnQueryPredicate GetAutoFilterClause()
        {
            if (_autoFilterClause == null)
            {
                var parser = new CqlParser();
                _autoFilterClause = parser.Parse("IsSystemContent:no");
            }
            return _autoFilterClause;
        }
        private static SnQueryPredicate GetLifespanFilterClause()
        {
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var parser = new CqlParser();
            var clause = parser.Parse($"EnableLifespan:no OR (+ValidFrom:<'{now}' +(ValidTill:>'{now}' ValidTill:'0001-01-01 00:00:00'))");
            return clause;
        }

        internal static IQueryEngine SelectQueryEngine(SnQuery query, IQueryContext context)
        {
            var queryInfo = SnQueryClassifier.Classify(query, context.AllVersions);
            query.QueryInfo = queryInfo;

            //UNDONE:!!!!!!!!!!!!! Choice query engine: SQL or configured

            return context.QueryEngine;
        }
    }
}
