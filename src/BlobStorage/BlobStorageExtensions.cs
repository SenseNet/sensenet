using System;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class BlobStorageExtensions
    {
        [Obsolete("Register blob providers as services instead.", true)]
        /// <summary>
        /// Set the external blob provider to be used by the built-in blob provider selector
        /// during write operations when the binary size exceeds a configured value.
        /// </summary>
        public static IRepositoryBuilder UseExternalBlobProvider(this IRepositoryBuilder builder, IBlobProvider provider)
        {
            if (provider == null)
                return builder;

            //UNDONE: [DIBLOB] create extension method for registering ExternalBlobProvider as a service
            //BuiltInBlobProviderSelector.ExternalBlobProvider = provider;
            
            return builder;
        }
    }
}
