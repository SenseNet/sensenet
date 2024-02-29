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

        /// <summary>
        /// The default user for internal clients. When creating a new client, 
        /// this user will be used if no other value was provided.
        /// Default value is "builtin\\admin".
        /// </summary>
        public string DefaultClientUserInternal { get; set; } = "builtin\\admin";
        /// <summary>
        /// The default user for external clients. When creating a new client,
        /// this user will be used if no other value was provided.
        /// Default value is "builtin\\publicadmin".
        /// </summary>
        public string DefaultClientUserExternal { get; set; } = "builtin\\publicadmin";
    }
}
