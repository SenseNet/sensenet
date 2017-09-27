using System;
using System.Text;
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
        public static SnQueryPredicate FullSetPredicate { get; } = new RangePredicate("Id", new IndexValue(0), null, false, false);


        public static IQueryResult<int> Query(string queryText, IQueryContext context)
        {
            var query = new CqlParser().Parse(queryText, context);
            return query.Execute(context);
        }
        public IQueryResult<int> Execute(IQueryContext context)
        {
            var permissionFilter = _permissionFilterFactory.Create(this, context);
            PrepareQuery(this, context);
            //UNDONE: SQL: ContentQueryExecutionAlgorithm
            return TryExecuteQuery(this, permissionFilter, context)
                   ?? context.QueryEngine.ExecuteQuery(this, permissionFilter, context);
        }

        public static IQueryResult<string> QueryAndProject(string queryText, IQueryContext context)
        {
            var query = new CqlParser().Parse(queryText, context);
            return query.ExecuteAndProject(context);
        }
        public IQueryResult<string> ExecuteAndProject(IQueryContext context)
        {
            var permissionFilter = _permissionFilterFactory.Create(this, context);
            PrepareQuery(this, context);
            //UNDONE: SQL: ContentQueryExecutionAlgorithm
            return TryExecuteQueryAndProject(this, permissionFilter, context)
                   ?? context.QueryEngine.ExecuteQueryAndProject(this, permissionFilter, context);
        }

        public void AddAndClause(SnQueryPredicate clause)
        {
            AddClause(clause, Occurence.Must);
        }
        public void AddOrClause(SnQueryPredicate clause)
        {
            AddClause(clause, Occurence.Should);
        }
        public void AddClause(SnQueryPredicate clause, Occurence occurence)
        {
            QueryTree = new LogicalPredicate(new[]
            {
                new LogicalClause(this.QueryTree, occurence),
                new LogicalClause(clause, occurence),
            });
        }

        internal static void PrepareQuery(SnQuery query, IQueryContext context)
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
                    topLevelPredicate.Clauses.Add(new LogicalClause(GetAutoFilterClause(context), Occurence.Must));
                if (lifespanFiltersEnabled)
                    topLevelPredicate.Clauses.Add(new LogicalClause(GetLifespanFilterClause(context), Occurence.Must));

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
        private static SnQueryPredicate GetAutoFilterClause(IQueryContext context)
        {
            if (_autoFilterClause == null)
            {
                var parser = new CqlParser();
                _autoFilterClause = parser.Parse("IsSystemContent:no", context).QueryTree;
            }
            return _autoFilterClause;
        }
        private static SnQueryPredicate GetLifespanFilterClause(IQueryContext context)
        {
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var parser = new CqlParser();
            var clause = parser.Parse($"EnableLifespan:no OR (+ValidFrom:<'{now}' +(ValidTill:>'{now}' ValidTill:'0001-01-01 00:00:00'))", context).QueryTree;
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

        public static SnQuery Parse(string queryText, IQueryContext context)
        {
            return new CqlParser().Parse(queryText, context);
        }

        public static SnQuery Create(SnQueryPredicate predicate)
        {
            return new SnQuery {QueryTree = predicate};
        }

        public override string ToString()
        {
            var visitor = new SnQueryToStringVisitor();
            visitor.Visit(this.QueryTree);
            var sb = new StringBuilder(visitor.Output);

            if (CountOnly)
                sb.Append(" ").Append(Cql.Keyword.CountOnly);
            if (Top != 0)
                sb.Append(" ").Append(Cql.Keyword.Top).Append(":").Append(Top);
            if (Skip != 0)
                sb.Append(" ").Append(Cql.Keyword.Skip).Append(":").Append(Skip);
            if (HasSort)
            {
                foreach (var sortInfo in Sort)
                    if (sortInfo.Reverse)
                        sb.Append(" ").Append(Cql.Keyword.ReverseSort).Append(":").Append(sortInfo.FieldName);
                    else
                        sb.Append(" ").Append(Cql.Keyword.Sort).Append(":").Append(sortInfo.FieldName);
            }
            if (EnableAutofilters != FilterStatus.Default && EnableAutofilters != EnableAutofiltersDefaultValue)
                sb.Append(" ").Append(Cql.Keyword.Autofilters).Append(":").Append(EnableAutofiltersDefaultValue == FilterStatus.Enabled ? Cql.Keyword.Off : Cql.Keyword.On);
            if (EnableLifespanFilter != FilterStatus.Default && EnableLifespanFilter != EnableLifespanFilterDefaultValue)
                sb.Append(" ").Append(Cql.Keyword.Lifespan).Append(":").Append(EnableLifespanFilterDefaultValue == FilterStatus.Enabled ? Cql.Keyword.Off : Cql.Keyword.On);
            if (QueryExecutionMode == QueryExecutionMode.Quick)
                sb.Append(" ").Append(Cql.Keyword.Quick);

            return sb.ToString();
        }
    }
}
