using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Xml.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using SenseNet.ContentRepository.Linq;
using SenseNet.OData.Formatters;
using SenseNet.OData.Metadata;
using SenseNet.Search;
using SenseNet.Search.Querying;
using STT = System.Threading.Tasks;
// ReSharper disable ArrangeThisQualifier

namespace SenseNet.OData
{
    /// <summary>
    /// AN ASP.NET Core middleware to process the OData requests.
    /// </summary>
    public class ODataMiddleware
    {
        // Do not remove setter.
        internal static IActionResolver ActionResolver { get; set; }

        internal static readonly string[] HeadFieldNames = new[] { "Id", "Name", "DisplayName", "Icon", "CreationDate", "ModificationDate", "CreatedBy", "ModifiedBy" };
        internal static readonly List<string> DisabledFieldNames = new List<string>(new[] { "TypeIs", "InTree", "InFolder", "NodeType", "Rate"/*, "VersioningMode", "ApprovingMode", "RateAvg", "RateCount"*/ });
        internal static readonly List<string> DeferredFieldNames = new List<string>(new[] { "AllowedChildTypes", "EffectiveAllowedChildTypes" });
        internal static readonly List<string> AllowedMethodNamesWithoutContent = new List<string>(new[] { "PATCH", "PUT", "POST", "DELETE" });

        internal static List<JsonConverter> JsonConverters { get; }
        internal static List<FieldConverter> FieldConverters { get; }

        static ODataMiddleware()
        {
            JsonConverters = new List<JsonConverter> {new Newtonsoft.Json.Converters.VersionConverter()};
            FieldConverters = new List<FieldConverter>();
            var fieldConverterTypes = TypeResolver.GetTypesByBaseType(typeof(FieldConverter));
            foreach (var fieldConverterType in fieldConverterTypes)
            {
                var fieldConverter = (FieldConverter)Activator.CreateInstance(fieldConverterType);
                JsonConverters.Add(fieldConverter);
                FieldConverters.Add(fieldConverter);
            }

            ActionResolver = new DefaultActionResolver();
        }

        internal static readonly DateTime BaseDate = new DateTime(1970, 1, 1);
        internal const string ModelRequestKeyName = "models";
        internal const string ActionsPropertyName = "Actions";
        internal const string ChildrenPropertyName = "Children";
        internal const string BinaryPropertyName = "Binary";
        internal const int ExpansionLimit = int.MaxValue - 1;

        internal ODataRequest ODataRequest { get; private set; }

        private readonly RequestDelegate _next;
        // Must have constructor with this signature, otherwise exception at run time
        public ODataMiddleware(RequestDelegate next)
        {
            _next = next;
        }


        public async STT.Task Invoke(HttpContext httpContext)
        {
            // CREATE ODATA-RESPONSE STRATEGY

            var odataRequest = ODataRequest.Parse(httpContext);
            httpContext.SetODataRequest(odataRequest);

            // ENABLE CUSTOMIZATION FOR NEXT MIDDLEWARE

            await _next(httpContext);

            // WRITE RESPONSE
            //UNDONE:ODATA: Remove SystemAccount when the authentication is finished
            using (new SystemAccount())
                await ProcessRequestAsync(httpContext, odataRequest);
        }

        internal STT.Task ProcessRequestAsync(HttpContext httpContext, ODataRequest odataRequest)
        {
            ProcessRequest(httpContext, odataRequest);
            //httpContext.Response.Body.Flush();
            return STT.Task.CompletedTask;
        }

