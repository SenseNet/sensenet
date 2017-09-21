using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Search;
using Lucene.Net.Index;
using System.Diagnostics;
using Lucene.Net.Analysis;
using SenseNet.ContentRepository;
using Lucene.Net.Util;
using System.Text.RegularExpressions;
using System.Globalization;
using SenseNet.Search.Indexing;
using System.IO;
using Lucene.Net.Analysis.Tokenattributes;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search.Lucene29;

namespace SenseNet.Search.Parser
{
    public class QueryFieldValue_OLD : IQueryFieldValue //UNDONE:!!! After LINQ: Delete ASAP
    {
        internal bool IsPhrase { get; private set; }
        internal SnLucLexer.Token Token { get; private set; }
        internal double? FuzzyValue { get; set; }
        public string StringValue { get; private set; }
        public object InputObject { get; private set; }

        public IndexableDataType Datatype { get; private set; }
        public Int32 IntValue { get; private set; }
        public Int64 LongValue { get; private set; }
        public Single SingleValue { get; private set; }
        public Double DoubleValue { get; private set; }

        public QueryFieldValue_OLD(object value)
        {
            InputObject = value;
        }

        internal QueryFieldValue_OLD(string stringValue, SnLucLexer.Token token, bool isPhrase)
        {
            Datatype = IndexableDataType.String;
            StringValue = stringValue;
            Token = token;
            IsPhrase = isPhrase;
        }

        public void Set(Int32 value)
        {
            Datatype = IndexableDataType.Int;
            IntValue = value;
        }
        public void Set(Int64 value)
        {
            Datatype = IndexableDataType.Long;
            LongValue = value;
        }
        public void Set(Single value)
        {
            Datatype = IndexableDataType.Float;
            SingleValue = value;
        }
        public void Set(Double value)
        {
            Datatype = IndexableDataType.Double;
            DoubleValue = value;
        }
        public void Set(String value)
        {
            Datatype = IndexableDataType.String;
            StringValue = value;
        }

        public override string ToString()
        {
            return String.Concat(Token, ":", StringValue, FuzzyValue == null ? "" : ":" + FuzzyValue);
        }
    }

    internal class SnLucParser //UNDONE:!!! After LINQ: Delete ASAP
    {
        public enum DefaultOperator { Or, And }

        public bool ParseEmptyQuery = false;

        internal class QueryControlParam
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        [DebuggerDisplay("{Name}[{DataType}]")]
        private class FieldInfo
        {
            public static readonly FieldInfo Default = new FieldInfo { Name = IndexFieldName.AllText, OperatorToken = SnLucLexer.Token.Colon, IsBinary = false };
            public string Name { get; set; }
            public SnLucLexer.Token OperatorToken { get; set; }
            public bool IsBinary { get; set; }

            private bool _indexingInfoResolved;
            private IPerFieldIndexingInfo _indexingInfo;
            public IPerFieldIndexingInfo IndexingInfo
            {
                get
                {
                    if (!_indexingInfoResolved)
                    {
                        _indexingInfo = StorageContext.Search.ContentRepository.GetPerFieldIndexingInfo(this.Name);
                        _indexingInfoResolved = true;
                    }
                    return _indexingInfo;
                }
            }
        }

        private SnLucLexer _lexer;
        private Stack<FieldInfo> _currentField = new Stack<FieldInfo>();
        private double _defaultSimilarity = 0.5;
        private TermAttribute _termAtt;
        private Analyzer _masterAnalyzer;

        private List<QueryControlParam> _controls = new List<QueryControlParam>();
        public List<QueryControlParam> Controls { get { return _controls; } }

        public DefaultOperator Operator { get; private set; }
        public QueryFieldLevel FieldLevel { get; private set; }

        public SnLucParser()
        {
            _masterAnalyzer = Lucene29IndexingEngine.GetAnalyzer();
        }

        public Query Parse(string queryText)
        {
            return Parse(queryText, DefaultOperator.Or);
        }
        public Query Parse(string queryText, DefaultOperator @operator)
        {
            _lexer = new SnLucLexer(queryText);
            _controls.Clear();
            Operator = @operator;
            return ParseTopLevelQueryExpList();
        }

