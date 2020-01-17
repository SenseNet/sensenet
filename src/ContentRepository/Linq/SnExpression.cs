using System;
using System.Linq;
using SenseNet.Search;
using System.Linq.Expressions;
using SenseNet.ContentRepository.Schema;
using SenseNet.Search.Querying;
using SenseNet.Search.Querying.Parser.Predicates;

namespace SenseNet.ContentRepository.Linq
{
    public class SnExpression
    {
        public static SnQuery BuildQuery(Expression expression, Type sourceCollectionItemType, string contextPath, QuerySettings settings)
        {
            var childrenDef = new ChildrenDefinition
            {
                PathUsage = PathUsageMode.InFolderAnd,
                Top = settings.Top,
                Skip = settings.Skip,
                Sort = settings.Sort,
                EnableAutofilters = settings.EnableAutofilters,
                EnableLifespanFilter = settings.EnableLifespanFilter
            };

            return BuildSnQuery(expression, sourceCollectionItemType, contextPath, childrenDef, out var elementSelection);
        }
        public static SnQuery BuildQuery(Expression expression, Type sourceCollectionItemType, string contextPath, ChildrenDefinition childrenDef)
        {
            return BuildSnQuery(expression, sourceCollectionItemType, contextPath, childrenDef, out var elementSelection);
        }
        public static SnQuery BuildQuery(Expression expression, Type sourceCollectionItemType, string contextPath, ChildrenDefinition childrenDef, out string elementSelection)
        {
            return BuildSnQuery(expression, sourceCollectionItemType, contextPath, childrenDef, out elementSelection);
        }
        private static SnQuery BuildSnQuery(Expression expression, Type sourceCollectionItemType, string contextPath, ChildrenDefinition childrenDef, out string elementSelection)
        {
            SnQueryPredicate q0 = null;
            elementSelection = null;

            SnLinqVisitor v = null;
            // #1 compiling linq expression
            if (expression != null)
            {
                var v1 = new SetExecVisitor();
                var expr1 = v1.Visit(expression);
                var expr2 = expr1;
                if (v1.HasParameter)
                {
                    var v2 = new ExecutorVisitor(v1.GetExpressions());
                    expr2 = v2.Visit(expr1);
                }
                v = new SnLinqVisitor();
                v.Visit(expr2);
                q0 = v.GetPredicate(sourceCollectionItemType, childrenDef);
                elementSelection = v.ElementSelection;
            }

            // #2 combining with additional query clause
            SnQuery lq = null;
            if (!string.IsNullOrEmpty(childrenDef?.ContentQuery))
            {
                var queryText = TemplateManager.Replace(typeof(ContentQueryTemplateReplacer), childrenDef.ContentQuery);

                lq = SnQuery.Parse(queryText, new SnQueryContext(QuerySettings.Default, User.Current.Id));
                q0 = q0 == null 
                    ? lq.QueryTree 
                    : CombineQueries(q0, lq.QueryTree);
            }

            // #3 combining with context path
            if (q0 == null)
            {
                if (childrenDef != null && childrenDef.PathUsage != PathUsageMode.NotUsed && contextPath != null)
                    q0 = GetPathPredicate(contextPath, childrenDef.PathUsage == PathUsageMode.InTreeAnd || childrenDef.PathUsage == PathUsageMode.InTreeOr);
            }
            else
            {
                if (childrenDef != null && childrenDef.PathUsage != PathUsageMode.NotUsed && contextPath != null)
                    q0 = CombinePathPredicate(q0, contextPath, childrenDef.PathUsage);
            }

            // #4 empty query substitution
            if (q0 == null)
                q0 = new RangePredicate(IndexFieldName.NodeId, new IndexValue(0), null, true, false);

            var q1 = OptimizeBooleans(q0);

            // #5 configuring query by linq expression (the smallest priority)
            var query = SnQuery.Create(q1);
            if (v != null)
            {
                query.Skip = v.Skip;
                query.Top = v.Top;
                query.CountOnly = v.CountOnly;
                query.Sort = v.Sort.ToArray();
                query.ThrowIfEmpty = v.ThrowIfEmpty;
                query.ExistenceOnly = v.ExistenceOnly;
            }
            // #6 configuring query by children definition
            if (childrenDef != null)
            {
                if (childrenDef.Skip > 0)
                    query.Skip = childrenDef.Skip;
                if (childrenDef.Top > 0)
                    query.Top = childrenDef.Top;
                if (childrenDef.Sort != null)
                    query.Sort = childrenDef.Sort.ToArray();
                if (childrenDef.CountAllPages != null)
                    query.CountAllPages = childrenDef.CountAllPages.Value;
                if(childrenDef.EnableAutofilters != FilterStatus.Default)
                    query.EnableAutofilters = childrenDef.EnableAutofilters;
                if (childrenDef.EnableLifespanFilter != FilterStatus.Default)
                    query.EnableLifespanFilter = childrenDef.EnableLifespanFilter;
                if (childrenDef.QueryExecutionMode != QueryExecutionMode.Default)
                    query.QueryExecutionMode = childrenDef.QueryExecutionMode;
            }

            // #7 configuring query by additional query clause (the biggest priority)
            if (lq != null)
            {
                if (lq.Skip > 0)
                    query.Skip = lq.Skip;
                if (lq.Top > 0 && lq.Top != int.MaxValue)
                    query.Top = lq.Top;
                if (lq.Sort != null && lq.Sort.Length > 0)
                    query.Sort = lq.Sort;
                if (lq.EnableAutofilters != FilterStatus.Default)
                    query.EnableAutofilters = lq.EnableAutofilters;
                if (lq.EnableLifespanFilter != FilterStatus.Default)
                    query.EnableLifespanFilter = lq.EnableLifespanFilter;
                if (lq.QueryExecutionMode != QueryExecutionMode.Default)
                    query.QueryExecutionMode = lq.QueryExecutionMode;
                if (lq.AllVersions)
                    query.AllVersions = true;
            }

            return query;
        }