        internal void ProcessRequest(HttpContext httpContext, ODataRequest odataRequest)
        {
            var request = httpContext.Request;
            var httpMethod = request.Method;
            var inputStream = request.Body;
            ODataFormatter formatter = null;
            try
            {
                Content content;
                if (odataRequest == null)
                    throw new ODataException("The Request is not an OData request.", ODataExceptionCode.RequestError);


                this.ODataRequest = odataRequest;
                Exception requestError = this.ODataRequest.RequestError;

                formatter = ODataFormatter.Create(httpContext, odataRequest);
                if (formatter == null)
                    throw new ODataException(ODataExceptionCode.InvalidFormatParameter);
                formatter.Initialize(odataRequest);

                httpContext.SetODataFormatter(formatter);

                if (requestError != null)
                {
                    var innerOdataError = requestError as ODataException;
                    var message = "An error occured during request parsing. " + requestError.Message +
                                  " See inner exception for details.";
                    var code = innerOdataError?.ODataExceptionCode ?? ODataExceptionCode.RequestError;
                    throw new ODataException(message, code, requestError);
                }

                odataRequest.Format = formatter.FormatName;

                var requestedContent = LoadContentByVersionRequest(odataRequest.RepositoryPath, httpContext);

                var exists = requestedContent != null;
                if (!exists && !odataRequest.IsServiceDocumentRequest && !odataRequest.IsMetadataRequest && !AllowedMethodNamesWithoutContent.Contains(httpMethod))
                {
                    ContentNotFound(httpContext);
                    return;
                }

                JObject model;
                switch (httpMethod)
                {
                    case "GET":
                        if (odataRequest.IsServiceDocumentRequest)
                        {
                            /*await*/ formatter.WriteServiceDocumentAsync(httpContext, odataRequest)
                                .ConfigureAwait(false).GetAwaiter().GetResult();
                        }
                        else if (odataRequest.IsMetadataRequest)
                        {
                            //return ODataResponse.CreateMetadataResponse(exists ? odataRequest.RepositoryPath : "/");
                            /*await*/ formatter.WriteMetadataAsync(httpContext, odataRequest)
                                .ConfigureAwait(false).GetAwaiter().GetResult();
                        }
                        else
                        {
                            if (!Node.Exists(odataRequest.RepositoryPath))
                                ContentNotFound(httpContext);
                            else if (odataRequest.IsCollection)
                                formatter.WriteChildrenCollection(odataRequest.RepositoryPath, httpContext, odataRequest);
                            else if (odataRequest.IsMemberRequest)
                                formatter.WriteContentProperty(odataRequest.RepositoryPath, odataRequest.PropertyName,
                                    odataRequest.IsRawValueRequest, httpContext, odataRequest);
                            else
                                formatter.WriteSingleContent(requestedContent, httpContext);
                        }
                        break;
                    case "PUT": // update
                        if (odataRequest.IsMemberRequest)
                        {
                            throw new ODataException("Cannot access a member with HTTP PUT.",
                                ODataExceptionCode.IllegalInvoke);
                        }
                        else
                        {
                            model = Read(inputStream);
                            content = LoadContentOrVirtualChild(odataRequest);
                            if (content == null)
                            {
                                ContentNotFound(httpContext);
                                return;
                            }

                            ResetContent(content);
                            UpdateContent(content, model, odataRequest);
                            formatter.WriteSingleContent(content, httpContext);
                        }
                        break;
                    case "MERGE":
                    case "PATCH": // update
                        if (odataRequest.IsMemberRequest)
                        {
                            throw new ODataException(
                                String.Concat("Cannot access a member with HTTP ", httpMethod, "."),
                                ODataExceptionCode.IllegalInvoke);
                        }
                        else
                        {
                            model = Read(inputStream);
                            content = LoadContentOrVirtualChild(odataRequest);
                            if (content == null)
                            {
                                ContentNotFound(httpContext);
                                return;
                            }

                            UpdateContent(content, model, odataRequest);
                            formatter.WriteSingleContent(content, httpContext);
                        }
                        break;
                    case "POST": // invoke an action, create content
                        if (odataRequest.IsMemberRequest)
                        {
                            formatter.WriteOperationResult(inputStream, httpContext, odataRequest);
                        }
                        else
                        {
                            // parent must exist
                            //UNDONE:ODATA: unnecessary check (?)
                            if (!Node.Exists(odataRequest.RepositoryPath))
                            {
                                ContentNotFound(httpContext);
                                return;
                            }
                            model = Read(inputStream);
                            var newContent = CreateNewContent(model, odataRequest);
                            formatter.WriteSingleContent(newContent, httpContext);
                        }
                        break;
                    case "DELETE":
                        if (odataRequest.IsMemberRequest)
                        {
                            throw new ODataException(
                                String.Concat("Cannot access a member with HTTP ", httpMethod, "."),
                                ODataExceptionCode.IllegalInvoke);
                        }
                        else
                        {
                            content = LoadContentOrVirtualChild(odataRequest);
                            content?.Delete();
                        }
                        break;
                }
            }
            catch (ContentNotFoundException e)
            {
                var oe = new ODataException(ODataExceptionCode.ResourceNotFound, e);
                formatter?.WriteErrorResponse(httpContext, oe);
            }
            catch (ODataException e)
            {
                if (e.HttpStatusCode == 500)
                    SnLog.WriteException(e);
                formatter?.WriteErrorResponse(httpContext, e);
            }
            catch (SenseNetSecurityException e)
            {
                // In case of a visitor we should not expose the information that this content actually exists. We return
                // a simple 404 instead to provide exactly the same response as the regular 404, where the content 
                // really does not exist. But do this only if the visitor really does not have permission for the
                // requested content (because security exception could be thrown by an action or something else too).
                if (odataRequest != null && User.Current.Id == Identifiers.VisitorUserId)
                {
                    //UNDONE:ODATA: Use loaded content
                    var head = NodeHead.Get(odataRequest.RepositoryPath);
                    if (head != null && !SecurityHandler.HasPermission(head, PermissionType.Open))
                    {
                        ContentNotFound(httpContext);
                        return;
                    }
                }

                var oe = new ODataException(ODataExceptionCode.NotSpecified, e);

                SnLog.WriteException(oe);

                formatter?.WriteErrorResponse(httpContext, oe);
            }
            catch (InvalidContentActionException ex)
            {
                var oe = new ODataException(ODataExceptionCode.NotSpecified, ex);
                if (ex.Reason != InvalidContentActionReason.NotSpecified)
                    oe.ErrorCode = Enum.GetName(typeof(InvalidContentActionReason), ex.Reason);

                // it is unnecessary to log this exception as this is not a real error
                formatter?.WriteErrorResponse(httpContext, oe);
            }
            catch (ContentRepository.Storage.Data.NodeAlreadyExistsException nae)
            {
                var oe = new ODataException(ODataExceptionCode.ContentAlreadyExists, nae);

                formatter?.WriteErrorResponse(httpContext, oe);
            }
            //UNDONE:ODATA: ?? Response.IsRequestBeingRedirected does not exist in ASPNET Core.
            //UNDONE:ODATA: ?? ThreadAbortException does not occur in this technology.
            //catch (System.Threading.ThreadAbortException tae)
            //{
            //    if (!httpContext.Response.IsRequestBeingRedirected)
            //    {
            //        var oe = new ODataException(ODataExceptionCode.RequestError, tae);
            //        //formatter?.WriteErrorResponse(httpContext, oe);
            //        return ODataResponse.CreateErrorResponse(oe);
            //    }
            //    // specific redirect response so do nothing
            //}
            catch (Exception ex)
            {
                var oe = new ODataException(ODataExceptionCode.NotSpecified, ex);

                SnLog.WriteException(oe);

                formatter?.WriteErrorResponse(httpContext, oe);
            }
            finally
            {
                //httpContext.Response.End();

                //UNDONE:ODATA: async
                //await _next(httpContext);
                //_next(httpContext).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        /* ==== */

        //private ODataEntity GetSingleContent(HttpContext httpContext, ODataRequest odataRequest, Content content)
        //{
        //    return CreateFieldDictionary(httpContext, odataRequest, content, false);
        //}

        /* ==== */

        //private ODataResponse GetChildrenCollectionResponse(Content content, HttpContext httpContext, ODataRequest req)
        //{
        //    var chdef = content.ChildrenDefinition;
        //    if (req.HasContentQuery)
        //    {
        //        chdef.ContentQuery = ContentQuery.AddClause(req.ContentQueryText, String.Concat("InTree:'", content.Path, "'"), LogicalOperator.And);

        //        if (req.AutofiltersEnabled != FilterStatus.Default)
        //            chdef.EnableAutofilters = req.AutofiltersEnabled;
        //        if (req.LifespanFilterEnabled != FilterStatus.Default)
        //            chdef.EnableLifespanFilter = req.LifespanFilterEnabled;
        //        if (req.QueryExecutionMode != QueryExecutionMode.Default)
        //            chdef.QueryExecutionMode = req.QueryExecutionMode;
        //        if (req.Top > 0)
        //            chdef.Top = req.Top;
        //        if (req.Skip > 0)
        //            chdef.Skip = req.Skip;
        //        if (req.Sort.Any())
        //            chdef.Sort = req.Sort;
        //    }
        //    else
        //    {
        //        chdef.EnableAutofilters = FilterStatus.Disabled;
        //        if (string.IsNullOrEmpty(chdef.ContentQuery))
        //        {
        //            chdef.ContentQuery = ContentQuery.AddClause(chdef.ContentQuery, String.Concat("InFolder:'", content.Path, "'"), LogicalOperator.And);
        //        }
        //    }

        //    var contents = ProcessOperationQueryResponse(chdef, req, httpContext, out var count);
        //    if (req.CountOnly)
        //        return ODataResponse.CreateCollectionCountResponse(count);
        //    return ODataResponse.CreateChildrenCollectionResponse(contents, count);
        //}
        //private IEnumerable<ODataEntity> ProcessOperationQueryResponse(ChildrenDefinition qdef, ODataRequest req, HttpContext httpContext, out int count)
        //{
        //    var queryText = qdef.ContentQuery;
        //    if (queryText.Contains("}}"))
        //    {
        //        queryText = ContentQuery.ResolveInnerQueries(qdef.ContentQuery, new QuerySettings
        //        {
        //            EnableAutofilters = qdef.EnableAutofilters,
        //            EnableLifespanFilter = qdef.EnableLifespanFilter,
        //            QueryExecutionMode = qdef.QueryExecutionMode,
        //            Sort = qdef.Sort
        //        });
        //    }

        //    var cdef = new ChildrenDefinition
        //    {
        //        PathUsage = qdef.PathUsage,
        //        ContentQuery = queryText,
        //        Top = req.Top > 0 ? req.Top : qdef.Top,
        //        Skip = req.Skip > 0 ? req.Skip : qdef.Skip,
        //        Sort = req.Sort != null && req.Sort.Any() ? req.Sort : qdef.Sort,
        //        CountAllPages = req.HasInlineCount ? req.InlineCount == InlineCount.AllPages : qdef.CountAllPages,
        //        EnableAutofilters = req.AutofiltersEnabled != FilterStatus.Default ? req.AutofiltersEnabled : qdef.EnableAutofilters,
        //        EnableLifespanFilter = req.LifespanFilterEnabled != FilterStatus.Default ? req.AutofiltersEnabled : qdef.EnableLifespanFilter,
        //        QueryExecutionMode = req.QueryExecutionMode != QueryExecutionMode.Default ? req.QueryExecutionMode : qdef.QueryExecutionMode,
        //    };

        //    var snQuery = SnExpression.BuildQuery(req.Filter, typeof(Content), null, cdef);
        //    if (cdef.EnableAutofilters != FilterStatus.Default)
        //        snQuery.EnableAutofilters = cdef.EnableAutofilters;
        //    if (cdef.EnableLifespanFilter != FilterStatus.Default)
        //        snQuery.EnableLifespanFilter = cdef.EnableLifespanFilter;
        //    if (cdef.QueryExecutionMode != QueryExecutionMode.Default)
        //        snQuery.QueryExecutionMode = cdef.QueryExecutionMode;

        //    var result = snQuery.Execute(new SnQueryContext(null, User.Current.Id));
        //    // for optimization purposes this combined condition is examined separately
        //    if (req.InlineCount == InlineCount.AllPages && req.CountOnly)
        //    {
        //        count = result.TotalCount;
        //        return null;
        //    }

        //    var ids = result.Hits.ToArray();
        //    count = req.InlineCount == InlineCount.AllPages ? result.TotalCount : ids.Length;
        //    if (req.CountOnly)
        //    {
        //        return null;
        //    }

        //    var contents = new List<ODataEntity>();
        //    var projector = Projector.Create(req, true);
        //    var missingIds = new List<int>();

        //    foreach (var id in ids)
        //    {
        //        var content = Content.Load(id);
        //        if (content == null)
        //        {
        //            // collect missing ids for logging purposes
        //            missingIds.Add(id);
        //            continue;
        //        }

        //        var fields = CreateFieldDictionary(httpContext, content, projector);
        //        contents.Add(fields);
        //    }

        //    if (missingIds.Count > 0)
        //    {
        //        // subtract missing count from result count
        //        count = Math.Max(0, count - missingIds.Count);

        //        // index anomaly: there are ids in the index that could not be loaded from the database
        //        SnLog.WriteWarning("Missing ids found in the index that could not be loaded from the database. See id list below.",
        //            EventId.Indexing,
        //            properties: new Dictionary<string, object>
        //            {
        //                {"MissingIds", string.Join(", ", missingIds.OrderBy(id => id))}
        //            });
        //    }

        //    return contents;
        //}

        //private ODataResponse GetMultiRefContentResponse(object references, HttpContext httpContext, ODataRequest req)
        //{
        //    if (references == null)
        //        //UNDONE:ODATA: Empty or null?
        //        return ODataResponse.CreateMultipleContentResponse(new ODataEntity[0], 0);

        //    var node = references as Node;
        //    var projector = Projector.Create(req, true);
        //    if (node != null)
        //    {
        //        var contents = new List<ODataEntity>
        //        {
        //            CreateFieldDictionary(httpContext, Content.Create(node), projector)
        //        };
        //        //TODO: ODATA: multiref item: get available types from reference property
        //        return ODataResponse.CreateMultipleContentResponse(contents, 1);
        //    }

        //    if (references is IEnumerable enumerable)
        //    {
        //        var skipped = 0;
        //        var allcount = 0;
        //        var count = 0;
        //        var realcount = 0;
        //        var contents = new List<ODataEntity>();
        //        if (req.HasFilter)
        //        {
        //            var filtered = new FilteredEnumerable(enumerable, (LambdaExpression)req.Filter, req.Top, req.Skip);
        //            foreach (Node item in filtered)
        //                contents.Add(CreateFieldDictionary(httpContext, Content.Create(item), projector));
        //            allcount = filtered.AllCount;
        //            realcount = contents.Count;
        //        }
        //        else
        //        {
        //            foreach (Node item in enumerable)
        //            {
        //                allcount++;
        //                if (skipped++ < req.Skip)
        //                    continue;
        //                if (req.Top == 0 || count++ < req.Top)
        //                {
        //                    contents.Add(CreateFieldDictionary(httpContext, Content.Create(item), projector));
        //                    realcount++;
        //                }
        //            }
        //        }
        //        return ODataResponse.CreateMultipleContentResponse(contents, req.InlineCount == InlineCount.AllPages ? allcount : realcount);
        //    }
        //    //UNDONE:ODATA: Empty or null?
        //    return ODataResponse.CreateMultipleContentResponse(new ODataEntity[0], 0);
        //}
        //private ODataResponse GetSingleRefContentResponse(object references, HttpContext httpContext, ODataRequest req)
        //{
        //    if (references != null)
        //    {
        //        if (references is Node node)
        //            return ODataResponse.CreateSingleContentResponse(CreateFieldDictionary(httpContext, req, Content.Create(node), false));

        //        if (references is IEnumerable enumerable)
        //            foreach (Node item in enumerable)
        //                // Only the first item plays
        //                return ODataResponse.CreateSingleContentResponse(CreateFieldDictionary(httpContext, req,
        //                    Content.Create(item), false));
        //    }
        //    //UNDONE:ODATA: Empty or null?
        //    return null;
        //}

        /* ==== */

        //internal ODataResponse GetContentPropertyResponse(Content content, string propertyName, bool rawValue,
        //    HttpContext httpContext, ODataRequest req)
        //{
        //    if (propertyName == ODataMiddleware.ActionsPropertyName)
        //    {
        //        var items = ODataTools.GetActionItems(content, req, httpContext).ToArray();
        //        return rawValue
        //            ? (ODataResponse)ODataResponse.CreateActionsPropertyRawResponse(items)
        //            : ODataResponse.CreateActionsPropertyResponse(items);
        //    }
        //    if (propertyName == ODataMiddleware.ChildrenPropertyName)
        //    {
        //        return GetChildrenCollectionResponse(content, httpContext, req);
        //    }

        //    if (content.Fields.TryGetValue(propertyName, out var field))
        //    {
        //        if (field is ReferenceField refField)
        //        {
        //            var refFieldSetting = refField.FieldSetting as ReferenceFieldSetting;
        //            var isMultiRef = true;
        //            if (refFieldSetting != null)
        //                isMultiRef = refFieldSetting.AllowMultiple == true;
        //            return isMultiRef
        //                ? GetMultiRefContentResponse(refField.GetData(), httpContext, req)
        //                : GetSingleRefContentResponse(refField.GetData(), httpContext, req);
        //        }

        //        if (field is AllowedChildTypesField actField)
        //            return GetMultiRefContentResponse(actField.GetData(), httpContext, req);

        //        if (!rawValue)
        //            return ODataResponse.CreateSingleContentResponse(new ODataEntity {{propertyName, field.GetData()}});

        //        return ODataResponse.CreateRawResponse(field.GetData());
        //    }

        //    return ExecuteFunction(httpContext, req, content);
        //}

        /* ==== */

        /// <summary>
        /// Handles GET operations. Parameters come from the URL or the request stream.
        /// </summary>
        // orig name:  GetOperationResultResponse
        //internal ODataResponse ExecuteFunction(HttpContext httpContext, ODataRequest odataReq, Content content)
        //{
        //    //var content = ODataMiddleware.LoadContentByVersionRequest(odataReq.RepositoryPath, httpContext);
        //    //if (content == null)
        //    //    throw new ContentNotFoundException(string.Format(SNSR.GetString("$Action,ErrorContentNotFound"), odataReq.RepositoryPath));

        //    var action = ActionResolver.GetAction(content, odataReq.Scenario, odataReq.PropertyName, null, null, httpContext);
        //    if (action == null)
        //    {
        //        // check if this is a versioning action (e.g. a checkout)
        //        SavingAction.AssertVersioningAction(content, odataReq.PropertyName, true);

        //        throw new InvalidContentActionException(InvalidContentActionReason.UnknownAction, content.Path, null, odataReq.PropertyName);
        //    }

        //    if (!action.IsODataOperation)
        //        throw new ODataException("Not an OData operation.", ODataExceptionCode.IllegalInvoke);
        //    if (action.CausesStateChange)
        //        throw new ODataException("OData action cannot be invoked with HTTP GET.", ODataExceptionCode.IllegalInvoke);

        //    if (action.Forbidden || (action.GetApplication() != null && !action.GetApplication().Security.HasPermission(PermissionType.RunApplication)))
        //        throw new InvalidContentActionException("Forbidden action: " + odataReq.PropertyName);

        //    var parameters = GetOperationParameters(action, httpContext.Request);
        //    var response = action.Execute(content, parameters);

        //    if (response is Content responseAsContent)
        //    {
        //        return ODataResponse.CreateSingleContentResponse(CreateFieldDictionary(httpContext, odataReq, responseAsContent, false));
        //        //WriteSingleContent(responseAsContent, httpContext);
        //        //return;
        //    }

        //    response = ProcessOperationResponse(response, odataReq, httpContext, out var count);
        //    return GetOperationResultResponse(response, httpContext, odataReq, count);
        //}

        //private ODataResponse GetOperationResultResponse(object result, HttpContext httpContext, ODataRequest odataReq, int allCount)
        //{
        //    //UNDONE:ODATA: is this test unnecessary?
        //    if (result is Content content)
        //        return ODataResponse.CreateSingleContentResponse(CreateFieldDictionary(httpContext, odataReq, content, false));

        //    //UNDONE:ODATA: is this test unnecessary?
        //    if (result is IEnumerable<Content> enumerable)
        //        return GetMultiRefContentResponse(enumerable, httpContext, odataReq);

        //    return ODataResponse.CreateOperationCustomResultResponse(result, odataReq.InlineCount == InlineCount.AllPages ? allCount : (int?)null);
        //}



        //private object ProcessOperationResponse(object response, ODataRequest odataReq, HttpContext httpContext, out int count)
        //{
        //    if (response is ChildrenDefinition qdef)
        //        return ProcessOperationQueryResponse(qdef, odataReq, httpContext, out count);

        //    if (response is IEnumerable<Content> coll)
        //        return ProcessOperationCollectionResponse(coll, odataReq, httpContext, out count);

        //    if (response is IDictionary dict)
        //    {
        //        count = dict.Count;
        //        var targetTypized = new Dictionary<Content, object>();
        //        foreach (var item in dict.Keys)
        //        {
        //            if (!(item is Content content))
        //                return response;
        //            targetTypized.Add(content, dict[content]);
        //        }
        //        return ProcessOperationDictionaryResponse(targetTypized, odataReq, httpContext, out count);
        //    }

        //    // get real count from an enumerable
        //    if (response is IEnumerable enumerable)
        //    {
        //        var c = 0;
        //        // ReSharper disable once UnusedVariable
        //        foreach (var x in enumerable)
        //            c++;
        //        count = c;
        //    }
        //    else
        //    {
        //        count = 1;
        //    }

        //    if (response != null && response.ToString() == "{ PreviewAvailable = True }")
        //        return true;
        //    if (response != null && response.ToString() == "{ PreviewAvailable = False }")
        //        return false;
        //    return response;
        //}
        //private IEnumerable<ODataEntity> ProcessOperationDictionaryResponse(IDictionary<Content, object> input,
        //    ODataRequest req, HttpContext httpContext, out int count)
        //{
        //    var x = ProcessODataFilters(input.Keys, req, out var totalCount);

        //    var output = new List<ODataEntity>();
        //    var projector = Projector.Create(req, true);
        //    foreach (var content in x)
        //    {
        //        var fields = CreateFieldDictionary(httpContext, content, projector);
        //        var item = new ODataEntity
        //        {
        //            {"key", fields},
        //            {"value", input[content]}
        //        };
        //        output.Add(item);
        //    }
        //    count = totalCount ?? output.Count;
        //    if (req.CountOnly)
        //        return null;
        //    return output;
        //}
        //private IEnumerable<ODataEntity> ProcessOperationCollectionResponse(IEnumerable<Content> inputContents,
        //    ODataRequest req, HttpContext httpContext, out int count)
        //{
        //    var x = ProcessODataFilters(inputContents, req, out var totalCount);

        //    var outContents = new List<ODataEntity>();
        //    var projector = Projector.Create(req, true);
        //    foreach (var content in x)
        //    {
        //        var fields = CreateFieldDictionary(httpContext, content, projector);
        //        outContents.Add(fields);
        //    }

        //    count = totalCount ?? outContents.Count;
        //    if (req.CountOnly)
        //        return null;
        //    return outContents;
        //}
        //private IEnumerable<Content> ProcessODataFilters(IEnumerable<Content> inputContents, ODataRequest req, out int? totalCount)
        //{
        //    var x = inputContents;
        //    if (req.HasFilter)
        //    {
        //        if (x is IQueryable<Content> y)
        //        {
        //            x = y.Where((Expression<Func<Content, bool>>)req.Filter);
        //        }
        //        else
        //        {
        //            var filter = SnExpression.GetCaseInsensitiveFilter(req.Filter);
        //            var lambdaExpr = (LambdaExpression)filter;
        //            x = x.Where((Func<Content, bool>)lambdaExpr.Compile());
        //        }
        //    }
        //    if (req.HasSort)
        //        x = AddSortToCollectionExpression(x, req.Sort);

        //    if (req.InlineCount == InlineCount.AllPages)
        //    {
        //        x = x.ToList();
        //        totalCount = ((IList)x).Count;
        //    }
        //    else
        //    {
        //        totalCount = null;
        //    }

        //    if (req.HasSkip)
        //        x = x.Skip(req.Skip);
        //    if (req.HasTop)
        //        x = x.Take(req.Top);

        //    return x;
        //}
        //private IEnumerable<Content> AddSortToCollectionExpression(IEnumerable<Content> contents, IEnumerable<SortInfo> sort)
        //{
        //    IOrderedEnumerable<Content> sortedContents = null;
        //    var contentArray = contents as Content[] ?? contents.ToArray();
        //    foreach (var sortInfo in sort)
        //    {
        //        if (sortedContents == null)
        //        {
        //            sortedContents = sortInfo.Reverse
        //                ? contentArray.OrderByDescending(c => c[sortInfo.FieldName])
        //                : contentArray.OrderBy(c => c[sortInfo.FieldName]);
        //        }
        //        else
        //        {
        //            sortedContents = sortInfo.Reverse
        //                ? sortedContents.ThenByDescending(c => c[sortInfo.FieldName])
        //                : sortedContents.ThenBy(c => c[sortInfo.FieldName]);
        //        }
        //    }
        //    return sortedContents ?? contents;
        //}



        //private object[] GetOperationParameters(ActionBase action, HttpRequest request)
        //{
        //    if ((action.ActionParameters?.Length ?? 0) == 0)
        //        return ActionParameter.EmptyValues;

        //    Debug.Assert(action.ActionParameters != null, "action.ActionParameters != null");
        //    var values = new object[action.ActionParameters.Length];

        //    var parameters = action.ActionParameters;
        //    if (parameters.Length == 1 && parameters[0].Name == null)
        //    {
        //        throw new ArgumentException("Cannot parse unnamed parameter from URL. This operation expects POST verb.");
        //    }
        //    else
        //    {
        //        var i = 0;
        //        foreach (var parameter in parameters)
        //        {
        //            var name = parameter.Name;
        //            var type = parameter.Type;
        //            var val = request.Query[name];
        //            if (val == StringValues.Empty)
        //            {
        //                if (parameter.Required)
        //                    throw new ArgumentNullException(parameter.Name);
        //            }
        //            else
        //            {
        //                var valStr = (string)val;

        //                if (type == typeof(string))
        //                {
        //                    values[i] = valStr;
        //                }
        //                else if (type == typeof(bool))
        //                {
        //                    // we handle "True", "true" and "1" as boolean true values
        //                    values[i] = JsonConvert.DeserializeObject(valStr.ToLower(), type);
        //                }
        //                else if (type.IsEnum)
        //                {
        //                    values[i] = Enum.Parse(type, valStr, true);
        //                }
        //                else if (type == typeof(string[]))
        //                {
        //                    var parsed = false;
        //                    try
        //                    {
        //                        values[i] = JsonConvert.DeserializeObject(valStr, type);
        //                        parsed = true;
        //                    }
        //                    catch // recompute
        //                    {
        //                        // ignored
        //                    }
        //                    if (!parsed)
        //                    {
        //                        if (valStr.StartsWith("'"))
        //                            values[i] = GetStringArrayFromString(name, valStr, '\'');
        //                        else if (valStr.StartsWith("\""))
        //                            values[i] = GetStringArrayFromString(name, valStr, '"');
        //                        else
        //                            values[i] = valStr.Split(',').Select(s => s?.Trim()).ToArray();
        //                    }
        //                }
        //                else
        //                {
        //                    values[i] = JsonConvert.DeserializeObject(valStr, type);
        //                }
        //            }
        //            i++;
        //        }
        //    }
        //    return values;
        //}
        //private string[] GetStringArrayFromString(string paramName, string src, char stringEnvelope)
        //{
        //    var result = new List<string>();
        //    int startPos = -1;
        //    bool started = false;
        //    for (int i = 0; i < src.Length; i++)
        //    {
        //        var c = src[i];
        //        if (c == stringEnvelope)
        //        {
        //            if (!started)
        //            {
        //                started = true;
        //                startPos = i + 1;
        //            }
        //            else
        //            {
        //                started = false;
        //                result.Add(src.Substring(startPos, i - startPos));
        //            }
        //        }
        //        else if (!started)
        //        {
        //            if (c != ' ' && c != ',')
        //                throw new ODataException("Parameter error: cannot parse a string array. Name: " + paramName, ODataExceptionCode.NotSpecified);
        //        }
        //    }
        //    return result.ToArray();
        //}

        /* ----------------------------------------------------------------------------------- */

        //private ODataEntity CreateFieldDictionary(HttpContext httpContext, Content content, Projector projector)
        //{
        //    return projector.Project(content, httpContext);
        //}
        //private ODataEntity CreateFieldDictionary(HttpContext httpContext, ODataRequest odataRequest, Content content,
        //    bool isCollectionItem)
        //{
        //    var projector = Projector.Create(odataRequest, isCollectionItem, content);
        //    return projector.Project(content, httpContext);
        //}

        /* =================================================================================== */




        internal static JObject Read(Stream inputStream)
        {
            string models;
            if (inputStream == null)
                return null;
            using (var reader = new StreamReader(inputStream))
                models = reader.ReadToEnd();

            return Read(models);
        }
        /// <summary>
        /// Helper method for deserializing the given string representation.
        /// </summary>
        /// <param name="models">JSON object that will be deserialized.</param>
        /// <returns>Deserialized JObject instance.</returns>
        public static JObject Read(string models)
        {
            if (string.IsNullOrEmpty(models))
                return null;

            var firstChar = models.Last() == ']' ? '[' : '{';
            var p = models.IndexOf(firstChar);
            if (p > 0)
                models = models.Substring(p);

            var settings = new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.IsoDateFormat };
            var serializer = JsonSerializer.Create(settings);
            var jreader = new JsonTextReader(new StringReader(models));
            var deserialized = serializer.Deserialize(jreader);

            if (deserialized is JObject jObject)
                return jObject;
            if (deserialized is JArray jArray)
                return jArray[0] as JObject;

            throw new SnNotSupportedException();
        }
        internal static object Read(Stream inputStream, Type type)
        {
            string models;
            using (var reader = new StreamReader(inputStream))
                models = reader.ReadToEnd(); // HttpUtility.UrlDecode(reader.ReadToEnd());

            if (string.IsNullOrEmpty(models))
                return null;

            var firstChar = models.Last() == ']' ? '[' : '{';
            var p = models.IndexOf(firstChar);
            if (p > 0)
                models = models.Substring(p);

            var settings = new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.IsoDateFormat };
            var serializer = JsonSerializer.Create(settings);
            var jreader = new JsonTextReader(new StringReader(models));
            var deserialized = serializer.Deserialize(jreader, type);

            return deserialized;
        }

