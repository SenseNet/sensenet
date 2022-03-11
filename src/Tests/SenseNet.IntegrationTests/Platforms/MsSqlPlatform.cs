using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Diagnostics;
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
        public override void OnBeforeGettingRepositoryBuilder(RepositoryBuilder builder)
        {
            PrepareDatabase();
            base.OnBeforeGettingRepositoryBuilder(builder);
        }
        public override void OnAfterRepositoryStart(RepositoryInstance repository)
        {
            var state = ((IndexManager)Providers.Instance.IndexManager).DistributedIndexingActivityQueue.GetCurrentState();
            ((IndexManager)Providers.Instance.IndexManager).DistributedIndexingActivityQueue._setCurrentExecutionState(IndexingActivityStatus.Startup);
            base.OnAfterRepositoryStart(repository);
        }

        private IOptions<ConnectionStringOptions> _connectionStringOptions;
        MsSqlDataProvider _dataProvider;
        public override DataProvider GetDataProvider()
        {
            _connectionStringOptions = Options.Create(new ConnectionStringOptions{Repository = RepositoryConnectionString });
            var dbInstallerOptions = Options.Create(new MsSqlDatabaseInstallationOptions());

            _dataProvider = new MsSqlDataProvider(Options.Create(DataOptions.GetLegacyConfiguration()), _connectionStringOptions,
                dbInstallerOptions,
                new MsSqlDatabaseInstaller(dbInstallerOptions, NullLoggerFactory.Instance.CreateLogger<MsSqlDatabaseInstaller>()),
                new MsSqlDataInstaller(_connectionStringOptions, NullLoggerFactory.Instance.CreateLogger<MsSqlDataInstaller>()),
                NullLoggerFactory.Instance.CreateLogger<MsSqlDataProvider>());
            return _dataProvider;
        }
        public override ISharedLockDataProvider GetSharedLockDataProvider()
        {
            return new MsSqlSharedLockDataProvider();
        }

        public override IEnumerable<IBlobProvider> GetBlobProviders()
        {
            return null;
        }

        public override IExclusiveLockDataProvider GetExclusiveLockDataProvider()
        {
            return new MsSqlExclusiveLockDataProvider();
        }
        public override IBlobStorageMetaDataProvider GetBlobMetaDataProvider(DataProvider dataProvider)
        {
            //TODO: get services and options from outside
            return new MsSqlBlobMetaDataProvider(Providers.Instance.BlobProviders,
                Options.Create(DataOptions.GetLegacyConfiguration()),
                Options.Create(BlobStorageOptions.GetLegacyConfiguration()),
                Options.Create(new ConnectionStringOptions { Repository = RepositoryConnectionString }));
        }
        public override IBlobProviderSelector GetBlobProviderSelector()
        {
            return new BuiltInBlobProviderSelector(
                Providers.Instance.BlobProviders,
                null,
                Options.Create(BlobStorageOptions.GetLegacyConfiguration()));
        }
        public override IAccessTokenDataProvider GetAccessTokenDataProvider()
        {
            return new MsSqlAccessTokenDataProvider();
        }
        public override IPackagingDataProvider GetPackagingDataProvider()
        {
            return new MsSqlPackagingDataProvider();
        }
        public override ISecurityDataProvider GetSecurityDataProvider(DataProvider dataProvider)
        {
            return new EFCSecurityDataProvider(new MessageSenderManager(), Options.Create(new Security.EFCSecurityStore.Configuration.DataOptions
            {
                ConnectionString = RepositoryConnectionString
            }), NullLogger<EFCSecurityDataProvider>.Instance);
        }
        public override ITestingDataProvider GetTestingDataProvider()
        {
            return new MsSqlTestingDataProvider(_dataProvider, _connectionStringOptions);
        }
        public override ISearchEngine GetSearchEngine()
        {
            //TODO:<?IntT: Customize indexDirectoryPath if there is more than one platform that uses a local lucene index.
            var indexingEngine = new Lucene29LocalIndexingEngine(null);
            var x = indexingEngine.LuceneSearchManager.IndexDirectory.CurrentDirectory;
            return new Lucene29SearchEngine(indexingEngine, new Lucene29LocalQueryEngine());
        }

        public override IStatisticalDataProvider GetStatisticalDataProvider()
        {
            return new MsSqlStatisticalDataProvider(Options.Create(new DataOptions()),
                Options.Create(new ConnectionStringOptions {Repository = RepositoryConnectionString }));
        }

        /* ============================================================== */

        protected void PrepareDatabase()
        {
            var scriptRootPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                // ReSharper disable once AssignNullToNotNullAttribute
                AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\Storage\Data\MsSqlClient\Scripts"));

            var databaseName = GetDatabaseName(RepositoryConnectionString);
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
            var builder = new SqlConnectionStringBuilder(RepositoryConnectionString);
            builder.InitialCatalog = databaseName;
            return builder.ToString();
        }
    }
}
