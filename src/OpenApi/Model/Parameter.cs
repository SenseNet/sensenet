using System.Collections.Generic;
using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class Parameter
    {
        [JsonProperty("$ref")]            public string Ref { get; set; }

        [JsonProperty("name")]            public string Name { get; set; }
        /// <summary>
        /// REQUIRED. The location of the parameter. Possible values are "query", "header", "path" or "cookie".
        /// </summary>
        [JsonProperty("in")]              public string In { get; set; }
        [JsonProperty("description")]     public string Description { get; set; }
        [JsonProperty("required")]        public bool? Required { get; set; }
        [JsonProperty("deprecated")]      public bool? Deprecated { get; set; }
        [JsonProperty("allowEmptyValue")] public bool? AllowEmptyValue { get; set; }

        /* ------------------------------------------------------------------------------------------------------ */

        [JsonProperty("style")]           public string Style { get; set; }
        [JsonProperty("explode")]         public bool? Explode { get; set; }
        [JsonProperty("allowReserved")]   public bool? AllowReserved { get; set; }
        [JsonProperty("schema")]          public Schema Schema { get; set; }
        [JsonProperty("example")]         public string Example { get; set; } // original specification: "any"
        [JsonProperty("examples")]        public IDictionary<string, Example> Examples { get; set; }


        /* ------------------------------------------------------------------------------------------------------ */

        [JsonProperty("content")]         public IDictionary<string, MediaType> Content { get; set; }
    }
}