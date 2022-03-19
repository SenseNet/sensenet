﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Components;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Lucene29;
using SenseNet.Security;
using SenseNet.Security.EFCSecurityStore;
using SenseNet.Security.Messaging;
using SenseNet.Storage.Data.MsSqlClient;
using SenseNet.Tests.Core.Implementations;

namespace SenseNet.IntegrationTests.Platforms
{
    public class MsSqlPlatform : Platform
    {
        private string _connectionString;
        public string ConnectionString => _connectionString ??= AppConfig.GetConnectionString("SnCrMsSql");

        public override void BuildServices(IConfiguration configuration, IServiceCollection services)
        {
            services
                //.AddSenseNetInstallPackage()
                .AddSenseNet(configuration, (repositoryBuilder, provider) =>
                {
                    repositoryBuilder
                        .UseLogger(provider)
                        .UseLucene29LocalSearchEngine(Path.Combine(Environment.CurrentDirectory, "App_Data",
                            "LocalIndex"))
                        .UseMsSqlExclusiveLockDataProvider();
                })
                .AddEFCSecurityDataProvider(options =>
                {
                    options.ConnectionString = ConnectionStrings.ConnectionString;
                })
                .AddSenseNetMsSqlStatisticalDataProvider()
                .AddSenseNetMsSqlClientStoreDataProvider()
                .AddComponent(provider => new MsSqlExclusiveLockComponent())
                .AddComponent(provider => new MsSqlStatisticsComponent())
                .AddComponent(provider => new MsSqlClientStoreComponent())

                .AddSingleton<ISharedLockDataProvider, MsSqlSharedLockDataProvider>() //UNDONE:TEST: generalize service addition
                .AddSingleton<IExclusiveLockDataProvider, MsSqlExclusiveLockDataProvider>() //UNDONE:TEST: generalize service addition
                .AddSingleton<IBlobProviderSelector, BuiltInBlobProviderSelector>() //UNDONE:TEST: generalize service addition
                .AddSingleton<IAccessTokenDataProvider, MsSqlAccessTokenDataProvider>() //UNDONE:TEST: generalize service addition
                .AddSingleton<IPackagingDataProvider, MsSqlPackagingDataProvider>() //UNDONE:TEST: generalize service addition
                .AddSingleton<ITestingDataProvider, MsSqlTestingDataProvider>() //UNDONE:TEST: generalize service addition
                ;
        }

        public override void OnBeforeGettingRepositoryBuilder(RepositoryBuilder builder)
        {
            //UNDONE: [DIREF]: do not set the global connection string
            ConnectionStrings.ConnectionString = ConnectionString;
            PrepareDatabase();
            base.OnBeforeGettingRepositoryBuilder(builder);
        }
        public override void OnAfterRepositoryStart(RepositoryInstance repository)
        {
            var state = ((IndexManager)Providers.Instance.IndexManager).DistributedIndexingActivityQueue.GetCurrentState();
            ((IndexManager)Providers.Instance.IndexManager).DistributedIndexingActivityQueue._setCurrentExecutionState(IndexingActivityStatus.Startup);
            base.OnAfterRepositoryStart(repository);
        }

        public override DataProvider GetDataProvider(IServiceProvider services)
        {
var xxx = services.GetService<DataProvider>();

            var connOptions = Options.Create(ConnectionStringOptions.GetLegacyConnectionStrings());
            var dbInstallerOptions = Options.Create(new MsSqlDatabaseInstallationOptions());

            return new MsSqlDataProvider(Options.Create(DataOptions.GetLegacyConfiguration()), connOptions,
                dbInstallerOptions,
                new MsSqlDatabaseInstaller(dbInstallerOptions, NullLoggerFactory.Instance.CreateLogger<MsSqlDatabaseInstaller>()),
                new MsSqlDataInstaller(connOptions, NullLoggerFactory.Instance.CreateLogger<MsSqlDataInstaller>()),
                NullLoggerFactory.Instance.CreateLogger<MsSqlDataProvider>());
        }
        public override ISharedLockDataProvider GetSharedLockDataProvider(IServiceProvider services) => services.GetRequiredService<ISharedLockDataProvider>();

        public override IEnumerable<IBlobProvider> GetBlobProviders()
        {
            return null;
        }

