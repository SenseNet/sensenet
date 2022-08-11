using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class Link
    {
        [JsonProperty("$ref")] public string Ref { get; set; }
        //UNDONE: Link or reference
    }
}