using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SenseNet.ContentRepository.Security.Clients
{
    [Flags]
    public enum ClientType
    {
        ExternalClient = 1,
        ExternalSpa = 2,
        InternalClient = 4,
        InternalSpa = 8,
        AdminUi = 16,
        All = ExternalClient | ExternalSpa | InternalClient | InternalSpa | AdminUi,
        AllExternal = ExternalClient | ExternalSpa,
        AllInternal = InternalClient | InternalSpa | AdminUi,
        AllClient = ExternalClient | InternalClient,
        AllSpa = ExternalSpa | InternalSpa
    }

    public class Client
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("repository")]
        public string Repository { get; set; }
        [JsonProperty("clientId")]
        public string ClientId { get; set; }
        [JsonProperty("userName")]
        public string UserName { get; set; }
        [JsonProperty("authority")]
        public string Authority { get; set; }
        [JsonProperty("type"), JsonConverter(typeof(StringEnumConverter))]
        public ClientType Type { get; set; }
        [JsonProperty("secrets")]
        public List<ClientSecret> Secrets { get; set; } = new List<ClientSecret>();

        public string GetFirstValidSecret()
        {
            return Secrets.FirstOrDefault(s => s.ValidTill > DateTime.UtcNow)?.Value;
        }

        public Client Clone()
        {
            return new Client
            {
                Name = Name,
                Repository = Repository,
                ClientId = ClientId,
                UserName = UserName,
                Authority = Authority,
                Type = Type,
                Secrets = Secrets.Select(x => x.Clone()).ToList()
            };
        }
    }
}
