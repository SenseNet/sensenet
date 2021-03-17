//using System;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using SenseNet.IntegrationTests.Common;

//namespace SenseNet.BlobStorage.IntegrationTests
//{
//    [TestClass]
//    public class BuiltInLocalDiskChunkTests : BlobStorageIntegrationTests
//    {
//        protected override Type ExpectedExternalBlobProviderType => typeof(LocalDiskChunkBlobProvider);
//        protected override Type ExpectedBlobProviderDataType => typeof(LocalDiskChunkBlobProvider.LocalDiskChunkBlobProviderData);
//        protected internal override void ConfigureMinimumSizeForFileStreamInBytes(int newValue, out int oldValue)
//        {
//            oldValue = Configuration.BlobStorage.MinimumSizeForBlobProviderInBytes;
//            Configuration.BlobStorage.MinimumSizeForBlobProviderInBytes = newValue;
//        }

//        /* ==================================================== Test cases */

//        //[TestMethod]
//        public void Blob_BuiltInLocalDiskChunk_CreateFileSmall()
//        {
//            TestCase_CreateFileSmall();
//        }
//        //[TestMethod]
//        public void Blob_BuiltInLocalDiskChunk_CreateFileBig()
//        {
//            TestCase_CreateFileBig();
//        }

//        //[TestMethod]
//        public void Blob_BuiltInLocalDiskChunk_UpdateFileSmallEmpty()
//        {
//            TestCase_UpdateFileSmallEmpty();
//        }
//        //[TestMethod]
//        public void Blob_BuiltInLocalDiskChunk_UpdateFileBigEmpty()
//        {
//            TestCase_UpdateFileBigEmpty();
//        }
//        //[TestMethod]
//        public void Blob_BuiltInLocalDiskChunk_UpdateFileSmallSmall()
//        {
//            TestCase_UpdateFileSmallSmall();
//        }
//        //[TestMethod]
//        public void Blob_BuiltInLocalDiskChunk_UpdateFileSmallBig()
//        {
//            TestCase_UpdateFileSmallBig();
//        }
//        //[TestMethod]
//        public void Blob_BuiltInLocalDiskChunk_UpdateFileBigSmall()
//        {
//            TestCase_UpdateFileBigSmall();
//        }
//        //[TestMethod]
//        public void Blob_BuiltInLocalDiskChunk_UpdateFileBigBig()
//        {
//            TestCase_UpdateFileBigBig();
//        }

//        //[TestMethod]
//        public void Blob_BuiltInLocalDiskChunk_WriteChunksSmall()
//        {
//            TestCase_WriteChunksSmall();
//        }
//        //[TestMethod]
//        public void Blob_BuiltInLocalDiskChunk_WriteChunksBig()
//        {
//            TestCase_WriteChunksBig();
//        }

//        //[TestMethod]
//        public void Blob_BuiltInLocalDiskChunk_DeleteBinaryPropertySmall()
//        {
//            TestCase_DeleteBinaryPropertySmall();
//        }
//        //[TestMethod]
//        public void Blob_BuiltInLocalDiskChunk_DeleteBinaryPropertyBig()
//        {
//            TestCase_DeleteBinaryPropertyBig();
//        }

//        //[TestMethod]
//        public void Blob_BuiltInLocalDiskChunk_CopyfileRowSmall()
//        {
//            TestCase_CopyfileRowSmall();
//        }
//        //[TestMethod]
//        public void Blob_BuiltInLocalDiskChunk_CopyfileRowBig()
//        {
//            TestCase_CopyfileRowBig();
//        }

//        //[TestMethod]
//        public void Blob_BuiltInLocalDiskChunk_BinaryCacheEntitySmall()
//        {
//            TestCase_BinaryCacheEntitySmall();
//        }
//        //[TestMethod]
//        public void Blob_BuiltInLocalDiskChunk_BinaryCacheEntityBig()
//        {
//            TestCase_BinaryCacheEntityBig();
//        }

//        //[TestMethod]
//        public void Blob_BuiltInLocalDiskChunk_DeleteSmall_Maintenance()
//        {
//            TestCase_DeleteSmall();
//        }
//        //[TestMethod]
//        public void Blob_BuiltInLocalDiskChunk_DeleteBig_Maintenance()
//        {
//            TestCase_DeleteBig();
//        }
//    }
//}
