using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search
{
    public class QuerySettings
    {
        public int Top { get; set; }
        public int Skip { get; set; }
        public IEnumerable<SortInfo> Sort { get; set; }

        public FilterStatus EnableAutofilters { get; set; }
        public FilterStatus EnableLifespanFilter { get; set; }

        public QueryExecutionMode QueryExecutionMode { get; set; }

        public bool AllVersions { get; set; }

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

        public static QuerySettings Default
        {
            get { return new QuerySettings(); }
        }
    }
}
