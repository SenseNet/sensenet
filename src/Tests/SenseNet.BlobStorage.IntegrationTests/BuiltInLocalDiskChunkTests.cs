using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.BlobStorage.IntegrationTests.Implementations;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.MsSqlFsBlobProvider;

namespace SenseNet.BlobStorage.IntegrationTests
{
    [TestClass]
    public class BuiltInLocalDiskChunkTests : BlobStorageIntegrationTests
    {
        protected override string DatabaseName => "sn7blobtests_builtin";
        protected override bool SqlFsEnabled => true;
        protected override bool SqlFsUsed => false;
        protected override Type ExpectedExternalBlobProviderType => typeof(LocalDiskChunkBlobProvider);
        protected override Type ExpectedMetadataProviderType => typeof(MsSqlBlobMetaDataProvider);
        protected override Type ExpectedBlobProviderDataType => typeof(LocalDiskChunkBlobProvider.LocalDiskChunkBlobProviderData);
        protected internal override void ConfigureMinimumSizeForFileStreamInBytes(int newValue, out int oldValue)
        {
            oldValue = Configuration.BlobStorage.MinimumSizeForBlobProviderInBytes;
            Configuration.BlobStorage.MinimumSizeForBlobProviderInBytes = newValue;
        }

        [ClassCleanup]
        public static void CleanupClass()
        {
            TearDown(typeof(BuiltInLocalDiskChunkTests));
        }


        [TestMethod]
        public void Blob_BuiltInLocalDiskChunk_01_CreateFileSmall()
        {
            TestCase01_CreateFileSmall();
        }
        [TestMethod]
        public void Blob_BuiltInLocalDiskChunk_02_CreateFileBig()
        {
            TestCase02_CreateFileBig();
        }

        [TestMethod]
        public void Blob_BuiltInLocalDiskChunk_03_UpdateFileSmallSmall()
        {
            TestCase03_UpdateFileSmallSmall();
        }
        [TestMethod]
        public void Blob_BuiltInLocalDiskChunk_04_UpdateFileSmallBig()
        {
            TestCase04_UpdateFileSmallBig();
        }
        [TestMethod]
        public void Blob_BuiltInLocalDiskChunk_05_UpdateFileBigSmall()
        {
            TestCase05_UpdateFileBigSmall();
        }
        [TestMethod]
        public void Blob_BuiltInLocalDiskChunk_06_UpdateFileBigBig()
        {
            TestCase06_UpdateFileBigBig();
        }

        [TestMethod]
        public void Blob_BuiltInLocalDiskChunk_07_WriteChunksSmall()
        {
            TestCase07_WriteChunksSmall();
        }
        [TestMethod]
        public void Blob_BuiltInLocalDiskChunk_08_WriteChunksBig()
        {
            TestCase08_WriteChunksBig();
        }

        [TestMethod]
        public void Blob_BuiltInLocalDiskChunk_09_DeleteBinaryPropertySmall()
        {
            TestCase09_DeleteBinaryPropertySmall();
        }
        [TestMethod]
        public void Blob_BuiltInLocalDiskChunk_10_DeleteBinaryPropertyBig()
        {
            TestCase10_DeleteBinaryPropertyBig();
        }

        [TestMethod]
        public void Blob_BuiltInLocalDiskChunk_11_CopyfileRowSmall()
        {
            TestCase11_CopyfileRowSmall();
        }
        [TestMethod]
        public void Blob_BuiltInLocalDiskChunk_12_CopyfileRowBig()
        {
            TestCase12_CopyfileRowBig();
        }

        [TestMethod]
        public void Blob_BuiltInLocalDiskChunk_13_BinaryCacheEntitySmall()
        {
            TestCase13_BinaryCacheEntitySmall();
        }
        [TestMethod]
        public void Blob_BuiltInLocalDiskChunk_14_BinaryCacheEntityBig()
        {
            TestCase14_BinaryCacheEntityBig();
        }

        [TestMethod]
        public void Blob_BuiltInLocalDiskChunk_15_DeleteSmall()
        {
            TestCase15_DeleteSmall();
        }
        [TestMethod]
        public void Blob_BuiltInLocalDiskChunk_16_DeleteBig()
        {
            TestCase16_DeleteBig();
        }
    }
}
