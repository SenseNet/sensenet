using System;
using System.Collections.Generic;
using SenseNet.Search.Parser.Nodes;

namespace SenseNet.Search.Parser
{
    internal class CqlParser : ISnQueryParser
    {
        public enum DefaultOperator { Or, And }

        public bool ParseEmptyQuery;

        internal class QueryControlParam
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
        private class FieldInfo
        {
            public static readonly FieldInfo Default = new FieldInfo { Name = IndexFieldName.AllText, OperatorToken = CqlLexer.Token.Colon, IsBinary = false };
            public string Name { get; set; }
            public CqlLexer.Token OperatorToken { get; set; }
            public bool IsBinary { get; set; }
        }

        public DefaultOperator Operator { get; private set; }

        private CqlLexer _lexer;
        private Stack<FieldInfo> _currentField = new Stack<FieldInfo>();
        private double _defaultSimilarity = 0.5;

        private List<QueryControlParam> _controls = new List<QueryControlParam>();

        private IQueryContext _context;

        public SnQuery Parse(string queryText, IQueryContext context)
        {
            _context = context;

            var rootNode = Parse(queryText, DefaultOperator.Or);

            //UNDONE:!!!! Use EmptyTermVisitor
            //if (ParseEmptyQuery)
            //{
            //    var visitor = new EmptyTermVisitor();
            //    rootNode = visitor.Visit(rootNode);
            //}

            var result = new SnQuery { Querytext = queryText, QueryTree = rootNode };

            var sortFields = new List<SortInfo>();
            foreach (var control in _controls)
            {
                //UNDONE:!!!! context.Settings is not used
                switch (control.Name)
                {
                    case CqlLexer.Keywords.Select:
                        result.Projection = control.Value;
                        break;
                    case CqlLexer.Keywords.Top:
                        result.Top = Convert.ToInt32(control.Value);
                        break;
                    case CqlLexer.Keywords.Skip:
                        result.Skip = Convert.ToInt32(control.Value);
                        break;
                    case CqlLexer.Keywords.Sort:
                        sortFields.Add(new SortInfo {FieldName = control.Value, Reverse = false});
                        break;
                    case CqlLexer.Keywords.ReverseSort:
                        sortFields.Add(new SortInfo { FieldName = control.Value, Reverse = true });
                        break;
                    case CqlLexer.Keywords.Autofilters:
                        result.EnableAutofilters = control.Value == CqlLexer.Keywords.On ? FilterStatus.Enabled : FilterStatus.Disabled;
                        break;
                    case CqlLexer.Keywords.Lifespan:
                        result.EnableLifespanFilter = control.Value == CqlLexer.Keywords.On ? FilterStatus.Enabled : FilterStatus.Disabled;
                        break;
                    case CqlLexer.Keywords.CountOnly:
                        result.CountOnly = true;
                        break;
                    case CqlLexer.Keywords.Quick:
                        result.QueryExecutionMode = QueryExecutionMode.Quick;
                        break;
                }
            }
            result.Sort = sortFields.ToArray();
            return result;
        }
        private SnQueryNode Parse(string queryText, DefaultOperator @operator)
        {
            _lexer = new CqlLexer(queryText);
            _controls.Clear();
            Operator = @operator;
            return ParseTopLevelQueryExpList();
        }


        /* ============================================================================ Recursive descent methods */

