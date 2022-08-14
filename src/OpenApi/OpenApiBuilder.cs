using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository.OData;
using SenseNet.OData;
using SenseNet.OpenApi.Model;

namespace SenseNet.OpenApi
{
    public class OpenApiBuilder
    {
        private OpenApiDocument _api;
        public Dictionary<string, ODataOperationInfo> SkippedODataOperations { get; }= new Dictionary<string, ODataOperationInfo>();

        #region Constants
        private static class T
        {
            public static readonly string Object = "object";
            public static readonly string String = "string";
            public static readonly string Boolean = "boolean";
            public static readonly string Integer = "integer";
            public static readonly string Number = "number";
            public static readonly string Array = "array";
        }
        /// <summary>Format = "{sub-type}"</summary>
        private static class F
        {
            /// <summary>Type = "string", Format = "date-time"</summary>
            public static readonly string DateTime = "date-time";
            /// <summary>Type = "string", Format = "time-span"</summary>
            public static readonly string TimeSpan = "time-span";
            /// <summary>Type = "integer", Format = "int32"</summary>
            public static readonly string Int32 = "int32";
            /// <summary>Type = "integer", Format = "int64"</summary>
            public static readonly string Int64 = "int64";
            /// <summary>Type = "string", Format = "ulong"</summary>
            public static readonly string Ulong = "ulong";
            /// <summary>Type = "number", Format = "decimal"</summary>
            public static readonly string Decimal = "decimal";
            /// <summary>Type = "number", Format = "double"</summary>
            public static readonly string Double = "double";
            /// <summary>Type = "number", Format = "float"</summary>
            public static readonly string Float = "float";
            /// <summary>Type = "string", Format = "guid"</summary>
            public static readonly string Guid = "guid";
        }

        private static class Ref
        {
            public static readonly string Parameters = "#/components/parameters/";
            public static readonly string Responses = "#/components/responses/";
            public static readonly string Schemas = "#/components/schemas/";

            public static class P
            {
                public static readonly string EntityName = "EntityName";
                public static readonly string EntityParentPath = "EntityParentPath";
                public static readonly string EntitySetPath = "EntitySetPath";
                public static readonly string Expand = "expand";
                public static readonly string Filter = "filter";
                public static readonly string Format = "format";
                public static readonly string IdInPath = "id-in-path";
                public static readonly string Inlinecount = "inlinecount";
                public static readonly string Metadata = "metadata";
                public static readonly string Metadata_format = "metadata-format";
                public static readonly string Orderby = "orderby";
                public static readonly string Query = "query";
                public static readonly string Select = "select";
                public static readonly string Skip = "skip";
                public static readonly string Top = "top";
                public static readonly string Version = "version";
            }
            public static class R
            {
                public static readonly string Http200_empty = "200-empty";
                public static readonly string Http200_metadata_document = "200-metadata-document";
                public static readonly string Http200_property = "200-property";
                public static readonly string Http200_service_document = "200-service-document";
                public static readonly string Http204 = "204";
                public static readonly string Http403 = "403";
                public static readonly string Http403_creation = "403-creation";
                public static readonly string Http404 = "404";
                public static readonly string Http404_creation = "404-creation";
                public static readonly string Http500_UnknownAction = "500-UnknownAction";
            }
            public static class S
            {
                public static readonly string Content = "Content";
                public static readonly string ContentCreationRequest = "ContentCreationRequest";
                public static readonly string ContentMetadata = "ContentMetadata";
                public static readonly string ContentMetaOperation = "ContentMetaOperation";
                public static readonly string ContentMetaParameter = "ContentMetaParameter";
                public static readonly string ContentMinimalMetadata = "ContentMinimalMetadata";
                public static readonly string ContentModificationRequest = "ContentModificationRequest";
                public static readonly string ContentResetRequest = "ContentResetRequest";
                public static readonly string DeferredReferenceField = "DeferredReferenceField";
                public static readonly string FieldSet = "FieldSet";
                public static readonly string MetadataFormat = "MetadataFormat";
                public static readonly string ODataEntity = "ODataEntity";
                public static readonly string ODataEntitySet = "ODataEntitySet";
                public static readonly string ReferenceField = "ReferenceField";
            }
        }
        #endregion

        public OpenApiBuilder(OpenApiDocument api)
        {
            _api = api;
        }

