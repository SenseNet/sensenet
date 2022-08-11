using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class SecurityRequirement
    {
        [JsonProperty("name")] public string[] Name { get; set; }
    }
}