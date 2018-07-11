using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;

namespace SenseNet.BlobStorage.IntegrationTests
{
    [TestClass]
    public class BuiltInProviderTests : BlobStorageIntegrationTests
    {
        protected override string DatabaseName => "sn7blobtests_builtin";
        protected override bool SqlFsEnabled => false;
        protected override bool SqlFsUsed => false;
        protected override Type ExpectedExternalBlobProviderType => null;
        protected override Type ExpectedMetadataProviderType => typeof(MsSqlBlobMetaDataProvider);

        protected override void BuildLegoBricks(RepositoryBuilder builder)
        {
            Configuration.BlobStorage.BlobProviderClassName = ExpectedExternalBlobProviderType?.FullName;
            BuiltInBlobProviderSelector.ExternalBlobProvider = null; // reset external provider

            var blobMetaDataProvider = (IBlobStorageMetaDataProvider)Activator.CreateInstance(ExpectedMetadataProviderType);

            builder
                .UseBlobMetaDataProvider(blobMetaDataProvider)
                .UseBlobProviderSelector(new BuiltInBlobProviderSelector());

            BlobStorageComponents.DataProvider = blobMetaDataProvider;
        }
        protected internal override void ConfigureMinimumSizeForFileStreamInBytes(int newValue, out int oldValue)
        {
            // do nothing
            oldValue = 0;
        }

        [ClassCleanup]
        public static void CleanupClass()
        {
            TearDown(typeof(BuiltInProviderTests));
        }

        /* ==================================================== Test cases */

        [TestMethod]
        public void Blob_BuiltIn_01_CreateFileSmall()
        {
            TestCase01_CreateFileSmall();
        }
        [TestMethod]
        public void Blob_BuiltIn_02_CreateFileBig()
        {
            TestCase02_CreateFileBig();
        }

        [TestMethod]
        public void Blob_BuiltIn_03_UpdateFileSmallSmall()
        {
            TestCase03_UpdateFileSmallSmall();
        }
        [TestMethod]
        public void Blob_BuiltIn_04_UpdateFileSmallBig()
        {
            TestCase04_UpdateFileSmallBig();
        }
        [TestMethod]
        public void Blob_BuiltIn_05_UpdateFileBigSmall()
        {
            TestCase05_UpdateFileBigSmall();
        }
        [TestMethod]
        public void Blob_BuiltIn_06_UpdateFileBigBig()
        {
            TestCase06_UpdateFileBigBig();
        }

        [TestMethod]
        public void Blob_BuiltIn_07_WriteChunksSmall()
        {
            TestCase07_WriteChunksSmall();
        }
        [TestMethod]
        public void Blob_BuiltIn_08_WriteChunksBig()
        {
            TestCase08_WriteChunksBig();
        }
    }
}