        private SnQueryNode ParseTopLevelQueryExpList()
        {
            // QueryExpList    ==>  QueryExp | QueryExpList QueryExp

            var queries = new List<SnQueryNode>();
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

            var boolQuery = new BoolList();
            foreach (var q in queries)
                AddBooleanClause(boolQuery, q, Occurence.Default);
            return boolQuery;

        }
        private SnQueryNode ParseQueryExpList()
        {
            // QueryExpList    ==>  QueryExp | QueryExpList QueryExp

            var queries = new List<SnQueryNode>();
            while (!IsEof() && _lexer.CurrentToken != CqlLexer.Token.RParen)
            {
                var q = ParseQueryExp();
                if (q != null)
                    queries.Add(q);
            }

            if (queries.Count == 0)
                return null;

            if (queries.Count == 1)
                return queries[0];

            var boolQuery = new BoolList();
            foreach (var q in queries)
                AddBooleanClause(boolQuery, q, Occurence.Default);
            return boolQuery;

        }
        private SnQueryNode ParseQueryExp()
        {
            // BinaryOr | ControlExp*
            while (_lexer.CurrentToken == CqlLexer.Token.ControlKeyword)
                ParseControlExp();
            if (IsEof())
                return null;
            return ParseBinaryOr();
        }
        private SnQueryNode ParseBinaryOr()
        {
            // BinaryOr        ==>  BinaryAnd | BinaryOr OR BinaryAnd
            var queries = new List<SnQueryNode>();
            queries.Add(ParseBinaryAnd());
            while (_lexer.CurrentToken == CqlLexer.Token.Or)
            {
                _lexer.NextToken();
                queries.Add(ParseBinaryAnd());
            }
            if (queries.Count == 1)
                return queries[0];
            var boolq = new BoolList();
            foreach (var query in queries)
                AddBooleanClause(boolq, query, Occurence.Should);
            return boolq;
        }
        private SnQueryNode ParseBinaryAnd()
        {
            // BinaryAnd       ==>  UnaryNot | BinaryAnd AND UnaryNot
            var queries = new List<SnQueryNode>();
            queries.Add(ParseUnaryNot());
            while (_lexer.CurrentToken == CqlLexer.Token.And)
            {
                _lexer.NextToken();
                queries.Add(ParseUnaryNot());
            }
            if (queries.Count == 1)
                return queries[0];
            var boolq = new BoolList();
            foreach (var query in queries)
                AddBooleanClause(boolq, query, Occurence.Must);
            return boolq;
        }
        private SnQueryNode ParseUnaryNot()
        {
            var not = false;
            if (_lexer.CurrentToken == CqlLexer.Token.Not)
            {
                _lexer.NextToken();
                not = true;
            }
            var query = ParseClause();
            if (!not)
                return query;
            var boolq = new BoolList();
            AddBooleanClause(boolq, query, Occurence.MustNot);
            return boolq;
        }
        private SnQueryNode ParseClause()
        {
            var occur = ParseOccur();
            var query = ParseQueryExpGroup();
            var boost = ParseBoost();
            if (boost != null)
                query.Boost = Convert.ToSingle(boost.Value);
            if (occur == Occurence.Default || occur == Occurence.Should)
                return query;
            var boolq = new BoolList();
            AddBooleanClause(boolq, query, occur);
            return boolq;
        }
        private Occurence ParseOccur()
        {
            if (_lexer.CurrentToken == CqlLexer.Token.Minus)
            {
                _lexer.NextToken();
                return Occurence.MustNot;
            }
            if (_lexer.CurrentToken == CqlLexer.Token.Plus)
            {
                _lexer.NextToken();
                return Occurence.Must;
            }
            if (Operator == DefaultOperator.And)
                return Occurence.Must;
            return Occurence.Default;
        }
        private SnQueryNode ParseQueryExpGroup()
        {
            // QueryExpGroup   ==>  LPAREN ClauseList RPAREN | TermExp
            if (_lexer.CurrentToken != CqlLexer.Token.LParen)
                return ParseTermExp();
            _lexer.NextToken();

            var clauses = ParseQueryExpList();
            if (_lexer.CurrentToken != CqlLexer.Token.RParen)
                throw ParserError("Missing ')'");
            _lexer.NextToken();
            return clauses;
        }

        private SnQueryNode ParseTermExp()
        {
            // TermExp           ==>  UnaryTermExp | BinaryTermExp | QueryExpGroup | DefaultFieldExp

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

            var result = ParseUnaryTermExp();
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
        private SnQueryNode ParseUnaryTermExp()
        {
            // UnaryTermExp      ==>  UnaryFieldHead UnaryFieldValue | ControlExp
            // UnaryFieldHead    ==>  STRING COLON | STRING NEQ
            // UnaryFieldValue   ==>  ValueGroup | Range

            var fieldInfo = _currentField.Peek();
            if (fieldInfo == null)
                return null;
            if (fieldInfo.IsBinary)
                return null;

            if (fieldInfo.OperatorToken == CqlLexer.Token.NEQ)
            {
                var value = ParseExactValue(true);
                var bq = new BoolList();
                bq.Clauses.Add(new Bool(CreateValueQuery(value), Occurence.MustNot));
                return bq;
            }

            var result = ParseValueGroup();
            if (result != null)
                return result;

            result = ParseRange();
            if (result != null)
                return result;

            throw ParserError(String.Concat("Unexpected '", _lexer.StringValue, "'"));
        }
        private SnQueryNode ParseBinaryTermExp()
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
                case CqlLexer.Token.LT:
                    return CreateRangeQuery(fieldInfo.Name, null, value, true, false);
                case CqlLexer.Token.LTE:
                    return CreateRangeQuery(fieldInfo.Name, null, value, true, true);
                case CqlLexer.Token.GT:
                    return CreateRangeQuery(fieldInfo.Name, value, null, false, true);
                case CqlLexer.Token.GTE:
                    return CreateRangeQuery(fieldInfo.Name, value, null, true, true);
            }

