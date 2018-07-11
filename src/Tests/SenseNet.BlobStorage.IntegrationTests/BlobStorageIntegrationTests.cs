using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
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
using SenseNet.Diagnostics;
using SenseNet.MsSqlFsBlobProvider;
using SenseNet.Tests.Implementations;

namespace SenseNet.BlobStorage.IntegrationTests
{
    [TestClass]
    public abstract class BlobStorageIntegrationTests
    {
        #region Infrastructure

        public TestContext TestContext { get; set; }

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
        protected abstract Type ExpectedExternalBlobProviderType { get; }
        protected abstract Type ExpectedMetadataProviderType { get; }
        protected abstract void BuildLegoBricks(RepositoryBuilder builder);
        protected internal abstract void ConfigureMinimumSizeForFileStreamInBytes(int newValue, out int oldValue);

        private static readonly string ConnetionStringBase = @"Data Source=.\SQL2016;Integrated Security=SSPI;Persist Security Info=False";
        private string GetConnectionString(string databaseName = null)
        {
            return $"Initial Catalog={databaseName ?? DatabaseName};{ConnetionStringBase}";
        }

        [AssemblyInitialize]
        public static void StartAllTests(TestContext testContext)
        {
            SnTrace.SnTracers.Clear();
            SnTrace.SnTracers.Add(new SnDebugViewTracer());
        }

        private string _connectionString;
        private string _connectionStringBackup;
        private string _securityConnectionStringBackup;
        private RepositoryInstance _repositoryInstance;
        [TestInitialize]
        public void Initialize()
        {
            // Test class initialization problem: the test framework
            // uses brand new instance for each test method.

            var prepared = Instances.TryGetValue(this.GetType(), out var instance);
            if (!prepared)
            {
                ContentTypeManager.Reset();
                //ActiveSchema.Reset();

                _connectionStringBackup = ConnectionStrings.ConnectionString;
                _securityConnectionStringBackup = ConnectionStrings.SecurityDatabaseConnectionString;
                _connectionString = GetConnectionString(DatabaseName);
                ConnectionStrings.ConnectionString = _connectionString;
                ConnectionStrings.SecurityDatabaseConnectionString = _connectionString;

                PrepareDatabase();

                using (Repository.Start(CreateRepositoryBuilderForInstall()))
                using (new SystemAccount())
                    PrepareRepository();

                Instances[this.GetType()] = this;
            }
            else
            {
                ConnectionStrings.ConnectionString = instance._connectionString;
                ConnectionStrings.SecurityDatabaseConnectionString = instance._connectionString;
            }

            _repositoryInstance = Repository.Start(CreateRepositoryBuilderForInstall());

            Assert.AreEqual(typeof(BuiltInBlobProviderSelector), BlobStorageComponents.ProviderSelector.GetType());
            Assert.AreEqual(ExpectedExternalBlobProviderType, BuiltInBlobProviderSelector.ExternalBlobProvider?.GetType());
            Assert.AreEqual(ExpectedMetadataProviderType, BlobStorageComponents.DataProvider.GetType());

            SnTrace.Test.Enabled = true;
            SnTrace.Test.Write("START test: {0}", TestContext.TestName);
        }
        [TestCleanup]
        public void CleanupTest()
        {
            _repositoryInstance?.Dispose();

            SnTrace.Test.Enabled = true;
            SnTrace.Test.Write("END test: {0}", TestContext.TestName);
            SnTrace.Flush();
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
            if (_securityConnectionStringBackup != null)
                ConnectionStrings.SecurityDatabaseConnectionString = _securityConnectionStringBackup;

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
            var builder = new RepositoryBuilder()
                .UseAccessProvider(new DesktopAccessProvider())
                .UseSearchEngine(new InMemorySearchEngine())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                //.StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(SettingsCache))
                .UseTraceCategories("Test", "Event", "Custom");
            BuildLegoBricks(builder);
            return builder;
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

        public void TestCase01_CreateFileSmall()
        {
            var expectedText = "Lorem ipsum dolo sit amet";
            var dbFile = CreateFileTest(expectedText, expectedText.Length + 10);
            var ctx = BlobStorageBase.GetBlobStorageContext(dbFile.FileId);

            Assert.IsNull(dbFile.FileStream);
            Assert.IsNotNull(dbFile.Stream);
            Assert.AreEqual(dbFile.Size, dbFile.Stream.Length);
            Assert.AreEqual(expectedText, GetStringFromBytes(dbFile.Stream));

            Assert.AreEqual(dbFile.FileId, ctx.FileId);
            Assert.AreEqual(dbFile.Size, ctx.Length);
            Assert.IsTrue(ctx.BlobProviderData is BuiltinBlobProviderData);
        }
        public void TestCase02_CreateFileBig()
        {
            var expectedText = "Lorem ipsum dolo sit amet";
            var dbFile = CreateFileTest(expectedText, expectedText.Length - 10);
            var ctx = BlobStorageBase.GetBlobStorageContext(dbFile.FileId);

            Assert.AreEqual(dbFile.FileId, ctx.FileId);
            Assert.AreEqual(dbFile.Size, ctx.Length);

            if (SqlFsUsed)
            {
                Assert.IsNull(dbFile.Stream);
                Assert.IsNotNull(dbFile.FileStream);
                Assert.AreEqual(dbFile.Size, dbFile.FileStream.Length);
                Assert.AreEqual(expectedText, GetStringFromBytes(dbFile.FileStream));

                Assert.IsTrue(ctx.BlobProviderData is SqlFileStreamBlobProviderData);
            }
            else
            {
                Assert.IsNull(dbFile.FileStream);
                Assert.IsNotNull(dbFile.Stream);
                Assert.AreEqual(dbFile.Size, dbFile.Stream.Length);
                Assert.AreEqual(expectedText, GetStringFromBytes(dbFile.Stream));

                Assert.IsTrue(ctx.BlobProviderData is BuiltinBlobProviderData);
            }
        }
        private DbFile CreateFileTest(string fileContent, int sizeLimit)
        {
            using (new SystemAccount())
            using (new SqlFileStreamSizeSwindler(this, sizeLimit))
            {
                var testRoot = CreateTestRoot();

                var file = new File(testRoot) { Name = "File1.file" };
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(fileContent));

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
                Assert.AreEqual(fileContent.Length + 3, dbFile.Size);

                return dbFile;
            }
        }

