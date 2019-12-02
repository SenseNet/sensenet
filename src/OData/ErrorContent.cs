using Newtonsoft.Json;

namespace SenseNet.OData
{
    public class ErrorContent
    {
        [JsonProperty(PropertyName = "content", Order = 1)]
        public object Content { get; set; }

        [JsonProperty(PropertyName = "error", Order = 2)]
        public Error Error { get; set; }
    }
}