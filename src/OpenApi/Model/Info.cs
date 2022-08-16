using System;
using Newtonsoft.Json;

namespace SenseNet.OpenApi.Model
{
    public class Info
    {
        [JsonProperty("title")]          public string Title { get; set; }
        [JsonProperty("description")]    public string Description { get; set; }
        [JsonProperty("termsOfService")] public string TermsOfService { get; set; }
        [JsonProperty("contact")]        public Contact Contact { get; set; }
        [JsonProperty("license")]        public License License { get; set; }
        [JsonProperty("version")]        public Version Version { get; set; }
    }
}
