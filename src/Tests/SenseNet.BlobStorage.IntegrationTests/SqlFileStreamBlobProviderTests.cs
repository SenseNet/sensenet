using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.MsSqlFsBlobProvider;

namespace SenseNet.BlobStorage.IntegrationTests
{
    [TestClass]
    public class SqlFileStreamBlobProviderTests : BlobStorageIntegrationTests
    {
        protected override string DatabaseName => "sn7blobtests_sqlfs";

        protected override bool SqlFsEnabled => true;
        protected override bool SqlFsUsed => true;
        protected override void BuildLegoBricks(RepositoryBuilder builder)
        {
            Configuration.BlobStorage.BlobProviderClassName = typeof(SqlFileStreamBlobProvider).FullName;

            builder
                .UseBlobMetaDataProvider(new SqlFileStreamBlobMetaDataProvider())
                .UseBlobProviderSelector(new BuiltInBlobProviderSelector()); // refreshes the external provider

            BlobStorageComponents.DataProvider = new SqlFileStreamBlobMetaDataProvider();
        }
        protected internal override void ConfigureMinimumSizeForFileStreamInBytes(int newValue, out int oldValue)
        {
            oldValue = Configuration.BlobStorage.MinimumSizeForBlobProviderInBytes;
            Configuration.BlobStorage.MinimumSizeForBlobProviderInBytes = newValue;
        }

        [TestMethod]
        public void Blob_SqlFS_01_CreateFileSmall()
        {
            TestCase01_CreateFileSmall();
        }
        [TestMethod]
        public void Blob_SqlFS_02_CreateFileBig()
        {
            TestCase02_CreateFileBig();
        }

        [TestMethod]
        public void Blob_SqlFS_03_UpdateFileSmallSmall()
        {
            TestCase03_UpdateFileSmallSmall();
        }
        [TestMethod]
        public void Blob_SqlFS_04_UpdateFileSmallBig()
        {
            TestCase04_UpdateFileSmallBig();
        }
        [TestMethod]
        public void Blob_SqlFS_05_UpdateFileBigSmall()
        {
            TestCase05_UpdateFileBigSmall();
        }
        [TestMethod]
        public void Blob_SqlFS_06_UpdateFileBigBig()
        {
            TestCase06_UpdateFileBigBig();
        }

    }
}
