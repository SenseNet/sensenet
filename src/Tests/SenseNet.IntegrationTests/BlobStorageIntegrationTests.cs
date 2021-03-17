using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using IO = System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.BackgroundOperations;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.IntegrationTests.Common;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.Testing;
using SenseNet.Tests.Core;
using SenseNet.Tests.Core.Implementations;
using STT = System.Threading.Tasks;
// ReSharper disable AccessToDisposedClosure

namespace SenseNet.BlobStorage.IntegrationTests
{
    [TestClass]
    public abstract class BlobStorageIntegrationTests
    {
        //UNDONE:<?: don't use hardcoded ConnectionStringForBlobStorageTests
        private string ConnectionStringForBlobStorageTests =
            @"Data Source=.\SQL2016;Integrated Security=SSPI;Persist Security Info=False";

        #region Infrastructure

        public TestContext TestContext { get; set; }

        private static readonly Dictionary<Type, BlobStorageIntegrationTests> Instances =
            new Dictionary<Type, BlobStorageIntegrationTests>();

        private string DatabaseName = "sn7blobtests_builtin";
        private Type ExpectedMetadataProviderType = typeof(MsSqlBlobMetaDataProvider);

        protected abstract Type ExpectedExternalBlobProviderType { get; }
        protected abstract Type ExpectedBlobProviderDataType { get; }
        protected internal abstract void ConfigureMinimumSizeForFileStreamInBytes(int newValue, out int oldValue);

        protected virtual void UpdateFileCreationDate(int fileId, DateTime dateTime)
        {
            var sql = $"UPDATE Files SET CreationDate = @CreationDate WHERE FileId = {fileId}";
            using (var ctx = GetDataContext())
            {
                ctx.ExecuteNonQueryAsync(sql, cmd =>
                {
                    cmd.Parameters.Add(ctx.CreateParameter("@CreationDate", DbType.DateTime2, dateTime));
                }).GetAwaiter().GetResult();
            }
        }

        private string GetConnectionString(string databaseName = null)
        {
            return $"Initial Catalog={databaseName ?? DatabaseName};{ConnectionStringForBlobStorageTests}";
        }

        //[AssemblyInitialize]
        public static void StartAllTests(TestContext testContext)
        {
            SnTrace.SnTracers.Clear();
            SnTrace.SnTracers.Add(new SnFileSystemTracer());
            SnTrace.SnTracers.Add(new SnDebugViewTracer());
            SnTrace.Test.Enabled = true;
            SnTrace.Test.Write("------------------------------------------------------");
        }

        //[AssemblyCleanup]
        public static void FinalizeAllTests()
        {
            _commonRepositoryInstance?.Dispose();
        }

