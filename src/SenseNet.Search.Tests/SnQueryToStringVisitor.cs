using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search.Parser;
using SenseNet.Search.Parser.Predicates;

namespace SenseNet.Search.Tests
{
    internal class SnQueryToStringVisitor : SnQueryVisitor
    {
        private StringBuilder _output = new StringBuilder();
        public string Output => _output.ToString();

        public override SnQueryNode VisitText(Text text)
        {
            PredicateToString(text.FieldName, text.Value, text.Boost, text.FuzzyValue);
            return base.VisitText(text);
        }

        public override SnQueryNode VisitIntegerNumber(IntegerNumber predicate)
        {
            PredicateToString(predicate.FieldName, predicate.Value, predicate.Boost, null);
            return base.VisitIntegerNumber(predicate);
        }
        public override SnQueryNode VisitLongNumber(LongNumber predicate)
        {
            PredicateToString(predicate.FieldName, predicate.Value, predicate.Boost, null);
            return base.VisitLongNumber(predicate);
        }
        public override SnQueryNode VisitSingleNumber(SingleNumber predicate)
        {
            PredicateToString(predicate.FieldName, predicate.Value, predicate.Boost, null);
            return base.VisitSingleNumber(predicate);
        }
        public override SnQueryNode VisitDoubleNumber(DoubleNumber predicate)
        {
            PredicateToString(predicate.FieldName, predicate.Value, predicate.Boost, null);
            return base.VisitDoubleNumber(predicate);
        }
        private void PredicateToString(string fieldName, object value, double? boost, double? fuzzy)
        {
            _output.Append($"{fieldName}:{value}");
            BoostTostring(boost);
            if (fuzzy.HasValue)
                _output.Append("~").Append(fuzzy.Value.ToString(CultureInfo.InvariantCulture));
        }
        private void BoostTostring(double? boost)
        {
            if (boost.HasValue && boost != 1.0d)
                _output.Append("^").Append(boost.Value.ToString(CultureInfo.InvariantCulture));
        }

        public override SnQueryNode VisitTextRange(TextRange range)
        {
            RangeToString(range.FieldName, range.Min, range.Max, range.MinExclusive, range.MaxExclusive, range.Boost);
            return base.VisitTextRange(range);
        }
        public override SnQueryNode VisitIntegerRange(IntegerRange range)
        {
            RangeToString(range.FieldName,
                range.Min == int.MinValue ? null : range.Min.ToString(CultureInfo.InvariantCulture),
                range.Max == int.MaxValue ? null : range.Max.ToString(CultureInfo.InvariantCulture),
                range.MinExclusive, range.MaxExclusive, range.Boost);
            return base.VisitIntegerRange(range);
        }
        public override SnQueryNode VisitLongRange(LongRange range)
        {
            RangeToString(range.FieldName,
                range.Min == long.MinValue ? null : range.Min.ToString(CultureInfo.InvariantCulture),
                range.Max == long.MaxValue ? null : range.Max.ToString(CultureInfo.InvariantCulture),
                range.MinExclusive, range.MaxExclusive, range.Boost);
            return base.VisitLongRange(range);
        }
        public override SnQueryNode VisitSingleRange(SingleRange range)
        {
            RangeToString(range.FieldName,
                float.IsNaN(range.Min) ? null : range.Min.ToString(CultureInfo.InvariantCulture),
                float.IsNaN(range.Max) ? null : range.Max.ToString(CultureInfo.InvariantCulture),
                range.MinExclusive, range.MaxExclusive, range.Boost);
            return base.VisitSingleRange(range);
        }
        public override SnQueryNode VisitDoubleRange(DoubleRange range)
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
        public override SnQueryNode VisitBooleanClauseList(BooleanClauseList boolClauseList)
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