        internal static string GetEntityUrl(string path)
        {
            path = path.TrimEnd('/');

            var p = path.LastIndexOf('/');
            if (p < 0)
                return string.Concat("(", path, ")");

            return string.Concat(path.Substring(0, p), "('", path.Substring(p + 1), "')");
        }

        internal static void ContentNotFound(HttpContext httpContext)
        {
            httpContext.Response.Clear();
            httpContext.Response.StatusCode = 404;
        }
        internal static void ContentAlreadyExists(string path)
        {
            throw new ODataException(SNSR.GetString(SNSR.Exceptions.OData.ContentAlreadyExists_1, path), ODataExceptionCode.ContentAlreadyExists);
        }
        internal static void ResourceNotFound(Content content, string propertyName)
        {
            throw new ODataException(SNSR.GetString(SNSR.Exceptions.OData.ResourceNotFound_2, content.Path, propertyName), ODataExceptionCode.ResourceNotFound);
        }
        internal static void ResourceNotFound()
        {
            throw new ODataException(SNSR.GetString(SNSR.Exceptions.OData.ResourceNotFound), ODataExceptionCode.ResourceNotFound);
        }

        // ==============================================================================================================

        internal static Content LoadContentByVersionRequest(string path, HttpContext httpContext)
        {
            var versionRequest = httpContext.Request.Query["version"].ToString();
            return !string.IsNullOrEmpty(versionRequest) && VersionNumber.TryParse(versionRequest, out var version)
                ? Content.Load(path, version)
                : Content.Load(path);
        }

