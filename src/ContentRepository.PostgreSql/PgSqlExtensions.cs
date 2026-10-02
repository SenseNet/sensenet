using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Components;
using SenseNet.ContentRepository.Security.Clients;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.ContentRepository.Storage.Data.PgSqlClient;
using SenseNet.ContentRepository.Storage.Data.PgSqlClient.Security;
using SenseNet.Security;
using SenseNet.Storage.Data.PgSqlClient;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class PgSqlExtensions
    {
        /// <summary>
        /// Adds PostgreSQL implementations of data related services to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetPgSqlProviders(this IServiceCollection services,
            Action<ConnectionStringOptions> configureConnectionStrings = null,
            Action<PgSqlDatabaseInstallationOptions> configureInstallation = null,
            Action<DataOptions> configureDataOptions = null)
        {
            return services.AddSenseNetPgSqlDataProvider()
                    .AddSingleton<ISharedLockDataProvider, PgSqlSharedLockDataProvider>()
                    .AddSingleton<IExclusiveLockDataProvider, PgSqlExclusiveLockDataProvider>()
                    .AddSingleton<IAccessTokenDataProvider, PgSqlAccessTokenDataProvider>()
                    .AddSingleton<IPackagingDataProvider, PgSqlPackagingDataProvider>()
                    .AddSenseNetPgSqlStatisticalDataProvider()
                    .AddDatabaseAuditEventWriter()
                    .AddSenseNetPgSqlClientStoreDataProvider()
                    .AddComponent<PgSqlExclusiveLockComponent>()
                    .AddComponent<PgSqlStatisticsComponent>()
                    .AddComponent<PgSqlClientStoreComponent>()

                    // Override blob storage providers with PostgreSQL implementations
                    .AddSingleton<IBlobStorageMetaDataProvider, PgSqlBlobMetaDataProvider>()
                    .AddSingleton<IBuiltInBlobProvider, PgSqlBuiltInBlobProvider>()
                    // Replace the MSSQL BuiltInBlobProvider with the PgSql one in the IBlobProvider
                    // collection. BlobProviderStore resolves providers via IBlobProvider, and
                    // IBuiltInBlobProvider is found by scanning that collection. Without this,
                    // the MSSQL BuiltInBlobProvider (registered by AddSenseNetBlobStorage) would
                    // be used for WriteChunk, leading to "Keyword not supported: 'host'" errors.
                    .RemoveAll<IBlobProvider>()
                    .AddSenseNetBlobProvider<PgSqlBuiltInBlobProvider>()

                    .Configure<ConnectionStringOptions>(options => { configureConnectionStrings?.Invoke(options); })
                    .Configure<PgSqlDatabaseInstallationOptions>(options => { configureInstallation?.Invoke(options); })
                    .Configure<DataOptions>(options => { configureDataOptions?.Invoke(options); })
                ;
        }

        /// <summary>
        /// Adds the default PostgreSQL data provider to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetPgSqlDataProvider(this IServiceCollection services)
        {
            return services.AddSenseNetDataProvider<PgSqlDataProvider>()
                .AddSenseNetDataInstaller<PgSqlDataInstaller>()
                .AddSingleton<PgSqlDatabaseInstaller>()
                .Configure<PgSqlDatabaseInstallationOptions>(_ =>
                {
                    // this method is for making sure that the option object is registered
                });
        }

        /// <summary>
        /// Adds the PostgreSQL statistical data provider to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetPgSqlStatisticalDataProvider(this IServiceCollection services)
        {
            return services.AddStatisticalDataProvider<PgSqlStatisticalDataProvider>();
        }

        /// <summary>
        /// Adds the PostgreSQL ClientStore data provider to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetPgSqlClientStoreDataProvider(this IServiceCollection services)
        {
            return services.AddSenseNetClientStoreDataProvider<PgSqlClientStoreDataProvider>();
        }

        /// <summary>
        /// Adds the PostgreSQL security data provider to the service collection.
        /// Replaces the SQL Server-only EFCSecurityDataProvider.
        /// </summary>
        public static IServiceCollection AddPgSqlSecurityDataProvider(this IServiceCollection services,
            Action<PgSqlSecurityDataOptions> configure = null)
        {
            if (configure != null)
                services.Configure(configure);

            return services.AddSingleton<ISecurityDataProvider, PgSqlSecurityDataProvider>();
        }
    }
}
