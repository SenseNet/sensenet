using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using SenseNet.Search.Querying;
using SenseNet.Search.Querying.Parser;
using SenseNet.Search.Querying.Parser.Predicates;
using LogicalClause = SenseNet.Search.Querying.Parser.Predicates.LogicalClause;

namespace SenseNet.Search.Lucene29
{
    public class SnQueryToLucQueryVisitor : SnQueryVisitor
    {
        public Query Result
        {
            get
            {
                if(_queryTree.Count != 1)
                    throw new CompilerException($"Result contains {_queryTree.Count} items. Expected: 1.");
                return _queryTree.Peek();
            }
        }

        private readonly Stack<Query> _queryTree = new Stack<Query>();

        private TermAttribute _termAtt;
        // ReSharper disable once NotAccessedField.Local
        private readonly IQueryContext _context;
        private readonly Analyzer _masterAnalyzer;

        public SnQueryToLucQueryVisitor(Analyzer masterAnalyzer, IQueryContext context)
        {
            _masterAnalyzer = masterAnalyzer;
            _context = context;
        }

        public override SnQueryPredicate VisitSimplePredicate(SimplePredicate simplePredicate)
        {
            var query = CreateStringValueQuery(simplePredicate.FieldName, simplePredicate.Value, simplePredicate.FuzzyValue );
            if(simplePredicate.Boost.HasValue)
                query.SetBoost(Convert.ToSingle(simplePredicate.Boost.Value));
            _queryTree.Push(query);
            return simplePredicate;
        }
        private Query CreateStringValueQuery(string fieldName, IndexValue value, double? fuzzyValue)
        {
            if (value.Type == IndexValueType.String)
            {
                var stringValue = value.StringValue;

                if (stringValue == SnQuery.EmptyText)
                    return new TermQuery(new Term(fieldName, stringValue));
                if (stringValue == SnQuery.EmptyInnerQueryText)
                    return new TermQuery(new Term("Id", NumericUtils.IntToPrefixCoded(0)));

                if (stringValue.Contains('*') || stringValue.Contains('?'))
                {
                    if (!stringValue.EndsWith("*"))
                        return new WildcardQuery(new Term(fieldName, stringValue));
                    var s = stringValue.TrimEnd('*');
                    if (s.Contains('?') || s.Contains('*'))
                        return new WildcardQuery(new Term(fieldName, stringValue));
                    return new PrefixQuery(new Term(fieldName, s));
                }

                var words = GetAnalyzedText(fieldName, stringValue);

                if (words.Length == 0)
                    words = new[] { string.Empty }; //return null;
                if (words.Length == 1)
                {
                    var term = new Term(fieldName, words[0]);
                    if (fuzzyValue == null)
                        return new TermQuery(term);
                    return new FuzzyQuery(term, Convert.ToSingle(fuzzyValue));
                }

                var phraseQuery = new PhraseQuery();
                foreach (var word in words)
                    phraseQuery.Add(new Term(fieldName, word));

                if (fuzzyValue != null)
                {
                    var slop = Convert.ToInt32(fuzzyValue.Value);
                    phraseQuery.SetSlop(slop);
                }
                return phraseQuery;
            }

            return new TermQuery(new Term(fieldName, ConvertToTermValue(value)));
        }
        private string ConvertToTermValue(IndexValue value)
        {
            switch (value.Type)
            {
                case IndexValueType.String:      return value.StringValue;
                case IndexValueType.StringArray: throw new NotSupportedException();
                case IndexValueType.Bool:        return value.BooleanValue ? IndexValue.Yes : IndexValue.No;
                case IndexValueType.Int:         return NumericUtils.IntToPrefixCoded(value.IntegerValue);
                case IndexValueType.Long:        return NumericUtils.LongToPrefixCoded(value.LongValue);
                case IndexValueType.Float:       return NumericUtils.FloatToPrefixCoded(value.SingleValue);
                case IndexValueType.Double:      return NumericUtils.DoubleToPrefixCoded(value.DoubleValue);
                case IndexValueType.DateTime:    return NumericUtils.LongToPrefixCoded(value.DateTimeValue.Ticks);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string[] GetAnalyzedText(string fieldName, string text)
        {
            //return TextSplitter.SplitText(field, text, _analyzers);
            var reader = new StringReader(text);
            var tokenStream = _masterAnalyzer.TokenStream(fieldName, reader);
            _termAtt = (TermAttribute)tokenStream.AddAttribute(typeof(TermAttribute));

            //var tokens = new List<string>();
            var words = new List<string>();
            while (tokenStream.IncrementToken())
            {
                //tokens.Add(_termAtt.ToString());
                words.Add(_termAtt.Term());
            }
            return words.ToArray();
        }

        public override SnQueryPredicate VisitRangePredicate(RangePredicate range)
        {
            var fieldName = range.FieldName;
            var minIncl = !range.MinExclusive;
            var maxIncl = !range.MaxExclusive;

            var min = ConvertRangeValue(range.Min);
            var max = ConvertRangeValue(range.Max);
            if (min == null && max == null)
                throw new CompilerException("Range is not supported if both min and max values are null.");

            if (min != null && max != null && (min.Type != max.Type))
                throw new CompilerException(
                    $"Range is not supported for different types: min value is {min.Type} but max value is {max.Type}");

            Query query;
            switch (min?.Type ?? max.Type)
            {
                case IndexValueType.String:
                    query = new TermRangeQuery(fieldName, min?.StringValue, max?.StringValue, minIncl, maxIncl);
                    break;
                case IndexValueType.Int:
                    query = NumericRangeQuery.NewIntRange(fieldName, min?.IntegerValue, max?.IntegerValue, minIncl, maxIncl);
                    break;
                case IndexValueType.Long:
                    query = NumericRangeQuery.NewIntRange(fieldName, min?.LongValue, max?.LongValue, minIncl, maxIncl);
                    break;
                case IndexValueType.Float:
                    query = NumericRangeQuery.NewIntRange(fieldName, min?.SingleValue, max?.SingleValue, minIncl, maxIncl);
                    break;
                case IndexValueType.Double:
                    query = NumericRangeQuery.NewIntRange(fieldName, min?.DoubleValue, max?.DoubleValue, minIncl, maxIncl);
                    break;
                default:
                    throw new CompilerException("Cannot create range query from this type");
            }
            _queryTree.Push(query);

            return range;
        }
        private IndexValue ConvertRangeValue(IndexValue input)
        {
            if (input == null)
                return null;

            switch (input.Type)
            {
                case IndexValueType.String:
                case IndexValueType.Int:
                case IndexValueType.Long:
                case IndexValueType.Float:
                case IndexValueType.Double:
                    return input;
                case IndexValueType.DateTime:
                    return new IndexValue(input.DateTimeValue.Ticks);

                case IndexValueType.StringArray:
                case IndexValueType.Bool:
                    throw new NotSupportedException("Range is not supported for this type: " + input.Type);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override SnQueryPredicate VisitLogicalPredicate(LogicalPredicate logic)
        {
            _queryTree.Push(new BooleanQuery());
            var visited = base.VisitLogicalPredicate(logic);
            return visited;
        }
        public override LogicalClause VisitLogicalClause(LogicalClause clause)
        {
            Visit(clause.Predicate);
            var compiledClause = new BooleanClause(_queryTree.Pop(), CompileOccur(clause.Occur));
            var booleanQuery = (BooleanQuery)_queryTree.Peek();
            booleanQuery.Add(compiledClause);
            return clause;
        }

        // ======================================================================
        
        private BooleanClause.Occur CompileOccur(Occurence occur)
        {
            switch (occur)
            {
                case Occurence.Default:
                case Occurence.Should: return BooleanClause.Occur.SHOULD;
                case Occurence.Must: return BooleanClause.Occur.MUST;
                case Occurence.MustNot: return BooleanClause.Occur.MUST_NOT;
                default:
                    throw new ArgumentOutOfRangeException(nameof(occur), occur, null);
            }
        }
    }
}
