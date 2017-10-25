using System.Collections.Generic;
using System.Text;

namespace SenseNet.Search.Parser
{
    public class SnQueryInfo
    {
        public SnQuery Query { get; set; }
        public SortInfo[] SortFields { get; set; }

        public List<string> QueryFieldNames { get; set; }
        public List<string> SortFieldNames { get; set; }
        public int Top { get; set; }
        public int Skip { get; set; }
        public bool CountAllPages { get; set; }
        public bool CountOnly { get; set; }
        public bool AllVersions { get; set; }

        public int ShouldClauses { get; set; }
        public int MustClauses { get; set; }
        public int MustNotClauses { get; set; }

        public int AsteriskWildcards { get; set; }
        public int QuestionMarkWildcards { get; set; }

        public int BooleanQueries { get; set; }
        public int FuzzyQueries { get; set; }
        public int WildcardQueries { get; set; }
        public int PrefixQueries { get; set; }
        public int TermQueries { get; set; }
        public int RangeQueries { get; set; }
        public int FullRangeQueries { get; set; }

        public SnQueryInfo()
        {
            QueryFieldNames = new List<string>();
            SortFieldNames = new List<string>();
        }

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
