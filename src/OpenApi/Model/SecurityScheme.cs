using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class SecurityScheme
    {
        [JsonProperty("$ref")]             public string Ref { get; set; }

        [JsonProperty("type")]             public string Type { get; set; }
        [JsonProperty("description")]      public string Description { get; set; }
        [JsonProperty("name")]             public string Name { get; set; }
        /// <summary>
        /// REQUIRED. The location of the API key. Valid values are "query", "header" or "cookie".
        /// </summary>
        [JsonProperty("in")]               public string In { get; set; }
        [JsonProperty("scheme")]           public string Scheme { get; set; }
        [JsonProperty("bearerFormat")]     public string BearerFormat { get; set; }
        [JsonProperty("flows")]            public OAuthFlows Flows { get; set; }
        [JsonProperty("openIdConnectUrl")] public string OpenIdConnectUrl { get; set; }
    }
}