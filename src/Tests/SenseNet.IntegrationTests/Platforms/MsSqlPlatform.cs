using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Lucene29;
using SenseNet.Security;
using SenseNet.Security.EFCSecurityStore;
using SenseNet.Tests.Core.Implementations;

namespace SenseNet.IntegrationTests.Platforms
{
    public class MsSqlPlatform : Platform
    {
        //UNDONE:<?IntT: remove local connectionstring
        public string ConnectionString { get; } =
            "Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=SenseNet.IntegrationTests;Data Source=.\\SQL2016";

        public override void OnBeforeGettingRepositoryBuilder(RepositoryBuilder builder)
        {
            ConnectionStrings.ConnectionString = ConnectionString;
            PrepareDatabase();
            base.OnBeforeGettingRepositoryBuilder(builder);
        }

        public override DataProvider GetDataProvider()
        {
            return new MsSqlDataProvider();
        }
        public override ISharedLockDataProviderExtension GetSharedLockDataProviderExtension()
        {
            return new MsSqlSharedLockDataProvider();
        }
        public override IExclusiveLockDataProviderExtension GetExclusiveLockDataProviderExtension()
        {
            return new MsSqlExclusiveLockDataProvider();
        }
        public override IBlobStorageMetaDataProvider GetBlobMetaDataProvider(DataProvider dataProvider)
        {
            //TODO: get services and options from outside
            return new MsSqlBlobMetaDataProvider(Providers.Instance.BlobProviders,
                Options.Create(DataOptions.GetLegacyConfiguration()),
                Options.Create(BlobStorageOptions.GetLegacyConfiguration()));
        }
        public override IBlobProviderSelector GetBlobProviderSelector()
        {
            return new BuiltInBlobProviderSelector(
                Providers.Instance.BlobProviders,
                null,
                Options.Create(BlobStorageOptions.GetLegacyConfiguration()));
        }
        public override IAccessTokenDataProviderExtension GetAccessTokenDataProviderExtension()
        {
            return new MsSqlAccessTokenDataProvider();
        }
        public override IPackagingDataProviderExtension GetPackagingDataProviderExtension()
        {
            return new MsSqlPackagingDataProvider();
        }
        public override ISecurityDataProvider GetSecurityDataProvider(DataProvider dataProvider)
        {
            return new EFCSecurityDataProvider(connectionString: ConnectionString);
        }
        public override ITestingDataProviderExtension GetTestingDataProviderExtension()
        {
            return new MsSqlTestingDataProvider();
        }
        public override ISearchEngine GetSearchEngine()
        {
            //TODO:<?IntT: Customize indexDirectoryPath if there is more than one platform that uses a local lucene index.
            var indexingEngine = new Lucene29LocalIndexingEngine(null);
            var x = indexingEngine.LuceneSearchManager.IndexDirectory.CurrentDirectory;
            //UNDONE:<?IntT: Force delete "write.lock" when getting the platform the first time.
            return new Lucene29SearchEngine()
            {
                IndexingEngine = indexingEngine,
                QueryEngine = new Lucene29LocalQueryEngine()
            };
        }

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
