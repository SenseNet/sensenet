using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Headers;
using Newtonsoft.Json;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.OData;
using SenseNet.OpenApi.Model;
using Formatting = Newtonsoft.Json.Formatting;

namespace SenseNet.OpenApi
{
    public partial class OpenApiGenerator
    {
        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static string GetOpenApiDocument(Content content, HttpContext httpContext)
        {
            var thisUri = new Uri(httpContext.Request.GetDisplayUrl());
            var thisUrl = $"{thisUri.Scheme}://{thisUri.Authority}";

            var api = new OpenApiGenerator().Generate(thisUrl);
            //var api = CreateOpenApiDocument(thisUrl);

            var settings = new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented};
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
                JsonSerializer.CreateDefault(settings).Serialize(writer, api);

            httpContext.Response.Headers.Add("content-type", "application/json");
            return sb.ToString();
        }

        private OpenApiDocument Generate(string thisUrl)
        {
            var documentationFiles = new Dictionary<Assembly, XmlDocument>();
            var operations = ODataTools.GetOperations();
            var api = CreateOpenApiDocument(thisUrl);
            var apiBuilder = new OpenApiBuilder(api);
            foreach (var operation in operations.Values.SelectMany(x => x))
            {
                var method = operation.Method;
                var oDataOp = BuildODataOperation(operation);
                var documentationElement = GetDocumentationElement(method, documentationFiles);
                oDataOp.ParseDocumentation(documentationElement);
                apiBuilder.Add(oDataOp);
            }

            api.Paths = api.Paths
                .OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Value);
            api.Components.Schemas = api.Components.Schemas
                .OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Value);

            return api;
        }

        private ODataOperationInfo BuildODataOperation(OperationInfo operation)
        {
            var op = new ODataOperationInfo();

            var method = operation.Method;
            op.MethodName = method.Name;
            op.ClassName = method.DeclaringType?.FullName;

            foreach (var attribute in method.GetCustomAttributes())
            {
                if (attribute is ODataOperationAttribute oDataOperationAttribute)
                {
                    op.OperationName = oDataOperationAttribute.OperationName ?? op.MethodName;
                    op.DisplayName = oDataOperationAttribute.DisplayName;
                    op.Description = oDataOperationAttribute.Description;
                    op.Icon = oDataOperationAttribute.Icon;
                }
                if (attribute is ODataAction)
                    op.IsAction = true;
                if (attribute is ContentTypesAttribute contentTypesAttribute)
                    op.ContentTypes.AddRange(contentTypesAttribute.Names);
                if (attribute is AllowedRolesAttribute allowedRolesAttribute)
                    op.AllowedRoles.AddRange(allowedRolesAttribute.Names);
                if (attribute is RequiredPermissionsAttribute requiredPermissionsAttribute)
                    op.RequiredPermissions.AddRange(requiredPermissionsAttribute.Names);
                if (attribute is RequiredPoliciesAttribute requiredPoliciesAttribute)
                    op.RequiredPolicies.AddRange(requiredPoliciesAttribute.Names);
                if (attribute is ScenarioAttribute scenarioAttribute)
                    op.Scenarios.AddRange(scenarioAttribute.Name.Split(',').Select(x=>x.Trim()));
                if (attribute is ObsoleteAttribute)
                    op.IsDeprecated = true;
            }
            op.IsStatic = op.ContentTypes.Count == 1 && op.ContentTypes[0] == "PortalRoot";

            foreach (var parameterInfo in method.GetParameters())
            {
                var param = new OperationParameterInfo
                {
                    Name = parameterInfo.Name,
                    Type = parameterInfo.ParameterType,
                    IsOptional = parameterInfo.IsOptional,
                };

                if (op.Parameters.Count == 0 && param.Type != typeof(Content))
                    op.IsValid = false;

                op.Parameters.Add(param);
            }

            op.IsStatic = op.ContentTypes.Count == 1 && op.ContentTypes[0] == "PortalRoot";

            op.ReturnValue.Type = ((MethodInfo) method).ReturnType;

            op.Normalize();
            //op.ParseDocumentation();

            return op;
        }

        XmlElement GetDocumentationElement(MethodBase method, Dictionary<Assembly, XmlDocument> docFiles)
        {
            if (method.DeclaringType == null)
                return null;

            var asm = method.Module.Assembly;
            if (!docFiles.TryGetValue(asm, out var xml))
            {
                var docFileName = System.IO.Path.ChangeExtension(asm.Location, ".xml");
                try
                {
                    xml = new XmlDocument();
                    xml.Load(docFileName);
                    docFiles.Add(asm, xml);
                }
                catch
                {
                    //UNDONE: Add "missing documentation file" error information to the OpenApiDocument summary.
                    docFiles.Add(asm, null);
                }
            }

            if (xml == null)
                return null;

            var paramTypeNames = method.GetParameters().Select(p => p.ParameterType.FullName).ToArray();
            var paramList = string.Join(",", paramTypeNames);
            XmlElement documentationElement = null;
            if (method.DeclaringType != null)
            {
                var name = $"M:{method.DeclaringType.FullName}.{method.Name}({paramList})";
                var xpath = $"doc/members/member[@name='{name}']";
                documentationElement = (XmlElement)xml.SelectSingleNode(xpath);
            }

            return documentationElement;
        }
    }
}
