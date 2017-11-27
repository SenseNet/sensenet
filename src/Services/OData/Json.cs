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

    /// <summary>
    /// Represents an item in the Actions list of the OData Content metadata.
    /// </summary>
    public class ODataActionItem
    {
        /// <summary>
        /// Gets or sets the name of the Action.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the human readable name of the Action.
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// Gets os sets the value that helps to sorting the items.
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// Gets or sets the icon name of the Action.
        /// </summary>
        public string Icon { get; set; }
        /// <summary>
        /// Gets or sets the URL of the Action.
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// Gets or sets a value that is true if the URL contains a back URL.
        /// </summary>
        public int IncludeBackUrl { get; set; }
        //UNDONE: XMLDOC: ODataActionItem.ClientAction
        public bool ClientAction { get; set; }
        /// <summary>
        /// Gets or sets a value that is true if the Action is an <see cref="ODataOperation"/>.
        /// </summary>
        public bool IsODataAction { get; set; }
        /// <summary>
        /// Gets or sets the parameter names of the Action.
        /// </summary>
        public string[] ActionParameters { get; set; }
        /// <summary>
        /// Gets or sets the scenarios when the Action can be applied.
        /// </summary>
        public string[] Scenarios { get; set; }
        /// <summary>
        /// Gets or sets a value that is true if the Action is
        /// visible but not executable for the current user.
        /// </summary>
        public bool Forbidden { get; set; }
    }

    /// <summary>
    /// Represents a JSON-serializable OData error.
    /// </summary>
    /// <example>
    /// The general message format is the following (JSON):
    /// <code>
    /// {
    ///    "error": {
    ///        "code": "NotSpecified",
    ///        "exceptiontype": "SenseNetSecurityException",
    ///        "message": {
    ///            "lang": "en-us",
    ///            "value": "Access denied. Path: /Root/...
    ///        },
    ///        "innererror": {
    ///            "trace": "ODataException: Access denied. Path: /Root/...
    ///        }
    ///    }
    /// }
    /// </code>
    /// </example>
    public class Error
    {
        /// <summary>
        /// Gets or sets the code of the error.
        /// </summary>
        [JsonProperty(PropertyName="code", Order=1)]
        public string Code { get; set; }
        /// <summary>
        /// Gets or sets the (not fully qualified) type name of the exception.
        /// </summary>
        [JsonProperty(PropertyName = "exceptiontype", Order = 2)]
        public string ExceptionType { get; set; }
        /// <summary>
        /// Gets or sets the message of the OData error.
        /// </summary>
        [JsonProperty(PropertyName = "message", Order = 3)]
        public ErrorMessage Message { get; set; }
        /// <summary>
        /// Gets or sets all information for debugger users.
        /// Contains the whole exception chain with messages and stack traces.
        /// </summary>
        [JsonProperty(PropertyName = "innererror", Order = 4)]
        public StackInfo InnerError { get; set; }
    }
    /// <summary>
    /// Represents the message of the OData error.
    /// </summary>
    public class ErrorMessage
    {
        /// <summary>
        /// Gets or sets the language code of the message (e.g. en-us).
        /// </summary>
        [JsonProperty(PropertyName = "lang", Order = 1)]
        public string Lang { get; set; }
        /// <summary>
        /// Gets or sets the message of the OData error.
        /// </summary>
        [JsonProperty(PropertyName = "value", Order = 1)]
        public string Value { get; set; }
    }
    /// <summary>
    /// Represents a stack trace information of the OData error.
    /// </summary>
    public class StackInfo
    {
        /// <summary>
        /// Gets or sets the stack trace information of an OData error.
        /// </summary>
        [JsonProperty(PropertyName = "trace", Order = 1)]
        public string Trace { get; set; }
    }

}
