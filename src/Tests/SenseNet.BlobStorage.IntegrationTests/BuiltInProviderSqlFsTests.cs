using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;

namespace SenseNet.BlobStorage.IntegrationTests
{
    [TestClass]
    public class BuiltInProviderSqlFsTests : BlobStorageIntegrationTests
    {
        protected override string DatabaseName => "sn7blobtests_builtinfs";
        protected override bool SqlFsEnabled => true;
        protected override bool SqlFsUsed => false;
        protected override Type ExpectedExternalBlobProviderType => null;
        protected override Type ExpectedMetadataProviderType => typeof(MsSqlBlobMetaDataProvider);

        protected internal override void ConfigureMinimumSizeForFileStreamInBytes(int newValue, out int oldValue)
        {
            // do nothing
            oldValue = 0;
        }

        [ClassCleanup]
        public static void CleanupClass()
        {
            TearDown(typeof(BuiltInProviderSqlFsTests));
        }

        /* ==================================================== Test cases */

        [TestMethod]
        public void Blob_BuiltInFS_01_CreateFileSmall()
        {
            TestCase01_CreateFileSmall();
        }
        [TestMethod]
        public void Blob_BuiltInFS_02_CreateFileBig()
        {
            TestCase02_CreateFileBig();
        }

        [TestMethod]
        public void Blob_BuiltInFS_03_UpdateFileSmallSmall()
        {
            TestCase03_UpdateFileSmallSmall();
        }
        [TestMethod]
        public void Blob_BuiltInFS_04_UpdateFileSmallBig()
        {
            TestCase04_UpdateFileSmallBig();
        }
        [TestMethod]
        public void Blob_BuiltInFS_05_UpdateFileBigSmall()
        {
            TestCase05_UpdateFileBigSmall();
        }
        [TestMethod]
        public void Blob_BuiltInFS_06_UpdateFileBigBig()
        {
            TestCase06_UpdateFileBigBig();
        }

        [TestMethod]
        public void Blob_BuiltInFS_07_WriteChunksSmall()
        {
            TestCase07_WriteChunksSmall();
        }
        [TestMethod]
        public void Blob_BuiltInFS_08_WriteChunksBig()
        {
            TestCase08_WriteChunksBig();
        }
    }
}
