using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics.PerformanceData;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IO = System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;
using ContentType = System.Net.Mime.ContentType;

namespace SenseNet.BlobStorage.IntegrationTests
{
    [TestClass]
    public abstract class BlobStorageIntegrationTests
    {
        #region Infrastructure

        private static readonly Dictionary<Type, BlobStorageIntegrationTests> Instances =
            new Dictionary<Type, BlobStorageIntegrationTests>();

        protected static BlobStorageIntegrationTests GetInstance(Type type)
        {
            Instances.TryGetValue(type, out var instance);
            return instance;
        }

        protected abstract string DatabaseName { get; }
        protected abstract bool SqlFsEnabled { get; }
        protected abstract bool SqlFsUsed { get; }
        protected abstract void ConfigureMinimumSizeForFileStreamInBytes(int newValue, out int oldValue);

        private static readonly string ConnetionStringBase = @"Data Source=.\SQL2016;Integrated Security=SSPI;Persist Security Info=False";
        private string GetConnectionString(string databaseName = null)
        {
            return $"Initial Catalog={databaseName ?? DatabaseName};{ConnetionStringBase}";
        }

        private bool _prepared;
        private string _connectionStringBackup;
        private string _securityDatabaseConnectionString;
        private RepositoryInstance _repositoryInstance;
        [TestInitialize]
        public void Initialize()
        {
            if (!_prepared)
            {
                ContentTypeManager.Reset();
                //ActiveSchema.Reset();

                _connectionStringBackup = Configuration.ConnectionStrings.ConnectionString;
                _securityDatabaseConnectionString = ConnectionStrings.SecurityDatabaseConnectionString;
                var cnstr = GetConnectionString(DatabaseName);
                ConnectionStrings.ConnectionString = cnstr;
                ConnectionStrings.SecurityDatabaseConnectionString = cnstr;

                PrepareDatabase();

                _repositoryInstance = Repository.Start(CreateRepositoryBuilderForInstall());
                using (new SystemAccount())
                    PrepareRepository();

                Instances[this.GetType()] = this;
                _prepared = true;
            }
        }

        protected static void TearDown(Type type)
        {
            Instances.TryGetValue(type, out var instance);
            instance?.TearDownPrivate();
        }
        private void TearDownPrivate()
        {
            if (_connectionStringBackup != null)
                ConnectionStrings.ConnectionString = _connectionStringBackup;
            if (_securityDatabaseConnectionString != null)
                ConnectionStrings.SecurityDatabaseConnectionString = _securityDatabaseConnectionString;

            _repositoryInstance?.Dispose();
        }

