using System.Collections.Generic;
using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class Discriminator
    {
        [JsonProperty("propertyName")] public string PropertyName { get; set; }
        [JsonProperty("mapping")]      public IDictionary<string, string> Mapping { get; set; }
    }
}