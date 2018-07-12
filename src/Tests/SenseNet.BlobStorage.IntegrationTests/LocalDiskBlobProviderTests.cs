using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.BlobStorage.IntegrationTests.Implementations;
using SenseNet.MsSqlFsBlobProvider;

namespace SenseNet.BlobStorage.IntegrationTests
{
    [TestClass]
    public class LocalDiskBlobProviderTests : BlobStorageIntegrationTests
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
            TearDown(typeof(LocalDiskBlobProviderTests));
        }


        [TestMethod]
        public void Blob_LocalDisk_01_CreateFileSmall()
        {
            TestCase01_CreateFileSmall();
        }
        [TestMethod]
        public void Blob_LocalDisk_02_CreateFileBig()
        {
            TestCase02_CreateFileBig();
        }

        [TestMethod]
        public void Blob_LocalDisk_03_UpdateFileSmallSmall()
        {
            TestCase03_UpdateFileSmallSmall();
        }
        [TestMethod]
        public void Blob_LocalDisk_04_UpdateFileSmallBig()
        {
            TestCase04_UpdateFileSmallBig();
        }
        [TestMethod]
        public void Blob_LocalDisk_05_UpdateFileBigSmall()
        {
            TestCase05_UpdateFileBigSmall();
        }
        [TestMethod]
        public void Blob_LocalDisk_06_UpdateFileBigBig()
        {
            TestCase06_UpdateFileBigBig();
        }

        [TestMethod]
        public void Blob_LocalDisk_07_WriteChunksSmall()
        {
            TestCase07_WriteChunksSmall();
        }
        [TestMethod]
        public void Blob_LocalDisk_08_WriteChunksBig()
        {
            TestCase08_WriteChunksBig();
        }

        [TestMethod]
        public void Blob_LocalDisk_09_DeleteBinaryPropertySmall()
        {
            TestCase09_DeleteBinaryPropertySmall();
        }
        [TestMethod]
        public void Blob_LocalDisk_10_DeleteBinaryPropertyBig()
        {
            TestCase10_DeleteBinaryPropertyBig();
        }

        [TestMethod]
        public void Blob_LocalDisk_11_CopyfileRowSmall()
        {
            TestCase11_CopyfileRowSmall();
        }
        [TestMethod]
        public void Blob_LocalDisk_12_CopyfileRowBig()
        {
            TestCase12_CopyfileRowBig();
        }

        [TestMethod]
        public void Blob_LocalDisk_13_BinaryCacheEntitySmall()
        {
            TestCase13_BinaryCacheEntitySmall();
        }
        [TestMethod]
        public void Blob_LocalDisk_14_BinaryCacheEntityBig()
        {
            TestCase14_BinaryCacheEntityBig();
        }

    }
}
