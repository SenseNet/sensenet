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

namespace SenseNet.Search.Lucene29
{
    internal class SnQueryToLucQueryVisitor : SnQueryVisitor
    {
        public Query Result { get; private set; }

        private TermAttribute _termAtt;
        private IQueryContext _context;
        private Analyzer _masterAnalyzer;

        public SnQueryToLucQueryVisitor(Analyzer masterAnalyzer, IQueryContext context)
        {
            _masterAnalyzer = masterAnalyzer;
            _context = context;
        }

        //public override SnQueryPredicate Visit(SnQueryPredicate predicate)
        //{
        //    var textPred = predicate as TextPredicate;
        //    if (textPred != null && textPred.Value == "asdf")
        //    {
        //        // only a mock
        //        Result = new TermQuery(new Term("_Text", "asdf"));
        //        return predicate;
        //    }
        //    throw new NotImplementedException(); //UNDONE:!!!!! implement visitor and delete this override
        //}

        public override SnQueryPredicate VisitText(TextPredicate predicate)
        {
            Result = CreateStringValueQuery(predicate);
            return base.VisitText(predicate);
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
    }
}
