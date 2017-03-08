using System.Collections.Generic;
using System.Diagnostics;

namespace SenseNet.Search
{
    public enum FilterStatus
    {
        Default,
        Enabled,
        Disabled
    }

    public enum QueryExecutionMode
    {
        Default,
        Strict,
        Quick
    }

    [DebuggerDisplay("{ToString()}")]
    public class SortInfo
    {
        public string FieldName { get; set; }
        public bool Reverse { get; set; }

        public override string  ToString()
        {
            return string.Format("{0} {1}", FieldName, Reverse ? "DESC" : "ASC");
        }
    }
    public class QuerySettings
    {
        public int Top { get; set; }
        public int Skip { get; set; }
        public IEnumerable<SortInfo> Sort { get; set; }

        public FilterStatus EnableAutofilters { get; set; }
        public FilterStatus EnableLifespanFilter { get; set; }

        public QueryExecutionMode QueryExecutionMode { get; set; }

        public static QuerySettings AdminSettings
        {
            get
            {
                return new QuerySettings
                {
                    EnableLifespanFilter = FilterStatus.Disabled,
                    EnableAutofilters = FilterStatus.Disabled
                };
            }
        }
    }
}
