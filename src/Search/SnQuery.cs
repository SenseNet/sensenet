using System;
using SenseNet.Search.Parser;

namespace SenseNet.Search
{
    public partial class SnQuery
    {
        public string Querytext { get; internal set; }
        public string Projection { get; internal set; }
        public int Top { get; internal set; }
        public int Skip { get; internal set; }
        public SortInfo[] Sort { get; internal set; }

        internal SnQueryPredicate QueryTree { get; set; }
        public FilterStatus EnableAutofilters { get; set; }
        public FilterStatus EnableLifespanFilter { get; set; }
        public bool CountOnly { get; set; }
        public QueryExecutionMode QueryExecutionMode { get; set; }
        public QueryFieldLevel FieldLevel { get; set; } //UNDONE: Set value in the query analysis project by a visitor
    }
}