        private string _connectionStringBackup;
        private string _securityConnectionStringBackup;
        private static RepositoryInstance _commonRepositoryInstance;
        [TestInitialize]
        public void Initialize()
        {
            SnTrace.Test.Enabled = true;
            SnTrace.Test.Write("START test: {0}", TestContext.TestName);

            _connectionStringBackup = ConnectionStrings.ConnectionString;
            _securityConnectionStringBackup = ConnectionStrings.SecurityDatabaseConnectionString;
            var connectionString = GetConnectionString(DatabaseName);
            ConnectionStrings.ConnectionString = connectionString;
            ConnectionStrings.SecurityDatabaseConnectionString = connectionString;

            if (_commonRepositoryInstance == null)
            {
                using (var op = SnTrace.Test.StartOperation("Initialize {0}", DatabaseName))
                {
                    ContentTypeManager.Reset();
                    //ActiveSchema.Reset();

                    PrepareDatabase();

                    RepositoryInstance repositoryInstance;
                    var builder = CreateRepositoryBuilder();
                    var securityDataProvider = Providers.Instance.SecurityDataProvider;
                    securityDataProvider.ConnectionString = ConnectionStrings.SecurityDatabaseConnectionString;
                    using (repositoryInstance = Repository.Start(builder))
                    using (new SystemAccount())
                        PrepareRepository();
                    _commonRepositoryInstance = repositoryInstance;

                    //UNDONE: new SnMaintenance().Shutdown();
                    //new SnMaintenance().Shutdown();

                    Instances[GetType()] = this;

                    op.Successful = true;
                }
            }
            else
            {
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
            if (_connectionStringBackup != null)
                ConnectionStrings.ConnectionString = _connectionStringBackup;
            if (_securityConnectionStringBackup != null)
                ConnectionStrings.SecurityDatabaseConnectionString = _securityConnectionStringBackup;

            SnTrace.Test.Enabled = true;
            SnTrace.Test.Write("END test: {0}", TestContext.TestName);
            SnTrace.Flush();
        }

        protected void PrepareDatabase()
        {
            var scriptRootPath = IO.Path.GetFullPath(IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\..\sensenet\src\Storage\Data\MsSqlClient\Scripts"));

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
            ExecuteSqlScriptNative(IO.Path.Combine(scriptRootPath, @"MsSqlInstall_Security.sql"), DatabaseName);
            ExecuteSqlScriptNative(IO.Path.Combine(scriptRootPath, @"MsSqlInstall_Schema.sql"), DatabaseName);
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

            /* ------------------------------------------------------------------------ */
            var dp2 = new MsSqlDataProvider();
            Providers.Instance.DataProvider = dp2;

            using (var op = SnTrace.Test.StartOperation("Install initial data."))
            {
                DataStore.InstallInitialDataAsync(GetInitialData(), CancellationToken.None).GetAwaiter().GetResult();
                op.Successful = true;
            }
            var inMemoryIndex = GetInitialIndex();

            dp2.SetExtension(typeof(ISharedLockDataProviderExtension), new MsSqlSharedLockDataProvider());
            dp2.SetExtension(typeof(IAccessTokenDataProviderExtension), new MsSqlAccessTokenDataProvider());
            dp2.SetExtension(typeof(ITestingDataProviderExtension), new MsSqlTestingDataProvider());

            /* ------------------------------------------------------------------------ */

            var builder = new RepositoryBuilder()
                .UseAccessProvider(new DesktopAccessProvider())
                .UseSearchEngine(new InMemorySearchEngine(inMemoryIndex))
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                //.StartIndexingEngine(false)
                .StartWorkflowEngine(false)
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(SettingsCache))
                .UseTraceCategories("Test", "Event", "Custom")
                .UseBlobMetaDataProvider(blobMetaDataProvider)
                .UseBlobProviderSelector(new BuiltInBlobProviderSelector());

            return builder as RepositoryBuilder;
        }
        private string[] LoadCtds()
        {
            var ctdRootPath = IO.Path.GetFullPath(IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\sensenet\src\nuget\snadmin\install-services\import\System\Schema\ContentTypes"));
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
            var idSet = DataStore.LoadNotIndexedNodeIdsAsync(0, 11000, CancellationToken.None).GetAwaiter().GetResult();
            var nodes = Node.LoadNodes(idSet);

            if (nodes.Count == 0)
                return;

            foreach (var node in nodes)
            {
                // ReSharper disable once UnusedVariable
                DataStore.SaveIndexDocumentAsync(node, false, false, CancellationToken.None).GetAwaiter().GetResult();
            }
        }
        protected void RebuildIndex()
        {
            var populator = SearchManager.GetIndexPopulator();
            populator.NodeIndexed += (o, e) => { /* collect paths if there is any problem */ };
            populator.ClearAndPopulateAllAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static InitialData _initialData;
        protected static InitialData GetInitialData()
        {
            var dataFile = InMemoryTestData.Instance;
            if (_initialData == null)
            {
                var initialData = InitialData.Load(dataFile, null);
                //initialData.ContentTypeDefinitions = dataFile.ContentTypeDefinitions;
                //initialData.Blobs = dataFile.Blobs;
                _initialData = initialData;
            }
            return _initialData;
        }

        private static InMemoryIndex _initialIndex;
        protected static InMemoryIndex GetInitialIndex()
        {
            if (_initialIndex == null)
            {
                var index = new InMemoryIndex();
                index.Load(new IO.StringReader(InMemoryTestIndex.Index));
                _initialIndex = index;
            }
            return _initialIndex.Clone();
        }

        #endregion

        public void TestCase_CreateFileSmall()
        {
            var expectedText = "Lorem ipsum dolo sit amet";
            var dbFile = CreateFileTest(expectedText, expectedText.Length + 10);
            var ctx = BlobStorageBase.GetBlobStorageContextAsync(dbFile.FileId, CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.IsNull(dbFile.FileStream);
            Assert.IsNotNull(dbFile.Stream);
            Assert.AreEqual(dbFile.Size, dbFile.Stream.Length);
            Assert.AreEqual(expectedText, GetStringFromBytes(dbFile.Stream));

            Assert.AreEqual(dbFile.FileId, ctx.FileId);
            Assert.AreEqual(dbFile.Size, ctx.Length);
            Assert.IsTrue(ctx.BlobProviderData is BuiltinBlobProviderData);
        }
        public void TestCase_CreateFileBig()
        {
            var expectedText = "Lorem ipsum dolo sit amet";
            var dbFile = CreateFileTest(expectedText, expectedText.Length - 10);
            var ctx = BlobStorageBase.GetBlobStorageContextAsync(dbFile.FileId, CancellationToken.None)
                .GetAwaiter().GetResult();

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


        public void TestCase_UpdateFileSmallEmpty()
        {
            // 20 chars:       |------------------|
            var initialText = "Lorem ipsum...";
            var updatedText = string.Empty;
            var dbFile = UpdateFileTest(initialText, updatedText, 20);

            Assert.IsNull(dbFile.FileStream);
            Assert.IsNotNull(dbFile.Stream);
            Assert.AreEqual(0L, dbFile.Size);
            Assert.AreEqual(dbFile.Size, dbFile.Stream.Length);
            Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.Stream));
        }
        public void TestCase_UpdateFileBigEmpty()
        {
            // 20 chars:       |------------------|
            var initialText = "Lorem ipsum dolo sit amet...";
            var updatedText = string.Empty;
            var dbFile = UpdateFileTest(initialText, updatedText, 20);

            Assert.IsNull(dbFile.FileStream);
            Assert.IsNotNull(dbFile.Stream);
            Assert.AreEqual(0L, dbFile.Size);
            Assert.AreEqual(dbFile.Size, dbFile.Stream.Length);
            Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.Stream));
        }
        public void TestCase_UpdateFileSmallSmall()
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
        public void TestCase_UpdateFileSmallBig()
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
            else
            {
                Assert.IsNotNull(dbFile.Stream);
                Assert.IsNull(dbFile.FileStream);
                Assert.AreEqual(dbFile.Size, dbFile.Stream.Length);
                Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.Stream));
            }
        }
        public void TestCase_UpdateFileBigSmall()
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
        public void TestCase_UpdateFileBigBig()
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
                if(updatedContent.Length == 0)
                    Assert.AreEqual(0, dbFile.Size);
                else
                    Assert.AreEqual(updatedContent.Length + 3, dbFile.Size);

                return dbFile;
            }
        }


        public void TestCase_WriteChunksSmall()
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
        public void TestCase_WriteChunksBig()
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


        public void TestCase_DeleteBinaryPropertySmall()
        {
            var initialText = "Lorem ipsum dolo sit amet..";
            DeleteBinaryPropertyTest(initialText, 222);
        }
        public void TestCase_DeleteBinaryPropertyBig()
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


        public void TestCase_CopyfileRowSmall()
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
        public void TestCase_CopyfileRowBig()
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


        public void TestCase_BinaryCacheEntitySmall()
        {
            var expectedText = "Lorem ipsum dolo sit amet";
            BinaryCacheEntityTest(expectedText, expectedText.Length + 10);
        }
        public void TestCase_BinaryCacheEntityBig()
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
                var binaryCacheEntity = ContentRepository.Storage.Data.BlobStorage.LoadBinaryCacheEntityAsync(
                    file.VersionId, propertyTypeId, CancellationToken.None).GetAwaiter().GetResult();

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


        public void TestCase_DeleteSmall()
        {
            var expectedText = "Lorem ipsum dolo sit amet";
            DeleteTest(expectedText, expectedText.Length + 10);
        }
        public void TestCase_DeleteBig()
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
                var ctx = BlobStorageComponents.DataProvider.GetBlobStorageContextAsync(file.Binary.FileId, false,
                    file.VersionId, propertyTypeId, CancellationToken.None).GetAwaiter().GetResult();
                UpdateFileCreationDate(file.Binary.FileId, DateTime.UtcNow.AddDays(-1));

                // Action #1
                file.ForceDelete();

                // Assert #1
                var dbFile = LoadDbFile(fileId);
                Assert.IsNotNull(dbFile);
                Assert.AreEqual(false, dbFile.IsDeleted);
                Assert.IsFalse(IsDeleted(ctx, external));

                // Action #2
                ContentRepository.Storage.Data.BlobStorage.CleanupFilesSetFlagAsync(CancellationToken.None)
                    .GetAwaiter().GetResult();

                // Assert #2
                dbFile = LoadDbFile(fileId);
                Assert.IsNotNull(dbFile);
                Assert.AreEqual(true, dbFile.IsDeleted);
                Assert.IsFalse(IsDeleted(ctx, external));

                // Action #3
                var _ = ContentRepository.Storage.Data.BlobStorage.CleanupFilesAsync(CancellationToken.None)
                    .GetAwaiter().GetResult();

                // Assert #3
                dbFile = LoadDbFile(fileId);
                Assert.IsNull(dbFile);
                Assert.IsTrue(IsDeleted(ctx, external));
            }
        }

        public void TestCase_DeletionPolicy_Default()
        {
            var dp = DataStore.DataProvider;
            var tdp = DataStore.GetDataProviderExtension<ITestingDataProviderExtension>();

            Assert.AreEqual(BlobDeletionPolicy.BackgroundDelayed, Configuration.BlobStorage.BlobDeletionPolicy);
            var countsBefore = GetDbObjectCountsAsync(null, dp, tdp).ConfigureAwait(false).GetAwaiter().GetResult();

            DeletionPolicy_TheTest();

            var countsAfter = GetDbObjectCountsAsync(null, dp, tdp).ConfigureAwait(false).GetAwaiter().GetResult();
            Assert.AreEqual(countsBefore.AllCountsExceptFiles, countsAfter.AllCountsExceptFiles);
            Assert.AreNotEqual(countsBefore.Files, countsAfter.Files);
            Thread.Sleep(500);
            Assert.AreNotEqual(countsBefore.Files, countsAfter.Files);
        }
        public void TestCase_DeletionPolicy_Immediately()
        {
            var dp = DataStore.DataProvider;
            var tdp = DataStore.GetDataProviderExtension<ITestingDataProviderExtension>();
            var countsBefore = GetDbObjectCountsAsync(null, dp, tdp).ConfigureAwait(false).GetAwaiter().GetResult();

            using (new BlobDeletionPolicySwindler(BlobDeletionPolicy.Immediately))
                DeletionPolicy_TheTest();

            var countsAfter = GetDbObjectCountsAsync(null, dp, tdp).ConfigureAwait(false).GetAwaiter().GetResult();
            Assert.AreEqual(countsBefore.Files, countsAfter.Files);
            Assert.AreEqual(countsBefore.AllCounts, countsAfter.AllCounts);
        }
        public void TestCase_DeletionPolicy_BackgroundImmediately()
        {
            var dp = DataStore.DataProvider;
            var tdp = DataStore.GetDataProviderExtension<ITestingDataProviderExtension>();
            var countsBefore = GetDbObjectCountsAsync(null, dp, tdp).ConfigureAwait(false).GetAwaiter().GetResult();

            using (new BlobDeletionPolicySwindler(BlobDeletionPolicy.BackgroundImmediately))
                DeletionPolicy_TheTest();

            var countsAfter = GetDbObjectCountsAsync(null, dp, tdp).ConfigureAwait(false).GetAwaiter().GetResult();
            Assert.AreEqual(countsBefore.AllCountsExceptFiles, countsAfter.AllCountsExceptFiles);
            Assert.AreNotEqual(countsBefore.Files, countsAfter.Files);
            Thread.Sleep(500);
            Assert.AreEqual(countsBefore.Files, countsAfter.Files);
        }
        private void DeletionPolicy_TheTest()
        {
            using (new SystemAccount())
            {
                // Create a small subtree
                var root = new SystemFolder(Repository.Root) {Name = "TestRoot"};
                root.Save();
                var f1 = new SystemFolder(root) {Name = "F1"};
                f1.Save();
                var f2 = new File(root) {Name = "F2"};
                f2.Binary.SetStream(RepositoryTools.GetStreamFromString("filecontent"));
                f2.Save();
                var f3 = new SystemFolder(f1) {Name = "F3"};
                f3.Save();
                var f4 = new File(root) {Name = "F4"};
                f4.Binary.SetStream(RepositoryTools.GetStreamFromString("filecontent"));
                f4.Save();

                // ACTION
                Node.ForceDelete(root.Path);

                // ASSERT
                Assert.IsNull(Node.Load<SystemFolder>(root.Id));
                Assert.IsNull(Node.Load<SystemFolder>(f1.Id));
                Assert.IsNull(Node.Load<File>(f2.Id));
                Assert.IsNull(Node.Load<SystemFolder>(f3.Id));
                Assert.IsNull(Node.Load<File>(f4.Id));
            }
        }
        private async STT.Task<(int Nodes, int Versions, int Binaries, int Files, int LongTexts, string AllCounts, string AllCountsExceptFiles)> GetDbObjectCountsAsync(string path, DataProvider dp, ITestingDataProviderExtension tdp)
        {
            var nodesTask = dp.GetNodeCountAsync(path, CancellationToken.None);
            var versionsTask = dp.GetVersionCountAsync(path, CancellationToken.None);
            var binariesTasks = tdp.GetBinaryPropertyCountAsync(path);
            var filesTask = tdp.GetFileCountAsync(path);
            var longTextsTask = tdp.GetLongTextCountAsync(path);

            STT.Task.WaitAll(nodesTask, versionsTask, binariesTasks, filesTask, longTextsTask);

            var nodes = nodesTask.Result;
            var versions = versionsTask.Result;
            var binaries = binariesTasks.Result;
            var files = filesTask.Result;
            var longTexts = longTextsTask.Result;
            
            var all = $"{nodes},{versions},{binaries},{files},{longTexts}";
            var allExceptFiles = $"{nodes},{versions},{binaries},{longTexts}";

            var result = (Nodes: nodes, Versions: versions, Binaries: binaries, Files: files, LongTexts: longTexts, AllCounts: all, AllCountsExceptFiles: allExceptFiles);
            return await STT.Task.FromResult(result);
        }
        private class BlobDeletionPolicySwindler : Swindler<BlobDeletionPolicy>
        {
            public BlobDeletionPolicySwindler(BlobDeletionPolicy hack) : base(
                hack,
                () => Configuration.BlobStorage.BlobDeletionPolicy,
                (value) => { Configuration.BlobStorage.BlobDeletionPolicy = value; })
            {
            }
        }



        private bool IsDeleted(BlobStorageContext context, bool external)
        {
            return external
                ? GetExternalData(context) == null
                : LoadDbFile(context.FileId) == null;
        }

        #region Tools

        private SnDataContext GetDataContext()
        {
            return ((RelationalDataProviderBase)DataStore.DataProvider).CreateDataContext(CancellationToken.None);
        }

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

        //protected class DbFile
        //{
        //    public int FileId;
        //    public string ContentType;
        //    public string FileNameWithoutExtension;
        //    public string Extension;
        //    public long Size;
        //    public string Checksum;
        //    public byte[] Stream;
        //    public DateTime CreationDate;
        //    public Guid RowGuid;
        //    public long Timestamp;
        //    public bool? Staging;
        //    public int StagingVersionId;
        //    public int StagingPropertyTypeId;
        //    public bool? IsDeleted;
        //    public string BlobProvider;
        //    public string BlobProviderData;
        //    public byte[] FileStream;
        //    public byte[] ExternalStream;
        //}
