using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
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
using SenseNet.OData.Metadata;
using STT = System.Threading.Tasks;
// ReSharper disable ArrangeThisQualifier

namespace SenseNet.OData
{
    public static class SenseNetODataExtensions
    {
        public static IApplicationBuilder UseSenseNetOdata(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ODataHandler>();
        }

        public static ODataResponse GetODataResponse(this HttpContext httpContext)
        {
            return httpContext.Items[ODataResponse.Key] as ODataResponse;
        }
        public static void SetODataResponse(this HttpContext httpContext, ODataResponse value)
        {
            httpContext.Items[ODataResponse.Key] = value;
        }
    }

    /// <summary>
    /// AN ASP.NET Core middleware to process the OData requests.
    /// </summary>
    public class ODataHandler
    {
        // Do not remove setter.
        internal static IActionResolver ActionResolver { get; set; }

        internal static readonly string[] HeadFieldNames = new[] { "Id", "Name", "DisplayName", "Icon", "CreationDate", "ModificationDate", "CreatedBy", "ModifiedBy" };
        internal static readonly List<string> DisabledFieldNames = new List<string>(new[] { "TypeIs", "InTree", "InFolder", "NodeType", "Rate"/*, "VersioningMode", "ApprovingMode", "RateAvg", "RateCount"*/ });
        internal static readonly List<string> DeferredFieldNames = new List<string>(new[] { "AllowedChildTypes", "EffectiveAllowedChildTypes" });
        internal static readonly List<string> AllowedMethodNamesWithoutContent = new List<string>(new[] { "PATCH", "PUT", "POST", "DELETE" });

        internal static List<JsonConverter> JsonConverters { get; }
        internal static List<FieldConverter> FieldConverters { get; }

        static ODataHandler()
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

        /// <inheritdoc select="summary" />
        /// <remarks>Returns with false in this implementation.</remarks>
        public bool IsReusable => false;

        internal ODataRequest ODataRequest { get; private set; }

        private readonly RequestDelegate _next;
        // Must have constructor with this signature, otherwise exception at run time
        public ODataHandler(RequestDelegate next)
        {
            _next = next;
        }


        public async STT.Task Invoke(HttpContext httpContext)
        {
            var response = ProcessRequest(httpContext);
            httpContext.SetODataResponse(response);

            await _next(httpContext);

            //UNDONE: Let to know whether ODataResponse is changed or not
            var changedResponse = httpContext.GetODataResponse();
            if (changedResponse != null)
            {
                httpContext.Response.ContentType = "text/plain";
                await httpContext.Response.WriteAsync(changedResponse.Value.ToString());
            }
        }

