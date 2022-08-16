using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class Link
    {
        [JsonProperty("$ref")] public string Ref { get; set; }
        //TODO: Link or reference
    }
}