        private Query ParseTopLevelQueryExpList()
        {
            // QueryExpList    ==>  QueryExp | QueryExpList QueryExp

            var queries = new List<Query>();
            while (!IsEof())
            {
                var q = ParseQueryExp();
                if (q != null)
                    queries.Add(q);
            }

            if (queries.Count == 0)
                throw ParserError("Empty query is not allowed.");

            if (queries.Count == 1)
                return queries[0];

            var boolQuery = new BooleanQuery();
            foreach (var q in queries)
                AddBooleanClause(boolQuery, q, null);
            return boolQuery;

        }
        private Query ParseQueryExpList()
        {
            // QueryExpList    ==>  QueryExp | QueryExpList QueryExp

            var queries = new List<Query>();
            while (!IsEof() && _lexer.CurrentToken != SnLucLexer.Token.RParen)
            {
                var q = ParseQueryExp();
                if(q != null)
                    queries.Add(q);
            }

            if (queries.Count == 0)
                return null;

            if(queries.Count == 1)
                return queries[0];

            var boolQuery = new BooleanQuery();
            foreach(var q in queries)
                AddBooleanClause(boolQuery, q, null);
            return boolQuery;

        }
        private Query ParseQueryExp()
        {
            // BinaryOr | ControlExp*
            while (_lexer.CurrentToken == SnLucLexer.Token.ControlKeyword)
                ParseControlExp();
            if (IsEof())
                return null;
            return ParseBinaryOr();
        }
        private Query ParseBinaryOr()
        {
            // BinaryOr        ==>  BinaryAnd | BinaryOr OR BinaryAnd
            var queries = new List<Query>();
            queries.Add(ParseBinaryAnd());
            while (_lexer.CurrentToken == SnLucLexer.Token.Or)
            {
                _lexer.NextToken();
                queries.Add(ParseBinaryAnd());
            }
            if (queries.Count == 1)
                return queries[0];
            var boolq = new BooleanQuery();
            foreach (var query in queries)
                AddBooleanClause(boolq, query, BooleanClause.Occur.SHOULD);
            return boolq;
        }
        private Query ParseBinaryAnd()
        {
            // BinaryAnd       ==>  UnaryNot | BinaryAnd AND UnaryNot
            var queries = new List<Query>();
            queries.Add(ParseUnaryNot());
            while (_lexer.CurrentToken == SnLucLexer.Token.And)
            {
                _lexer.NextToken();
                queries.Add(ParseUnaryNot());
            }
            if (queries.Count == 1)
                return queries[0];
            var boolq = new BooleanQuery();
            foreach (var query in queries)
                AddBooleanClause(boolq, query, BooleanClause.Occur.MUST);
            return boolq;
        }
        private Query ParseUnaryNot()
        {
            var not = false;
            if (_lexer.CurrentToken == SnLucLexer.Token.Not)
            {
                _lexer.NextToken();
                not = true;
            }
            var query = ParseClause();
            if (!not)
                return query;
            var boolq = new BooleanQuery();
            AddBooleanClause(boolq, query, BooleanClause.Occur.MUST_NOT);
            return boolq;

        }
        private Query ParseClause()
        {
            var occur = ParseOccur();
            var query = ParseQueryExpGroup();
            var boost = ParseBoost();
            if (boost != null)
                query.SetBoost(Convert.ToSingle(boost.Value));
            if (occur == null || occur == BooleanClause.Occur.SHOULD)
                return query;
            var boolq = new BooleanQuery();
            AddBooleanClause(boolq, query, occur);
            return boolq;
        }
        private BooleanClause.Occur ParseOccur()
        {
            if (_lexer.CurrentToken == SnLucLexer.Token.Minus)
            {
                _lexer.NextToken();
                return BooleanClause.Occur.MUST_NOT;
            }
            if (_lexer.CurrentToken == SnLucLexer.Token.Plus)
            {
                _lexer.NextToken();
                return BooleanClause.Occur.MUST;
            }
            if(Operator == DefaultOperator.And)
                return BooleanClause.Occur.MUST;
            return null;
        }
        private Query ParseQueryExpGroup()
        {
            // QueryExpGroup   ==>  LPAREN ClauseList RPAREN | TermExp
            if (_lexer.CurrentToken != SnLucLexer.Token.LParen)
                return ParseTermExp();
            _lexer.NextToken();

            var clauses = ParseQueryExpList();
            if (_lexer.CurrentToken != SnLucLexer.Token.RParen)
                throw ParserError("Missing ')'");
            _lexer.NextToken();
            return clauses;
        }

