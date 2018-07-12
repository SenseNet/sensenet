using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
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
        protected override Type ExpectedExternalBlobProviderType => typeof(SqlFileStreamBlobProvider);
        protected override Type ExpectedMetadataProviderType => typeof(SqlFileStreamBlobMetaDataProvider);

        protected internal override void ConfigureMinimumSizeForFileStreamInBytes(int newValue, out int oldValue)
        {
            oldValue = Configuration.BlobStorage.MinimumSizeForBlobProviderInBytes;
            Configuration.BlobStorage.MinimumSizeForBlobProviderInBytes = newValue;
        }

        [ClassCleanup]
        public static void CleanupClass()
        {
            TearDown(typeof(SqlFileStreamBlobProviderTests));
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

        [TestMethod]
        public void Blob_SqlFs_07_WriteChunksSmall()
        {
            TestCase07_WriteChunksSmall();
        }
        [TestMethod]
        public void Blob_SqlFs_08_WriteChunksBig()
        {
            TestCase08_WriteChunksBig();
        }

        [TestMethod]
        public void Blob_SqlFs_09_DeleteBinaryPropertySmall()
        {
            TestCase09_DeleteBinaryPropertySmall();
        }
        [TestMethod]
        public void Blob_SqlFs_10_DeleteBinaryPropertyBig()
        {
            TestCase10_DeleteBinaryPropertyBig();
        }

        [TestMethod]
        public void Blob_SqlFs_11_CopyfileRowSmall()
        {
            TestCase11_CopyfileRowSmall();
        }
        [TestMethod]
        public void Blob_SqlFs_12_CopyfileRowBig()
        {
            TestCase12_CopyfileRowBig();
        }

        [TestMethod]
        public void Blob_SqlFs_13_BinaryCacheEntitySmall()
        {
            TestCase13_BinaryCacheEntitySmall();
        }
        [TestMethod]
        public void Blob_SqlFs_14_BinaryCacheEntityBig()
        {
            TestCase14_BinaryCacheEntityBig();
        }

    }
}
