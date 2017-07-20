using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using System.Xml;
using System.Text;
using SenseNet.Search.Parser;
using System.Reflection;
using SenseNet.ContentRepository;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Storage.Diagnostics;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Tools;

namespace SenseNet.Search
{
    /// <summary>
    /// A marker interface for classes that hold safe queries in static readonly string properties. The visibility of these properties are irrelevant.
    /// In a solution can be more ISafeQueryHolder implementations. The property values from these classes will be collected
    ///   in order to build the white list of queries that can be accepted in elevated mode.
    /// Implementation classes can be anywhere in the solution. Property name can be anything because only the values will be collected.
    /// </summary>
    /// <example>Here is an example that explains a full implementation of some safe queries
    /// <code>
    /// public class SafeQueries : ISafeQueryHolder
    /// {
    ///     public static string AllDevices { get { return "+InTree:/Root/System/Devices +TypeIs:Device .AUTOFILTERS:OFF"; } }
    ///     public static string InFolderAndSomeType { get { return "+InFolder:@0 +TypeIs:(@1)"; } }
    /// }
    /// </code>
    /// </example>
    public interface ISafeQueryHolder { }

    public class ContentQuery : IContentQuery
    {
        public static readonly string EmptyText = "$##$EMPTY$##$";
        internal static readonly string EmptyInnerQueryText = "$##$EMPTYINNERQUERY$##$";

        private static readonly string[] QuerySettingParts = new[] { "SKIP", "TOP", "SORT", "REVERSESORT", "AUTOFILTERS", "LIFESPAN", "COUNTONLY" };
        private static readonly string RegexKeywordsAndComments = "//|/\\*|(\\.(?<keyword>[A-Z]+)(([ ]*:[ ]*[#]?\\w+(\\.\\w+)?)|([\\) $\\r\\n]+)))";
        private static readonly string RegexCommentEndSingle = "$";
        private static readonly string RegexCommentEndMulti = "\\*/|\\z";
        private static readonly string MultilineCommentStart = "/*";
        private static readonly string MultilineCommentEnd = "*/";

        public static ContentQuery CreateQuery(string text)
        {
            return CreateQuery(text, null);
        }
        public static ContentQuery CreateQuery(string text, QuerySettings settings)
        {
            return CreateQuery(text, settings, null);
        }
        public static ContentQuery CreateQuery(string text, QuerySettings settings, params object[] parameters)
        {
            var isSafe = IsSafeQuery(text);
            if (parameters != null && parameters.Length > 0)
                text = SubstituteParameters(text, parameters);
            var query = new ContentQuery
            {
                Text = text,
                IsSafe = isSafe,
                Settings = settings,
            };
            return query;
        }

