using System.Collections.Generic;
using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class MediaType
    {
        //[JsonProperty("$ref")] public string Ref { get; set; }

        [JsonProperty("schema")]   public Schema Schema { get; set; }
        [JsonProperty("example")]  public string Example { get; set; } // original specification: "any"
        [JsonProperty("examples")] public IDictionary<string, Example> Examples { get; set; }
        [JsonProperty("encoding")] public IDictionary<string, Encoding> Encoding { get; set; }
    }
}