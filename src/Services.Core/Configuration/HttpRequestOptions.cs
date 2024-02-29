using SenseNet.Tools.Configuration;

namespace SenseNet.Services.Core.Configuration
{
    /// <summary>
    /// Options for configuring how the OData layer handles HTTP requests.
    /// </summary>
    [OptionsClass(sectionName: "sensenet:HttpRequest")]
    public class HttpRequestOptions
    {
        /// <summary>
        /// The maximum size of the request body in bytes.
        /// </summary>
        public long MaxRequestBodySize { get; set; }
    }
}
