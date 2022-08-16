using System.Collections.Generic;
using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class Encoding
    {
        [JsonProperty("contentType")]   public string ContentType { get; set; }
        [JsonProperty("headers")]       public IDictionary<string, Header> Headers { get; set; }
        [JsonProperty("style")]         public string Style { get; set; }
        [JsonProperty("explode")]       public bool? Explode { get; set; }
        [JsonProperty("allowReserved")] public bool? AllowReserved { get; set; }
    }
}