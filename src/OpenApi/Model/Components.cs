using System.Collections.Generic;
using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class Components
    {
        [JsonProperty("schemas")]         public IDictionary<string, Schema> Schemas { get; set; }
        [JsonProperty("responses")]       public IDictionary<string, Response> Responses { get; set; }
        [JsonProperty("parameters")]      public IDictionary<string, Parameter> Parameters { get; set; }
        [JsonProperty("examples")]        public IDictionary<string, Example>  Examples { get; set; }
        [JsonProperty("requestBodies")]   public IDictionary<string, RequestBody> RequestBodies { get; set; }
        [JsonProperty("headers")]         public IDictionary<string, Header> Headers { get; set; }
        [JsonProperty("securitySchemes")] public IDictionary<string, SecurityScheme> SecuritySchemes { get; set; }
        [JsonProperty("links")]           public IDictionary<string, Link> Links { get; set; }
        [JsonProperty("callbacks")]       public IDictionary<string, CallBack> Callbacks { get; set; }
    }
}