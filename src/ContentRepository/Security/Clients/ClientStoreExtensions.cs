using Microsoft.Extensions.DependencyInjection;
using SenseNet.ContentRepository.Security.Clients;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class ClientStoreExtensions
    {
        /// <summary>
        /// Adds the provided client manager to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetClientManager<T>(this IServiceCollection services) where T : class, IClientManager
        {
            services.AddSingleton<ClientStore, ClientStore>();

            return services.AddSingleton<IClientManager, T>();
        }
        /// <summary>
        /// Adds the default client manager to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetDefaultClientManager(this IServiceCollection services)
        {
            return services.AddSenseNetClientManager<DefaultClientManager>();
        }
        /// <summary>
        /// Adds the provided ClientStore data provider to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetClientStoreDataProvider<T>(this IServiceCollection services) 
            where T : class, IClientStoreDataProviderExtension
        {
            return services.AddSingleton<IClientStoreDataProviderExtension, T>();
        }
        /// <summary>
        /// Adds the MS SQL ClientStore data provider to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetMsSqlClientStoreDataProvider(this IServiceCollection services)
        {
            return services.AddSenseNetClientStoreDataProvider<MsSqlClientStoreDataProviderExtension>();
        }
        /// <summary>
        /// Adds the in-memory ClientStore data provider to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetInMemoryClientStoreDataProvider(this IServiceCollection services)
        {
            return services.AddSenseNetClientStoreDataProvider<InMemoryClientStoreDataProviderExtension>();
        }
    }
}
