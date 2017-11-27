using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Portal.Virtualization;
using SenseNet.Search;
using SenseNet.ContentRepository.Linq;
using System.Linq.Expressions;
using SenseNet.ContentRepository.Schema;
using SenseNet.Tools;

namespace SenseNet.Portal.OData
{
    public class ODataHandler : IHttpHandler
    {
        internal static IActionResolver ActionResolver { get; private set; }

        internal static readonly string[] HeadFieldNames = new[] { "Id", "Name", "DisplayName", "Icon", "CreationDate", "ModificationDate", "CreatedBy", "ModifiedBy" };
        internal static readonly List<string> DisabledFieldNames = new List<string>(new[] { "TypeIs", "InTree", "InFolder", "NodeType", "Rate"/*, "VersioningMode", "ApprovingMode", "RateAvg", "RateCount"*/ });
        internal static readonly List<string> DeferredFieldNames = new List<string>(new[] { "AllowedChildTypes", "EffectiveAllowedChildTypes" });
        internal static readonly List<string> AllowedMethodNamesWithoutContent = new List<string>(new[] { "PATCH", "PUT", "POST", "DELETE" });

        private static List<JsonConverter> _jsonConverters;
        internal static List<JsonConverter> JsonConverters { get { return _jsonConverters; } }
        private static List<FieldConverter> _fieldConverters;
        internal static List<FieldConverter> FieldConverters { get { return _fieldConverters; } }

        static ODataHandler()
        {
            _jsonConverters = new List<JsonConverter>();
            _jsonConverters.Add(new Newtonsoft.Json.Converters.VersionConverter());
            _fieldConverters = new List<FieldConverter>();
            var fieldConverterTypes = TypeResolver.GetTypesByBaseType(typeof(FieldConverter));
            foreach (var fieldConverterType in fieldConverterTypes)
            {
                var fieldConverter = (FieldConverter)Activator.CreateInstance(fieldConverterType);
                _jsonConverters.Add(fieldConverter);
                _fieldConverters.Add(fieldConverter);
            }

            ActionResolver = new DefaultActionResolver();
        }

        internal static readonly DateTime BASEDATE = new DateTime(1970, 1, 1);
        internal const string REQUESTKEY_MODEL = "models";
        internal const string PROPERTY_ACTIONS = "Actions";
        internal const string PROPERTY_BINARY = "Binary";
        internal const int EXPANSIONLIMIT = Int32.MaxValue;

        public bool IsReusable
        {
            get { return false; }
        }

        internal ODataRequest ODataRequest { get; private set; }

