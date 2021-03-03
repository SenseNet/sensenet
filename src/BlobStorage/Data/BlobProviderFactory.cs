using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;

namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Defines methods for loading the appropriate blob provider
    /// based on the current context or by name.
    /// </summary>
    public interface IBlobProviderFactory
    {
        IBlobProvider GetProvider(long fullSize);
        IBlobProvider GetProvider(string providerName);
    }

    //UNDONE: [DIBLOB]: register new factory class as a service!

    public class BlobProviderFactory : IBlobProviderFactory
    {
        private IBlobProviderSelector ProviderSelector { get; }
        private IBuiltInBlobProvider BuiltInProvider { get; }
        private Dictionary<string, IBlobProvider> Providers { get; }

        protected BlobProviderFactory(
            IBlobProviderSelector providerSelector,
            IBuiltInBlobProvider builtInProvider,
            IEnumerable<IBlobProvider> providers)
        {
            //UNDONE: [DIBLOB] register the IBuiltInBlobProvider that we request here
            //UNDONE: [DIBLOB] register a BlobStorage instance as BlobStorageBase

            ProviderSelector = providerSelector;
            BuiltInProvider = builtInProvider;
            Providers = providers
                //UNDONE: [DIBLOB] add builtin provider to the list or not?
                //.Where(bp => !(bp is IBuiltInBlobProvider))
                .ToDictionary(bp => bp.GetType().FullName, bp => bp);
        }

        public IBlobProvider GetProvider(long fullSize)
        {
            return ProviderSelector.GetProvider(fullSize, Providers, BuiltInProvider);
        }

        /// <summary>
        /// Gets the blob provider instance with the specified name. Default is the built-in provider.
        /// </summary>
        public IBlobProvider GetProvider(string providerName)
        {
            if (providerName == null)
                return BuiltInProvider;
            if (Providers.TryGetValue(providerName, out var provider))
                return provider;
            throw new InvalidOperationException("BlobProvider not found: '" + providerName + "'.");
        }
    }
}
