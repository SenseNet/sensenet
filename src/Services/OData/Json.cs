using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
//TODO: Rename Json.cs to more generalized name
namespace SenseNet.Portal.OData
{
    internal class ODataSimpleMeta
    {
        [JsonProperty(PropertyName = "uri", Order = 1)]
        public string Uri { get; set; }
        [JsonProperty(PropertyName = "type", Order = 2)]
        public string Type { get; set; }
    }
    internal class ODataFullMeta : ODataSimpleMeta
    {
        [JsonProperty(PropertyName = "actions", Order = 3)]
        public ODataOperation[] Actions { get; set; }
        [JsonProperty(PropertyName = "functions", Order = 4)]
        public ODataOperation[] Functions { get; set; }
    }
    internal class ODataOperation
    {
        [JsonProperty(PropertyName = "title", Order = 1)]
        public string Title { get; set; }
        [JsonProperty(PropertyName = "name", Order = 2)]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "target", Order = 3)]
        public string Target { get; set; }
        [JsonProperty(PropertyName = "forbidden", Order = 4)]
        public bool Forbidden { get; set; }
        [JsonProperty(PropertyName = "parameters", Order = 5)]
        public ODataOperationParameter[] Parameters { get; set; }
    }
    internal class ODataOperationParameter
    {
        [JsonProperty(PropertyName = "name", Order = 1)]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "type", Order = 2)]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "required", Order = 3)]
        public bool Required { get; set; }
    }

    internal class ODataSingleContent
    {
        [JsonProperty(PropertyName = "d", Order = 1)]
        public Dictionary<string, object> FieldData { get; set; }
    }
    internal class ODataMultipleContent
    {
        [JsonProperty(PropertyName = "d", Order = 1)]
        public Dictionary<string, object> Contents { get; private set; }
        public static ODataMultipleContent Create(IEnumerable<Dictionary<string, object>> data, int count)
        {
            var array = data.ToArray();
            var dict = new Dictionary<string, object>
            {
                {"__count", count == 0 ? array.Length : count},
                {"results", array}
            };
            return new ODataMultipleContent { Contents = dict };
        }
    }

    internal class ODataReference
    {
        [JsonProperty(PropertyName = "__deferred", Order = 1)]
        public ODataUri Reference { get; private set; }
        public static ODataReference Create(string uri)
        {
            return new ODataReference { Reference = new ODataUri { Uri = uri } };
        }
    }

    internal class ODataUri
    {
        [JsonProperty(PropertyName = "uri", Order = 1)]
        public string Uri { get; set; }
    }
    internal class ODataBinary
    {
        [JsonProperty(PropertyName = "__mediaresource", Order = 1)]
        public ODataMediaResource Resource { get; private set; }
        public static ODataBinary Create(string url, string editUrl, string mimeType, string etag)
        {
            return new ODataBinary { Resource = new ODataMediaResource { EditMediaUri = editUrl, MediaUri = url, MimeType = mimeType, MediaETag = etag } };
        }
    }
    internal class ODataMediaResource
    {
        [JsonProperty(PropertyName = "edit_media", Order = 1)]
        public string EditMediaUri { get; set; }
        [JsonProperty(PropertyName = "media_src", Order = 2)]
        public string MediaUri { get; set; }
        [JsonProperty(PropertyName = "content_type", Order = 3)]
        public string MimeType { get; set; }
        [JsonProperty(PropertyName = "media_etag", Order = 4)]
        public string MediaETag { get; set; }
    }

    public class ODataActionItem
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public int Index { get; set; }
        public string Icon { get; set; }
        public string Url { get; set; }
        public int IncludeBackUrl { get; set; }
        public bool ClientAction { get; set; }
        public bool Forbidden { get; set; }
    }

    public class Error
    {
        // {
        //    "error": {
        //        "code": "NotSpecified",
        //        "exceptiontype": "SenseNetSecurityException",
        //        "message": {
        //            "lang": "en-us",
        //            "value": "Access denied. Path: /Root/Sites/Default_Site/workspaces/Document/londondocumentworkspace PermissionType: RecallOldVersion User: BuiltIn\Visitor UserId: 6"
        //        },
        //        "innererror": {
        //            "trace": "ODataException: Access denied. Path: /Root/Sites/Default_Site/workspaces/Document/londondocumentworkspace PermissionType: RecallOldVersion User: BuiltIn\\Visitor UserId: 6\\n\\n---- Inner Exception:\\nSenseNetSecurityException: Access denied. Path: /Root/Sites/Default_Site/workspaces/Document/londondocumentworkspace PermissionType: RecallOldVersion User: BuiltIn\\Visitor UserId: 6\\n   at SenseNet.ContentRepository.Storage.Security.SecurityHandler.GetAccessDeniedException(String path, Int32 creatorId, Int32 lastModifierId, String message, PermissionType[] permissionTypes, IUser user) in C:\\Dev10\\SenseNet\\Development\\Budapest\\Source\\SenseNet\\Storage\\Security\\SecurityHandler.cs:line 1055\\n   at SenseNet.ContentRepository.Storage.Security.SecurityHandler.Assert(String path, Int32 creatorId, Int32 lastModifierId, String message, PermissionType[] permissionTypes) in C:\\Dev10\\SenseNet\\Development\\Budapest\\Source\\SenseNet\\Storage\\Security\\SecurityHandler.cs:line 383\\n   at SenseNet.ContentRepository.Storage.Security.SecurityHandler.Assert(Node node, PermissionType[] permissionTypes) in C:\\Dev10\\SenseNet\\Development\\Budapest\\Source\\SenseNet\\Storage\\Security\\SecurityHandler.cs:line 353\\n   at SenseNet.ContentRepository.Storage.Security.SecurityHandler.Assert(PermissionType[] permissionTypes) in C:\\Dev10\\SenseNet\\Development\\Budapest\\Source\\SenseNet\\Storage\\Security\\SecurityHandler.cs:line 343\\n   at SenseNet.ContentRepository.Storage.Node.LoadVersions() in C:\\Dev10\\SenseNet\\Development\\Budapest\\Source\\SenseNet\\Storage\\Node.cs:line 1772\\n   at SenseNet.ContentRepository.GenericContent.get_Versions() in C:\\Dev10\\SenseNet\\Development\\Budapest\\Source\\SenseNet\\ContentRepository\\GenericContent.cs:line 1237\\n   at SenseNet.ContentRepository.GenericContent.GetProperty(String name) in C:\\Dev10\\SenseNet\\Development\\Budapest\\Source\\SenseNet\\ContentRepository\\GenericContent.cs:line 579\\n   at SenseNet.ContentRepository.Folder.GetProperty(String name) in C:\\Dev10\\SenseNet\\Development\\Budapest\\Source\\SenseNet\\ContentRepository\\Folder.cs:line 51\\n   at SenseNet.ContentRepository.Workspaces.Workspace.GetProperty(String name) in C:\\Dev10\\SenseNet\\Development\\Budapest\\Source\\SenseNet\\ContentRepository\\Workspaces\\Workspace.cs:line 118\\n   at SenseNet.ContentRepository.Field.ReadProperty(String propertyName) in C:\\Dev10\\SenseNet\\Development\\Budapest\\Source\\SenseNet\\ContentRepository\\Field.cs:line 194\\n   at SenseNet.ContentRepository.Field.ReadProperties() in C:\\Dev10\\SenseNet\\Development\\Budapest\\Source\\SenseNet\\ContentRepository\\Field.cs:line 181\\n   at SenseNet.ContentRepository.Field.get_Value() in C:\\Dev10\\SenseNet\\Development\\Budapest\\Source\\SenseNet\\ContentRepository\\Field.cs:line 136\\n   at SenseNet.ContentRepository.Field.GetData(Boolean localized) in C:\\Dev10\\SenseNet\\Development\\Budapest\\Source\\SenseNet\\ContentRepository\\Field.cs:line 260\\n   at SenseNet.Portal.OData.ODataFormatter.WriteContentProperty(String path, String propertyName, Boolean rawValue, PortalContext portalContext, ODataRequest req) in C:\\Dev10\\SenseNet\\Development\\Budapest\\Source\\SenseNet\\Portal\\OData\\ODataFormatter.cs:line 271\\n   at SenseNet.Portal.OData.ODataHandler.ProcessRequest(HttpContext context, String httpMethod, Stream inputStream) in C:\\Dev10\\SenseNet\\Development\\Budapest\\Source\\SenseNet\\Portal\\OData\\ODataHandler.cs:line 116\\n=====================\\n"
        //        }
        //    }
        // }
        [JsonProperty(PropertyName="code", Order=1)]
        public string Code { get; set; }
        [JsonProperty(PropertyName = "exceptiontype", Order = 2)]
        public string ExceptionType { get; set; }
        [JsonProperty(PropertyName = "message", Order = 3)]
        public ErrorMessage Message { get; set; }
        [JsonProperty(PropertyName = "innererror", Order = 4)]
        public StackInfo InnerError { get; set; }
    }
    public class ErrorMessage
    {
        [JsonProperty(PropertyName = "lang", Order = 1)]
        public string Lang { get; set; }
        [JsonProperty(PropertyName = "value", Order = 1)]
        public string Value { get; set; }
    }
    public class StackInfo
    {
        [JsonProperty(PropertyName = "trace", Order = 1)]
        public string Trace { get; set; }
    }

}
