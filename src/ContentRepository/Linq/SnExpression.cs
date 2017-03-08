using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Search;
using Lucene.Net.Search;
using Lucene.Net.Index;
using System.Collections;
using System.Linq.Expressions;

namespace SenseNet.ContentRepository.Linq
{
    public class SnExpression
    {
        public static LucQuery BuildQuery(System.Linq.Expressions.Expression expression, Type sourceCollectionItemType, string contextPath, QuerySettings settings)
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
        public static LucQuery BuildQuery(System.Linq.Expressions.Expression expression, Type sourceCollectionItemType, string contextPath, ChildrenDefinition childrenDef)
        {
            return BuildLucQuery(expression, sourceCollectionItemType, contextPath, childrenDef);
        }
        private static LucQuery BuildLucQuery(System.Linq.Expressions.Expression expression, Type sourceCollectionItemType, string contextPath, ChildrenDefinition childrenDef)
        {
            Query q0 = null;

            CQVisitor v = null;
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
                v = new CQVisitor();
                v.Visit(expr2);
                q0 = v.GetQuery(sourceCollectionItemType, childrenDef);
            }

            // #2 combining with additional query clause
            LucQuery lq = null;
            if (!string.IsNullOrEmpty(childrenDef.ContentQuery))
            {
                lq = LucQuery.Parse(childrenDef.ContentQuery);
                if (q0 == null)
                    q0 = lq.Query;
                else
                    q0 = CombineQueries(q0, lq.Query);
            }

            // #3 combining with context path
            if (q0 == null)
            {
                if (childrenDef != null && childrenDef.PathUsage != PathUsageMode.NotUsed && contextPath != null)
                    q0 = GetPathQuery(contextPath, childrenDef.PathUsage == PathUsageMode.InTreeAnd || childrenDef.PathUsage == PathUsageMode.InTreeOr);
            }
            else
            {
                if (childrenDef != null && childrenDef.PathUsage != PathUsageMode.NotUsed && contextPath != null)
                    q0 = CombinePathQuery(q0, contextPath, childrenDef.PathUsage);
            }

            // #4 empty query is invalid in this place
            if (q0 == null)
                throw new NotSupportedException("Cannot execute empty query. Expression: " + expression);

            var q1 = OptimizeBooleans(q0);

            // #5 configuring query by linq expression (the smallest priority)
            var query = LucQuery.Create(q1);
            if (v != null)
            {
                query.Skip = v.Skip;
                query.Top = v.Top;
                query.CountOnly = v.CountOnly;
                query.SortFields = v.SortFields.ToArray();
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
                    query.SetSort(childrenDef.Sort);
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
                if (lq.SortFields != null && lq.SortFields.Length > 0)
                    query.SortFields = lq.SortFields;
                if (lq.EnableAutofilters != FilterStatus.Default)
                    query.EnableAutofilters = lq.EnableAutofilters;
                if (lq.EnableLifespanFilter != FilterStatus.Default)
                    query.EnableLifespanFilter = lq.EnableLifespanFilter;
                if (lq.QueryExecutionMode != QueryExecutionMode.Default)
                    query.QueryExecutionMode = lq.QueryExecutionMode;
            }

            return query;
        }
        internal static Query OptimizeBooleans(Query q)
        {
            var v = new OptimizeBooleansVisitor();
            var q1 = v.Visit(q);
            var bq = q1 as BooleanQuery;
            if(bq == null)
                return q1;
            var clauses = bq.GetClauses();
            if (clauses.Length != 1)
                return bq;
            if(clauses[0].GetOccur() != BooleanClause.Occur.MUST_NOT)
                return bq;
            bq.Add(LucQuery.FullSetQuery, BooleanClause.Occur.MUST);
            return bq;
        }
        private static Query CombinePathQuery(Query q, string contextPath, PathUsageMode pathUsageMode)
        {
            if (q == null)
                return null;
            var pathQuery = GetPathQuery(contextPath, pathUsageMode == PathUsageMode.InTreeAnd || pathUsageMode == PathUsageMode.InTreeOr);
            if (pathQuery == null)
                return q;
            var occur = pathUsageMode == PathUsageMode.InFolderOr || pathUsageMode == PathUsageMode.InTreeOr
                ? BooleanClause.Occur.SHOULD
                : BooleanClause.Occur.MUST;
            var bq = new BooleanQuery();
            bq.Add(new BooleanClause(q, occur));
            bq.Add(new BooleanClause(pathQuery, occur));

            return bq;
        }
        private static Query CombineQueries(Query q1, Query q2)
        {
            var bq = new BooleanQuery();
            bq.Add(new BooleanClause(q1, BooleanClause.Occur.MUST));
            bq.Add(new BooleanClause(q2, BooleanClause.Occur.MUST));
            return bq;
        }

        internal static Query GetPathQuery(string path, bool inTree)
        {
            if (path == null)
                return null;

            SenseNet.Search.Indexing.FieldIndexHandler converter = null;
            string fieldName = null;
            if (inTree)
            {
                converter = new SenseNet.Search.Indexing.InTreeIndexHandler();
                fieldName = LucObject.FieldName.InTree;
            }
            else
            {
                converter = new SenseNet.Search.Indexing.InFolderIndexHandler();
                fieldName = LucObject.FieldName.InFolder;
            }

            var qvalue = new SenseNet.Search.Parser.QueryFieldValue(path);
            converter.ConvertToTermValue(qvalue);
            return new TermQuery(new Term(fieldName, qvalue.StringValue));
        }

        public static Expression GetCaseInsensitiveFilter(Expression expression)
        {
            var v1 = new CaseInsensitiveFilterVisitor();
            var result = v1.Visit(expression);
            return result;
        }
    }
}
