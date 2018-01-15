//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Lucene.Net.Search;
//using Lucene.Net.Index;
//using SenseNet.Search.Parser;
//using SenseNet.ContentRepository.Storage;
//using SenseNet.Diagnostics;

//namespace SenseNet.Search.Lucene29
//{
//    // DO NOT DELETE: integration tests use this class (in budapest branch).
//    internal class DumpVisitor : LucQueryVisitor
//    {
//        private StringBuilder _dump = new StringBuilder();

//        public override Query VisitBooleanQuery(BooleanQuery booleanq)
//        {
//            _dump.Append("BoolQ(");
//            var clauses = booleanq.GetClauses();
//            var visitedClauses = VisitBooleanClauses(clauses);
//            BooleanQuery newQuery = null;
//            if (visitedClauses != clauses)
//            {
//                newQuery = new BooleanQuery(booleanq.IsCoordDisabled());
//                for (int i = 0; i < visitedClauses.Length; i++)
//                    newQuery.Add(clauses[i]);
//            }
//            _dump.Append(")");
//            return newQuery ?? booleanq;
//        }
//        public override Query VisitPhraseQuery(PhraseQuery phraseq)
//        {
//            _dump.Append("PhraseQ(");

//            var terms = phraseq.GetTerms();
//            PhraseQuery newQuery = null;

//            int index = 0;
//            int count = terms.Length;
//            while (index < count)
//            {
//                var visitedTerm = VisitTerm(terms[index]);
//                if (newQuery != null)
//                {
//                    newQuery.Add(visitedTerm);
//                }
//                else if (visitedTerm != terms[index])
//                {
//                    newQuery = new PhraseQuery();
//                    for (int i = 0; i < index; i++)
//                        newQuery.Add(terms[i]);
//                    newQuery.Add(visitedTerm);
//                }
//                index++;
//                if (index < count)
//                    _dump.Append(", ");
//            }
//            _dump.Append(", Slop:").Append(phraseq.GetSlop()).Append(BoostToString(phraseq)).Append(")");
//            if (newQuery != null)
//                return newQuery;
//            return phraseq;
//        }
//        public override Query VisitPrefixQuery(PrefixQuery prefixq)
//        {
//            _dump.Append("PrefixQ(");
//            var q = base.VisitPrefixQuery(prefixq);
//            _dump.Append(BoostToString(q));
//            _dump.Append(")");
//            return q;
//        }
//        public override Query VisitFuzzyQuery(FuzzyQuery fuzzyq)
//        {
//            _dump.Append("FuzzyQ(");
//            var q = base.VisitFuzzyQuery(fuzzyq);
//            var fq = q as FuzzyQuery;
//            if (fq != null)
//            {
//                _dump.Append(", minSimilarity:");
//                _dump.Append(fq.GetMinSimilarity());
//            }
//            _dump.Append(BoostToString(q));
//            _dump.Append(")");
//            return q;
//        }
//        public override Query VisitWildcardQuery(WildcardQuery wildcardq)
//        {
//            _dump.Append("WildcardQ(");
//            var q = base.VisitWildcardQuery(wildcardq);
//            _dump.Append(BoostToString(q));
//            _dump.Append(")");
//            return q;
//        }
//        public override Query VisitTermQuery(TermQuery termq)
//        {
//            _dump.Append("TermQ(");
//            var q = base.VisitTermQuery(termq);
//            _dump.Append(BoostToString(q));
//            _dump.Append(")");
//            return q;
//        }

//        public override Query VisitConstantScoreQuery(ConstantScoreQuery constantScoreq) { throw new SnNotSupportedException(); }
//        public override Query VisitDisjunctionMaxQuery(DisjunctionMaxQuery disjunctionMaxq) { throw new SnNotSupportedException(); }
//        public override Query VisitFieldScoreQuery(Lucene.Net.Search.Function.FieldScoreQuery fieldScoreq) { throw new SnNotSupportedException(); }
//        public override Query VisitFilteredQuery(FilteredQuery filteredq) { throw new SnNotSupportedException(); }
//        public override Query VisitMatchAllDocsQuery(MatchAllDocsQuery matchAllDocsq) { throw new SnNotSupportedException(); }
//        public override Query VisitMultiPhraseQuery(MultiPhraseQuery multiPhraseq) { throw new SnNotSupportedException(); }
//        public override Query VisitSpanFirstQuery(Lucene.Net.Search.Spans.SpanFirstQuery spanFirstq) { throw new SnNotSupportedException(); }
//        public override Query VisitSpanNearQuery(Lucene.Net.Search.Spans.SpanNearQuery spanNearq) { throw new SnNotSupportedException(); }
//        public override Query VisitSpanNotQuery(Lucene.Net.Search.Spans.SpanNotQuery spanNotq) { throw new SnNotSupportedException(); }
//        public override Query VisitSpanOrQuery(Lucene.Net.Search.Spans.SpanOrQuery spanOrq) { throw new SnNotSupportedException(); }
//        public override Query VisitSpanTermQuery(Lucene.Net.Search.Spans.SpanTermQuery spanTermq) { throw new SnNotSupportedException(); }
//        public override Query VisitValueSourceQuery(Lucene.Net.Search.Function.ValueSourceQuery valueSourceq) { throw new SnNotSupportedException(); }
//        public override Query VisitTermRangeQuery(TermRangeQuery termRangeq)
//        {
//            var q = (TermRangeQuery)base.VisitTermRangeQuery(termRangeq);
//            _dump.AppendFormat("TermRangeQ({0}:{1}{2} TO {3}{4}{5})",
//                q.GetField(), q.IncludesLower() ? "[" : "{",
//                q.GetLowerTerm(), q.GetUpperTerm(), q.IncludesUpper() ? "]" : "}", BoostToString(q));
//            return q;
//        }
//        public override Query VisitNumericRangeQuery(NumericRangeQuery numericRangeq)
//        {
//            var q = (NumericRangeQuery)base.VisitNumericRangeQuery(numericRangeq);
//            _dump.AppendFormat("NumericRangeQ({0}:{1}{2} TO {3}{4}{5})",
//                q.GetField(), q.IncludesMin() ? "[" : "{",
//                q.GetMin(), q.GetMax(), q.IncludesMax() ? "]" : "}", BoostToString(q));
//            return q;
//        }

//        public override BooleanClause VisitBooleanClause(BooleanClause clause)
//        {
//            var cl = clause.GetOccur();
//            var clString = cl == null ? " " : cl.ToString();
//            if (clString == "")
//                clString = " ";
//            _dump.Append("Cl((").Append(clString).Append("), ");
//            var c = base.VisitBooleanClause(clause);
//            _dump.Append(")");
//            return c;
//        }
//        public override Term VisitTerm(Term term)
//        {
//            _dump.Append("T(");
//            var t = base.VisitTerm(term);
//            _dump.Append(t);
//            _dump.Append(")");
//            return t;
//        }

//        private string BoostToString(Query query)
//        {
//            var sb = new StringBuilder();
//            var boost = query.GetBoost();
//            if (boost != 1.0)
//                sb.Append(", Boost(").Append(boost).Append(")");
//            return sb.ToString();
//        }

//        public override string ToString()
//        {
//            return _dump.ToString();
//        }
//    }
//}
