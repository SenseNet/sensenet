using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search.Parser;
using SenseNet.Tools;

namespace SenseNet.Search
{
    public class SafeQueries_NEW
    {
        private static readonly string[] _safeQueries;
        static SafeQueries_NEW()
        {
            var genuineQueries = new List<string>();
            foreach (Type t in TypeResolver.GetTypesByInterface(typeof(ISafeQueryHolder)))
            {
                genuineQueries.AddRange(
                    t.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(x => x.GetSetMethod() == null)
                    .Select(x => x.GetGetMethod(true).Invoke(null, null) as string)
                    .Where(y => y != null).Distinct().ToArray());
            }
            _safeQueries = genuineQueries.ToArray();
        }

        public static bool IsSafe(string queryText)
        {
            return _safeQueries.Contains(queryText);
        }
    }

    public class ContentQuery_NEW
    {
        internal static readonly string EmptyInnerQueryText = "$##$EMPTYINNERQUERY$##$";

        private static readonly string RegexCommentEndSingle = "$";
        private static readonly string RegexCommentEndMulti = "\\*/|\\z";
        private static readonly string MultilineCommentStart = "/*";
        private static readonly string MultilineCommentEnd = "*/";

        private string _text;
        public string Text
        {
            get { return _text; }
            set { _text = FixMultilineComment(value); }
        }

        private QuerySettings _settings = new QuerySettings();
        public QuerySettings Settings
        {
            get { return _settings; }
            set { _settings = value ?? new QuerySettings(); }
        }

        public bool IsSafe { get; private set; }

