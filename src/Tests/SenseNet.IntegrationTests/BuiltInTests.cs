using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;

namespace SenseNet.BlobStorage.IntegrationTests
{
    [TestClass]
    public class BuiltInTests : BlobStorageIntegrationTests
    {
        protected override Type ExpectedExternalBlobProviderType => null;
        protected override Type ExpectedBlobProviderDataType => typeof(BuiltinBlobProviderData);
        protected internal override void ConfigureMinimumSizeForFileStreamInBytes(int newValue, out int oldValue)
        {
            // do nothing
            oldValue = 0;
        }

        /* ==================================================== Test cases */

        [TestMethod]
        public void Blob_BuiltIn_CreateFileSmall()
        {
            TestCase_CreateFileSmall();
        }
        [TestMethod]
        public void Blob_BuiltIn_CreateFileBig()
        {
            TestCase_CreateFileBig();
        }

        [TestMethod]
        public void Blob_BuiltIn_UpdateFileSmallEmpty()
        {
            TestCase_UpdateFileSmallEmpty();
        }
        [TestMethod]
        public void Blob_BuiltIn_UpdateFileBigEmpty()
        {
            TestCase_UpdateFileBigEmpty();
        }
        [TestMethod]
        public void Blob_BuiltIn_UpdateFileSmallSmall()
        {
            TestCase_UpdateFileSmallSmall();
        }
        [TestMethod]
        public void Blob_BuiltIn_UpdateFileSmallBig()
        {
            TestCase_UpdateFileSmallBig();
        }
        [TestMethod]
        public void Blob_BuiltIn_UpdateFileBigSmall()
        {
            TestCase_UpdateFileBigSmall();
        }
        [TestMethod]
        public void Blob_BuiltIn_UpdateFileBigBig()
        {
            TestCase_UpdateFileBigBig();
        }

        [TestMethod]
        public void Blob_BuiltIn_WriteChunksSmall()
        {
            TestCase_WriteChunksSmall();
        }
        [TestMethod]
        public void Blob_BuiltIn_WriteChunksBig()
        {
            TestCase_WriteChunksBig();
        }

        [TestMethod]
        public void Blob_BuiltIn_DeleteBinaryPropertySmall()
        {
            TestCase_DeleteBinaryPropertySmall();
        }
        [TestMethod]
        public void Blob_BuiltIn_DeleteBinaryPropertyBig()
        {
            TestCase_DeleteBinaryPropertyBig();
        }

        [TestMethod]
        public void Blob_BuiltIn_CopyfileRowSmall()
        {
            TestCase_CopyfileRowSmall();
        }
        [TestMethod]
        public void Blob_BuiltIn_CopyfileRowBig()
        {
            TestCase_CopyfileRowBig();
        }

        [TestMethod]
        public void Blob_BuiltIn_BinaryCacheEntitySmall()
        {
            TestCase_BinaryCacheEntitySmall();
        }
        [TestMethod]
        public void Blob_BuiltIn_BinaryCacheEntityBig()
        {
            TestCase_BinaryCacheEntityBig();
        }

        [TestMethod]
        public void Blob_BuiltIn_DeleteSmall_Maintenance()
        {
            TestCase_DeleteSmall();
        }
        [TestMethod]
        public void Blob_BuiltIn_DeleteBig_Maintenance()
        {
            TestCase_DeleteBig();
        }

        [TestMethod]
        public void Blob_BuiltIn_DeletionPolicy_Default()
        {
            TestCase_DeletionPolicy_Default();
        }
        [TestMethod]
        public void Blob_BuiltIn_DeletionPolicy_Immediately()
        {
            TestCase_DeletionPolicy_Immediately();
        }
        //[TestMethod]
        public void Blob_BuiltIn_DeletionPolicy_BackgroundImmediately()
        {
            // This test cannot be executed well because the background threading does not work.
            TestCase_DeletionPolicy_BackgroundImmediately();
        }
    }
}
