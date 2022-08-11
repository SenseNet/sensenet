using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class OpenApiDocument
    {
        [JsonProperty("openapi")]      public Version OpenApi { get; set; }
        [JsonProperty("x-generator")]  public string Generator { get; set; }
        [JsonProperty("info")]         public Info Info { get; set; }
        [JsonProperty("servers")]      public Server[] Servers { get; set; }
        [JsonProperty("tags")]         public List<Tag> Tags { get; set; }
        [JsonProperty("paths")]        public IDictionary<string, PathItem> Paths { get; set; }
        [JsonProperty("components")]   public Components Components { get; set; }
        [JsonProperty("externalDocs")] public ExternalDocumentation ExternalDocs { get; set; }
        [JsonProperty("security")]     public IDictionary<string, string[]>[] Security { get; set; }
    }
}
