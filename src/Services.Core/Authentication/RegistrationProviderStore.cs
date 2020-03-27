using System;
using System.Collections.Generic;

namespace SenseNet.Services.Core.Authentication
{
    /// <summary>
    /// Central in-memory storage for registration providers.
    /// There will be a singleton instance of this class in an application
    /// that will be registered during app start.
    /// </summary>
    internal class RegistrationProviderStore
    {
        private readonly IDictionary<string, IRegistrationProvider> Providers = new Dictionary<string, IRegistrationProvider>();
        private readonly DefaultRegistrationProvider DefaultProvider = new DefaultRegistrationProvider();

        public RegistrationProviderStore(RegistrationOptions options)
        {
            DefaultProvider.DefaultGroups = options.Groups;
            DefaultProvider.DefaultUserType = options.UserType;            
        }

        public void Add(string name, IRegistrationProvider provider)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            Providers[name] = provider ?? throw new ArgumentNullException(nameof(provider));
        }
        public IRegistrationProvider Get(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            return Providers.TryGetValue(name, out var provider)
                ? provider
                : DefaultProvider;
        }
    }
}
