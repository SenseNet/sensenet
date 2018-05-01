using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections;
using SenseNet.Search;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search.Querying;

namespace SenseNet.ContentRepository.Linq
{
    public interface ISnQueryable<T> : IOrderedQueryable<T>
    {
        ISnQueryable<T> CountOnly();
        ISnQueryable<T> HeadersOnly();
        ISnQueryable<T> EnableAutofilters();
        ISnQueryable<T> DisableAutofilters();
        ISnQueryable<T> EnableLifespan();
        ISnQueryable<T> DisableLifespan();

        Content First();
        Content FirstOrDefault();
        Content Last();
        Content LastOrDefault();
    }
    public class ContentSet<T> : IOrderedEnumerable<T>, IQueryProvider, ISnQueryable<T>
    {
        private static StringBuilder _traceLog;
        internal static bool TracingEnabled { get; set; }
        internal static StringBuilder TraceLog => _traceLog ?? (_traceLog = new StringBuilder());
        public ChildrenDefinition ChildrenDefinition { get; }
        public string ContextPath { get; }
        private bool ExecuteQuery => ChildrenDefinition?.BaseCollection == null;

        // --------------------------------------------
        protected internal bool CountOnlyEnabled { get; protected set; }
        protected internal bool HeadersOnlyEnabled { get; protected set; }
        protected internal Type TypeFilter { get; protected set; }

        // -------------------------------------------- ISnQueryable
        public ISnQueryable<T> CountOnly()
        {
            this.CountOnlyEnabled = true;
            return this;
        }
        public ISnQueryable<T> HeadersOnly()
        {
            this.HeadersOnlyEnabled = true;
            return this;
        }
        public ISnQueryable<T> EnableAutofilters()
        {
            this.ChildrenDefinition.EnableAutofilters = FilterStatus.Enabled;
            return this;
        }
        public ISnQueryable<T> DisableAutofilters()
        {
            this.ChildrenDefinition.EnableAutofilters = FilterStatus.Disabled;
            return this;
        }
        public ISnQueryable<T> EnableLifespan()
        {
            this.ChildrenDefinition.EnableLifespanFilter = FilterStatus.Enabled;
            return this;
        }
        public ISnQueryable<T> DisableLifespan()
        {
            this.ChildrenDefinition.EnableLifespanFilter = FilterStatus.Disabled;
            return this;
        }
        public ISnQueryable<T> SetExecutionMode(QueryExecutionMode executionMode)
        {
            this.ChildrenDefinition.QueryExecutionMode = executionMode;
            return this;
        }

        public Content First()
        {
            throw new SnNotSupportedException("SnLinq: ContentSet.First");
        }
        public Content FirstOrDefault()
        {
            throw new SnNotSupportedException("SnLinq: ContentSet.FirstOrDefault");
        }
        public Content Last()
        {
            throw new SnNotSupportedException("SnLinq: ContentSet.Last");
        }
        public Content LastOrDefault()
        {
            throw new SnNotSupportedException("SnLinq: ContentSet.LastOrDefault");
        }

        // =====================================================================

        private Expression _expression;

        internal ContentSet(bool countOnly, bool headersOnly, ChildrenDefinition childrenDef, string contextPath)
        {
            CountOnlyEnabled = countOnly;
            HeadersOnlyEnabled = headersOnly;
            ChildrenDefinition = childrenDef;
            ContextPath = contextPath;
        }
        internal ContentSet(Expression expression, ChildrenDefinition childrenDef, string contextPath)
        {
            _expression = expression;
            ChildrenDefinition = childrenDef;
            ContextPath = contextPath;
        }
        internal ContentSet(ChildrenDefinition childrenDef, string contextPath)
        {
            ChildrenDefinition = childrenDef;
            ContextPath = contextPath;
        }

