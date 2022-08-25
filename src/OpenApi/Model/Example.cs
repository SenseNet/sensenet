using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class Example
    {
        [JsonProperty("$ref")]          public string Ref { get; set; }

        [JsonProperty("summary")]       public string Summary { get; set; }
        [JsonProperty("description")]   public string Description { get; set; }
        [JsonProperty("value")]         public string Value { get; set; } // original specification: "any"
        [JsonProperty("externalValue")] public string ExternalValue { get; set; }
    }
}