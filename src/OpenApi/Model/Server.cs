using System.Collections.Generic;
using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class Server
    {
        [JsonProperty("url")]         public string Url { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("variables")]   public IDictionary<string, ServerVariable> Variables { get; set; }
    }
}