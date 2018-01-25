using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Search;
using SenseNet.Diagnostics;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using SenseNet.Search.Lucene29.QueryExecutors;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Lucene29
{
    public class LucQuery
    {
        public static Query FullSetQuery = NumericRangeQuery.NewIntRange("Id", 0, null, false, false); // MachAllDocsQuery in 3.0.3
        //public static readonly string NullReferenceValue = "null";

        public Query Query { get; private set; }

        public string QueryText => QueryToString(Query);

        internal LuceneSearchManager LuceneSearchManager { get; private set; }

        public SortField[] SortFields { get; set; }
        public bool HasSort => SortFields != null && SortFields.Length > 0;
        public string Projection { get; private set; }

        public bool ForceLuceneExecution { get; set; }

        [Obsolete("Use Skip instead. Be aware that StartIndex is 1-based but Skip is 0-based.")]
        public int StartIndex
        {
            get
            {
                return Skip + 1;
            }
            set
            {
                Skip = Math.Max(0, value - 1);
            }
        }
        public int Skip { get; set; }
        public int PageSize { get; set; }
        public int Top { get; set; }
        public bool CountOnly { get; set; }
        public bool CountAllPages { get; set; }
        public QueryExecutionMode QueryExecutionMode { get; set; }
        public FilterStatus EnableAutofilters { get; set; }
        public FilterStatus EnableLifespanFilter { get; set; }
        [Obsolete("Use SnQuery.EnableAutofiltersDefaultValue")]
        // ReSharper disable once InconsistentNaming
        public static readonly FilterStatus EnableAutofilters_DefaultValue = SnQuery.EnableAutofiltersDefaultValue;
        [Obsolete("Use SnQuery.EnableLifespanFilterDefaultValue")]
        // ReSharper disable once InconsistentNaming
        public static readonly FilterStatus EnableLifespanFilter_DefaultValue = SnQuery.EnableLifespanFilterDefaultValue;
        public bool ThrowIfEmpty { get; set; }  // only carries: linq visitor sets, executor reads
        public bool ExistenceOnly { get; set; } // only carries: linq visitor sets, executor reads

        public int TotalCount { get; private set; }

        private LucQuery() { }

        public static LucQuery Create(Query luceneQuery, LuceneSearchManager searchManager)
        {
            return new LucQuery
            {
                Query = luceneQuery,
                LuceneSearchManager = searchManager
            };
        }

        [Obsolete("Use Lucene29LocalQueryEngine.CreateSortField instead.", true)]
        public static SortField CreateSortField(string fieldName, bool reverse)
        {
            // CreateSortField has been moved up to the query engine.
            throw new NotSupportedException();
        }

        [Obsolete("Use SearchManager.IsAutofilterEnabled", true)]
        public static bool IsAutofilterEnabled(FilterStatus value)
        {
            throw new InvalidOperationException();
        }
        [Obsolete("Use SearchManager.IsLifespanFilterEnabled", true)]
        public static bool IsLifespanFilterEnabled(FilterStatus value)
        {
            throw new InvalidOperationException();
        }

        // ========================================================================================

        public IEnumerable<LucObject> Execute(IPermissionFilter filter, IQueryContext context)
        {
            using (var op = SnTrace.Query.StartOperation("LucQuery: {0}", this))
            {
                IQueryExecutor executor;

                if (CountOnly)
                    executor = new QueryExecutor20131012CountOnly();
                else
                    executor = new QueryExecutor20131012();

                executor.Initialize(this, filter);

                var result = executor.Execute();
                TotalCount = executor.TotalCount;

                SnTrace.Query.Write("LucQuery.Execute total count: {0}", TotalCount);

                op.Successful = true;
                return result ?? new LucObject[0];
            }
        }

        //TODO: Part of 'CQL to SQL compiler' for future use.
        public enum ContentQueryExecutionAlgorithm { Default, Provider, LuceneOnly, Validation }

        //TODO: Part of 'CQL to SQL compiler' for future use.
        protected void AssertResultsAreEqual(IEnumerable<LucObject> expected, IEnumerable<LucObject> actual, string cql, string sql)
        {
            var exp = string.Join(",", expected.Select(x => x.NodeId).Distinct().OrderBy(y => y));
            var act = string.Join(",", actual.Select(x => x.NodeId).OrderBy(y => y));
            if (exp != act)
            {
                var msg = string.Format("VALIDATION: Results are different. Expected:{0}, actual:{1}, CQL:{2}, SQL:{3}", exp, act, cql, sql);
                SnTrace.Test.Write(msg);
                throw new Exception(msg);
            }
        }

        public override string ToString()
        {
            var result = new StringBuilder(QueryText);
            if (CountOnly)
                result.Append(" ").Append(Cql.Keyword.CountOnly);
            if (Top != 0)
                result.Append(" ").Append(Cql.Keyword.Top).Append(":").Append(Top);
            if (Skip != 0)
                result.Append(" ").Append(Cql.Keyword.Skip).Append(":").Append(Skip);

            if (HasSort)
            {
                foreach (var sortField in SortFields)
                    if (sortField.GetReverse())
                        result.Append(" ").Append(Cql.Keyword.ReverseSort).Append(":").Append(sortField.GetField());
                    else
                        result.Append(" ").Append(Cql.Keyword.Sort).Append(":").Append(sortField.GetField());
            }

            if (EnableAutofilters != FilterStatus.Default && EnableAutofilters != SnQuery.EnableAutofiltersDefaultValue)
                result.Append(" ").Append(Cql.Keyword.Autofilters).Append(":").Append(SnQuery.EnableAutofiltersDefaultValue == FilterStatus.Enabled ? Cql.Keyword.Off : Cql.Keyword.On);
            if (EnableLifespanFilter != FilterStatus.Default && EnableLifespanFilter != SnQuery.EnableLifespanFilterDefaultValue)
                result.Append(" ").Append(Cql.Keyword.Lifespan).Append(":").Append(SnQuery.EnableLifespanFilterDefaultValue == FilterStatus.Enabled ? Cql.Keyword.Off : Cql.Keyword.On);
            if (QueryExecutionMode == QueryExecutionMode.Quick)
                result.Append(" ").Append(Cql.Keyword.Quick);

            return result.ToString();
        }
        private string QueryToString(Query query)
        {
            try
            {
                var visitor = new LucQueryToStringVisitor(LuceneSearchManager);
                visitor.Visit(query);
                return visitor.ToString();
            }
            catch (Exception e)
            {
                SnLog.WriteException(e);

                var c = query.ToString().ToCharArray();
                for (int i = 0; i < c.Length; i++)
                    if (c[i] < ' ')
                        c[i] = '.';
                return new String(c);
            }
        }

        [Obsolete("SetSort is not supported anymore.", true)]
        public void SetSort(IEnumerable<SortInfo> sort)
        {
            // SearchManager.GetPerFieldIndexingInfo is not accessible in this layer.
            throw new NotSupportedException();
        }

        public void AddAndClause(LucQuery q2)
        {
            var boolQ = new BooleanQuery();
            boolQ.Add(Query, BooleanClause.Occur.MUST);
            boolQ.Add(q2.Query, BooleanClause.Occur.MUST);
            Query = boolQ;
        }
        public void AddOrClause(LucQuery q2)
        {
            var boolQ = new BooleanQuery();
            boolQ.Add(Query, BooleanClause.Occur.SHOULD);
            boolQ.Add(q2.Query, BooleanClause.Occur.SHOULD);
            Query = boolQ;
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class SearchParams
    {
        internal int collectorSize;
        internal Searcher searcher;
        internal int numDocs;
        internal Query query;
        internal int skip;
        internal int top;
        internal int howMany;
        internal bool useHowMany;
        internal LuceneQueryExecutor executor;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class SearchResult
    {
        public static readonly SearchResult Empty;

        static SearchResult()
        {
            Empty = new SearchResult(null) { searches = 0 };
        }

        internal SearchResult(Stopwatch timer)
        {
        }

        internal List<LucObject> result;
        internal int totalCount;
        internal int nextIndex;
        internal int searches = 1;

        internal void Add(SearchResult other)
        {
            result.AddRange(other.result);
            nextIndex = other.nextIndex;
            searches += other.searches;
        }
    }
}
