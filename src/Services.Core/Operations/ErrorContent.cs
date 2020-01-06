using Newtonsoft.Json;

namespace SenseNet.Services.Core.Operations
{

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
        [JsonProperty(PropertyName = "code", Order = 1)]
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

    public class ErrorContent
    {
        [JsonProperty(PropertyName = "content", Order = 1)]
        public object Content { get; set; }

        [JsonProperty(PropertyName = "error", Order = 2)]
        public Error Error { get; set; }
    }
}