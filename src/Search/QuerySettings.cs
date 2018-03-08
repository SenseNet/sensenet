using System.Collections.Generic;
using SenseNet.Search.Querying;

namespace SenseNet.Search
{
    /// <summary>
    /// Defines the query extension values.
    /// </summary>
    public class QuerySettings
    {
        /// <summary>
        /// Gets or sets the maximum count of the query result.
        /// </summary>
        public int Top { get; set; }
        /// <summary>
        /// Gets or sets the number of items that are skipped in the beginning of the result list. 
        /// </summary>
        public int Skip { get; set; }
        /// <summary>
        /// Gets or sets the sorting criterias in order of importance.
        /// </summary>
        public IEnumerable<SortInfo> Sort { get; set; }

        /// <summary>
        /// Gets or sets the value of the switch that controls the auto filtering.
        /// </summary>
        public FilterStatus EnableAutofilters { get; set; }
        /// <summary>
        /// Gets or sets the value of the switch that controls the lifespan filtering.
        /// </summary>
        public FilterStatus EnableLifespanFilter { get; set; }

        /// <summary>
        /// Gets or sets the performance option of the query executor.
        /// </summary>
        public QueryExecutionMode QueryExecutionMode { get; set; }

        /// <summary>
        /// Gets or sets the value of the switch that controls the querying of the old versions.
        /// </summary>
        public bool AllVersions { get; set; }

        /// <summary>
        /// Returns a new QuerySettings instance. Auto- and lifespan filters are off. The other values are default.
        /// This is a shotcut of the "QuerySettings for Administrators"
        /// </summary>
        public static QuerySettings AdminSettings => new QuerySettings
        {
            EnableLifespanFilter = FilterStatus.Disabled,
            EnableAutofilters = FilterStatus.Disabled
        };
        /// <summary>
        /// Returns a new QuerySettings instance. All property values are default.
        /// This is a shotcut for "QuerySettings for general purposes".
        /// </summary>
        public static QuerySettings Default => new QuerySettings();
    }
}
