using System.Collections.Generic;
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

        public SnQueryPredicate QueryTree { get; internal set; }
        public FilterStatus EnableAutofilters { get; internal set; }
        public FilterStatus EnableLifespanFilter { get; internal set; }
        public bool CountOnly { get; internal set; }
        public QueryExecutionMode QueryExecutionMode { get; internal set; }

        //UNDONE: Set FieldLevel value in the query analysis project by UsedFieldNames
        public QueryFieldLevel FieldLevel { get; internal set; }
        public List<string> UsedFieldNames { get; internal set; }


    }
}