using SenseNet.Tools.Configuration;

namespace SenseNet.ContentRepository.Security.Clients
{
    // This class is a mirror of some parts of the AuthenticationOptions class in the services layer
    // so that we can have the same property values here without having to configure them twice.
    [OptionsClass(sectionName: "sensenet:Authentication")]
    public class ClientStoreOptions
    {
        /// <summary>
        /// Url of the authentication authority - for example IdentityServer.
        /// </summary>
        public string Authority { get; set; }
        /// <summary>
        /// Repository url.
        /// </summary>
        public string RepositoryUrl { get; set; }

        public string DefaultClientUserInternal { get; set; } = "builtin\\admin";
        public string DefaultClientUserExternal { get; set; } = "builtin\\publicadmin";
    }
}
