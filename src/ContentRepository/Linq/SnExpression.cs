using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Search;
using System.Collections;
using System.Linq.Expressions;
using SenseNet.Search.Lucene29;
using SenseNet.Search.Parser;
using SenseNet.Search.Parser.Predicates;

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

            return BuildLucQuery(expression, sourceCollectionItemType, contextPath, childrenDef);
        }
        public static SnQuery BuildQuery(Expression expression, Type sourceCollectionItemType, string contextPath, ChildrenDefinition childrenDef)
        {
            return BuildLucQuery(expression, sourceCollectionItemType, contextPath, childrenDef);
        }
        private static SnQuery BuildLucQuery(Expression expression, Type sourceCollectionItemType, string contextPath, ChildrenDefinition childrenDef)
        {
            SnQueryPredicate q0 = null;

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
                q0 = v.GetQuery(sourceCollectionItemType, childrenDef);
            }

            // #2 combining with additional query clause
            SnQuery lq = null;
            if (!string.IsNullOrEmpty(childrenDef.ContentQuery))
            {
                lq = SnQuery.Parse(childrenDef.ContentQuery);
                if (q0 == null)
                    q0 = lq.QueryTree;
                else
                    q0 = CombineQueries(q0, lq.QueryTree);
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

            // #4 empty query is invalid in this place
            if (q0 == null)
                throw new NotSupportedException("Cannot execute empty query. Expression: " + expression);

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
                if (lq.Top > 0)
                    query.Top = lq.Top;
                if (lq.Sort != null && lq.Sort.Length > 0)
                    query.Sort = lq.Sort;
                if (lq.EnableAutofilters != FilterStatus.Default)
                    query.EnableAutofilters = lq.EnableAutofilters;
                if (lq.EnableLifespanFilter != FilterStatus.Default)
                    query.EnableLifespanFilter = lq.EnableLifespanFilter;
                if (lq.QueryExecutionMode != QueryExecutionMode.Default)
                    query.QueryExecutionMode = lq.QueryExecutionMode;
            }

            return query;
        }

        internal static SnQueryPredicate OptimizeBooleans(SnQueryPredicate predicate)
        {
            var v = new OptimizeBooleansVisitor();
            var optimizedPredicate = v.Visit(predicate);
            var logicalPredicate = optimizedPredicate as LogicalPredicate;
            if (logicalPredicate == null)
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
            return new TextPredicate(inTree ? IndexFieldName.InTree : IndexFieldName.InFolder, path);
        }

        public static Expression GetCaseInsensitiveFilter(Expression expression)
        {
            var v1 = new CaseInsensitiveFilterVisitor();
            var result = v1.Visit(expression);
            return result;
        }
    }
}
