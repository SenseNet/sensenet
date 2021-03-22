using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;

namespace SenseNet.Extensions.DependencyInjection
{
    public static class StorageExtensions
    {
        /// <summary>
        /// Adds the default blob infrastructure to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetBlobStorage(this IServiceCollection services)
        {
            services.AddSingleton<IBlobStorage, BlobStorage>();
            services.AddSingleton<IExternalBlobProviderFactory, NullExternalBlobProviderFactory>();
            
            services.AddSenseNetBlobProvider<BuiltInBlobProvider>();
            services.AddSingleton<IBlobProviderSelector, BuiltInBlobProviderSelector>();
            
            // default implementation is MS SQL
            services.AddSingleton<IBlobStorageMetaDataProvider, MsSqlBlobMetaDataProvider>();

            return services;
        }
        /// <summary>
        /// Adds a blob provider for loading previously saved binaries.
        /// </summary>
        public static IServiceCollection AddSenseNetBlobProvider<T>(this IServiceCollection services)
            where T : class, IBlobProvider
        {
            return services.AddSingleton<IBlobProvider, T>();
        }
        /// <summary>
        /// Registers the main blob provider that will be used to save binaries above a certain size.
        /// </summary>
        public static IServiceCollection AddSenseNetExternalBlobProvider<T>(this IServiceCollection services) 
            where T : class, IBlobProvider
        {
            // Register the provider type as a regular provider and set it
            // as the primary blob provider.
            services.AddSenseNetBlobProvider<T>();
            services.AddSingleton<IExternalBlobProviderFactory, ExternalBlobProviderFactory<T>>();

            return services;
        }
    }
}
