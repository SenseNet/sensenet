using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;
using SenseNet.Portal.Virtualization;
using SenseNet.Search;
using SNS = SenseNet.ContentRepository.Storage;
using SenseNet.Portal.OData.Parser;
using System.Linq.Expressions;
using SenseNet.ContentRepository.Storage;
using System.Text.RegularExpressions;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search.Querying;

namespace SenseNet.Portal.OData
{
    internal enum OutputFormat { None, JSON, VerboseJSON, Atom, Xml }

    /// <summary>
    /// Defines values for handling collection count.
    /// </summary>
    public enum InlineCount
    {
        /// <summary>
        /// Not defined value. This is the default.
        /// </summary>
        None,
        /// <summary>
        /// Defines that the client requires the total count of collection even if the
        ///  request has restrictions in connection with cardinality (e.g. "$top=10").
        /// </summary>
        AllPages
    }

    /// <summary>
    /// Defines values for metadata verbosity.
    /// </summary>
    public enum MetadataFormat
    {
        /// <summary>
        /// There is no metadata writing at all.
        /// </summary>
        None,
        /// <summary>
        /// Writing metadata with minimal information.
        /// </summary>
        Minimal,
        /// <summary>
        /// Writing the whole metadata. This is the default value.
        /// </summary>
        Full
    }

    /// <summary>
    /// Represents a class that contains all OData related elements of the current webrequest.
    /// </summary>
    public class ODataRequest
    {
        /// <summary>
        /// Gets the id of the requested <see cref="Content"/>.
        /// </summary>
        public int RequestedContentId { get; private set; }
        /// <summary>
        /// Gets the path of the requested <see cref="Content"/>.
        /// </summary>
        public string RepositoryPath { get; private set; }
        /// <summary>
        /// Gets the name of the requested property or null.
        /// </summary>
        public string PropertyName { get; private set; }
        /// <summary>
        /// Gets the value of the "query" webrequest's parameter 
        /// if there is one. Otherwise returns with null.
        /// </summary>
        public string ContentQueryText { get; private set; }
        /// <summary>
        /// Gets the value of the "scenario" webrequest's parameter 
        /// if there is one. Otherwise returns with null.
        /// </summary>
        public string Scenario { get; private set; }

        /// <summary>
        /// Gets a value that is true if the requested resource is a collection.
        /// </summary>
        public bool IsCollection { get; private set; }
        /// <summary>
        /// Gets a value that is true if the current webrequest is an OData metadata request
        /// (the URI of the requested resource contains the "$metadata" segment).
        /// </summary>
        public bool IsMetadataRequest { get; private set; }
        /// <summary>
        /// Gets true if the URI of the requested single resource refers to its member.
        /// </summary>
        public bool IsMemberRequest { get; private set; }
        /// <summary>
        /// Gets true if the URI of the requested single resource refers to its member's value.
        /// (the URI of the requested resource ends with the "$value" segment).
        /// </summary>
        public bool IsRawValueRequest { get; private set; }
        /// <summary>
        /// Gets true if the webrequest is the service document request.
        /// </summary>
        public bool IsServiceDocumentRequest { get; private set; }

        internal const string SCENARIO = "scenario";   // url param
        internal const string CONTENTQUERY = "query";  // url param
        private const string IDREQUEST_REGEX = "/content\\((?<id>\\d+)\\)";

        /// <summary>
        /// Gets the value of the "$top" OData parameter if it exists, otherwise returns with 0.
        /// </summary>
        public int Top { get; private set; }
        /// <summary>
        /// Gets the value of the "$skip" OData parameter if it exists, otherwise returns with 0.
        /// </summary>
        public int Skip { get; private set; }
        /// <summary>
        /// Gets the value of the "$inlinecount" OData parameter if it exists, otherwise returns with "None".
        /// </summary>
        public InlineCount InlineCount { get; private set; }
        /// <summary>
        /// Gets true if the last URI segment of the requested resource is "$count".
        /// </summary>
        public bool CountOnly { get; private set; }
        /// <summary>
        /// Gets the value of the "$format" OData parameter if it exists, otherwise returns with null.
        /// </summary>
        public string Format { get; internal set; }
        /// <summary>
        /// Gets the collection of <see cref="SortInfo"/> from the "$orderby" parameter of the webrequest.
        /// </summary>
        public IEnumerable<SortInfo> Sort { get; internal set; }
        /// <summary>
        /// Gets the string list of the projection from the "$select" parameter of the webrequest.
        /// </summary>
        public List<string> Select { get; private set; }
        /// <summary>
        /// Gets the string list of the expanded members from the "$expand" parameter of the webrequest.
        /// </summary>
        public List<string> Expand { get; private set; }
        /// <summary>
        /// Gets the parsed <see cref="Expression"/> from the "$filter" parameter of the webrequest.
        /// </summary>
        public Expression Filter { get; internal set; }
        /// <summary>
        /// Gets the value of the "enableautofilters" parameter of the webrequest if it exists, otherwise returs with FilterStatus.Default.
        /// </summary>
        public FilterStatus AutofiltersEnabled { get; internal set; }
        /// <summary>
        /// Gets the value of the "enablelifespanfilter" parameter of the webrequest if it exists, otherwise returs with FilterStatus.Default.
        /// </summary>
        public FilterStatus LifespanFilterEnabled { get; internal set; }
        /// <summary>
        /// Gets the value of the "queryexecutionmode" parameter of the webrequest if it exists, otherwise returs with FilterStatus.Default.
        /// </summary>
        public QueryExecutionMode QueryExecutionMode { get; internal set; }
        /// <summary>
        /// Gets the value of the "metadata" parameter of the webrequest if it exists, otherwise returs with MetadataFormat.Full.
        /// </summary>
        public MetadataFormat EntityMetadata { get; internal set; }
        /// <summary>
        /// Gets true if the "includebackurl" parameter of the webrequest is "true" if it exists, otherwise returs with false.
        /// </summary>
        public bool IncludeBackUrl { get; private set; }

