using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Search;
using Lucene.Net.Search.Function;
using Lucene.Net.Search.Spans;
using Lucene.Net.Index;
using System.Globalization;
using Lucene.Net.Util;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Lucene29
{
    internal abstract class LucQueryVisitor
    {
        public virtual Query Visit(Query q)
        {
            if (q == null)
                return null;

            var booleanq = q as BooleanQuery; if (booleanq != null) return VisitBooleanQuery(booleanq);
            // SKIPPED: var boostingq = q as BoostingQuery; if (boostingq != null) return VisitBoostingQuery(boostingq);
            var constantScoreq = q as ConstantScoreQuery; if (constantScoreq != null) return VisitConstantScoreQuery(constantScoreq);
            var customScoreq = q as CustomScoreQuery; if (customScoreq != null) return VisitCustomScoreQuery(customScoreq);
            var disjunctionMaxq = q as DisjunctionMaxQuery; if (disjunctionMaxq != null) return VisitDisjunctionMaxQuery(disjunctionMaxq);
            var filteredq = q as FilteredQuery; if (filteredq != null) return VisitFilteredQuery(filteredq);
            // SKIPPED: var fuzzyLikeThisq = q as FuzzyLikeThisQuery; if (fuzzyLikeThisq != null) return VisitFuzzyLikeThisQuery(fuzzyLikeThisq);
            var matchAllDocsq = q as MatchAllDocsQuery; if (matchAllDocsq != null) return VisitMatchAllDocsQuery(matchAllDocsq);
            // SKIPPED: var moreLikeThisq = q as MoreLikeThisQuery; if (moreLikeThisq != null) return VisitMoreLikeThisQuery(moreLikeThisq);
            var multiPhraseq = q as MultiPhraseQuery; if (multiPhraseq != null) return VisitMultiPhraseQuery(multiPhraseq);
            var fuzzyq = q as FuzzyQuery; if (fuzzyq != null) return VisitFuzzyQuery(fuzzyq);
            var wildcardq = q as WildcardQuery; if (wildcardq != null) return VisitWildcardQuery(wildcardq);
            var phraseq = q as PhraseQuery; if (phraseq != null) return VisitPhraseQuery(phraseq);
            var prefixq = q as PrefixQuery; if (prefixq != null) return VisitPrefixQuery(prefixq);
            var spanFirstq = q as SpanFirstQuery; if (spanFirstq != null) return VisitSpanFirstQuery(spanFirstq);
            var spanNearq = q as SpanNearQuery; if (spanNearq != null) return VisitSpanNearQuery(spanNearq);
            var spanNotq = q as SpanNotQuery; if (spanNotq != null) return VisitSpanNotQuery(spanNotq);
            var spanOrq = q as SpanOrQuery; if (spanOrq != null) return VisitSpanOrQuery(spanOrq);
            var spanTermq = q as SpanTermQuery; if (spanTermq != null) return VisitSpanTermQuery(spanTermq);
            var termq = q as TermQuery; if (termq != null) return VisitTermQuery(termq);
            var valueSourceq = q as ValueSourceQuery; if (valueSourceq != null) return VisitValueSourceQuery(valueSourceq);
            var fieldScoreq = q as FieldScoreQuery; if (fieldScoreq != null) return VisitFieldScoreQuery(fieldScoreq);
            // <V2.9.2>
            var termRangeq = q as TermRangeQuery; if (termRangeq != null) return VisitTermRangeQuery(termRangeq);
            var numericRangeq = q as NumericRangeQuery; if (numericRangeq != null) return VisitNumericRangeQuery(numericRangeq);
            // </V2.9.2>

            throw new SnNotSupportedException("Unknown query type: " + q.GetType().FullName);
        }

        public virtual Query VisitBooleanQuery(BooleanQuery booleanq)
        {
            var clauses = booleanq.GetClauses();
            var visitedClauses = VisitBooleanClauses(clauses);
            BooleanQuery newQuery = null;
            if (visitedClauses != clauses)
            {
                newQuery = new BooleanQuery(booleanq.IsCoordDisabled());
                for (int i = 0; i < visitedClauses.Length; i++)
                    newQuery.Add(visitedClauses[i]);
            }
            return newQuery ?? booleanq;

        }
        public virtual BooleanClause[] VisitBooleanClauses(BooleanClause[] clauses)
        {
            List<BooleanClause> newList = null;
            int index = 0;
            int count = clauses.Length;
            while (index < count)
            {
                var visitedClause = VisitBooleanClause(clauses[index]);
                if (newList != null)
                {
                    newList.Add(visitedClause);
                }
                else if (visitedClause != clauses[index])
                {
                    newList = new List<BooleanClause>();
                    for (int i = 0; i < index; i++)
                        newList.Add(clauses[i]);
                    newList.Add(visitedClause);
                }
                index++;
            }
            return newList != null ? newList.ToArray() : clauses;
        }
        public virtual Query VisitConstantScoreQuery(ConstantScoreQuery constantScoreq) { throw new SnNotSupportedException(); }
        public virtual Query VisitCustomScoreQuery(CustomScoreQuery customScoreq) { throw new SnNotSupportedException(); }
        public virtual Query VisitDisjunctionMaxQuery(DisjunctionMaxQuery disjunctionMaxq) { throw new SnNotSupportedException(); }
        public virtual Query VisitFilteredQuery(FilteredQuery filteredq) { throw new SnNotSupportedException(); }
        public virtual Query VisitMatchAllDocsQuery(MatchAllDocsQuery matchAllDocsq) { throw new SnNotSupportedException(); }
        public virtual Query VisitMultiPhraseQuery(MultiPhraseQuery multiPhraseq) { throw new SnNotSupportedException(); }
        public virtual Query VisitFuzzyQuery(FuzzyQuery fuzzyq)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var term = fuzzyq.GetTerm();
#pragma warning restore CS0618 // Type or member is obsolete
            var visited = VisitTerm(term);
            if (term == visited)
                return fuzzyq;
            return new FuzzyQuery(visited);
        }
        public virtual Query VisitWildcardQuery(WildcardQuery wildcardq)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var term = wildcardq.GetTerm();
#pragma warning restore CS0618 // Type or member is obsolete
            var visited = VisitTerm(term);
            if (term == visited)
                return wildcardq;
            return new WildcardQuery(visited);
        }
        public virtual Query VisitPhraseQuery(PhraseQuery phraseq)
        {
            var terms = phraseq.GetTerms();
            PhraseQuery newQuery = null;

            int index = 0;
            int count = terms.Length;
            while (index < count)
            {
                var visitedTerm = VisitTerm(terms[index]);
                if (newQuery != null)
                {
                    newQuery.Add(visitedTerm);
                }
                else if (visitedTerm != terms[index])
                {
                    newQuery = new PhraseQuery();
                    for (int i = 0; i < index; i++)
                        newQuery.Add(terms[i]);
                    newQuery.Add(visitedTerm);
                }
                index++;
            }
            if (newQuery != null)
                return newQuery;
            return phraseq;
        }
        public virtual Query VisitPrefixQuery(PrefixQuery prefixq)
        {
            var term = prefixq.GetPrefix();
            var visited = VisitTerm(term);
            if (term == visited)
                return prefixq;
            return new WildcardQuery(visited);
        }
        public virtual Query VisitSpanFirstQuery(SpanFirstQuery spanFirstq) { throw new SnNotSupportedException(); }
        public virtual Query VisitSpanNearQuery(SpanNearQuery spanNearq) { throw new SnNotSupportedException(); }
        public virtual Query VisitSpanNotQuery(SpanNotQuery spanNotq) { throw new SnNotSupportedException(); }
        public virtual Query VisitSpanOrQuery(SpanOrQuery spanOrq) { throw new SnNotSupportedException(); }
        public virtual Query VisitSpanTermQuery(SpanTermQuery spanTermq) { throw new SnNotSupportedException(); }
        public virtual Query VisitTermQuery(TermQuery termq)
        {
            var term = termq.GetTerm();
            var visited = VisitTerm(term);
            if (term == visited)
                return termq;
            return new TermQuery(visited);
        }
        public virtual Query VisitValueSourceQuery(ValueSourceQuery valueSourceq) { throw new SnNotSupportedException(); }
        public virtual Query VisitFieldScoreQuery(FieldScoreQuery fieldScoreq) { throw new SnNotSupportedException(); }
        // <V2.9.2>
        public virtual Query VisitTermRangeQuery(TermRangeQuery termRangeq)
        {
            var field = termRangeq.GetField();
            var visitedField = VisitField(field);
            if (field == visitedField)
                return termRangeq;
            return new TermRangeQuery(visitedField, termRangeq.GetLowerTerm(), termRangeq.GetUpperTerm(), termRangeq.IncludesLower(), termRangeq.IncludesUpper());
        }
        public virtual Query VisitNumericRangeQuery(NumericRangeQuery numericRangeq)
        {
            var field = numericRangeq.GetField();
            var visitedField = VisitField(field);
            if (field == visitedField)
                return numericRangeq;

            var min = numericRangeq.GetMin();
            if (min is Int32)
                return NumericRangeQuery.NewIntRange(visitedField, numericRangeq.GetMin(), numericRangeq.GetMax(), numericRangeq.IncludesMin(), numericRangeq.IncludesMax());
            if (min is Int64)
                return NumericRangeQuery.NewLongRange(visitedField, numericRangeq.GetMin(), numericRangeq.GetMax(), numericRangeq.IncludesMin(), numericRangeq.IncludesMax());
            if (min is Single)
                return NumericRangeQuery.NewFloatRange(visitedField, (Single)numericRangeq.GetMin(), (Single)numericRangeq.GetMax(), numericRangeq.IncludesMin(), numericRangeq.IncludesMax());
            if (min is Double)
                return NumericRangeQuery.NewDoubleRange(visitedField, (Double)numericRangeq.GetMin(), (Double)numericRangeq.GetMax(), numericRangeq.IncludesMin(), numericRangeq.IncludesMax());

            throw new SnNotSupportedException(String.Format("VisitNumericRangeQuery with {0} minvalue is not supported.", min.GetType().Name));
        }
        // </V2.9.2>
        public virtual BooleanClause VisitBooleanClause(BooleanClause clause)
        {
            var occur = clause.GetOccur();
            var query = clause.GetQuery();
            var visited = Visit(query);
            if (query == visited)
                return clause;
            return new BooleanClause(visited, occur);
        }
        public virtual Term VisitTerm(Term term)
        {
            var field = term.Field();
            var text = term.Text();
            var visitedField = VisitField(field);
            var visitedText = VisitFieldText(text);
            if (field == visitedField && text == visitedText)
                return term;
            return new Term(visitedField, visitedText);
        }
        public virtual string VisitField(string field)
        {
            return field;
        }
        public virtual string VisitFieldText(string text)
        {
            return text;
        }

    }

    internal class LucQueryToStringVisitor : LucQueryVisitor
    {
        private StringBuilder _text = new StringBuilder();
        public override string ToString()
        {
            return _text.ToString();
        }

        private int booleanCount;
        public override Query VisitBooleanQuery(BooleanQuery booleanq)
        {
            if (booleanCount++ > 0)
                _text.Append("(");
            var q = base.VisitBooleanQuery(booleanq);
            if (--booleanCount > 0)
                _text.Append(")");
            return q;
        }
        public override BooleanClause[] VisitBooleanClauses(BooleanClause[] clauses)
        {
            List<BooleanClause> newList = null;
            int index = 0;
            int count = clauses.Length;
            while (index < count)
            {
                if (index > 0)
                    _text.Append(" ");
                var visitedClause = VisitBooleanClause(clauses[index]);
                if (newList != null)
                {
                    newList.Add(visitedClause);
                }
                else if (visitedClause != clauses[index])
                {
                    newList = new List<BooleanClause>();
                    for (int i = 0; i < index; i++)
                        newList.Add(clauses[i]);
                    newList.Add(visitedClause);
                }
                index++;
            }
            return newList != null ? newList.ToArray() : clauses;
        }
        public override BooleanClause VisitBooleanClause(BooleanClause clause)
        {
            _text.Append(clause.GetOccur());
            return base.VisitBooleanClause(clause);
        }
        public override Query VisitFuzzyQuery(FuzzyQuery fuzzyq)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            _text.Append(TermToString(fuzzyq.GetTerm()));
#pragma warning restore CS0618 // Type or member is obsolete
            _text.Append('~');
            _text.Append(Lucene.Net.Support.Single.ToString(fuzzyq.GetMinSimilarity()));
            _text.Append(BoostToString(fuzzyq.GetBoost()));

            return base.VisitFuzzyQuery(fuzzyq);
        }
        public override Query VisitWildcardQuery(WildcardQuery wildcardq)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            _text.Append(TermToString(wildcardq.GetTerm()));
