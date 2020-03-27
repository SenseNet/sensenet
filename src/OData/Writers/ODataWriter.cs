using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Linq;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Search.Querying;
using SenseNet.Tools;
using SenseNet.OData.Metadata.Model;
using SenseNet.Services.Core.Operations;
using Task = System.Threading.Tasks.Task;
using Utility = SenseNet.Tools.Utility;
// ReSharper disable ArrangeThisQualifier

namespace SenseNet.OData.Writers
{
    /// <summary>
    /// Defines a base class for serializing the OData response object to various formats.
    /// </summary>
    public abstract class ODataWriter
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

        private static readonly object ODataWriterTypeLock = new object();
        private static Dictionary<string, Type> _odataWriterTypes;
        internal static Dictionary<string, Type> ODataWriterTypes
        {
            get
            {
                if (_odataWriterTypes == null)
                {
                    lock(ODataWriterTypeLock)
                    {
                        if (_odataWriterTypes == null)
                        {
                            _odataWriterTypes = LoadODataWriterTypes();

                            SnLog.WriteInformation("ODataWriter types loaded: " +
                                string.Join(", ", _odataWriterTypes.Values.Select(t => t.FullName)));
                        }
                    }
                }

                return _odataWriterTypes;
            }
        }

        private static Dictionary<string, Type> LoadODataWriterTypes()
        {
            var writerTypes = new Dictionary<string, Type>();
            var types = TypeResolver.GetTypesByBaseType(typeof(ODataWriter));

            foreach (var type in types)
            {
                var protoType = (ODataWriter)Activator.CreateInstance(type);
                writerTypes[protoType.FormatName] = type;
            }

            return writerTypes;
        }

        internal static ODataWriter Create(HttpContext httpContext, ODataRequest odataReq)
        {
            var formatName = httpContext.Request.Query["$format"].ToString();
            if (string.IsNullOrEmpty(formatName))
                formatName = odataReq == null ? "json" : odataReq.IsMetadataRequest ? "xml" : "json";
            else
                formatName = formatName.ToLower();

            return Create(formatName);
        }
        internal static ODataWriter Create(string formatName)
        {
            if (!ODataWriterTypes.TryGetValue(formatName, out var writerType))
                return null;

            var writer = (ODataWriter)Activator.CreateInstance(writerType);
            return writer;
        }

        internal void Initialize(ODataRequest odataRequest)
        {
            this.ODataRequest = odataRequest;
        }

        // --------------------------------------------------------------------------------------------------------------- metadata

        internal async Task WriteServiceDocumentAsync(HttpContext httpContext, ODataRequest req)
        {
            var mimeType = this.MimeType;
            if (mimeType != null)
                httpContext.Response.ContentType = mimeType;
            await WriteServiceDocumentAsync(httpContext, GetTopLevelNames(req)).ConfigureAwait(false);
        }
        private string[] GetTopLevelNames(ODataRequest req)
        {
            var rootContent = Content.Load(req.RepositoryPath);
            var topLevelNames = rootContent?.Children.Select(n => n.Name).ToArray() ?? new[] { Repository.RootName };

            return topLevelNames;
        }
        /// <summary>
        /// Writes the OData service document with the given root names to the webresponse.
        /// </summary>
        /// <param name="httpContext">The current <see cref="HttpContext"/> instance containing the current web-response.</param>
        /// <param name="names">Root names.</param>
        protected abstract Task WriteServiceDocumentAsync(HttpContext httpContext, IEnumerable<string> names);

        internal async Task WriteMetadataAsync(HttpContext httpContext, ODataRequest req)
        {
            var content = ODataMiddleware.LoadContentByVersionRequest(req.RepositoryPath, httpContext);

            //var isRoot = content?.ContentType.IsInstaceOfOrDerivedFrom("Site") ?? true;
            var isRoot = content == null;
            var metadata = isRoot
                ? MetaGenerator.GetMetadata()
                : MetaGenerator.GetMetadata(content, req.IsCollection);

            var mimeType = this.MimeType;
            if (mimeType != null)
                httpContext.Response.ContentType = mimeType;

            await WriteMetadataAsync(httpContext, metadata);
        }
        /// <summary>
        /// Writes the OData service metadata to the current web-response.
        /// </summary>
        /// <param name="httpContext">The current <see cref="HttpContext"/> instance containing the current web-response.</param>
        /// <param name="edmx">Metadata that will be written.</param>
        protected abstract Task WriteMetadataAsync(HttpContext httpContext, Edmx edmx);