        internal static SnQueryPredicate OptimizeBooleans(SnQueryPredicate predicate)
        {
            var v = new OptimizeBooleansVisitor();
            var optimizedPredicate = v.Visit(predicate);
            if (!(optimizedPredicate is LogicalPredicate logicalPredicate))
                return optimizedPredicate;
            var clauses = logicalPredicate.Clauses;
            if (clauses.Count != 1)
                return logicalPredicate;
            if (clauses[0].Occur != Occurence.MustNot)
                return logicalPredicate;
            logicalPredicate.Clauses.Add(new LogicalClause(SnQuery.FullSetPredicate, Occurence.Must));
            return logicalPredicate;
        }

        private static SnQueryPredicate CombinePathPredicate(SnQueryPredicate predicate, string contextPath, PathUsageMode pathUsageMode)
        {
            if (predicate == null)
                return null;

            var pathQuery = GetPathPredicate(contextPath, pathUsageMode == PathUsageMode.InTreeAnd || pathUsageMode == PathUsageMode.InTreeOr);
            if (pathQuery == null)
                return predicate;

            var occur = pathUsageMode == PathUsageMode.InFolderOr || pathUsageMode == PathUsageMode.InTreeOr
                ? Occurence.Should
                : Occurence.Must;

            return new LogicalPredicate(new[]
                {new LogicalClause(predicate, occur), new LogicalClause(pathQuery, occur)});
        }

        private static SnQueryPredicate CombineQueries(SnQueryPredicate p1, SnQueryPredicate p2)
        {
            return new LogicalPredicate(new[]
                {new LogicalClause(p1, Occurence.Must), new LogicalClause(p2, Occurence.Must)});
        }

        internal static SnQueryPredicate GetPathPredicate(string path, bool inTree)
        {
            if (path == null)
                return null;
            var fieldName = inTree ? IndexFieldName.InTree : IndexFieldName.InFolder;
            var converter = ContentTypeManager.GetPerFieldIndexingInfo(fieldName).IndexFieldHandler;
            return new SimplePredicate(fieldName, converter.ConvertToTermValue(path));
        }

        public static Expression GetCaseInsensitiveFilter(Expression expression)
        {
            var v1 = new CaseInsensitiveFilterVisitor();
            var result = v1.Visit(expression);
            return result;
        }

        internal static Exception CallingAsEnunerableExpectedError(string methodName, Exception innerException = null)
        {
            var message = $"Cannot resolve an expression. Use 'AsEnumerable()' method before calling the '{methodName}' method. {innerException?.Message ?? string.Empty}";
            return new NotSupportedException(message);
        }
        internal static Exception CallingAsEnunerableExpectedError(Expression expression)
        {
            return new NotSupportedException($"Cannot resolve an expression: Use 'AsEnumerable()' method before using the '{expression}' expression.");
        }

    }
}