#pragma warning restore CS0618 // Type or member is obsolete
            _text.Append(BoostToString(wildcardq.GetBoost()));

            return base.VisitWildcardQuery(wildcardq);
        }
        public override Query VisitPhraseQuery(PhraseQuery phraseq)
        {
            var terms = phraseq.GetTerms();
            var field = terms[0].Field();

            _text.Append(field);
            _text.Append(":\"");

            var positions = new int[terms.Length];
            for (int i = 0; i < positions.Length; i++)
                positions[i] = i;

            var pieces = new string[terms.Length];
            for (int i = 0; i < terms.Length; i++)
            {
                int pos = ((System.Int32)positions[i]);
                System.String s = pieces[pos];
                if (s == null)
                    s = (terms[i]).Text();
                else
                    s += "|" + (terms[i]).Text();
                pieces[pos] = s;
            }
            for (int i = 0; i < pieces.Length; i++)
            {
                if (i > 0)
                    _text.Append(' ');
                System.String s = pieces[i];
                if (s == null)
                    _text.Append('?');
                else
                    _text.Append(s);
            }
            _text.Append("\"");

            var slop = phraseq.GetSlop();
            if (slop != 0)
            {
                _text.Append("~");
                _text.Append(slop);
            }

            _text.Append(BoostToString(phraseq.GetBoost()));

            return base.VisitPhraseQuery(phraseq);
        }
        public override Query VisitPrefixQuery(PrefixQuery prefixq)
        {
            _text.Append(TermToString(prefixq.GetPrefix()));
            _text.Append('*');
            _text.Append(BoostToString(prefixq.GetBoost()));

            return base.VisitPrefixQuery(prefixq);
        }
        public override Query VisitTermQuery(TermQuery termq)
        {
            var term = termq.GetTerm();
            _text.Append(term.Field());
            _text.Append(":");
            _text.Append(GetTermText(term));
            _text.Append(BoostToString(termq.GetBoost()));
            return base.VisitTermQuery(termq);
        }
        public override Query VisitTermRangeQuery(TermRangeQuery termRangeq)
        {
            var field = termRangeq.GetField();
            var lowerTerm = GetTermText(termRangeq.GetLowerTerm());
            var upperTerm = GetTermText(termRangeq.GetUpperTerm());
            var includesLower = termRangeq.IncludesLower();
            var includesUpper = termRangeq.IncludesUpper();
            string oneTerm = null;

            _text.Append(field);
            _text.Append(":");

            string op = null;
            if (lowerTerm == null)
            {
                op = includesUpper ? "<=" : "<";
                oneTerm = upperTerm;
            }
            if (upperTerm == null)
            {
                op = includesLower ? ">=" : ">";
                oneTerm = lowerTerm;
            }

            if (op == null)
            {
                _text.Append(includesLower ? '[' : '{');
                _text.Append(lowerTerm);
                _text.Append(" TO ");
                _text.Append(upperTerm);
                _text.Append(includesUpper ? ']' : '}');
            }
            else
            {
                _text.Append(op).Append(oneTerm);
            }

            _text.Append(BoostToString(termRangeq.GetBoost()));

            return base.VisitTermRangeQuery(termRangeq);
        }
        public override Query VisitNumericRangeQuery(NumericRangeQuery numericRangeq)
        {
            var field = numericRangeq.GetField();
            var min = numericRangeq.GetMin();
            var max = numericRangeq.GetMax();
            var includesMin = numericRangeq.IncludesMin();
            var includesMax = numericRangeq.IncludesMax();
            ValueType oneValue = null;

            _text.Append(field);
            _text.Append(":");

            string op = null;
            if (min == null)
            {
                op = includesMax ? "<=" : "<";
                oneValue = max;
            }
            if (max == null)
            {
                op = includesMin ? ">=" : ">";
                oneValue = min;
            }

            if (op == null)
            {
                _text.Append(includesMin ? '[' : '{');
                _text.Append(Convert.ToString(min, CultureInfo.InvariantCulture));
                _text.Append(" TO ");
                _text.Append(Convert.ToString(max, CultureInfo.InvariantCulture));
                _text.Append(includesMax ? ']' : '}');
            }
            else
            {
                _text.Append(op).Append(Convert.ToString(oneValue, CultureInfo.InvariantCulture));
            }

            _text.Append(BoostToString(numericRangeq.GetBoost()));

            return base.VisitNumericRangeQuery(numericRangeq);
        }

        private string GetTermText(Term term)
        {
            var fieldName = term.Field();
            var fieldText = term.Text();
            if (fieldText == null)
                return null;

            var info = SearchManager.ContentRepository.GetPerFieldIndexingInfo(fieldName);
            if (info == null)
            {
                var c = fieldText.ToCharArray();
                for (int i = 0; i < c.Length; i++)
                    if (c[i] < ' ')
                        c[i] = '.';
                return new String(c);
            }
            var fieldHandler = info.IndexFieldHandler;
            switch (fieldHandler.IndexFieldType)
            {
                case IndexValueType.Bool:
                case IndexValueType.String:
                case IndexValueType.StringArray:
                    return GetTermText(fieldText);
                case IndexValueType.Int:
                    return Convert.ToString(NumericUtils.PrefixCodedToInt(fieldText), CultureInfo.InvariantCulture);
                case IndexValueType.Long:
                    return Convert.ToString(NumericUtils.PrefixCodedToLong(fieldText), CultureInfo.InvariantCulture);
                case IndexValueType.Float:
                    return Convert.ToString(NumericUtils.PrefixCodedToFloat(fieldText), CultureInfo.InvariantCulture);
                case IndexValueType.Double:
                    return Convert.ToString(NumericUtils.PrefixCodedToDouble(fieldText), CultureInfo.InvariantCulture);
                case IndexValueType.DateTime:
                    var d = new DateTime(NumericUtils.PrefixCodedToLong(fieldText));
                    if (d.Hour == 0 && d.Minute == 0 && d.Second == 0)
                        return GetTermText(d.ToString("yyyy-MM-dd"));
                    if (d.Second == 0)
                        return GetTermText(d.ToString("yyyy-MM-dd HH:mm"));
                    return GetTermText(d.ToString("yyyy-MM-dd HH:mm:ss"));
                default:
                    throw new SnNotSupportedException("Unknown IndexFieldType: " + fieldHandler.IndexFieldType);
            }
        }
        private string GetTermText(string text)
        {
            if (text == null)
                return text;
            if(text.Length==0)
                return String.Concat("'", text, "'");
            if (HasMoreWords(text))
                return String.Concat("\"", text, "\"");
            return text;
        }
        private bool HasMoreWords(string text)
        {
            for (int i = 0; i < text.Length; i++)
                if (!IsWordChar(text[i]))
                    return true;
            return false;
        }
        private bool IsWordChar(char c)
        {
            if (Char.IsWhiteSpace(c))
                return false;
            return !(Cql.StringTerminatorChars.Contains(c));
        }
        private string BoostToString(float boost)
        {
            return boost == 1.0f ? String.Empty : "^" + boost.ToString(CultureInfo.InvariantCulture);
        }
        private string TermToString(Term t)
        {
            var fieldName = t.Field();
            var value = t.Text();
            return String.Concat(fieldName, ":", value);
        }
    }
    
    internal class EmptyTermVisitor : LucQueryVisitor
    {
        public override Query VisitBooleanQuery(BooleanQuery booleanq)
        {
            var clauses = booleanq.GetClauses();
            var visitedClauses = VisitBooleanClauses(clauses);
            BooleanQuery newQuery = null;
            if (visitedClauses != clauses)
            {
                if (visitedClauses == null)
                    return null;
                newQuery = new BooleanQuery(booleanq.IsCoordDisabled());
                for (int i = 0; i < visitedClauses.Length; i++)
                    newQuery.Add(visitedClauses[i]);
            }
            return newQuery ?? booleanq;
        }
        public override BooleanClause[] VisitBooleanClauses(BooleanClause[] clauses)
        {
            List<BooleanClause> newList = null;
            int index = 0;
            int count = clauses.Length;
            while (index < count)
            {
                var visitedClause = VisitBooleanClause(clauses[index]);
                if (newList != null)
                {
                    if (visitedClause != null)
                        newList.Add(visitedClause);
                }
                else if (visitedClause != clauses[index])
                {
                    newList = new List<BooleanClause>();
                    for (int i = 0; i < index; i++)
                        newList.Add(clauses[i]);
                    if (visitedClause != null)
                        newList.Add(visitedClause);
                }
                index++;
            }
            return newList != null
                    ? newList.Count > 0
                        ? newList.ToArray()
                        : null
                    : clauses;
        }
        public override BooleanClause VisitBooleanClause(BooleanClause clause)
        {
            var occur = clause.GetOccur();
            var query = clause.GetQuery();
            var visited = Visit(query);
            if (query == visited)
                return clause;
            if (visited == null)
                return null;
            return new BooleanClause(visited, occur);
        }
        public override Query VisitFuzzyQuery(FuzzyQuery fuzzyq)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var term = fuzzyq.GetTerm();
#pragma warning restore CS0618 // Type or member is obsolete
            var visited = VisitTerm(term);
            if (term == visited)
                return fuzzyq;
            if (visited == null)
                return null;
            return new FuzzyQuery(visited);
        }
        public override Query VisitWildcardQuery(WildcardQuery wildcardq)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var term = wildcardq.GetTerm();