        public void Add(ODataOperationInfo oDataOp)
        {
            if (_api.Paths.ContainsKey(oDataOp.Url))
            {
                SkippedODataOperations.Add(oDataOp.Url, oDataOp);
                return;
            }

            var pathItem = new PathItem();
            _api.Paths.Add(oDataOp.Url, pathItem);

            var apiOperation = new Operation();
            if (oDataOp.IsAction)
                pathItem.Post = apiOperation;
            else
                pathItem.Get = apiOperation;

            if (oDataOp.IsDeprecated)
                apiOperation.Deprecated = true;

            apiOperation.OperationId = GenerateOperationId(oDataOp);

            SetParameters(apiOperation, oDataOp);

            apiOperation.Responses = new Dictionary<string, Response>();
            var odataReturnType = oDataOp.ReturnValue;
            var apiResponse = CreateResponse(odataReturnType);
            if (apiResponse == null)
                apiOperation.Responses.Add("418",
                    new Response { Description = $"TODO: {odataReturnType.Type}: {odataReturnType.Documentation}" });
            else if (apiResponse.Ref == Ref.Responses + Ref.R.Http204)
                apiOperation.Responses.Add("204", apiResponse);
            else
                apiOperation.Responses.Add("200", apiResponse);
        }
        private string GenerateOperationId(ODataOperationInfo odataOperation)
        {
            var paramList = string.Join("", odataOperation.Parameters.Skip(1).Select(x => "_" + x.Name));
            return $"sn_v1_{(odataOperation.IsAction ? "action" : "function")}_{odataOperation.OperationName}{paramList}";
        }
        private void SetParameters(Operation apiOperation, ODataOperationInfo odataOperation)
        {
            var parameters = new List<Parameter>();

            if (!odataOperation.IsStatic)
            {
                parameters.Add(new Parameter { Ref = Ref.Parameters + Ref.P.EntityParentPath });
                parameters.Add(new Parameter { Ref = Ref.Parameters + Ref.P.EntityName });
            }

            Parameter param;
            foreach (var parameter in odataOperation.Parameters.Skip(1))
                if ((param = CreateParameter(parameter)) != null)
                    parameters.Add(param);

            apiOperation.Parameters = parameters.ToArray();
        }
        public Parameter CreateParameter(OperationParameterInfo odataParameter)
        {
            if (odataParameter.Type == typeof(ODataRequest))
                return null;
            if (odataParameter.Type.Name == "HttpContext")
                return null;

            return new Parameter
            {
                Name = odataParameter.Name,
                In = "query",
                Required = !odataParameter.IsOptional,
                Description = odataParameter.Documentation,
                Example = odataParameter.Example,
                Schema = GetSchema(odataParameter.Type)
            };
        }

        private Response CreateResponse(OperationParameterInfo odataReturnType)
        {
            var schema = GetSchema(odataReturnType.Type);
            if (schema == null)
                return new Response { Ref = Ref.Responses + Ref.R.Http204 };

            AssertEmptyObject(schema);

            return new Response
            {
                Description = odataReturnType.Documentation ?? "...",
                Content = new Dictionary<string, MediaType>
                {
                    {"application/json", new MediaType {Schema = schema}}
                }
            };
        }
        private void AssertEmptyObject(Schema schema)
        {
            if (schema == null)
                return;

            if (schema.Type == T.Object)
            {
                if (schema is DictionarySchema dictionarySchema)
                {
                    AssertEmptyObject(dictionarySchema.AdditionalProperties);
                    return;
                }

                if (schema.Properties == null && schema.Description == null)
                    schema.Description = "TODO: EMPTY OBJECT";
            }
        }

        public Schema GetSchema(Type type)
        {
            string typeName = null;
            var isArray = false;
            string format = null;
            string description = null;
            var referencedSchema = false;
            var isDictionary = false;
            Schema subSchema = null;
            string[] enumValues = null;
            var nullable = false;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                type = type.GetGenericArguments()[0];
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                nullable = true;
                type = type.GetGenericArguments()[0];
            }

            if (type.IsArray)
            {
                isArray = true;
                type = type.GetElementType();
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                typeName = T.Object;
                isDictionary = true;
                var subType = type.GetGenericArguments()[1];
                subSchema = GetSchema(subType);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                typeName = T.Object;
                isDictionary = true;
                var subType = type.GetGenericArguments()[1];
                subSchema = GetSchema(subType);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                type = type.GetGenericArguments()[0];
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    typeName = T.Object;
                    isDictionary = true;
                    var subType = type.GetGenericArguments()[1];
                    subSchema = GetSchema(subType);
                }
                else
                {
                    isArray = true;
                }
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                isArray = true;
                type = type.GetGenericArguments()[0];
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
            {
                isArray = true;
                type = type.GetGenericArguments()[0];
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ODataArray<>))
            {
                isArray = true;
                type = type.GetGenericArguments()[0];
            }


            if (type == typeof(SenseNet.ContentRepository.Content)) { referencedSchema = true; typeName = Ref.S.ODataEntity; }
            else if (type == typeof(IEnumerable<SenseNet.ContentRepository.Content>)) { referencedSchema = true; typeName = Ref.S.ODataEntitySet; }
            else if (type.IsEnum)
            {
                referencedSchema = true;
                typeName = InferSchema(type);
            }
            else if (type == typeof(string)) { typeName = T.String; }
            else if (type == typeof(int)) { typeName = T.Integer; format = F.Int32; }
            else if (type == typeof(long)) { typeName = T.Integer; format = F.Int64; }
            else if (type == typeof(bool)) { typeName = T.Boolean; }
            else if (type == typeof(object)) { typeName = T.Object; }
            else if (type == typeof(ulong)) { typeName = T.String; format = F.Ulong; }
            else if (type == typeof(decimal)) { typeName = T.Number; format = F.Decimal; }
            else if (type == typeof(double)) { typeName = T.Number; format = F.Double; }
            else if (type == typeof(float)) { typeName = T.Number; format = F.Float; }
            else if (type == typeof(DateTime)) { typeName = T.String; format = F.DateTime; }
            else if (type == typeof(TimeSpan)) { typeName = T.String; format = F.TimeSpan; }
            else if (type == typeof(Guid)) { typeName = T.String; format = F.Guid; }
            else if (type == typeof(Version)) { typeName = T.String; format = "version"; }