        public static QueryResult Query(string text)
        {
            return Query(text, null);
        }
        public static QueryResult Query(string text, QuerySettings settings)
        {
            return Query(text, settings, null);
        }
        /// <summary>
        /// Executes a prepared query. Before execution substitutes the parameters into the placeholders.
        /// Placeholder is a '@' character followed by a number that means the (zero based) index in the paramter array.
        /// Example: +TypeIs:@0 +Name:@1
        /// Parameter values will be escaped and quotation marks will be used in the appropriate places.
        /// Do not surround the placeholders with quotation mark (") or apsthrophe (').
        /// In case of inconsistence beetween parameter count and placeholder indexes InvalidOperationException will be thrown.
        /// </summary>
        /// <param name="text">Query text containing placeholders</param>
        /// <param name="settings">Additional control parameters (top, skip, sort, automations). It can be null.</param>
        /// <param name="parameters">Value list that will be substituted into the placeholders of the query text.</param>
        /// <returns>Contains result set and its metadata.</returns>
        public static QueryResult Query(string text, QuerySettings settings, params object[] parameters)
        {
            return CreateQuery(text, settings, parameters).Execute(ExecutionHint.None);
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
                        var index = int.Parse(nr);
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
            var enumerableValue = value as System.Collections.IEnumerable;
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
                    if (char.IsWhiteSpace(c))
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

        // ================================================================== Genuine query checking

        private static string[] _safeQueries;
        static ContentQuery()
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
        internal static bool IsSafeQuery(string queryText)
        {
            return _safeQueries.Contains(queryText);
        }
        public bool IsSafe { get; private set; }

        // ================================================================== IContentQuery Members

        private string _text;
        public string Text
        {
            get { return _text; }
            set { _text = FixMultilineComment(value); }
        }

        public int TotalCount { get; private set; }

        private QuerySettings _settings;
        public QuerySettings Settings
        {
            get { return _settings ?? (_settings = new QuerySettings()); }
            set { _settings = value; }
        }

        public void AddClause(string text)
        {
            AddClause(text, ChainOperator.And);
        }
        public void AddClause(string text, ChainOperator chainOp)
        {
            AddClause(text, chainOp, null);
        }
        public void AddClause(string text, ChainOperator chainOp, params object[] parameters)
        {
            var isSafe = this.IsSafe && IsSafeQuery(text);
            if (parameters != null && parameters.Length > 0)
                text = SubstituteParameters(text, parameters);
            this.IsSafe = isSafe;
            AddClausePrivate(text, chainOp);
        }
        private void AddClausePrivate(string text, ChainOperator chainOp)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            if (text.Length == 0)
                throw new ArgumentException("Clause cannot be empty", "text");

            if (string.IsNullOrEmpty(this.Text))
            {
                this.Text = text;
            }
            else
            {
                // we can modify the _text variable here directly because it was already fixed at init time
                switch (chainOp)
                {
                    case ChainOperator.And:
                        this._text = MoveSettingsToTheEnd(string.Format("+({0}) +({1})", Text, text)).Trim();
                        break;
                    case ChainOperator.Or:
                        this._text = MoveSettingsToTheEnd(string.Format("({0}) {1}", Text, text));
                        break;
                }
            }
        }

        public static string AddClause(string originalText, string addition, ChainOperator chainOp)
        {
            if (addition == null)
                throw new ArgumentNullException("addition");
            if (addition.Length == 0)
                throw new ArgumentException("Clause cannot be empty", "addition");

            if (string.IsNullOrEmpty(originalText))
                return addition;

            var queryText = string.Empty;

            switch (chainOp)
            {
                case ChainOperator.And:
                    queryText = MoveSettingsToTheEnd(string.Format("+({0}) +({1})", originalText, addition)).Trim();
                    break;
                case ChainOperator.Or:
                    queryText = MoveSettingsToTheEnd(string.Format("({0}) {1}", originalText, addition));
                    break;
            }

            return queryText;
        }

        public QueryResult Execute()
        {
            return Execute(ExecutionHint.None);
        }
        public QueryResult Execute(ExecutionHint hint)
        {
            return new QueryResult(GetIdResults(), TotalCount);
        }
        public IEnumerable<int> ExecuteToIds()
        {
            return ExecuteToIds(ExecutionHint.None);
        }
        public IEnumerable<int> ExecuteToIds(ExecutionHint hint)
        {
            // We need to get the pure id list for one single query.
            // If you run Execute, it returns a NodeList that loads
            // all result ids, not only the page you specified.
            return GetIdResults();
        }

        // ================================================================== Get result ids

