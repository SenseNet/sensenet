using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class Contact
    {
        [JsonProperty("name")]  public string Name { get; set; }
        [JsonProperty("url")]   public string Url { get; set; }
        [JsonProperty("email")] public string Email { get; set; }
    }
}