        private Content CreateNewContent(JObject model, ODataRequest odataRequest)
        {
            var contentTypeName = GetPropertyValue<string>("__ContentType", model);
            var templateName = GetPropertyValue<string>("__ContentTemplate", model);

            var name = GetPropertyValue<string>("Name", model);
            if (string.IsNullOrEmpty(name))
            {
                var displayName = GetPropertyValue<string>("DisplayName", model);
                name = ContentNamingProvider.GetNameFromDisplayName(displayName);
            }
            else
            {
                // do not allow saving a content with unencoded name
                name = ContentNamingProvider.GetNameFromDisplayName(name);
            }

            var parent = Node.Load<GenericContent>(odataRequest.RepositoryPath);
            if (string.IsNullOrEmpty(contentTypeName))
            {
                var allowedChildTypeNames = parent.GetAllowedChildTypeNames();

                if (allowedChildTypeNames is AllContentTypeNames)
                {
                    contentTypeName = typeof(ContentRepository.File).Name;
                }
                else
                {
                    var allowedContentTypeNames = parent.GetAllowedChildTypeNames().ToArray();
                    contentTypeName = allowedContentTypeNames.FirstOrDefault();
                    if (string.IsNullOrEmpty(contentTypeName))
                        contentTypeName = typeof(ContentRepository.File).Name;
                }
            }

            Content content;
            Node template = null;
            if (templateName != null)
                template = ContentTemplate.GetNamedTemplate(contentTypeName, templateName);

            if (template == null)
            {
                content = Content.CreateNew(contentTypeName, parent, name);
            }
            else
            {
                var templated = ContentTemplate.CreateFromTemplate(parent, template, name);
                content = Content.Create(templated);
            }


            UpdateFields(content, model);

            if (odataRequest.MultistepSave)
                content.Save(SavingMode.StartMultistepSave);
            else
                content.Save();

            return content;
        }
        private static readonly List<string> SafeFieldsInReset = new List<string>(new[] {
            "Name",
            "CreatedBy", "CreatedById", "CreationDate",
            "ModifiedBy", "ModifiedById", "ModificationDate" });

