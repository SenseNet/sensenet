using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search.Parser.Predicates;

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
        public bool CountAllPages{get;set;}
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

    internal class SnQueryClassifier : SnQueryVisitor
    {
        public static SnQueryInfo Classify(SnQuery query, bool allVersions)
        {
            var sortfieldNames = query.Sort?.Select(x => x.FieldName).ToList() ?? new List<string>();
            var queryInfo = new SnQueryInfo
            {
                Query = query,
                SortFields = query.Sort,
                Top = query.Top,
                Skip = query.Skip,
                SortFieldNames = sortfieldNames,
                CountAllPages = query.CountAllPages,
                CountOnly = query.CountOnly,
                AllVersions = allVersions
            };

            var visitor = new QueryClassifierVisitor(queryInfo);
            visitor.Visit(query.QueryTree);

            return queryInfo;
        }

        private class QueryClassifierVisitor : SnQueryVisitor
        {
            private readonly SnQueryInfo _queryInfo;

            public QueryClassifierVisitor(SnQueryInfo queryInfo)
            {
                _queryInfo = queryInfo;
            }

            public override SnQueryPredicate VisitTextPredicate(TextPredicate text)
            {
                if (!_queryInfo.QueryFieldNames.Contains(text.FieldName))
                    _queryInfo.QueryFieldNames.Add(text.FieldName);

                var asterisks = text.Value.Count(c => c == '*');
                var questionMarks = text.Value.Count(c => c == '?');

                if (asterisks + questionMarks > 0)
                {
                    if (asterisks == 1 && questionMarks == 0 && text.Value.EndsWith("*"))
                        _queryInfo.PrefixQueries++;
                    else
                        _queryInfo.WildcardQueries++;

                    _queryInfo.AsteriskWildcards += asterisks;
                    _queryInfo.QuestionMarkWildcards += questionMarks;
                }
                else if (text.FuzzyValue != null)
                {
                    _queryInfo.FuzzyQueries++;
                }
                else
                {
                    _queryInfo.TermQueries++;
                }

                return base.VisitTextPredicate(text);
            }

            public override SnQueryPredicate VisitRangePredicate(RangePredicate range)
            {
                if (!_queryInfo.QueryFieldNames.Contains(range.FieldName))
                    _queryInfo.QueryFieldNames.Add(range.FieldName);

                _queryInfo.RangeQueries++;
                if (range.Min != null && range.Max != null)
                    _queryInfo.FullRangeQueries++;

                return base.VisitRangePredicate(range);
            }

            public override SnQueryPredicate VisitLogicalPredicate(LogicalPredicate logic)
            {
                _queryInfo.BooleanQueries++;
                return base.VisitLogicalPredicate(logic);
            }

            public override LogicalClause VisitLogicalClause(LogicalClause clause)
            {
                var occur = clause.Occur;

                switch (clause.Occur)
                {
                    case Occurence.Default:
                    case Occurence.Should:
                        _queryInfo.ShouldClauses++;
                        break;
                    case Occurence.Must:
                        _queryInfo.MustClauses++;
                        break;
                    case Occurence.MustNot:
                        _queryInfo.MustNotClauses++;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return base.VisitLogicalClause(clause);
            }
        }
    }
}