        internal static SnQuery GetSnQuery(Expression expr, bool autoFiltersEnabled,bool lifespanEnabled, ChildrenDefinition childrenDef, string contextPath)
        {
            return SnExpression.BuildQuery(expr, typeof(T), contextPath, childrenDef);
        }

        // ===================================================================== IEnumerable<Content> Members
        public virtual IEnumerator<T> GetEnumerator()
        {
            // in most cases we work with a content query
            if (this.ExecuteQuery)
                return new LinqContentEnumerator<T>(this);
            
            // if there is a predefined list of nodes, use its enumerator
            return typeof(T) == typeof(Content) 
                       ? ChildrenDefinition.BaseCollection.Select(Content.Create).Cast<T>().GetEnumerator()
                       : ChildrenDefinition.BaseCollection.Cast<T>().GetEnumerator();
        }

        // ===================================================================== IEnumerable Members
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // ===================================================================== IQueryable Members
        public Type ElementType => throw new SnNotSupportedException("SnLinq: ContentSet.ElementType");

        public Expression Expression => _expression ?? (_expression = Expression.Constant(this));

        public IQueryProvider Provider => this;

        // ===================================================================== IQueryProvider Members
        public virtual IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return Clone<TElement>(expression);
        }
        public virtual IQueryable CreateQuery(Expression expression)
        {
            return CreateQuery<Content>(expression);
        }

        public virtual TResult Execute<TResult>(Expression expression)
        {
            var count = 0;

            // in case there is a predefined list of nodes, we do not execute a query (but we still need to build it)
            if (!this.ExecuteQuery)
                count = ChildrenDefinition.BaseCollection.Count();

            var query = SnExpression.BuildQuery(expression, typeof(T), this.ContextPath, this.ChildrenDefinition, out var elementSelection);
            if (TracingEnabled)
            {
                TraceLog.Append("Expression: ").AppendLine(expression.ToString());
                TraceLog.Append("Query:      ").AppendLine(query.ToString());
                TraceLog.AppendLine("--------------");
            }
            var result = this.ExecuteQuery ? query.Execute(SnQueryContext.CreateDefault()) : null;
            if (query.CountOnly)
            {
                // ReSharper disable once PossibleNullReferenceException
                // Result cannot be null here because the query definitely executed.
                if (this.ExecuteQuery)
                    count = result.TotalCount;

                if (query.ExistenceOnly)
                    return (TResult)Convert.ChangeType(count > 0, typeof(TResult));
                return (TResult)Convert.ChangeType(count, typeof(TResult));
            }

            // ReSharper disable once PossibleNullReferenceException
            // Result cannot be null here because the query definitely executed.
            if (this.ExecuteQuery)
                count = result.TotalCount;

            if (count == 0)
            {
                if (query.ThrowIfEmpty)
                {
                    if (elementSelection == "elementat")
                        throw new ArgumentOutOfRangeException("Index was out of range.");
                    else
                        throw new InvalidOperationException("Sequence contains no elements.");
                }
                return default(TResult);
            }

            if (typeof(Node).IsAssignableFrom(typeof(TResult)))
            {
                // ReSharper disable once PossibleNullReferenceException
                // Result cannot be null here because the query definitely executed.
                if (ExecuteQuery)
                    return (TResult)Convert.ChangeType(Node.LoadNode(result.Hits.First()), typeof(TResult));
                return (TResult)Convert.ChangeType(ChildrenDefinition.BaseCollection.First(), typeof(TResult));
            }
            // ReSharper disable once PossibleNullReferenceException
            // Result cannot be null here because the query definitely executed.

            switch (elementSelection)
            {
                case "first":
                    return ExecuteQuery
                        ? (TResult)Convert.ChangeType(Content.Load(result.Hits.FirstOrDefault()), typeof(TResult))
                        : (TResult)Convert.ChangeType(Content.Create(ChildrenDefinition.BaseCollection.FirstOrDefault()), typeof(TResult));
                case "last":
                    return ExecuteQuery
                        ? (TResult)Convert.ChangeType(Content.Load(result.Hits.LastOrDefault()), typeof(TResult))
                        : (TResult)Convert.ChangeType(Content.Create(ChildrenDefinition.BaseCollection.LastOrDefault()), typeof(TResult));
                case "single":
                    return ExecuteQuery
                        ? (TResult)Convert.ChangeType(Content.Load(result.Hits.SingleOrDefault()), typeof(TResult))
                        : (TResult)Convert.ChangeType(Content.Create(ChildrenDefinition.BaseCollection.SingleOrDefault()), typeof(TResult));
                case "elementat":
                    var any = ExecuteQuery ? result.Hits.Any() : ChildrenDefinition.BaseCollection.Any();
                    if(!any)
                    {
                        if (query.ThrowIfEmpty)
                            throw new ArgumentOutOfRangeException("Index was out of range.");
                        else
                            return default(TResult);
                    }
                    return ExecuteQuery
                        ? (TResult)Convert.ChangeType(Content.Load(result.Hits.FirstOrDefault()), typeof(TResult))
                        : (TResult)Convert.ChangeType(Content.Create(ChildrenDefinition.BaseCollection.FirstOrDefault()), typeof(TResult));
                default:
                    //if (elementSelection.StartsWith("index"))
                    //{
                    //    var index = int.Parse(elementSelection.Substring(6));
                    //    if (index >= count)
                    //    {
                    //        if (query.ThrowIfEmpty)
                    //            throw new ArgumentOutOfRangeException("Index was out of range.");
                    //        return default(TResult);
                    //    }
                    //    if (index == 0)
                    //    {
                    //        return ExecuteQuery
                    //            ? (TResult)Convert.ChangeType(Content.Load(result.Hits.FirstOrDefault()), typeof(TResult))
                    //            : (TResult)Convert.ChangeType(Content.Create(ChildrenDefinition.BaseCollection.FirstOrDefault()), typeof(TResult));
                    //    }
                    //    return ExecuteQuery
                    //        ? (TResult)Convert.ChangeType(Content.Load(result.Hits.Skip(index - 1).First()), typeof(TResult))
                    //        : (TResult)Convert.ChangeType(Content.Create(ChildrenDefinition.BaseCollection.Skip(index - 1).First()), typeof(TResult));
                    //}
                    throw new SnNotSupportedException();
            }
        }
        public virtual object Execute(Expression expression)
        {
            throw new SnNotSupportedException("SnLinq: ContentSet.Execute");
        }

