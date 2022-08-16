using System.Collections.Generic;
using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class Operation
    {
        [JsonProperty("$ref")]         public string Ref { get; set; }

        [JsonProperty("tags")]         public string[] Tags { get; set; }
        [JsonProperty("summary")]      public string Summary { get; set; }
        [JsonProperty("description")]  public string Description { get; set; }
        [JsonProperty("externalDocs")] public ExternalDocumentation ExternalDocs { get; set; }
        [JsonProperty("operationId")]  public string OperationId { get; set; }
        [JsonProperty("parameters")]   public Parameter[] Parameters { get; set; }
        [JsonProperty("requestBody")]  public RequestBody RequestBody { get; set; }
        [JsonProperty("responses")]    public IDictionary<string, Response> Responses { get; set; }
        [JsonProperty("callbacks")]    public IDictionary<string, CallBack> Callbacks { get; set; }
        [JsonProperty("deprecated")]   public bool? Deprecated { get; set; }
        [JsonProperty("security")]     public SecurityRequirement[] Security { get; set; }
        [JsonProperty("servers")]      public Server[] Servers { get; set; }
    }
}