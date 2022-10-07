using System;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Communication.Messaging;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Storage.DistributedApplication.Messaging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class StorageExtensions
    {
        /// <summary>
        /// Adds the default blob infrastructure to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetBlobStorage(this IServiceCollection services)
        {
            services.AddSingleton<IBlobProviderStore, BlobProviderStore>();
            services.AddSingleton<IBlobStorage, BlobStorage>();
            services.AddSingleton<IExternalBlobProviderFactory, NullExternalBlobProviderFactory>();
            
            services.AddSenseNetBlobProvider<BuiltInBlobProvider>();
            services.AddSingleton<IBlobProviderSelector, BuiltInBlobProviderSelector>();
            
            // default implementation is MS SQL
            services.AddSenseNetBlobStorageMetaDataProvider<MsSqlBlobMetaDataProvider>();

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
        
        /// <summary>
        /// Adds a data provider to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetDataProvider<T>(this IServiceCollection services)
            where T : DataProvider
        {
            return services.AddSingleton<DataProvider, T>()
                .AddSenseNetDataStore<DataStore>();
        }

        /// <summary>
        /// Registers a custom data store in the service collection. You only have to call this
        /// if you need to replace the default data store implementation.
        /// </summary>
        /// <typeparam name="T">A custom <see cref="IDataStore"/> implementation.</typeparam>
        public static IServiceCollection AddSenseNetDataStore<T>(this IServiceCollection services) where T : class, IDataStore
        {
            return services.AddSingleton<IDataStore, T>();
        }
        
        /// <summary>
        /// Adds a data installer to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetDataInstaller<T>(this IServiceCollection services) where T : class, IDataInstaller
        {
            return services.AddSingleton<IDataInstaller, T>();
        }

        /// <summary>
        /// Adds a message provider to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetMessaging<T>(this IServiceCollection services,
            Action<ClusterMemberInfo> configure = null) where T: class, IClusterChannel
        {
            services
                .AddSingleton<IClusterMessageFormatter, SnMessageFormatter>()
                .AddSingleton<IClusterChannel, T>()
                .AddSenseNetBackgroundService<ClusterChannelMonitor>()
                .Configure<ClusterMemberInfo>(cmi => { configure?.Invoke(cmi); });

            return services;
        }
        
        /// <summary>
        /// Adds an ISnService implementation to the service collection. These services
        /// will be started and stopped during repository startup and shutdown.
        /// </summary>
        public static IServiceCollection AddSenseNetBackgroundService<T>(this IServiceCollection services) where T : class, ISnService
        {
            return services.AddSingleton<ISnService, T>();
        }
    }
}