        private IEnumerable<int> GetIdResults()
        {
            return GetIdResults(Settings.Top, Settings.Skip, Settings.Sort,
                Settings.EnableAutofilters, Settings.EnableLifespanFilter, Settings.QueryExecutionMode);
        }
        private IEnumerable<int> GetIdResults(int top, int skip, IEnumerable<SortInfo> sort, FilterStatus enableAutofilters, FilterStatus enableLifespanFilter, QueryExecutionMode executionMode)
        {
            if (AccessProvider.Current.GetCurrentUser().Id == Configuration.Identifiers.SystemUserId && !this.IsSafe)
            {
                var ex = new InvalidOperationException("Cannot execute this query, please convert it to a safe query.");
                ex.Data.Add("EventId", EventId.Querying);
                ex.Data.Add("Query", this._text);

                throw ex;
            }

            if (string.IsNullOrEmpty(Text))
                throw new InvalidOperationException("Cannot execute query with null or empty Text");

            using (var op = SnTrace.Query.StartOperation("ContentQuery: {0} | Top:{1} Skip:{2} Sort:{3} Mode:{4}", this._text, _settings.Top, _settings.Skip, _settings.Sort, _settings.QueryExecutionMode))
            {
                var result = GetIdResultsWithLucQuery(top, skip, sort, enableAutofilters, enableLifespanFilter, executionMode);
                op.Successful = true;
                return result;
            }
        }
        private IEnumerable<int> GetIdResultsWithLucQuery(int top, int skip, IEnumerable<SortInfo> sort,
            FilterStatus enableAutofilters, FilterStatus enableLifespanFilter, QueryExecutionMode executionMode)
        {
            var queryText = Text;

            if (!queryText.Contains("}}"))
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

                if (skip != 0)
                    query.Skip = skip;

                query.Top = System.Math.Min(top == 0 ? int.MaxValue : top, query.Top == 0 ? int.MaxValue : query.Top);
                if (query.Top == 0)
                    query.Top = GetDefaultMaxResults();

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
                this.Settings.Top = query.PageSize;
                this.Settings.Skip = query.Skip;

                var lucObjects = query.Execute().ToList();

                TotalCount = query.TotalCount;

                return (from luco in lucObjects
                        select luco.NodeId).ToList();
            }
            else
            {
                List<string> log;
                int count;
                var result = RecursiveExecutor.ExecuteRecursive(queryText, top, skip,
                            sort, enableAutofilters, enableLifespanFilter, executionMode, this.Settings, out count, out log);

                TotalCount = count;

                return result;
            }
        }

        // ================================================================== Filter methods

        public static string AddAutofilterToNodeQuery(string originalText)
        {
            return AddFilterToNodeQuery(originalText, GetAutofilterForNodeQuery());
        }

        public static string AddFilterToNodeQuery(string originalText, string filterText)
        {
            if (string.IsNullOrEmpty(filterText))
                return originalText;

            var filterXml = new XmlDocument();
            try
            {
                filterXml.LoadXml(filterText);
            }
            catch (XmlException ex)
            {
                throw new InvalidContentQueryException(filterText, "Invalid content query filter", ex);
            }

            var filterTopLogicalElement = (XmlElement)filterXml.SelectSingleNode("/*/*[1]");
            if (filterTopLogicalElement == null)
                return originalText;
            var filterInnerXml = filterTopLogicalElement.InnerXml;
            if (string.IsNullOrEmpty(filterInnerXml))
                return originalText;

            var originalXml = new XmlDocument();
            try
            {
                originalXml.LoadXml(originalText);
            }
            catch (XmlException ex)
            {
                throw new InvalidContentQueryException(originalText ?? string.Empty, innerException: ex);
            }

            var originalTopLogicalElement = (XmlElement)originalXml.SelectSingleNode("/*/*[1]");
            if (originalTopLogicalElement == null)
                return originalText;
            var originalOuterXml = originalTopLogicalElement.OuterXml;
            if (string.IsNullOrEmpty(originalOuterXml))
                return originalText;

            filterTopLogicalElement.InnerXml = String.Concat(filterInnerXml, originalOuterXml);

            return filterXml.OuterXml;
        }

        public static string AddLifespanFilterToNodeQuery(string originalText, string filterText)
        {
            if (string.IsNullOrEmpty(filterText))
                return originalText;

            return originalText;
        }

