using System;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Components;
using SenseNet.ContentRepository.Security.Clients;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Storage.Data.MsSqlClient;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class MsSqlExtensions
    {
        /// <summary>
        /// Adds all MS SQL implementations to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetMsSqlProviders(this IServiceCollection services,
            Action<ConnectionStringOptions> configureConnectionStrings = null,
            Action<MsSqlDatabaseInstallationOptions> configureInstallation = null,
            Action<DataOptions> configureDataOptions = null)
        {
            return services.AddSenseNetMsSqlDataProvider()
                    .AddSingleton<ISharedLockDataProvider, MsSqlSharedLockDataProvider>()
                    .AddSingleton<IExclusiveLockDataProvider, MsSqlExclusiveLockDataProvider>()
                    .AddSingleton<IAccessTokenDataProvider, MsSqlAccessTokenDataProvider>()
                    .AddSingleton<IPackagingDataProvider, MsSqlPackagingDataProvider>()
                    .AddSenseNetMsSqlStatisticalDataProvider()
                    .AddDatabaseAuditEventWriter()
                    .AddSenseNetMsSqlClientStoreDataProvider()
                    .AddComponent<MsSqlExclusiveLockComponent>()
                    .AddComponent<MsSqlStatisticsComponent>()
                    .AddComponent<MsSqlClientStoreComponent>()

                    .Configure<ConnectionStringOptions>(options => { configureConnectionStrings?.Invoke(options); })
                    .Configure<MsSqlDatabaseInstallationOptions>(options => { configureInstallation?.Invoke(options); })
                    .Configure<DataOptions>(options => { configureDataOptions?.Invoke(options); })
                ;
        }

        /// <summary>
        /// Adds the default MS SQL data provider to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetMsSqlDataProvider(this IServiceCollection services)
        {
            return services.AddSenseNetDataProvider<MsSqlDataProvider>()
                .AddSenseNetDataInstaller<MsSqlDataInstaller>()
                .AddSingleton<MsSqlDatabaseInstaller>()
                .Configure<MsSqlDatabaseInstallationOptions>(_ =>
                {
                    // this method is for making sure that the option object is registered
                });
        }

        /// <summary>
        /// Adds the MS SQL statistical data provider to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetMsSqlStatisticalDataProvider(this IServiceCollection services)
        {
            return services.AddStatisticalDataProvider<MsSqlStatisticalDataProvider>();
        }
        /// <summary>
        /// Adds the MS SQL ClientStore data provider to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetMsSqlClientStoreDataProvider(this IServiceCollection services)
        {
            return services.AddSenseNetClientStoreDataProvider<MsSqlClientStoreDataProvider>();
        }

        //========================================================= Legacy extensions for IRepositoryBuilder

        /// <summary>
        /// Sets an <see cref="MsSqlExclusiveLockDataProvider"/> as the current
        /// <see cref="IExclusiveLockDataProvider"/> instance that will be responsible
        /// for managing exclusive locks.
        /// </summary>
        /// <param name="builder">The IRepositoryBuilder instance.</param>
        /// <returns>The updated IRepositoryBuilder.</returns>
        [Obsolete("Do not use this method anymore. Register MsSqlExclusiveLockDataProvider as a service instead.", true)]
        public static IRepositoryBuilder UseMsSqlExclusiveLockDataProvider(this IRepositoryBuilder builder)
        {
            return builder;
        }
    }
}
