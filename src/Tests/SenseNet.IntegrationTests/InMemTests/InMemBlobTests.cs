using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemBlobTests : BlobStorageIntegrationTest<InMemBlobStoragePlatform, BlobProviderTestCases>
    {
        // Name convention: IntT_InMem_Blob_
        //   IntT: integration test
        //   InMem: main database platform
        //   Blob: test category
        // Note that the blobs are not stored in the Files table.

        [TestMethod]
        public void IntT_InMem_Blob_CreateFileSmall()
        {
            TestCase.TestCase_CreateFileSmall();
        }
        [TestMethod]
        public void IntT_InMem_Blob_CreateFileBig()
        {
            TestCase.TestCase_CreateFileBig();
        }

        [TestMethod]
        public void IntT_InMem_Blob_UpdateFileSmallEmpty()
        {
            TestCase.TestCase_UpdateFileSmallEmpty();
        }
        [TestMethod]
        public void IntT_InMem_Blob_UpdateFileBigEmpty()
        {
            TestCase.TestCase_UpdateFileBigEmpty();
        }
        [TestMethod]
        public void IntT_InMem_Blob_UpdateFileSmallSmall()
        {
            TestCase.TestCase_UpdateFileSmallSmall();
        }
        [TestMethod]
        public void IntT_InMem_Blob_UpdateFileSmallBig()
        {
            TestCase.TestCase_UpdateFileSmallBig();
        }
        [TestMethod]
        public void IntT_InMem_Blob_UpdateFileBigSmall()
        {
            TestCase.TestCase_UpdateFileBigSmall();
        }
        [TestMethod]
        public void IntT_InMem_Blob_UpdateFileBigBig()
        {
            TestCase.TestCase_UpdateFileBigBig();
        }

        [TestMethod]
        public void IntT_InMem_Blob_WriteChunksSmall()
        {
            TestCase.TestCase_WriteChunksSmall();
        }
        [TestMethod]
        public void IntT_InMem_Blob_WriteChunksBig()
        {
            TestCase.TestCase_WriteChunksBig();
        }


    }
}
