using System;
using SenseNet.Search.Parser;
using SenseNet.Search.Parser.Predicates;

namespace SenseNet.Search
{
    public partial class SnQuery
    {
        private static IPermissionFilterFactory _permissionFilterFactory;
        public static void SetPermissionFilterFactory(IPermissionFilterFactory factory)
        {
            _permissionFilterFactory = factory;
        }

        internal bool FiltersPrepared { get; private set; }

        public static IQueryResult<int> Query(string queryText, IQueryContext context)
        {
            var query = new CqlParser().Parse(queryText, context);
            var permissionFilter = _permissionFilterFactory.Create(query, context);
            PrepareQuery(query);
            //UNDONE: SQL: ContentQueryExecutionAlgorithm
            return TryExecuteQuery(query, permissionFilter, context)
                   ?? context.QueryEngine.ExecuteQuery(query, permissionFilter, context);
        }
        public static IQueryResult<string> QueryAndProject(string queryText, IQueryContext context)
        {
            var query = new CqlParser().Parse(queryText, context);
            var permissionFilter = _permissionFilterFactory.Create(query, context);
            PrepareQuery(query);
            //UNDONE: SQL: ContentQueryExecutionAlgorithm
            return TryExecuteQueryAndProject(query, permissionFilter, context)
                   ?? context.QueryEngine.ExecuteQueryAndProject(query, permissionFilter, context);
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

        private static IQueryResult<int> TryExecuteQuery(SnQuery query, IPermissionFilter permissionFilter, IQueryContext context)
        {
            try
            {
                return context.MetaQueryEngine?.TryExecuteQuery(query, permissionFilter, context);
            }
            catch
            {
                return null;
            }
        }
        private static IQueryResult<string> TryExecuteQueryAndProject(SnQuery query, IPermissionFilter permissionFilter, IQueryContext context)
        {
            try
            {
                return context.MetaQueryEngine?.TryExecuteQueryAndProject(query, permissionFilter, context);
            }
            catch
            {
                return null;
            }
        }

        //UNDONE: SQL: Develop query validation
        //private static void ValidateQuery<T>(IQueryResult<T> x, IQueryResult<T> y)
        //{
        //    executor = SearchProvider.GetExecutor(this);
        //    executor.Initialize(this, permissionChecker);
        //    result = Execute(executor);
        //    if (!(executor is LuceneQueryExecutor))
        //    {
        //        var fallbackExecutor = SearchProvider.GetFallbackExecutor(this);
        //        fallbackExecutor.Initialize(this, permissionChecker);
        //        var expectedResult = Execute(fallbackExecutor);
        //        AssertResultsAreEqual(expectedResult, result, fallbackExecutor.QueryString, executor.QueryString);
        //    }
        //}
        //protected void AssertResultsAreEqual(IEnumerable<LucObject> expected, IEnumerable<LucObject> actual, string cql, string sql)
        //{
        //    var exp = string.Join(",", expected.Select(x => x.NodeId).Distinct().OrderBy(y => y));
        //    var act = string.Join(",", actual.Select(x => x.NodeId).OrderBy(y => y));
        //    if (exp != act)
        //    {
        //        var msg = string.Format("VALIDATION: Results are different. Expected:{0}, actual:{1}, CQL:{2}, SQL:{3}", exp, act, cql, sql);
        //        SnTrace.Test.Write(msg);
        //        throw new Exception(msg);
        //    }
        //}

    }
}