        public virtual IOrderedEnumerable<T> CreateOrderedEnumerable<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer, bool descending)
        {
            throw new SnNotSupportedException("SnLinq: ContentSet.CreateOrderedEnumerable<TKey>");
        }

        // ==================================================================================================================================================
        private ContentSet<Q> Clone<Q>(Expression expression)
        {
            if (typeof(Content) != typeof(Q))
                this.TypeFilter = typeof(Q);
            if (typeof(Q) == typeof(Content) || typeof(Node).IsAssignableFrom(typeof(Q)))
                return new ContentSet<Q>(expression, this.ChildrenDefinition.Clone(), this.ContextPath)
                {
                    CountOnlyEnabled = this.CountOnlyEnabled,
                    HeadersOnlyEnabled = this.HeadersOnlyEnabled,
                    TypeFilter = this.TypeFilter
                };
            if (expression is MethodCallExpression callExpr)
            {
                var lastMethodName = callExpr.Method.Name;
                throw SnExpression.CallingAsEnunerableExpectedError(lastMethodName);
            }
            throw SnExpression.CallingAsEnunerableExpectedError(expression);
        }

        // ==================================================================================================================================================
        public SnQuery GetCompiledQuery()
        {
            var q = SnExpression.BuildQuery(this.Expression, typeof(T), this.ContextPath, this.ChildrenDefinition);
            return q;
        }
    }
}
