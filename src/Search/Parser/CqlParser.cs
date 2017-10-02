using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SenseNet.Search.Parser.Predicates;

namespace SenseNet.Search.Parser
{
    internal class CqlParser : ISnQueryParser
    {
        public enum DefaultOperator { Or, And }

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

        private DefaultOperator _defaultOperator = DefaultOperator.Or;

        private IQueryContext _context;
        private CqlLexer _lexer;
        private readonly Stack<FieldInfo> _currentField = new Stack<FieldInfo>();
        private readonly List<QueryControlParam> _controls = new List<QueryControlParam>();
        //private readonly List<string> _usedFieldNames = new List<string>();
        private bool _hasEmptyQuery;

        public SnQuery Parse(string queryText, IQueryContext context)
        {
            _context = context;
            var rootNode = ParsePredicate(queryText);

            if (_hasEmptyQuery)
                rootNode = new EmptyPredicateVisitor().Visit(rootNode);

            var result = new SnQuery { Querytext = queryText, QueryTree = rootNode };

            var sortFields = new List<SortInfo>();
            foreach (var control in _controls)
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (control.Name)
                {
                    case Cql.Keyword.Select:
                        result.Projection = control.Value;
                        break;
                    case Cql.Keyword.Top:
                        result.Top = Convert.ToInt32(control.Value);
                        break;
                    case Cql.Keyword.Skip:
                        result.Skip = Convert.ToInt32(control.Value);
                        break;
                    case Cql.Keyword.Sort:
                        sortFields.Add(new SortInfo(control.Value));
                        break;
                    case Cql.Keyword.ReverseSort:
                        sortFields.Add(new SortInfo(control.Value, true));
                        break;
                    case Cql.Keyword.Autofilters:
                        result.EnableAutofilters = control.Value == Cql.Keyword.On ? FilterStatus.Enabled : FilterStatus.Disabled;
                        break;
                    case Cql.Keyword.Lifespan:
                        result.EnableLifespanFilter = control.Value == Cql.Keyword.On ? FilterStatus.Enabled : FilterStatus.Disabled;
                        break;
                    case Cql.Keyword.CountOnly:
                        result.CountOnly = true;
                        break;
                    case Cql.Keyword.Quick:
                        result.QueryExecutionMode = QueryExecutionMode.Quick;
                        break;
                    case Cql.Keyword.AllVersions:
                        result.AllVersions = true;
                        break;
                }
            }
            result.Sort = sortFields.ToArray();
            AggregateSettings(result, context.Settings);

            return result;
        }
        private SnQueryPredicate ParsePredicate(string queryText)
        {
            _lexer = new CqlLexer(queryText);
            _controls.Clear();
            return ParseTopLevelQueryExpList();
        }
        private static void AggregateSettings(SnQuery query, QuerySettings settings)
        {
            query.Top = Math.Min(
                settings.Top == 0 ? int.MaxValue : settings.Top,
                query.Top == 0 ? int.MaxValue : query.Top);
            if (settings.Skip > 0)
                query.Skip = settings.Skip;
            if (settings.Sort != null && settings.Sort.Any())
                query.Sort = settings.Sort.ToArray();
            if (settings.EnableAutofilters != FilterStatus.Default)
                query.EnableAutofilters = settings.EnableAutofilters;
            if (settings.EnableLifespanFilter != FilterStatus.Default)
                query.EnableLifespanFilter = settings.EnableLifespanFilter;
            if (settings.QueryExecutionMode != QueryExecutionMode.Default)
                query.QueryExecutionMode = settings.QueryExecutionMode;
            if (settings.AllVersions)
                query.AllVersions = true;
        }

        /* ============================================================================ Recursive descent methods */

