using System.Runtime.Serialization;
using SenseNet.Search.Querying.Parser.Predicates;

namespace SenseNet.Search.Querying
{
    /// <summary>
    /// Represents a parsed CQL query encapsulating all extensions.
    /// </summary>
    [DataContract]
    public partial class SnQuery
    {
        /// <summary>
        /// Constant value to represent an empty query text for internal usage.
        /// </summary>
        public static readonly string EmptyText = "$##$empty$##$";
        /// <summary>
        /// Constant value to represent an empty inner query text for internal usage.
        /// </summary>
        public static readonly string EmptyInnerQueryText = "$##$emptyinnerquery$##$";
        /// <summary>
        /// Constant value of the default scoring. The value is 0.5.
        /// </summary>
        public static readonly double DefaultSimilarity = 0.5d;
        /// <summary>
        /// Constant value of the default fuzzy. The value is 0.5.
        /// </summary>
        public static readonly double DefaultFuzzyValue = 0.5d;
        /// <summary>
        /// Constant value to represent the null reference. The value is "null".
        /// </summary>
        public static readonly string NullReferenceValue = "null";

        /// <summary>
        /// Constant value of the default auto filter status. The value is FilterStatus.Enabled.
        /// </summary>
        public static readonly FilterStatus EnableAutofiltersDefaultValue = FilterStatus.Enabled;
        /// <summary>
        /// Constant value of the default lifespan filter status. The value is FilterStatus.Disabled.
        /// </summary>
        public static readonly FilterStatus EnableLifespanFilterDefaultValue = FilterStatus.Disabled;

        /// <summary>
        /// Gets the original text representation of the query.
        /// </summary>
        [DataMember]
        public string Querytext { get; internal set; }
        /// <summary>
        /// Gets the projection value. Default projection is NodeId.
        /// </summary>
        [DataMember]
        public string Projection { get; internal set; }
        /// <summary>
        /// Gets or sets the maximum count of the query result.
        /// </summary>
        [DataMember]
        public int Top { get; set; }
        /// <summary>
        /// Gets or sets the number of items that are skipped in the beginning of the result list. 
        /// </summary>
        [DataMember]
        public int Skip { get; set; }
        /// <summary>
        /// Gets or sets the sorting criterias in order of importance.
        /// </summary>
        [DataMember]
        public SortInfo[] Sort { get; set; }
        /// <summary>
        /// Gets true if the Sort property is not null and contains one or more elements.
        /// </summary>
        public bool HasSort => Sort != null && Sort.Length > 0;

        /// <summary>
        /// Gets the predicate tree representation of the query.
        /// </summary>
        [DataMember]
        public SnQueryPredicate QueryTree { get; internal set; }

        /// <summary>
        /// Gets or sets the value of the switch that controls the auto filtering.
        /// </summary>
        [DataMember]
        public FilterStatus EnableAutofilters { get; set; }
        /// <summary>
        /// Gets or sets the value of the switch that controls the lifespan filtering.
        /// </summary>
        [DataMember]
        public FilterStatus EnableLifespanFilter { get; set; }
        /// <summary>
        /// Gets or set a value that is true if only the conunt of query result is relevant.
        /// </summary>
        [DataMember]
        public bool CountOnly { get; set; }
        /// <summary>
        /// Gets or sets the performance option of the query executor.
        /// </summary>
        [DataMember]
        public QueryExecutionMode QueryExecutionMode { get; set; }
        /// <summary>
        /// Gets or set a value that is true if all versions are relavant in the query result.
        /// </summary>
        [DataMember]
        public bool AllVersions { get; set; }

        /// <summary>
        /// Gets or sets a value that is true if the query result need to contain all count
        /// without "top" and "skip" restrictions.
        /// </summary>
        [DataMember]
        public bool CountAllPages { get; set; }

        /// <summary>
        /// Gets or sets a value to indicate that the result set have to contains one or more elements or not.
        /// This is a simple storage slot.
        /// </summary>
        [DataMember]
        public bool ThrowIfEmpty { get; set; }
        /// <summary>
        /// Gets or sets a value to indicate that only the first item's existence is relevant.
        /// This is a simple storage slot.
        /// </summary>
        [DataMember]
        public bool ExistenceOnly { get; set; }
    }
}