using System;
using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class PathItem
    {
        [JsonProperty("$ref")]        public string Ref { get; set; }

        [JsonProperty("summary")]     public string Summary { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("parameters")]  public Parameter[] Parameters { get; set; }

        [JsonProperty("get")]         public Operation Get { get; set; }
        [JsonProperty("patch")]       public Operation Patch { get; set; }
        [JsonProperty("post")]        public Operation Post { get; set; }
        [JsonProperty("delete")]      public Operation Delete { get; set; }
        [JsonProperty("options")]     public Operation Options { get; set; }
        [JsonProperty("head")]        public Operation Head { get; set; }
        [JsonProperty("put")]         public Operation Put { get; set; }
    }
}
