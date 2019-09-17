using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SenseNet.ContentRepository;
using System.IO;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository.Linq;
using System.Web;
using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;
//using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search.Querying;
using SenseNet.Tools;
using Utility = SenseNet.Tools.Utility;
// ReSharper disable ArrangeThisQualifier

namespace SenseNet.OData
{
    /// <summary>
    /// Defines a base class for serializing the OData response object to various formats.
    /// </summary>
    public abstract class ODataFormatter
    {
        /// <summary>
        /// Gets the name of the format that is used in the "$format" parameter of the OData webrequest.
        /// </summary>
        public abstract string FormatName { get; }
        /// <summary>
        /// Gets the mime type of the converted object.
        /// </summary>
        public abstract string MimeType { get; }

        internal ODataRequest ODataRequest { get; private set; }

        private static readonly object FormatterTypeLock = new object();
        private static Dictionary<string, Type> _formatterTypes;
        internal static Dictionary<string, Type> FormatterTypes
        {
            get
            {
                if (_formatterTypes == null)
                {
                    lock(FormatterTypeLock)
                    {
                        if (_formatterTypes == null)
                        {
                            _formatterTypes = LoadFormatterTypes();

                            SnLog.WriteInformation("OData formatter types loaded: " +
                                string.Join(", ", _formatterTypes.Values.Select(t => t.FullName)));
                        }
                    }
                }

                return _formatterTypes;
            }
        }

        private static Dictionary<string, Type> LoadFormatterTypes()
        {
            var formatterTypes = new Dictionary<string, Type>();
            var types = TypeResolver.GetTypesByBaseType(typeof(ODataFormatter));

            foreach (var type in types)
            {
                var protoType = (ODataFormatter)Activator.CreateInstance(type);
                formatterTypes[protoType.FormatName] = type;
            }

            return formatterTypes;
        }

        internal static ODataFormatter Create(HttpContext httpContext, ODataRequest odataReq)
        {
            var formatName = httpContext.Request.Query["$format"].ToString();
            if (string.IsNullOrEmpty(formatName))
                formatName = odataReq == null ? "json" : odataReq.IsMetadataRequest ? "xml" : "json";
            else
                formatName = formatName.ToLower();

            return Create(formatName);
        }
        internal static ODataFormatter Create(string formatName)
        {
            if (!FormatterTypes.TryGetValue(formatName, out var formatterType))
                return null;

            var formatter = (ODataFormatter)Activator.CreateInstance(formatterType);
            return formatter;
        }

        internal void Initialize(ODataRequest odataRequest)
        {
            this.ODataRequest = odataRequest;
        }

        // --------------------------------------------------------------------------------------------------------------- metadata

        /// <summary>
        /// Writes the OData service document with the given root names to the webresponse.
        /// </summary>
        /// <param name="httpContext">The current <see cref="HttpContext"/> instance containing the current web-response.</param>
        /// <param name="names">Root names.</param>
        protected abstract void WriteServiceDocument(HttpContext httpContext, IEnumerable<string> names);

        internal void WriteMetadata(HttpContext httpContext, ODataRequest req)
        {
            //var content = ODataHandler.LoadContentByVersionRequest(req.RepositoryPath, httpContext);

            //var isRoot = content?.ContentType.IsInstaceOfOrDerivedFrom("Site") ?? true;
            //if (isRoot)
            //    MetaGenerator.WriteMetadata(httpContext.Response.Output, this);
            //else
            //    MetaGenerator.WriteMetadata(httpContext.Response.Output, this, content, req.IsCollection);

            //var mimeType = this.MimeType;
            //if (mimeType != null)
            //    httpContext.Response.ContentType = mimeType;
            throw new NotImplementedException(); //UNDONE:ODATA: Not implemented.
        }
        internal void WriteMetadataInternal(TextWriter writer, Metadata.Edmx edmx)
        {
            WriteMetadata(writer, edmx);
        }
        /// <summary>
        /// Writes the OData service metadata to the given text writer
        /// </summary>
        /// <param name="writer">Output writer.</param>
        /// <param name="edmx">Metadata that will be written.</param>
        protected abstract void WriteMetadata(TextWriter writer, Metadata.Edmx edmx);

        // --------------------------------------------------------------------------------------------------------------- contents

