using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class License
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("url")]  public string Url { get; set; }
    }
}