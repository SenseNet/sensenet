using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;

namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// <inheritdoc cref="IBlobProviderStore"/>
    /// </summary>
    public class BlobProviderStore : Dictionary<string, IBlobProvider>, IBlobProviderStore
    {
        private IBuiltInBlobProvider _builtInProvider;
        public IBuiltInBlobProvider BuiltInBlobProvider
        {
            get
            {
                return _builtInProvider ?? (_builtInProvider =
                           (IBuiltInBlobProvider)Values.FirstOrDefault(p => p is IBuiltInBlobProvider));
            }
        }

        public BlobProviderStore(IEnumerable<IBlobProvider> providers)
        : base(providers.ToDictionary(bp => bp.GetType().FullName, bp => bp))
        {
            
        }

        public void AddProvider(IBlobProvider provider)
        {
            this[provider?.GetType().FullName ?? throw new ArgumentNullException(nameof(provider))] = provider;
        }
        public IBlobProvider GetProvider(string providerName)
        {
            if (providerName == null)
                return BuiltInBlobProvider;
            if (TryGetValue(providerName, out var provider))
                return provider;

            throw new InvalidOperationException("BlobProvider not found: '" + providerName + "'.");
        }
    }
}