        protected void PrepareDatabase()
        {
            var scriptRootPath = IO.Path.GetFullPath(IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Storage\Data\SqlClient\Scripts"));

            var dbid = ExecuteSqlScalarNative<int?>($"SELECT database_id FROM sys.databases WHERE Name = '{DatabaseName}'", "master");
            if (dbid == null)
            {
                // create database
                var sqlPath = IO.Path.Combine(scriptRootPath, "Create_SenseNet_Database_Templated.sql");
                string sql;
                using (var reader = new IO.StreamReader(sqlPath))
                    sql = reader.ReadToEnd();
                sql = sql.Replace("{DatabaseName}", DatabaseName);
                ExecuteSqlCommandNative(sql, "master");
            }
            // prepare database
            ExecuteSqlScriptNative(IO.Path.Combine(scriptRootPath, @"Install_Security.sql"), DatabaseName);
            ExecuteSqlScriptNative(IO.Path.Combine(scriptRootPath, @"Install_01_Schema.sql"), DatabaseName);
            ExecuteSqlScriptNative(IO.Path.Combine(scriptRootPath, @"Install_02_Procs.sql"), DatabaseName);
            ExecuteSqlScriptNative(IO.Path.Combine(scriptRootPath, @"Install_03_Data_Phase1.sql"), DatabaseName);
            ExecuteSqlScriptNative(IO.Path.Combine(scriptRootPath, @"Install_04_Data_Phase2.sql"), DatabaseName);

            DataProvider.InitializeForTests();

            if (SqlFsEnabled)
                ExecuteSqlScriptNative(IO.Path.Combine(scriptRootPath, @"EnableFilestream.sql"), DatabaseName);
        }
        private void ExecuteSqlScriptNative(string scriptPath, string databaseName)
        {
            string sql;
            using (var reader = new IO.StreamReader(scriptPath))
                sql = reader.ReadToEnd();
            ExecuteSqlCommandNative(sql, databaseName);
        }

        private void ExecuteSqlCommandNative(string sql, string databaseName)
        {
            var cnstr = GetConnectionString(databaseName);
            var scripts = sql.Split(new[] {"\r\nGO"}, StringSplitOptions.RemoveEmptyEntries);
            var index = 0;

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
            var ctdRootPath = IO.Path.GetFullPath(IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\nuget\snadmin\install-services\import\System\Schema\ContentTypes"));
            var xmlSources = IO.Directory.GetFiles(ctdRootPath, "*.xml")
                .Select(p =>
                {
                    using (var r = new IO.StreamReader(p))
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
        private DbDataReader ExecuteSqlReader(string sql)
        {
            var proc = DataProvider.CreateDataProcedure(sql);
            proc.CommandType = CommandType.Text;
            return proc.ExecuteReader();
        }

        #endregion

        public void TestCase01_CreateFile()
        {
            using (new SystemAccount())
            {
                var testRoot = CreateTestRoot();

                var file = new File(testRoot) {Name = "File1.file"};
                var expectedText = "Lorem ipsum dolo sit amet";
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(expectedText));

                // action
                file.Save();

                // assert
                var dbFiles = LoadDbFiles(file.VersionId);
                Assert.AreEqual(1, dbFiles.Length);
                var dbFile = dbFiles[0];
                Assert.IsNull(dbFile.BlobProvider);
                Assert.IsNull(dbFile.BlobProviderData);
                Assert.AreEqual(false, dbFile.IsDeleted);
                Assert.AreEqual(false, dbFile.Staging);
                Assert.AreEqual(0, dbFile.StagingVersionId);
                Assert.AreEqual(0, dbFile.StagingPropertyTypeId);
                Assert.AreEqual(expectedText.Length + 3, dbFile.Size);
                if (SqlFsUsed)
                {
                    Assert.IsNull(dbFile.Stream);
                    Assert.IsNotNull(dbFile.FileStream);
                    Assert.AreEqual(dbFile.Size, dbFile.FileStream.Length);
                    Assert.AreEqual(expectedText, GetStringFromBytes(dbFile.FileStream));
                }
                else
                {
                    Assert.IsNull(dbFile.FileStream);
                    Assert.IsNotNull(dbFile.Stream);
                    Assert.AreEqual(dbFile.Size, dbFile.Stream.Length);
                    Assert.AreEqual(expectedText, GetStringFromBytes(dbFile.Stream));
                }
            }
        }

        #region Tools

        private class DbFile
        {
            public int FileId;
            public string ContentType;
            public string FileNameWithoutExtension;
            public string Extension;
            public long Size;
            public string Checksum;
            public byte[] Stream;
            public DateTime CreationDate;
            public Guid RowGuid;
            public long Timestamp;
            public bool? Staging;
            public int StagingVersionId;
            public int StagingPropertyTypeId;
            public bool? IsDeleted;
            public string BlobProvider;
            public string BlobProviderData;
            public byte[] FileStream;
        }
        private DbFile[] LoadDbFiles(int versionId, string propertyName = "Binary")
        {
            var propTypeId = ActiveSchema.PropertyTypes[propertyName].Id;
            var sql = $@"SELECT f.* FROM BinaryProperties b JOIN Files f on f.FileId = b.FileId WHERE b.VersionId = {versionId} and b.PropertyTypeId = {propTypeId}";
            var dbFiles = new List<DbFile>();
            using (var reader = ExecuteSqlReader(sql))
            {
                while (reader.Read())
                {
                    var file = new DbFile();
                    file.FileId = reader.GetInt32(reader.GetOrdinal("FileId"));
                    file.BlobProvider = reader.GetSafeString(reader.GetOrdinal("BlobProvider"));
                    file.BlobProviderData = reader.GetSafeString(reader.GetOrdinal("BlobProviderData"));
                    file.ContentType = reader.GetSafeString(reader.GetOrdinal("ContentType"));
                    file.FileNameWithoutExtension = reader.GetSafeString(reader.GetOrdinal("FileNameWithoutExtension"));
                    file.Extension = reader.GetSafeString(reader.GetOrdinal("Extension"));
                    file.Size = reader.GetSafeInt64(reader.GetOrdinal("Size"));
                    file.CreationDate = reader.GetSafeDateTime(reader.GetOrdinal("CreationDate")) ?? DateTime.MinValue;
                    file.IsDeleted = reader.GetSafeBoolFromBit(reader.GetOrdinal("IsDeleted"));
                    file.Staging = reader.GetSafeBoolFromBit(reader.GetOrdinal("Staging"));
                    file.StagingPropertyTypeId = reader.GetSafeInt32(reader.GetOrdinal("StagingPropertyTypeId"));
                    file.StagingVersionId = reader.GetSafeInt32(reader.GetOrdinal("StagingVersionId"));
                    file.Stream = (byte[]) reader[reader.GetOrdinal("Stream")];
                    file.Checksum = reader.GetSafeString(reader.GetOrdinal("Checksum"));
                    file.RowGuid = reader.GetGuid(reader.GetOrdinal("RowGuid"));
                    file.Timestamp = DataProvider.GetLongFromBytes((byte[])reader[reader.GetOrdinal("Timestamp")]);
                    if (reader.FieldCount > 16)
                        file.FileStream = reader.GetSafeBytes(reader.GetOrdinal("FileStream"));
                    dbFiles.Add(file);
                }
            }

            return dbFiles.ToArray();
        }

        private Node CreateTestRoot()
        {
            var root = new SystemFolder(Repository.Root) {Name = Guid.NewGuid().ToString()};
            root.Save();
            return root;
        }

        private string GetStringFromBytes(byte[] bytes)
        {
            using (var stream = new IO.MemoryStream(bytes))
            using (var reader = new IO.StreamReader(stream))
                return reader.ReadToEnd();
        }
        #endregion
    }
    internal static class DbReaderExtensions
    {
        public static long GetSafeInt64(this IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0 : reader.GetInt64(index);
        }
        public static int GetSafeInt32(this IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0 : reader.GetInt32(index);
        }
        public static DateTime? GetSafeDateTime(this IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? (DateTime?)null : reader.GetDateTime(index);
        }
        public static string GetSafeString(this IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? null : reader.GetString(index);
        }
        public static bool GetSafeBoolFromBit(this IDataReader reader, int index)
        {
            return !reader.IsDBNull(index) && reader.GetBoolean(index);
        }
        public static byte[] GetSafeBytes(this IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? null : (byte[])reader[index];
        }
    }

}
