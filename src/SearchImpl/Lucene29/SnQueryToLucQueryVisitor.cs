using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using SenseNet.Search.Parser;
using SenseNet.Search.Parser.Predicates;
using LogicalClause = SenseNet.Search.Parser.Predicates.LogicalClause;

namespace SenseNet.Search.Lucene29
{
    internal class SnQueryToLucQueryVisitor : SnQueryVisitor
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
        private readonly IQueryContext _context;
        private readonly Analyzer _masterAnalyzer;

        public SnQueryToLucQueryVisitor(Analyzer masterAnalyzer, IQueryContext context)
        {
            _masterAnalyzer = masterAnalyzer;
            _context = context;
        }

        public override SnQueryPredicate VisitTextPredicate(TextPredicate text)
        {
            var query = CreateStringValueQuery(text);
            if(text.Boost.HasValue)
                query.SetBoost(Convert.ToSingle(text.Boost.Value));
            _queryTree.Push(query);
            return text;
        }
        private Query CreateStringValueQuery(TextPredicate predicate)
        {
            var fieldName = predicate.FieldName;
            var value = predicate.Value;

            if (value == SnQuery.EmptyText)
                return new TermQuery(new Term(fieldName, value));
            if (value == SnQuery.EmptyInnerQueryText)
                return new TermQuery(new Term("Id", NumericUtils.IntToPrefixCoded(0)));

            var hasWildcard = value.Contains('*') || value.Contains('?');
            var text = fieldName == "_Text" ? value : ConvertTermValue(fieldName, value, false);

            if (hasWildcard)
            {
                if (!text.EndsWith("*"))
                    return new WildcardQuery(new Term(fieldName, text));
                var s = text.TrimEnd('*');
                if (s.Contains('?') || s.Contains('*'))
                    return new WildcardQuery(new Term(fieldName, text));
                return new PrefixQuery(new Term(fieldName, s));
            }

            var words = GetAnalyzedText(fieldName, text);
            var fuzzyValue = predicate.FuzzyValue;

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
        private string ConvertTermValue(string fieldName, string text, bool throwEx)
        {
            var converter = _context.GetPerFieldIndexingInfo(fieldName)?.IndexFieldHandler;
            if (converter == null)
                return text;

            var val = new QueryCompilerValue(text);
            if (!converter.Compile(val))
            {
                if (throwEx)
                    throw new CompilerException($"Cannot parse the '{fieldName}' field value: {text}");
                return null;
            }

            switch (val.Datatype)
            {
                case IndexableDataType.String: return val.StringValue;
                case IndexableDataType.Int:    return NumericUtils.IntToPrefixCoded(val.IntValue);
                case IndexableDataType.Long:   return NumericUtils.LongToPrefixCoded(val.LongValue);
                case IndexableDataType.Float:  return NumericUtils.FloatToPrefixCoded(val.SingleValue);
                case IndexableDataType.Double: return NumericUtils.DoubleToPrefixCoded(val.DoubleValue);
                default: throw new ArgumentOutOfRangeException();
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

            QueryCompilerValue min, max;
            var fieldType = ConvertRangeValue(range.FieldName, range.Min, range.Max, out min, out max);

            Query query;
            switch (fieldType)
            {
                case IndexableDataType.String:
                    query = new TermRangeQuery(fieldName, min?.StringValue, max?.StringValue, minIncl, maxIncl);
                    break;
                case IndexableDataType.Int:
                    query = NumericRangeQuery.NewIntRange(fieldName, min?.IntValue, max?.IntValue, minIncl, maxIncl);
                    break;
                case IndexableDataType.Long:
                    query = NumericRangeQuery.NewIntRange(fieldName, min?.LongValue, max?.LongValue, minIncl, maxIncl);
                    break;
                case IndexableDataType.Float:
                    query = NumericRangeQuery.NewIntRange(fieldName, min?.SingleValue, max?.SingleValue, minIncl, maxIncl);
                    break;
                case IndexableDataType.Double:
                    query = NumericRangeQuery.NewIntRange(fieldName, min?.DoubleValue, max?.DoubleValue, minIncl, maxIncl);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            _queryTree.Push(query);

            return range;
        }
        private IndexableDataType ConvertRangeValue(string fieldName, string min, string max, out QueryCompilerValue convertedMin, out QueryCompilerValue convertedMax)
        {
            var converter = _context.GetPerFieldIndexingInfo(fieldName)?.IndexFieldHandler;
            if (converter == null)
            {
                convertedMin = min == null ? null : new QueryCompilerValue(min);
                convertedMax = min == null ? null : new QueryCompilerValue(max);
                return IndexableDataType.String;
            }

            if (min != null)
            {
                convertedMin = new QueryCompilerValue(min);
                if (!converter.Compile(convertedMin))
                    throw new CompilerException(
                        $"Cannot parse the '{fieldName}' as {converter.IndexFieldType} field value: {min}");
            }
            else
            {
                convertedMin = null;
            }
            if (max != null)
            {
                convertedMax = new QueryCompilerValue(max);
                if (!converter.Compile(convertedMax))
                    throw new CompilerException(
                        $"Cannot parse the '{fieldName}' as {converter.IndexFieldType} field value: {max}");
            }
            else
            {
                convertedMax = null;
            }

            return (convertedMin ?? convertedMax).Datatype;
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
            var compiledClause = new Lucene.Net.Search.BooleanClause(_queryTree.Pop(), CompileOccur(clause.Occur));
            var booleanQuery = (BooleanQuery)_queryTree.Peek();
            booleanQuery.Add(compiledClause);
            return clause;
        }

        // ======================================================================


        private Lucene.Net.Search.BooleanClause.Occur CompileOccur(Occurence occur)
        {
            switch (occur)
            {
                case Occurence.Default:
                case Occurence.Should: return Lucene.Net.Search.BooleanClause.Occur.SHOULD;
                case Occurence.Must: return Lucene.Net.Search.BooleanClause.Occur.MUST;
                case Occurence.MustNot: return Lucene.Net.Search.BooleanClause.Occur.MUST_NOT;
                default:
                    throw new ArgumentOutOfRangeException(nameof(occur), occur, null);
            }
        }
    }
}