/**/        protected DbFile[] LoadDbFiles(int versionId, string propertyName = "Binary")
        {
            var propTypeId = ActiveSchema.PropertyTypes[propertyName].Id;
            var sql = $@"SELECT f.* FROM BinaryProperties b JOIN Files f on f.FileId = b.FileId WHERE b.VersionId = {versionId} and b.PropertyTypeId = {propTypeId}";
            var dbFiles = new List<DbFile>();

            using (var ctx = GetDataContext())
            {
                var _ = ctx.ExecuteReaderAsync(sql, async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    while (await reader.ReadAsync(cancel))
                    {
                        cancel.ThrowIfCancellationRequested();
                        dbFiles.Add(GetFileFromReader(reader));
                    }
                    return true;
                }).GetAwaiter().GetResult();
            }

            return dbFiles.ToArray();
        }
        protected DbFile LoadDbFile(int fileId)
        {
            var sql = $@"SELECT * FROM Files WHERE FileId = {fileId}";
            using (var ctx = GetDataContext())
                return ctx.ExecuteReaderAsync(sql, async (reader, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    return await reader.ReadAsync(cancel)
                        ? GetFileFromReader(reader)
                        : null;
                }).GetAwaiter().GetResult();
        }

        private DbFile GetFileFromReader(IDataReader reader)
        {
            var file = new DbFile
            {
                FileId = reader.GetInt32("FileId"),
                BlobProvider = reader.GetSafeString("BlobProvider"),
                BlobProviderData = reader.GetSafeString("BlobProviderData"),
                ContentType = reader.GetSafeString("ContentType"),
                FileNameWithoutExtension = reader.GetSafeString("FileNameWithoutExtension"),
                Extension = reader.GetSafeString("Extension"),
                Size = reader.GetSafeInt64("Size"),
                CreationDate = reader.GetSafeDateTime("CreationDate") ?? DateTime.MinValue,
                IsDeleted = reader.GetSafeBooleanFromBoolean("IsDeleted"),
                Staging = reader.GetSafeBooleanFromBoolean("Staging"),
                StagingPropertyTypeId = reader.GetSafeInt32("StagingPropertyTypeId"),
                StagingVersionId = reader.GetSafeInt32("StagingVersionId"),
                Stream = reader.GetSafeByteArray("Stream"),
                Checksum = reader.GetSafeString("Checksum"),
                RowGuid = reader.GetGuid(reader.GetOrdinal("RowGuid")),
                Timestamp = reader.GetSafeLongFromBytes("Timestamp")
            };
            if (reader.FieldCount > 16)
                file.FileStream = reader.GetSafeByteArray("FileStream");
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

        protected string GetStringFromBinary(BinaryData binaryData)
        {
            using (var stream = binaryData.GetStream())
            using (IO.StreamReader sr = new IO.StreamReader(stream))
                return sr.ReadToEnd();
        }

        protected Type GetUsedBlobProvider(File file)
        {
            file = Node.Load<File>(file.Id);
            var bin = file.Binary;
            var ctx = BlobStorageComponents.DataProvider.GetBlobStorageContextAsync(bin.FileId, false, file.VersionId,
                PropertyType.GetByName("Binary").Id, CancellationToken.None).GetAwaiter().GetResult();
            return ctx.Provider.GetType();
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

        protected class BlobProviderSwindler : IDisposable
        {
            private readonly string _originalValue;

            public BlobProviderSwindler(Type cheat)
            {
                _originalValue = Configuration.BlobStorage.BlobProviderClassName;
                Configuration.BlobStorage.BlobProviderClassName = cheat.FullName;
                BlobStorageComponents.ProviderSelector = new BuiltInBlobProviderSelector();
            }
            public void Dispose()
            {
                Configuration.BlobStorage.BlobProviderClassName = _originalValue;
            }
        }

        #endregion
    }
}