        private Query ParseTermExp()
        {
            // TermExp           ==>  UnaryTermExp | BinaryTermExp | QueryExpGroup | DefaultFieldExp

            Query result = null;

            var fieldInfo = ParseFieldHead();
            if (fieldInfo != null)
                _currentField.Push(fieldInfo);
            else if (_currentField.Count != 0)
            {
                _currentField.Push(_currentField.Peek());
            }
            else
            {
                _currentField.Push(FieldInfo.Default);
                FieldLevel = QueryFieldLevel.BinaryOrFullText;
            }
            result = ParseUnaryTermExp();
            if (result != null)
            {
                _currentField.Pop();
                return result;
            }
            result = ParseBinaryTermExp();
            if (result != null)
            {
                _currentField.Pop();
                return result;
            }
            result = ParseQueryExpGroup();
            if (result != null)
            {
                _currentField.Pop();
                return result;
            }
            result = ParseDefaultFieldExp();
            if (result != null)
            {
                _currentField.Pop();
                return result;
            }
            throw ParserError("Expected field expression, expression group or simple term.");
        }
        private Query ParseUnaryTermExp()
        {
            // UnaryTermExp      ==>  UnaryFieldHead UnaryFieldValue | ControlExp
            // UnaryFieldHead    ==>  STRING COLON | STRING NEQ
            // UnaryFieldValue   ==>  ValueGroup | Range

            Query result = null;

            var fieldInfo = _currentField.Peek();
            if (fieldInfo == null)
                return null;
            if (fieldInfo.IsBinary)
                return null;

            if (fieldInfo.OperatorToken == SnLucLexer.Token.NEQ)
            {
                var value = ParseExactValue(true);
                var bq = new BooleanQuery();
                bq.Add(new BooleanClause(CreateValueQuery(value), BooleanClause.Occur.MUST_NOT));
                return bq;
            }

            result = ParseValueGroup();
            if (result != null)
                return result;

            result = ParseRange();
            if (result != null)
                return result;

            throw ParserError(String.Concat("Unexpected '", _lexer.StringValue, "'"));
        }
        private Query ParseBinaryTermExp()
        {
            // BinaryTermExp     ==>  BinaryFieldHead Value
            // BinaryFieldHead   ==>  STRING LT | STRING GT | STRING LTE | STRING GTE

            var fieldInfo = _currentField.Peek();
            if (fieldInfo == null)
                return null;
            if (!fieldInfo.IsBinary)
                return null;

            var value = ParseExactValue(true);
            switch (fieldInfo.OperatorToken)
            {
                case SnLucLexer.Token.LT:
                    return CreateRangeQuery(fieldInfo.Name, null, value, true, false);
                case SnLucLexer.Token.LTE:
                    return CreateRangeQuery(fieldInfo.Name, null, value, true, true);
                case SnLucLexer.Token.GT:
                    return CreateRangeQuery(fieldInfo.Name, value, null, false, true);
                case SnLucLexer.Token.GTE:
                    return CreateRangeQuery(fieldInfo.Name, value, null, true, true);
            }

            throw new SnNotSupportedException("Unexpected OperatorToken: " + fieldInfo.OperatorToken);
        }
        private Query ParseDefaultFieldExp()
        {
            // DefaultFieldExp   ==>  ValueGroup
            return ParseValueGroup();
        }

        private Query ParseValueGroup()
        {
            // ValueGroup        ==>  LPAREN ValueExpList RPAREN | FuzzyValue

            if (_lexer.CurrentToken != SnLucLexer.Token.LParen)
            {
                var value = ParseFuzzyValue();
                if (value != null)
                    return CreateValueQuery(value);
                return null;
            }
            _lexer.NextToken();

            var result = ParseValueExpList();

            if (_lexer.CurrentToken != SnLucLexer.Token.RParen)
                throw ParserError("Expcted: ')'");
            _lexer.NextToken();

            return result;
        }
        private Query ParseValueExpList()
        {
            // ValueExpList      ==>  ValueExp | ValueExpList ValueExp

            BooleanQuery boolQuery;
            var first = ParseValueExp();

            if (_lexer.CurrentToken == SnLucLexer.Token.RParen)
                return first;

            boolQuery = new BooleanQuery();
            AddBooleanClause(boolQuery, first, null);

            while (!IsEof() && _lexer.CurrentToken != SnLucLexer.Token.RParen)
            {
                var q = ParseValueExp();
                AddBooleanClause(boolQuery, q, null);
            }

            return boolQuery;
        }
        private Query ParseValueExp()
        {
            // ValueExp          ==>  ValueBinaryOr
            return ParseValueBinaryOr();
        }
        private Query ParseValueBinaryOr()
        {
            // ValueBinaryOr     ==>  ValueBinaryAnd | ValueBinaryOr OR ValueBinaryAnd
            var queries = new List<Query>();
            queries.Add(ParseValueBinaryAnd());
            while (_lexer.CurrentToken == SnLucLexer.Token.Or)
            {
                _lexer.NextToken();
                queries.Add(ParseValueBinaryAnd());
            }
            if (queries.Count == 1)
                return queries[0];
            var boolq = new BooleanQuery();
            foreach (var query in queries)
                AddBooleanClause(boolq, query, BooleanClause.Occur.SHOULD);
            return boolq;
        }
        private Query ParseValueBinaryAnd()
        {
            // ValueBinaryAnd    ==>  ValueUnaryNot | ValueBinaryAnd AND ValueUnaryNot
            var queries = new List<Query>();
            queries.Add(ParseValueUnaryNot());
            while (_lexer.CurrentToken == SnLucLexer.Token.And)
            {
                _lexer.NextToken();
                queries.Add(ParseValueUnaryNot());
            }
            if (queries.Count == 1)
                return queries[0];
            var boolq = new BooleanQuery();
            foreach (var query in queries)
                AddBooleanClause(boolq, query, BooleanClause.Occur.MUST);
            return boolq;
        }
        private Query ParseValueUnaryNot()
        {
            // ValueUnaryNot     ==>  ValueClause | NOT ValueClause
            // ValueClause       ==>  [Occur] ValueGroup
            var not = false;
            if (_lexer.CurrentToken == SnLucLexer.Token.Not)
            {
                _lexer.NextToken();
                not = true;
            }
            var query = ParseValueClause();
            if (!not)
                return query;
            var boolq = new BooleanQuery();
            AddBooleanClause(boolq, query, BooleanClause.Occur.MUST_NOT);
            return boolq;
        }
        private Query ParseValueClause()
        {
            // ValueClause       ==>  [Occur] ValueGroup
            var occur = ParseOccur();
            var query = ParseValueGroup();
            var boost = ParseBoost();
            if (occur == null || occur == BooleanClause.Occur.SHOULD)
                return query;
            var boolq = new BooleanQuery();
            AddBooleanClause(boolq, query, occur);
            return boolq;
        }

