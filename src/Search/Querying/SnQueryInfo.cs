using System.Collections.Generic;
using System.Text;

namespace SenseNet.Search.Querying
{
    /// <summary>
    /// Represents a result of an SnQuery analysis.
    /// Produced by the SnQueryClassifier.
    /// </summary>
    public class SnQueryInfo
    {
        /// <summary>
        /// Gets the analyzed query object.
        /// </summary>
        public SnQuery Query { get; internal set; }
        /// <summary>
        /// Gets the sorting criterias in order of importance.
        /// </summary>
        public SortInfo[] SortFields { get; internal set; }

        /// <summary>
        /// Gets all field names in the query.
        /// </summary>
        public List<string> QueryFieldNames { get; internal set; }
        /// <summary>
        /// Gets all field names in the sort fields.
        /// </summary>
        public List<string> SortFieldNames { get; internal set; }
        /// <summary>
        /// Gets the Top property value of the query.
        /// </summary>
        public int Top { get; internal set; }
        /// <summary>
        /// Gets the Skip property value of the query.
        /// </summary>
        public int Skip { get; internal set; }
        /// <summary>
        /// Gets the CountAllPages property value of the query.
        /// </summary>
        public bool CountAllPages { get; internal set; }
        /// <summary>
        /// Gets the CountOnly property value of the query.
        /// </summary>
        public bool CountOnly { get; internal set; }
        /// <summary>
        /// Gets the AllVersions property value of the query.
        /// </summary>
        public bool AllVersions { get; internal set; }

        /// <summary>
        /// Gets the count of logical predicates with SHOULD occurence in any depth of the query.
        /// </summary>
        public int ShouldClauses { get; internal set; }
        /// <summary>
        /// Gets the count of logical predicates with MUST occurence in any depth of the query.
        /// </summary>
        public int MustClauses { get; internal set; }
        /// <summary>
        /// Gets the count of logical predicates with MUST NOT occurence in any depth of the query.
        /// </summary>
        public int MustNotClauses { get; internal set; }

        /// <summary>
        /// Gets the count of asterisks as wildcard in all SimplePredicates in any depth of the query.
        /// </summary>
        public int AsteriskWildcards { get; internal set; }
        /// Gets the count of question marks as wildcard in all SimplePredicates in any depth of the query.
        public int QuestionMarkWildcards { get; internal set; }

        /// <summary>
        /// Gets the count of the LogicalPredicates.
        /// </summary>
        public int BooleanQueries { get; internal set; }
        /// <summary>
        /// Gets the count of predicates that have not null fuzzy value.
        /// </summary>
        public int FuzzyQueries { get; internal set; }
        /// <summary>
        /// Gets the count of predicates that contain any wildcards.
        /// </summary>
        public int WildcardQueries { get; internal set; }
        /// <summary>
        /// Gets the count of predicates that ends with the asterisk wildcard and do not contain any question mark wildcard.
        /// </summary>
        public int PrefixQueries { get; internal set; }
        /// <summary>
        /// Gets the count of SimplePredicates that are not in PrefixQueries, WildcardQueries and FuzzyQueries.
        /// </summary>
        public int TermQueries { get; internal set; }
        /// <summary>
        /// Gets the count of RangePredicates.
        /// </summary>
        public int RangeQueries { get; internal set; }
        /// <summary>
        /// Gets the count of RangePredicates with minimum and maximum definition..
        /// </summary>
        public int FullRangeQueries { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the SnQueryInfo.
        /// </summary>
        public SnQueryInfo()
        {
            QueryFieldNames = new List<string>();
            SortFieldNames = new List<string>();
        }

        /// <summary>
        /// Returns with the string representation of this object.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("QueryBy:[").Append(string.Join(",", QueryFieldNames)).Append("], ");
            if (SortFieldNames.Count > 0)
                sb.Append("SortBy:[").Append(string.Join(",", SortFieldNames)).Append("], ");
            if (Top > 0)
                sb.Append("Top:").Append(Top).Append(", ");
            if (Skip > 0)
                sb.Append("Skip:").Append(Skip).Append(", ");
            if (CountOnly)
                sb.Append("CountOnly:true, ");
            if (ShouldClauses > 0)
                sb.Append("Should:").Append(ShouldClauses).Append(", ");
            if (MustClauses > 0)
                sb.Append("Must:").Append(MustClauses).Append(", ");
            if (MustNotClauses > 0)
                sb.Append("MustNot:").Append(MustNotClauses).Append(", ");
            if (AsteriskWildcards + QuestionMarkWildcards > 0)
                sb.Append("Wildcards (*/?):").Append(AsteriskWildcards).Append("/").Append(QuestionMarkWildcards).Append(", ");

            if (BooleanQueries > 0)
                sb.Append("BooleanQueries:").Append(BooleanQueries).Append(", ");
            if (FuzzyQueries > 0)
                sb.Append("FuzzyQueries:").Append(FuzzyQueries).Append(", ");
            if (WildcardQueries > 0)
                sb.Append("WildcardQueries:").Append(WildcardQueries).Append(", ");
            if (PrefixQueries > 0)
                sb.Append("PrefixQueries:").Append(PrefixQueries).Append(", ");
            if (TermQueries > 0)
                sb.Append("TermQueries:").Append(TermQueries).Append(", ");
            if (RangeQueries > 0)
                sb.Append("RangeQueries:").Append(RangeQueries).Append(", ");
            if (FullRangeQueries > 0)
                sb.Append("FullRangeQueries:").Append(FullRangeQueries).Append(", ");

            return sb.ToString();
        }
    }
}
