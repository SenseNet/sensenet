using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Diagnostics;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class BlobStorageExtensions
    {
        /// <summary>
        /// Set the external blob provider to be used by the built-in blob provider selector
        /// during write operations when the binary size exceeds a configured value.
        /// </summary>
        public static IRepositoryBuilder UseExternalBlobProvider(this IRepositoryBuilder builder, IBlobProvider provider)
        {
            if (provider == null)
                return builder;

            BuiltInBlobProviderSelector.ExternalBlobProvider = provider;

            // we have to add this manually configured provider to the automatically collected list
            if (BlobStorageBase.Providers == null)
                BlobStorageBase.Providers = new Dictionary<string, IBlobProvider>();

            // ReSharper disable once AssignNullToNotNullAttribute
            BlobStorageBase.Providers[provider.GetType().FullName] = provider;

            SnTrace.System.Write($"External blob provider configured: {provider.GetType().FullName}.");

            return builder;
        }
    }
}