        public void ProcessRequest(HttpContext context)
        {
            ProcessRequest(context, context.Request.HttpMethod.ToUpper(), context.Request.InputStream);
        }
        public void ProcessRequest(HttpContext context, string httpMethod, Stream inputStream)
        {
            ODataRequest odataReq = null;
            ODataFormatter formatter = null;
            var portalContext = (PortalContext)context.Items[PortalContext.CONTEXT_ITEM_KEY];
            try
            {
                Content content;

                odataReq = portalContext.ODataRequest;
                if (odataReq == null)
                {
                    formatter = ODataFormatter.Create("json", portalContext);
                    throw new ODataException("The Request is not an OData request.", ODataExceptionCode.RequestError);
                }

                this.ODataRequest = portalContext.ODataRequest;
                Exception requestError = this.ODataRequest.RequestError;

                formatter = ODataFormatter.Create(portalContext, odataReq);
                if (formatter == null)
                {
                    formatter = ODataFormatter.Create("json", portalContext);
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

                var exists = Node.Exists(odataReq.RepositoryPath);
                if (!exists && !odataReq.IsServiceDocumentRequest && !odataReq.IsMetadataRequest && !AllowedMethodNamesWithoutContent.Contains(httpMethod))
                {
                    ContentNotFound(context, odataReq.RepositoryPath);
                    return;
                }

                JObject model = null;
                switch (httpMethod)
                {
                    case "GET":
                        if (odataReq.IsServiceDocumentRequest)
                        {
                            formatter.WriteServiceDocument(portalContext, odataReq);
                        }
                        else if (odataReq.IsMetadataRequest)
                        {
                            formatter.WriteMetadata(context, odataReq);
                        }
                        else
                        {
                            if (!Node.Exists(odataReq.RepositoryPath))
                                ContentNotFound(context, odataReq.RepositoryPath);
                            else if (odataReq.IsCollection)
                                formatter.WriteChildrenCollection(odataReq.RepositoryPath, portalContext, odataReq);
                            else if (odataReq.IsMemberRequest)
                                formatter.WriteContentProperty(odataReq.RepositoryPath, odataReq.PropertyName,
                                    odataReq.IsRawValueRequest, portalContext, odataReq);
                            else
                                formatter.WriteSingleContent(odataReq.RepositoryPath, portalContext);
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
                                ContentNotFound(context, odataReq.RepositoryPath);
                                return;
                            }

                            ResetContent(content);
                            UpdateContent(content, model, odataReq);
                            formatter.WriteSingleContent(content, portalContext);
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
                                ContentNotFound(context, odataReq.RepositoryPath);
                                return;
                            }

                            UpdateContent(content, model, odataReq);
                            formatter.WriteSingleContent(content, portalContext);
                        }
                        break;
                    case "POST": // invoke an action, create content
                        if (odataReq.IsMemberRequest)
                        {
                            formatter.WriteOperationResult(inputStream, portalContext, odataReq);
                        }
                        else
                        {
                            // parent must exist
                            if (!Node.Exists(odataReq.RepositoryPath))
                            {
                                ContentNotFound(context, odataReq.RepositoryPath);
                                return;
                            }
                            model = Read(inputStream);
                            content = CreateContent(model, odataReq);
                            formatter.WriteSingleContent(content, portalContext);
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
                            if (content != null)
                                content.Delete();
                        }
                        break;
                }
            }
            catch (ContentNotFoundException e)
            {
                var oe = new ODataException(ODataExceptionCode.ResourceNotFound, e);

                formatter.WriteErrorResponse(context, oe);
            }
            catch (ODataException e)
            {
                if (e.HttpStatusCode == 500)
                    SnLog.WriteException(e);

                formatter.WriteErrorResponse(context, e);
            }
            catch (SenseNetSecurityException e)
            {
                // In case of a visitor we should not expose the information that this content actually exists. We return
                // a simple 404 instead to provide exactly the same response as the regular 404, where the content 
                // really does not exist. But do this only if the visitor really does not have permission for the
                // requested content (because security exception could be thrown by an action or something else too).
                if (odataReq != null && User.Current.Id == User.Visitor.Id)
                {
                    var head = NodeHead.Get(odataReq.RepositoryPath);
                    if (head != null && !SecurityHandler.HasPermission(head, PermissionType.Open))
                    {
                        ContentNotFound(context, odataReq.RepositoryPath);
                        return;
                    }
                }

                var oe = new ODataException(ODataExceptionCode.NotSpecified, e);

                SnLog.WriteException(oe);

                formatter.WriteErrorResponse(context, oe);
            }
            catch (InvalidContentActionException ex)
            {
                var oe = new ODataException(ODataExceptionCode.NotSpecified, ex);
                if (ex.Reason != InvalidContentActionReason.NotSpecified)
                    oe.ErrorCode = Enum.GetName(typeof(InvalidContentActionReason), ex.Reason);

                // it is unnecessary to log this exception as this is not a real error
                formatter.WriteErrorResponse(context, oe);
            }
            catch (ContentRepository.Storage.Data.NodeAlreadyExistsException nae)
            {
                var oe = new ODataException(ODataExceptionCode.ContentAlreadyExists, nae);

                formatter.WriteErrorResponse(context, oe);
            }
            catch (System.Threading.ThreadAbortException tae)
            {
                if (!context.Response.IsRequestBeingRedirected)
                {
                    var oe = new ODataException(ODataExceptionCode.RequestError, tae);
                    formatter.WriteErrorResponse(context, oe);
                }
                // specific redirect response so do nothing
            }
            catch (Exception ex)
            {
                var oe = new ODataException(ODataExceptionCode.NotSpecified, ex);

                SnLog.WriteException(oe);

                formatter.WriteErrorResponse(context, oe);
            }
            finally
            {
                context.Response.End();
            }
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

            var jObject = deserialized as JObject;
            if (jObject != null)
                return jObject;
            var jArray = deserialized as JArray;
            if (jArray != null)
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
            if (PortalContext.Current != null)
            {
                var sitePath = PortalContext.Current.Site?.Path;
                if (!string.IsNullOrEmpty(sitePath) &&
                    path.StartsWith(sitePath, StringComparison.OrdinalIgnoreCase) && 
                    !sitePath.Equals(path, StringComparison.OrdinalIgnoreCase))
                    path = path.Substring(sitePath.Length);
            }

            var p = path.LastIndexOf('/');
            if (p < 0)
                return string.Concat("(", path, ")");

            return string.Concat(path.Substring(0, p), "('", path.Substring(p + 1), "')");
        }

        internal static void ContentNotFound(HttpContext context, string path)
        {
            context.Response.Clear();
            context.Response.Status = "404 Not Found";
            context.Response.StatusCode = 404;
        }
        internal static void ContentAlreadyExists(PortalContext portalContext, string path)
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

        private static Exception GetInvalidFormatOptionException()
        {
            var availableFormatterNames = String.Join(", ", ODataFormatter.FormatterTypes.Keys.Select(s => "\"" + s + "\""));
            return new ODataException(SNSR.Exceptions.OData.InvalidFormatOption, ODataExceptionCode.InvalidFormatParameter);
        }