        /// <summary>
        /// Gets true if the value of the Top property is greater than 0.
        /// </summary>
        public bool HasTop { get { return Top > 0; } }
        /// <summary>
        /// Gets true if the value of the Skip property is greater than 0.
        /// </summary>
        public bool HasSkip { get { return Skip > 0; } }
        /// <summary>
        /// Gets true if the value of the Sort property contains any element.
        /// </summary>
        public bool HasSort { get { return Sort.Count() > 0; } }
        /// <summary>
        /// Gets true if the value of the InlineCount property is not "None".
        /// </summary>
        public bool HasInlineCount { get { return InlineCount != InlineCount.None; } }
        /// <summary>
        /// Gets true if the value of the Select property contains any element.
        /// </summary>
        public bool HasSelect { get { return Select.Count > 0; } }
        /// <summary>
        /// Gets true if the value of the Expand property contains any element.
        /// </summary>
        public bool HasExpand { get { return Expand.Count > 0; } }
        /// <summary>
        /// Gets true if the Filter is not null.
        /// </summary>
        public bool HasFilter { get { return Filter != null; } }
        /// <summary>
        /// Gets true if the ContentQueryText property is not null.
        /// </summary>
        public bool HasContentQuery { get { return !String.IsNullOrEmpty(this.ContentQueryText); } }

        /// <summary>
        /// Gets true if the webrequest contains the "multistepsave" parameter with "true" value.
        /// </summary>
        public bool MultistepSave { get; private set; }

        /// <summary>
        /// Gets the <see cref="Exception"/> instance if there was any request parsing error.
        /// </summary>
        public Exception RequestError { get; private set; }

        private ODataRequest()
        {
            InlineCount = InlineCount.None;
            Sort = new SortInfo[0];
            Select = new List<string>();
            Expand = new List<string>();
            AutofiltersEnabled = FilterStatus.Default;
            IncludeBackUrl = true;
        }

        internal static ODataRequest CreateSingleContentRequest(IEnumerable<string> select = null, IEnumerable<string> expand = null)
        {
            var req = new ODataRequest();

            if (select != null)
                req.Select.AddRange(select);
            if (expand != null)
                req.Expand.AddRange(expand);

            return req;
        }

        internal static string GetContentPathFromODataRequest(string requestPath)
        {
            // requestPath must be the AbsolutePath of the request uri after the "/odata.svc"

            if (string.IsNullOrEmpty(requestPath))
                return string.Empty;
            if ("/" == requestPath)
                return string.Empty;
            var path = requestPath.Substring(1);

            // check if this is an id request instead of a path request
            var match = new Regex(IDREQUEST_REGEX, RegexOptions.IgnoreCase).Match(path);
            if (match.Success)
            {
                var idString = match.Groups["id"].Value;
                var nodeId = 0;
                if (int.TryParse(idString, out nodeId))
                {
                    var nodeHead = NodeHead.Get(nodeId);
                    if (nodeHead != null)
                        return nodeHead.Path;
                }
            }

            // remove everything after item request characters (property accessor or action/function name)
            var itemLastIndex = path.LastIndexOf("')");
            if (itemLastIndex > 0 && path.Length > itemLastIndex + 2)
                path = path.Substring(0, itemLastIndex + 2);

            // remove odata individual item request characters
            path = path.Replace("('", "/").Replace("')", string.Empty);
            path = path.TrimEnd('/');

            return path;
        }