        private static Content LoadContentOrVirtualChild(ODataRequest odataReq)
        {
            var content = Content.Load(odataReq.RepositoryPath);

            if (content == null)
            {
                // try to load a virtual content
                var parentPath = RepositoryPath.GetParentPath(odataReq.RepositoryPath);
                var name = RepositoryPath.GetFileName(odataReq.RepositoryPath);
                if (Node.LoadNode(parentPath) is ISupportsVirtualChildren vp)
                    content = vp.GetChild(name);
            }

            return content;
        }

        private void ResetContent(Content content)
        {
            // Create "dummy" content
            var newContent = SystemAccount.Execute(() => Content.CreateNew(content.ContentType.Name, content.ContentHandler.Parent, null));

            Aspect[] aspects = null;
            if (content.ContentHandler.HasProperty(GenericContent.ASPECTS))
            {
                // Get aspects
                aspects = content.ContentHandler.GetReferences(GenericContent.ASPECTS).Cast<Aspect>().ToArray();

                // Reset aspect fields
                if (content.ContentHandler is GenericContent gc)
                {
                    content.RemoveAllAspects();
                    gc.AspectData = null;
                    gc.ClearReference(GenericContent.ASPECTS);
                }
            }

            // Reset regular fields
            foreach (var field in content.Fields.Values)
            {
                var fieldName = field.Name;
                if (newContent.Fields.Any(f => f.Value.Name == fieldName) && !field.ReadOnly && !SafeFieldsInReset.Contains(fieldName))
                    content[fieldName] = newContent[fieldName];
            }

            if (content.ContentHandler.HasProperty(GenericContent.ASPECTS))
            {
                // Re-add all the aspects
                content.AddAspects(aspects);
            }
        }
        private void UpdateContent(Content content, JObject model, ODataRequest odataRequest)
        {
            UpdateFields(content, model);

            if (odataRequest.MultistepSave)
                content.Save(SavingMode.StartMultistepSave);
            else
                content.Save();
        }
        /// <summary>
        /// Helper method for updating the given <see cref="Content"/> with a model represented by JObject.
        /// The <see cref="Content"/> will not be saved.
        /// </summary>
        /// <param name="content">The <see cref="Content"/> that will be modified. Cannot be null.</param>
        /// <param name="model">The modifier JObject instance. Cannot be null.</param>
        public static void UpdateFields(Content content, JObject model)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var isNew = content.Id == 0;
            foreach (var prop in model.Properties())
            {
                if (string.IsNullOrEmpty(prop.Name) || prop.Name == "__ContentType" || prop.Name == "__ContentTemplate" || prop.Name == "Type" || prop.Name == "ContentType")
                    continue;

                var hasField = content.Fields.TryGetValue(prop.Name, out var field);
                if (!hasField && content.SupportsAddingFieldsOnTheFly && (prop.Value as JValue)?.Value != null)
                {
                    var value = ((JValue)prop.Value).Value;
                    var fieldSetting = FieldSetting.InferFieldSettingFromType(value.GetType(), prop.Name);
                    var meta = new FieldMetadata(true, true, prop.Name, prop.Name, fieldSetting);
                    hasField = content.AddFieldsOnTheFly(new[] { meta }) && content.Fields.TryGetValue(prop.Name, out field);
                }

                if (hasField)
                {
                    if (!field.ReadOnly)
                    {
                        if (prop.Value is JValue jvalue)
                        {
                            if (field is IntegerField)
                            {
                                field.SetData(Convert.ToInt32(jvalue.Value));
                                continue;
                            }
                            if (field is DateTimeField && jvalue.Value == null)
                                continue;
                            if (isNew && field is ReferenceField && jvalue.Value == null)
                            {
                                if (field.Name == "CreatedBy" || field.Name == "ModifiedBy")
                                    continue;
                            }
                            if (field is ReferenceField && jvalue.Value != null)
                            {
                                var refNode = jvalue.Type == JTokenType.Integer
                                    ? Node.LoadNode(Convert.ToInt32(jvalue.Value))
                                    : Node.LoadNode(jvalue.Value.ToString());

                                field.SetData(refNode);
                                continue;
                            }
                            if (isNew && field.Name == "Name" && jvalue.Value != null)
                            {
                                field.SetData(ContentNamingProvider.GetNameFromDisplayName(jvalue.Value.ToString()));
                                continue;
                            }

                            field.SetData(jvalue.Value);
                            continue;
                        }

                        if (prop.Value is JObject)
                        {
                            //TODO: ODATA: setting field when posted value is JObject.
                            // field.SetData(jvalue.Value);
                            continue;
                        }

                        if (prop.Value is JArray avalue)
                        {
                            if (field is ReferenceField)
                            {
                                var refValues = avalue.Values().ToList();
                                if (refValues.Count == 0)
                                {
                                    field.SetData(null);
                                    continue;
                                }

                                var fsetting = field.FieldSetting as ReferenceFieldSetting;
                                var nodes = refValues.Select(rv => rv.Type == JTokenType.Integer ? Node.LoadNode(Convert.ToInt32(rv.ToString())) : Node.LoadNode(rv.ToString()));

                                if (fsetting?.AllowMultiple != null && fsetting.AllowMultiple.Value)
                                    field.SetData(nodes);
                                else
                                    field.SetData(nodes.First());

                            }
                            else if (field is ChoiceField)
                            {
                                // ChoiceField expects the value to be of type List<string>
                                var list = new List<string>();
                                foreach (var token in avalue)
                                {
                                    if (token is JValue value)
                                        list.Add(value.Value.ToString());
                                    else
                                        throw new Exception(
                                            $"Token type {token.GetType().Name} for field {field.Name} (type {field.GetType().Name}) is not supported.");
                                }

                                field.SetData(list);
                            }
                            else if (field is AllowedChildTypesField &&
                                     field.Name == "AllowedChildTypes" &&
                                     content.ContentHandler is GenericContent gc)
                            {
                                var types = avalue.Values().Select(rv =>
                                {
                                    switch (rv.Type)
                                    {
                                        case JTokenType.Integer:
                                            return Node.LoadNode(Convert.ToInt32(rv.ToString())) as ContentType;
                                        default:
                                            var typeId = rv.ToString();
                                            if (RepositoryPath.IsValidPath(typeId) == RepositoryPath.PathResult.Correct)
                                                return Node.LoadNode(typeId) as ContentType;
                                            return ContentType.GetByName(typeId);
                                    }
                                }).Where(ct => ct != null).ToArray();

                                gc.SetAllowedChildTypes(types);
                            }

                            continue;
                        }

                        throw new SnNotSupportedException();
                    }
                }
            }
        }

