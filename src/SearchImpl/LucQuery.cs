using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage.Search;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using System.Collections;
using SenseNet.ContentRepository.Storage.Security;
using Lucene.Net.Index;
using Lucene.Net.Util;
using SenseNet.Diagnostics;
using System.Diagnostics;
using SenseNet.ContentRepository;
using SenseNet.Search.Parser;
using SenseNet.Search.Indexing;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Diagnostics;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search.Lucene29.QueryExecutors;

namespace SenseNet.Search
{
    public class LucQuery //UNDONE:!!! After LINQ: Delete ASAP
    {
        private static string[] _headOnlyFields = SenseNet.ContentRepository.Storage.Node.GetHeadOnlyProperties();

        public static Query FullSetQuery = NumericRangeQuery.NewIntRange("Id", 0, null, false, false); // MachAllDocsQuery in 3.0.3
        //public static readonly string NullReferenceValue = "null";

        private Query __query;
        public Query Query
        {
            get { return __query; }
            private set { __query = value; }
        }
        public string QueryText { get { return QueryToString(Query); } }

        [Obsolete("", true)]
        internal QueryFieldLevel FieldLevel { get; set; }

        public IUser User { get; set; }
        public SortField[] SortFields { get; set; }
        public bool HasSort { get { return SortFields != null && SortFields.Length > 0; } }
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
        public static readonly FilterStatus EnableAutofilters_DefaultValue = FilterStatus.Enabled;
        public static readonly FilterStatus EnableLifespanFilter_DefaultValue = FilterStatus.Disabled;
        public bool ThrowIfEmpty { get; set; }  // only carries: linq visitor sets, executor reads
        public bool ExistenceOnly { get; set; } // only carries: linq visitor sets, executor reads

        public int TotalCount { get; private set; }

        private LucQuery() { }

        private static string GetFieldNameByPropertyName(string propertyName)
        {
            if (propertyName == "NodeId") return "Id";
            return propertyName;
        }
        private static int GetSortType(string propertyName)
        {
            var x = StorageContext.Search.ContentRepository.GetPerFieldIndexingInfo(GetFieldNameByPropertyName(propertyName));
            if (x != null)
                return x.IndexFieldHandler.SortingType;
            return SortField.STRING;
        }
        public static LucQuery Create(Query luceneQuery)
        {
            return new LucQuery { Query = luceneQuery };
        }

        public static LucQuery Parse(string luceneQueryText)
        {
            var result = new LucQuery();
            var parser = new SnLucParser();
            Query query;

            var replacedText = StorageContext.Search.ContentRepository.ReplaceQueryTemplates(luceneQueryText); 
            query = parser.Parse(replacedText);

            // Run EmptyTermVisitor if the parser created empty query term.
            if (parser.ParseEmptyQuery)
            {
                var visitor = new EmptyTermVisitor();
                result.Query = visitor.Visit(query);
            }
            else
            {
                result.Query = query;
            }

            var sortFields = new List<SortField>();
            foreach (var control in parser.Controls)
            {
                switch (control.Name)
                {
                    case SnLucLexer.Keywords.Select:
                        result.Projection = control.Value;
                        break;
                    case SnLucLexer.Keywords.Top:
                        result.Top = Convert.ToInt32(control.Value);
                        break;
                    case SnLucLexer.Keywords.Skip:
                        result.Skip = Convert.ToInt32(control.Value);
                        break;
                    case SnLucLexer.Keywords.Sort:
                        sortFields.Add(CreateSortField(control.Value, false));
                        break;
                    case SnLucLexer.Keywords.ReverseSort:
                        sortFields.Add(CreateSortField(control.Value, true));
                        break;
                    case SnLucLexer.Keywords.Autofilters:
                        result.EnableAutofilters = control.Value == SnLucLexer.Keywords.On ? FilterStatus.Enabled : FilterStatus.Disabled;
                        break;
                    case SnLucLexer.Keywords.Lifespan:
                        result.EnableLifespanFilter = control.Value == SnLucLexer.Keywords.On ? FilterStatus.Enabled : FilterStatus.Disabled;
                        break;
                    case SnLucLexer.Keywords.CountOnly:
                        result.CountOnly = true;
                        break;
                    case SnLucLexer.Keywords.Quick:
                        result.QueryExecutionMode = QueryExecutionMode.Quick;
                        break;
                }
            }
            result.SortFields = sortFields.ToArray();
            //result.FieldLevel = parser.FieldLevel;
            return result;
        }
        public static SortField CreateSortField(string fieldName, bool reverse)
        {
            var info = StorageContext.Search.ContentRepository.GetPerFieldIndexingInfo(fieldName);
            var sortType = SortField.STRING;
            if (info != null)
            {
                sortType = info.IndexFieldHandler.SortingType;
                fieldName = info.IndexFieldHandler.GetSortFieldName(fieldName);
            }
            if (sortType == SortField.STRING)
                return new SortField(fieldName, System.Threading.Thread.CurrentThread.CurrentCulture, reverse);
            return new SortField(fieldName, sortType, reverse);
        }