            throw ParserError("Unexpected OperatorToken: " + fieldInfo.OperatorToken);
        }
        private SnQueryNode ParseDefaultFieldExp()
        {
            // DefaultFieldExp   ==>  ValueGroup
            return ParseValueGroup();
        }

        private SnQueryNode ParseValueGroup()
        {
            // ValueGroup        ==>  LPAREN ValueExpList RPAREN | FuzzyValue

            if (_lexer.CurrentToken != CqlLexer.Token.LParen)
            {
                var value = ParseFuzzyValue();
                if (value != null)
                    return CreateValueQuery(value);
                return null;
            }
            _lexer.NextToken();

            var result = ParseValueExpList();

            if (_lexer.CurrentToken != CqlLexer.Token.RParen)
                throw ParserError("Expcted: ')'");
            _lexer.NextToken();

            return result;
        }
        private SnQueryNode ParseValueExpList()
        {
            // ValueExpList      ==>  ValueExp | ValueExpList ValueExp

            BoolList boolQuery;
            var first = ParseValueExp();

            if (_lexer.CurrentToken == CqlLexer.Token.RParen)
                return first;

            boolQuery = new BoolList();
            AddBooleanClause(boolQuery, first, Occurence.Default);

            while (!IsEof() && _lexer.CurrentToken != CqlLexer.Token.RParen)
            {
                var q = ParseValueExp();
                AddBooleanClause(boolQuery, q, Occurence.Default);
            }

            return boolQuery;
        }
        private SnQueryNode ParseValueExp()
        {
            // ValueExp          ==>  ValueBinaryOr
            return ParseValueBinaryOr();
        }
        private SnQueryNode ParseValueBinaryOr()
        {
            // ValueBinaryOr     ==>  ValueBinaryAnd | ValueBinaryOr OR ValueBinaryAnd
            var queries = new List<SnQueryNode>();
            queries.Add(ParseValueBinaryAnd());
            while (_lexer.CurrentToken == CqlLexer.Token.Or)
            {
                _lexer.NextToken();
                queries.Add(ParseValueBinaryAnd());
            }
            if (queries.Count == 1)
                return queries[0];
            var boolq = new BoolList();
            foreach (var query in queries)
                AddBooleanClause(boolq, query, Occurence.Should);
            return boolq;
        }
        private SnQueryNode ParseValueBinaryAnd()
        {
            // ValueBinaryAnd    ==>  ValueUnaryNot | ValueBinaryAnd AND ValueUnaryNot
            var queries = new List<SnQueryNode>();
            queries.Add(ParseValueUnaryNot());
            while (_lexer.CurrentToken == CqlLexer.Token.And)
            {
                _lexer.NextToken();
                queries.Add(ParseValueUnaryNot());
            }
            if (queries.Count == 1)
                return queries[0];
            var boolq = new BoolList();
            foreach (var query in queries)
                AddBooleanClause(boolq, query, Occurence.Must);
            return boolq;
        }
        private SnQueryNode ParseValueUnaryNot()
        {
            // ValueUnaryNot     ==>  ValueClause | NOT ValueClause
            // ValueClause       ==>  [Occur] ValueGroup
            var not = false;
            if (_lexer.CurrentToken == CqlLexer.Token.Not)
            {
                _lexer.NextToken();
                not = true;
            }
            var query = ParseValueClause();
            if (!not)
                return query;
            var boolq = new BoolList();
            AddBooleanClause(boolq, query, Occurence.MustNot);
            return boolq;
        }
        private SnQueryNode ParseValueClause()
        {
            // ValueClause       ==>  [Occur] ValueGroup
            var occur = ParseOccur();
            var query = ParseValueGroup();
            query.Boost = ParseBoost();
            if (occur == Occurence.Default || occur == Occurence.Should)
                return query;
            var boolq = new BoolList();
            AddBooleanClause(boolq, query, occur);
            return boolq;
        }

