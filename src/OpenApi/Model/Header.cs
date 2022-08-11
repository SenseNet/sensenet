using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class Header
    {
        [JsonProperty("$ref")] public string Ref { get; set; }
        //UNDONE: Header or reference
    }
}