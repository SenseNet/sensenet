using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.BlobStorage.IntegrationTests.Implementations;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.MsSqlFsBlobProvider;
using File = SenseNet.ContentRepository.File;

namespace SenseNet.BlobStorage.IntegrationTests
{
    [TestClass]
    public class SqlFsMigrationTests : BlobStorageIntegrationTests
    {
        protected override string DatabaseName => "sn7blobtests_sqlfs";

        protected override bool SqlFsEnabled => true;
        protected override bool SqlFsUsed => true;
        protected override Type ExpectedExternalBlobProviderType => typeof(SqlFileStreamBlobProvider);
        protected override Type ExpectedMetadataProviderType => typeof(SqlFileStreamBlobMetaDataProvider);
        protected override Type ExpectedBlobProviderDataType => null;
        protected internal override void ConfigureMinimumSizeForFileStreamInBytes(int newValue, out int oldValue)
        {
            oldValue = Configuration.BlobStorage.MinimumSizeForBlobProviderInBytes;
            Configuration.BlobStorage.MinimumSizeForBlobProviderInBytes = newValue;
        }

        [ClassCleanup]
        public static void CleanupClass()
        {
            TearDown(typeof(SqlFsMigrationTests));
        }


        private static readonly string _text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit,"
                                           + " sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.";

        private readonly int _smallSizeLimit = 50;
        private readonly int _bigSizeLimit = 500;

        [TestMethod]
        public void Blob_Migration_SqlFsMeta_SqlFsToBuiltIn()
        {
            using (new SystemAccount())
            using (new SizeLimitSwindler(this, _smallSizeLimit))
            {
                var file = CreateFile(_text);
                Assert.AreEqual(typeof(SqlFileStreamBlobProvider), GetUsedBlobProvider(file));

                using (new SizeLimitSwindler(this, _bigSizeLimit))
                    MoveData(file);

                Assert.AreEqual(typeof(BuiltInBlobProvider), GetUsedBlobProvider(file));
            }
        }
        [TestMethod]
        public void Blob_Migration_SqlFsMeta_SqlFsToExternal()
        {
            using (new SystemAccount())
            using (new SizeLimitSwindler(this, _smallSizeLimit))
            {
                var file = CreateFile(_text);
                Assert.AreEqual(typeof(SqlFileStreamBlobProvider), GetUsedBlobProvider(file));

                using (new BlobProviderSwindler(typeof(LocalDiskBlobProvider)))
                    MoveData(file);

                Assert.AreEqual(typeof(LocalDiskBlobProvider), GetUsedBlobProvider(file));
            }
        }
        [TestMethod]
        public void Blob_Migration_SqlFsMeta_BuiltInToExternal()
        {
            using (new SystemAccount())
            using (new SizeLimitSwindler(this, _bigSizeLimit))
            {
                var file = CreateFile(_text);
                Assert.AreEqual(typeof(BuiltInBlobProvider), GetUsedBlobProvider(file));

                using (new BlobProviderSwindler(typeof(LocalDiskBlobProvider)))
                using (new SizeLimitSwindler(this, _smallSizeLimit))
                    MoveData(file);

                Assert.AreEqual(typeof(LocalDiskBlobProvider), GetUsedBlobProvider(file));
            }
        }

        /* ====================================================================================== */

        private File CreateFile(string fileContent)
        {
            var testRoot = CreateTestRoot();

            var file = new File(testRoot) { Name = "File1.file" };
            file.Binary.SetStream(RepositoryTools.GetStreamFromString(fileContent));
            file.Save();

            return file;
        }

        private void MoveData(File file)
        {
            var readerStream = file.Binary.GetStream();
            var buffer = new byte[readerStream.Length];
            readerStream.Read(buffer, 0, readerStream.Length.ToInt());

            var updaterStream = new MemoryStream(buffer);
            file.Binary.SetStream(updaterStream);

            file.Save();
        }

        private Type GetUsedBlobProvider(File file)
        {
            file = Node.Load<File>(file.Id);
            var bin = file.Binary;
            var ctx = BlobStorageComponents.DataProvider.GetBlobStorageContext(bin.FileId, false, file.VersionId,
                PropertyType.GetByName("Binary").Id);
            return ctx.Provider.GetType();
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

    }
}