        /* ===================================================== */

        private FieldInfo ParseFieldHead()
        {
            // UnaryFieldHead    ==>  STRING COLON | STRING COLON NEQ
            // BinaryFieldHead   ==>  STRING COLON LT | STRING COLON GT | STRING COLON LTE | STRING COLON GTE

            FieldInfo fieldInfo = null;
            if (_lexer.CurrentToken == CqlLexer.Token.Field)
            {
                var name = _lexer.StringValue;
                if (name == "Password" || name == "PasswordHash")
                    throw new InvalidOperationException("Cannot search by '" + name + "' field name");


                fieldInfo = new FieldInfo { Name = name };
                fieldInfo.OperatorToken = _lexer.CurrentToken;
                SetFieldLevel(fieldInfo);

                _lexer.NextToken();
                if (_lexer.CurrentToken != CqlLexer.Token.Colon)
                    throw new InvalidOperationException("#### ParseFieldHead ####");


                _lexer.NextToken();
                switch (_lexer.CurrentToken)
                {
                    case CqlLexer.Token.NEQ:
                        fieldInfo.IsBinary = false;
                        fieldInfo.OperatorToken = _lexer.CurrentToken;
                        _lexer.NextToken();
                        break;
                    case CqlLexer.Token.LT:
                    case CqlLexer.Token.LTE:
                    case CqlLexer.Token.GT:
                    case CqlLexer.Token.GTE:
                        fieldInfo.IsBinary = true;
                        fieldInfo.OperatorToken = _lexer.CurrentToken;
                        _lexer.NextToken();
                        break;
                }
            }

            return fieldInfo;
        }
        private SnQueryNode ParseRange()
        {
            // Range        ==>  RangeStart ExactValue TO ExactValue RangeEnd
            var start = ParseRangeStart();
            if (start == null)
                return null;
            var minValue = ParseExactValue(false);
            if (_lexer.CurrentToken != CqlLexer.Token.To)
                throw SyntaxError();
            _lexer.NextToken();
            var maxValue = ParseExactValue(false);
            var end = ParseRangeEnd();
            if (end == null)
                throw ParserError("Unterminated Range expression");

            var fieldInfo = _currentField.Peek();
            var fieldName = fieldInfo.Name;
            var includeLower = start.Value;
            var includeUpper = end.Value;

            if (minValue == null && maxValue == null)
                throw ParserError("Invalid range.");

            return CreateRangeQuery(fieldName, minValue, maxValue, includeLower, includeUpper);
        }
        private QueryFieldValue ParseFuzzyValue()
        {
            // FuzzyValue   ==>  Value [Fuzzy]
            var val = ParseValue(false);
            if (val == null)
                return null;
            val.FuzzyValue = ParseFuzzy(val.IsPhrase);
            return val;
        }
        private QueryFieldValue ParseValue(bool throwEx)
        {
            // Value        ==>  ExactValue | WILDCARDSTRING
            var val = ParseExactValue(false);
            if (val != null)
                return val;
            if (_lexer.CurrentToken == CqlLexer.Token.WildcardString)
            {
                val = new QueryFieldValue(_lexer.StringValue.ToLower(), _lexer.CurrentToken, _lexer.IsPhrase);
                _lexer.NextToken();
                return val;
            }
            if (!throwEx)
                return null;
            throw ParserError($"Unexpected {_lexer.CurrentToken}. Expected: STRING | NUMBER | WILDCARDSTRING");
        }
        private QueryFieldValue ParseExactValue(bool throwEx)
        {
            // ExactValue   ==>  STRING | NUMBER | EMPTY
            if (_lexer.StringValue == SnQuery.EmptyText)
            {
                ParseEmptyQuery = true;
                var fieldVal = new QueryFieldValue(_lexer.StringValue, _lexer.CurrentToken, _lexer.IsPhrase);
                _lexer.NextToken();
                return fieldVal;
            }
            if (_lexer.CurrentToken != CqlLexer.Token.String && _lexer.CurrentToken != CqlLexer.Token.Number)
            {
                if (throwEx)
                    throw ParserError(String.Concat("Unexpected ", _lexer.CurrentToken, ". Expected: STRING | NUMBER"));
                return null;
            }

            var field = _currentField.Peek();
            var fieldName = field.Name;
            var val = new QueryFieldValue(_lexer.StringValue, _lexer.CurrentToken, _lexer.IsPhrase);
            if (fieldName != IndexFieldName.AllText && _lexer.StringValue != SnQuery.EmptyInnerQueryText)
            {
                var info = _context.GetPerFieldIndexingInfo(fieldName);
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
            if (_lexer.CurrentToken != CqlLexer.Token.Tilde)
                return null;
            _lexer.NextToken();
            if (isPhrase)
            {
                if (_lexer.CurrentToken != CqlLexer.Token.Number)
                    throw ParserError("Missing proximity value");
                _lexer.NextToken();
                return _lexer.NumberValue;
            }
            else
            {
                if (_lexer.CurrentToken != CqlLexer.Token.Number)
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
            if (_lexer.CurrentToken != CqlLexer.Token.Circ)
                return null;
            _lexer.NextToken();
            if (_lexer.CurrentToken != CqlLexer.Token.Number)
                throw SyntaxError();
            _lexer.NextToken();
            return _lexer.NumberValue;
        }
        private bool? ParseRangeStart()
        {
            // RangeStart   ==>  LBRACKET | LBRACE
            if (_lexer.CurrentToken == CqlLexer.Token.LBracket)//excl
            {
                _lexer.NextToken();
                return true;
            }
            if (_lexer.CurrentToken == CqlLexer.Token.LBrace)//incl
            {
                _lexer.NextToken();
                return false;
            }
            return null;
        }
        private bool? ParseRangeEnd()
        {
            // RangeEnd     ==>  RBRACKET | RBRACE
            if (_lexer.CurrentToken == CqlLexer.Token.RBracket)//excl
            {
                _lexer.NextToken();
                return true;
            }
            if (_lexer.CurrentToken == CqlLexer.Token.RBrace)//incl
            {
                _lexer.NextToken();
                return false;
            }
            return null;
        }


        /* ============================================================================ Parse control */

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
            if (_lexer.CurrentToken != CqlLexer.Token.Colon)
                throw ParserError("Expected: Colon (':')");
            _lexer.NextToken();
            if (_lexer.CurrentToken != CqlLexer.Token.Number)
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
                case CqlLexer.Keywords.Top: name = CqlLexer.Keywords.Top; break;
                case CqlLexer.Keywords.Skip: name = CqlLexer.Keywords.Skip; break;
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
            if (_lexer.CurrentToken != CqlLexer.Token.Colon)
                throw ParserError("Expected: Colon (':')");
            _lexer.NextToken();
            if (_lexer.CurrentToken != CqlLexer.Token.String)
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
                case CqlLexer.Keywords.Select: name = CqlLexer.Keywords.Select; break;
                case CqlLexer.Keywords.Sort: name = CqlLexer.Keywords.Sort; break;
                case CqlLexer.Keywords.ReverseSort: name = CqlLexer.Keywords.ReverseSort; break;
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
            if (_lexer.CurrentToken != CqlLexer.Token.Colon)
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
                case CqlLexer.Keywords.Autofilters: name = CqlLexer.Keywords.Autofilters; break;
                case CqlLexer.Keywords.Lifespan: name = CqlLexer.Keywords.Lifespan; break;
            }
            if (name != null)
                _lexer.NextToken();
            return name;

        }
        private string ParseSwitchParam()
        {
            // SwitchParam             ==>  ON | OFF
            if ((_lexer.CurrentToken != CqlLexer.Token.String)
                || (_lexer.StringValue != CqlLexer.Keywords.On && _lexer.StringValue != CqlLexer.Keywords.Off))
                throw ParserError(String.Concat("Invalid parameter: ", _lexer.StringValue,
                    ". Expected: '", CqlLexer.Keywords.On, "' or '", CqlLexer.Keywords.Off, "'"));
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
                case CqlLexer.Keywords.CountOnly: name = CqlLexer.Keywords.CountOnly; break;
                case CqlLexer.Keywords.Quick: name = CqlLexer.Keywords.Quick; break;
            }
            if (name != null)
                _lexer.NextToken();
            return name;
        }

        private void InterpretControl(string name, string param)
        {
            _controls.Add(new QueryControlParam { Name = name, Value = param });
        }

        /* ============================================================================ */

        private void AddBooleanClause(BoolList boolNode, SnQueryNode query, Occurence occur)
        {
            var boolQ = query as BoolList;
            if (boolQ == null)
            {
                boolNode.Clauses.Add(new Bool(query, occur));
                return;
            }
            var clauses = boolQ.Clauses;
            if (clauses.Count == 0)
            {
                throw ParserError("Empty BooleanNode");
            }
            if (clauses.Count > 1)
            {
                boolNode.Clauses.Add(new Bool(query, occur));
                return;
            }

            // boolQ has one clause: combine occurs
            var clause = clauses[0];
            var clauseOccur = clause.Occur;
            Occurence effectiveOccur;
            if (Operator == DefaultOperator.Or)
            {
                //   in  cl      eff
                //    ?  _  ==>  _
                //    ?  +  ==>  +
                //    ?  -  ==>  -
                //    _  _  ==>  _
                //    _  +  ==>  +
                //    _  -  ==>  -
                //    +  _  ==>  +
                //    +  +  ==>  +
                //    +  -  ==>  -
                //    -  _  ==>  -
                //    -  +  ==>  -
                //    -  -  ==>  -
                if (occur == Occurence.Default || occur == Occurence.Should)
                    effectiveOccur = clauseOccur;
                else if (occur == Occurence.MustNot)
                    effectiveOccur = occur;
                else if (clauseOccur == Occurence.MustNot)
                    effectiveOccur = clauseOccur;
                else
                    effectiveOccur = occur;
            }
            else
            {
                //   in  cl      eff
                //    ?  _  ==>  _
                //    ?  +  ==>  +
                //    ?  -  ==>  -
                //    _  _  ==>  _
                //    _  +  ==>  _
                //    _  -  ==>  -
                //    +  _  ==>  +
                //    +  +  ==>  +
                //    +  -  ==>  -
                //    -  _  ==>  -
                //    -  +  ==>  -
                //    -  -  ==>  -
                if (occur == Occurence.Default)
                    effectiveOccur = clauseOccur;
                else if (occur == Occurence.MustNot)
                    effectiveOccur = occur;
                else if (clauseOccur == Occurence.MustNot)
                    effectiveOccur = clauseOccur;
                else
                    effectiveOccur = occur;
            }
            clause.Occur = effectiveOccur;
            boolNode.Clauses.Add(clause);
        }

        private SnQueryNode CreateValueQuery(QueryFieldValue value)
        {
            var currentField = _currentField.Peek();
            var fieldName = currentField.Name;
            switch (value.Datatype)
            {
                case IndexableDataType.String:
                    return CreateStringValueQuery(value, currentField);
                case IndexableDataType.Int: return new Numeric<int>(fieldName, value.IntValue); //UNDONE: Use NumericUtils.IntToPrefixCoded(value) in the compiler
                case IndexableDataType.Long: return new Numeric<long>(fieldName, value.LongValue); //UNDONE: Use NumericUtils.LongToPrefixCoded(value) in the compiler
                case IndexableDataType.Float: return new Numeric<float>(fieldName, value.SingleValue); //UNDONE: Use NumericUtils.FloatToPrefixCoded(value) in the compiler
                case IndexableDataType.Double: return new Numeric<double>(fieldName, value.DoubleValue); //UNDONE: Use NumericUtils.DoubleToPrefixCoded(value) in the compiler
                default:
                    throw ParserError("Unknown IndexableDataType enum value: " + value.Datatype);
            }
        }
        private SnQueryNode CreateStringValueQuery(QueryFieldValue value, FieldInfo currentField)
        {
            switch (value.Token)
            {
                case CqlLexer.Token.Number:
                case CqlLexer.Token.String:
                    if (value.StringValue == SnQuery.EmptyText)
                        return new Text(currentField.Name, value.StringValue);
                    if (value.StringValue == SnQuery.EmptyInnerQueryText)
                        return new Numeric<int>(IndexFieldName.NodeId, 0);
                    return new Text(currentField.Name, value.StringValue, value.FuzzyValue);
                case CqlLexer.Token.WildcardString:
                    return new Text(currentField.Name, value.StringValue, value.FuzzyValue);
                default:
                    throw ParserError("CreateValueQuery with Token: " + value.Token);
            }
        }
        private SnQueryNode CreateRangeQuery(string fieldName, QueryFieldValue minValue, QueryFieldValue maxValue, bool includeLower, bool includeUpper)
        {
            if (minValue != null && minValue.StringValue == SnQuery.EmptyText && maxValue == null)
            {
                ParseEmptyQuery = true;
                return new Text(fieldName, minValue.StringValue);
            }
            if (maxValue != null && maxValue.StringValue == SnQuery.EmptyText && minValue == null)
            {
                ParseEmptyQuery = true;
                return new Text(fieldName, maxValue.StringValue);
            }
            if (minValue != null && minValue.StringValue == SnQuery.EmptyText)
                minValue = null;
            if (maxValue != null && maxValue.StringValue == SnQuery.EmptyText)
                maxValue = null;

            if (minValue == null && maxValue == null)
                throw ParserError("Invalid range: the minimum and the maximum value are cannot be null/empty together.");

            switch (minValue?.Datatype ?? maxValue.Datatype)
            {
                case IndexableDataType.String:
                    var lowerTerm = minValue?.StringValue.ToLower();
                    var upperTerm = maxValue?.StringValue.ToLower();
                    return new Range<string>(fieldName, lowerTerm, upperTerm, includeLower, includeUpper);
                case IndexableDataType.Int:
                    var lowerInt = minValue?.IntValue ?? 0;
                    var upperInt = maxValue?.IntValue ?? 0;
                    return new Range<int>(fieldName, lowerInt, upperInt, includeLower, includeUpper);
                case IndexableDataType.Long:
                    var lowerLong = minValue?.LongValue ?? 0;
                    var upperLong = maxValue?.LongValue ?? 0;
                    return new Range<long>(fieldName, lowerLong, upperLong, includeLower, includeUpper);
                case IndexableDataType.Float:
                    var lowerFloat = minValue?.SingleValue ?? float.MinValue;
                    var upperFloat = maxValue?.SingleValue ?? float.MaxValue;
                    return new Range<float>(fieldName, lowerFloat, upperFloat, includeLower, includeUpper);
                case IndexableDataType.Double:
                    var lowerDouble = minValue?.DoubleValue ?? double.MinValue;
                    var upperDouble = maxValue?.DoubleValue ?? double.MaxValue;
                    return new Range<double>(fieldName, lowerDouble, upperDouble, includeLower, includeUpper);
                default:
                    throw ParserError("Unknown IndexableDataType: " + (minValue ?? maxValue).Datatype);
            }
        }

        private void SetFieldLevel(FieldInfo field)
        {
            //UNDONE:!!!! implement SetFieldLevel in a Visitor
            throw new NotImplementedException();

            //var fieldName = field.Name;
            //QueryFieldLevel level;

            //if (fieldName == IndexFieldName.AllText)
            //    level = QueryFieldLevel.BinaryOrFullText;
            //else if (indexingInfo == null)
            //    level = QueryFieldLevel.BinaryOrFullText;
            //else if (indexingInfo.FieldDataType == typeof(SenseNet.ContentRepository.Storage.BinaryData))
            //    level = QueryFieldLevel.BinaryOrFullText;
            //else if (fieldName == IndexFieldName.InFolder || fieldName == IndexFieldName.InTree
            //    || fieldName == IndexFieldName.Type || fieldName == IndexFieldName.TypeIs
            //    || _headOnlyFields.Contains(fieldName))
            //    level = QueryFieldLevel.HeadOnly;
            //else
            //    level = QueryFieldLevel.NoBinaryOrFullText;

            //FieldLevel = (QueryFieldLevel)(Math.Max((int)level, (int)FieldLevel));
        }

        private bool IsEof()
        {
            return _lexer.CurrentToken == CqlLexer.Token.Eof;
        }
        private Exception SyntaxError()
        {
            return ParserError("Syntax error");
        }
        private Exception ParserError(string msg)
        {
            return new ParserException($"{msg}, (query: \"{_lexer.Source}\")", _lexer.CreateLastLineInfo());
        }

    }
}