        /// <summary>
        /// This method moves all the settings keywords (e.g. SKIP, TOP, etc.) to the end of the text, skipping comments.
        /// </summary>
        /// <param name="queryText">Original query text</param>
        /// <returns>Updated query text</returns>
        private static string MoveSettingsToTheEnd(string queryText)
        {
            if (string.IsNullOrEmpty(queryText))
                return queryText;

            var backParts = string.Empty;
            var index = 0;
            var regex = new Regex(RegexKeywordsAndComments, RegexOptions.Multiline);

            while (true)
            {
                if (index >= queryText.Length)
                    break;

                // find the next setting keyword or comment start
                var match = regex.Match(queryText, index);
                if (!match.Success)
                    break;

                // if it is not a keyword than it is a comment --> skip it
                if (!match.Value.StartsWith("."))
                {
                    index = GetCommentEndIndex(queryText, match.Index);
                    continue;
                }

                // if we do not recognise the keyword, skip it (it may be in the middle of a text between quotation marks)
                if (!QuerySettingParts.Contains(match.Groups["keyword"].Value))
                {
                    index = match.Index + match.Length;
                    continue;
                }

                // remove the setting from the original position and store it
                queryText = queryText.Remove(match.Index, match.Length);
                index = match.Index;
                backParts += " " + match.Value;
            }

            // add the stored settings to the end of the query
            return string.Concat(queryText, backParts);
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

        private static string FixMultilineComment(string queryText)
        {
            if (string.IsNullOrEmpty(queryText))
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

        private static int GetDefaultMaxResults()
        {
            return int.MaxValue;
        }
        private static string GetAutofilterForNodeQuery()
        {
            return "";
        }
        private static string GetLifespanFilterForNodeQuery()
        {
            return "";
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

            private static InnerQueryResult ExecuteInnerScript(string src, int top, int skip,
                IEnumerable<SortInfo> sort, FilterStatus enableAutofilters, FilterStatus enableLifespanFilter, QueryExecutionMode executionMode,
                QuerySettings settings, bool enableProjection, out int count)
            {
                LucQuery query;

                try
                {
                    query = LucQuery.Parse(src);
                }
                catch (ParserException ex)
                {
                    throw new InvalidContentQueryException(src, innerException: ex);
                }

                var projection = query.Projection;
                if (projection != null)
                {
                    if (!enableProjection)
                        throw new ApplicationException(String.Format("Projection in top level query is not allowed ({0}:{1})", Parser.SnLucLexer.Keywords.Select, projection));
                    query.ForceLuceneExecution = true;
                }

                if (skip != 0)
                    query.Skip = skip;

                if (top != 0)
                    query.PageSize = top;
                else
                    if (query.PageSize == 0)
                        query.PageSize = GetDefaultMaxResults();

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

                InnerQueryResult result;

                var qresult = query.Execute().ToList();
                if (projection == null || !enableProjection)
                {
                    var idResult = qresult.Select(o => o.NodeId).ToArray();
                    result = new InnerQueryResult { IsIntArray = true, IntArray = idResult, StringArray = idResult.Select(i => i.ToString()).ToArray() };
                }
                else
                {
                    var stringResult = qresult.Select(o => o[projection, false]).Where(r => !String.IsNullOrEmpty(r));
                    var escaped = new List<string>();
                    foreach (var s in stringResult)
                        escaped.Add(EscapeForQuery(s));
                    result = new InnerQueryResult { IsIntArray = false, StringArray = escaped.ToArray() };
                }

                count = query.TotalCount;

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
                                foreach (var c in SenseNet.Search.Parser.SnLucLexer.STRINGTERMINATORCHARS.ToCharArray())
                                    pattern.Append("\\" + c);
                                pattern.Append("]");
                                __escaperRegex = new Regex(pattern.ToString());
                            }
                        }
                    }
                    return __escaperRegex;
                }
            }

            public static string EscapeForQuery(string value)
            {
                if (EscaperRegex.IsMatch(value))
                    return String.Concat("'", value, "'");
                return value;
            }
        }

    }
}
