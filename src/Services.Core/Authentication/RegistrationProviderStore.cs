using System;
using System.Collections.Generic;

namespace SenseNet.Services.Core.Authentication
{
    internal class RegistrationProviderStore
    {
        private readonly IDictionary<string, IRegistrationProvider> _providers = new Dictionary<string, IRegistrationProvider>();
        private static readonly IRegistrationProvider DefaultProvider = new DefaultRegistrationProvider();

        public void Add(string name, IRegistrationProvider provider)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            _providers[name] = provider ?? throw new ArgumentNullException(nameof(provider));
        }
        public IRegistrationProvider Get(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            return _providers.TryGetValue(name, out var provider)
                ? provider
                : DefaultProvider;
        }
    }
}
