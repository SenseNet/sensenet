using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using SenseNet.Search.Parser;
using SenseNet.Search.Parser.Predicates;
using BooleanClause = SenseNet.Search.Parser.Predicates.BooleanClause;

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


        public override SnQueryPredicate VisitText(TextPredicate predicate)
        {
            var query = CreateStringValueQuery(predicate);
            if(predicate.Boost.HasValue)
                query.SetBoost(Convert.ToSingle(predicate.Boost.Value));
            _queryTree.Push(query);
            return predicate;
        }
        private Query CreateStringValueQuery(TextPredicate predicate)
        {
            var fieldName = predicate.FieldName;
            var value = predicate.Value;

            if (value == SnQuery.EmptyText)
                return new TermQuery(new Term(fieldName, value));
            if (value == SnQuery.EmptyInnerQueryText)
                return new TermQuery(new Term("Id", NumericUtils.IntToPrefixCoded(0)));

            var text = fieldName == "_Text" ? value : ConvertTermValue(fieldName, value, false);

            var hasWildcard = text.Contains('*') || text.Contains('?');
            if (hasWildcard)
            {
                if (!text.EndsWith("*"))
                    return new WildcardQuery(new Term(fieldName, text));
                var s = text.TrimEnd('*');
                if (s.Contains('?') || s.Contains('*'))
                    return new WildcardQuery(new Term(fieldName, text));
                return new PrefixQuery(new Term(fieldName, s));
            }
            else
            {
                var words = GetAnalyzedText(fieldName, text);
                var fuzzyValue = predicate.FuzzyValue;

                if (words.Length == 0)
                    words = new string[] { string.Empty }; //return null;
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
        }
        private string ConvertTermValue(string fieldName, string text, bool throwEx)
        {
            var indexingInfo = _context.GetPerFieldIndexingInfo(fieldName);
            if (indexingInfo == null)
                return text; //UNDONE:!!!!! Use LowerStringIndexHandler instead
            var converter = indexingInfo.IndexFieldHandler;
            if (converter == null)
                return text; //UNDONE:!!!!! Use LowerStringIndexHandler instead

            var val = new QueryCompilerValue(text);
            if (!converter.Compile(val))
            {
                if (throwEx)
                    throw new CompilerException($"Cannot parse the '{fieldName}' field value: {text}");
                return null; //UNDONE:!!!!! Use LowerStringIndexHandler instead
            }

            if (val.Datatype == IndexableDataType.String)
                return val.StringValue;

            throw new NotImplementedException(); //UNDONE:!!!!! ConvertTermValue: data type is a kind of number.
        }
        private string[] GetAnalyzedText(string fieldName, string text)
        {
            //return TextSplitter.SplitText(field, text, _analyzers);
            var reader = new StringReader(text);
            var tokenStream = _masterAnalyzer.TokenStream(fieldName, reader);
            _termAtt = (TermAttribute)tokenStream.AddAttribute(typeof(TermAttribute));

            var tokens = new List<string>();
            var words = new List<string>();
            while (tokenStream.IncrementToken())
            {
                tokens.Add(_termAtt.ToString());
                words.Add(_termAtt.Term());
            }
            return words.ToArray();
        }

        public override SnQueryPredicate VisitLongNumber(LongNumberPredicate predicate)
        {
            return base.VisitLongNumber(predicate);
        }

        public override SnQueryPredicate VisitDoubleNumber(DoubleNumberPredicate predicate)
        {
            return base.VisitDoubleNumber(predicate);
        }

        public override SnQueryPredicate VisitTextRange(TextRange range)
        {
            return base.VisitTextRange(range);
        }

        public override SnQueryPredicate VisitLongRange(LongRange range)
        {
            return base.VisitLongRange(range);
        }

        public override SnQueryPredicate VisitDoubleRange(DoubleRange range)
        {
            return base.VisitDoubleRange(range);
        }

        public override SnQueryPredicate VisitBooleanClauseList(BooleanClauseList boolClauseList)
        {
            _queryTree.Push(new BooleanQuery());
            var visited = base.VisitBooleanClauseList(boolClauseList);
            return visited;
        }

        public override BooleanClause VisitBooleanClause(BooleanClause clause)
        {
            Visit(clause.Predicate);
            var compiledClause = new Lucene.Net.Search.BooleanClause(_queryTree.Pop(), CompileOccur(clause.Occur));
            var booleanQuery = (BooleanQuery)_queryTree.Peek();
            booleanQuery.Add(compiledClause);
            return clause;
        }

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
