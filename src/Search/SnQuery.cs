using System.Collections.Generic;
using System.Linq;
using SenseNet.Search.Parser;

namespace SenseNet.Search
{
    public partial class SnQuery
    {
        public static readonly string EmptyText = "$##$empty$##$";
        public static readonly string EmptyInnerQueryText = "$##$emptyinnerquery$##$";
        public static readonly double DefaultSimilarity = 0.5d;
        public static readonly double DefaultFuzzyValue = 0.5d;
        public static readonly string NullReferenceValue = "null";

        public static readonly FilterStatus EnableAutofiltersDefaultValue = FilterStatus.Enabled;
        public static readonly FilterStatus EnableLifespanFilterDefaultValue = FilterStatus.Disabled;

        public string Querytext { get; internal set; }
        public string Projection { get; internal set; }
        public int Top { get; set; }
        public int Skip { get; set; }
        public SortInfo[] Sort { get; set; }
        public bool HasSort => Sort != null && Sort.Length > 0;

        public SnQueryPredicate QueryTree { get; internal set; }
        public FilterStatus EnableAutofilters { get; set; }
        public FilterStatus EnableLifespanFilter { get; set; }
        public bool CountOnly { get; set; }
        public QueryExecutionMode QueryExecutionMode { get; set; }
        public bool AllVersions { get; set; }

        public bool CountAllPages { get; set; }

        public bool ThrowIfEmpty { get; set; }
        public bool ExistenceOnly { get; set; }
    }
}