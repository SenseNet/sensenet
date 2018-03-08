using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search.Querying;
using SenseNet.Search.Querying.Parser;

// ReSharper disable once CheckNamespace
namespace SenseNet.Search
{
    /// <summary>
    /// Represents a content query and encapsulates the content query related operations.
    /// </summary>
    public class ContentQuery
    {
        /// <summary>
        /// Gets the empty text representation in a simple predicate.
        /// </summary>
        public static string EmptyText => SnQuery.EmptyText;

        private static readonly string[] QuerySettingParts = { "SKIP", "TOP", "SORT", "REVERSESORT", "AUTOFILTERS", "LIFESPAN", "COUNTONLY" };
        private static readonly string RegexKeywordsAndComments = "//|/\\*|(\\.(?<keyword>[A-Z]+)(([ ]*:[ ]*[#]?\\w+(\\.\\w+)?)|([\\) $\\r\\n]+)))";
        private static readonly string RegexCommentEndSingle = "$";
        private static readonly string RegexCommentEndMulti = "\\*/|\\z";
        private static readonly string MultilineCommentStart = "/*";
        private static readonly string MultilineCommentEnd = "*/";

        private string _text;
        /// <summary>
        /// Gets or sets the CQL query text.
        /// </summary>
        public string Text
        {
            get => _text;
            set => _text = FixMultilineComment(value);
        }

        private QuerySettings _settings = new QuerySettings();
        /// <summary>
        /// Gets or sets an instance of the <see cref="QuerySettings"/> that is the extension of the represented query.
        /// </summary>
        public QuerySettings Settings
        {
            get => _settings;
            set => _settings = value ?? new QuerySettings();
        }

        /// <summary>
        /// Gets a value that is "true" if the query is safe.
        /// </summary>
        public bool IsSafe { get; internal set; }

        private static readonly Regex EscaperRegex;
        static ContentQuery()
        {
            var pattern = new StringBuilder("[");
            foreach (var c in Cql.StringTerminatorChars.ToCharArray())
                pattern.Append("\\" + c);
            pattern.Append("]");
            EscaperRegex = new Regex(pattern.ToString());
        }

        /// <summary>
        /// Returns with the <see cref="QueryResult"/> of the given CQL query.
        /// </summary>
        /// <param name="text">CQL query text.</param>
        public static QueryResult Query(string text)
        {
            return Query(text, null, null);
        }
        /// <summary>
        /// Returns with the <see cref="QueryResult"/> of the given CQL query.
        /// </summary>
        /// <param name="text">CQL query text.</param>
        /// <param name="settings"><see cref="QuerySettings"/> that extends the query.</param>
        /// <param name="parameters">Values to substitute the parameters of the CQL query text.</param>
        /// <returns></returns>
        public static QueryResult Query(string text, QuerySettings settings, params object[] parameters)
        {
            return CreateQuery(text, settings, parameters).Execute();
        }