        public static QueryResult Query(string text, QuerySettings settings, params object[] parameters)
        {
            return CreateQuery(text, settings, parameters).Execute();
        }
        private static ContentQuery_NEW CreateQuery(string text, QuerySettings settings, params object[] parameters)
        {
            var isSafe = IsSafeQuery(text);
            if (parameters != null && parameters.Length > 0)
                text = SubstituteParameters(text, parameters);
            var query = new ContentQuery_NEW
            {
                Text = text,
                IsSafe = isSafe,
                Settings = settings,
            };
            return query;
        }
        internal static bool IsSafeQuery(string queryText)
        {
            return SafeQueries_NEW.IsSafe(queryText);
        }
        private static string SubstituteParameters(string text, object[] parameters)
        {
            var stringValues = new string[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                stringValues[i] = EscapeParameter(parameters[i]);

            var sb = new StringBuilder();
            var p = -1;
            while (++p < text.Length)
            {
                if (text[p] == '@')
                {
                    var q = p;
                    while (++q < text.Length && Char.IsDigit(text[q]))
                        ;
                    var nr = text.Substring(p + 1, q - p - 1);
                    if (nr.Length > 0)
                    {
                        var index = Int32.Parse(nr);
                        if (index >= parameters.Length)
                            throw new InvalidOperationException("Invalid format string.");
                        sb.Append(stringValues[index]);
                        p = q - 1;
                    }
                    else
                    {
                        sb.Append(text[p]);
                    }
                }
                else
                {
                    sb.Append(text[p]);
                }
            }
            return sb.ToString();
        }
        private static string EscapeParameter(object value)
        {
            var enumerableValue = value as IEnumerable;
            if (!(value is string) && enumerableValue != null)
            {
                var escaped = new List<string>();
                foreach (var x in enumerableValue)
                    if (x != null)
                        escaped.Add(EscapeParameter(x.ToString()));
                var joined = String.Join(" ", escaped);
                if (escaped.Count < 2)
                    return joined;
                return "(" + joined + ")";
            }
            else
            {
                var stringValue = value.ToString();
                var neeqQuot = false;
                foreach (var c in stringValue)
                {
                    if (Char.IsWhiteSpace(c))
                    {
                        neeqQuot = true;
                        break;
                    }
                    if (c == '\'' || c == '"' || c == '\\' || c == '+' || c == '-' || c == '&' || c == '|' || c == '!' || c == '(' || c == ')'
                         || c == '{' || c == '}' || c == '[' || c == ']' || c == '^' || c == '~' || c == '*' || c == '?' || c == ':' || c == '/' || c == '.')
                    {
                        neeqQuot = true;
                        break;
                    }
                }
                if (neeqQuot)
                    stringValue = "\"" + stringValue.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

                return stringValue;
            }
        }

        private static string FixMultilineComment(string queryText)
        {
            if (String.IsNullOrEmpty(queryText))
                return queryText;

            // find the last multiline comment
            var commentStartIndex = queryText.LastIndexOf(MultilineCommentStart);
            if (commentStartIndex < 0)
                return queryText;

            // find the end of the multiline comment: /* ... */
            var commentEndIndex = GetCommentEndIndex(queryText, commentStartIndex);
            if (commentEndIndex < queryText.Length - 1)
                return queryText;

            // comment is not closed --> close it manually
            return queryText + MultilineCommentEnd;
        }
        private static int GetCommentEndIndex(string queryText, int commentStartIndex)
        {
            // construct a single- or multiline end-commend regex
            var regexEndComment = new Regex(queryText.Substring(commentStartIndex, 2) == "//"
                ? RegexCommentEndSingle
                : RegexCommentEndMulti, RegexOptions.Multiline);

            var matchEndComment = regexEndComment.Match(queryText, commentStartIndex);

            // this will always be true, as both regexes contain the end-of-string character
            if (matchEndComment.Success)
            {
                return matchEndComment.Index + matchEndComment.Length;
            }

            return queryText.Length;
        }

        private static int GetDefaultMaxResults()
        {
            return Int32.MaxValue;
        }

        public QueryResult Execute()
        {
            var queryText = Text;

            if (String.IsNullOrEmpty(queryText))
                throw new InvalidOperationException("Cannot execute query with null or empty Text");

            if (AccessProvider.Current.GetCurrentUser().Id == Identifiers.SystemUserId && !this.IsSafe)
            {
                var ex = new InvalidOperationException("Cannot execute this query, please convert it to a safe query.");
                ex.Data.Add("EventId", EventId.Querying);
                ex.Data.Add("Query", this._text);

                throw ex;
            }

            IEnumerable<int> identifiers;
            int totalCount;
            using (var op = SnTrace.Query.StartOperation("ContentQuery: {0} | Top:{1} Skip:{2} Sort:{3} Mode:{4}", queryText, _settings.Top, _settings.Skip, _settings.Sort, _settings.QueryExecutionMode))
            {
                if (!queryText.Contains("}}"))
                {
                    string projection;
                    var lucObjects = ExecuteAtomic(queryText, Settings.Top, Settings.Skip, Settings.Sort, Settings.EnableAutofilters,
                        Settings.EnableLifespanFilter, Settings.QueryExecutionMode, Settings, false, out projection,
                        out totalCount);
                    identifiers = lucObjects.Select(l => l.NodeId).ToArray();
                }
                else
                {
                    List<string> log;
                    identifiers = RecursiveExecutor.ExecuteRecursive(queryText, Settings.Top, Settings.Skip, Settings.Sort, Settings.EnableAutofilters,
                        Settings.EnableLifespanFilter, Settings.QueryExecutionMode,
                        Settings, out totalCount, out log);
                }
                op.Successful = true;
            }
            return new QueryResult(identifiers, totalCount);
        }

        //UNDONE: ## Deepest level in the general layer
        private static IEnumerable<LucObject> ExecuteAtomic(string queryText, int top, int skip, IEnumerable<SortInfo> sort, FilterStatus enableAutofilters, FilterStatus enableLifespanFilter, QueryExecutionMode executionMode, QuerySettings settings, bool enableProjection, out string projection, out int totalCount)
        {
            LucQuery query;

            try
            {
                query = LucQuery.Parse(queryText);
            }
            catch (ParserException ex)
            {
                throw new InvalidContentQueryException(queryText, innerException: ex);
            }

            projection = query.Projection;
            if (projection != null)
            {
                if (!enableProjection)
                    throw new ApplicationException(
                        $"Projection in top level query is not allowed ({SnLucLexer.Keywords.Select}:{projection})");
                query.ForceLuceneExecution = true;
            }

            if (skip != 0)
                query.Skip = skip;

            if (query.Top == 0)
                query.Top = GetDefaultMaxResults();
            if (top == 0)
                top = GetDefaultMaxResults();
            query.Top = Math.Min(top, query.Top);

            query.PageSize = query.Top;
            if (sort != null && sort.Count() > 0)
                query.SetSort(sort);

            if (enableAutofilters != FilterStatus.Default)
                query.EnableAutofilters = enableAutofilters;
            if (enableLifespanFilter != FilterStatus.Default)
                query.EnableLifespanFilter = enableLifespanFilter;
            if (executionMode != QueryExecutionMode.Default)
                query.QueryExecutionMode = executionMode;

            // Re-set settings values. This is important for NodeList that
            // uses the paging info written into the query text.
            if (settings != null)
            {
                settings.Top = query.PageSize;
                settings.Skip = query.Skip;
            }

            var lucObjects = query.Execute().ToList();
            totalCount = query.TotalCount;
            return lucObjects;
        }

        // ================================================================== Recursive executor class

        private static class RecursiveExecutor
        {
            private class InnerQueryResult
            {
                internal bool IsIntArray;
                internal string[] StringArray;
                internal int[] IntArray;
            }

            public static IEnumerable<int> ExecuteRecursive(string queryText, int top, int skip,
                IEnumerable<SortInfo> sort, FilterStatus enableAutofilters, FilterStatus enableLifespanFilter, QueryExecutionMode executionMode,
                QuerySettings settings, out int count, out List<string> log)
            {
                log = new List<string>();
                IEnumerable<int> result = new int[0];
                var src = queryText;
                log.Add(src);
                var control = GetControlString(src);

                while (true)
                {
                    int start;
                    var sss = GetInnerScript(src, control, out start);
                    var end = sss == String.Empty;

                    if (!end)
                    {
                        src = src.Remove(start, sss.Length);
                        control = control.Remove(start, sss.Length);

                        int innerCount;
                        var innerResult = ExecuteInnerScript(sss.Substring(2, sss.Length - 4), 0, 0,
                            sort, enableAutofilters, enableLifespanFilter, executionMode, null, true, out innerCount).StringArray;

                        switch (innerResult.Length)
                        {
                            case 0:
                                sss = EmptyInnerQueryText;
                                break;
                            case 1:
                                sss = innerResult[0];
                                break;
                            default:
                                sss = String.Join(" ", innerResult);
                                sss = "(" + sss + ")";
                                break;
                        }
                        src = src.Insert(start, sss);
                        control = control.Insert(start, sss);
                        log.Add(src);
                    }
                    else
                    {
                        result = ExecuteInnerScript(src, top, skip, sort, enableAutofilters, enableLifespanFilter, executionMode,
                            settings, false, out count).IntArray;

                        log.Add(String.Join(" ", result.Select(i => i.ToString()).ToArray()));
                        break;
                    }
                }
                return result;
            }
            private static string GetControlString(string src)
            {
                var s = src.Replace("\\'", "__").Replace("\\\"", "__");
                var @out = new StringBuilder(s.Length);
                var instr = false;
                var strlimit = '\0';
                var esc = false;
                foreach (var c in s)
                {
                    if (c == '\\')
                    {
                        esc = true;
                        @out.Append('_');
                    }
                    else
                    {
                        if (esc)
                        {
                            esc = false;
                            @out.Append('_');
                        }
                        else
                        {
                            if (instr)
                            {
                                if (c == strlimit)
                                    instr = !instr;
                                @out.Append('_');
                            }
                            else
                            {
                                if (c == '\'' || c == '"')
                                {
                                    instr = !instr;
                                    strlimit = c;
                                    @out.Append('_');
                                }
                                else
                                {
                                    @out.Append(c);
                                }
                            }
                        }
                    }
                }

                var l0 = src.Length;
                var l1 = @out.Length;

                return @out.ToString();
            }
            private static string GetInnerScript(string src, string control, out int start)
            {
                start = 0;
                var p1 = control.IndexOf("}}");
                if (p1 < 0)
                    return String.Empty;
                var p0 = control.LastIndexOf("{{", p1);
                if (p0 < 0)
                    return String.Empty;
                start = p0;
                var ss = src.Substring(p0, p1 - p0 + 2);
                return ss;
            }

            private static InnerQueryResult ExecuteInnerScript(string queryText, int top, int skip, IEnumerable<SortInfo> sort, FilterStatus enableAutofilters, FilterStatus enableLifespanFilter, QueryExecutionMode executionMode, QuerySettings settings, bool enableProjection, out int totalCount)
            {
                InnerQueryResult result;

                string projection;
                var lucObjects = ExecuteAtomic(queryText, top, skip, sort, enableAutofilters, enableLifespanFilter, executionMode, settings, enableProjection, out projection, out totalCount);

                if (projection == null || !enableProjection)
                {
                    var idResult = lucObjects.Select(o => o.NodeId).ToArray();
                    result = new InnerQueryResult { IsIntArray = true, IntArray = idResult, StringArray = idResult.Select(i => i.ToString()).ToArray() };
                }
                else
                {
                    var stringResult = lucObjects.Select(o => o[projection, false]).Where(r => !String.IsNullOrEmpty(r));
                    var escaped = new List<string>();
                    foreach (var s in stringResult)
                        escaped.Add(EscapeForQuery(s));
                    result = new InnerQueryResult { IsIntArray = false, StringArray = escaped.ToArray() };
                }

                return result;
            }

            private static object __escaperRegexSync = new object();
            private static Regex __escaperRegex;
            private static Regex EscaperRegex
            {
                get
                {
                    if (__escaperRegex == null)
                    {
                        lock (__escaperRegexSync)
                        {
                            if (__escaperRegex == null)
                            {
                                var pattern = new StringBuilder("[");
                                foreach (var c in SnLucLexer.STRINGTERMINATORCHARS.ToCharArray())
                                    pattern.Append("\\" + c);
                                pattern.Append("]");
                                __escaperRegex = new Regex(pattern.ToString());
                            }
                        }
                    }
                    return __escaperRegex;
                }
            }

            private static string EscapeForQuery(string value)
            {
                if (EscaperRegex.IsMatch(value))
                    return String.Concat("'", value, "'");
                return value;
            }
        }
    }
}
