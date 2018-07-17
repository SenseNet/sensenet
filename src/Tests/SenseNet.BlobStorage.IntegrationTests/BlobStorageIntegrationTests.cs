using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
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
using SenseNet.ContentRepository.Storage.Schema;
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

        protected abstract string DatabaseName { get; }
        protected abstract bool SqlFsEnabled { get; }
        protected abstract bool SqlFsUsed { get; }
        protected abstract Type ExpectedExternalBlobProviderType { get; }
        protected abstract Type ExpectedMetadataProviderType { get; }
        protected abstract Type ExpectedBlobProviderDataType { get; }
        protected internal abstract void ConfigureMinimumSizeForFileStreamInBytes(int newValue, out int oldValue);

        protected virtual void UpdateFileCreationDate(int fileId, DateTime dateTime)
        {
            var sql = $"UPDATE Files SET CreationDate = @CreationDate WHERE FileId = {fileId}";
            using (var proc = DataProvider.CreateDataProcedure(sql))
            {
                proc.CommandType = CommandType.Text;
                var parameter = DataProvider.CreateParameter();
                parameter.ParameterName = "@CreationDate";
                parameter.Value = dateTime;
                proc.Parameters.Add(parameter);
                proc.ExecuteNonQuery();
            }
        }

        private static readonly string ConnetionStringBase = @"Data Source=.\SQL2016;Integrated Security=SSPI;Persist Security Info=False";
        private string GetConnectionString(string databaseName = null)
        {
            return $"Initial Catalog={databaseName ?? DatabaseName};{ConnetionStringBase}";
        }

        [AssemblyInitialize]
        public static void StartAllTests(TestContext testContext)
        {
            SnTrace.SnTracers.Clear();
            SnTrace.SnTracers.Add(new SnFileSystemTracer());
            SnTrace.SnTracers.Add(new SnDebugViewTracer());
            SnTrace.Test.Enabled = true;
            SnTrace.Test.Write("------------------------------------------------------");
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

            SnTrace.Test.Enabled = true;
            SnTrace.Test.Write("START test: {0}", TestContext.TestName);

            var prepared = Instances.TryGetValue(GetType(), out var instance);
            if (!prepared)
            {
                using (var op = SnTrace.Test.StartOperation("Initialize {0}", DatabaseName))
                {
                    ContentTypeManager.Reset();
                    //ActiveSchema.Reset();

                    _connectionStringBackup = ConnectionStrings.ConnectionString;
                    _securityConnectionStringBackup = ConnectionStrings.SecurityDatabaseConnectionString;
                    _connectionString = GetConnectionString(DatabaseName);
                    ConnectionStrings.ConnectionString = _connectionString;
                    ConnectionStrings.SecurityDatabaseConnectionString = _connectionString;

                    PrepareDatabase();

                    RepositoryInstance repositoryInstance;
                    using (repositoryInstance = Repository.Start(CreateRepositoryBuilder()))
                    using (new SystemAccount())
                        PrepareRepository();
                    _repositoryInstance = repositoryInstance;

                    new SnMaintenance().Shutdown();

                    Instances[GetType()] = this;

                    op.Successful = true;
                }
            }
            else
            {
                ConnectionStrings.ConnectionString = instance._connectionString;
                ConnectionStrings.SecurityDatabaseConnectionString = instance._connectionString;

                BuiltInBlobProviderSelector.ExternalBlobProvider = ExpectedExternalBlobProviderType == null
                    ? null
                    : (IBlobProvider)Activator.CreateInstance(ExpectedExternalBlobProviderType);
            }

            Assert.AreEqual(typeof(BuiltInBlobProviderSelector), BlobStorageComponents.ProviderSelector.GetType());
            Assert.AreEqual(ExpectedExternalBlobProviderType, BuiltInBlobProviderSelector.ExternalBlobProvider?.GetType());
            Assert.AreEqual(ExpectedMetadataProviderType, BlobStorageComponents.DataProvider.GetType());
        }
        [TestCleanup]
        public void CleanupTest()
        {
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
            {
                ExecuteSqlScriptNative(
                    IO.Path.Combine(IO.Path.GetFullPath(IO.Path.Combine(
                                AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\MsSqlFsBlobProvider\Scripts")),
                        @"EnableFilestream.sql"), DatabaseName);
            }
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
            var scripts = sql.Split(new[] { "\r\nGO" }, StringSplitOptions.RemoveEmptyEntries);

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
                    return (T)proc.ExecuteScalar();
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
        protected RepositoryBuilder CreateRepositoryBuilder()
        {
            Configuration.BlobStorage.BlobProviderClassName = ExpectedExternalBlobProviderType?.FullName;
            BuiltInBlobProviderSelector.ExternalBlobProvider = null; // reset external provider

            var blobMetaDataProvider = (IBlobStorageMetaDataProvider)Activator.CreateInstance(ExpectedMetadataProviderType);

            var builder = new RepositoryBuilder()
                .UseAccessProvider(new DesktopAccessProvider())
                .UseSearchEngine(new InMemorySearchEngine())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                //.StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(SettingsCache))
                .UseTraceCategories("Test", "Event", "Custom")
                .UseBlobMetaDataProvider(blobMetaDataProvider)
                .UseBlobProviderSelector(new BuiltInBlobProviderSelector());

            BlobStorageComponents.DataProvider = blobMetaDataProvider;

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
            var populator = SearchManager.GetIndexPopulator();
            populator.NodeIndexed += (o, e) => { /* collect paths if there is any problem */ };
            populator.ClearAndPopulateAll();
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

            if (NeedExternal())
            {
                Assert.IsNull(dbFile.Stream);
                Assert.IsNull(dbFile.FileStream);
                Assert.AreEqual(dbFile.Size, dbFile.ExternalStream.Length);
                Assert.AreEqual(expectedText, GetStringFromBytes(dbFile.ExternalStream));

                Assert.AreEqual(ExpectedBlobProviderDataType, ctx.BlobProviderData.GetType());
            }
            else if (SqlFsUsed)
            {
                Assert.IsNull(dbFile.Stream);
                Assert.IsNotNull(dbFile.FileStream);
                Assert.AreEqual(dbFile.Size, dbFile.FileStream.Length);
                Assert.AreEqual(expectedText, GetStringFromBytes(dbFile.FileStream));

                Assert.IsTrue(ctx.BlobProviderData is SqlFileStreamBlobProviderData);
            }
            else
            {
                Assert.IsNotNull(dbFile.Stream);
                Assert.IsNull(dbFile.FileStream);
                Assert.AreEqual(dbFile.Size, dbFile.Stream.Length);
                Assert.AreEqual(expectedText, GetStringFromBytes(dbFile.Stream));

                Assert.IsTrue(ctx.BlobProviderData is BuiltinBlobProviderData);
            }
        }
        private DbFile CreateFileTest(string fileContent, int sizeLimit)
        {
            using (new SystemAccount())
            using (new SizeLimitSwindler(this, sizeLimit))
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
                if (NeedExternal(fileContent, sizeLimit))
                {
                    Assert.AreEqual(ExpectedExternalBlobProviderType.FullName, dbFile.BlobProvider);
                    Assert.IsNotNull(dbFile.BlobProviderData);
                }
                else
                {
                    Assert.IsNull(dbFile.BlobProvider);
                    Assert.IsNull(dbFile.BlobProviderData);
                }
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

            if (NeedExternal())
            {
                Assert.IsTrue(dbFile.Stream == null || dbFile.Stream.Length == 0);
                Assert.IsTrue(dbFile.FileStream == null || dbFile.FileStream.Length == 0);
                Assert.AreEqual(dbFile.Size, dbFile.ExternalStream.Length);
                Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.ExternalStream));
            }
            else if (SqlFsUsed)
            {
                Assert.IsNull(dbFile.Stream);
                Assert.IsNotNull(dbFile.FileStream);
                Assert.AreEqual(dbFile.Size, dbFile.FileStream.Length);
                Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.FileStream));
            }
            else
            {
                Assert.IsNotNull(dbFile.Stream);
                Assert.IsNull(dbFile.FileStream);
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

            if (NeedExternal())
            {
                Assert.IsTrue(dbFile.Stream == null || dbFile.Stream.Length == 0);
                Assert.IsTrue(dbFile.FileStream == null || dbFile.FileStream.Length == 0);
                Assert.AreEqual(dbFile.Size, dbFile.ExternalStream.Length);
                Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.ExternalStream));
            }
            else if (SqlFsUsed)
            {
                Assert.IsNull(dbFile.Stream);
                Assert.IsNotNull(dbFile.FileStream);
                Assert.AreEqual(dbFile.Size, dbFile.FileStream.Length);
                Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.FileStream));
            }
            else
            {
                Assert.IsNotNull(dbFile.Stream);
                Assert.IsNull(dbFile.FileStream);
                Assert.AreEqual(dbFile.Size, dbFile.Stream.Length);
                Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.Stream));
            }
        }
        private DbFile UpdateFileTest(string initialContent, string updatedContent, int sizeLimit)
        {
            using (new SystemAccount())
            using (new SizeLimitSwindler(this, sizeLimit))
            {
                var testRoot = CreateTestRoot();

                var file = new File(testRoot) { Name = "File1.file" };
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(initialContent));
                file.Save();
                var fileId = file.Id;
                file = Node.Load<File>(fileId);
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(updatedContent));

                // action
                file.Save();

                // assert
                var dbFiles = LoadDbFiles(file.VersionId);
                Assert.AreEqual(1, dbFiles.Length);
                var dbFile = dbFiles[0];
                //Assert.AreNotEqual(initialBlobId, file.Binary.FileId);
                if (NeedExternal(updatedContent, sizeLimit))
                {
                    Assert.AreEqual(ExpectedExternalBlobProviderType.FullName, dbFile.BlobProvider);
                    Assert.IsNotNull(dbFile.BlobProviderData);
                }
                else
                {
                    Assert.IsNull(dbFile.BlobProvider);
                    Assert.IsNull(dbFile.BlobProviderData);
                }
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

            if (NeedExternal())
            {
                Assert.IsNull(dbFile.Stream);
                Assert.IsTrue(dbFile.FileStream == null || dbFile.FileStream.Length == 0);
                Assert.AreEqual(dbFile.Size, dbFile.ExternalStream.Length);
                Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.ExternalStream));
            }
            else if (SqlFsUsed)
            {
                Assert.IsNull(dbFile.Stream);
                Assert.IsNotNull(dbFile.FileStream);
                Assert.AreEqual(dbFile.Size, dbFile.FileStream.Length);
                Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.FileStream));
            }
            else
            {
                Assert.IsNotNull(dbFile.Stream);
                Assert.IsNull(dbFile.FileStream);
                Assert.AreEqual(dbFile.Size, dbFile.Stream.Length);
                Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.Stream));
            }
        }
        private DbFile UpdateByChunksTest(string initialContent, string updatedText, int sizeLimit, int chunkSize)
        {
            using (new SystemAccount())
            using (new SizeLimitSwindler(this, sizeLimit))
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
                if (NeedExternal(updatedText, sizeLimit))
                {
                    Assert.AreEqual(ExpectedExternalBlobProviderType.FullName, dbFile.BlobProvider);
                    Assert.IsNotNull(dbFile.BlobProviderData);
                }
                else
                {
                    Assert.IsNull(dbFile.BlobProvider);
                    Assert.IsNull(dbFile.BlobProviderData);
                }
                Assert.AreEqual(false, dbFile.IsDeleted);
                Assert.AreEqual(false, dbFile.Staging);
                Assert.AreEqual(0, dbFile.StagingVersionId);
                Assert.AreEqual(0, dbFile.StagingPropertyTypeId);
                Assert.AreEqual(fullSize, dbFile.Size);

                return dbFile;
            }
        }


        public void TestCase09_DeleteBinaryPropertySmall()
        {
            var initialText = "Lorem ipsum dolo sit amet..";
            DeleteBinaryPropertyTest(initialText, 222);
        }
        public void TestCase10_DeleteBinaryPropertyBig()
        {
            var initialText = "Lorem ipsum dolo sit amet..";
            DeleteBinaryPropertyTest(initialText, 20);
        }
        private void DeleteBinaryPropertyTest(string fileContent, int sizeLimit)
        {
            using (new SystemAccount())
            using (new SizeLimitSwindler(this, sizeLimit))
            {
                var testRoot = CreateTestRoot();

                var file = new File(testRoot) { Name = "File1.file" };
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(fileContent));
                file.Save();
                var fileId = file.Id;

                file = Node.Load<File>(fileId);
                // action
                file.Binary = null;
                file.Save();

                // assert
                var dbFiles = LoadDbFiles(file.VersionId);
                Assert.AreEqual(0, dbFiles.Length);
            }
        }


        public void TestCase11_CopyfileRowSmall()
        {
            // 20 chars:       |------------------|
            // 10 chars:       |--------|---------|---------|
            var initialText = "Lorem ipsum dolo sit amet..";
            var updatedText = "Cras lobortis consequat nisi..";
            var dbFiles = CopyfileRowTest(initialText, updatedText, 222);

            Assert.IsNull(dbFiles[0].FileStream);
            Assert.IsNotNull(dbFiles[0].Stream);
            Assert.AreEqual(dbFiles[0].Size, dbFiles[0].Stream.Length);
            Assert.AreEqual(initialText, GetStringFromBytes(dbFiles[0].Stream));

            Assert.IsNull(dbFiles[1].FileStream);
            Assert.IsNotNull(dbFiles[1].Stream);
            Assert.AreEqual(dbFiles[1].Size, dbFiles[1].Stream.Length);
            Assert.AreEqual(updatedText, GetStringFromBytes(dbFiles[1].Stream));
        }
        public void TestCase12_CopyfileRowBig()
        {
            // 20 chars:       |------------------|
            // 10 chars:       |--------|---------|---------|
            var initialText = "Lorem ipsum dolo sit amet..";
            var updatedText = "Cras lobortis consequat nisi..";
            var dbFiles = CopyfileRowTest(initialText, updatedText, 20);

            if (NeedExternal())
            {
                Assert.IsNull(dbFiles[0].Stream);
                Assert.IsTrue(dbFiles[0].FileStream == null || dbFiles[0].FileStream.Length == 0);
                Assert.AreEqual(dbFiles[0].Size, dbFiles[0].ExternalStream.Length);
                Assert.AreEqual(initialText, GetStringFromBytes(dbFiles[0].ExternalStream));

                Assert.IsTrue(dbFiles[1].Stream == null || dbFiles[1].Stream.Length == 0);
                Assert.IsTrue(dbFiles[1].FileStream == null || dbFiles[1].FileStream.Length == 0);
                Assert.AreEqual(dbFiles[1].Size, dbFiles[1].ExternalStream.Length);
                Assert.AreEqual(updatedText, GetStringFromBytes(dbFiles[1].ExternalStream));

            }
            else if (SqlFsUsed)
            {
                Assert.IsNull(dbFiles[0].Stream);
                Assert.IsNotNull(dbFiles[0].FileStream);
                Assert.AreEqual(dbFiles[0].Size, dbFiles[0].FileStream.Length);
                Assert.AreEqual(initialText, GetStringFromBytes(dbFiles[0].FileStream));

                Assert.IsNull(dbFiles[1].Stream);
                Assert.IsNotNull(dbFiles[1].FileStream);
                Assert.AreEqual(dbFiles[1].Size, dbFiles[1].FileStream.Length);
                Assert.AreEqual(updatedText, GetStringFromBytes(dbFiles[1].FileStream));
            }
            else
            {
                Assert.IsNull(dbFiles[0].FileStream);
                Assert.IsNotNull(dbFiles[0].Stream);
                Assert.AreEqual(dbFiles[0].Size, dbFiles[0].Stream.Length);
                Assert.AreEqual(initialText, GetStringFromBytes(dbFiles[0].Stream));

                Assert.IsNull(dbFiles[1].FileStream);
                Assert.IsNotNull(dbFiles[1].Stream);
                Assert.AreEqual(dbFiles[1].Size, dbFiles[1].Stream.Length);
                Assert.AreEqual(updatedText, GetStringFromBytes(dbFiles[1].Stream));
            }
        }
        private DbFile[] CopyfileRowTest(string initialContent, string updatedText, int sizeLimit)
        {
            using (new SystemAccount())
            using (new SizeLimitSwindler(this, sizeLimit))
            {
                var testRoot = CreateTestRoot();
                var target = new SystemFolder(testRoot) { Name = "Target" };
                target.Save();

                var file = new File(testRoot) { Name = "File1.file" };
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(initialContent));
                file.Save();

                // action
                file.CopyTo(target);

                // assert
                var copy = Node.Load<File>(RepositoryPath.Combine(target.Path, file.Name));
                Assert.AreNotEqual(file.Id, copy.Id);
                Assert.AreNotEqual(file.VersionId, copy.VersionId);
                Assert.AreEqual(file.Binary.FileId, copy.Binary.FileId);

                // action 2
                copy.Binary.SetStream(RepositoryTools.GetStreamFromString(updatedText));
                copy.Save();

                // assert 2
                Assert.AreNotEqual(file.Binary.FileId, copy.Binary.FileId);

                var dbFiles = new DbFile[2];

                var loadedDbFiles = LoadDbFiles(file.VersionId);
                Assert.AreEqual(1, loadedDbFiles.Length);
                dbFiles[0] = loadedDbFiles[0];

                loadedDbFiles = LoadDbFiles(copy.VersionId);
                Assert.AreEqual(1, loadedDbFiles.Length);
                dbFiles[1] = loadedDbFiles[0];

                if (NeedExternal(initialContent, sizeLimit))
                {
                    Assert.AreEqual(ExpectedExternalBlobProviderType.FullName, dbFiles[0].BlobProvider);
                    Assert.IsNotNull(dbFiles[0].BlobProviderData);
                }
                else
                {
                    Assert.IsNull(dbFiles[0].BlobProvider);
                    Assert.IsNull(dbFiles[0].BlobProviderData);
                }
                Assert.AreEqual(false, dbFiles[0].IsDeleted);
                Assert.AreEqual(false, dbFiles[0].Staging);
                Assert.AreEqual(0, dbFiles[0].StagingVersionId);
                Assert.AreEqual(0, dbFiles[0].StagingPropertyTypeId);
                Assert.AreEqual(initialContent.Length + 3, dbFiles[0].Size);

                if (NeedExternal(updatedText, sizeLimit))
                {
                    Assert.AreEqual(ExpectedExternalBlobProviderType.FullName, dbFiles[1].BlobProvider);
                    Assert.IsNotNull(dbFiles[1].BlobProviderData);
                }
                else
                {
                    Assert.IsNull(dbFiles[1].BlobProvider);
                    Assert.IsNull(dbFiles[1].BlobProviderData);
                }
                Assert.AreEqual(false, dbFiles[1].IsDeleted);
                Assert.AreEqual(false, dbFiles[1].Staging);
                Assert.AreEqual(0, dbFiles[1].StagingVersionId);
                Assert.AreEqual(0, dbFiles[1].StagingPropertyTypeId);
                Assert.AreEqual(updatedText.Length + 3, dbFiles[1].Size);

                return dbFiles;
            }
        }


        public void TestCase13_BinaryCacheEntitySmall()
        {
            var expectedText = "Lorem ipsum dolo sit amet";
            BinaryCacheEntityTest(expectedText, expectedText.Length + 10);
        }
        public void TestCase14_BinaryCacheEntityBig()
        {
            var expectedText = "Lorem ipsum dolo sit amet";
            BinaryCacheEntityTest(expectedText, expectedText.Length - 10);
        }
        private void BinaryCacheEntityTest(string fileContent, int sizeLimit)
        {
            using (new SystemAccount())
            using (new SizeLimitSwindler(this, sizeLimit))
            {
                var testRoot = CreateTestRoot();

                var file = new File(testRoot) { Name = "File1.file" };
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(fileContent));
                file.Save();
                var versionId = file.VersionId;
                var binaryPropertyId = file.Binary.Id;
                var fileId = file.Binary.FileId;
                var propertyTypeId = PropertyType.GetByName("Binary").Id;

                // action
                var binaryCacheEntity = DataProvider.Current.LoadBinaryCacheEntity(file.VersionId, propertyTypeId);

                // assert
                Assert.AreEqual(binaryPropertyId, binaryCacheEntity.BinaryPropertyId);
                Assert.AreEqual(fileId, binaryCacheEntity.FileId);
                Assert.AreEqual(fileContent.Length + 3, binaryCacheEntity.Length);

                Assert.AreEqual(versionId, binaryCacheEntity.Context.VersionId);
                Assert.AreEqual(propertyTypeId, binaryCacheEntity.Context.PropertyTypeId);
                Assert.AreEqual(fileId, binaryCacheEntity.Context.FileId);
                Assert.AreEqual(fileContent.Length + 3, binaryCacheEntity.Context.Length);

                if (NeedExternal(fileContent, sizeLimit))
                {
                    Assert.IsTrue(binaryCacheEntity.Context.Provider.GetType() == ExpectedExternalBlobProviderType);
                    Assert.IsTrue(binaryCacheEntity.Context.BlobProviderData.GetType() == ExpectedBlobProviderDataType);
                    Assert.AreEqual(fileContent, GetStringFromBytes(GetExternalData(binaryCacheEntity.Context)));
                }
                else
                {
                    Assert.AreEqual(fileContent, GetStringFromBytes(binaryCacheEntity.RawData));
                }
            }
        }


        public void TestCase15_DeleteSmall()
        {
            var expectedText = "Lorem ipsum dolo sit amet";
            DeleteTest(expectedText, expectedText.Length + 10);
        }
        public void TestCase16_DeleteBig()
        {
            var expectedText = "Lorem ipsum dolo sit amet";
            DeleteTest(expectedText, expectedText.Length - 10);
        }
        private void DeleteTest(string fileContent, int sizeLimit)
        {
            using (new SystemAccount())
            using (new SizeLimitSwindler(this, sizeLimit))
            {
                var propertyTypeId = PropertyType.GetByName("Binary").Id;
                var external = NeedExternal(fileContent, sizeLimit);

                var testRoot = CreateTestRoot();

                var file = new File(testRoot) { Name = "File1.file" };
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(fileContent));
                file.Save();
                var fileId = file.Binary.FileId;
                // memorize blob storage context for further check
                var ctx = BlobStorageComponents.DataProvider.GetBlobStorageContext(file.Binary.FileId, false,
                    file.VersionId, propertyTypeId);
                UpdateFileCreationDate(file.Binary.FileId, DateTime.UtcNow.AddDays(-1));

                // Action #1
                file.ForceDelete();

                // Assert #1
                var dbFile = LoadDbFile(fileId);
                Assert.IsNotNull(dbFile);
                Assert.AreEqual(false, dbFile.IsDeleted);
                Assert.IsFalse(IsDeleted(ctx, external));

                // Action #2
                ContentRepository.Storage.Data.BlobStorage.CleanupFilesSetFlag();

                // Assert #2
                dbFile = LoadDbFile(fileId);
                Assert.IsNotNull(dbFile);
                Assert.AreEqual(true, dbFile.IsDeleted);
                Assert.IsFalse(IsDeleted(ctx, external));

                // Action #3
                ContentRepository.Storage.Data.BlobStorage.CleanupFiles();

                // Assert #3
                dbFile = LoadDbFile(fileId);
                Assert.IsNull(dbFile);
                Assert.IsTrue(IsDeleted(ctx, external));
            }
        }

        private bool IsDeleted(BlobStorageContext context, bool external)
        {
            return external
                ? GetExternalData(context) == null
                : LoadDbFile(context.FileId) == null;
        }

        #region Tools

        private bool NeedExternal(string fileContent, int sizeLimit)
        {
            if (fileContent.Length + 3 < sizeLimit)
                return false;
            return NeedExternal();
        }
        private bool NeedExternal()
        {
            if (ExpectedExternalBlobProviderType == null)
                return false;
            if (ExpectedExternalBlobProviderType == typeof(BuiltInBlobProvider))
                return false;
            if (ExpectedExternalBlobProviderType == typeof(SqlFileStreamBlobProvider))
                return false;
            return true;
        }

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
            public byte[] ExternalStream;
        }
        protected DbFile[] LoadDbFiles(int versionId, string propertyName = "Binary")
        {
            var propTypeId = ActiveSchema.PropertyTypes[propertyName].Id;
            var sql = $@"SELECT f.* FROM BinaryProperties b JOIN Files f on f.FileId = b.FileId WHERE b.VersionId = {versionId} and b.PropertyTypeId = {propTypeId}";
            var dbFiles = new List<DbFile>();
            using (var reader = ExecuteSqlReader(sql))
                while (reader.Read())
                    dbFiles.Add(GetFileFromReader(reader));

            return dbFiles.ToArray();
        }
        protected DbFile LoadDbFile(int fileId)
        {
            var sql = $@"SELECT * FROM Files WHERE FileId = {fileId}";
            using (var reader = ExecuteSqlReader(sql))
                return reader.Read() ? GetFileFromReader(reader) : null;
        }

        private DbFile GetFileFromReader(IDataReader reader)
        {
            var file = new DbFile
            {
                FileId = reader.GetInt32(reader.GetOrdinal("FileId")),
                BlobProvider = reader.GetSafeString(reader.GetOrdinal("BlobProvider")),
                BlobProviderData = reader.GetSafeString(reader.GetOrdinal("BlobProviderData")),
                ContentType = reader.GetSafeString(reader.GetOrdinal("ContentType")),
                FileNameWithoutExtension = reader.GetSafeString(reader.GetOrdinal("FileNameWithoutExtension")),
                Extension = reader.GetSafeString(reader.GetOrdinal("Extension")),
                Size = reader.GetSafeInt64(reader.GetOrdinal("Size")),
                CreationDate = reader.GetSafeDateTime(reader.GetOrdinal("CreationDate")) ?? DateTime.MinValue,
                IsDeleted = reader.GetSafeBoolFromBit(reader.GetOrdinal("IsDeleted")),
                Staging = reader.GetSafeBoolFromBit(reader.GetOrdinal("Staging")),
                StagingPropertyTypeId = reader.GetSafeInt32(reader.GetOrdinal("StagingPropertyTypeId")),
                StagingVersionId = reader.GetSafeInt32(reader.GetOrdinal("StagingVersionId")),
                Stream = reader.GetSafeBytes(reader.GetOrdinal("Stream")),
                Checksum = reader.GetSafeString(reader.GetOrdinal("Checksum")),
                RowGuid = reader.GetGuid(reader.GetOrdinal("RowGuid")),
                Timestamp = DataProvider.GetLongFromBytes((byte[])reader[reader.GetOrdinal("Timestamp")])
            };
            if (reader.FieldCount > 16)
                file.FileStream = reader.GetSafeBytes(reader.GetOrdinal("FileStream"));
            file.ExternalStream = GetExternalData(file);

            return file;
        }
        private byte[] GetExternalData(DbFile file)
        {
            if (file.BlobProvider == null)
                return new byte[0];

            var provider = BlobStorageBase.GetProvider(file.BlobProvider);
            var context = new BlobStorageContext(provider, file.BlobProviderData) {Length = file.Size};
            return GetExternalData(context);
        }
        private byte[] GetExternalData(BlobStorageContext context)
        {
            try
            {
                using (var stream = context.Provider.GetStreamForRead(context))
                {
                    var buffer = new byte[stream.Length.ToInt()];
                    stream.Read(buffer, 0, buffer.Length);
                    return buffer;
                }
            }
            catch
            {
                return null;
            }
        }

        protected Node CreateTestRoot()
        {
            var root = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
            root.Save();
            return root;
        }

        protected string GetStringFromBytes(byte[] bytes)
        {
            using (var stream = new IO.MemoryStream(bytes))
            using (var reader = new IO.StreamReader(stream))
                return reader.ReadToEnd();
        }

        protected class SizeLimitSwindler : IDisposable
        {
            private readonly BlobStorageIntegrationTests _testClass;
            private readonly int _originalValue;

            public SizeLimitSwindler(BlobStorageIntegrationTests testClass, int cheat)
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