        public static bool IsAutofilterEnabled(FilterStatus value)
        {
            switch (value)
            {
                case FilterStatus.Default:
                    return EnableAutofilters_DefaultValue == FilterStatus.Enabled;
                case FilterStatus.Enabled:
                    return true;
                case FilterStatus.Disabled:
                    return false;
                default:
                    throw new SnNotSupportedException("Unknown FilterStatus: " + value);
            }
        }
        public static bool IsLifespanFilterEnabled(FilterStatus value)
        {
            switch (value)
            {
                case FilterStatus.Default:
                    return EnableLifespanFilter_DefaultValue == FilterStatus.Enabled;
                case FilterStatus.Enabled:
                    return true;
                case FilterStatus.Disabled:
                    return false;
                default:
                    throw new SnNotSupportedException("Unknown FilterStatus: " + value);
            }
        }

        // ========================================================================================

        [Obsolete("", false)] //UNDONE:!!! LINQ: [Obsolete("", true)]
        public IEnumerable<LucObject> Execute()
        {
            return Execute(false);
        }
        [Obsolete("", true)]
        public IEnumerable<LucObject> Execute(bool allVersions)
        {
            using (var op = SnTrace.Query.StartOperation("LucQuery: {0}", this))
            {
                //if (FieldLevel == QueryFieldLevel.NotDefined)
                //    FieldLevel = GetFieldLevel();
                //var permissionChecker = new PermissionChecker(this.User ?? AccessProvider.Current.GetCurrentUser(), FieldLevel, allVersions);

                IEnumerable<LucObject> result = null;
                IQueryExecutor executor = null;
                if (this.CountOnly)
                    executor = new QueryExecutor20131012CountOnly();
                else
                    executor = new QueryExecutor20131012();

                //UNDONE: SQL: ContentQueryExecutionAlgorithm
                var executionAlgorithm = ForceLuceneExecution
                    ? ContentQueryExecutionAlgorithm.LuceneOnly
                    : Configuration.Querying.ContentQueryExecutionAlgorithm;

                //executor.Initialize(this, permissionChecker);
                result = executor.Execute();

                op.Successful = true;
                return result;
            }
        }

        public IEnumerable<LucObject> Execute(IPermissionFilter filter, IQueryContext context)
        {
            using (var op = SnTrace.Query.StartOperation("LucQuery: {0}", this))
            {
                IEnumerable<LucObject> result = null;
                IQueryExecutor executor = null;
                if (this.CountOnly)
                    executor = new QueryExecutor20131012CountOnly();
                else
                    executor = new QueryExecutor20131012();

                executor.Initialize(this, filter);
                result = executor.Execute();
                TotalCount = executor.TotalCount;
                SnTrace.Query.Write("LucQuery.Execute total count: {0}", TotalCount);

                op.Successful = true;
                return result ?? new LucObject[0];
            }
        }