#pragma warning restore CS0618 // Type or member is obsolete
            var visited = VisitTerm(term);
            if (term == visited)
                return wildcardq;
            if (visited == null)
                return null;
            return new WildcardQuery(visited);
        }
        public override Query VisitPhraseQuery(PhraseQuery phraseq)
        {
            var terms = phraseq.GetTerms();
            PhraseQuery newQuery = null;

            int index = 0;
            int count = terms.Length;
            while (index < count)
            {
                var visitedTerm = VisitTerm(terms[index]);
                if (newQuery != null)
                {
                    if (visitedTerm != null)
                        newQuery.Add(visitedTerm);
                }
                else if (visitedTerm != terms[index])
                {
                    newQuery = new PhraseQuery();
                    for (int i = 0; i < index; i++)
                        newQuery.Add(terms[i]);
                    if (visitedTerm != null)
                        newQuery.Add(visitedTerm);
                }
                index++;
            }
            if (newQuery != null)
            {
                if (newQuery.GetTerms().Length > 0)
                    return newQuery;
                return null;
            }
            return phraseq;
        }
        public override Query VisitPrefixQuery(PrefixQuery prefixq)
        {
            var term = prefixq.GetPrefix();
            var visited = VisitTerm(term);
            if (term == visited)
                return prefixq;
            if (visited == null)
                return null;
            return new WildcardQuery(visited);
        }
        public override Query VisitTermQuery(TermQuery termq)
        {
            var term = termq.GetTerm();
            var visited = VisitTerm(term);
            if (term == visited)
                return termq;
            if (visited == null)
                return null;
            return new TermQuery(visited);
        }
        public override Term VisitTerm(Term term)
        {
            if (term.Text().Equals(SnQuery.EmptyText))
                return null;
            return base.VisitTerm(term);
        }

    }
}
