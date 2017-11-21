using Newtonsoft.Json;

namespace SenseNet.Portal.OData
{
    internal class ErrorContent
    {
        [JsonProperty(PropertyName = "content", Order = 1)]
        public object Content { get; set; }

        [JsonProperty(PropertyName = "error", Order = 2)]
        public Error Error { get; set; }
    }
}