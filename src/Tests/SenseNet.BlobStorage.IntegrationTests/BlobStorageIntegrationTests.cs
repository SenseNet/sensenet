using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;
using ContentType = System.Net.Mime.ContentType;

namespace SenseNet.BlobStorage.IntegrationTests
{
    [TestClass]
    public abstract class BlobStorageIntegrationTests
    {
        protected abstract string DatabaseName { get; }
        protected abstract bool SqlFileStreamEnabled { get; }

        private static readonly string ConnetionStringBase = @"Data Source=.\SQL2016;Integrated Security=SSPI;Persist Security Info=False";
        private string GetConnectionString(string databaseName = null)
        {
            return $"Initial Catalog={databaseName ?? DatabaseName};{ConnetionStringBase}";
        }

        private bool _prepared;
        private string _connectionStringBackup;
        private string _securityDatabaseConnectionString;
        [TestInitialize]
        public void InitializeTest()
        {
            if (!_prepared)
            {
                _connectionStringBackup = Configuration.ConnectionStrings.ConnectionString;
                _securityDatabaseConnectionString = ConnectionStrings.SecurityDatabaseConnectionString;
                var cnstr = GetConnectionString(DatabaseName);
                ConnectionStrings.ConnectionString = cnstr;
                ConnectionStrings.SecurityDatabaseConnectionString = cnstr;

                PrepareDatabase();

                using (Repository.Start(CreateRepositoryBuilderForInstall()))
                using (new SystemAccount())
                    PrepareRepository();

                _prepared = true;
            }

        }
        [TestCleanup]
        public void CleanupTest()
        {
            if (_connectionStringBackup != null)
                ConnectionStrings.ConnectionString = _connectionStringBackup;
            if (_securityDatabaseConnectionString != null)
                ConnectionStrings.SecurityDatabaseConnectionString = _securityDatabaseConnectionString;
        }

        protected void PrepareDatabase()
        {
            var scriptRootPath = Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Storage\Data\SqlClient\Scripts"));

            var dbid = ExecuteSqlScalarNative<int?>($"SELECT database_id FROM sys.databases WHERE Name = '{DatabaseName}'", "master");
            if (dbid == null)
            {
                // create database
                var sqlPath = Path.Combine(scriptRootPath, "Create_SenseNet_Database_Templated.sql");
                string sql;
                using (var reader = new StreamReader(sqlPath))
                    sql = reader.ReadToEnd();
                sql = sql.Replace("{DatabaseName}", DatabaseName);
                ExecuteSqlCommandNative(sql, "master");
            }
            // prepare database
            ExecuteSqlScriptNative(Path.Combine(scriptRootPath, @"Install_Security.sql"), DatabaseName);
            ExecuteSqlScriptNative(Path.Combine(scriptRootPath, @"Install_01_Schema.sql"), DatabaseName);
            ExecuteSqlScriptNative(Path.Combine(scriptRootPath, @"Install_02_Procs.sql"), DatabaseName);
            ExecuteSqlScriptNative(Path.Combine(scriptRootPath, @"Install_03_Data_Phase1.sql"), DatabaseName);
            ExecuteSqlScriptNative(Path.Combine(scriptRootPath, @"Install_04_Data_Phase2.sql"), DatabaseName);

            DataProvider.InitializeForTests();

            if (SqlFileStreamEnabled)
                ExecuteSqlScriptNative(Path.Combine(scriptRootPath, @"EnableFilestream.sql"), DatabaseName);
        }
        private void ExecuteSqlScriptNative(string scriptPath, string databaseName)
        {
            string sql;
            using (var reader = new StreamReader(scriptPath))
                sql = reader.ReadToEnd();
            ExecuteSqlCommandNative(sql, databaseName);
        }
        private void ExecuteSqlCommandNative(string sql, string databaseName)
        {
            var cnstr = GetConnectionString(databaseName);
            var scripts = sql.Split(new []{"\r\nGO"}, StringSplitOptions.RemoveEmptyEntries);
            var index = 0;
            try
            {
                using (var cn = new SqlConnection(cnstr))
                {
                    cn.Open();
                    foreach (var script in scripts)
                    {
                        using (var proc = new SqlCommand(script, cn))
                        {
                            proc.CommandType = CommandType.Text;
                            proc.ExecuteNonQuery();
                        }
                        index++;
                    }
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }
        private T ExecuteSqlScalarNative<T>(string sql, string databaseName)
        {
            var cnstr = GetConnectionString(databaseName);
            using (var cn = new SqlConnection(cnstr))
            {
                cn.Open();
                using (var proc = new SqlCommand(sql, cn))
                {
                    proc.CommandType = CommandType.Text;
                    return (T) proc.ExecuteScalar();
                }
            }
        }

        private void PrepareRepository()
        {
            SecurityHandler.SecurityInstaller.InstallDefaultSecurityStructure();
            ContentTypeInstaller.InstallContentType(LoadCtds());
            SaveInitialIndexDocuments();
            RebuildIndex();
        }
        protected RepositoryBuilder CreateRepositoryBuilderForInstall()
        {
            return new RepositoryBuilder()
                .UseAccessProvider(new DesktopAccessProvider())
                .UseSearchEngine(new InMemorySearchEngine())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                //.StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(SettingsCache))
                .UseTraceCategories("Test", "Event", "Custom");
        }
        private string[] LoadCtds()
        {
            var ctdRootPath = Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\nuget\snadmin\install-services\import\System\Schema\ContentTypes"));
            var xmlSources = Directory.GetFiles(ctdRootPath, "*.xml")
                .Select(p =>
                {
                    using (var r = new StreamReader(p))
                        return r.ReadToEnd();
                })
                .ToArray();
            return xmlSources;
        }
        protected void SaveInitialIndexDocuments()
        {
            var idSet = DataProvider.LoadIdsOfNodesThatDoNotHaveIndexDocument(0, 11000);
            var nodes = Node.LoadNodes(idSet);

            if (nodes.Count == 0)
                return;

            foreach (var node in nodes)
            {
                // ReSharper disable once UnusedVariable
                DataBackingStore.SaveIndexDocument(node, false, false, out var hasBinary);
            }
        }
        protected void RebuildIndex()
        {
            var paths = new List<string>();
            var populator = SearchManager.GetIndexPopulator();
            populator.NodeIndexed += (o, e) => { paths.Add(e.Path); };
            populator.ClearAndPopulateAll();
        }

        private void ExecuteSqlCommand(string sql)
        {
            var proc = DataProvider.CreateDataProcedure(sql);
            proc.CommandType = CommandType.Text;
            proc.ExecuteNonQuery();
        }
        private T ExecuteSqlScalar<T>(string sql, string databaseName)
        {
            var proc = DataProvider.CreateDataProcedure(sql);
            proc.CommandType = CommandType.Text;
            return (T)proc.ExecuteScalar();
        }

        [TestMethod]
        public void Blob_CreateFile()
        {
            Assert.Inconclusive();
        }

    }
}
