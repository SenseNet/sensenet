using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SenseNet.Search.Parser;
using SenseNet.Search.Parser.Predicates;

namespace SenseNet.Search.Tests.Implementations
{
    public sealed class SnQueryToStringVisitor : SnQueryVisitor
    {
        private StringBuilder _output = new StringBuilder();
        public string Output => _output.ToString();

        private readonly Regex _escaperRegex;

        public SnQueryToStringVisitor()
        {
            var pattern = new StringBuilder("[");
            pattern.Append("\\s");
            foreach (var c in CqlLexer.STRINGTERMINATORCHARS.ToCharArray())
                pattern.Append("\\" + c);
            pattern.Append("]");
            _escaperRegex = new Regex(pattern.ToString());
        }

        public override SnQueryPredicate VisitTextPredicate(TextPredicate text)
        {
            var value = Escape(text.Value);
            _output.Append($"{text.FieldName}:{value}");
            BoostTostring(text.Boost);
            FuzzyToString(text.FuzzyValue);

            return base.VisitTextPredicate(text);
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

        public override SnQueryPredicate VisitRangePredicate(RangePredicate range)
        {
            var min = range.Min;
            var max = range.Max;
            var minExclusive = range.MinExclusive;
            var maxExclusive = range.MaxExclusive;

            string oneTerm = null;

            _output.Append(range.FieldName);
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
            BoostTostring(range.Boost);

            return base.VisitRangePredicate(range);
        }

        private int _booleanCount;
        public override SnQueryPredicate VisitLogicalPredicate(LogicalPredicate logic)
        {
            if (_booleanCount++ > 0)
                _output.Append("(");
            var list = base.VisitLogicalPredicate(logic);
            if (--_booleanCount > 0)
                _output.Append(")");
            return list;
        }

        public override List<LogicalClause> VisitLogicalClauses(List<LogicalClause> clauses)
        {
            // The list item cannot be rewritten because this class is sealed.
            if (clauses.Count > 0)
            {
                VisitLogicalClause(clauses[0]);
                for (var i = 1; i < clauses.Count; i++)
                {
                    _output.Append(" ");
                    VisitLogicalClause(clauses[i]);
                }
            }
            return clauses;
        }               

        public override LogicalClause VisitLogicalClause(LogicalClause clause)
        {
            switch (clause.Occur)
            {
                case Occurence.Must: _output.Append('+'); break;
                case Occurence.MustNot:_output.Append('-');break;
            }

            return base.VisitLogicalClause(clause);
        }
    }
}