        internal static Content LoadContentByVersionRequest(string path)
        {
            // load content by version if the client provided the version string
            VersionNumber version;
            return PortalContext.Current != null && !string.IsNullOrEmpty(PortalContext.Current.VersionRequest) && VersionNumber.TryParse(PortalContext.Current.VersionRequest, out version)
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
                    contentTypeName = typeof(SenseNet.ContentRepository.File).Name;
                }
                else
                {
                    var allowedContentTypeNames = parent.GetAllowedChildTypeNames().ToArray();
                    contentTypeName = allowedContentTypeNames.FirstOrDefault();
                    if (string.IsNullOrEmpty(contentTypeName))
                        contentTypeName = typeof(SenseNet.ContentRepository.File).Name;
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
                var vp = Node.LoadNode(parentPath) as ISupportsVirtualChildren;
                if (vp != null)
                    content = vp.GetChild(name);
            }

            return content;
        }

        private void ResetContent(Content content)
        {
            // Create "dummy" content
            var newContent = SystemAccount.Execute(() => { return Content.CreateNew(content.ContentType.Name, content.ContentHandler.Parent, null); });

            Aspect[] aspects = null;
            if (content.ContentHandler.HasProperty(GenericContent.ASPECTS))
            {
                // Get aspects
                aspects = content.ContentHandler.GetReferences(GenericContent.ASPECTS).Cast<Aspect>().ToArray();

                // Reset aspect fields
                var gc = content.ContentHandler as GenericContent;
                if (gc != null)
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
        public static void UpdateFields(Content content, JObject model)
        {
            Field field;
            var isNew = content.Id == 0;
            foreach (var prop in model.Properties())
            {
                if (string.IsNullOrEmpty(prop.Name) || prop.Name == "__ContentType" || prop.Name == "__ContentTemplate" || prop.Name == "Type" || prop.Name == "ContentType")
                    continue;

                var hasField = content.Fields.TryGetValue(prop.Name, out field);
                if (!hasField && content.SupportsAddingFieldsOnTheFly && prop.Value is JValue && ((JValue)prop.Value).Value != null)
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
                        var jvalue = prop.Value as JValue;
                        if (jvalue != null)
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

                        var ovalue = prop.Value as JObject;
                        if (ovalue != null)
                        {
                            //TODO: ODATA: setting field when posted value is JObject.
                            // field.SetData(jvalue.Value);
                            continue;
                        }

                        var avalue = prop.Value as JArray;
                        if (avalue != null)
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

                                if (fsetting.AllowMultiple.HasValue && fsetting.AllowMultiple.Value)
                                    field.SetData(nodes);
                                else
                                    field.SetData(nodes.First());

                                continue;
                            }
                            else if (field is ChoiceField)
                            {
                                // ChoiceField expects the value to be of type List<string>
                                var list = new List<string>();
                                foreach (var token in avalue)
                                {
                                    if (token is JValue)
                                        list.Add(((JValue)token).Value.ToString());
                                    else
                                        throw new Exception(string.Format("Token type {0} for field {1} (type {2}) is not supported.", token.GetType().Name, field.Name, field.GetType().Name));
                                }

                                field.SetData(list);
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
            var jvalue = model[name] as JValue;
            if (jvalue != null)
                return (T)jvalue.Value;
            return default(T);
        }

        /// <summary>
        /// Returns an OData path that can request the entity identified by the given path. This path is part of the OData entity request. For example
        /// "/Root/MyFolder/MyDocument.doc" will be transformed to "/Root/MyFolder('MyDocument.doc')"
        /// </summary>
        /// <param name="path">This path will be transformed</param>
        /// <returns>An OData path.</returns>
        public static string GetODataPath(string path)
        {
            if (String.Compare(path, Repository.Root.Path, true) == 0)
                return string.Empty;
            return GetODataPath(RepositoryPath.GetParentPath(path), RepositoryPath.GetFileName(path));
        }
        /// <summary>
        /// Returns an OData path that can request the entity identified by the given path plus name. This path is part of the OData entity request. For example
        /// path = "/Root/MyFolder" and name = "MyDocument.doc" will be transformed to "/Root/MyFolder('MyDocument.doc')"
        /// </summary>
        /// <param name="parentPath">A container path</param>
        /// <param name="name">Content's name in the given container</param>
        /// <returns>An OData path.</returns>
        public static string GetODataPath(string parentPath, string name)
        {
            return string.Format("{0}('{1}')", parentPath, name);
        }
    }

    internal interface IActionResolver
    {
        GenericScenario GetScenario(string name, string parameters);
        IEnumerable<ActionBase> GetActions(Content context, string scenario, string backUri);
        ActionBase GetAction(Content context, string scenario, string actionName, string backUri, object parameters);
    }
    internal class DefaultActionResolver : IActionResolver
    {
        public GenericScenario GetScenario(string name, string parameters)
        {
            return ScenarioManager.GetScenario(name, HttpContext.Current.Request.QueryString.ToString());
        }
        public IEnumerable<ActionBase> GetActions(Content context, string scenario, string backUri)
        {
            return ActionFramework.GetActions(context, scenario, null, backUri);
        }
        public ActionBase GetAction(Content context, string scenario, string actionName, string backUri, object parameters)
        {
            return backUri == null
                ? ActionFramework.GetAction(actionName, context, parameters)
                : ActionFramework.GetAction(actionName, context, backUri, parameters);
        }
    }
}
