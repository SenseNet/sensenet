using System.Collections.Generic;
using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class RequestBody
    {
        [JsonProperty("$ref")]        public string Ref { get; set; }

        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("content")]     public IDictionary<string, MediaType> Content { get; set; }
        [JsonProperty("required")]    public bool? Required { get; set; }
    }
}