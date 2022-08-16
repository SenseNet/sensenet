using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class ServerVariable
    {
        [JsonProperty("enum")]        public string[] Values { get; set; }
        [JsonProperty("default")]     public string DefaultValue { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
    }
}