        public void TestCase03_UpdateFileSmallSmall()
        {
            // 20 chars:       |------------------|
            var initialText = "Lorem ipsum...";
            var updatedText = "Cras lobortis...";
            var dbFile = UpdateFileTest(initialText, updatedText, 20);

            Assert.IsNull(dbFile.FileStream);
            Assert.IsNotNull(dbFile.Stream);
            Assert.AreEqual(dbFile.Size, dbFile.Stream.Length);
            Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.Stream));
        }
        public void TestCase04_UpdateFileSmallBig()
        {
            // 20 chars:       |------------------|
            var initialText = "Lorem ipsum...";
            var updatedText = "Cras lobortis consequat nisi...";
            var dbFile = UpdateFileTest(initialText, updatedText, 20);

            if (SqlFsUsed)
            {
                Assert.IsNull(dbFile.Stream);
                Assert.IsNotNull(dbFile.FileStream);
                Assert.AreEqual(dbFile.Size, dbFile.FileStream.Length);
                Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.FileStream));
            }
            else
            {
                Assert.IsNull(dbFile.FileStream);
                Assert.IsNotNull(dbFile.Stream);
                Assert.AreEqual(dbFile.Size, dbFile.Stream.Length);
                Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.Stream));
            }
        }
        public void TestCase05_UpdateFileBigSmall()
        {
            // 20 chars:       |------------------|
            var initialText = "Lorem ipsum dolo sit amet...";
            var updatedText = "Cras lobortis...";
            var dbFile = UpdateFileTest(initialText, updatedText, 20);

            Assert.IsNull(dbFile.FileStream);
            Assert.IsNotNull(dbFile.Stream);
            Assert.AreEqual(dbFile.Size, dbFile.Stream.Length);
            Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.Stream));
        }
        public void TestCase06_UpdateFileBigBig()
        {
            // 20 chars:       |------------------|
            var initialText = "Lorem ipsum dolo sit amet...";
            var updatedText = "Cras lobortis consequat nisi...";
            var dbFile = UpdateFileTest(initialText, updatedText, 20);

            if (SqlFsUsed)
            {
                Assert.IsNull(dbFile.Stream);
                Assert.IsNotNull(dbFile.FileStream);
                Assert.AreEqual(dbFile.Size, dbFile.FileStream.Length);
                Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.FileStream));
            }
            else
            {
                Assert.IsNull(dbFile.FileStream);
                Assert.IsNotNull(dbFile.Stream);
                Assert.AreEqual(dbFile.Size, dbFile.Stream.Length);
                Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.Stream));
            }
        }
        private DbFile UpdateFileTest(string initialContent, string updatedContent, int sizeLimit)
        {
            using (new SystemAccount())
            using (new SqlFileStreamSizeSwindler(this, sizeLimit))
            {
                var testRoot = CreateTestRoot();

                var file = new File(testRoot) { Name = "File1.file" };
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(initialContent));
                file.Save();
                var fileId = file.Id;
                var initialBlobId = file.Binary.FileId;
                file = Node.Load<File>(fileId);
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(updatedContent));

                // action
                file.Save();

                // assert
                var dbFiles = LoadDbFiles(file.VersionId);
                Assert.AreEqual(1, dbFiles.Length);
                var dbFile = dbFiles[0];
                //Assert.AreNotEqual(initialBlobId, file.Binary.FileId);
                Assert.IsNull(dbFile.BlobProvider);
                Assert.IsNull(dbFile.BlobProviderData);
                Assert.AreEqual(false, dbFile.IsDeleted);
                Assert.AreEqual(false, dbFile.Staging);
                Assert.AreEqual(0, dbFile.StagingVersionId);
                Assert.AreEqual(0, dbFile.StagingPropertyTypeId);
                Assert.AreEqual(updatedContent.Length + 3, dbFile.Size);

                return dbFile;
            }
        }

        public void TestCase07_WriteChunksSmall()
        {
            // 20 chars:       |------------------|
            // 10 chars:       |--------|---------|---------|
            var initialText = "Lorem ipsum dolo sit amet..";
            var updatedText = "Cras lobortis consequat nisi..";
            var dbFile = UpdateByChunksTest(initialText, updatedText, 222, 10);

            Assert.IsNull(dbFile.FileStream);
            Assert.IsNotNull(dbFile.Stream);
            Assert.AreEqual(dbFile.Size, dbFile.Stream.Length);
            Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.Stream));
        }
        public void TestCase08_WriteChunksBig()
        {
            // 20 chars:       |------------------|
            // 10 chars:       |--------|---------|---------|
            var initialText = "Lorem ipsum dolo sit amet..";
            var updatedText = "Cras lobortis consequat nisi..";
            var dbFile = UpdateByChunksTest(initialText, updatedText, 20, 10);

            if (SqlFsUsed)
            {
                Assert.IsNull(dbFile.Stream);
                Assert.IsNotNull(dbFile.FileStream);
                Assert.AreEqual(dbFile.Size, dbFile.FileStream.Length);
                Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.FileStream));
            }
            else
            {
                Assert.IsNull(dbFile.FileStream);
                Assert.IsNotNull(dbFile.Stream);
                Assert.AreEqual(dbFile.Size, dbFile.Stream.Length);
                Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.Stream));
            }
        }
        private DbFile UpdateByChunksTest(string initialContent, string updatedText, int sizeLimit, int chunkSize)
        {
            using (new SystemAccount())
            using (new SqlFileStreamSizeSwindler(this, sizeLimit))
            {
                var testRoot = CreateTestRoot();

                var file = new File(testRoot) { Name = "File1.file" };
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(initialContent));
                file.Save();
                var fileId = file.Id;

                var chunks = SplitFile(updatedText, chunkSize, out var fullSize);

                file = Node.Load<File>(fileId);
                file.Save(SavingMode.StartMultistepSave);
                var token = BinaryData.StartChunk(fileId, fullSize);

                var offset = 0;
                foreach (var chunk in chunks)
                {
                    BinaryData.WriteChunk(fileId, token, fullSize, chunk, offset);
                    offset += chunkSize;
                }

                BinaryData.CommitChunk(fileId, token, fullSize);

                file = Node.Load<File>(fileId);
                file.FinalizeContent();


                // assert
                var dbFiles = LoadDbFiles(file.VersionId);
                Assert.AreEqual(1, dbFiles.Length);
                var dbFile = dbFiles[0];
                //Assert.AreNotEqual(initialBlobId, file.Binary.FileId);
                Assert.IsNull(dbFile.BlobProvider);
                Assert.IsNull(dbFile.BlobProviderData);
                Assert.AreEqual(false, dbFile.IsDeleted);
                Assert.AreEqual(false, dbFile.Staging);
                Assert.AreEqual(0, dbFile.StagingVersionId);
                Assert.AreEqual(0, dbFile.StagingPropertyTypeId);
                Assert.AreEqual(fullSize, dbFile.Size);

                return dbFile;
            }
        }

        #region Tools

        private List<byte[]> SplitFile(string text, int chunkSize, out int fullSize)
        {
            var stream = (IO.MemoryStream)RepositoryTools.GetStreamFromString(text);
            var buffer = stream.GetBuffer();
            var bytes = new byte[text.Length + 3];
            fullSize = bytes.Length;

            Array.Copy(buffer, 0, bytes, 0, bytes.Length);

            var chunks = new List<byte[]>();
            //var bytes = Encoding.UTF8.GetBytes(text);
            var p = 0;
            while (p < bytes.Length)
            {
                var size = Math.Min(chunkSize, bytes.Length - p);
                var chunk = new byte[size];
                Array.Copy(bytes, p, chunk, 0, size);
                chunks.Add(chunk);
                p += chunkSize;
            }
            return chunks;
        }

        protected class DbFile
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
        protected DbFile[] LoadDbFiles(int versionId, string propertyName = "Binary")
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
                    file.Stream = reader.GetSafeBytes(reader.GetOrdinal("Stream"));
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

        protected Node CreateTestRoot()
        {
            var root = new SystemFolder(Repository.Root) {Name = Guid.NewGuid().ToString()};
            root.Save();
            return root;
        }

        protected string GetStringFromBytes(byte[] bytes)
        {
            using (var stream = new IO.MemoryStream(bytes))
            using (var reader = new IO.StreamReader(stream))
                return reader.ReadToEnd();
        }

        protected class SqlFileStreamSizeSwindler : IDisposable
        {
            private readonly BlobStorageIntegrationTests _testClass;
            private readonly int _originalValue;

            public SqlFileStreamSizeSwindler(BlobStorageIntegrationTests testClass, int cheat)
            {
                _testClass = testClass;
                testClass.ConfigureMinimumSizeForFileStreamInBytes(cheat, out _originalValue);
            }
            public void Dispose()
            {
                _testClass.ConfigureMinimumSizeForFileStreamInBytes(_originalValue, out _);
            }
        }

        #endregion
    }
}
