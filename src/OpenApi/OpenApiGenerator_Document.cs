using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage;
using SenseNet.OpenApi.Model;
using SenseNet.Tools;

namespace SenseNet.OpenApi
{
    public partial class OpenApiGenerator
    {
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
                //// -----------------------
                //public static readonly string BackupInfo = "BackupInfo";
                //public static readonly string BackupState = "BackupState";
                //public static readonly string BackupResponse = "BackupResponse";
                //public static readonly string CompletionState = "CompletionState";
                //public static readonly string CheckPreviewsResponse = "CheckPreviewsResponse";
                //public static readonly string GetExistingPreviewImagesResponse = "GetExistingPreviewImagesResponse";
                //public static readonly string GetPreviewsFolderResponse = "GetPreviewsFolderResponse";
                //public static readonly string IndexingActivityDependencyState = "IndexingActivityDependencyState";
                //public static readonly string IndexingActivityHistory = "IndexingActivityHistory";
                //public static readonly string IndexingActivityHistoryItem = "IndexingActivityHistoryItem";
                //public static readonly string IndexingActivityQueueState = "IndexingActivityQueueState";
                //public static readonly string IndexingActivitySerializerState = "IndexingActivitySerializerState";
                //public static readonly string IndexingActivityStatus = "IndexingActivityStatus";
                //public static readonly string IndexProperties = "IndexProperties";
                //public static readonly string IndexRebuildLevel = "IndexRebuildLevel";
                //public static readonly string PreviewAvailableResponse = "PreviewAvailableResponse";
                //public static readonly string PreviewStatus = "PreviewStatus";
                //public static readonly string RegeneratePreviewsResponse = "RegeneratePreviewsResponse";
                //public static readonly string SecurityActivityDependencyState = "SecurityActivityDependencyState";
                //public static readonly string SecurityActivityHistory = "SecurityActivityHistory";
                //public static readonly string SecurityActivityHistoryItem = "SecurityActivityHistoryItem";
                //public static readonly string SecurityActivityQueueState = "SecurityActivityQueueState";
                //public static readonly string SecurityActivitySerializerState = "SecurityActivitySerializerState";
                //public static readonly string SecurityConsistencyResult = "SecurityConsistencyResult";
                //public static readonly string SecurityEntityInfo = "SecurityEntityInfo";
                //public static readonly string SecurityMembershipInfo = "SecurityMembershipInfo";
                //public static readonly string SharingLevel = "SharingLevel";
                //public static readonly string SharingMode = "SharingMode";
                //public static readonly string SnTask = "SnTask";
                //public static readonly string SnTaskError = "SnTaskError";
                //public static readonly string SnTaskResult = "SnTaskResult";
                //public static readonly string StoredAceDebugInfo = "StoredAceDebugInfo";
                //// -----------------------
                //public static readonly string IdentityInfo = "IdentityInfo";
                //public static readonly string GroupInfo = "GroupInfo";
                //public static readonly string ChildPermissionInfo = "ChildPermissionInfo";
                //public static readonly string PermissionInfo = "PermissionInfo";
                //public static readonly string PermissionInfoResponse = "PermissionInfoResponse";
                //public static readonly string SinglePermissionInfoResponse = "SinglePermissionInfoResponse";
                //public static readonly string ChildrenPermissionInfoResponse = "ChildrenPermissionInfoResponse";
                //public static readonly string GetPermissionInfoResponse = "GetPermissionInfoResponse";
                //public static readonly string GetSinglePermissionInfoResponse = "GetSinglePermissionInfoResponse";
                //public static readonly string GetChildrenPermissionInfoResponse = "GetChildrenPermissionInfoResponse";
                //// -----------------------
            }
        }
        #endregion

        private static OpenApiDocument CreateOpenApiDocument(string thisUrl)
        {
            var asms = TypeResolver.GetAssemblies();
            var cr = asms.First(x => x.GetName().Name.Equals("SenseNet.ContentRepository", StringComparison.OrdinalIgnoreCase));
            var crVersion = TypeHandler.GetVersion(cr);
            var sc = asms.First(x => x.GetName().Name.Equals("SenseNet.Services.Core", StringComparison.OrdinalIgnoreCase));
            var scVersion = TypeHandler.GetVersion(sc);
            var od = asms.First(x => x.GetName().Name.Equals("SenseNet.OData", StringComparison.OrdinalIgnoreCase));
            var odVersion = TypeHandler.GetVersion(od);

            var infoDesc = "Experimental OpenaApi interface of the sensenet API.\n\n" +
                           "Main components (TODO: delete this section):\n" +
                           $"- sensenet ContentRepository: {crVersion}\n" +
                           $"- sensenet Services Core: {scVersion}\n" +
                           $"- sensenet OData: {odVersion}";

            return new OpenApiDocument
            {
                OpenApi = Version.Parse("3.0.0"),
                Generator = "sensenet OpenApi V1.0",
                Info = new Info
                {
                    Title = "sensenet API",
                    Description = infoDesc,
                    Version = crVersion,
                    License = new License
                    {
                        Name = "GNU General Public License v2.0",
                        Url = "https://github.com/SenseNet/sensenet/blob/master/LICENSE"
                    }
                },
                Servers = new[] {new Server {Url = thisUrl.TrimEnd('/')}},
                Tags = new List<Tag>
                {
                    new Tag{Name="Metadata", Description = "Methods that provide OData metadata documents."},
                    new Tag{Name="Basic-Entity", Description = "Methods that provide CRUD operations of the entities."},
                    new Tag{Name="Basic-Collection", Description = "Methods that provide queries of the entity sets."},
                },
                Paths = new Dictionary<string, PathItem>
                {
                    {"/OData.svc", new PathItem
                        {
                            Get = new Operation
                            {
                                Tags = new []{ "Metadata" },
                                Summary = "Returns OData Service Document.",
                                OperationId = "sn_v1_get_service_document",
                                Responses = new Dictionary<string, Response>
                                {
                                    {"200", new Response{Ref = Ref.Responses + Ref.R.Http200_service_document}}
                                }
                            },
                        }
                    },
                    {"/OData.svc/$metadata", new PathItem
                        {
                            Get = new Operation
                            {
                                Tags = new []{ "Metadata" },
                                Summary = "Returns Service Metadata Document.",
                                Description = "Returns the Service Metadata Document that exposes the data model of the service" +
                                              " in XML or JSON (default: XML). This document is the global (static)" +
                                              " metadata that cannot contain content specific information e.g. expando" +
                                              " (Content List) fields.",
                                OperationId = "sn_v1_get_service_metadata_document",
                                Parameters = new []
                                {
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Metadata_format},
                                },
                                Responses = new Dictionary<string, Response>
                                {
                                    {"403", new Response{Ref = Ref.Responses + Ref.R.Http403}},
                                    {"404", new Response{Ref = Ref.Responses + Ref.R.Http404}},
                                    {"200", new Response{Ref = Ref.Responses + Ref.R.Http200_metadata_document}}
                                }
                            },
                        }
                    },
                    {"/OData.svc/content({id})/$metadata", new PathItem
                        {
                            Parameters = new[]
                            {
                                new Parameter { Ref = Ref.Parameters + Ref.P.IdInPath },
                            },
                            Get = new Operation
                            {
                                Tags = new []{ "Metadata" },
                                Summary = "Returns metadata of the requested entity.",
                                Description = "Returns metadata of the requested entity" +
                                              " in XML or JSON (default: XML). The metadata request of collection and entity" +
                                              " is identical.",
                                OperationId = "sn_v1_get_metadata_of_entity_by_id",
                                Parameters = new []
                                {
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Metadata_format},
                                },
                                Responses = new Dictionary<string, Response>
                                {
                                    {"403", new Response{Ref = Ref.Responses + Ref.R.Http403}},
                                    {"404", new Response{Ref = Ref.Responses + Ref.R.Http404}},
                                    {"200", new Response{Ref = Ref.Responses + Ref.R.Http200_metadata_document}}
                                }
                            },
                        }
                    },
                    {"/OData.svc/{_path}/$metadata", new PathItem
                        {
                            Parameters = new[]
                            {
                                new Parameter { Ref = Ref.Parameters + Ref.P.EntitySetPath }
                            },
                            Get = new Operation
                            {
                                Tags = new []{ "Metadata" },
                                Summary = "Returns metadata of the requested entity.",
                                Description = "Returns metadata of the requested entity (not the collection)" +
                                              " in XML or JSON (default: XML). The metadata request of collection and entity" +
                                              " is identical.",
                                OperationId = "sn_v1_get_metadata_of_collection",
                                Parameters = new []
                                {
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Metadata_format},
                                },
                                Responses = new Dictionary<string, Response>
                                {
                                    {"403", new Response{Ref = Ref.Responses + Ref.R.Http403}},
                                    {"404", new Response{Ref = Ref.Responses + Ref.R.Http404}},
                                    {"200", new Response{Ref = Ref.Responses + Ref.R.Http200_metadata_document}}
                                }
                            },
                        }
                    },
                    {"/OData.svc/{_path}('{_name}')/$metadata", new PathItem
                        {
                            Parameters = new[]
                            {
                                new Parameter { Ref = Ref.Parameters + Ref.P.EntitySetPath },
                                new Parameter { Ref = Ref.Parameters + Ref.P.EntityName }
                            },
                            Get = new Operation
                            {
                                Tags = new []{ "Metadata" },
                                Summary = "Returns metadata of the requested entity.",
                                Description = "Returns metadata of the requested entity" +
                                              " in XML or JSON (default: XML). The metadata request of collection and entity" +
                                              " is identical.",
                                OperationId = "sn_v1_get_metadata_of_entity_by_path",
                                Parameters = new []
                                {
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Metadata_format},
                                },
                                Responses = new Dictionary<string, Response>
                                {
                                    {"403", new Response{Ref = Ref.Responses + Ref.R.Http403}},
                                    {"404", new Response{Ref = Ref.Responses + Ref.R.Http404}},
                                    {"200", new Response{Ref = Ref.Responses + Ref.R.Http200_metadata_document}}
                                }
                            },
                        }
                    },
                    {"/OData.svc/{_path}", new PathItem
                        {
                            Parameters = new[]
                            {
                                new Parameter { Ref = Ref.Parameters + Ref.P.EntitySetPath }
                            },
                            Get = new Operation
                            {
                                Tags = new []{ "Basic-Collection" },
                                Summary = "Returns child elements under the {_path}",
                                Description = "???",
                                OperationId = "sn_v1_get_children",
                                Parameters = new []
                                {
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Metadata},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Top},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Skip},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Inlinecount},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Orderby},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Expand},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Select},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Filter},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Format},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Version},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Query},
                                },
                                Responses = new Dictionary<string, Response>
                                {
                                    {"403", new Response{Ref = Ref.Responses + Ref.R.Http403}},
                                    {"404", new Response{Ref = Ref.Responses + Ref.R.Http404}},
                                    {"200", new Response
                                        {
                                            Description = "Payload.",
                                            Content = new Dictionary<string, MediaType>
                                            {
                                                {"application/json", new MediaType
                                                    {
                                                        Schema = new ObjectSchema{ Ref = Ref.Schemas + Ref.S.ODataEntitySet }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                        }
                    },
                    {"/OData.svc/{_path}/$count", new PathItem
                        {
                            Parameters = new[]
                            {
                                new Parameter { Ref = Ref.Parameters + Ref.P.EntitySetPath }
                            },
                            Get = new Operation
                            {
                                Tags = new []{ "Basic-Collection" },
                                Summary = "Returns count of children.",
                                Description = "Returns count of child elements under the {_path}.",
                                OperationId = "sn_v1_get_children_count",
                                Responses = new Dictionary<string, Response>
                                {
                                    {"403", new Response{Ref = Ref.Responses + Ref.R.Http403}},
                                    {"404", new Response{Ref = Ref.Responses + Ref.R.Http404}},
                                    {"200", new Response
                                        {
                                            Description = "Count of the requested collection in plain text.",
                                            Content = new Dictionary<string, MediaType>
                                            {
                                                {"text/plain", new MediaType {Schema = new ObjectSchema{ Type = T.String }}}
                                            }
                                        }
                                    }
                                }
                            },
                        }
                    },
                    {"/OData.svc/content({id})", new PathItem
                        {
                            Parameters = new[]
                            {
                                new Parameter { Ref = Ref.Parameters + Ref.P.IdInPath },
                            },
                            Get = new Operation
                            {
                                Tags = new []{ "Basic-Entity" },
                                Summary = "Gets a single content by id.",
                                Description = "Returns the requested Content if it is permitted for the current user. The collection of properties depends from the Content's ContentType and the value of the '$select' parameter.",
                                OperationId = "sn_v1_get_entity_by_id",
                                Parameters = new []
                                {
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Metadata},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Expand},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Select},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Format},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Version},
                                },
                                Responses = new Dictionary<string, Response>
                                {
                                    {"403", new Response{Ref = Ref.Responses + Ref.R.Http403}},
                                    {"404", new Response{Ref = Ref.Responses + Ref.R.Http404}},
                                    {"200", new Response
                                        {
                                            Description = "Payload.",
                                            Content = new Dictionary<string, MediaType>
                                            {
                                                {"application/json", new MediaType
                                                    {
                                                        Schema = new ObjectSchema{ Ref = Ref.Schemas + Ref.S.ODataEntity }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            Post = new Operation
                            {
                                Tags = new []{ "Basic-Entity" },
                                Summary = "Creates a new child content.",
                                Description = "???",
                                OperationId = "sn_v1_create_content_under_entity_by_id",
                                Parameters = new []
                                {
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Metadata},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Expand},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Select},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Format},
                                },
                                RequestBody = new RequestBody
                                {
                                    Description = "Data for creation. Only the `__ContentType` is required. After creation, the unspecified properties are assigned their default values.",
                                    Required = true,
                                    Content = new Dictionary<string, MediaType>
                                    {
                                        {"application/json", new MediaType
                                            {
                                                Schema = new ObjectSchema{Ref = Ref.Schemas + Ref.S.ContentCreationRequest}
                                            }
                                        }
                                    }
                                },
                                Responses = new Dictionary<string, Response>
                                {
                                    {"403", new Response{Ref = Ref.Responses + Ref.R.Http403_creation}},
                                    {"404", new Response{Ref = Ref.Responses + Ref.R.Http404_creation}},
                                    {"200", new Response
                                        {
                                            Description = "The newly created content according to the given parameters (metadata, $expand, $select, $format).",
                                            Content = new Dictionary<string, MediaType>
                                            {
                                                {"application/json", new MediaType
                                                    {
                                                        Schema = new ObjectSchema{ Ref = Ref.Schemas + Ref.S.ODataEntity }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            Put = new Operation
                            {
                                Tags = new []{ "Basic-Entity" },
                                Summary = "Resets the content.",
                                Description = "Modifies the specified properties of the content." +
                                              " All other properties take their default values.",
                                OperationId = "sn_v1_reset_content_by_id",
                                Parameters = new []
                                {
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Metadata},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Expand},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Select},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Format},
                                },
                                RequestBody = new RequestBody
                                {
                                    Description = "???",
                                    Required = true,
                                    Content = new Dictionary<string, MediaType>
                                    {
                                        {"application/json", new MediaType
                                            {
                                                Schema = new ObjectSchema{Ref = Ref.Schemas + Ref.S.ContentModificationRequest}
                                            }
                                        }
                                    }
                                },
                                Responses = new Dictionary<string, Response>
                                {
                                    {"403", new Response{Ref = Ref.Responses + Ref.R.Http403}},
                                    {"404", new Response{Ref = Ref.Responses + Ref.R.Http404}},
                                    {"200", new Response
                                        {
                                            Description = "The modified content according to the given parameters (metadata, $expand, $select, $format).",
                                            Content = new Dictionary<string, MediaType>
                                            {
                                                {"application/json", new MediaType
                                                    {
                                                        Schema = new ObjectSchema{ Ref = Ref.Schemas + Ref.S.ODataEntity }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            Patch = new Operation
                            {
                                Tags = new []{ "Basic-Entity" },
                                Summary = "Modifies the content.",
                                Description = "Modifies the desired properties of the content.",
                                OperationId = "sn_v1_patch_content_by_id",
                                Parameters = new []
                                {
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Metadata},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Expand},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Select},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Format},
                                },
                                RequestBody = new RequestBody
                                {
                                    Description = "???",
                                    Required = true,
                                    Content = new Dictionary<string, MediaType>
                                    {
                                        {"application/json", new MediaType
                                            {
                                                Schema = new ObjectSchema{Ref = Ref.Schemas + Ref.S.ContentModificationRequest}
                                            }
                                        }
                                    }
                                },
                                Responses = new Dictionary<string, Response>
                                {
                                    {"403", new Response{Ref = Ref.Responses + Ref.R.Http403}},
                                    {"404", new Response{Ref = Ref.Responses + Ref.R.Http404}},
                                    {"200", new Response
                                        {
                                            Description = "The modified content according to the given parameters (metadata, $expand, $select, $format).",
                                            Content = new Dictionary<string, MediaType>
                                            {
                                                {"application/json", new MediaType
                                                    {
                                                        Schema = new ObjectSchema{ Ref = Ref.Schemas + Ref.S.ODataEntity }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            Delete = new Operation
                            {
                                Tags = new []{ "Basic-Entity" },
                                Summary = "Deletes the content.",
                                Description = "The content will be deleted or trashed.",
                                OperationId = "sn_v1_delete_entity_by_id",
                                Parameters = new Parameter[]
                                {
                                    new Parameter
                                    {
                                        Name = "permanent",
                                        In = "query",
                                        Description = "False if the content will be moved to the Trash.",
                                        Required = false,
                                        Schema = new ObjectSchema {Type = T.Boolean}
                                    }
                                },
                                Responses = new Dictionary<string, Response>
                                {
                                    {"200", new Response{Ref = Ref.Responses + Ref.R.Http200_empty}},
                                }
                            },
                        }
                    },
                    {"/OData.svc/content({id})/{property}", new PathItem
                        {
                            Parameters = new[]
                            {
                                new Parameter { Ref = Ref.Parameters + Ref.P.IdInPath },
                                new Parameter
                                {
                                    Name = "property",
                                    In = "path",
                                    Required = true,
                                    AllowEmptyValue = false,
                                    Description = "Name of the requested property.",
                                    Schema = new ObjectSchema {Type = T.String}
                                },
                            },
                            Get = new Operation
                            {
                                Tags = new []{ "Basic-Entity" },
                                Summary = "Gets a property of the given Content.",
                                Description = "Returns the requested property of the given Content",
                                OperationId = "sn_v1_get_property_by_id",
                                Responses = new Dictionary<string, Response>
                                {
                                    {"403", new Response{Ref = Ref.Responses + Ref.R.Http403}},
                                    {"404", new Response{Ref = Ref.Responses + Ref.R.Http404}},
                                    {"200", new Response{Ref = Ref.Responses + Ref.R.Http200_property}},
                                }
                            },
                        }
                    },
                    {"/OData.svc/content({id})/{property}/$value", new PathItem
                        {
                            Parameters = new[]
                            {
                                new Parameter { Ref = Ref.Parameters + Ref.P.IdInPath },
                                new Parameter
                                {
                                    Name = "property",
                                    In = "path",
                                    Required = true,
                                    AllowEmptyValue = false,
                                    Description = "Name of the requested property.",
                                    Schema = new ObjectSchema {Type = T.String}
                                },
                            },
                            Get = new Operation
                            {
                                Tags = new []{ "Basic-Entity" },
                                Summary = "Gets a property value of the given Content.",
                                Description = "Returns the requested property value of the given Content",
                                OperationId = "sn_v1_get_property_value_by_id",
                                Responses = new Dictionary<string, Response>
                                {
                                    {"403", new Response{Ref = Ref.Responses + Ref.R.Http403}},
                                    {"404", new Response{Ref = Ref.Responses + Ref.R.Http404}},
                                    {"200", new Response
                                        {
                                            Description = "Property value response.",
                                            Content = new Dictionary<string, MediaType>
                                            {
                                                {"text/plain", new MediaType {Schema = new ObjectSchema{ Type = T.String }}}
                                            }
                                        }
                                    }
                                }
                            },
                        }
                    },
                    {"/OData.svc/{_path}('{_name}')", new PathItem
                        {
                            Parameters = new[]
                            {
                                new Parameter { Ref = Ref.Parameters + Ref.P.EntityParentPath },
                                new Parameter { Ref = Ref.Parameters + Ref.P.EntityName }
                            },
                            Get = new Operation
                            {
                                Tags = new []{ "Basic-Entity" },
                                Summary = "Gets a single content by path.",
                                Description = "Returns the requested Content if it is permitted for the current user. The collection of properties depends from the Content's ContentType and the value of the '$select' parameter.",
                                OperationId = "sn_v1_get_entity_by_path",
                                Parameters = new []
                                {
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Metadata},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Expand},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Select},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Format},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Version},
                                },
                                Responses = new Dictionary<string, Response>
                                {
                                    {"403", new Response{Ref = Ref.Responses + Ref.R.Http403}},
                                    {"404", new Response{Ref = Ref.Responses + Ref.R.Http404}},
                                    {"200", new Response
                                        {
                                            Description = "Payload.",
                                            Content = new Dictionary<string, MediaType>
                                            {
                                                {"application/json", new MediaType
                                                    {
                                                        Schema = new ObjectSchema{ Ref = Ref.Schemas + Ref.S.ODataEntity }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            Post = new Operation
                            {
                                Tags = new []{ "Basic-Entity" },
                                Summary = "Creates a new child content.",
                                Description = "???",
                                OperationId = "sn_v1_create_content_under_entity_by_path",
                                Parameters = new []
                                {
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Metadata},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Expand},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Select},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Format},
                                },
                                RequestBody = new RequestBody
                                {
                                    Description = "???",
                                    Required = true,
                                    Content = new Dictionary<string, MediaType>
                                    {
                                        {"application/json", new MediaType
                                            {
                                                Schema = new ObjectSchema{Ref = Ref.Schemas + Ref.S.ContentCreationRequest}
                                            }
                                        }
                                    }
                                },
                                Responses = new Dictionary<string, Response>
                                {
                                    {"403", new Response{Ref = Ref.Responses + Ref.R.Http403_creation}},
                                    {"404", new Response{Ref = Ref.Responses + Ref.R.Http404_creation}},
                                    {"200", new Response
                                        {
                                            Description = "The newly created content according to the given parameters (metadata, $expand, $select, $format).",
                                            Content = new Dictionary<string, MediaType>
                                            {
                                                {"application/json", new MediaType
                                                    {
                                                        Schema = new ObjectSchema{ Ref = Ref.Schemas + Ref.S.ODataEntity }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            Put = new Operation
                            {
                                Tags = new []{ "Basic-Entity" },
                                Summary = "Resets the content.",
                                Description = "Modifies the specified properties of the content." +
                                              " All other properties take their default values.",
                                OperationId = "sn_v1_reset_content_by_path",
                                Parameters = new []
                                {
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Metadata},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Expand},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Select},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Format},
                                },
                                RequestBody = new RequestBody
                                {
                                    Description = "???",
                                    Required = true,
                                    Content = new Dictionary<string, MediaType>
                                    {
                                        {"application/json", new MediaType
                                            {
                                                Schema = new ObjectSchema{Ref = Ref.Schemas + Ref.S.ContentModificationRequest}
                                            }
                                        }
                                    }
                                },
                                Responses = new Dictionary<string, Response>
                                {
                                    {"403", new Response{Ref = Ref.Responses + Ref.R.Http403}},
                                    {"404", new Response{Ref = Ref.Responses + Ref.R.Http404}},
                                    {"200", new Response
                                        {
                                            Description = "The modified content according to the given parameters (metadata, $expand, $select, $format).",
                                            Content = new Dictionary<string, MediaType>
                                            {
                                                {"application/json", new MediaType
                                                    {
                                                        Schema = new ObjectSchema{ Ref = Ref.Schemas + Ref.S.ODataEntity }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            Patch = new Operation
                            {
                                Tags = new []{ "Basic-Entity" },
                                Summary = "Modifies the content.",
                                Description = "Modifies the desired properties of the content.",
                                OperationId = "sn_v1_patch_content_by_path",
                                Parameters = new []
                                {
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Metadata},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Expand},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Select},
                                    new Parameter{Ref = Ref.Parameters + Ref.P.Format},
                                },
                                RequestBody = new RequestBody
                                {
                                    Description = "???",
                                    Required = true,
                                    Content = new Dictionary<string, MediaType>
                                    {
                                        {"application/json", new MediaType
                                            {
                                                Schema = new ObjectSchema{Ref = Ref.Schemas + Ref.S.ContentModificationRequest}
                                            }
                                        }
                                    }
                                },
                                Responses = new Dictionary<string, Response>
                                {
                                    {"403", new Response{Ref = Ref.Responses + Ref.R.Http403}},
                                    {"404", new Response{Ref = Ref.Responses + Ref.R.Http404}},
                                    {"200", new Response
                                        {
                                            Description = "The modified content according to the given parameters (metadata, $expand, $select, $format).",
                                            Content = new Dictionary<string, MediaType>
                                            {
                                                {"application/json", new MediaType
                                                    {
                                                        Schema = new ObjectSchema{ Ref = Ref.Schemas + Ref.S.ODataEntity }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            Delete = new Operation
                            {
                                Tags = new []{ "Basic-Entity" },
                                Summary = "Deletes the content.",
                                Description = "The content will be deleted or trashed.",
                                OperationId = "sn_v1_delete_entity_by_path",
                                Parameters = new Parameter[]
                                {
                                    new Parameter
                                    {
                                        Name = "permanent",
                                        In = "query",
                                        Description = "False if the content will be moved to the Trash.",
                                        Required = false,
                                        Schema = new ObjectSchema {Type = T.Boolean}
                                    }
                                },
                                Responses = new Dictionary<string, Response>
                                {
                                    {"200", new Response{Ref = Ref.Responses + Ref.R.Http204}},
                                }
                            }
                        }
                    },
                    {"/OData.svc/{_path}('{_name}')/{property}", new PathItem
                        {
                            Parameters = new[]
                            {
                                new Parameter { Ref = Ref.Parameters + Ref.P.EntityParentPath },
                                new Parameter { Ref = Ref.Parameters + Ref.P.EntityName },
                                new Parameter
                                {
                                    Name = "property",
                                    In = "path",
                                    Required = true,
                                    AllowEmptyValue = false,
                                    Description = "Name of the requested property.",
                                    Schema = new ObjectSchema {Type = T.String}
                                },
                            },
                            Get = new Operation
                            {
                                Tags = new []{ "Basic-Entity" },
                                Summary = "Gets a property of the given Content.",
                                Description = "Returns the requested property of the given Content",
                                OperationId = "sn_v1_get_property_by_path",
                                Responses = new Dictionary<string, Response>
                                {
                                    {"403", new Response{Ref = Ref.Responses + Ref.R.Http403}},
                                    {"404", new Response{Ref = Ref.Responses + Ref.R.Http404}},
                                    {"200", new Response{Ref = Ref.Responses + Ref.R.Http200_property}},
                                }
                            },
                        }
                    },
                    {"/OData.svc/{_path}('{_name}')/{property}/$value", new PathItem
                        {
                            Parameters = new[]
                            {
                                new Parameter { Ref = Ref.Parameters + Ref.P.EntityParentPath },
                                new Parameter { Ref = Ref.Parameters + Ref.P.EntityName },
                                new Parameter
                                {
                                    Name = "property",
                                    In = "path",
                                    Required = true,
                                    AllowEmptyValue = false,
                                    Description = "Name of the requested property.",
                                    Schema = new ObjectSchema {Type = T.String}
                                },
                            },
                            Get = new Operation
                            {
                                Tags = new []{ "Basic-Entity" },
                                Summary = "Gets a property value of the given Content.",
                                Description = "Returns the requested property value of the given Content. The property should be exist otherwise the response will be `UnknownAction` error.",
                                OperationId = "sn_v1_get_property_value_by_path",
                                Responses = new Dictionary<string, Response>
                                {
                                    {"403", new Response{Ref = Ref.Responses + Ref.R.Http403}},
                                    {"404", new Response{Ref = Ref.Responses + Ref.R.Http404}},
                                    {"500", new Response{Ref = Ref.Responses + Ref.R.Http500_UnknownAction}},
                                    {"200", new Response
                                        {
                                            Description = "Property value response.",
                                            Content = new Dictionary<string, MediaType>
                                            {
                                                {"text/plain", new MediaType {Schema = new ObjectSchema{ Type = T.String }}}
                                            }
                                        }
                                    }
                                }
                            },
                        }
                    },
                },
                Components = new Components
                {
                    Parameters = new Dictionary<string, Parameter>
                    {
                        {Ref.P.IdInPath, new Parameter
                            {
                                Name = "id",
                                In = "path",
                                Required = true,
                                AllowEmptyValue = false,
                                Description = "Unique identifier of the content.",
                                Schema = new ObjectSchema
                                {
                                    Type = T.Integer,
                                    Format = F.Int32,
                                    Nullable = false,
                                    Minimum = 1,
                                }
                            }
                        },
                        {Ref.P.EntitySetPath, new Parameter
                            {
                                Name = "_path",
                                In = "path",
                                Required = true,
                                AllowEmptyValue = false,
                                Description = "Path of the content. Leading slash is omitted.",
                                Schema = new ObjectSchema { Type = T.String }
                            }
                        },
                        {Ref.P.EntityParentPath, new Parameter
                            {
                                Name = "_path",
                                In = "path",
                                Required = true,
                                AllowEmptyValue = true,
                                Description = "Path of the container content. Leading slash is omitted. If empty, the {name} should be 'Root'.",
                                Schema = new ObjectSchema { Type = T.String }
                            }
                        },
                        {Ref.P.EntityName, new Parameter
                            {
                                Name = "_name",
                                In = "path",
                                Required = true,
                                AllowEmptyValue = false,
                                Description = "Unique local name under the container content. Cannot be empty.",
                                Schema = new ObjectSchema { Type = T.String }
                            }
                        },
                        {Ref.P.Metadata, new Parameter
                            {
                                Name = "metadata",
                                In = "query",
                                Description = "Controls the metadata format.",
                                Schema = new ObjectSchema { Ref = Ref.Schemas + Ref.S.MetadataFormat }
                            }
                        },
                        {Ref.P.Top, new Parameter
                            {
                                Name = "$top",
                                In = "query",
                                Description = "Limits the collection result.",
                                Schema = new ObjectSchema { Type = T.Integer, Format = F.Int32}
                            }
                        },
                        {Ref.P.Skip, new Parameter
                            {
                                Name = "$skip",
                                In = "query",
                                Description = "Hides the first given number of elements from the result",
                                Schema = new ObjectSchema { Type = T.Integer, Format = F.Int32 }
                            }
                        },
                        {Ref.P.Inlinecount, new Parameter
                            {
                                Name = "$inlinecount",
                                In = "query",
                                Description = "Controls the `__count` property's value that can be found in every collection response. Its valid values are: `allpages` and none (other value causes an error, default value is `none`).\n\n- **allpages**: count of the whole set (filter, top, skip options are ignored)\n- **none**: result shows the actual count of items",
                                Schema = new ObjectSchema
                                {
                                    Type = T.String,
                                    Enum = new [] { "none", "allpages" },
                                    //EnumNames = new [] { "None", "AllPages" }
                                }
                            }
                        },
                        {Ref.P.Orderby, new Parameter
                            {
                                Name = "$orderby",
                                In = "query",
                                Description = "Comma separated list of the collection sorting criteria. Every criteria contains" +
                                              " one or two words: first is the property name, second is the direction that can be" +
                                              " 'asc' or 'desc'. The 'asc' can be omitted because it is the default direction.\n\n" +
                                              " Some examples: `$sort=Index`, `$sort=Name asc`, `$sort=Name,ModificationDate desc`.",
                                Schema = new ObjectSchema
                                {
                                    Type = T.Array,
                                    UniqueItems = true,
                                    Items = new ObjectSchema
                                    {
                                        Type = T.String
                                    }
                                }
                            }
                        },
                        {Ref.P.Expand, new Parameter
                            {
                                Name = "$expand",
                                In = "query",
                                Description = "Indicates that a related item should be represented inline in the response with full" +
                                              " content instead of as a simple link. In our case this means that any Reference Field" +
                                              " can be expanded allowing you to get metadata of a content and one or more related" +
                                              " content with a single HTTP request.\n\nThe value provided in the `$expand` option is" +
                                              " a comma separated list of navigational properties (in sensenet these are reference" +
                                              " fields). The `$expand` option works with a collection or with a single content" +
                                              " request as well. You may indicate that you want to expand one or more fields (e.g." +
                                              " `ModifiedBy` and `CreatedBy` at the same time).\n\nThe fields of the expanded content" +
                                              " can even be expanded by specifying a 'field name chain', separated by slashes (e.g." +
                                              " `CreatedBy/CreatedBy`).\n\nIt is possible to specify the list of fields the response" +
                                              " should contain. This works with expanded properties as well: you may specify which" +
                                              " fields of the expanded content should be added to the response by providing a 'field" +
                                              " name chain', separated by slashes (e.g. `$select=CreatedBy/DisplayName`).\n\n" +
                                              "Example (expand and select): $expand=Manager&$select=Path,DisplayName,Manager/DisplayName",
                                Explode = false,
                                Schema = new ObjectSchema
                                {
                                    Type = T.Array,
                                    UniqueItems = true,
                                    Items = new ObjectSchema
                                    {
                                        Type = T.String
                                    }
                                }
                            }
                        },
                        {Ref.P.Select, new Parameter
                            {
                                Name = "$select",
                                In = "query",
                                Description = "Specifies the displayed properties in a comma separated list of the property names." +
                                              " Property names are case sensitive. Without this option the result will contain all" +
                                              " available properties. In case of one entry, the available property set is the" +
                                              " entry's all fields. In case of collection all fields of the available content in the" +
                                              " collection.\n\n*Limitation*: A select clause can be only a property name. Expressions" +
                                              " in select clauses are not yet supported in sensenet.\n\n" +
                                              "Example: `$select=Id,Path,DisplayName,Type`",
                                Explode = false,
                                Schema = new ObjectSchema
                                {
                                    Type = T.Array,
                                    UniqueItems = true,
                                    Items = new ObjectSchema
                                    {
                                        Type = T.String
                                    }
                                }
                            }
                        },
                        {Ref.P.Filter, new Parameter
                            {
                                Name = "$filter",
                                In = "query",
                                Description = "Defines a subset of the entries from a specified collection. The filter expression" +
                                              " can contain global functions and operations according to the" +
                                              " [OData](https://www.odata.org/documentation/odata-version-2-0/uri-conventions/)" +
                                              " standard.\n\nThe filtering only works on children. The sensenet repository" +
                                              " is tree-based and not table based. So our collections are not only tables as typed" +
                                              " collections rather children of a tree node. As the collection request returns a" +
                                              " container's children, `$filter` option is working only on children items.\n\nFiltering" +
                                              " does not work on reference properties. In sensenet filtering for reference fields" +
                                              " is not available so this type of filters will be skipped. Do not use relational" +
                                              " database specific operations in a filter. Since the search engine of sensenet is" +
                                              " based on Lucene.NET, it is text based and not a relational one. Because of that you" +
                                              " cannot use two or more fields in one logical operation, and cannot perform operations" +
                                              " on fields. For example: comparing values of two fields or executing field operations" +
                                              " in terms.\n\nExamples:\n\n" +
                                              "- Items that have an Index greater than 11: `$filter=Index gt 11`\n" +
                                              "- Items when the Description field contains the given string: `$filter=substringof('Lorem', Description) eq true`\n" +
                                              "- Items when the Name field starts with the given string: `$filter=startswith(Name, 'Document') eq true`\n" +
                                              "- Items when the Name field ends with the given string: `$filter=endswith(Name, 'Library') eq true`\n" +
                                              "- Items that were uploaded after the given date: `$filter=CreationDate gt datetime'2019-03-26T03:55:00'`\n" +
                                              "- Items with an exact type skipping the contents with one of the inherited types: `$filter=ContentType eq 'Folder'`\n" +
                                              "- Items with the given type and all of its inherited ones: `$filter=isof('Folder')`\n",
                                Explode = false,
                                Schema = new ObjectSchema
                                {
                                    Type = T.String,
                                }
                            }
                        },
                        {Ref.P.Format, new Parameter
                            {
                                Name = "$format",
                                In = "query",
                                Description = "Defines the output format. Default: `verbosejson`. The 'json' and 'verbosejson'" +
                                              " are functionally identical in this version. Note that the `table` format is not" +
                                              " standard and is only used for development purposes. The standard `xml` and `atom`" +
                                              " cannot be use here in this version.",
                                Schema = new ObjectSchema
                                {
                                    Type = T.String,
                                    Enum = new [] { "none", "json", "verbosejson", "atom", "xml", "table" },
                                    //EnumNames = new [] { "None", "JSON", "VerboseJSON", "Atom", "Xml", "Table" }
                                }
                            }
                        },
                        {Ref.P.Metadata_format, new Parameter
                            {
                                Name = "$format",
                                In = "query",
                                Description = "Defines the output format. Default: `xml`. The 'json' and 'verbosejson'" +
                                              " are functionally identical in this version.",
                                Schema = new ObjectSchema
                                {
                                    Type = T.String,
                                    Enum = new [] { "none", "json", "verbosejson", "xml" },
                                    //EnumNames = new [] { "None", "JSON", "VerboseJSON", "Xml" }
                                }
                            }
                        },
                        {Ref.P.Version, new Parameter
                            {
                                Name = "version",
                                Description = "???\n\nValid examples: '1.2', 'v1.2', 'V1.2', '1.2.D', 'lastmajor', 'lastminor'.",
                                In = "query",
                                Schema = new ObjectSchema
                                {
                                    Type = T.String,
                                }
                            }
                        },
                        {Ref.P.Query, new Parameter
                            {
                                Name = "query",
                                Description = "Gets filtered collection of entities with Content Query (without '$' prefix). The scope of the query is the subtree with requested entity as a root.\n\n*Performance considerations*:\n\nDo not forget that querying on huge collections may impact server performance. Always use limiters when using the queries in such cases. If you use the custom `query` option use the CQL limiters and sorting in the `query` option (.TOP .SKIP) instead of using the OData ones ($top, $skip, $orderby).",
                                In = "query",
                                Schema = new ObjectSchema
                                {
                                    Type = T.String,
                                }
                            }
                        },
                    },
                    Responses = new Dictionary<string, Response>
                    {
                        {Ref.R.Http200_empty, new Response {Description = "The operation completed successfully, but there is no returned content."}},
                        {Ref.R.Http204, new Response {Description = "The operation completed successfully, but there is no returned content."}},
                        {Ref.R.Http403_creation, new Response {Description = "The user has not enough permission to content creation under the given parent content."}},
                        {Ref.R.Http403, new Response {Description = "The user has not enough permission to requested operation on the given content."}},
                        {Ref.R.Http404_creation, new Response {Description = "The given parent content does not exist or not permitted."}},
                        {Ref.R.Http404, new Response {Description = "The given content does not exist or not permitted."}},
                        {Ref.R.Http500_UnknownAction, new Response {Description = "Operation not found error."}},
                        {Ref.R.Http200_service_document, new Response
                            {
                                Description = "Metadata document",
                                Content = new Dictionary<string, MediaType>
                                {
                                    {"application/json", new MediaType
                                        {
                                            Schema = new ObjectSchema
                                            {
                                                Type = T.Object,
                                                Properties = new Dictionary<string, Schema>
                                                {
                                                    {"d", new ObjectSchema
                                                        {
                                                            Type = T.Object,
                                                            Properties = new Dictionary<string, Schema>
                                                            {
                                                                {"EntitySets", new ObjectSchema
                                                                    {
                                                                        Type = T.Array,
                                                                        Items = new ObjectSchema{Type = T.String}
                                                                    }
                                                                },
                                                            }
                                                        }
                                                    },
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        {Ref.R.Http200_metadata_document, new Response
                            {
                                Description = "Metadata document",
                                Content = new Dictionary<string, MediaType>
                                {
                                    {"application/xml", new MediaType
                                        {
                                            Schema = new ObjectSchema
                                            {
                                                Type = T.Object,
                                                Xml = new Xml
                                                {
                                                    Namespace = "http://schemas.microsoft.com/ado/2007/06/edmx",
                                                    Prefix = "edmx",
                                                    Name = "Edmx"
                                                }
                                            }
                                        }
                                    },
                                    {"application/json", new MediaType
                                        {
                                            Schema = new ObjectSchema
                                            {
                                                Type = T.Object,
                                                Properties = new Dictionary<string, Schema>
                                                {
                                                    {"Version", new ObjectSchema{Type = T.String}},
                                                    {"DataServices", new ObjectSchema
                                                        {
                                                            Type = T.Object,
                                                            Properties = new Dictionary<string, Schema>
                                                            {
                                                                {"DataServiceVersion", new ObjectSchema{Type = T.String}},
                                                                {"Schemas", new ObjectSchema
                                                                    {
                                                                        Type = T.Array,
                                                                        Items = new ObjectSchema{Type = T.Object}
                                                                    }
                                                                },
                                                            }
                                                        }
                                                    },
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        {Ref.R.Http200_property, new Response
                            {
                                Description = "Property response.",
                                Content = new Dictionary<string, MediaType>
                                {
                                    {"application/json", new MediaType
                                        {
                                            Schema = new ObjectSchema
                                            {
                                                Type = T.Object,
                                                Properties = new Dictionary<string, Schema>
                                                {
                                                    {"d", new ObjectSchema
                                                        {
                                                            Type = T.Object,
                                                            Properties = new Dictionary<string, Schema>
                                                            {
                                                                {"PropertyName", new ObjectSchema
                                                                    {
                                                                        OneOf = new []
                                                                        {
                                                                            new ObjectSchema{Type = T.String},
                                                                            new ObjectSchema{Type = T.Integer},
                                                                            new ObjectSchema{Type = T.Boolean},
                                                                            new ObjectSchema{Type = T.Number},
                                                                            new ObjectSchema{Type = T.Array},
                                                                            new ObjectSchema{Type = T.Object},
                                                                        }
                                                                    }

                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    Schemas = new Dictionary<string, Schema>
                    {
                        {Ref.S.MetadataFormat, new ObjectSchema
                            {
                                Type = T.String,
                                Description = "Defines metadata formats.\n\n" +
                                              "- No: there is no any metadata.\n" +
                                              "- Minimal: Every entity has minimal metadata (uri, type).\n" +
                                              "- Full (default): every entity contains the full metadata (uri, type, actions, functions).",
                                //EnumNames = new [] { "No", "Minimal", "Full" },
                                Enum = new [] { "no", "minimal", "full" },
                            }
                        },
                        {Ref.S.ODataEntity, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "???",
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"d", new ObjectSchema
                                        {
                                            Type = T.Object,
                                            OneOf = new []
                                            {
                                                new ObjectSchema { Ref = Ref.Schemas + Ref.S.Content }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        {Ref.S.ODataEntitySet, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "???",
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"d", new ObjectSchema
                                        {
                                            Type = T.Object,
                                            Properties = new Dictionary<string, Schema>
                                            {
                                                {"__count", new ObjectSchema {Type = T.Integer, Format = F.Int32}},
                                                {"results", new ObjectSchema
                                                    {
                                                        Type = T.Array,
                                                        Items = new ObjectSchema
                                                        {
                                                            OneOf = new []
                                                            {
                                                                new ObjectSchema{ Ref = Ref.Schemas + Ref.S.Content}
                                                            }
                                                        }
                                                    }
                                                },
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        {Ref.S.ContentCreationRequest, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Defines content creation request model. Available properties are depends on the desired" +
                                              " ContentType. Missing properties get their default values. Only the `__ContentType` " +
                                              " parameter is required.",
                                AdditionalProperties = true,
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"__ContentType", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Description = "Content type name of the new content. The ContentType should be exist.",
                                            Nullable = false,
                                        }
                                    },
                                    {"Name", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Description = "Name of the new content.",
                                            Nullable = true,
                                        }
                                    },
                                    {"DisplayName", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Description = "Display-name of the new content.",
                                            Nullable = true,
                                        }
                                    },
                                }
                            }
                        },
                        {Ref.S.ContentResetRequest, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Defines content modification request model. Available properties are depends on" +
                                              " the desired ContentType. Missing properties get their default values.",
                                AdditionalProperties = true,
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"DisplayName", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Description = "Display-name of the content.",
                                            Nullable = false,
                                        }
                                    },
                                }
                            }
                        },
                        {Ref.S.ContentModificationRequest, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Defines content modification request model. Available properties are depends on" +
                                              " the desired ContentType. Only the patched properties will be changed.",
                                AdditionalProperties = true,
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"DisplayName", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Description = "Display-name of the new content.",
                                            Nullable = false,
                                        }
                                    },
                                }
                            }
                        },
                        {Ref.S.Content, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Set of properties with any type extended with some metadata. The collection of properties depends from the Content's ContentType and the value of the '$select' parameter. Metadata appearance can be controlled with `metadata` parameter.",
                                AllOf = new [] { new ObjectSchema { Ref = Ref.Schemas + Ref.S.FieldSet } },
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"__metadata", new ObjectSchema
                                        {
                                            Description = "???",
                                            Nullable = true,
                                            OneOf = new [] { new ObjectSchema { Ref = Ref.Schemas + Ref.S.ContentMetadata } },
                                        }
                                    }
                                }
                            }
                        },
                        {Ref.S.FieldSet, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "???",
                                AdditionalProperties = true,
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"Id", new ObjectSchema{Type = T.Integer, Format = F.Int32, Nullable = true}},
                                    {"Type", new ObjectSchema{Type = T.String, Nullable = true}},
                                    {"Name", new ObjectSchema{Type = T.String, Nullable = true}},
                                    {"CreatedBy", new ObjectSchema
                                        {
                                            Type = T.Object,
                                            Nullable = true,
                                            OneOf = new [] { new ObjectSchema { Ref = Ref.Schemas + Ref.S.ReferenceField } },
                                        }
                                    },
                                }
                            }
                        },
                        {Ref.S.ReferenceField, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "???",
                                OneOf = new []
                                {
                                    new ObjectSchema { Ref = Ref.Schemas + Ref.S.DeferredReferenceField },
                                    new ObjectSchema { Ref = Ref.Schemas + Ref.S.Content }
                                },
                            }
                        },
                        {Ref.S.DeferredReferenceField, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "???",
                                Properties = new Dictionary<string, Schema>
                                {
                                    { "uri", new ObjectSchema { Type = T.String } }
                                },
                            }
                        },
                        {Ref.S.ContentMinimalMetadata, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "???",
                                Properties = new Dictionary<string, Schema>
                                {
                                    { "uri", new ObjectSchema { Type = T.String } },
                                    { "type", new ObjectSchema { Type = T.String } }
                                },
                            }
                        },
                        {Ref.S.ContentMetadata, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "???",
                                AllOf = new [] { new ObjectSchema { Ref = Ref.Schemas + Ref.S.ContentMinimalMetadata } },
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"actions", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Description = "???",
                                            Items = new ObjectSchema { Ref = Ref.Schemas + Ref.S.ContentMetaOperation }
                                        }
                                    },
                                    {"functions", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Description = "???",
                                            Items = new ObjectSchema { Ref = Ref.Schemas + Ref.S.ContentMetaOperation }
                                        }
                                    },
                                },
                            }
                        },
                        {Ref.S.ContentMetaOperation, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "???",
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"title", new ObjectSchema
                                        {
                                            Description = "???",
                                            Nullable = true,
                                            Type = T.String
                                        }
                                    },
                                    {"name", new ObjectSchema
                                        {
                                            Description = "???",
                                            Nullable = true,
                                            Type = T.String
                                        }
                                    },
                                    {"opId", new ObjectSchema
                                        {
                                            Description = "???",
                                            Nullable = true,
                                            Type = T.String
                                        }
                                    },
                                    {"target", new ObjectSchema
                                        {
                                            Description = "???",
                                            Nullable = true,
                                            Type = T.String
                                        }
                                    },
                                    {"forbidden", new ObjectSchema
                                        {
                                            Description = "???",
                                            Nullable = true,
                                            Type = T.Boolean
                                        }
                                    },
                                    {"parameters", new ObjectSchema
                                        {
                                            Description = "???",
                                            Nullable = true,
                                            Type = T.Array,
                                            Items = new ObjectSchema { Ref = Ref.Schemas + Ref.S.ContentMetaParameter }
                                        }
                                    },
                                },
                            }
                        },
                        {Ref.S.ContentMetaParameter, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "???",
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"name", new ObjectSchema
                                        {
                                            Description = "???",
                                            Nullable = true,
                                            Type = T.String
                                        }
                                    },
                                    {"type", new ObjectSchema
                                        {
                                            Description = "???",
                                            Nullable = true,
                                            Type = T.String
                                        }
                                    },
                                    {"required", new ObjectSchema
                                        {
                                            Description = "???",
                                            Nullable = true,
                                            Type = T.Boolean
                                        }
                                    },
                                },
                            }
                        },
                        /*
                        {Ref.S.SnTaskResult, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Represents a task execution result.",
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"MachineName", new ObjectSchema
                                        {
                                            Description = "Agent machine name.",
                                            Nullable = true,
                                            Type = T.String
                                        }
                                    },
                                    {"AgentName", new ObjectSchema
                                        {
                                            Description = "Agent name.",
                                            Nullable = true,
                                            Type = T.String
                                        }
                                    },
                                    {"Task", new ObjectSchema {Ref = Ref.Schemas + Ref.S.SnTask}},
                                    {"ResultCode", new ObjectSchema
                                        {
                                            Description = "Execution result code.",
                                            Nullable = true,
                                            Type = T.Integer,
                                            Format = F.Int32
                                        }
                                    },
                                    {"ResultData", new ObjectSchema
                                        {
                                            Description = "Result data.",
                                            Nullable = true,
                                            Type = T.String
                                        }
                                    },
                                    {"Error", new ObjectSchema {Ref = Ref.Schemas + Ref.S.SnTaskError}},
                                    {"Successful", new ObjectSchema
                                        {
                                            Description = "Gets whether the task execution was successful.",
                                            Nullable = true,
                                            Type = T.Boolean
                                        }
                                    },
                                },
                            }
                        },
                        {Ref.S.SnTaskError, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Represents an error that occured during task execution.",
                                Nullable = true,
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"ErrorCode", new ObjectSchema{ Type=T.String, Description = "Error code."}},
                                    {"ErrorType", new ObjectSchema{ Type=T.String, Description = "Exception type name or a custom error type."}},
                                    {"Message", new ObjectSchema{ Type=T.String, Description = "Error message."}},
                                    {"Details", new ObjectSchema{ Type=T.String, Description = "Error details."}},
                                    {"CallingContext", new ObjectSchema{ Type=T.String, Description = "Custom error data serialized in JSON format."}},
                                }
                            }
                        },
                        {Ref.S.SnTask, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Represents a unit of work that is registered by client applications with the central Task Management web application",
                                Nullable = true,
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"Id", new ObjectSchema{ Type=T.Integer, Format = F.Int32, Description = "Task identifier."}},
                                    {"Type", new ObjectSchema{ Type=T.String, Description = "Task type. Identifies the executor command line tool (without the '.exe' extension) that will be started by the agent."}},
                                    {"Title", new ObjectSchema{ Type=T.String, Description = "Task title."}},
                                    {"Order", new ObjectSchema{ Type="number", Format = F.Double, Description = "Task priority. Must be one of the predefined TaskPriority enum values: 0, 1, 10, 100, 1000."}},
                                    {"Tag", new ObjectSchema{ Type=T.String, Description = "Optional tag that classifies the task in the client application (e.g. the workspace where the task was generated in)."}},
                                    {"RegisteredAt", new ObjectSchema{ Type=T.String, Format = F.DateTime, Description = "Task registration UTC time."}},
                                    {"AppId", new ObjectSchema{ Type=T.String, Description = "Client application that registered the task."}},
                                    {"LastLockUpdate", new ObjectSchema{ Type=T.String, Format = F.DateTime, Description = "When the executing agent updated the lock on the task."}},
                                    {"LockedBy", new ObjectSchema{ Type=T.String, Description = "The agent that locked this task."}},
                                    {"TaskKey", new ObjectSchema{ Type=T.String, Description = "Optional task key."}},
                                    {"Hash", new ObjectSchema{ Type=T.Integer, Format = F.Int64, Description = " Hash that represents this task. Can be provided by the client application, but usually it is generated by the Task Management component."}},
                                    {"TaskData", new ObjectSchema{ Type=T.String, Description = "String representation of the information necessary for the task executor tool to run. Usually a JSON object containing properties - e.g. content id or any other custom value. This data is not parsed by the Task Management component, it is only passed over to the executor without changes."}},
                                    {"FinalizeUrl", new ObjectSchema{ Type=T.String, Description = "Custom callback url for this task type."}},
                                }
                            }
                        },
                        {Ref.S.PreviewStatus, new ObjectSchema
                            {
                                Type = T.String,
                                Description = "???",
                                Enum = new [] {"noprovider", "postponed", "error", "notsupported", "inprogress", "emptydocument", "ready"},
                                //EnumNames = new [] {"NoProvider", "Postponed", "Error", "NotSupported", "InProgress", "EmptyDocument", "Ready"}
                            }
                        },
                        {Ref.S.IndexRebuildLevel, new ObjectSchema
                            {
                                Type = T.String,
                                Description = "Defines constants for the level of index rebuilding.",
                                Enum = new [] { "indexonly", "databaseandindex"},
                                //EnumNames = new [] { "IndexOnly", "DatabaseAndIndex" }
                            }
                        },
                        {Ref.S.SharingMode, new ObjectSchema
                            {
                                Type = T.String,
                                Description = "Specifies the rules how the generated sharing link can be used and by whom.",
                                Enum = new [] { "public", "authenticated", "private"},
                                //EnumNames = new [] { "Public", "Authenticated", "Private" }
                            }
                        },
                        {Ref.S.SharingLevel, new ObjectSchema
                            {
                                Type = T.String,
                                Description = "Specifies the level of access the user will have for the shared content.",
                                Enum = new [] { "open", "edit"},
                                //EnumNames = new [] { "Open", "Edit" }
                            }
                        },
                        {Ref.S.BackupResponse, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Contains return information for the backup actions and status queries.",
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"State", new ObjectSchema{ Ref = Ref.Schemas + Ref.S.BackupState}},
                                    {"Current", new ObjectSchema{ Ref = Ref.Schemas + Ref.S.BackupInfo}},
                                    {"History", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Items = new ObjectSchema{ Ref = Ref.Schemas + Ref.S.BackupInfo}
                                        }
                                    },
                                }
                            }
                        },
                        {Ref.S.BackupState, new ObjectSchema
                            {
                                Type = T.String,
                                Description = "Represents a state of the backup operation.",
                                Enum = new [] { "initial", "started", "executing", "finished", "cancelRequested", "canceled", "faulted"},
                                EnumNames = new [] {"Initial", "Started", "Executing", "Finished", "CancelRequested", "Canceled", "Faulted"}
                            }
                        },
                        {Ref.S.BackupInfo, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Represents an index backup operation.",
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"StartedAt", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Format = F.DateTime,
                                            Description = "UTC time of the start.",
                                        }
                                    },
                                    {"FinishedAt", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Format = F.DateTime,
                                            Description = "UTC time of the finish. The value is DateTime.MinValue if the operation is unfinished.",
                                        }
                                    },
                                    {"TotalBytes", new ObjectSchema
                                        {
                                            Type = T.Integer,
                                            Format = F.Int64,
                                            Description = "Total length of the files to be copied.",
                                        }
                                    },
                                    {"CopiedBytes", new ObjectSchema
                                        {
                                            Type = T.Integer,
                                            Format = F.Int64,
                                            Description = "Total length of the copied files.",
                                        }
                                    },
                                    {"CountOfFiles", new ObjectSchema
                                        {
                                            Type = T.Integer,
                                            Format = F.Int32,
                                            Description = "Count of the files to be copied.",
                                        }
                                    },
                                    {"CopiedFiles", new ObjectSchema
                                        {
                                            Type = T.Integer,
                                            Format = F.Int32,
                                            Description = "Count of the copied files.",
                                        }
                                    },
                                    {"CurrentlyCopiedFile", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Description = "Name of the currently copied file.",
                                        }
                                    },
                                    {"Message", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Description = "Error or cancellation message. In case of currently executing or successfully finished operations the value is null.",
                                        }
                                    },
                                }
                            }
                        },
                        {Ref.S.IndexProperties, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Provides aggregated information of the index.",
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"IndexingActivityStatus", new ObjectSchema{Ref = Ref.Schemas + Ref.S.IndexingActivityStatus}},
                                    {"FieldInfo", new DictionarySchema
                                        {
                                            Description = "Ordered list of all field names and term count of the index.",
                                            Type = T.Object,
                                            AdditionalProperties = new ObjectSchema {Type = T.Integer, Format = F.Int32}
                                        }
                                    },
                                    {"VersionIds", new ObjectSchema
                                        {
                                            Description = "Ordered list of all VersionIds in the index.",
                                            Type = T.Array,
                                            Items = new ObjectSchema {Type = T.Integer, Format = F.Int32}
                                        }
                                    },
                                }
                            }
                        },
                        {Ref.S.IndexingActivityStatus, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Represents an indexing state.",
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"LastActivityId", new ObjectSchema
                                    {
                                        Description = "Last written activity id.",
                                        Type = T.Integer,
                                        Format = F.Int32
                                    }},
                                    {"Gaps", new ObjectSchema
                                    {
                                        Description = "Array of the missing activity ids that are less than the LastActivityId.",
                                        Type = T.Array,
                                        Items = new ObjectSchema {Type = T.Integer, Format = F.Int32}
                                    }},
                                }
                            }
                        },
                        {Ref.S.IndexingActivityHistory, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Defines a data class that provides information about the short history of the indexing activity execution in the population of the local index.",
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"State", new ObjectSchema{Ref = Ref.Schemas + Ref.S.IndexingActivityQueueState}},
                                    {"Message", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Description = "Message that occurs when there are one or more unfinished history item. This is happens tipically in case of the webserver's heavy load."
                                        }
                                    },
                                    {"RecentLength", new ObjectSchema
                                        {
                                            Type = T.Integer,
                                            Format = F.Int32,
                                            Description = "Length of the recent list."
                                        }
                                    },
                                    {"Recent", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Description = "Last relevant items in the history.",
                                            Items = new ObjectSchema{Ref = Ref.Schemas + Ref.S.IndexingActivityHistoryItem}
                                        }
                                    },
                                }
                            }
                        },
                        {Ref.S.IndexingActivityHistoryItem, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Defines a data class that represents an item in the short history of the indexing activity execution in the population of the local index.",
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"Id", new ObjectSchema
                                        {
                                            Type = T.Integer,
                                            Format = F.Int32,
                                            Description = "Id of the indexing activity."
                                        }
                                    },
                                    {"TypeName", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Description = "Type name of the indexing activity."
                                        }
                                    },
                                    {"FromReceiver", new ObjectSchema
                                        {
                                            Type = T.Boolean,
                                            Description = "True if the indexing activity is received from a messaging channel."
                                        }
                                    },
                                    {"FromDb", new ObjectSchema
                                        {
                                            Type = T.Boolean,
                                            Description = "True if the indexing activity is loaded from the database."
                                        }
                                    },
                                    {"IsStartup", new ObjectSchema
                                        {
                                            Type = T.Boolean,
                                            Description = "True if the indexing activity is executed in the system startup sequence."
                                        }
                                    },
                                    {"Error", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Description = "Error message if any error occured in the execution of the indexing activity."
                                        }
                                    },
                                    {"WaitedFor", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Description = "Id array of the indexing activities that are blocked the current activity's execution.",
                                            Items = new ObjectSchema{Type = T.Integer, Format = F.Int32}
                                        }
                                    },
                                    {"ArrivedAt", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Format = F.DateTime,
                                            Description = "Arrival time of the indexing activity."
                                        }
                                    },
                                    {"StartedAt", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Format = F.DateTime,
                                            Description = "Starting time of the indexing activity execution."
                                        }
                                    },
                                    {"FinishedAt", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Format = F.DateTime,
                                            Description = "Finishing time of the indexing activity execution."
                                        }
                                    },
                                    {"WaitTime", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Format = F.TimeSpan,
                                            Description = "Waiting time of the indexing activity."
                                        }
                                    },
                                    {"ExecTime", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Format = F.TimeSpan,
                                            Description = "Execution time of the indexing activity."
                                        }
                                    },
                                    {"FullTime", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Format = F.TimeSpan,
                                            Description = "Full execution time of the indexing activity."
                                        }
                                    },
                                }
                            }
                        },
                        {Ref.S.IndexingActivityQueueState, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Defines a data class that provides information about the indexing activity organizer in the population of the local index.",
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"Serializer", new ObjectSchema{Ref = Ref.Schemas + Ref.S.IndexingActivitySerializerState}},
                                    {"DependencyManager", new ObjectSchema{Ref = Ref.Schemas + Ref.S.IndexingActivityDependencyState}},
                                    {"Termination", new ObjectSchema{Ref = Ref.Schemas + Ref.S.IndexingActivityStatus}},
                                }
                            }
                        },
                        {Ref.S.IndexingActivitySerializerState, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Defines a data class that provides information about the activity execution serialization in the population of the local index.",
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"LastQueued", new ObjectSchema
                                        {
                                            Type = T.Integer,
                                            Format = F.Int32,
                                            Description = "Id of the last queued indexing activity."
                                        }
                                    },
                                    {"QueueLength", new ObjectSchema
                                        {
                                            Type = T.Integer,
                                            Format = F.Int32,
                                            Description = "Length of the arrival queue of the indexing activities."
                                        }
                                    },
                                    {"Queue", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Description = "Ids of the activities in the arrival queue.",
                                            Items = new ObjectSchema{Type = T.Integer, Format = F.Int32}
                                        }
                                    },
                                }
                            }
                        },
                        {Ref.S.IndexingActivityDependencyState, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Defines a data class that provides information about the waiting activities in the population of the local index.",
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"WaitingSetLength", new ObjectSchema
                                        {
                                            Type = T.Integer,
                                            Format = F.Int32,
                                            Description = "Length of list that contains waiting indexing activities."
                                        }
                                    },
                                    {"WaitingSet", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Description = "Ids of the waiting indexing activities.",
                                            Items = new ObjectSchema{Type = T.Integer, Format = F.Int32}
                                        }
                                    },
                                }
                            }
                        },
                        {Ref.S.SecurityActivityHistory, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Contains momentary state information about the security activity execution and the recent processed activities in details.",
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"State", new ObjectSchema{Ref = Ref.Schemas + Ref.S.SecurityActivityQueueState}},
                                    {"Message", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Description = "It is empty or contains a message about any error in connection with the SecurityActivityHistory feature.",
                                        }
                                    },
                                    {"RecentLength", new ObjectSchema
                                        {
                                            Type = T.Integer,
                                            Format = F.Int32,
                                            Description = "Length of the Recent",
                                        }
                                    },
                                    {"Recent", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Description = "Array of the recently executed activities.",
                                            Items = new ObjectSchema{Ref = Ref.Schemas + Ref.S.SecurityActivityHistoryItem}
                                        }
                                    },
                                }
                            }
                        },
                        {Ref.S.SecurityActivityQueueState, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Contains momentary state information about the security activity execution for debugging purposes.",
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"Serializer", new ObjectSchema{Ref = Ref.Schemas + Ref.S.SecurityActivitySerializerState}},
                                    {"DependencyManager", new ObjectSchema{Ref = Ref.Schemas + Ref.S.SecurityActivityDependencyState}},
                                    {"Termination", new ObjectSchema{Ref = Ref.Schemas + Ref.S.CompletionState}},
                                }
                            }
                        },
                        {Ref.S.SecurityActivitySerializerState, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Contains information about the serialized activities on the arrival size.",
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"LastQueued", new ObjectSchema
                                        {
                                            Type = T.Integer,
                                            Format = F.Int32,
                                            Description = "Id of the last arrived activity.",
                                        }
                                    },
                                    {"QueueLength", new ObjectSchema
                                        {
                                            Type = T.Integer,
                                            Format = F.Int32,
                                            Description = "Length of the Queue",
                                        }
                                    },
                                    {"Queue", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Description = "Ids of th Arrived but not parallel activities.",
                                            Items = new ObjectSchema{Type = T.Integer, Format = F.Int32}
                                        }
                                    },
                                }
                            }
                        },
                        {Ref.S.SecurityActivityDependencyState, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Contains information about the waiting activities.",
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"WaitingSetLength", new ObjectSchema
                                        {
                                            Type = T.Integer,
                                            Format = F.Int32,
                                            Description = "Length of the WaitingSet.",
                                        }
                                    },
                                    {"WaitingSet", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Description = "Ids of the all waiting activities.",
                                            Items = new ObjectSchema{Type = T.Integer, Format = F.Int32}
                                        }
                                    },
                                }
                            }
                        },
                        {Ref.S.CompletionState, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Contains information about the executed activities.",
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"LastActivityId", new ObjectSchema
                                        {
                                            Type = T.Integer,
                                            Format = F.Int32,
                                            Description = "Id of the last executed activity.",
                                        }
                                    },
                                    {"GapsLength", new ObjectSchema
                                        {
                                            Type = T.Integer,
                                            Format = F.Int32,
                                            Description = "Length of the Gaps array.",
                                        }
                                    },
                                    {"Gaps", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Description = "Contains activity ids that are not executed yet and are lower than the LastActivityId.",
                                            Items = new ObjectSchema{Type = T.Integer, Format = F.Int32}
                                        }
                                    },
                                }
                            }
                        },
                        {Ref.S.SecurityActivityHistoryItem, new ObjectSchema
                            {
                                Type = T.Object,
                                Description = "Contains debug information about a security activity execution.",
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"Id", new ObjectSchema
                                        {
                                            Type = T.Integer,
                                            Format = F.Int32,
                                            Description = "Id of the activity.",
                                        }
                                    },
                                    {"TypeName", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Description = "Short name of the activity type.",
                                        }
                                    },
                                    {"FromReceiver", new ObjectSchema
                                        {
                                            Type = T.Boolean,
                                            Description = "True if the activity was received from another computer.",
                                        }
                                    },
                                    {"FromDb", new ObjectSchema
                                        {
                                            Type = T.Boolean,
                                            Description = "True if the activity was received from another computer.",
                                        }
                                    },
                                    {"IsStartup", new ObjectSchema
                                        {
                                            Type = T.Boolean,
                                            Description = "True if the activity is instantiated during in the startup process.",
                                        }
                                    },
                                    {"Error", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Description = "Contains error message if the activity execution was unsuccessful.",
                                        }
                                    },
                                    {"WaitedFor", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Description = "Ids of the activities that are delayed the execution of this activity.",
                                            Items = new ObjectSchema{Type = T.Integer, Format = F.Int32}
                                        }
                                    },
                                    {"ArrivedAt", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Format = F.DateTime,
                                            Description = "Arrival time.",
                                        }
                                    },
                                    {"StartedAt", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Format = F.DateTime,
                                            Description = "Time of the execution start.",
                                        }
                                    },
                                    {"FinishedAt", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Format = F.DateTime,
                                            Description = "Time of the execution end.",
                                        }
                                    },
                                    {"WaitTime", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Format = F.TimeSpan,
                                            Description = "Waiting time (StartedAt - ArrivedAt)",
                                        }
                                    },
                                    {"ExecTime", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Format = F.TimeSpan,
                                            Description = "Execution time (FinishedAt - StartedAt)",
                                        }
                                    },
                                    {"FullTime", new ObjectSchema
                                        {
                                            Type = T.String,
                                            Format = F.TimeSpan,
                                            Description = "Full time (FinishedAt - ArrivedAt)",
                                        }
                                    },
                                }
                            }
                        },
                        {Ref.S.SecurityConsistencyResult, new ObjectSchema
                            {
                                Type = T.Object,
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"IsConsistent", new ObjectSchema{Type = T.Boolean}},
                                    {"IsMembershipConsistent", new ObjectSchema{Type = T.Boolean}},
                                    {"IsEntityStructureConsistent", new ObjectSchema{Type = T.Boolean}},
                                    {"IsAcesConsistent", new ObjectSchema{Type = T.Boolean}},
                                    {"ElapsedTime", new ObjectSchema{Type = T.String, Format = F.TimeSpan}},
                                    {"MissingEntitiesFromRepository", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Items = new ObjectSchema{Ref = Ref.Schemas + Ref.S.SecurityEntityInfo},
                                        }
                                    },
                                    {"MissingEntitiesFromSecurityDb", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Items = new ObjectSchema{Ref = Ref.Schemas + Ref.S.SecurityEntityInfo},
                                        }
                                    },
                                    {"MissingEntitiesFromSecurityCache", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Items = new ObjectSchema{Ref = Ref.Schemas + Ref.S.SecurityEntityInfo},
                                        }
                                    },
                                    {"MissingMembershipsFromCache", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Items = new ObjectSchema{Ref = Ref.Schemas + Ref.S.SecurityMembershipInfo},
                                        }
                                    },
                                    {"UnknownMembershipInSecurityDb", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Items = new ObjectSchema{Ref = Ref.Schemas + Ref.S.SecurityMembershipInfo},
                                        }
                                    },
                                    {"MissingMembershipsFromSecurityDb", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Items = new ObjectSchema{Ref = Ref.Schemas + Ref.S.SecurityMembershipInfo},
                                        }
                                    },
                                    {"UnknownMembershipInCache", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Items = new ObjectSchema{Ref = Ref.Schemas + Ref.S.SecurityMembershipInfo},
                                        }
                                    },
                                    {"MissingRelationFromFlattenedUsers", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Items = new ObjectSchema{Ref = Ref.Schemas + Ref.S.SecurityMembershipInfo},
                                        }
                                    },
                                    {"UnknownRelationInFlattenedUsers", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Items = new ObjectSchema{Ref = Ref.Schemas + Ref.S.SecurityMembershipInfo},
                                        }
                                    },
                                    {"InvalidACE_MissingEntity", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Items = new ObjectSchema{Ref = Ref.Schemas + Ref.S.StoredAceDebugInfo},
                                        }
                                    },
                                    {"InvalidACE_MissingIdentity", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Items = new ObjectSchema{Ref = Ref.Schemas + Ref.S.StoredAceDebugInfo},
                                        }
                                    },
                                }
                            }
                        },
                        {Ref.S.SecurityEntityInfo, new ObjectSchema
                            {
                                Type = T.Object,
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"Id", new ObjectSchema{Type = T.Integer, Format = F.Int32}},
                                    {"ParentId", new ObjectSchema{Type = T.Integer, Format = F.Int32}},
                                    {"OwnerId", new ObjectSchema{Type = T.Integer, Format = F.Int32}},
                                    {"Path", new ObjectSchema{Type = T.String}},
                                }
                            }
                        },
                        {Ref.S.SecurityMembershipInfo, new ObjectSchema
                            {
                                Type = T.Object,
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"GroupId", new ObjectSchema{Type = T.Integer, Format = F.Int32}},
                                    {"MemberId", new ObjectSchema{Type = T.Integer, Format = F.Int32}},
                                    {"GroupPath", new ObjectSchema{Type = T.String}},
                                    {"MemberPath", new ObjectSchema{Type = T.String}},
                                }
                            }
                        },
                        {Ref.S.StoredAceDebugInfo, new ObjectSchema
                            {
                                Type = T.Object,
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"EntityId", new ObjectSchema{Type = T.Integer, Format = F.Int32}},
                                    {"IdentityId", new ObjectSchema{Type = T.Integer, Format = F.Int32}},
                                    {"LocalOnly", new ObjectSchema{Type = T.Boolean}},
                                    {"AllowBits", new ObjectSchema{Type = T.String, Format = F.Ulong}},
                                    {"DenyBits", new ObjectSchema{Type = T.String, Format = F.Ulong}},
                                    {"StringView", new ObjectSchema{Type = T.String}},
                                }
                            }
                        },
                        {Ref.S.RegeneratePreviewsResponse, new ObjectSchema
                            {
                                Type = T.Object,
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"PageCount", new ObjectSchema{Type = T.Integer, Format = F.Int32}},
                                    {"PreviewCount", new ObjectSchema{Type = T.Integer, Format = F.Int32}},
                                }
                            }
                        },
                        {Ref.S.GetPreviewsFolderResponse, new ObjectSchema
                            {
                                Type = T.Object,
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"Id", new ObjectSchema{Type = T.Integer, Format = F.Int32}},
                                    {"Path", new ObjectSchema{Type = T.String}},
                                }
                            }
                        },
                        {Ref.S.PreviewAvailableResponse, new ObjectSchema
                            {
                                Type = T.Object,
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"PreviewAvailable", new ObjectSchema{Type = T.String}},
                                    {"Width", new ObjectSchema{Type = T.Integer, Format = F.Int32}},
                                    {"Height", new ObjectSchema{Type = T.Integer, Format = F.Int32}},
                                }
                            }
                        },
                        {Ref.S.GetExistingPreviewImagesResponse, new ObjectSchema
                            {
                                Type = T.Object,
                                AllOf = new [] { new ObjectSchema { Ref = Ref.Schemas + Ref.S.PreviewAvailableResponse } },
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"Index", new ObjectSchema{Type = T.Integer, Format = F.Int32}},
                                }
                            }
                        },
                        {Ref.S.CheckPreviewsResponse, new ObjectSchema
                            {
                                Type = T.Object,
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"PageCount", new ObjectSchema{Type = T.Integer, Format = F.Int32}},
                                    {"PreviewCount", new ObjectSchema{Type = T.Integer, Format = F.Int32}},
                                }
                            }
                        },
                        {Ref.S.IdentityInfo, new ObjectSchema
                            {
                                Type = T.Object,
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"path", new ObjectSchema{Type = T.String}},
                                    {"name", new ObjectSchema{Type = T.String}},
                                    {"displayName", new ObjectSchema{Type = T.String}},
                                    {"groups", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Items = new ObjectSchema{Ref = Ref.Schemas + Ref.S.GroupInfo}
                                        }
                                    },
                                }
                            }
                        },
                        {Ref.S.GroupInfo, new ObjectSchema
                            {
                                Type = T.Object,
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"path", new ObjectSchema{Type = T.String}},
                                    {"name", new ObjectSchema{Type = T.String}},
                                    {"displayName", new ObjectSchema{Type = T.String}},
                                }
                            }
                        },
                        {Ref.S.ChildPermissionInfo, new ObjectSchema
                            {
                                Type = T.Object,
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"path", new ObjectSchema{Type = T.String}},
                                    {"name", new ObjectSchema{Type = T.String}},
                                    {"displayName", new ObjectSchema{Type = T.String}},
                                    {"isFolder", new ObjectSchema{Type = T.Boolean}},
                                    {"break", new ObjectSchema{Type = T.Boolean}},
                                    {"permissions", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Items = new ObjectSchema{Ref = Ref.Schemas + Ref.S.PermissionInfo}
                                        }
                                    },
                                    {"subPermissions", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Items = new ObjectSchema{Ref = Ref.Schemas + Ref.S.PermissionInfo}
                                        }
                                    },
                                }
                            }
                        },
                        {Ref.S.PermissionInfo, new ObjectSchema
                            {
                                Type = T.Object,
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"name", new ObjectSchema{Type = T.String}},
                                    {"index", new ObjectSchema{Type = T.Integer, Format = F.Int32}},
                                    {"type", new ObjectSchema{Type = T.String}},
                                    {"localOnly", new ObjectSchema{Type = T.Boolean}},
                                }
                            }
                        },
                        {Ref.S.PermissionInfoResponse, new ObjectSchema
                            {
                                Type = T.Object,
                                OneOf = new []
                                {
                                    new ObjectSchema{Ref = Ref.Schemas + Ref.S.SinglePermissionInfoResponse},
                                    new ObjectSchema{Ref = Ref.Schemas + Ref.S.ChildrenPermissionInfoResponse},
                                },
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"identity", new ObjectSchema{Ref = Ref.Schemas + Ref.S.IdentityInfo}},
                                }
                            }
                        },
                        {Ref.S.SinglePermissionInfoResponse, new ObjectSchema
                            {
                                Type = T.Object,
                                AllOf = new []
                                {
                                    new ObjectSchema{Ref = Ref.Schemas + Ref.S.PermissionInfoResponse},
                                },
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"permissionInfo", new ObjectSchema{Ref = Ref.Schemas + Ref.S.ChildPermissionInfo}},
                                }
                            }
                        },
                        {Ref.S.ChildrenPermissionInfoResponse, new ObjectSchema
                            {
                                Type = T.Object,
                                AllOf = new []
                                {
                                    new ObjectSchema{Ref = Ref.Schemas + Ref.S.PermissionInfoResponse},
                                },
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"results", new ObjectSchema
                                        {
                                            Type = T.Array,
                                            Items = new ObjectSchema{Ref = Ref.Schemas + Ref.S.ChildPermissionInfo}
                                        }
                                    },
                                }
                            }
                        },
                        {Ref.S.GetPermissionInfoResponse, new ObjectSchema
                            {
                                Type = T.Object,
                                OneOf = new []
                                {
                                    new ObjectSchema{Ref = Ref.Schemas + Ref.S.GetSinglePermissionInfoResponse},
                                    new ObjectSchema{Ref = Ref.Schemas + Ref.S.GetChildrenPermissionInfoResponse},
                                },
                            }
                        },
                        {Ref.S.GetSinglePermissionInfoResponse, new ObjectSchema
                            {
                                Type = T.Object,
                                AllOf = new []
                                {
                                    new ObjectSchema{Ref = Ref.Schemas + Ref.S.GetPermissionInfoResponse},
                                },
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"d", new ObjectSchema{Ref = Ref.Schemas + Ref.S.SinglePermissionInfoResponse}},
                                }
                            }
                        },
                        {Ref.S.GetChildrenPermissionInfoResponse, new ObjectSchema
                            {
                                Type = T.Object,
                                AllOf = new []
                                {
                                    new ObjectSchema{Ref = Ref.Schemas + Ref.S.GetPermissionInfoResponse},
                                },
                                Properties = new Dictionary<string, Schema>
                                {
                                    {"d", new ObjectSchema{Ref = Ref.Schemas + Ref.S.ChildrenPermissionInfoResponse}},
                                }
                            }
                        },
                        */
                    },
                    SecuritySchemes = new Dictionary<string, SecurityScheme>
                    {
                        {"JWT", new SecurityScheme
                            {
                                Type = "apiKey",
                                Description = "Paste your JWT token from the token endpoint.",
                                Name = "Authorization",
                                In = "header"
                            }
                        }
                    }
                },
                Security = new[] { new Dictionary<string, string[]> { { "JWT", Array.Empty<string>() } } }
            };
        }
    }
}
