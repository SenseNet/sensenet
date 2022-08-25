using System.Collections.Generic;
using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class Response
    {
        [JsonProperty("$ref")]        public string Ref { get; set; }

        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("headers")]     public IDictionary<string, Header> Headers { get; set; }
        [JsonProperty("content")]     public IDictionary<string, MediaType> Content { get; set; }
        [JsonProperty("links")]       public IDictionary<string, Link> Links { get; set; }
    }
}