        // =====================================================

        private FieldInfo ParseFieldHead()
        {
            // UnaryFieldHead    ==>  STRING COLON | STRING COLON NEQ
            // BinaryFieldHead   ==>  STRING COLON LT | STRING COLON GT | STRING COLON LTE | STRING COLON GTE

            FieldInfo fieldInfo = null;
            if (_lexer.CurrentToken == SnLucLexer.Token.Field)
            {
                var name = _lexer.StringValue;
                if (name == "Password" || name == "PasswordHash")
                    throw new InvalidOperationException("Cannot search by '" + name + "' field name");


                fieldInfo = new FieldInfo { Name = name };
                fieldInfo.OperatorToken = _lexer.CurrentToken;
                SetFieldLevel(fieldInfo);

                _lexer.NextToken();
                if (_lexer.CurrentToken != SnLucLexer.Token.Colon)
                    throw new InvalidOperationException("#### ParseFieldHead ####");


                _lexer.NextToken();
                switch (_lexer.CurrentToken)
                {
                    case SnLucLexer.Token.NEQ:
                        fieldInfo.IsBinary = false;
                        fieldInfo.OperatorToken = _lexer.CurrentToken;
                        _lexer.NextToken();
                        break;
                    case SnLucLexer.Token.LT:
                    case SnLucLexer.Token.LTE:
                    case SnLucLexer.Token.GT:
                    case SnLucLexer.Token.GTE:
                        fieldInfo.IsBinary = true;
                        fieldInfo.OperatorToken = _lexer.CurrentToken;
                        _lexer.NextToken();
                        break;
                }
            }

            return fieldInfo;
        }
        private Query ParseRange()
        {
            // Range        ==>  RangeStart ExactValue TO ExactValue RangeEnd
            var start = ParseRangeStart();
            if (start == null)
                return null;
            var minValue = ParseExactValue(false);
            if (_lexer.CurrentToken != SnLucLexer.Token.To)
                throw SyntaxError();
            _lexer.NextToken();
            var maxValue = ParseExactValue(false);
            var end = ParseRangeEnd();
            if (end == null)
                throw ParserError("Unterminated Range expression");

            var fieldInfo = _currentField.Peek();
            var fieldName = fieldInfo.Name;
            var includeLower = start.Value == true;
            var includeUpper = end.Value == true;

            if (minValue == null && maxValue == null)
                throw ParserError("Invalid range.");

            return CreateRangeQuery(fieldName, minValue, maxValue, includeLower, includeUpper);
        }
        private QueryFieldValue_OLD ParseFuzzyValue()
        {
            // FuzzyValue   ==>  Value [Fuzzy]
            var val = ParseValue(false);
            if (val == null)
                return null;
            val.FuzzyValue = ParseFuzzy(val.IsPhrase);
            return val;
        }
        private QueryFieldValue_OLD ParseValue(bool throwEx)
        {
            // Value        ==>  ExactValue | WILDCARDSTRING
            var val = ParseExactValue(false);
            if (val != null)
                return val;
            if (_lexer.CurrentToken == SnLucLexer.Token.WildcardString)
            {
                val = new QueryFieldValue_OLD(_lexer.StringValue.ToLower(), _lexer.CurrentToken, _lexer.IsPhrase );
                _lexer.NextToken();
                return val;
            }
            if (!throwEx)
                return null;
            throw ParserError(String.Concat("Unexpected ", _lexer.CurrentToken, ". Expected: STRING | NUMBER | WILDCARDSTRING"));
        }
        private QueryFieldValue_OLD ParseExactValue(bool throwEx)
        {
            // ExactValue   ==>  STRING | NUMBER | EMPTY
            if (_lexer.StringValue == SnQuery.EmptyText)
            {
                ParseEmptyQuery = true;
                var fieldVal = new QueryFieldValue_OLD(_lexer.StringValue, _lexer.CurrentToken, _lexer.IsPhrase);
                _lexer.NextToken();
                return fieldVal;
            }
            if (_lexer.CurrentToken != SnLucLexer.Token.String && _lexer.CurrentToken != SnLucLexer.Token.Number)
            {
                if (throwEx)
                    throw ParserError(String.Concat("Unexpected ", _lexer.CurrentToken, ". Expected: STRING | NUMBER"));
                return null;
            }

            var field = _currentField.Peek();
            var fieldName = field.Name;
            var val = new QueryFieldValue_OLD(_lexer.StringValue, _lexer.CurrentToken, _lexer.IsPhrase);
            if (fieldName != IndexFieldName.AllText && _lexer.StringValue != SnQuery.EmptyInnerQueryText)
            {
                var info = StorageContext.Search.ContentRepository.GetPerFieldIndexingInfo(fieldName);
                if (info != null)
                {
                    var fieldHandler = info.IndexFieldHandler;
                    if (fieldHandler != null)
                    {
                        if (!fieldHandler.TryParseAndSet(val))
                        {
                            if (throwEx)
                                throw ParserError(String.Concat("Cannot parse the '", fieldName, "' field value: ", _lexer.StringValue));
                            return null;
                        }
                    }
                }
            }
            _lexer.NextToken();
            return val;
        }
        private double? ParseFuzzy(bool isPhrase)
        {
            // Fuzzy        ==>  TILDE [NUMBER]
            if (_lexer.CurrentToken != SnLucLexer.Token.Tilde)
                return null;
            _lexer.NextToken();
            if (isPhrase)
            {
                if (_lexer.CurrentToken != SnLucLexer.Token.Number)
                    throw ParserError("Missing proximity value");
                _lexer.NextToken();
                return _lexer.NumberValue;
            }
            else
            {
                if (_lexer.CurrentToken != SnLucLexer.Token.Number)
                    return _defaultSimilarity;
                if (_lexer.NumberValue < 0.0 || _lexer.NumberValue > 1.0)
                    throw ParserError(String.Concat("Invalid fuzzy value (0.0-1.0): ", _lexer.NumberValue));
                _lexer.NextToken();
                return _lexer.NumberValue;
            }
        }
        private double? ParseBoost()
        {
            // Boost        ==>  CIRC NUMBER
            if (_lexer.CurrentToken != SnLucLexer.Token.Circ)
                return null;
            _lexer.NextToken();
            if (_lexer.CurrentToken != SnLucLexer.Token.Number)
                throw SyntaxError();
            _lexer.NextToken();
            return _lexer.NumberValue;
        }
        private bool? ParseRangeStart()
        {
            // RangeStart   ==>  LBRACKET | LBRACE
            if (_lexer.CurrentToken == SnLucLexer.Token.LBracket)//excl
            {
                _lexer.NextToken();
                return true;
            }
            if (_lexer.CurrentToken == SnLucLexer.Token.LBrace)//incl
            {
                _lexer.NextToken();
                return false;
            }
            return null;
        }
        private bool? ParseRangeEnd()
        {
            // RangeEnd     ==>  RBRACKET | RBRACE
            if (_lexer.CurrentToken == SnLucLexer.Token.RBracket)//excl
            {
                _lexer.NextToken();
                return true;
            }
            if (_lexer.CurrentToken == SnLucLexer.Token.RBrace)//incl
            {
                _lexer.NextToken();
                return false;
            }
            return null;
        }

