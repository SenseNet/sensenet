using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemBlobChunkTests : BlobStorageIntegrationTest<InMemChunkBlobStoragePlatform, BlobProviderTestCases>
    {
        // Name convention: IntT_InMem_Blob_Chunk_
        //   IntT: integration test
        //   InMem: main database platform
        //   Blob: test category
        //   Chunk: test subcategory
        // Note that the blobs are not stored in the Files table.

        [TestMethod]
        public void IntT_InMem_Blob_Chunk_CreateFileSmall()
        {
            TestCase.TestCase_CreateFileSmall();
        }
        [TestMethod]
        public void IntT_InMem_Blob_Chunk_CreateFileBig()
        {
            TestCase.TestCase_CreateFileBig();
        }

        [TestMethod]
        public void IntT_InMem_Blob_Chunk_UpdateFileSmallEmpty()
        {
            TestCase.TestCase_UpdateFileSmallEmpty();
        }
        [TestMethod]
        public void IntT_InMem_Blob_Chunk_UpdateFileBigEmpty()
        {
            TestCase.TestCase_UpdateFileBigEmpty();
        }
        [TestMethod]
        public void IntT_InMem_Blob_Chunk_UpdateFileSmallSmall()
        {
            TestCase.TestCase_UpdateFileSmallSmall();
        }
        [TestMethod]
        public void IntT_InMem_Blob_Chunk_UpdateFileSmallBig()
        {
            TestCase.TestCase_UpdateFileSmallBig();
        }
        [TestMethod]
        public void IntT_InMem_Blob_Chunk_UpdateFileBigSmall()
        {
            TestCase.TestCase_UpdateFileBigSmall();
        }
        [TestMethod]
        public void IntT_InMem_Blob_Chunk_UpdateFileBigBig()
        {
            TestCase.TestCase_UpdateFileBigBig();
        }

        [TestMethod]
        public void IntT_InMem_Blob_Chunk_WriteChunksSmall()
        {
            TestCase.TestCase_WriteChunksSmall();
        }
        [TestMethod]
        public void IntT_InMem_Blob_Chunk_WriteChunksBig()
        {
            TestCase.TestCase_WriteChunksBig();
        }

        [TestMethod]
        public void IntT_InMem_Blob_Chunk_DeleteBinaryPropertySmall()
        {
            TestCase.TestCase_DeleteBinaryPropertySmall();
        }
        [TestMethod]
        public void IntT_InMem_Blob_Chunk_DeleteBinaryPropertyBig()
        {
            TestCase.TestCase_DeleteBinaryPropertyBig();
        }

        [TestMethod]
        public void IntT_InMem_Blob_Chunk_CopyFileRowSmall()
        {
            TestCase.TestCase_CopyFileRowSmall();
        }
        [TestMethod]
        public void IntT_InMem_Blob_Chunk_CopyFileRowBig()
        {
            TestCase.TestCase_CopyFileRowBig();
        }

        [TestMethod]
        public void IntT_InMem_Blob_Chunk_BinaryCacheEntitySmall()
        {
            TestCase.TestCase_BinaryCacheEntitySmall();
        }
        [TestMethod]
        public void IntT_InMem_Blob_Chunk_BinaryCacheEntityBig()
        {
            TestCase.TestCase_BinaryCacheEntityBig();
        }

        [TestMethod]
        public void IntT_InMem_Blob_Chunk_DeleteSmall()
        {
            TestCase.TestCase_DeleteSmall();
        }
        [TestMethod]
        public void IntT_InMem_Blob_Chunk_DeleteBig()
        {
            TestCase.TestCase_DeleteBig();
        }

        [TestMethod]
        public void IntT_InMem_Blob_Chunk_DeletionPolicy_Default()
        {
            TestCase.TestCase_DeletionPolicy_Default();
        }
        [TestMethod]
        public void IntT_InMem_Blob_Chunk_DeletionPolicy_Immediately()
        {
            TestCase.TestCase_DeletionPolicy_Immediately();
        }
        [TestMethod]
        public void IntT_InMem_Blob_Chunk_DeletionPolicy_BackgroundImmediately()
        {
            TestCase.TestCase_DeletionPolicy_BackgroundImmediately();
        }
    }
}
