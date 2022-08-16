using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class ExternalDocumentation
    {
        [JsonProperty("description")]  public string Description { get; set; }
        [JsonProperty("url")]          public string Url { get; set; }
    }
}