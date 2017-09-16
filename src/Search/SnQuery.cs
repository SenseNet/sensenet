using System.Collections.Generic;
using System.Linq;
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

        public static readonly FilterStatus EnableAutofiltersDefaultValue = FilterStatus.Enabled;
        public static readonly FilterStatus EnableLifespanFilterDefaultValue = FilterStatus.Disabled;

        public string Querytext { get; internal set; }
        public string Projection { get; internal set; }
        public int Top { get; internal set; }
        public int Skip { get; internal set; }
        public SortInfo[] Sort { get; internal set; }

        public SnQueryPredicate QueryTree { get; internal set; }
        public FilterStatus EnableAutofilters { get; internal set; }
        public FilterStatus EnableLifespanFilter { get; internal set; }
        public bool CountOnly { get; internal set; }
        public QueryExecutionMode QueryExecutionMode { get; internal set; }

        public bool CountAllPages { get; set; }

        public SnQueryInfo QueryInfo { get; internal set; }

        //public List<string> QueryFieldNames { get; internal set; }
        //public string[] SortFieldNames
        //{
        //    get { return Sort?.Select(s => s.FieldName).Distinct().ToArray() ?? new string[0]; }
        //}
    }
}