        /// <inheritdoc />
        /// <remarks>Processes the OData web request.</remarks>
        public ODataResponse ProcessRequest(HttpContext httpContext)
        {
            return ProcessRequest(httpContext, httpContext.Request.Method.ToUpper(), httpContext.Request.Body);
        }
        /// <summary>
        /// Processes the OData web request. Designed for test purposes.
        /// </summary>
        /// <param name="httpContext">An <see cref="HttpContext" /> object that provides references to the intrinsic server objects (for example, <see langword="Request" />, <see langword="Response" />, <see langword="Session" />, and <see langword="Server" />) used to service HTTP requests. </param>
        /// <param name="httpMethod">HTTP protocol method.</param>
        /// <param name="inputStream">Request stream containing the posted JSON object.</param>
        public ODataResponse ProcessRequest(HttpContext httpContext, string httpMethod, Stream inputStream)
        {
            ODataRequest odataReq = null;
            ODataFormatter formatter = null;
            try
            {
                Content content;
                //var uri = new Uri(httpContext.Request.GetEncodedUrl());
                //var path = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
                var path = httpContext.Request.Path;
                odataReq = ODataRequest.Parse(path, httpContext);
                if (odataReq == null)
                {
                    formatter = ODataFormatter.Create("json");
                    throw new ODataException("The Request is not an OData request.", ODataExceptionCode.RequestError);
                }

                this.ODataRequest = odataReq;
                Exception requestError = this.ODataRequest.RequestError;

                formatter = ODataFormatter.Create(httpContext, odataReq);
                if (formatter == null)
                {
                    formatter = ODataFormatter.Create("json");
                    throw new ODataException(ODataExceptionCode.InvalidFormatParameter);
                }

                if (requestError != null)
                {
                    var innerOdataError = requestError as ODataException;
                    var message = "An error occured during request parsing. " + requestError.Message +
                                  " See inner exception for details.";
                    var code = innerOdataError?.ODataExceptionCode ?? ODataExceptionCode.RequestError;
                    throw new ODataException(message, code, requestError);
                }

                odataReq.Format = formatter.FormatName;
                formatter.Initialize(odataReq);

                var exists = false;// Node.Exists(odataReq.RepositoryPath);
                if (!exists && !odataReq.IsServiceDocumentRequest && !odataReq.IsMetadataRequest && !AllowedMethodNamesWithoutContent.Contains(httpMethod))
                {
                    return ODataResponse.CreateNoContentResponse();//404
                }

                JObject model;
                switch (httpMethod)
                {
                    case "GET":
                        if (odataReq.IsServiceDocumentRequest)
                        {
                            return ODataResponse.CreateServiceDocumentResponse(GetServiceDocument(httpContext, odataReq));
                        }
                        else if (odataReq.IsMetadataRequest)
                        {
                            formatter.WriteMetadata(httpContext, odataReq);
                        }
                        else
                        {
                            if (!Node.Exists(odataReq.RepositoryPath))
                                ContentNotFound(httpContext);
                            else if (odataReq.IsCollection)
                                formatter.WriteChildrenCollection(odataReq.RepositoryPath, httpContext, odataReq);
                            else if (odataReq.IsMemberRequest)
                                formatter.WriteContentProperty(odataReq.RepositoryPath, odataReq.PropertyName,
                                    odataReq.IsRawValueRequest, httpContext, odataReq);
                            else
                                formatter.WriteSingleContent(odataReq.RepositoryPath, httpContext);
                        }
                        break;
                    case "PUT": // update
                        if (odataReq.IsMemberRequest)
                        {
                            throw new ODataException("Cannot access a member with HTTP PUT.",
                                ODataExceptionCode.IllegalInvoke);
                        }
                        else
                        {
                            model = Read(inputStream);
                            content = LoadContentOrVirtualChild(odataReq);
                            if (content == null)
                            {
                                return ODataResponse.CreateNoContentResponse();
                            }

                            ResetContent(content);
                            UpdateContent(content, model, odataReq);
                            formatter.WriteSingleContent(content, httpContext);
                        }
                        break;
                    case "MERGE":
                    case "PATCH": // update
                        if (odataReq.IsMemberRequest)
                        {
                            throw new ODataException(
                                String.Concat("Cannot access a member with HTTP ", httpMethod, "."),
                                ODataExceptionCode.IllegalInvoke);
                        }
                        else
                        {
                            model = Read(inputStream);
                            content = LoadContentOrVirtualChild(odataReq);
                            if (content == null)
                            {
                                return ODataResponse.CreateNoContentResponse();
                            }

                            UpdateContent(content, model, odataReq);
                            formatter.WriteSingleContent(content, httpContext);
                        }
                        break;
                    case "POST": // invoke an action, create content
                        if (odataReq.IsMemberRequest)
                        {
                            formatter.WriteOperationResult(inputStream, httpContext, odataReq);
                        }
                        else
                        {
                            // parent must exist
                            if (!Node.Exists(odataReq.RepositoryPath))
                            {
                                return ODataResponse.CreateNoContentResponse();
                            }
                            model = Read(inputStream);
                            content = CreateContent(model, odataReq);
                            formatter.WriteSingleContent(content, httpContext);
                        }
                        break;
                    case "DELETE":
                        if (odataReq.IsMemberRequest)
                        {
                            throw new ODataException(
                                String.Concat("Cannot access a member with HTTP ", httpMethod, "."),
                                ODataExceptionCode.IllegalInvoke);
                        }
                        else
                        {
                            content = LoadContentOrVirtualChild(odataReq);
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
                if (odataReq != null && User.Current.Id == Identifiers.VisitorUserId)
                {
                    var head = NodeHead.Get(odataReq.RepositoryPath);
                    if (head != null && !SecurityHandler.HasPermission(head, PermissionType.Open))
                    {
                        return ODataResponse.CreateNoContentResponse();
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
            //        formatter?.WriteErrorResponse(httpContext, oe);
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

            return ODataResponse.CreateNoContentResponse();
        }

        internal string[] GetServiceDocument(HttpContext httpContext, ODataRequest req)
        {
            var rootContent = Content.Load(req.RepositoryPath);
            var topLevelNames = rootContent?.Children.Select(n => n.Name).ToArray() ?? new[] {Repository.RootName};

            return topLevelNames;
        }







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
            var versionRequest = httpContext.Request.Query["version"];
            return VersionNumber.TryParse(versionRequest, out var version)
                ? Content.Load(path, version)
                : Content.Load(path);
        }

        private Content CreateContent(JObject model, ODataRequest odataRequest)
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
