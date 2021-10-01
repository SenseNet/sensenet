using System;
using Newtonsoft.Json;

namespace SenseNet.ContentRepository.Security.Clients
{
    public class ClientSecret
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
        [JsonProperty("creationDate")]
        public DateTime CreationDate { get; set; }
        [JsonProperty("validTill")]
        public DateTime ValidTill { get; set; }

        public ClientSecret Clone()
        {
            return new ClientSecret
            {
                Id = Id,
                Value = Value,
                CreationDate = CreationDate,
                ValidTill = ValidTill,
            };
        }
    }
}