        internal static ODataRequest Parse(string path, PortalContext portalContext)
        {
            if (!portalContext.IsOdataRequest)
                throw new InvalidOperationException("The Request is not an OData request.");

            var req = new ODataRequest();
            try
            {
                var relPath =
                    path.Substring(path.IndexOf(Configuration.Services.ODataServiceToken, StringComparison.OrdinalIgnoreCase) +
                                   Configuration.Services.ODataServiceToken.Length);

                req.IsServiceDocumentRequest = relPath.Length == 0;

                var segments = relPath.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
                var resSegments = new List<string>();
                var prmSegments = new List<string>();
                req.IsCollection = true;
                for (var i = 0; i < segments.Length; i++)
                {
                    if (String.Compare(segments[i], "$metadata", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        req.IsMetadataRequest = true;
                        break;
                    }
                    if (String.Compare(segments[i], "$value", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        req.IsRawValueRequest = true;
                        break;
                    }
                    if (String.Compare(segments[i], "$count", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        req.CountOnly = true;
                        break;
                    }

                    if (!req.IsCollection)
                    {
                        prmSegments.Add(segments[i]);
                        continue;
                    }

                    var segment = segments[i];
                    if (i == 0 && segment.StartsWith("content(", StringComparison.OrdinalIgnoreCase) &&
                        segment.EndsWith(")"))
                    {
                        var idStr = segment.Substring(8).Trim('(', ')');
                        if (idStr[0] != '\'' && idStr[idStr.Length - 1] != '\'')
                        {
                            int id;
                            if (!int.TryParse(idStr, out id))
                                throw new ODataException(SNSR.Exceptions.OData.InvalidId, ODataExceptionCode.InvalidId);
                            req.IsCollection = false;
                            req.RequestedContentId = id;
                            resSegments.Add(segment);
                            continue;
                        }
                    }
                    if (!segment.EndsWith("')"))
                    {
                        resSegments.Add(segment);
                    }
                    else
                    {
                        req.IsCollection = false;

                        var ii = segment.IndexOf("('");
                        var entityName = segment.Substring(ii).Trim('(', ')').Trim('\'');
                        segment = segment.Substring(0, ii);
                        if (segment.Length > 0)
                            resSegments.Add(segment);
                        resSegments.Add(entityName);
                    }
                }

                var pathIsRelative = true;
                if (resSegments.Count == 0)
                {
                    req.RepositoryPath = portalContext.Site?.Path;
                    if (req.RepositoryPath == null)
                        pathIsRelative = false;
                    req.IsCollection = true;
                }
                else
                {
                    pathIsRelative = String.Compare(resSegments[0], "root", StringComparison.OrdinalIgnoreCase) != 0;
                    if (pathIsRelative)
                        pathIsRelative = !resSegments[0].StartsWith("root(", StringComparison.OrdinalIgnoreCase);

                    if (prmSegments.Count > 0)
                    {
                        req.IsMemberRequest = true;
                        req.PropertyName = prmSegments[0];
                    }
                }

                Content content;
                if (req.RequestedContentId > 0 && (content = SystemAccount.Execute(() => Content.Load(req.RequestedContentId))) != null)
                {
                    req.RepositoryPath = content.Path;
                }
                else
                {
                    var newPath = String.Concat("/", String.Join("/", resSegments));
                    if (pathIsRelative)
                    {
                        if(portalContext.Site == null)
                            newPath = "/";
                        else
                            newPath = newPath == "/"
                                ? portalContext.Site.Path
                                : string.Concat(portalContext.Site.Path, newPath);
                    }
                    req.RepositoryPath = newPath;
                }

                req.ParseQuery(path, portalContext);
            }
            catch (Exception e)
            {
                req.RequestError = e;
            }
            return req;
        }
        private void ParseQuery(string path, PortalContext portalContext)
        {
            var req = portalContext.OwnerHttpContext.Request;

            // --------------------------------------------------------------- $top
            var topStr = req["$top"];
            int top = 0;
            if (topStr != null)
                if (!int.TryParse(topStr, out top))
                    throw new ODataException(SNSR.Exceptions.OData.InvalidTopOption, ODataExceptionCode.InvalidTopParameter);
            if (top < 0)
                throw new ODataException(SNSR.Exceptions.OData.InvalidTopOption, ODataExceptionCode.NegativeTopParameter);
            Top = top;

            // --------------------------------------------------------------- $skip
            var skipStr = req["$skip"];
            int skip = 0;
            if (skipStr != null)
                if (!int.TryParse(skipStr, out skip))
                    throw new ODataException(SNSR.Exceptions.OData.InvalidSkipOption, ODataExceptionCode.InvalidSkipParameter);
            if (skip < 0)
                throw new ODataException(SNSR.Exceptions.OData.InvalidSkipOption, ODataExceptionCode.NegativeSkipParameter);
            Skip = skip;

            // --------------------------------------------------------------- $orderby
            var orderStr = req["$orderby"];
            Sort = ParseSort(orderStr);

            // --------------------------------------------------------------- $inlinecount
            var inlineCountStr = req["$inlinecount"];
            var ic = InlineCount.None;
            if (inlineCountStr != null)
            {
                var x = Enum.TryParse<InlineCount>(inlineCountStr, true, out ic);
                if (!x || ic < 0 || (int)ic > 1)
                    throw new ODataException(SNSR.Exceptions.OData.InvalidInlineCountOption, ODataExceptionCode.InvalidInlineCountParameter);
            }
            InlineCount = ic;

            // --------------------------------------------------------------- $select
            var selectStr = req["$select"];
            this.Select = ParseSelect(selectStr);

            // --------------------------------------------------------------- $expand
            var expandStr = req["$expand"];
            this.Expand = ParseExpand(expandStr);

            // --------------------------------------------------------------- $filter
            var filterStr = req["$filter"];
            new ODataParser().Parse(filterStr, this);

            // --------------------------------------------------------------- scenario
            this.Scenario = ParseScenario(portalContext);

            // --------------------------------------------------------------- contentquery
            this.ContentQueryText = ParseContentQueryText(portalContext);

            // --------------------------------------------------------------- contentquery and filter options
            var booltext = req["enableautofilters"];
            if(!String.IsNullOrEmpty(booltext))
                AutofiltersEnabled = booltext == "true" ? FilterStatus.Enabled : FilterStatus.Disabled;
            booltext = req["enablelifespanfilter"];
            if (!String.IsNullOrEmpty(booltext))
                LifespanFilterEnabled = booltext == "true" ? FilterStatus.Enabled : FilterStatus.Disabled;

            var modeValue = req["queryexecutionmode"];
            QueryExecutionMode queryExecutionMode;
            if (!String.IsNullOrEmpty(modeValue))
                if (Enum.TryParse<QueryExecutionMode>(modeValue, true, out queryExecutionMode))
                    QueryExecutionMode = queryExecutionMode;

            // --------------------------------------------------------------- metadata format
            var metadatatext = req["metadata"];
            switch (metadatatext)
            {
                case "no": EntityMetadata = MetadataFormat.None; break;
                case "minimal": EntityMetadata = MetadataFormat.Minimal; break;
                default: EntityMetadata = MetadataFormat.Full; break;
            }
            // --------------------------------------------------------------- multistep saving
            var multistepStr = req["multistepsave"];
            if (!string.IsNullOrEmpty(multistepStr) && string.Compare(multistepStr, "true", StringComparison.OrdinalIgnoreCase) == 0)
                this.MultistepSave = true;

            // --------------------------------------------------------------- include back url
            var includeBackUrlStr = req["includebackurl"];
            if (!string.IsNullOrEmpty(includeBackUrlStr) && string.Compare(includeBackUrlStr, "false", StringComparison.OrdinalIgnoreCase) == 0)
                this.IncludeBackUrl = false;
        }
        private IEnumerable<SortInfo> ParseSort(string orderStr)
        {
            if (string.IsNullOrEmpty(orderStr))
                return new SortInfo[0];

            var sort = new List<SortInfo>();
            foreach (var sortStr in orderStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var sa = sortStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (sa.Length > 2)
                    throw new ODataException(SNSR.Exceptions.OData.InvalidOrderByOption, ODataExceptionCode.InvalidOrderByParameter);
                bool reverse = false;
                if (sa.Length == 2)
                {
                    if (sa[1] == "desc")
                        reverse = true;
                    else if (sa[1] != "asc")
                        throw new ODataException(SNSR.Exceptions.OData.InvalidOrderByOption, ODataExceptionCode.InvalidOrderByDirectionParameter);
                }
                sort.Add(new SortInfo(sa[0].Trim(), reverse));
            }
            return sort;
        }
        private List<string> ParseSelect(string selectStr)
        {
            if (string.IsNullOrEmpty(selectStr))
                return new List<string>();
            var x = selectStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            if (x.Count == 1 && x[0] == "*")
                x.RemoveAt(0);
            return x;
        }
        private List<string> ParseExpand(string expandStr)
        {
            if (string.IsNullOrEmpty(expandStr))
                return new List<string>();
            var x = expandStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            return x;
        }
        private string ParseScenario(PortalContext portalContext)
        {
            return portalContext.OwnerHttpContext.Request[SCENARIO];
        }
        private string ParseContentQueryText(PortalContext portalContext)
        {
            return portalContext.OwnerHttpContext.Request[CONTENTQUERY];
        }
    }
}