        private T GetPropertyValue<T>(string name, JObject model)
        {
            if (model[name] is JValue jvalue)
                return (T)jvalue.Value;
            return default(T);
        }

        /// <summary>
        /// Returns an OData path that can request the entity identified by the given path. This path is part of the OData entity request. For example
        /// "/Root/MyFolder/MyDocument.doc" will be transformed to "/Root/MyFolder('MyDocument.doc')"
        /// </summary>
        /// <param name="path">This path will be transformed.</param>
        /// <returns>An OData path.</returns>
        public static string GetODataPath(string path)
        {
            if (string.Compare(path, Identifiers.RootPath, StringComparison.OrdinalIgnoreCase) == 0)
                return string.Empty;

            return GetODataPath(RepositoryPath.GetParentPath(path), RepositoryPath.GetFileName(path));
        }
        /// <summary>
        /// Returns an OData path that can request the entity identified by the given path plus name. This path is part of the OData entity request. For example
        /// path = "/Root/MyFolder" and name = "MyDocument.doc" will be transformed to "/Root/MyFolder('MyDocument.doc')".
        /// </summary>
        /// <param name="parentPath">A container path.</param>
        /// <param name="name">Content's name in the given container.</param>
        /// <returns>An OData path.</returns>
        public static string GetODataPath(string parentPath, string name)
        {
            return $"{parentPath}('{name}')";
        }
    }

    internal interface IActionResolver
    {
        GenericScenario GetScenario(string name, string parameters, HttpContext httpContext);
        IEnumerable<ActionBase> GetActions(Content context, string scenario, string backUri, HttpContext httpContext);
        ActionBase GetAction(Content context, string scenario, string actionName, string backUri, object parameters, HttpContext httpContext);
    }
    internal class DefaultActionResolver : IActionResolver
    {
        public GenericScenario GetScenario(string name, string parameters, HttpContext httpContext)
        {
            return ScenarioManager.GetScenario(name, httpContext.Request.QueryString.ToString());
        }
        public IEnumerable<ActionBase> GetActions(Content context, string scenario, string backUri, HttpContext httpContext)
        {
            return ActionFramework.GetActions(context, scenario, null, backUri);
        }
        public ActionBase GetAction(Content context, string scenario, string actionName, string backUri, object parameters, HttpContext httpContext)
        {
            return backUri == null
                ? ActionFramework.GetAction(actionName, context, parameters)
                : ActionFramework.GetAction(actionName, context, backUri, parameters);
        }
    }
}
