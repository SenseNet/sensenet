using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Search.Parser;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    //UNDONE: SQL: Develop SqlQueryEngine.TryExecuteQuery
    internal class SnLucToSqlCompiler
    {
        //private static Dictionary<string, SqlQueryFieldInfo> _sqlFields;
        //private static string[] _enabledFields;
        //private static string[] _indexedFields;

        //static SnLucToSqlCompiler()
        //{
        //    _sqlFields = new Dictionary<string, SqlQueryFieldInfo>
        //    {
        //        // indexed fields
        //        { "Id",                  new QueryFieldInfo_Int               { SqlName = "NodeId" } },
        //        { "Type",                new QueryFieldInfo_NodeType          { SqlName = "NodeTypeId" } },
        //        { "TypeIs",              new QueryFieldInfo_NodeTypeRecursive { SqlName = "NodeTypeId", SqlOperator = "IN" } },
        //        { "ParentId",            new QueryFieldInfo_Int               { SqlName = "ParentNodeId" } },
        //        { "InFolder",            new QueryFieldInfo_PathToId          { SqlName = "ParentNodeId" } },

        //        // not indexed fields
        //        { "Name",                new QueryFieldInfo_String            { SqlName = "Name", NeedApos = true } },
        //        { "IsSystemContent",     new QueryFieldInfo_Bool              { SqlName = "IsSystem" } },
        //        { "LastMinorVersionId",  new QueryFieldInfo_Int               { SqlName = "LastMinorVersionId" } },
        //        { "LastMajorVersionId",  new QueryFieldInfo_Int               { SqlName = "LastMajorVersionId" } },
        //        { "Index",               new QueryFieldInfo_Int               { SqlName = "[Index]", ParameterName = "Index" } },
        //        { "ContentListTypeId",   new QueryFieldInfo_Int               { SqlName = "ContentListTypeId" } },
        //        { "ContentListId",       new QueryFieldInfo_Int               { SqlName = "ContentListId" } },
        //        { "Locked",              new QueryFieldInfo_Locked            { SqlName = "LockedById" } },
        //        { "ModifiedById",        new QueryFieldInfo_Int               { SqlName = "LockedById" } },
        //        { "CreatedById",         new QueryFieldInfo_Int               { SqlName = "CreatedById" } },

        //        // disabled fields (because SQL cannot handle dates smaller than 1753)
        //        // { "CreationDate",        new QueryFieldInfo_Date              { SqlName = "CreationDate", NeedApos = true } },
        //        // { "ModificationDate",    new QueryFieldInfo_Date              { SqlName = "ModificationDate", NeedApos = true } },

        //        // disabled fields
        //        // { "InTree",              new QueryFieldInfo_String            { SqlName = "Path", SqlOperator = "LIKE%" } },
        //        // { "Path",                new QueryFieldInfo_String            { SqlName = "Path", NeedApos = true } },
        //        // { "IsInherited",         new QueryFieldInfo_Bool              { SqlName = "IsInherited" } }, // not used
        //        // { "LockedById",          new QueryFieldInfo_NullableInt       { SqlName = "LockedById" } },
        //    };
        //    _enabledFields = _sqlFields.Keys.ToArray();
        //    _indexedFields = new[] { "Id", "Type", "TypeIs", "ParentId", "InFolder", };
        //}

        //internal static bool TryCompile(SnQueryPredicate query, int top, int skip, SortInfo[] orders, bool countOnly, out string sqlQueryText, out NodeQueryParameter[] sqlParameters)
        //{
        //    try
        //    {
        //        var compiler = new SnLucToSqlCompiler();
        //        sqlQueryText = compiler.Compile(query, top, skip, orders, countOnly, out sqlParameters);
        //        return true;
        //    }
        //    catch (SnNotSupportedException e)
        //    {
        //        SnTrace.Query.Write("SQL:Not supported: {0}, original query: {1}", e.Message, query);
        //        sqlQueryText = null;
        //        sqlParameters = null;
        //        return false;
        //    }
        //}
        //private string Compile(SnQueryPredicate query, int top, int skip, SortInfo[] orders, bool countOnly, out NodeQueryParameter[] parameters)
        //{
        //    throw new NotImplementedException();
        //}

        //public static bool TryCompile(Query query, int top, int skip, SortField[] orders, bool countOnly, out string sqlQueryText, out SenseNet.ContentRepository.Storage.Search.NodeQueryParameter[] parameters)
        //{
        //    try
        //    {
        //        var compiler = new SnLucToSqlCompiler();
        //        sqlQueryText = compiler.Compile(query, top, skip, orders, countOnly, out parameters);
        //        return true;
        //    }
        //    catch (SnNotSupportedException e)
        //    {
        //        SnTrace.Query.Write("SQL:Not supported: {0}, original query: {1}", e.Message, query);
        //        sqlQueryText = null;
        //        parameters = null;
        //        return false;
        //    }
        //}
        //public string Compile(Query query, int top, int skip, SortField[] orders, bool countOnly, out NodeQueryParameter[] parameters)
        //{
        //    if (countOnly)
        //        throw new SnNotSupportedException("'CountOnly' is not supported.");
        //    if (skip > 0)
        //        throw new SnNotSupportedException("Paging is not supported (skip > 0).");

        //    var whereCompiler = new SqlWhereVisitor();
        //    whereCompiler.Visit(query);

        //    var sb = new StringBuilder();
        //    sb.Append("SELECT");
        //    if (top > 0 && top < int.MaxValue)
        //        sb.Append(" TOP ").Append(top);
        //    sb.AppendLine(" NodeId, LastMajorVersionId FROM Nodes");
        //    sb.Append("WHERE ");

        //    sb.AppendLine(whereCompiler.ToString());

        //    if (orders != null && orders.Count() > 0)
        //    {
        //        sb.Append("ORDER BY ");
        //        sb.AppendLine(String.Join(", ", orders.Select(o => _sqlFields[o.GetField()].SqlName + (o.GetReverse() ? " DESC" : String.Empty)).ToArray()));
        //    }

        //    parameters = whereCompiler.Parameters;
        //    return sb.ToString();
        //}

        //internal static bool CanCompile(SnQueryInfo queryInfo)
        //{
        //    var trace = SnTrace.Query.Enabled;
        //    var msg = CanCompile(queryInfo, trace);
        //    if (trace && (msg != null))
        //        SnTrace.Query.Write(msg);
        //    return msg == null;
        //}
        //internal static string CanCompile(SnQueryInfo queryInfo, bool withMessages)
        //{
        //    var msg = "error";

        //    if (queryInfo.AllVersions)
        //    {
        //        if (withMessages)
        //            msg = "Cannot compile to SQL: AllVersions";
        //        return msg;
        //    }
        //    if (queryInfo.CountOnly)
        //    {
        //        if (withMessages)
        //            msg = "Cannot compile to SQL: CountOnly";
        //        return msg;
        //    }

        //    if (0 < queryInfo.Top)
        //    {
        //        if (withMessages)
        //            msg = "Cannot compile to SQL: Top:" + queryInfo.Top;
        //        return msg;
        //    }
        //    if (0 < queryInfo.Skip)
        //    {
        //        if (withMessages)
        //            msg = "Cannot compile to SQL: Skip:" + queryInfo.Skip;
        //        return msg;
        //    }
        //    if (queryInfo.CountAllPages)
        //    {
        //        return withMessages ? "Cannot compile to SQL: InlineCount: AllPages": msg;
        //    }
        //    if (0 < queryInfo.FuzzyQueries)
        //    {
        //        if (withMessages)
        //            msg = "Cannot compile to SQL: FuzzyQuery is forbidden.";
        //        return msg;
        //    }
        //    if (0 < queryInfo.QuestionMarkWildcards)
        //    {
        //        if (withMessages)
        //            msg = "Cannot compile to SQL: a question mark wildcard exists.";
        //        return msg;
        //    }
        //    if (0 < queryInfo.FullRangeQueries)
        //    {
        //        if (withMessages)
        //            msg = "Cannot compile to SQL: in a range query both limit are defined.";
        //        return msg;
        //    }

        //    var forbiddenFields = queryInfo.QueryFieldNames.Except(_enabledFields).ToArray();
        //    if (forbiddenFields.Length > 0)
        //    {
        //        if (withMessages)
        //            msg = string.Format("Cannot compile to SQL: Forbidden fields: [{0}]", string.Join(", ", forbiddenFields));
        //        return msg;
        //    }
        //    forbiddenFields = queryInfo.SortFieldNames.Except(_enabledFields).ToArray();
        //    if (forbiddenFields.Length > 0)
        //    {
        //        if (withMessages)
        //            msg = string.Format("Cannot compile to SQL: Forbidden sort fields: [{0}]", string.Join(", ", forbiddenFields));
        //        return msg;
        //    }

        //    if (!queryInfo.QueryFieldNames.Intersect(_indexedFields).Any())
        //    {
        //        if (withMessages)
        //            msg = string.Format("Cannot compile to SQL: Missing required field, at least one of the followings: [{0}]", string.Join(", ", _indexedFields));
        //        return msg;
        //    }

        //    return null;
        //}
        //internal class SqlWhereVisitor : LucQueryVisitor
        //{
        //    private StringBuilder _sql = new StringBuilder();
        //    private List<string> _paramNames = new List<string>();
        //    private List<SenseNet.ContentRepository.Storage.Search.NodeQueryParameter> _parameters = new List<SenseNet.ContentRepository.Storage.Search.NodeQueryParameter>();
        //    public SenseNet.ContentRepository.Storage.Search.NodeQueryParameter[] Parameters
        //    {
        //        get { return _parameters.ToArray(); }
        //    }
        //    private Stack<string> _operators = new Stack<string>();

        //    public SqlWhereVisitor()
        //    {
        //        _operators.Push(" = ");
        //    }

        //    public override Query VisitPhraseQuery(PhraseQuery phraseq)
        //    {
        //        throw new SnNotSupportedException("Cannot compile PhraseQuery to SQL expression.");
        //    }
        //    public override Query VisitFuzzyQuery(FuzzyQuery fuzzyq)
        //    {
        //        throw new SnNotSupportedException("Cannot compile FuzzyQuery to SQL expression.");
        //    }
        //    public override Query VisitPrefixQuery(PrefixQuery prefixq)
        //    {
        //        _operators.Push("LIKE%");
        //        var q = base.VisitPrefixQuery(prefixq);
        //        _operators.Pop();
        //        return q;
        //    }
        //    public override Query VisitWildcardQuery(WildcardQuery wildcardq)
        //    {
        //        var pattern = wildcardq.GetTerm().Text();

        //        if (pattern.Contains("?"))
        //            throw new SnNotSupportedException("Cannot compile WildcardQuery, which contains '?', to SQL expression");

        //        if (pattern.StartsWith("*") && pattern.EndsWith("*"))
        //            _operators.Push("%LIKE%");
        //        else if (pattern.StartsWith("*"))
        //            _operators.Push("%LIKE");
        //        else if (pattern.EndsWith("*"))
        //            _operators.Push("LIKE%");

        //        var q = base.VisitWildcardQuery(wildcardq);
        //        _operators.Pop();
        //        return q;
        //    }
        //    public override Query VisitTermQuery(TermQuery termq)
        //    {
        //        var q = base.VisitTermQuery(termq);
        //        return q;
        //    }
        //    public override Query VisitTermRangeQuery(TermRangeQuery termRangeq)
        //    {
        //        var q = (TermRangeQuery)base.VisitTermRangeQuery(termRangeq);
        //        CompileRange(q.GetField(), q.GetLowerTerm(), q.GetUpperTerm(), q.IncludesLower(), q.IncludesUpper());
        //        return q;
        //    }
        //    public override Query VisitNumericRangeQuery(NumericRangeQuery numericRangeq)
        //    {
        //        var q = (NumericRangeQuery)base.VisitNumericRangeQuery(numericRangeq);
        //        var min = q.GetMin();
        //        var max = q.GetMax();
        //        var mins = min == null ? null : min.ToString();
        //        var maxs = max == null ? null : max.ToString();
        //        CompileRange(q.GetField(), mins, maxs, q.IncludesMin(), q.IncludesMax());
        //        return q;
        //    }

        //    private void CompileRange(string fieldName, string lowerTerm, string upperTerm, bool incLower, bool incUpper)
        //    {
        //        if (lowerTerm != null && upperTerm != null)
        //        {
        //            //TODO: full range
        //            throw new SnNotSupportedException();
        //        }
        //        else
        //        {
        //            Term t = null;
        //            if (upperTerm == null)
        //            {
        //                _operators.Push(incLower ? " >= " : " > ");
        //                t = new Term(fieldName, lowerTerm);
        //            }
        //            else if (lowerTerm == null)
        //            {
        //                _operators.Push(incUpper ? " <= " : " < ");
        //                t = new Term(fieldName, upperTerm);
        //            }
        //            VisitTerm(t);
        //            _operators.Pop();
        //        }
        //    }

        //    public override Query VisitBooleanQuery(BooleanQuery booleanq)
        //    {
        //        return base.VisitBooleanQuery(booleanq);
        //    }
        //    public override BooleanClause[] VisitBooleanClauses(BooleanClause[] clauses)
        //    {
        //        List<BooleanClause> newList = null;

        //        clauses = new BooleanClauseOptimizer().VisitBooleanClauses(clauses);

        //        // optimize to IN clause if possible
        //        var simpleTerms = GetTermIfEverythingIsTermQuery(clauses);
        //        if (simpleTerms != null)
        //        {
        //            if (CompileToInClause(simpleTerms))
        //                return clauses;
        //        }

        //        int index = 0;
        //        int count = clauses.Length;

        //        _sql.Append("(");
        //        while (index < count)
        //        {
        //            if (index > 0)
        //                _sql.Append(" " + GetSqlOperator(clauses[index - 1], clauses[index]) + " ");

        //            if (clauses[index].GetOccur() == BooleanClause.Occur.MUST_NOT)
        //                _sql.Append("NOT ");

        //            var visitedClause = VisitBooleanClause(clauses[index]);
        //            if (newList != null)
        //            {
        //                newList.Add(visitedClause);
        //            }
        //            else if (visitedClause != clauses[index])
        //            {
        //                newList = new List<BooleanClause>();
        //                for (int i = 0; i < index; i++)
        //                    newList.Add(clauses[i]);
        //                newList.Add(visitedClause);
        //            }
        //            index++;
        //        }
        //        _sql.Append(")");

        //        return newList != null ? newList.ToArray() : clauses;
        //    }
        //    private Term[] GetTermIfEverythingIsTermQuery(BooleanClause[] clauses)
        //    {
        //        if (clauses.Length < 2)
        //            return null;

        //        var terms = new Term[clauses.Length];
        //        for (int i = 0; i < clauses.Length; i++)
        //        {
        //            var occur = clauses[i].GetOccur();
        //            if (occur != null && occur != BooleanClause.Occur.SHOULD)
        //                return null;
        //            var termQuery = clauses[i].GetQuery() as TermQuery;
        //            if (termQuery == null)
        //                return null;
        //            terms[i] = termQuery.GetTerm();
        //        }

        //        return terms;
        //    }
        //    private bool CompileToInClause(Term[] terms)
        //    {
        //        string fieldName = null;
        //        SqlQueryFieldInfo fieldInfo = null;
        //        string sqlName = null;
        //        var values = new string[terms.Length];
        //        bool needApos = false;

        //        for (int i = 0; i < terms.Length; i++)
        //        {
        //            var term = base.VisitTerm(terms[i]);
        //            var field = term.Field();
        //            if (i == 0)
        //            {
        //                fieldName = field;
        //                fieldInfo = GetSqlField(fieldName);
        //                sqlName = fieldInfo.SqlName;
        //                needApos = fieldInfo.NeedApos;
        //            }
        //            else if (fieldName != field)
        //            {
        //                return false;
        //            }
        //            values[i] = fieldInfo.GetSqlTextValue(term.Text());
        //        }

        //        if (sqlName.Equals("Index", StringComparison.OrdinalIgnoreCase))
        //            sqlName = "[Index]";

        //        _sql.Append(sqlName).Append(" IN (");

        //        for (int i = 0; i < values.Length; i++)
        //        {
        //            var value = values[i];
        //            if (needApos)
        //                value = "'" + value + "'";

        //            if (i > 0)
        //                _sql.Append(", ");
        //            _sql.Append(value);
        //        }
        //        _sql.Append(")");

        //        return true;
        //    }

        //    private string GetSqlOperator(BooleanClause leftClause, BooleanClause rightClause)
        //    {
        //        var occurence = OccurenceToString(leftClause.GetOccur()) + OccurenceToString(rightClause.GetOccur());
        //        switch (occurence)
        //        {
        //            case "--":
        //            case "+-":
        //            case "-+":
        //            case "++": return "AND";
        //            case "??": return "OR";
        //        }
        //        throw new SnNotSupportedException("Unknown occurence combination: '" + occurence + "'");
        //    }
        //    private string OccurenceToString(BooleanClause.Occur occurence)
        //    {
        //        if (occurence == null)
        //            return "?";
        //        if (occurence == BooleanClause.Occur.SHOULD)
        //            return "?";
        //        if (occurence == BooleanClause.Occur.MUST)
        //            return "+";
        //        if (occurence == BooleanClause.Occur.MUST_NOT)
        //            return "-";
        //        throw new SnNotSupportedException("Unknown occurence: " + occurence);
        //    }

        //    public override Term VisitTerm(Term term)
        //    {
        //        var t = base.VisitTerm(term);
        //        CompileTerm(t);
        //        return t;
        //    }
        //    private void CompileTerm(Term t)
        //    {
        //        var fieldName = t.Field();
        //        var termValue = t.Text();

        //        var fieldInfo = GetSqlField(fieldName);
        //        var sqlName = fieldInfo.SqlName;
        //        var parameterValue = fieldInfo.GetParameterValue(termValue);
        //        var sqlTextValue = fieldInfo.GetSqlTextValue(termValue);
        //        string @operator = fieldInfo.SqlOperator;
        //        var needApos = fieldInfo.NeedApos;
        //        var paramName = fieldInfo.ParameterName;

        //        if (parameterValue == null && @operator == null)
        //            @operator = "ISNULL";

        //        var index = 0;
        //        while (_paramNames.Contains(paramName))
        //            paramName = sqlName + ++index;
        //        _paramNames.Add(paramName);
        //        paramName = "@" + paramName;

        //        if (@operator == null)
        //            @operator = _operators.Peek();

        //        if (@operator == "LIKE%" || @operator == "%LIKE" || @operator == "%LIKE%")
        //        {
        //            sqlTextValue = sqlTextValue.Trim('*');
        //            switch (@operator)
        //            {
        //                case "LIKE%":
        //                    sqlTextValue = sqlTextValue + "%";
        //                    break;
        //                case "%LIKE":
        //                    sqlTextValue = "%" + sqlTextValue;
        //                    break;
        //                case "%LIKE%":
        //                    sqlTextValue = "%" + sqlTextValue + "%";
        //                    break;
        //            }
        //            @operator = " LIKE ";
        //            parameterValue = sqlTextValue;
        //            sqlTextValue = "'" + sqlTextValue + "'";
        //            needApos = false;
        //        }
        //        if (needApos)
        //            sqlTextValue = "'" + sqlTextValue + "'";
        //        if (@operator == "IN")
        //        {
        //            _sql.Append(sqlName).Append(" IN (").Append(sqlTextValue).Append(")");
        //        }
        //        else if (@operator == "ISNULL")
        //        {
        //            _sql.Append(sqlName).Append(sqlTextValue);
        //        }
        //        else
        //        {
        //            _parameters.Add(new SenseNet.ContentRepository.Storage.Search.NodeQueryParameter { Name = paramName, Value = parameterValue });
        //            _sql.Append(sqlName).Append(@operator).Append(paramName);
        //        }
        //    }

        //    private SqlQueryFieldInfo GetSqlField(string name)
        //    {
        //        SqlQueryFieldInfo result;
        //        if (_sqlFields.TryGetValue(name, out result))
        //            return result;
        //        throw new SnNotSupportedException("Cannot compile to Sql query because the content query contains forbidden field: " + name);
        //    }

        //    public override string ToString()
        //    {
        //        return _sql.ToString();
        //    }

        //    private class BooleanClauseOptimizer : LucQueryVisitor
        //    {
        //        public override BooleanClause[] VisitBooleanClauses(BooleanClause[] clauses)
        //        {
        //            List<BooleanClause> newList = null;
        //            int index = 0;
        //            int count = clauses.Length;
        //            while (index < count)
        //            {
        //                var visitedClause = VisitBooleanClause(clauses[index]);
        //                if (newList != null)
        //                {
        //                    newList.Add(visitedClause);
        //                }
        //                else if (visitedClause != clauses[index])
        //                {
        //                    newList = new List<BooleanClause>();
        //                    for (int i = 0; i < index; i++)
        //                        newList.Add(clauses[i]);
        //                    newList.Add(visitedClause);
        //                }
        //                index++;
        //            }
        //            if (newList == null)
        //                return OptimizeBooleanClauses(clauses);
        //            return OptimizeBooleanClauses(newList);
        //        }
        //        private BooleanClause[] OptimizeBooleanClauses(IEnumerable<BooleanClause> clauses)
        //        {
        //            var shouldCount = 0;
        //            var mustCount = 0;
        //            foreach (var clause in clauses)
        //            {
        //                var occur = clause.GetOccur();
        //                if (occur == null || occur == BooleanClause.Occur.SHOULD)
        //                    shouldCount++;
        //                else if (occur == BooleanClause.Occur.MUST)
        //                    mustCount++;
        //            }
        //            if (mustCount * shouldCount == 0)
        //                return clauses.ToArray();
        //            var newList = new List<BooleanClause>();
        //            foreach (var clause in clauses)
        //            {
        //                var occur = clause.GetOccur();
        //                if (occur != null && occur != BooleanClause.Occur.SHOULD)
        //                    newList.Add(clause);
        //            }
        //            return newList.ToArray();
        //        }
        //    }
        //}
    }
}