            else if (type == typeof(Task) || type == typeof(void)) { typeName = "void"; }
            else if (typeName == null)
            {
                if (!type.Namespace.StartsWith("SenseNet"))
                {
                    typeName = T.Object;
                    description = "TODO: Outer object: " + type.FullName;
                }
                else
                {
                    typeName = InferSchema(type);
                    referencedSchema = true;
                }
            }

            string itemType = null;
            if (isArray)
            {
                itemType = typeName;
                typeName = T.Array;
            }

            if (typeName == "void")
                return null;

            var schema = isDictionary ? (Schema)new DictionarySchema() : new ObjectSchema();
            if (referencedSchema && typeName != T.Array)
            {
                schema.Ref = Ref.Schemas + typeName;
            }
            else
            {
                schema.Type = typeName;
                schema.Description = description;
                //schema.Enum = enumValues;
                if(nullable)
                    schema.Nullable = true;

                if (typeName == T.Array)
                {
                    if (referencedSchema)
                        schema.Items = new ObjectSchema { Ref = Ref.Schemas + itemType };
                    else
                        schema.Items = new ObjectSchema { Type = itemType, Format = format };
                    AssertEmptyObject(schema.Items);
                }
                else
                {
                    schema.Format = format;
                }

                if (isDictionary)
                {
                    var dictionarySchema = subSchema == null
                        ? new ObjectSchema { Type = itemType, Format = format }
                        : subSchema;
                    ((DictionarySchema)schema).AdditionalProperties = dictionarySchema;

                    AssertEmptyObject(subSchema);
                }
            }
            AssertEmptyObject(schema);

            return schema;
        }

        private readonly Dictionary<Type, string> _inferredSchemas = new Dictionary<Type, string>
        {
            {typeof(MetadataFormat), "MetadataFormat"}
        };
        private Type[] _wellKnownBaseTypes = new[] {typeof(object), typeof(Enum), typeof(ValueType)};
        private string InferSchema(Type type)
        {
            if(_inferredSchemas.TryGetValue(type, out var schemaName))
                return schemaName;

Trace.WriteLine(">>>> InferSchema: " + type.FullName + " : " + type.BaseType.FullName);

            var baseType = type.BaseType;
            Schema[] baseSchemas = null;
            if (!_wellKnownBaseTypes.Contains(baseType))
            {
                var baseTypeName = InferSchema(baseType);
                baseSchemas = new[] {new ObjectSchema {Ref = Ref.Schemas + baseTypeName}};
            }

            var schemas = _api.Components.Schemas;
            var inferredSchema = type.IsEnum
                ? new ObjectSchema
                {
                    Type = T.String, Enum = GetEnumValues(type)
                }
                : new ObjectSchema
                {
                    Type = T.Object,
                    Properties = InferProperties(type),
                    AllOf = baseSchemas
                };

            if (type.Name == "SinglePermissionInfoResponse")
            {
                int q = 0;
            }

            schemaName = type.Name;
            var suffix = 2;
            while (schemas.ContainsKey(schemaName))
                schemaName = type.Name + suffix++;

            schemas.Add(schemaName, inferredSchema);
            _inferredSchemas.Add(type, schemaName);
            return schemaName;
        }

        private IDictionary<string, Schema> InferProperties(Type type)
        {
            var properties = type.GetProperties();

            var result = new Dictionary<string, Schema>();
            foreach (var property in properties)
            {
                if (property.GetMethod.IsStatic)
                    continue;
                var isInherited = property.DeclaringType != type;
                if (isInherited)
                    continue;

Trace.WriteLine(">>>>     InferProperty: " + property.Name);

                var attr = property.GetCustomAttribute<JsonPropertyAttribute>();
                var name = attr?.PropertyName ?? property.Name;

                var schema = GetSchema(property.PropertyType);
                result.Add(name, schema);
            }

            return result;
        }

        private string[] GetEnumValues(Type type)
        {
            return Enum.GetNames(type).Select(x => ConvertEnumName(x, type)).ToArray();
        }

        private Dictionary<string, string> _camelCaseExceptions = new Dictionary<string, string>
        {
            {"OutputFormat.JSON", "json"},
            {"OutputFormat.VerboseJSON", "verboseJson"},
            {"MetadataFormat.None", "no"},
        };
        private string ConvertEnumName(string name, Type enumType)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var fullName = enumType.Name + "." + name;
            if (_camelCaseExceptions.TryGetValue(fullName, out var word))
                return word;

            var chars = name.ToCharArray();
            chars[0] = char.ToLowerInvariant(chars[0]);
            return new string(chars);
        }
    }
}