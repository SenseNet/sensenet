using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SenseNet.Search.Parser;
using SenseNet.Search.Parser.Predicates;

namespace SenseNet.Search.Tests.Implementations
{
    internal class SnQueryToStringVisitor : SnQueryVisitor
    {
        private StringBuilder _output = new StringBuilder();
        public string Output => _output.ToString();

        private Regex _escaperRegex;

        public SnQueryToStringVisitor()
        {
            var pattern = new StringBuilder("[");
            pattern.Append("\\s");
            foreach (var c in CqlLexer.STRINGTERMINATORCHARS.ToCharArray())
                pattern.Append("\\" + c);
            pattern.Append("]");
            _escaperRegex = new Regex(pattern.ToString());
        }

        public override SnQueryPredicate VisitText(TextPredicate text)
        {
            PredicateToString(text.FieldName, text.Value, text.Boost, text.FuzzyValue);
            return base.VisitText(text);
        }

        public override SnQueryPredicate VisitLongNumber(LongNumberPredicate predicate)
        {
            PredicateToString(predicate.FieldName, predicate.Value, predicate.Boost, null);
            return base.VisitLongNumber(predicate);
        }

        public override SnQueryPredicate VisitDoubleNumber(DoubleNumberPredicate predicate)
        {
            PredicateToString(predicate.FieldName, predicate.Value.ToString(CultureInfo.InvariantCulture), predicate.Boost, null);
            return base.VisitDoubleNumber(predicate);
        }
        private void PredicateToString(string fieldName, object value, double? boost, double? fuzzy)
        {
            value = Escape(value);
            _output.Append($"{fieldName}:{value}");
            BoostTostring(boost);
            FuzzyToString(fuzzy);
        }
        private object Escape(object value)
        {
            var stringValue = value as string;
            if (stringValue == null)
                return value;
            if (_escaperRegex.IsMatch(stringValue))
                return $"'{stringValue}'";
            return stringValue;
        }
        private void BoostTostring(double? boost)
        {
            if (boost.HasValue && boost != SnQuery.DefaultSimilarity)
                _output.Append("^").Append(boost.Value.ToString(CultureInfo.InvariantCulture));
        }
        private void FuzzyToString(double? fuzzy)
        {
            if (fuzzy.HasValue && fuzzy != SnQuery.DefaultFuzzyValue)
                _output.Append("~").Append(fuzzy.Value.ToString(CultureInfo.InvariantCulture));
        }

        public override SnQueryPredicate VisitTextRange(TextRange range)
        {
            RangeToString(range.FieldName, range.Min, range.Max, range.MinExclusive, range.MaxExclusive, range.Boost);
            return base.VisitTextRange(range);
        }
        public override SnQueryPredicate VisitLongRange(LongRange range)
        {
            RangeToString(range.FieldName,
                range.Min == long.MinValue ? null : range.Min.ToString(CultureInfo.InvariantCulture),
                range.Max == long.MaxValue ? null : range.Max.ToString(CultureInfo.InvariantCulture),
                range.MinExclusive, range.MaxExclusive, range.Boost);
            return base.VisitLongRange(range);
        }
        public override SnQueryPredicate VisitDoubleRange(DoubleRange range)
        {
            RangeToString(range.FieldName,
                double.IsNaN(range.Min) ? null : range.Min.ToString(CultureInfo.InvariantCulture),
                double.IsNaN(range.Max) ? null : range.Max.ToString(CultureInfo.InvariantCulture),
                range.MinExclusive, range.MaxExclusive, range.Boost);
            return base.VisitDoubleRange(range);
        }
        private void RangeToString(string fieldName, string min, string max, bool minExclusive, bool maxExclusive, double? boost)
        {
            string oneTerm = null;

            _output.Append(fieldName);
            _output.Append(":");

            string op = null;
            if (min == null)
            {
                op = !maxExclusive ? "<=" : "<";
                oneTerm = max;
            }
            if (max == null)
            {
                op = !minExclusive ? ">=" : ">";
                oneTerm = min;
            }

            if (op == null)
            {
                _output.Append(!minExclusive ? '[' : '{');
                _output.Append(min);
                _output.Append(" TO ");
                _output.Append(max);
                _output.Append(!maxExclusive ? ']' : '}');
            }
            else
            {
                _output.Append(op).Append(oneTerm);
            }
            BoostTostring(boost);
        }

        private int _booleanCount;
        public override SnQueryPredicate VisitBooleanClauseList(BooleanClauseList boolClauseList)
        {
            if (_booleanCount++ > 0)
                _output.Append("(");
            var list = base.VisitBooleanClauseList(boolClauseList);
            if (--_booleanCount > 0)
                _output.Append(")");
            return list;
        }
        public override List<BooleanClause> VisitBooleanClauses(List<BooleanClause> clauses)
        {
            List<BooleanClause> newList = null;
            var index = 0;
            var count = clauses.Count;
            while (index < count)
            {
                if (index > 0)
                    _output.Append(" ");
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
            return newList ?? clauses;
        }
        public override BooleanClause VisitBooleanClause(BooleanClause clause)
        {
            switch (clause.Occur)
            {
                case Occurence.Must: _output.Append('+'); break;
                case Occurence.MustNot:_output.Append('-');break;
            }

            return base.VisitBooleanClause(clause);
        }
    }
}
