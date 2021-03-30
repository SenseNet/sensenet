using System;
using SenseNet.ContentRepository.Storage.Data;
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
        [Obsolete("Register blob providers as services instead.", true)]
        public static IRepositoryBuilder UseExternalBlobProvider(this IRepositoryBuilder builder, IBlobProvider provider)
        {
            return builder;
        }
    }
}