        // -----------------------------------------------------

        private void ParseControlExp()
        {
            // ControlExp              ==>  ControlExpWithParam | ControlExpWithoutParam
            var startString = _lexer.StringValue;
            if (ParseControlExpWithParam())
                return;
            if (ParseControlExpWithoutParam())
                return;
            throw ParserError("Unknown control keyword: " + startString);
        }
        private bool ParseControlExpWithParam()
        {
            // ControlExpWithParam     ==>  ControlByNumberParam | ControlByStringParam | ControlBySwitchParam
            if (ParseControlByNumberParam())
                return true;
            if (ParseControlByStringParam())
                return true;
            if (ParseControlBySwitchParam())
                return true;
            if (ParseControlExpWithoutParam())
                return true;
            throw ParserError(String.Concat("Cannot parse '", _lexer.StringValue, "' (ParseControlExpWithParam)"));
        }
        private bool ParseControlByNumberParam()
        {
            // ControlByNumberParam    ==>  ControlByNumberName COLON Number
            var name = ParseControlByNumberName();
            if (name == null)
                return false;
            if (_lexer.CurrentToken != SnLucLexer.Token.Colon)
                throw ParserError("Expected: Colon (':')");
            _lexer.NextToken();
            if (_lexer.CurrentToken != SnLucLexer.Token.Number)
                throw ParserError("Expected: Number");
            InterpretControl(name, _lexer.StringValue);
            _lexer.NextToken();
            return true;
        }
        private string ParseControlByNumberName()
        {
            // ControlByNumberName     ==>  TOP | SKIP
            string name = null;
            switch (_lexer.StringValue)
            {
                case SnLucLexer.Keywords.Top: name = SnLucLexer.Keywords.Top; break;
                case SnLucLexer.Keywords.Skip: name = SnLucLexer.Keywords.Skip; break;
            }
            if (name != null)
                _lexer.NextToken();
            return name;
        }
        private bool ParseControlByStringParam()
        {
            // ControlByStringParam    ==>  ControlByStringName COLON String
            var name = ParseControlByStringName();
            if (name == null)
                return false;
            if (_lexer.CurrentToken != SnLucLexer.Token.Colon)
                throw ParserError("Expected: Colon (':')");
            _lexer.NextToken();
            if (_lexer.CurrentToken != SnLucLexer.Token.String)
                throw ParserError("Expected: String");
            InterpretControl(name, _lexer.StringValue);
            _lexer.NextToken();
            return true;
        }
        private string ParseControlByStringName()
        {
            // ControlByStringName     ==>  SORT | REVERSESORT
            string name = null;
            switch (_lexer.StringValue)
            {
                case SnLucLexer.Keywords.Select: name = SnLucLexer.Keywords.Select; break;
                case SnLucLexer.Keywords.Sort: name = SnLucLexer.Keywords.Sort; break;
                case SnLucLexer.Keywords.ReverseSort: name = SnLucLexer.Keywords.ReverseSort; break;
            }
            if (name != null)
                _lexer.NextToken();
            return name;
        }
        private bool ParseControlBySwitchParam()
        {
            // ControlBySwitchParam    ==>  ControlBySwitchName COLON SwitchParam
            var name = ParseControlBySwitchName();
            if (name == null)
                return false;
            if (_lexer.CurrentToken != SnLucLexer.Token.Colon)
                throw ParserError("Expected: Colon (':')");
            _lexer.NextToken();
            var param = ParseSwitchParam();
            InterpretControl(name, param);
            return true;
        }
        private string ParseControlBySwitchName()
        {
            // ControlBySwitchName     ==>  AUTOFILTERS
            string name = null;
            switch (_lexer.StringValue)
            {
                case SnLucLexer.Keywords.Autofilters: name = SnLucLexer.Keywords.Autofilters; break;
                case SnLucLexer.Keywords.Lifespan: name = SnLucLexer.Keywords.Lifespan; break;
            }
            if (name != null)
                _lexer.NextToken();
            return name;

        }
        private string ParseSwitchParam()
        {
            // SwitchParam             ==>  ON | OFF
            if ((_lexer.CurrentToken != SnLucLexer.Token.String)
                || (_lexer.StringValue != SnLucLexer.Keywords.On && _lexer.StringValue != SnLucLexer.Keywords.Off))
                throw ParserError(String.Concat("Invalid parameter: ", _lexer.StringValue, 
                    ". Expected: '", SnLucLexer.Keywords.On, "' or '", SnLucLexer.Keywords.Off, "'"));
            var value = _lexer.StringValue;
            _lexer.NextToken();
            return value;
        }

