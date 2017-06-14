using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Search;

namespace SenseNet.Search.Parser
{
    internal class QueryInfo
    {
        public LucQuery Query { get; set; }
        public SortField[] SortFields { get; set; }

        public List<string> QueryFieldNames { get; set; }
        public List<string> SortFieldNames { get; set; }
        public int Top { get; set; }
        public int Skip { get; set; }
        public bool AllPages{get;set;}
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
        public int PhraseQueries { get; set; }
        public int PrefixQueries { get; set; }
        public int TermQueries { get; set; }
        public int TermRangeQueries { get; set; }
        public int NumericRangeQueries { get; set; }

        public int FullRangeTermQueries { get; set; }
        public int FullRangeNumericQueries { get; set; }

        public QueryInfo()
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
            if (PhraseQueries > 0)
                sb.Append("PhraseQueries:").Append(PhraseQueries).Append(", ");
            if (PrefixQueries > 0)
                sb.Append("PrefixQueries:").Append(PrefixQueries).Append(", ");
            if (TermQueries > 0)
                sb.Append("TermQueries:").Append(TermQueries).Append(", ");
            if (TermRangeQueries > 0)
                sb.Append("TermRangeQueries:").Append(TermRangeQueries).Append(", ");
            if (NumericRangeQueries > 0)
                sb.Append("NumericRangeQueries:").Append(NumericRangeQueries).Append(", ");
            if (FullRangeTermQueries > 0)
                sb.Append("FullRangeTermQueries:").Append(FullRangeTermQueries).Append(", ");
            if (FullRangeNumericQueries > 0)
                sb.Append("FullRangeNumericQueries:").Append(FullRangeNumericQueries).Append(", ");

            return sb.ToString();
        }
    }

    internal class QueryClassifier : LucQueryVisitor
    {
        public static QueryInfo Classify(LucQuery query, int top, int skip, SortField[] orders, bool countOnly, bool allVersions)
        {
            var sortfieldNames = orders == null ? new List<string>() : orders.Select(x => x.GetField()).ToList();
            var queryInfo = new QueryInfo
            {
                Query = query,
                SortFields = orders,
                Top = top,
                Skip = skip,
                SortFieldNames = sortfieldNames,
                AllPages = query.AllPages,
                CountOnly = countOnly,
                AllVersions = allVersions
            };

            var visitor = new QueryClassifierVisitor(queryInfo);
            visitor.Visit(query.Query);

            return queryInfo;
        }

        private class QueryClassifierVisitor : LucQueryVisitor
        {
            private QueryInfo _queryInfo;

            public QueryClassifierVisitor(QueryInfo queryInfo)
            {
                _queryInfo = queryInfo;
            }

            public override Query VisitBooleanQuery(BooleanQuery booleanq)
            {
                _queryInfo.BooleanQueries++;
                return base.VisitBooleanQuery(booleanq);
            }
            public override Query VisitFuzzyQuery(FuzzyQuery fuzzyq)
            {
                _queryInfo.FuzzyQueries++;
                return base.VisitFuzzyQuery(fuzzyq);
            }
            public override Query VisitWildcardQuery(WildcardQuery wildcardq)
            {
                _queryInfo.WildcardQueries++;

                var text = wildcardq.GetTerm().Text();
                foreach(var c in text)
                {
                    if (c == '*') _queryInfo.AsteriskWildcards++;
                    if (c == '?') _queryInfo.QuestionMarkWildcards++;
                }

                return base.VisitWildcardQuery(wildcardq);
            }
            public override Query VisitPhraseQuery(PhraseQuery phraseq)
            {
                _queryInfo.PhraseQueries++;
                return base.VisitPhraseQuery(phraseq);
            }
            public override Query VisitPrefixQuery(PrefixQuery prefixq)
            {
                _queryInfo.PrefixQueries++;
                return base.VisitPrefixQuery(prefixq);
            }
            public override Query VisitTermQuery(TermQuery termq)
            {
                _queryInfo.TermQueries++;
                return base.VisitTermQuery(termq);
            }
            public override Query VisitTermRangeQuery(TermRangeQuery termRangeq)
            {
                _queryInfo.TermRangeQueries++;
                if (termRangeq.GetLowerTerm() != null && termRangeq.GetUpperTerm() != null)
                    _queryInfo.FullRangeTermQueries++;
                return base.VisitTermRangeQuery(termRangeq);
            }
            public override Query VisitNumericRangeQuery(NumericRangeQuery numericRangeq)
            {
                _queryInfo.NumericRangeQueries++;
                if (numericRangeq.GetMin() != null && numericRangeq.GetMax() != null)
                    _queryInfo.FullRangeNumericQueries++;
                return base.VisitNumericRangeQuery(numericRangeq);
            }
            public override BooleanClause VisitBooleanClause(BooleanClause clause)
            {
                var occur = clause.GetOccur();

                if (occur == BooleanClause.Occur.MUST)
                    _queryInfo.MustClauses++;
                else if (occur == BooleanClause.Occur.MUST_NOT)
                    _queryInfo.MustNotClauses++;
                else
                    _queryInfo.ShouldClauses++;

                return base.VisitBooleanClause(clause);
            }

            public override string VisitField(string field)
            {
                if (!_queryInfo.QueryFieldNames.Contains(field))
                    _queryInfo.QueryFieldNames.Add(field);

                return base.VisitField(field);
            }
        }
    }
}
