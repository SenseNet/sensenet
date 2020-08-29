using System.Collections.Generic;

namespace SenseNet.Services.Core.Authentication.IdentityServer4
{
    public class SnIdentityServerClient
    {
        public string ClientType { get; set; }
        public string ClientId { get; set; }
    }

    public class ClientRequestOptions
    {
        public ICollection<SnIdentityServerClient> Clients { get; } = new List<SnIdentityServerClient>();
    }
}