        internal void WriteSingleContent(String path, HttpContext httpContext)
        {
            WriteSingleContent(ODataHandler.LoadContentByVersionRequest(path, httpContext), httpContext);
        }
        internal void WriteSingleContent(Content content, HttpContext httpContext)
        {
            var fields = CreateFieldDictionary(content, false, httpContext);
            WriteSingleContent(httpContext, fields);
        }
        /// <summary>
        /// Writes the given fields of a Content to the webresponse.
        /// </summary>
        /// <param name="httpContext">The current <see cref="HttpContext"/> instance containing the current web-response.</param>
        /// <param name="fields">A Dictionary&lt;string, object&gt; that will be written.</param>
        protected abstract void WriteSingleContent(HttpContext httpContext, Dictionary<string, object> fields);

        internal void WriteChildrenCollection(String path, HttpContext httpContext, ODataRequest req)
        {
            var content = Content.Load(path);
            var chdef = content.ChildrenDefinition;
            if (req.HasContentQuery)
            {
                chdef.ContentQuery = ContentQuery.AddClause(req.ContentQueryText, String.Concat("InTree:'", path, "'"), LogicalOperator.And);

                if (req.AutofiltersEnabled != FilterStatus.Default)
                    chdef.EnableAutofilters = req.AutofiltersEnabled;
                if (req.LifespanFilterEnabled != FilterStatus.Default)
                    chdef.EnableLifespanFilter = req.LifespanFilterEnabled;
                if (req.QueryExecutionMode != QueryExecutionMode.Default)
                    chdef.QueryExecutionMode = req.QueryExecutionMode;
                if (req.Top > 0)
                    chdef.Top = req.Top;
                if (req.Skip > 0)
                    chdef.Skip = req.Skip;
                if (req.Sort.Any())
                    chdef.Sort = req.Sort;
            }
            else
            {
                chdef.EnableAutofilters = FilterStatus.Disabled;
                if (string.IsNullOrEmpty(chdef.ContentQuery))
                {
                    chdef.ContentQuery = ContentQuery.AddClause(chdef.ContentQuery, String.Concat("InFolder:'", path, "'"), LogicalOperator.And);
                }
            }


            var contents = ProcessOperationQueryResponse(chdef, req, httpContext, out var count);
            if (req.CountOnly)
                WriteCount(httpContext, count);
            else
                WriteMultipleContent(httpContext, contents, count);
        }
        private void WriteMultiRefContents(object references, HttpContext httpContext, ODataRequest req)
        {
            if (references == null)
                return;

            var node = references as Node;
            var projector = Projector.Create(req, true);
            if (node != null)
            {
                var contents = new List<Dictionary<string, object>>
                {
                    CreateFieldDictionary(Content.Create(node), projector,httpContext)
                };
                //TODO: ODATA: multiref item: get available types from reference property
                WriteMultipleContent(httpContext, contents, 1);
            }
            else
            {
                if (references is IEnumerable enumerable)
                {
                    var skipped = 0;
                    var allcount = 0;
                    var count = 0;
                    var realcount = 0;
                    var contents = new List<Dictionary<string, object>>();
                    if (req.HasFilter)
                    {
                        var filtered = new FilteredEnumerable(enumerable, (LambdaExpression)req.Filter, req.Top, req.Skip);
                        foreach (Node item in filtered)
                            contents.Add(CreateFieldDictionary(Content.Create(item), projector, httpContext));
                        allcount = filtered.AllCount;
                        realcount = contents.Count;
                    }
                    else
                    {
                        foreach (Node item in enumerable)
                        {
                            allcount++;
                            if (skipped++ < req.Skip)
                                continue;
                            if (req.Top == 0 || count++ < req.Top)
                            {
                                contents.Add(CreateFieldDictionary(Content.Create(item), projector, httpContext));
                                realcount++;
                            }
                        }
                    }
                    WriteMultipleContent(httpContext, contents, req.InlineCount == InlineCount.AllPages ? allcount : realcount);
                }
            }
        }
        private void WriteSingleRefContent(object references, HttpContext httpContext)
        {
            if (references != null)
            {
                if (references is Node node)
                {
                    WriteSingleContent(httpContext, CreateFieldDictionary(Content.Create(node), false, httpContext));
                }
                else
                {
                    if (references is IEnumerable enumerable)
                    {
                        foreach (Node item in enumerable)
                        {
                            WriteSingleContent(httpContext, CreateFieldDictionary(Content.Create(item), false, httpContext));
                            break;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Writes the given Content list to the webresponse.
        /// </summary>
        /// <param name="httpContext">The current <see cref="HttpContext"/> instance containing the current web-response.</param>
        /// <param name="contents">A List&lt;Dictionary&lt;string, object&gt;&gt; that will be written.</param>
        /// <param name="count">Count of contents. This value can be different from the count of the written content list if the request has restrictions in connection with cardinality (e.g. "$top=10") but specifies the total count of the collection ("$inlinecount=allpages").</param>
        protected abstract void WriteMultipleContent(HttpContext httpContext, List<Dictionary<string, object>> contents, int count);
        /// <summary>
        /// Writes only the count of the requested resource to the webresponse.
        /// Activated if the URI of the requested resource contains the "$count" segment.
        /// </summary>
        /// <param name="httpContext">The current <see cref="HttpContext"/> instance containing the current web-response.</param>
        /// <param name="count"></param>
        protected abstract void WriteCount(HttpContext httpContext, int count);

        internal void WriteContentProperty(String path, string propertyName, bool rawValue, HttpContext httpContext, ODataRequest req)
        {
            var content = ODataHandler.LoadContentByVersionRequest(path, httpContext);
            if (content == null)
            {
                ODataHandler.ContentNotFound(httpContext);
                return;
            }

            if (propertyName == ODataHandler.ActionsPropertyName)
            {
                WriteActionsProperty(httpContext, ODataTools.GetActionItems(content, req, httpContext).ToArray(), rawValue);
                return;
            }
            if (propertyName == ODataHandler.ChildrenPropertyName)
            {
                WriteChildrenCollection(path, httpContext, req);
                return;
            }

            if (content.Fields.TryGetValue(propertyName, out var field))
            {
                if (field is ReferenceField refField)
                {
                    var refFieldSetting = refField.FieldSetting as ReferenceFieldSetting;
                    var isMultiRef = true;
                    if (refFieldSetting != null)
                        isMultiRef = refFieldSetting.AllowMultiple == true;
                    if (isMultiRef)
                    {
                        WriteMultiRefContents(refField.GetData(), httpContext, req);
                    }
                    else
                    {
                        WriteSingleRefContent(refField.GetData(), httpContext);
                    }
                }
                else if (field is AllowedChildTypesField actField)
                {
                    WriteMultiRefContents(actField.GetData(), httpContext, req);
                }
                else if (!rawValue)
                {
                    WriteSingleContent(httpContext, new Dictionary<string, object> { { propertyName, field.GetData() } });
                }
                else
                {
                    WriteRaw(field.GetData(), httpContext);
                }
            }
            else
            {
                WriteOperationResult(httpContext, req);
            }
        }
        /// <summary>
        /// Writes the available actions of the current <see cref="Content"/> to the webresponse.
        /// </summary>
        /// <param name="httpContext">The current <see cref="HttpContext"/> instance containing the current web-response.</param>
        /// <param name="actions">Array of <see cref="ODataActionItem"/> that will be written.</param>
        /// <param name="raw"></param>
        protected abstract void WriteActionsProperty(HttpContext httpContext, ODataActionItem[] actions, bool raw);



        internal void WriteErrorResponse(HttpContext context, ODataException oe)
        {
            var error = new Error
            {
                Code = string.IsNullOrEmpty(oe.ErrorCode) ? Enum.GetName(typeof(ODataExceptionCode), oe.ODataExceptionCode) : oe.ErrorCode,
                ExceptionType = oe.InnerException?.GetType().Name ?? oe.GetType().Name,
                Message = new ErrorMessage
                {
                    Lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower(),
                    Value = SNSR.GetString(oe.Message).Replace(Environment.NewLine, "\\n").Replace('"', ' ').Replace('\'', ' ').Replace(" \\ ", " ")
                },
                InnerError =
#if DEBUG
new StackInfo
{
    Trace = Utility.CollectExceptionMessages(oe)
}
#else
                HttpContext.Current != null && HttpContext.Current.IsDebuggingEnabled 
                    ? new StackInfo { Trace = Utility.CollectExceptionMessages(oe) }
                    : null
#endif
            };
            context.Response.ContentType = "application/json";
            WriteError(context, error);
            context.Response.StatusCode = oe.HttpStatusCode;
            //UNDONE:ODATA: Search ASPNET Core alternative of this: Response.TrySkipIisCustomErrors
            //context.Response.TrySkipIisCustomErrors = true;

        }
        /// <summary>
        /// Writes the given <see cref="Error"/> instance to the webresponse.
        /// </summary>
        /// <param name="context">The current <see cref="HttpContext"/> instance.</param>
        /// <param name="error">The <see cref="Error"/> instance that will be written.</param>
        protected abstract void WriteError(HttpContext context, Error error);

        // --------------------------------------------------------------------------------------------------------------- operations

        /// <summary>
        /// Handles GET operations. Parameters come from the URL or the request stream.
        /// </summary>
        internal void WriteOperationResult(HttpContext httpContext, ODataRequest odataReq)
        {
            var content = ODataHandler.LoadContentByVersionRequest(odataReq.RepositoryPath, httpContext);
            if (content == null)
                throw new ContentNotFoundException(string.Format(SNSR.GetString("$Action,ErrorContentNotFound"), odataReq.RepositoryPath));

            var action = ODataHandler.ActionResolver.GetAction(content, odataReq.Scenario, odataReq.PropertyName, null, null, httpContext);
            if (action == null)
            {
                // check if this is a versioning action (e.g. a checkout)
                SavingAction.AssertVersioningAction(content, odataReq.PropertyName, true);

                throw new InvalidContentActionException(InvalidContentActionReason.UnknownAction, content.Path, null, odataReq.PropertyName);
            }

            if (!action.IsODataOperation)
                throw new ODataException("Not an OData operation.", ODataExceptionCode.IllegalInvoke);
            if (action.CausesStateChange)
                throw new ODataException("OData action cannot be invoked with HTTP GET.", ODataExceptionCode.IllegalInvoke);

            if (action.Forbidden || (action.GetApplication() != null && !action.GetApplication().Security.HasPermission(PermissionType.RunApplication)))
                throw new InvalidContentActionException("Forbidden action: " + odataReq.PropertyName);

            var parameters = GetOperationParameters(action, httpContext.Request);
            var response = action.Execute(content, parameters);

            if (response is Content responseAsContent)
            {
                WriteSingleContent(responseAsContent, httpContext);
                return;
            }

            response = ProcessOperationResponse(response, odataReq, httpContext, out var count);
            WriteOperationResult(response, httpContext, odataReq, count);
        }
        /// <summary>
        /// Handles POST operations. Parameters come from request stream.
        /// </summary>
        internal void WriteOperationResult(Stream inputStream, HttpContext httpContext, ODataRequest odataReq)
        {
            var content = ODataHandler.LoadContentByVersionRequest(odataReq.RepositoryPath, httpContext);

            if (content == null)
                throw new ContentNotFoundException(string.Format(SNSR.GetString("$Action,ErrorContentNotFound"), odataReq.RepositoryPath));

            var action = ODataHandler.ActionResolver.GetAction(content, odataReq.Scenario, odataReq.PropertyName, null, null, httpContext);
            if (action == null)
            {
                // check if this is a versioning action (e.g. a checkout)
                SavingAction.AssertVersioningAction(content, odataReq.PropertyName, true);

                throw new InvalidContentActionException(InvalidContentActionReason.UnknownAction, content.Path, null, odataReq.PropertyName);
            }

            if (action.Forbidden || (action.GetApplication() != null && !action.GetApplication().Security.HasPermission(PermissionType.RunApplication)))
                throw new InvalidContentActionException("Forbidden action: " + odataReq.PropertyName);

            var parameters = GetOperationParameters(action, inputStream);
            var response = action.Execute(content, parameters);

            if (response is Content responseAsContent)
            {
                WriteSingleContent(responseAsContent, httpContext);
                return;
            }

            response = ProcessOperationResponse(response, odataReq, httpContext, out var count);
            WriteOperationResult(response, httpContext, odataReq, count);
        }
        private void WriteOperationResult(object result, HttpContext httpContext, ODataRequest odataReq, int allCount)
        {
            if (result is Content content)
            {
                WriteSingleContent(content, httpContext);
                return;
            }

            if (result is IEnumerable<Content> enumerable)
            {
                WriteMultiRefContents(enumerable, httpContext, odataReq);
                return;
            }

            WriteOperationCustomResult(httpContext, result, odataReq.InlineCount == InlineCount.AllPages ? allCount : (int?)null);
        }
        /// <summary>
        /// Writes a custom operations's result object to the webresponse.
        /// </summary>
        /// <param name="httpContext">The current <see cref="HttpContext"/> instance containing the current web-response.</param>
        /// <param name="result">The object that will be written.</param>
        /// <param name="allCount">A nullable int that contains the count of items in the result object 
        /// if the request specifies the total count of the collection ("$inlinecount=allpages"), otherwise the value is null.</param>
        protected abstract void WriteOperationCustomResult(HttpContext httpContext, object result, int? allCount);

        private object ProcessOperationResponse(object response, ODataRequest odataReq, HttpContext httpContext, out int count)
        {
            if (response is ChildrenDefinition qdef)
                return ProcessOperationQueryResponse(qdef, odataReq, httpContext, out count);

            if (response is IEnumerable<Content> coll)
                return ProcessOperationCollectionResponse(coll, odataReq, httpContext, out count);

            if (response is IDictionary dict)
            {
                count = dict.Count;
                var targetTypized = new Dictionary<Content, object>();
                foreach (var item in dict.Keys)
                {
                    if (!(item is Content content))
                        return response;
                    targetTypized.Add(content, dict[content]);
                }
                return ProcessOperationDictionaryResponse(targetTypized, odataReq, httpContext, out count);
            }

            // get real count from an enumerable
            if (response is IEnumerable enumerable)
            {
                var c = 0;
                // ReSharper disable once UnusedVariable
                foreach (var x in enumerable)
                    c++;
                count = c;
            }
            else
            {
                count = 1;
            }

            if (response != null && response.ToString() == "{ PreviewAvailable = True }")
                return true;
            if (response != null && response.ToString() == "{ PreviewAvailable = False }")
                return false;
            return response;
        }
        private List<Dictionary<string, object>> ProcessOperationQueryResponse(ChildrenDefinition qdef, ODataRequest req, HttpContext httpContext, out int count)
        {
            var queryText = qdef.ContentQuery;
            if (queryText.Contains("}}"))
            {
                queryText = ContentQuery.ResolveInnerQueries(qdef.ContentQuery, new QuerySettings
                {
                    EnableAutofilters = qdef.EnableAutofilters,
                    EnableLifespanFilter = qdef.EnableLifespanFilter,
                    QueryExecutionMode = qdef.QueryExecutionMode,
                    Sort = qdef.Sort
                });
            }

            var cdef = new ChildrenDefinition
            {
                PathUsage = qdef.PathUsage,
                ContentQuery = queryText,
                Top = req.Top > 0 ? req.Top : qdef.Top,
                Skip = req.Skip > 0 ? req.Skip : qdef.Skip,
                Sort = req.Sort != null && req.Sort.Any() ? req.Sort : qdef.Sort,
                CountAllPages = req.HasInlineCount ? req.InlineCount == InlineCount.AllPages : qdef.CountAllPages,
                EnableAutofilters = req.AutofiltersEnabled != FilterStatus.Default ? req.AutofiltersEnabled : qdef.EnableAutofilters,
                EnableLifespanFilter = req.LifespanFilterEnabled != FilterStatus.Default ? req.AutofiltersEnabled : qdef.EnableLifespanFilter,
                QueryExecutionMode = req.QueryExecutionMode != QueryExecutionMode.Default ? req.QueryExecutionMode : qdef.QueryExecutionMode,
            };

            var snQuery = SnExpression.BuildQuery(req.Filter, typeof(Content), null, cdef);
            if (cdef.EnableAutofilters != FilterStatus.Default)
                snQuery.EnableAutofilters = cdef.EnableAutofilters;
            if (cdef.EnableLifespanFilter != FilterStatus.Default)
                snQuery.EnableLifespanFilter = cdef.EnableLifespanFilter;
            if (cdef.QueryExecutionMode != QueryExecutionMode.Default)
                snQuery.QueryExecutionMode = cdef.QueryExecutionMode;

            var result = snQuery.Execute(new SnQueryContext(null, User.Current.Id));
            // for optimization purposes this combined condition is examined separately
            if (req.InlineCount == InlineCount.AllPages && req.CountOnly)
            {
                count = result.TotalCount;
                return null;
            }

            var ids = result.Hits.ToArray();
            count = req.InlineCount == InlineCount.AllPages ? result.TotalCount : ids.Length;
            if (req.CountOnly)
            {
                return null;
            }

            var contents = new List<Dictionary<string, object>>();
            var projector = Projector.Create(req, true);
            var missingIds = new List<int>();

            foreach (var id in ids)
            {
                var content = Content.Load(id);
                if (content == null)
                {
                    // collect missing ids for logging purposes
                    missingIds.Add(id);
                    continue;
                }

                var fields = CreateFieldDictionary(content, projector, httpContext);
                contents.Add(fields);
            }

            if (missingIds.Count > 0)
            {
                // subtract missing count from result count
                count = Math.Max(0, count - missingIds.Count);

                // index anomaly: there are ids in the index that could not be loaded from the database
                SnLog.WriteWarning("Missing ids found in the index that could not be loaded from the database. See id list below.",
                    EventId.Indexing,
                    properties: new Dictionary<string, object>
                    {
                        {"MissingIds", string.Join(", ", missingIds.OrderBy(id => id))}
                    });
            }

            return contents;
        }
        private List<Dictionary<string, object>> ProcessOperationDictionaryResponse(IDictionary<Content, object> input,
            ODataRequest req, HttpContext httpContext, out int count)
        {
            var x = ProcessODataFilters(input.Keys, req, out var totalCount);

            var output = new List<Dictionary<string, object>>();
            var projector = Projector.Create(req, true);
            foreach (var content in x)
            {
                var fields = CreateFieldDictionary(content, projector, httpContext);
                var item = new Dictionary<string, object>
                {
                    {"key", fields},
                    {"value", input[content]}
                };
                output.Add(item);
            }
            count = totalCount ?? output.Count;
            if (req.CountOnly)
                return null;
            return output;
        }
        private List<Dictionary<string, object>> ProcessOperationCollectionResponse(IEnumerable<Content> inputContents,
            ODataRequest req, HttpContext httpContext, out int count)
        {
            var x = ProcessODataFilters(inputContents, req, out var totalCount);

            var outContents = new List<Dictionary<string, object>>();
            var projector = Projector.Create(req, true);
            foreach (var content in x)
            {
                var fields = CreateFieldDictionary(content, projector, httpContext);
                outContents.Add(fields);
            }

            count = totalCount ?? outContents.Count;
            if (req.CountOnly)
                return null;
            return outContents;
        }

        private IEnumerable<Content> ProcessODataFilters(IEnumerable<Content> inputContents, ODataRequest req, out int? totalCount)
        {
            var x = inputContents;
            if (req.HasFilter)
            {
                if (x is IQueryable<Content> y)
                {
                    x = y.Where((Expression<Func<Content, bool>>)req.Filter);
                }
                else
                {
                    var filter = SnExpression.GetCaseInsensitiveFilter(req.Filter);
                    var lambdaExpr = (LambdaExpression)filter;
                    x = x.Where((Func<Content, bool>)lambdaExpr.Compile());
                }
            }
            if (req.HasSort)
                x = AddSortToCollectionExpression(x, req.Sort);

            if (req.InlineCount == InlineCount.AllPages)
            {
                x = x.ToList();
                totalCount = ((IList)x).Count;
            }
            else
            {
                totalCount = null;
            }

            if (req.HasSkip)
                x = x.Skip(req.Skip);
            if (req.HasTop)
                x = x.Take(req.Top);

            return x;
        }
        private IEnumerable<Content> AddSortToCollectionExpression(IEnumerable<Content> contents, IEnumerable<SortInfo> sort)
        {
            IOrderedEnumerable<Content> sortedContents = null;
            var contentArray = contents as Content[] ?? contents.ToArray();
            foreach (var sortInfo in sort)
            {
                if (sortedContents == null)
                {
                    sortedContents = sortInfo.Reverse 
                        ? contentArray.OrderByDescending(c => c[sortInfo.FieldName])
                        : contentArray.OrderBy(c => c[sortInfo.FieldName]);
                }
                else
                {
                    sortedContents = sortInfo.Reverse
                        ? sortedContents.ThenByDescending(c => c[sortInfo.FieldName])
                        : sortedContents.ThenBy(c => c[sortInfo.FieldName]);
                }
            }
            return sortedContents ?? contents;
        }

        // --------------------------------------------------------------------------------------------------------------- utilities

        private object[] GetOperationParameters(ActionBase action, HttpRequest request)
        {
            if ((action.ActionParameters?.Length ?? 0) == 0)
                return ActionParameter.EmptyValues;

            Debug.Assert(action.ActionParameters != null, "action.ActionParameters != null");
            var values = new object[action.ActionParameters.Length];

            var parameters = action.ActionParameters;
            if (parameters.Length == 1 && parameters[0].Name == null)
            {
                throw new ArgumentException("Cannot parse unnamed parameter from URL. This operation expects POST verb.");
            }
            else
            {
                var i = 0;
                foreach (var parameter in parameters)
                {
                    var name = parameter.Name;
                    var type = parameter.Type;
                    var val = request.Query[name];
                    if (val == StringValues.Empty)
                    {
                        if (parameter.Required)
                            throw new ArgumentNullException(parameter.Name);
                    }
                    else
                    {
                        var valStr = (string)val;

                        if (type == typeof(string))
                        {
                            values[i] = valStr;
                        }
                        else if (type == typeof(bool))
                        {
                            // we handle "True", "true" and "1" as boolean true values
                            values[i] = JsonConvert.DeserializeObject(valStr.ToLower(), type);
                        }
                        else if (type.IsEnum)
                        {
                            values[i] = Enum.Parse(type, valStr, true);
                        }
                        else if (type == typeof(string[]))
                        {
                            var parsed = false;
                            try
                            {
                                values[i] = JsonConvert.DeserializeObject(valStr, type);
                                parsed = true;
                            }
                            catch // recompute
                            {
                                // ignored
                            }
                            if (!parsed)
                            {
                                if (valStr.StartsWith("'"))
                                    values[i] = GetStringArrayFromString(name, valStr, '\'');
                                else if (valStr.StartsWith("\""))
                                    values[i] = GetStringArrayFromString(name, valStr, '"');
                                else
                                    values[i] = valStr.Split(',').Select(s => s?.Trim()).ToArray();
                            }
                        }
                        else
                        {
                            values[i] = JsonConvert.DeserializeObject(valStr, type);
                        }
                    }
                    i++;
                }
            }
            return values;
        }
        private string[] GetStringArrayFromString(string paramName, string src, char stringEnvelope)
        {
            var result = new List<string>();
            int startPos = -1;
            bool started = false;
            for (int i = 0; i < src.Length; i++)
            {
                var c = src[i];
                if (c == stringEnvelope)
                {
                    if (!started)
                    {
                        started = true;
                        startPos = i + 1;
                    }
                    else
                    {
                        started = false;
                        result.Add(src.Substring(startPos, i - startPos));
                    }
                }
                else if (!started)
                {
                    if (c != ' ' && c != ',')
                        throw new ODataException("Parameter error: cannot parse a string array. Name: " + paramName, ODataExceptionCode.NotSpecified);
                }
            }
            return result.ToArray();
        }
        private object[] GetOperationParameters(ActionBase action, Stream inputStream)
        {
            if (action.ActionParameters.Length == 0)
                return ActionParameter.EmptyValues;

            var values = new object[action.ActionParameters.Length];

            var parameters = action.ActionParameters;
            if (parameters.Length == 1 && parameters[0].Name == null)
            {
                var parameter = parameters[0];
                if (parameter.Type == null)
                {
                    using (var reader = new StreamReader(inputStream))
                        values[0] = reader.ReadToEnd();
                    if (parameter.Required && values[0] == null)
                        // ReSharper disable once NotResolvedInText
                        throw new ArgumentNullException("[unnamed]", "Request parameter is required.");
                }
                else
                {
                    values[0] = ODataHandler.Read(inputStream, parameter.Type);
                    if (parameter.Required && values[0] == null)
                        // ReSharper disable once NotResolvedInText
                        throw new ArgumentNullException("[unnamed]", "Request parameter is required. Type: " + parameter.Type.FullName);
                }
            }
            else
            {
                var model = ODataHandler.Read(inputStream);
                var i = 0;
                foreach (var parameter in parameters)
                {
                    var name = parameter.Name;
                    var type = parameter.Type;
                    var val = model?[name];
                    if (val == null)
                    {
                        if (parameter.Required)
                        {
                            throw new ArgumentNullException(parameter.Name);
                        }
                        values[i] = Type.Missing;
                    }
                    else
                    {
                        var valStr = val.ToString();

                        if (type == typeof(string))
                        {
                            values[i] = valStr;
                        }
                        else if (type == typeof(Boolean))
                        {
                            // we handle "True", "true" and "1" as boolean true values
                            values[i] = JsonConvert.DeserializeObject(valStr.ToLower(), type);
                        }
                        else if (type.IsEnum)
                        {
                            values[i] = Enum.Parse(type, valStr, true);
                        }
                        else
                        {
                            values[i] = JsonConvert.DeserializeObject(valStr, type);
                        }
                    }
                    i++;
                }
            }
            return values;
        }
        private Dictionary<string, object> CreateFieldDictionary(Content content, Projector projector, HttpContext httpContext)
        {
            return projector.Project(content, httpContext);
        }
        private Dictionary<string, object> CreateFieldDictionary(Content content, bool isCollectionItem, HttpContext httpContext)
        {
            var projector = Projector.Create(this.ODataRequest, isCollectionItem, content);
            return projector.Project(content, httpContext);
        }

        //TODO: Bad name: GetJsonObject is a method for odata serializing
        internal static object GetJsonObject(Field field, string selfUrl)
        {
            object data;
            if (field is ReferenceField)
            {
                return ODataReference.Create(String.Concat(selfUrl, "/", field.Name));
            }
            else if (field is BinaryField)
            {
                var binaryField = (BinaryField)field;
                var binaryData = (BinaryData)binaryField.GetData();

                return ODataBinary.Create(BinaryField.GetBinaryUrl(field.Content.Id, field.Name, binaryData.Timestamp), null, binaryData.ContentType, null);
            }
            else if (ODataHandler.DeferredFieldNames.Contains(field.Name))
            {
                return ODataReference.Create(String.Concat(selfUrl, "/", field.Name));
            }
            try
            {
                data = field.GetData();
            }
            catch (SenseNetSecurityException)
            {
                // The user does not have access to this field (e.g. cannot load
                // a referenced content). In this case we serve a null value.
                data = null;

                SnTrace.Repository.Write("PERMISSION warning: user {0} does not have access to field '{1}' of {2}.", User.LoggedInUser.Username, field.Name, field.Content.Path);
            }

            if (data is NodeType nodeType)
                return nodeType.Name;
            return data;
        }

        /// <summary>
        /// Writes an object to the webresponse.
        /// </summary>
        /// <param name="response">The object that will be written.</param>
        /// <param name="httpContext">The current <see cref="HttpContext"/> instance containing the current web-response.</param>
        protected void Write(object response, HttpContext httpContext)
        {
            //var resp = httpContext.Response;

            //if (response == null)
            //{
            //    resp.StatusCode = 204;
            //    return;
            //}

            //if (response is string)
            //{
            //    WriteRaw(response, httpContext);
            //    return;
            //}

            //var settings = new JsonSerializerSettings
            //{
            //    DateFormatHandling = DateFormatHandling.IsoDateFormat,
            //    Formatting = Formatting.Indented,
            //    Converters = ODataHandler.JsonConverters
            //};
            //var serializer = JsonSerializer.Create(settings);
            //serializer.Serialize(httpContext.Response.Output, response);
            //resp.ContentType = "application/json;odata=verbose;charset=utf-8";
            throw new NotImplementedException(); //UNDONE:ODATA: Not implemented.
        }
        /// <summary>
        /// Writes an object to the webresponse. Tipically used for writing a simple object (e.g. <see cref="Field"/> values).
        /// </summary>
        /// <param name="response">The object that will be written.</param>
        /// <param name="httpContext">The current <see cref="HttpContext"/> instance containing the current web-response.</param>
        protected void WriteRaw(object response, HttpContext httpContext)
        {
            //var resp = httpContext.Response;
            //resp.Write(response);
            throw new NotImplementedException(); //UNDONE:ODATA: Not implemented.
        }

    }
}