        private SnQueryPredicate ParseTopLevelQueryExpList()
        {
            // QueryExpList    ==>  QueryExp | QueryExpList QueryExp

            var queries = new List<SnQueryPredicate>();
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

            var boolQuery = new LogicalPredicate();
            foreach (var q in queries)
                AddBooleanClause(boolQuery, q, Occurence.Default);
            return boolQuery;

        }
        private SnQueryPredicate ParseQueryExpList()
        {
            // QueryExpList    ==>  QueryExp | QueryExpList QueryExp

            var queries = new List<SnQueryPredicate>();
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

            var boolQuery = new LogicalPredicate();
            foreach (var q in queries)
                AddBooleanClause(boolQuery, q, Occurence.Default);
            return boolQuery;

        }
        private SnQueryPredicate ParseQueryExp()
        {
            // BinaryOr | ControlExp*
            while (_lexer.CurrentToken == CqlLexer.Token.ControlKeyword)
                ParseControlExp();
            if (IsEof())
                return null;
            return ParseBinaryOr();
        }
        private SnQueryPredicate ParseBinaryOr()
        {
            // BinaryOr        ==>  BinaryAnd | BinaryOr OR BinaryAnd
            var queries = new List<SnQueryPredicate>();
            queries.Add(ParseBinaryAnd());
            while (_lexer.CurrentToken == CqlLexer.Token.Or)
            {
                _lexer.NextToken();
                queries.Add(ParseBinaryAnd());
            }
            if (queries.Count == 1)
                return queries[0];
            var boolq = new LogicalPredicate();
            foreach (var query in queries)
                AddBooleanClause(boolq, query, Occurence.Should);
            return boolq;
        }
        private SnQueryPredicate ParseBinaryAnd()
        {
            // BinaryAnd       ==>  UnaryNot | BinaryAnd AND UnaryNot
            var queries = new List<SnQueryPredicate>();
            queries.Add(ParseUnaryNot());
            while (_lexer.CurrentToken == CqlLexer.Token.And)
            {
                _lexer.NextToken();
                queries.Add(ParseUnaryNot());
            }
            if (queries.Count == 1)
                return queries[0];
            var boolq = new LogicalPredicate();
            foreach (var query in queries)
                AddBooleanClause(boolq, query, Occurence.Must);
            return boolq;
        }
        private SnQueryPredicate ParseUnaryNot()
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
            var boolq = new LogicalPredicate();
            AddBooleanClause(boolq, query, Occurence.MustNot);
            return boolq;
        }
        private SnQueryPredicate ParseClause()
        {
            var occur = ParseOccur();
            var query = ParseQueryExpGroup();
            var boost = ParseBoost();
            if (boost != null)
                query.Boost = Convert.ToSingle(boost.Value);
            if (occur == Occurence.Default || occur == Occurence.Should)
                return query;
            var boolq = new LogicalPredicate();
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
            if (_defaultOperator == DefaultOperator.And)
                return Occurence.Must;
            return Occurence.Default;
        }
        private SnQueryPredicate ParseQueryExpGroup()
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

        private SnQueryPredicate ParseTermExp()
        {
            // TermExp           ==>  UnaryTermExp | BinaryTermExp | QueryExpGroup | DefaultFieldExp

            var fieldInfo = ParseFieldHead();
            if (fieldInfo != null)
                _currentField.Push(fieldInfo);
            else if (_currentField.Count != 0)
                _currentField.Push(_currentField.Peek());
            else
                _currentField.Push(FieldInfo.Default);

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
        private SnQueryPredicate ParseUnaryTermExp()
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
                var bq = new LogicalPredicate();
                bq.Clauses.Add(new LogicalClause(CreateValueQuery(value), Occurence.MustNot));
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
        private SnQueryPredicate ParseBinaryTermExp()
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
        private SnQueryPredicate ParseDefaultFieldExp()
        {
            // DefaultFieldExp   ==>  ValueGroup
            return ParseValueGroup();
        }

        private SnQueryPredicate ParseValueGroup()
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
        private SnQueryPredicate ParseValueExpList()
        {
            // ValueExpList      ==>  ValueExp | ValueExpList ValueExp

            LogicalPredicate boolQuery;
            var first = ParseValueExp();

            if (_lexer.CurrentToken == CqlLexer.Token.RParen)
                return first;

            boolQuery = new LogicalPredicate();
            AddBooleanClause(boolQuery, first, Occurence.Default);

            while (!IsEof() && _lexer.CurrentToken != CqlLexer.Token.RParen)
            {
                var q = ParseValueExp();
                AddBooleanClause(boolQuery, q, Occurence.Default);
            }

            return boolQuery;
        }
        private SnQueryPredicate ParseValueExp()
        {
            // ValueExp          ==>  ValueBinaryOr
            return ParseValueBinaryOr();
        }
        private SnQueryPredicate ParseValueBinaryOr()
        {
            // ValueBinaryOr     ==>  ValueBinaryAnd | ValueBinaryOr OR ValueBinaryAnd
            var queries = new List<SnQueryPredicate>();
            queries.Add(ParseValueBinaryAnd());
            while (_lexer.CurrentToken == CqlLexer.Token.Or)
            {
                _lexer.NextToken();
                queries.Add(ParseValueBinaryAnd());
            }
            if (queries.Count == 1)
                return queries[0];
            var boolq = new LogicalPredicate();
            foreach (var query in queries)
                AddBooleanClause(boolq, query, Occurence.Should);
            return boolq;
        }
        private SnQueryPredicate ParseValueBinaryAnd()
        {
            // ValueBinaryAnd    ==>  ValueUnaryNot | ValueBinaryAnd AND ValueUnaryNot
            var queries = new List<SnQueryPredicate>();
            queries.Add(ParseValueUnaryNot());
            while (_lexer.CurrentToken == CqlLexer.Token.And)
            {
                _lexer.NextToken();
                queries.Add(ParseValueUnaryNot());
            }
            if (queries.Count == 1)
                return queries[0];
            var boolq = new LogicalPredicate();
            foreach (var query in queries)
                AddBooleanClause(boolq, query, Occurence.Must);
            return boolq;
        }
        private SnQueryPredicate ParseValueUnaryNot()
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
            var boolq = new LogicalPredicate();
            AddBooleanClause(boolq, query, Occurence.MustNot);
            return boolq;
        }
        private SnQueryPredicate ParseValueClause()
        {
            // ValueClause       ==>  [Occur] ValueGroup
            var occur = ParseOccur();
            var query = ParseValueGroup();
            query.Boost = ParseBoost();
            if (occur == Occurence.Default || occur == Occurence.Should)
                return query;
            var boolq = new LogicalPredicate();
            AddBooleanClause(boolq, query, occur);
            return boolq;
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
            return false;
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
                case Cql.Keyword.Top: name = Cql.Keyword.Top; break;
                case Cql.Keyword.Skip: name = Cql.Keyword.Skip; break;
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
                case Cql.Keyword.Select: name = Cql.Keyword.Select; break;
                case Cql.Keyword.Sort: name = Cql.Keyword.Sort; break;
                case Cql.Keyword.ReverseSort: name = Cql.Keyword.ReverseSort; break;
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
                case Cql.Keyword.Autofilters: name = Cql.Keyword.Autofilters; break;
                case Cql.Keyword.Lifespan: name = Cql.Keyword.Lifespan; break;
            }
            if (name != null)
                _lexer.NextToken();
            return name;

        }
        private string ParseSwitchParam()
        {
            // SwitchParam             ==>  ON | OFF
            if ((_lexer.CurrentToken != CqlLexer.Token.String)
                || (_lexer.StringValue != Cql.Keyword.On && _lexer.StringValue != Cql.Keyword.Off))
                throw ParserError(String.Concat("Invalid parameter: ", _lexer.StringValue,
                    ". Expected: '", Cql.Keyword.On, "' or '", Cql.Keyword.Off, "'"));
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
                case Cql.Keyword.CountOnly: name = Cql.Keyword.CountOnly; break;
                case Cql.Keyword.Quick: name = Cql.Keyword.Quick; break;
                case Cql.Keyword.AllVersions: name = Cql.Keyword.AllVersions; break;
            }
            if (name != null)
                _lexer.NextToken();
            return name;
        }

        private void InterpretControl(string name, string param)
        {
            _controls.Add(new QueryControlParam { Name = name, Value = param });
        }

        /* ============================================================================ helpers */

        private FieldInfo ParseFieldHead()
        {
            // UnaryFieldHead    ==>  STRING COLON | STRING COLON NEQ
            // BinaryFieldHead   ==>  STRING COLON LT | STRING COLON GT | STRING COLON LTE | STRING COLON GTE

            FieldInfo fieldInfo = null;
            if (_lexer.CurrentToken == CqlLexer.Token.Field)
            {
                var name = _lexer.StringValue;
                if (IndexDocument.ForbiddenFields.Contains(name))
                    throw new InvalidOperationException("Cannot search by '" + name + "' field name");


                fieldInfo = new FieldInfo { Name = name };
                fieldInfo.OperatorToken = _lexer.CurrentToken;

                //if (!_usedFieldNames.Contains(name))
                //    _usedFieldNames.Add(name);

                _lexer.NextToken();
                if (_lexer.CurrentToken != CqlLexer.Token.Colon)
                    throw ParserError("Missing field name.");


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
        private SnQueryPredicate ParseRange()
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
            var val = ParseValue();
            if (val == null)
                return null;
            val.FuzzyValue = ParseFuzzy(val.IsPhrase);
            return val;
        }
        private QueryFieldValue ParseValue()
        {
            // Value        ==>  ExactValue | WILDCARDSTRING
            var val = ParseExactValue(false);
            if (val != null)
                return val;
            if (_lexer.CurrentToken == CqlLexer.Token.WildcardString)
            {
                val = new QueryFieldValue(_lexer.StringValue, _lexer.IsPhrase);
                _lexer.NextToken();
                return val;
            }
            return null;
        }
        private QueryFieldValue ParseExactValue(bool throwEx)
        {
            // ExactValue   ==>  STRING | NUMBER | EMPTY
            if (_lexer.StringValue == SnQuery.EmptyText)
            {
                _hasEmptyQuery = true;
                var fieldVal = new QueryFieldValue(_lexer.StringValue, _lexer.IsPhrase);
                _lexer.NextToken();
                return fieldVal;
            }
            if (_lexer.CurrentToken != CqlLexer.Token.String && _lexer.CurrentToken != CqlLexer.Token.Number)
            {
                if (throwEx)
                    throw ParserError(String.Concat("Unexpected ", _lexer.CurrentToken, ". Expected: STRING | NUMBER"));
                return null;
            }

            var val = new QueryFieldValue(_lexer.StringValue, _lexer.IsPhrase);
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
                    return SnQuery.DefaultSimilarity;
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

        /* ============================================================================ */

        private void AddBooleanClause(LogicalPredicate boolNode, SnQueryPredicate query, Occurence occur)
        {
            var boolQ = query as LogicalPredicate;
            if (boolQ == null)
            {
                boolNode.Clauses.Add(new LogicalClause(query, occur));
                return;
            }
            var clauses = boolQ.Clauses;
            if (clauses.Count == 0)
            {
                throw ParserError("Empty BooleanNode");
            }
            if (clauses.Count > 1)
            {
                boolNode.Clauses.Add(new LogicalClause(query, occur));
                return;
            }

            // boolQ has one clause: combine occurs
            var clause = clauses[0];
            var clauseOccur = clause.Occur;
            Occurence effectiveOccur;
            if (_defaultOperator == DefaultOperator.Or)
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

        private SnQueryPredicate CreateValueQuery(QueryFieldValue value)
        {
            var currentField = _currentField.Peek();
            var fieldName = currentField.Name;

            var parsedValue = ParseValue(fieldName, value, _context);

            if (parsedValue.Type == IndexValueType.String)
            {
                if (parsedValue.StringValue == SnQuery.EmptyText)
                    return new TextPredicate(currentField.Name, parsedValue);
                if (parsedValue.StringValue == SnQuery.EmptyInnerQueryText)
                    return new TextPredicate(IndexFieldName.NodeId, new IndexValue(0));
            }
            return new TextPredicate(currentField.Name, parsedValue, value.FuzzyValue);
        }
        private IndexValue ParseValue(string fieldName, QueryFieldValue value, IQueryContext context, bool throwIfError = true)
        {
            if (value == null)
                return null;

            var parser = context.GetPerFieldIndexingInfo(fieldName);
            var parsed = parser.IndexFieldHandler.Parse(value.StringValue);
            if(parsed == null)
                throw new ParserException($"Cannot parse the value. FieldName {fieldName}, Parser: {parser.GetType().Name}", _lexer.CreateLastLineInfo());

            return parsed;
        }

        private SnQueryPredicate CreateRangeQuery(string fieldName, QueryFieldValue minValue, QueryFieldValue maxValue, bool includeLower, bool includeUpper)
        {
            var min = ParseValue(fieldName, minValue, _context);
            var max = ParseValue(fieldName, maxValue, _context);

            if (min?.StringValue == SnQuery.EmptyText && max == null)
            {
                _hasEmptyQuery = true;
                return new TextPredicate(fieldName, min);
            }
            if (max?.StringValue == SnQuery.EmptyText && min == null)
            {
                _hasEmptyQuery = true;
                return new TextPredicate(fieldName, max);
            }

            if (min?.StringValue == SnQuery.EmptyText)
                min = null;
            if (max?.StringValue == SnQuery.EmptyText)
                max = null;

            if (min == null && max == null)
                throw ParserError("Invalid range: the minimum and the maximum value are cannot be null/empty together.");

            return new RangePredicate(fieldName, min, max, !includeLower, !includeUpper);
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
