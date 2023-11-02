using SenseNet.Tools.Configuration;

namespace SenseNet.Services.Core.Configuration
{
    [OptionsClass(sectionName: "sensenet:HttpRequest")]
    public class HttpRequestOptions
    {
        public long MaxRequestBodySize { get; set; }
    }
}