        /* ---------------------------------------------------------------------------------------------------- content requests */

        //internal void WriteSingleContent(String path, HttpContext httpContext)
        //{
        //    WriteSingleContent(ODataMiddleware.LoadContentByVersionRequest(path, httpContext), httpContext);
        //}
        internal async Task WriteSingleContentAsync(Content content, HttpContext httpContext)
        {
            var fields = CreateFieldDictionary(content, false, httpContext);
            await WriteSingleContentAsync(httpContext, fields).ConfigureAwait(false);
        }
        /// <summary>
        /// Writes the given fields of a Content to the webresponse.
        /// </summary>
        /// <param name="httpContext">The current <see cref="HttpContext"/> instance containing the current web-response.</param>
        /// <param name="fields">A Dictionary&lt;string, object&gt; that will be written.</param>
        protected abstract Task WriteSingleContentAsync(HttpContext httpContext, ODataEntity fields);

        internal async Task WriteChildrenCollectionAsync(String path, HttpContext httpContext, ODataRequest req)
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
                await WriteCountAsync(httpContext, count).ConfigureAwait(false);
            else
                await WriteMultipleContentAsync(httpContext, contents, count).ConfigureAwait(false);
        }
        private async Task WriteMultiRefContentsAsync(object references, HttpContext httpContext, ODataRequest req)
        {
            if (references == null)
                return;

            var node = references as Node;
            var projector = Projector.Create(req, true);
            if (node != null)
            {
                var contents = new List<ODataEntity>
                {
                    CreateFieldDictionary(Content.Create(node), projector,httpContext)
                };
                //TODO: ODATA: multiref item: get available types from reference property
                await WriteMultipleContentAsync(httpContext, contents, 1)
                    .ConfigureAwait(false);
            }
            else
            {
                if (references is IEnumerable enumerable)
                {
                    var skipped = 0;
                    var allcount = 0;
                    var count = 0;
                    var realcount = 0;
                    var contents = new List<ODataEntity>();
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
                    await WriteMultipleContentAsync(httpContext, contents, req.InlineCount == InlineCount.AllPages ? allcount : realcount)
                        .ConfigureAwait(false);
                }
            }
        }
        private async Task WriteSingleRefContentAsync(object references, HttpContext httpContext)
        {
            if (references != null)
            {
                if (references is Node node)
                {
                    await WriteSingleContentAsync(httpContext, CreateFieldDictionary(Content.Create(node), false, httpContext))
                        .ConfigureAwait(false);
                }
                else
                {
                    if (references is IEnumerable enumerable)
                    {
                        foreach (Node item in enumerable)
                        {
                            await WriteSingleContentAsync(httpContext, CreateFieldDictionary(Content.Create(item), false, httpContext))
                                .ConfigureAwait(false);
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
        protected abstract Task WriteMultipleContentAsync(HttpContext httpContext, IEnumerable<ODataEntity> contents, int count);
        /// <summary>
        /// Writes only the count of the requested resource to the webresponse.
        /// Activated if the URI of the requested resource contains the "$count" segment.
        /// </summary>
        /// <param name="httpContext">The current <see cref="HttpContext"/> instance containing the current web-response.</param>
        /// <param name="count"></param>
        protected abstract Task WriteCountAsync(HttpContext httpContext, int count);

        internal async Task WriteContentPropertyAsync(string path, string propertyName, bool rawValue, HttpContext httpContext, ODataRequest req)
        {
            var content = ODataMiddleware.LoadContentByVersionRequest(path, httpContext);
            if (content == null)
            {
                ODataMiddleware.ContentNotFound(httpContext);
                return;
            }

            if (propertyName == ODataMiddleware.ActionsPropertyName)
            {
                await WriteActionsPropertyAsync(httpContext, 
                    ODataTools.GetActionItems(content, req, httpContext).ToArray(), rawValue)
                    .ConfigureAwait(false);
                return;
            }
            if (propertyName == ODataMiddleware.ChildrenPropertyName)
            {
                await WriteChildrenCollectionAsync(path, httpContext, req)
                    .ConfigureAwait(false);
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
                        await WriteMultiRefContentsAsync(refField.GetData(), httpContext, req)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await WriteSingleRefContentAsync(refField.GetData(), httpContext)
                            .ConfigureAwait(false);
                    }
                }
                else if (field is AllowedChildTypesField actField)
                {
                    await WriteMultiRefContentsAsync(actField.GetData(), httpContext, req)
                        .ConfigureAwait(false);
                }
                else if (!rawValue)
                {
                    await WriteSingleContentAsync(httpContext, new ODataEntity {{propertyName, field.GetData()}})
                        .ConfigureAwait(false);
                }
                else
                {
                    await WriteRawAsync(field.GetData(), httpContext)
                        .ConfigureAwait(false);
                }
            }
            else
            {
                await WriteGetOperationResultAsync(httpContext, req)
                    .ConfigureAwait(false);
            }
        }
        /// <summary>
        /// Writes the available actions of the current <see cref="Content"/> to the webresponse.
        /// </summary>
        /// <param name="httpContext">The current <see cref="HttpContext"/> instance containing the current web-response.</param>
        /// <param name="actions">Array of <see cref="ODataActionItem"/> that will be written.</param>
        /// <param name="raw"></param>
        protected abstract Task WriteActionsPropertyAsync(HttpContext httpContext, ODataActionItem[] actions, bool raw);


        internal async Task WriteErrorResponseAsync(HttpContext httpContext, ODataException oe)
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
                        null
#endif
            };

            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = oe.HttpStatusCode;
            await WriteErrorAsync(httpContext, error).ConfigureAwait(false);
        }
        /// <summary>
        /// Writes the given <see cref="Error"/> instance to the webresponse.
        /// </summary>
        /// <param name="context">The current <see cref="HttpContext"/> instance.</param>
        /// <param name="error">The <see cref="Error"/> instance that will be written.</param>
        protected abstract Task WriteErrorAsync(HttpContext context, Error error);

        // --------------------------------------------------------------------------------------------------------------- operations

        /// <summary>
        /// Handles GET operations. Parameters come from the URL or the request stream.
        /// </summary>
        internal async Task WriteGetOperationResultAsync(HttpContext httpContext, ODataRequest odataReq)
        {
            var content = ODataMiddleware.LoadContentByVersionRequest(odataReq.RepositoryPath, httpContext);
            if (content == null)
                throw new ContentNotFoundException(string.Format(SNSR.GetString("$Action,ErrorContentNotFound"), odataReq.RepositoryPath));

            var action = ODataMiddleware.ActionResolver.GetAction(content, odataReq.Scenario, odataReq.PropertyName, null, null, httpContext);
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

            var response = action is ODataOperationMethodExecutor odataAction
                ? (odataAction.IsAsync ? await odataAction.ExecuteAsync(content) : action.Execute(content))
                : action.Execute(content, GetOperationParameters(action, httpContext.Request));

            if (response is Content responseAsContent)
            {
                await WriteSingleContentAsync(responseAsContent, httpContext)
                    .ConfigureAwait(false);
                return;
            }

            response = ProcessOperationResponse(response, odataReq, httpContext, out var count);
            await WriteOperationResultAsync(response, httpContext, odataReq, count)
                .ConfigureAwait(false);
        }
        /// <summary>
        /// Handles POST operations. Parameters come from request stream.
        /// </summary>
        internal async Task WritePostOperationResultAsync(HttpContext httpContext, ODataRequest odataReq)
        {
            var content = ODataMiddleware.LoadContentByVersionRequest(odataReq.RepositoryPath, httpContext);

            if (content == null)
                throw new ContentNotFoundException(string.Format(SNSR.GetString("$Action,ErrorContentNotFound"), odataReq.RepositoryPath));

            var action = ODataMiddleware.ActionResolver.GetAction(content, odataReq.Scenario, odataReq.PropertyName, null, null, httpContext);
            if (action == null)
            {
                // check if this is a versioning action (e.g. a checkout)
                SavingAction.AssertVersioningAction(content, odataReq.PropertyName, true);

                throw new InvalidContentActionException(InvalidContentActionReason.UnknownAction, content.Path, null, odataReq.PropertyName);
            }

            if (action.Forbidden || (action.GetApplication() != null && !action.GetApplication().Security.HasPermission(PermissionType.RunApplication)))
                throw new InvalidContentActionException("Forbidden action: " + odataReq.PropertyName);

            var response = action is ODataOperationMethodExecutor odataAction
            ? (odataAction.IsAsync ? await odataAction.ExecuteAsync(content) : action.Execute(content))
            : action.Execute(content, await GetOperationParametersAsync(action, httpContext, odataReq));

            if (response is Content responseAsContent)
            {
                await WriteSingleContentAsync(responseAsContent, httpContext)
                    .ConfigureAwait(false);
                return;
            }

            response = ProcessOperationResponse(response, odataReq, httpContext, out var count);
            await WriteOperationResultAsync(response, httpContext, odataReq, count)
                .ConfigureAwait(false);
        }
        private async Task WriteOperationResultAsync(object result, HttpContext httpContext, ODataRequest odataReq, int allCount)
        {
            if (result is Content content)
            {
                await WriteSingleContentAsync(content, httpContext)
                    .ConfigureAwait(false);
                return;
            }

            if (result is IEnumerable<Content> enumerable)
            {
                await WriteMultiRefContentsAsync(enumerable, httpContext, odataReq)
                    .ConfigureAwait(false);
                return;
            }

            await WriteOperationCustomResultAsync(httpContext, result,
                    odataReq.InlineCount == InlineCount.AllPages ? allCount : (int?)null)
                .ConfigureAwait(false);
        }
        /// <summary>
        /// Writes a custom operations's result object to the webresponse.
        /// </summary>
        /// <param name="httpContext">The current <see cref="HttpContext"/> instance containing the current web-response.</param>
        /// <param name="result">The object that will be written.</param>
        /// <param name="allCount">A nullable int that contains the count of items in the result object 
        /// if the request specifies the total count of the collection ("$inlinecount=allpages"), otherwise the value is null.</param>
        protected abstract Task WriteOperationCustomResultAsync(HttpContext httpContext, object result, int? allCount);

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
        private IEnumerable<ODataEntity> ProcessOperationQueryResponse(ChildrenDefinition qdef, ODataRequest req, HttpContext httpContext, out int count)
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

            var contents = new List<ODataEntity>();
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
                    properties: new ODataEntity
                    {
                        {"MissingIds", string.Join(", ", missingIds.OrderBy(id => id))}
                    });
            }

            return contents;
        }
        private List<ODataEntity> ProcessOperationDictionaryResponse(IDictionary<Content, object> input,
            ODataRequest req, HttpContext httpContext, out int count)
        {
            var x = ProcessODataFilters(input.Keys, req, out var totalCount);

            var output = new List<ODataEntity>();
            var projector = Projector.Create(req, true);
            foreach (var content in x)
            {
                var fields = CreateFieldDictionary(content, projector, httpContext);
                var item = new ODataEntity
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
        private List<ODataEntity> ProcessOperationCollectionResponse(IEnumerable<Content> inputContents,
            ODataRequest req, HttpContext httpContext, out int count)
        {
            var x = ProcessODataFilters(inputContents, req, out var totalCount);

            var outContents = new List<ODataEntity>();
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
                                    values[i] = valStr.Split(',').Select(s => s.Trim()).ToArray();
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
        private async Task<object[]> GetOperationParametersAsync(ActionBase action,
            HttpContext httpContext, ODataRequest odataRequest)
        {
            if (action.ActionParameters.Length == 0)
                return ActionParameter.EmptyValues;

            var inputStream = httpContext?.Request?.Body;
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
                    values[0] = ODataMiddleware.Read(inputStream, parameter.Type);
                    if (parameter.Required && values[0] == null)
                        // ReSharper disable once NotResolvedInText
                        throw new ArgumentNullException("[unnamed]", "Request parameter is required. Type: " + parameter.Type.FullName);
                }
            }
            else
            {
                var model = await ODataMiddleware.ReadToJsonAsync(httpContext);
                var i = 0;
                foreach (var parameter in parameters)
                {
                    var name = parameter.Name;
                    var type = parameter.Type;
                    if (type == typeof(HttpContext))
                    {
                        values[i] = httpContext;
                    }
                    else if (type == typeof(ODataRequest))
                    {
                        values[i] = odataRequest;
                    }
                    else
                    {
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
                    }
                    i++;
                }
            }
            return values;
        }
        private ODataEntity CreateFieldDictionary(Content content, Projector projector, HttpContext httpContext)
        {
            return projector.Project(content, httpContext);
        }
        private ODataEntity CreateFieldDictionary(Content content, bool isCollectionItem, HttpContext httpContext)
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
            else if (ODataMiddleware.DeferredFieldNames.Contains(field.Name))
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
        /// Writes an object to the webresponse. Tipically used for writing a simple object (e.g. <see cref="Field"/> values).
        /// </summary>
        /// <param name="response">The object that will be written.</param>
        /// <param name="httpContext">The current <see cref="HttpContext"/> instance containing the current web-response.</param>
        protected virtual async Task WriteRawAsync(object response, HttpContext httpContext)
        {
            await httpContext.Response.WriteAsync(response.ToString()).ConfigureAwait(false);
        }

    }
}
