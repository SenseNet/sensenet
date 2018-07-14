using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.BlobStorage.IntegrationTests.Implementations;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.MsSqlFsBlobProvider;

namespace SenseNet.BlobStorage.IntegrationTests
{
    [TestClass]
    public class SqlFsLocalDiskTests : BlobStorageIntegrationTests
    {
        protected override string DatabaseName => "sn7blobtests_builtinfs";
        protected override bool SqlFsEnabled => true;
        protected override bool SqlFsUsed => false;
        protected override Type ExpectedExternalBlobProviderType => typeof(LocalDiskBlobProvider);
        protected override Type ExpectedMetadataProviderType => typeof(SqlFileStreamBlobMetaDataProvider);
        protected override Type ExpectedBlobProviderDataType => typeof(LocalDiskBlobProvider.LocalDiskBlobProviderData);
        protected internal override void ConfigureMinimumSizeForFileStreamInBytes(int newValue, out int oldValue)
        {
            oldValue = Configuration.BlobStorage.MinimumSizeForBlobProviderInBytes;
            Configuration.BlobStorage.MinimumSizeForBlobProviderInBytes = newValue;
        }

        [ClassCleanup]
        public static void CleanupClass()
        {
            TearDown(typeof(SqlFsLocalDiskTests));
        }


        [TestMethod]
        public void Blob_SqlFsLocalDisk_01_CreateFileSmall()
        {
            TestCase01_CreateFileSmall();
        }
        [TestMethod]
        public void Blob_SqlFsLocalDisk_02_CreateFileBig()
        {
            TestCase02_CreateFileBig();
        }

        [TestMethod]
        public void Blob_SqlFsLocalDisk_03_UpdateFileSmallSmall()
        {
            TestCase03_UpdateFileSmallSmall();
        }
        [TestMethod]
        public void Blob_SqlFsLocalDisk_04_UpdateFileSmallBig()
        {
            TestCase04_UpdateFileSmallBig();
        }
        [TestMethod]
        public void Blob_SqlFsLocalDisk_05_UpdateFileBigSmall()
        {
            TestCase05_UpdateFileBigSmall();
        }
        [TestMethod]
        public void Blob_SqlFsLocalDisk_06_UpdateFileBigBig()
        {
            TestCase06_UpdateFileBigBig();
        }

        [TestMethod]
        public void Blob_SqlFsLocalDisk_07_WriteChunksSmall()
        {
            TestCase07_WriteChunksSmall();
        }
        [TestMethod]
        public void Blob_SqlFsLocalDisk_08_WriteChunksBig()
        {
            TestCase08_WriteChunksBig();
        }

        [TestMethod]
        public void Blob_SqlFsLocalDisk_09_DeleteBinaryPropertySmall()
        {
            TestCase09_DeleteBinaryPropertySmall();
        }
        [TestMethod]
        public void Blob_SqlFsLocalDisk_10_DeleteBinaryPropertyBig()
        {
            TestCase10_DeleteBinaryPropertyBig();
        }

        [TestMethod]
        public void Blob_SqlFsLocalDisk_11_CopyfileRowSmall()
        {
            TestCase11_CopyfileRowSmall();
        }
        [TestMethod]
        public void Blob_SqlFsLocalDisk_12_CopyfileRowBig()
        {
            TestCase12_CopyfileRowBig();
        }

        [TestMethod]
        public void Blob_SqlFsLocalDisk_13_BinaryCacheEntitySmall()
        {
            TestCase13_BinaryCacheEntitySmall();
        }
        [TestMethod]
        public void Blob_SqlFsLocalDisk_14_BinaryCacheEntityBig()
        {
            TestCase14_BinaryCacheEntityBig();
        }

        [TestMethod]
        public void Blob_SqlFsLocalDisk_15_DeleteSmall()
        {
            TestCase15_DeleteSmall();
        }
        [TestMethod]
        public void Blob_SqlFsLocalDisk_16_DeleteBig()
        {
            TestCase16_DeleteBig();
        }
    }
}