        /// <summary>
        /// Returns with a new instance of the ContentQuery.
        /// </summary>
        /// <param name="text">CQL text of the query.</param>
        public static ContentQuery CreateQuery(string text)
        {
            return CreateQuery(text, null, null);
        }
        /// <summary>
        /// Returns with a new instance of the ContentQuery.
        /// </summary>
        /// <param name="text">CQL text of the query.</param>
        /// <param name="settings"><see cref="QuerySettings"/> that extends the query.</param>
        /// <param name="parameters">Values to substitute the parameters of the CQL query text.</param>
        /// <returns></returns>
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
        private static bool IsSafeQuery(string queryText)
        {
            return SafeQueries.IsSafe(queryText);
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
                    while (++q < text.Length && char.IsDigit(text[q])) { /* do nothing */ }
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
            if (!(value is string) && value is IEnumerable enumerableValue)
            {
                var escaped = new List<string>();
                foreach (var x in enumerableValue)
                    if (x != null)
                        escaped.Add(EscapeParameter(x.ToString()));
                var joined = string.Join(" ", escaped);
                if (escaped.Count < 2)
                    return joined;
                return "(" + joined + ")";
            }

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

        /// <summary>
        /// Extends the Text property with the given additional clause. Uses AND relation.
        /// </summary>
        /// <param name="text">The additional clause.</param>
        public void AddClause(string text)
        {
            AddClause(text, LogicalOperator.And);
        }
        /// <summary>
        /// Extends the Text property with the given additional clause.
        /// </summary>
        /// <param name="text">The additional clause.</param>
        /// <param name="logicalOp">The operator in the concatenation. Can be AND / OR.</param>
        public void AddClause(string text, LogicalOperator logicalOp)
        {
            AddClause(text, logicalOp, null);
        }
        /// <summary>
        /// Extends the Text property with the given additional clause.
        /// </summary>
        /// <param name="text">The additional clause.</param>
        /// <param name="logicalOp">The operator in the concatenation. Can be AND / OR.</param>
        /// <param name="parameters">Values to substitute the parameters of the additional clause.</param>
        public void AddClause(string text, LogicalOperator logicalOp, params object[] parameters)
        {
            var isSafe = IsSafe && IsSafeQuery(text);
            if (parameters != null && parameters.Length > 0)
                text = SubstituteParameters(text, parameters);
            IsSafe = isSafe;
            AddClausePrivate(text, logicalOp);
        }
        private void AddClausePrivate(string text, LogicalOperator logicalOp)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            if (text.Length == 0)
                throw new ArgumentException("Clause cannot be empty", nameof(text));

            if (string.IsNullOrEmpty(Text))
            {
                Text = text;
            }
            else
            {
                // we can modify the _text variable here directly because it was already fixed at init time
                switch (logicalOp)
                {
                    case LogicalOperator.And:
                        _text = MoveSettingsToTheEnd($"+({Text}) +({text})").Trim();
                        break;
                    case LogicalOperator.Or:
                        _text = MoveSettingsToTheEnd($"({Text}) {text}");
                        break;
                }
            }
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

        /// <summary>
        /// Returns with a combination of a valid CQL query and an additional clause.
        /// </summary>
        public static string AddClause(string originalText, string addition, LogicalOperator logicalOp)
        {
            if (addition == null)
                throw new ArgumentNullException(nameof(addition));
            if (addition.Length == 0)
                throw new ArgumentException("Clause cannot be empty", nameof(addition));

            if (string.IsNullOrEmpty(originalText))
                return addition;

            var queryText = string.Empty;

            switch (logicalOp)
            {
                case LogicalOperator.And:
                    queryText = MoveSettingsToTheEnd($"+({originalText}) +({addition})").Trim();
                    break;
                case LogicalOperator.Or:
                    queryText = MoveSettingsToTheEnd($"({originalText}) {addition}");
                    break;
            }

            return queryText;
        }


        private static string FixMultilineComment(string queryText)
        {
            if (string.IsNullOrEmpty(queryText))
                return queryText;

            // find the last multiline comment
            var commentStartIndex = queryText.LastIndexOf(MultilineCommentStart, StringComparison.Ordinal);
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

        /// <summary>
        /// Executes the represented query and returns with the QueryResult.
        /// </summary>
        public QueryResult Execute()
        {
            var queryText = Text;

            if (string.IsNullOrEmpty(queryText))
                throw new InvalidOperationException("Cannot execute query with null or empty Text");

            var userId = AccessProvider.Current.GetCurrentUser().Id;
            if (userId == Identifiers.SystemUserId && !IsSafe)
            {
                var ex = new InvalidOperationException("Cannot execute this query, please convert it to a safe query.");
                ex.Data.Add("EventId", EventId.Querying);
                ex.Data.Add("Query", _text);

                throw ex;
            }

            QueryResult result;
            using (var op = SnTrace.Query.StartOperation("ContentQuery: {0} | Top:{1} Skip:{2} Sort:{3} Mode:{4} AllVersions:{5}", queryText, _settings.Top, _settings.Skip, _settings.Sort, _settings.QueryExecutionMode, _settings.AllVersions))
            {
                var query = TemplateManager.Replace(typeof(ContentQueryTemplateReplacer), queryText);

                result = query.Contains("}}")
                    ? RecursiveExecutor.ExecuteRecursive(query, Settings, userId)
                    : Execute(query, new SnQueryContext(Settings, userId));

                op.Successful = true;
            }
            return result;
        }

        private static QueryResult Execute(string query, SnQueryContext context)
        {
            try
            {
                var snQueryResultresult = SnQuery.Query(query, context);
                return new QueryResult(snQueryResultresult.Hits, snQueryResultresult.TotalCount);
            }
            catch (ParserException ex)
            {
                throw new InvalidContentQueryException(query, innerException: ex);
            }
        }
        private static string[] ExecuteAndProject(string query, SnQueryContext context)
        {
            try
            {
                var snQueryresult = SnQuery.QueryAndProject(query, context);
                return snQueryresult.Hits
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Select(s => EscaperRegex.IsMatch(s) ? string.Concat("'", s, "'") : s)
                    .ToArray();
            }
            catch (ParserException ex)
            {
                throw new InvalidContentQueryException(query, innerException: ex);
            }
        }

        // ================================================================== Recursive executor class

        private static class RecursiveExecutor
        {
            public static QueryResult ExecuteRecursive(string queryText, QuerySettings querySettings, int userId)
            {
                QueryResult result;

                var src = queryText;
                var control = GetControlString(src);

                var recursiveQuerySettings = new QuerySettings
                {
                    Skip = 0,
                    Top = 0,
                    Sort = querySettings.Sort,
                    EnableAutofilters = querySettings.EnableAutofilters,
                    EnableLifespanFilter = querySettings.EnableLifespanFilter,
                    QueryExecutionMode = querySettings.QueryExecutionMode,
                    // AllVersions be always false in the inner queries
                };
                var recursiveQueryContext = new SnQueryContext(recursiveQuerySettings, userId);

                while (true)
                {
                    var innerScript = GetInnerScript(src, control, out var start);
                    var end = innerScript == string.Empty;

                    if (!end)
                    {
                        src = src.Remove(start, innerScript.Length);
                        control = control.Remove(start, innerScript.Length);

                        // execute inner query
                        var subQuery = innerScript.Substring(2, innerScript.Length - 4);
                        var innerResult = ExecuteAndProject(subQuery, recursiveQueryContext);

                        // process inner query result
                        switch (innerResult.Length)
                        {
                            case 0:
                                innerScript = SnQuery.EmptyInnerQueryText;
                                break;
                            case 1:
                                innerScript = innerResult[0];
                                break;
                            default:
                                innerScript = string.Join(" ", innerResult);
                                innerScript = "(" + innerScript + ")";
                                break;
                        }
                        src = src.Insert(start, innerScript);
                        control = control.Insert(start, innerScript);
                    }
                    else
                    {
                        // execute and process top level query
                        result = Execute(src, new SnQueryContext(querySettings, userId));
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
                                    instr = false;
                                @out.Append('_');
                            }
                            else
                            {
                                if (c == '\'' || c == '"')
                                {
                                    instr = true;
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

                return @out.ToString();
            }
            private static string GetInnerScript(string src, string control, out int start)
            {
                start = 0;
                var p1 = control.IndexOf("}}", StringComparison.Ordinal);
                if (p1 < 0)
                    return string.Empty;
                var p0 = control.LastIndexOf("{{", p1, StringComparison.Ordinal);
                if (p0 < 0)
                    return string.Empty;
                start = p0;
                var ss = src.Substring(p0, p1 - p0 + 2);
                return ss;
            }
        }
    }
}
