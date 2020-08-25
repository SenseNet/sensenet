using System;
using System.Collections.Generic;
using System.Linq;

namespace SenseNet.Services.Core.Authentication
{
    /// <summary>
    /// Central in-memory storage for registration providers.
    /// There will be a singleton instance of this class in an application
    /// that will be registered during app start.
    /// </summary>
    internal class RegistrationProviderStore
    {
        private readonly IDictionary<string, IRegistrationProvider> _providers = new Dictionary<string, IRegistrationProvider>();
        private readonly DefaultRegistrationProvider _defaultProvider;

        public RegistrationProviderStore(DefaultRegistrationProvider defaultProvider, IEnumerable<IRegistrationProvider> registeredProviders)
        {
            _defaultProvider = defaultProvider;

            foreach (var provider in registeredProviders.Where(p => !string.IsNullOrEmpty(p?.Name)))
            {
                _providers[provider.Name] = provider;
            }
        }

        public IRegistrationProvider Get(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            return _providers.TryGetValue(name, out var provider)
                ? provider
                : _defaultProvider;
        }
    }
}
