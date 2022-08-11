using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class Tag
    {
        [JsonProperty("name")]         public string Name { get; set; }
        [JsonProperty("description")]  public string Description { get; set; }
        [JsonProperty("externalDocs")] public ExternalDocumentation ExternalDocs { get; set; }
    }
}