        //UNDONE: SQL: ContentQueryExecutionAlgorithm
        public enum ContentQueryExecutionAlgorithm { Default, Provider, LuceneOnly, Validation }

        //UNDONE: SQL: ContentQueryExecutionAlgorithm
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

        private QueryFieldLevel GetFieldLevel()
        {
            var v = new FieldNameVisitor();
            v.Visit(this.Query);
            return GetFieldLevel(v.FieldNames);
        }
        internal static QueryFieldLevel GetFieldLevel(IEnumerable<string> fieldNames)
        {
            var fieldLevel = QueryFieldLevel.NotDefined;
            foreach (var fieldName in fieldNames)
            {
                var indexingInfo = StorageContext.Search.ContentRepository.GetPerFieldIndexingInfo(fieldName);
                var level = GetFieldLevel(fieldName, indexingInfo);
                fieldLevel = level > fieldLevel ? level : fieldLevel;
            }
            return fieldLevel;
        }
        internal static QueryFieldLevel GetFieldLevel(string fieldName, IPerFieldIndexingInfo indexingInfo)
        {
            QueryFieldLevel level;

            if (fieldName == IndexFieldName.AllText)
                level = QueryFieldLevel.BinaryOrFullText;
            else if (indexingInfo == null)
                level = QueryFieldLevel.BinaryOrFullText;
            else if (indexingInfo.FieldDataType == typeof(SenseNet.ContentRepository.Storage.BinaryData))
                level = QueryFieldLevel.BinaryOrFullText;
            else if (fieldName == IndexFieldName.InFolder || fieldName == IndexFieldName.InTree
                || fieldName == IndexFieldName.Type || fieldName == IndexFieldName.TypeIs
                || _headOnlyFields.Contains(fieldName))
                level = QueryFieldLevel.HeadOnly;
            else
                level = QueryFieldLevel.NoBinaryOrFullText;

            return level;
        }

        public override string ToString()
        {
            var result = new StringBuilder(QueryText);
            if (CountOnly)
                result.Append(" ").Append(SnLucLexer.Keywords.CountOnly);
            if (Top != 0)
                result.Append(" ").Append(SnLucLexer.Keywords.Top).Append(":").Append(Top);
            if (Skip != 0)
                result.Append(" ").Append(SnLucLexer.Keywords.Skip).Append(":").Append(Skip);
            if (this.HasSort)
            {
                foreach (var sortField in this.SortFields)
                    if (sortField.GetReverse())
                        result.Append(" ").Append(SnLucLexer.Keywords.ReverseSort).Append(":").Append(sortField.GetField());
                    else
                        result.Append(" ").Append(SnLucLexer.Keywords.Sort).Append(":").Append(sortField.GetField());
            }
            if (EnableAutofilters != FilterStatus.Default && EnableAutofilters != EnableAutofilters_DefaultValue)
                result.Append(" ").Append(SnLucLexer.Keywords.Autofilters).Append(":").Append(EnableAutofilters_DefaultValue == FilterStatus.Enabled ? SnLucLexer.Keywords.Off : SnLucLexer.Keywords.On);
            if (EnableLifespanFilter != FilterStatus.Default && EnableLifespanFilter != EnableLifespanFilter_DefaultValue)
                result.Append(" ").Append(SnLucLexer.Keywords.Lifespan).Append(":").Append(EnableLifespanFilter_DefaultValue == FilterStatus.Enabled ? SnLucLexer.Keywords.Off : SnLucLexer.Keywords.On);
            if (QueryExecutionMode == QueryExecutionMode.Quick)
                result.Append(" ").Append(SnLucLexer.Keywords.Quick);
            return result.ToString();
        }
        private string QueryToString(Query query)
        {
            try
            {
                var visitor = new LucQueryToStringVisitor();
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

        public void SetSort(IEnumerable<SortInfo> sort)
        {
            var sortFields = new List<SortField>();
            if (sort != null)
                foreach (var field in sort)
                    sortFields.Add(CreateSortField(field.FieldName, field.Reverse));
            this.SortFields = sortFields.ToArray();
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