        private bool ParseControlExpWithoutParam()
        {
            // ControlExpWithoutParam  ==>  SimpleControlName
            // SimpleControlName       ==>  

            var name = ParseControlWithoutParamName();
            if (name == null)
                return false;
            InterpretControl(name, null);
            return true;
        }
        private string ParseControlWithoutParamName()
        {
            // ControlBySwitchName     ==>  COUNTONLY | QUICK
            string name = null;
            switch (_lexer.StringValue)
            {
                case SnLucLexer.Keywords.CountOnly: name = SnLucLexer.Keywords.CountOnly; break;
                case SnLucLexer.Keywords.Quick: name = SnLucLexer.Keywords.Quick; break;
            }
            if (name != null)
                _lexer.NextToken();
            return name;
        }

        // -----------------------------------------------------

        private void InterpretControl(string name, string param)
        {
            _controls.Add(new QueryControlParam { Name = name, Value = param });
        }

        private void AddBooleanClause(BooleanQuery boolQuery, Query query, BooleanClause.Occur occur)
        {
            var boolQ = query as BooleanQuery;
            if (boolQ == null)
            {
                boolQuery.Add(new BooleanClause(query, occur));
                return;
            }
            var clauses = boolQ.GetClauses();
            if (clauses.Length == 0)
            {
                throw ParserError("Empty BooleanQuery");
            }
            if (clauses.Length > 1)
            {
                boolQuery.Add(new BooleanClause(query, occur));
                return;
            }

            // boolQ has one clause: combine occurs
            var clause = (BooleanClause)clauses[0];
            var clauseOccur = clause.GetOccur();
            BooleanClause.Occur effectiveOccur;
            if (Operator == DefaultOperator.Or)
            {
                // in    cl      eff
                // null  _  ==>  _
                // null  +  ==>  +
                // null  -  ==>  -
                //    _  _  ==>  _
                //    _  +  ==>  +
                //    _  -  ==>  -
                //    +  _  ==>  +
                //    +  +  ==>  +
                //    +  -  ==>  -
                //    -  _  ==>  -
                //    -  +  ==>  -
                //    -  -  ==>  -
                if (occur == null || occur == BooleanClause.Occur.SHOULD)
                    effectiveOccur = clauseOccur;
                else if (occur == BooleanClause.Occur.MUST_NOT)
                    effectiveOccur = occur;
                else if (clauseOccur == BooleanClause.Occur.MUST_NOT)
                    effectiveOccur = clauseOccur;
                else
                    effectiveOccur = occur;
            }
            else
            {
                // in    cl      eff
                // null  _  ==>  _
                // null  +  ==>  +
                // null  -  ==>  -
                //    _  _  ==>  _
                //    _  +  ==>  _
                //    _  -  ==>  -
                //    +  _  ==>  +
                //    +  +  ==>  +
                //    +  -  ==>  -
                //    -  _  ==>  -
                //    -  +  ==>  -
                //    -  -  ==>  -
                if (occur == null)
                    effectiveOccur = clauseOccur;
                else if (occur == BooleanClause.Occur.MUST_NOT)
                    effectiveOccur = occur;
                else if (clauseOccur == BooleanClause.Occur.MUST_NOT)
                    effectiveOccur = clauseOccur;
                else
                    effectiveOccur = occur;
            }
            clause.SetOccur(effectiveOccur);
            boolQuery.Add(clause);
        }
        private Query CreateValueQuery(QueryFieldValue_OLD value)
        {
            var currentField = _currentField.Peek();
            string numval;
            switch (value.Datatype)
            {
                case IndexableDataType.String:
                    return CreateStringValueQuery(value, currentField);
                case IndexableDataType.Int: numval = NumericUtils.IntToPrefixCoded(value.IntValue); break;
                case IndexableDataType.Long: numval = NumericUtils.LongToPrefixCoded(value.LongValue); break;
                case IndexableDataType.Float: numval = NumericUtils.FloatToPrefixCoded(value.SingleValue); break;
                case IndexableDataType.Double: numval = NumericUtils.DoubleToPrefixCoded(value.DoubleValue); break;
                default:
                    throw new SnNotSupportedException("Unknown IndexableDataType enum value: " + value.Datatype);
            }
            var numterm = new Term(currentField.Name, numval);
            return new TermQuery(numterm);
        }
        private Query CreateStringValueQuery(QueryFieldValue_OLD value, FieldInfo currentField)
        {
            switch (value.Token)
            {
                case SnLucLexer.Token.Number:
                case SnLucLexer.Token.String:
                    if(value.StringValue == SnQuery.EmptyText)
                        return new TermQuery(new Term(currentField.Name, value.StringValue));
                    if (value.StringValue == SnQuery.EmptyInnerQueryText)
                        return new TermQuery(new Term("Id", NumericUtils.IntToPrefixCoded(0)));

                    var words = GetAnalyzedText(currentField.Name, value.StringValue);

                    if (words.Length == 0)
                        words = new String[] { String.Empty };
                    if (words.Length == 1)
                    {
                        var term = new Term(currentField.Name, words[0]);
                        if(value.FuzzyValue == null)
                            return new TermQuery(term);
                        return new FuzzyQuery(term, Convert.ToSingle(value.FuzzyValue));
                    }

                    var phraseQuery = new PhraseQuery();
                    foreach(var word in words)
                        phraseQuery.Add(new Term(currentField.Name, word));

                    if (value.FuzzyValue != null)
                    {
                        var slop = Convert.ToInt32(value.FuzzyValue.Value);
                        phraseQuery.SetSlop(slop);
                    }
                    return phraseQuery;
                case SnLucLexer.Token.WildcardString:
                    if (!value.StringValue.EndsWith("*"))
                        return new WildcardQuery(new Term(currentField.Name, value.StringValue));
                    var s = value.StringValue.TrimEnd('*');
                    if (s.Contains('?') || s.Contains('*'))
                        return new WildcardQuery(new Term(currentField.Name, value.StringValue));
                    return new PrefixQuery(new Term(currentField.Name, s));
                default:
                    throw new SnNotSupportedException("CreateValueQuery with Token: " + value.Token);
            }
        }
        private Query CreateRangeQuery(string fieldName, QueryFieldValue_OLD minValue, QueryFieldValue_OLD maxValue, bool includeLower, bool includeUpper)
        {
            if (minValue != null && minValue.StringValue == SnQuery.EmptyText && maxValue == null)
            {
                ParseEmptyQuery = true;
                return new TermQuery(new Term(fieldName, minValue.StringValue));
            }
            if (maxValue != null && maxValue.StringValue == SnQuery.EmptyText && minValue == null)
            {
                ParseEmptyQuery = true;
                return new TermQuery(new Term(fieldName, maxValue.StringValue));
            }
            if (minValue != null && minValue.StringValue == SnQuery.EmptyText)
                minValue = null;
            if (maxValue != null && maxValue.StringValue == SnQuery.EmptyText)
                maxValue = null;

            switch (minValue != null ? minValue.Datatype : maxValue.Datatype)
            {
                case IndexableDataType.String:
                    var lowerTerm = minValue == null ? null : minValue.StringValue.ToLower();
                    var upperTerm = maxValue == null ? null : maxValue.StringValue.ToLower();
                    return new TermRangeQuery(fieldName, lowerTerm, upperTerm, includeLower, includeUpper);
                case IndexableDataType.Int:
                    var lowerInt = minValue == null ? null : (Int32?)minValue.IntValue;
                    var upperInt = maxValue == null ? null : (Int32?)maxValue.IntValue;
                    return NumericRangeQuery.NewIntRange(fieldName, lowerInt, upperInt, includeLower, includeUpper);
                case IndexableDataType.Long:
                    var lowerLong = minValue == null ? null : (Int64?)minValue.LongValue;
                    var upperLong = maxValue == null ? null : (Int64?)maxValue.LongValue;
                    return NumericRangeQuery.NewLongRange(fieldName, 8, lowerLong, upperLong, includeLower, includeUpper);
                case IndexableDataType.Float:
                    var lowerFloat = minValue == null ? Single.MinValue : minValue.SingleValue;
                    var upperFloat = maxValue == null ? Single.MaxValue : maxValue.SingleValue;
                    return NumericRangeQuery.NewFloatRange(fieldName, lowerFloat, upperFloat, includeLower, includeUpper);
                case IndexableDataType.Double:
                    var lowerDouble = minValue == null ? Double.MinValue : minValue.DoubleValue;
                    var upperDouble = maxValue == null ? Double.MaxValue : maxValue.DoubleValue;
                    return NumericRangeQuery.NewDoubleRange(fieldName, 8, lowerDouble, upperDouble, includeLower, includeUpper);
                default:
                    throw new SnNotSupportedException("Unknown IndexableDataType: " + minValue.Datatype);
            }
        }