        public override IExclusiveLockDataProvider GetExclusiveLockDataProvider(IServiceProvider services) => services.GetRequiredService<IExclusiveLockDataProvider>();
        public override IBlobStorageMetaDataProvider GetBlobMetaDataProvider(DataProvider dataProvider, IServiceProvider services) => services.GetRequiredService<IBlobStorageMetaDataProvider>();
        public override IBlobProviderSelector GetBlobProviderSelector(IServiceProvider services) => services.GetRequiredService<IBlobProviderSelector>();
        public override IAccessTokenDataProvider GetAccessTokenDataProvider(IServiceProvider services) => services.GetRequiredService<IAccessTokenDataProvider>();
        public override IPackagingDataProvider GetPackagingDataProvider(IServiceProvider services) => services.GetRequiredService<IPackagingDataProvider>();

        public override ISecurityDataProvider GetSecurityDataProvider(DataProvider dataProvider, IServiceProvider services)
        {
            return new EFCSecurityDataProvider(new MessageSenderManager(), Options.Create(new Security.EFCSecurityStore.Configuration.DataOptions
            {
                ConnectionString = ConnectionString
            }), NullLogger<EFCSecurityDataProvider>.Instance);
        }

        public override ITestingDataProvider GetTestingDataProvider(IServiceProvider services) => services.GetRequiredService<ITestingDataProvider>();

        public override ISearchEngine GetSearchEngine()
        {
            //TODO:<?IntT: Customize indexDirectoryPath if there is more than one platform that uses a local lucene index.
            var indexingEngine = new Lucene29LocalIndexingEngine(null);
            var x = indexingEngine.LuceneSearchManager.IndexDirectory.CurrentDirectory;
            return new Lucene29SearchEngine(indexingEngine, new Lucene29LocalQueryEngine());
        }

        public override IStatisticalDataProvider GetStatisticalDataProvider(IServiceProvider services) => services.GetRequiredService<IStatisticalDataProvider>();

        /* ============================================================== */

        protected void PrepareDatabase()
        {
            var scriptRootPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                // ReSharper disable once AssignNullToNotNullAttribute
                AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\Storage\Data\MsSqlClient\Scripts"));

            var databaseName = GetDatabaseName(ConnectionString);
            var dbid = ExecuteSqlScalarNative<int?>($"SELECT database_id FROM sys.databases WHERE Name = '{databaseName}'", "master");
            if (dbid == null)
            {
                // create database
                var sqlPath = System.IO.Path.Combine(scriptRootPath, "Create_SenseNet_Database_Templated.sql");
                string sql;
                using (var reader = new System.IO.StreamReader(sqlPath))
                    sql = reader.ReadToEnd();
                sql = sql.Replace("{DatabaseName}", databaseName);
                ExecuteSqlCommandNative(sql, "master");
            }
            // prepare database
            ExecuteSqlScriptNative(System.IO.Path.Combine(scriptRootPath, @"MsSqlInstall_Security.sql"), databaseName);
            ExecuteSqlScriptNative(System.IO.Path.Combine(scriptRootPath, @"MsSqlInstall_Schema.sql"), databaseName);
        }
        private void ExecuteSqlScriptNative(string scriptPath, string databaseName)
        {
            string sql;
            using (var reader = new System.IO.StreamReader(scriptPath))
                sql = reader.ReadToEnd();
            ExecuteSqlCommandNative(sql, databaseName);
        }
        private void ExecuteSqlCommandNative(string sql, string databaseName)
        {
            var scripts = sql.Split(new[] { "\r\nGO" }, StringSplitOptions.RemoveEmptyEntries);

            using (var cn = new SqlConnection(GetConnectionString(databaseName)))
            {
                cn.Open();
                foreach (var script in scripts)
                {
                    using (var proc = new SqlCommand(script, cn))
                    {
                        proc.CommandType = CommandType.Text;
                        proc.ExecuteNonQuery();
                    }
                }
            }
        }
        private T ExecuteSqlScalarNative<T>(string sql, string databaseName)
        {
            using (var cn = new SqlConnection(GetConnectionString(databaseName)))
            {
                cn.Open();
                using (var proc = new SqlCommand(sql, cn))
                {
                    proc.CommandType = CommandType.Text;
                    return (T)proc.ExecuteScalar();
                }
            }
        }
        private string GetDatabaseName(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return builder.InitialCatalog;
        }
        private string GetConnectionString(string databaseName)
        {
            var builder = new SqlConnectionStringBuilder(ConnectionString);
            builder.InitialCatalog = databaseName;
            return builder.ToString();
        }
    }
}