        private static string[] _headOnlyFields = SenseNet.ContentRepository.Storage.Node.GetHeadOnlyProperties();
        private void SetFieldLevel(FieldInfo field)
        {
            var fieldName = field.Name;
            var indexingInfo = field.IndexingInfo;
            QueryFieldLevel level;

            if (fieldName == IndexFieldName.AllText)
                level = QueryFieldLevel.BinaryOrFullText;
            else if(indexingInfo == null)
                level = QueryFieldLevel.BinaryOrFullText;
            else if (indexingInfo.FieldDataType == typeof(SenseNet.ContentRepository.Storage.BinaryData))
                level = QueryFieldLevel.BinaryOrFullText;
            else if (fieldName == IndexFieldName.InFolder || fieldName == IndexFieldName.InTree
                || fieldName == IndexFieldName.Type || fieldName == IndexFieldName.TypeIs
                || _headOnlyFields.Contains(fieldName))
                level = QueryFieldLevel.HeadOnly;
            else
                level = QueryFieldLevel.NoBinaryOrFullText;

            FieldLevel = (QueryFieldLevel)(Math.Max((int)level, (int)FieldLevel));
        }

        private string[] GetAnalyzedText(string field, string text)
        {
            var reader = new StringReader(text);
            var tokenStream = _masterAnalyzer.TokenStream(field, reader);
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

        private bool IsEof()
        {
            return _lexer.CurrentToken == SnLucLexer.Token.Eof;
        }
        private Exception SyntaxError()
        {
            return ParserError("Syntax error");
        }
        private Exception ParserError(string msg)
        {
            return new ParserException_OLD(String.Concat(msg, " (query: \"", _lexer.Source, "\")"), _lexer.CreateLastLineInfo());
        }
    }
}
