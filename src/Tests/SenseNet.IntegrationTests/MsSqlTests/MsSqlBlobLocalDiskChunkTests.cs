using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.MsSqlTests
{
    [TestClass]
    public class MsSqlBlobLocalDiskChunkTests : BlobStorageIntegrationTest<MsSqlLocalDiskChunkBlobStoragePlatform, BlobProviderTestCases>
    {
        // Name convention: IntT_MsSql_Blob_LocalDiskChunk_
        //   IntT: integration test
        //   MsSql: main database platform
        //   Blob: test category
        //   LocalDiskChunk: blob provider.

        [TestMethod]
        public void IntT_MsSql_Blob_LocalDiskChunk_CreateFileSmall()
        {
            TestCase.TestCase_CreateFileSmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDiskChunk_CreateFileBig()
        {
            TestCase.TestCase_CreateFileBig();
        }

        [TestMethod]
        public void IntT_MsSql_Blob_LocalDiskChunk_UpdateFileSmallEmpty()
        {
            TestCase.TestCase_UpdateFileSmallEmpty();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDiskChunk_UpdateFileBigEmpty()
        {
            TestCase.TestCase_UpdateFileBigEmpty();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDiskChunk_UpdateFileSmallSmall()
        {
            TestCase.TestCase_UpdateFileSmallSmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDiskChunk_UpdateFileSmallBig()
        {
            TestCase.TestCase_UpdateFileSmallBig();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDiskChunk_UpdateFileBigSmall()
        {
            TestCase.TestCase_UpdateFileBigSmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDiskChunk_UpdateFileBigBig()
        {
            TestCase.TestCase_UpdateFileBigBig();
        }

        [TestMethod]
        public void IntT_MsSql_Blob_LocalDiskChunk_WriteChunksSmall()
        {
            TestCase.TestCase_WriteChunksSmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDiskChunk_WriteChunksBig()
        {
            //UNDONE:<?Blob: Check database after this test
            TestCase.TestCase_WriteChunksBig();
        }

        [TestMethod]
        public void IntT_MsSql_Blob_LocalDiskChunk_DeleteBinaryPropertySmall()
        {
            TestCase.TestCase_DeleteBinaryPropertySmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDiskChunk_DeleteBinaryPropertyBig()
        {
            TestCase.TestCase_DeleteBinaryPropertyBig();
        }

        [TestMethod]
        public void IntT_MsSql_Blob_LocalDiskChunk_CopyFileRowSmall()
        {
            TestCase.TestCase_CopyFileRowSmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDiskChunk_CopyFileRowBig()
        {
            TestCase.TestCase_CopyFileRowBig();
        }

        [TestMethod]
        public void IntT_MsSql_Blob_LocalDiskChunk_BinaryCacheEntitySmall()
        {
            TestCase.TestCase_BinaryCacheEntitySmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDiskChunk_BinaryCacheEntityBig()
        {
            TestCase.TestCase_BinaryCacheEntityBig();
        }

        [TestMethod]
        public void IntT_MsSql_Blob_LocalDiskChunk_DeleteSmall()
        {
            TestCase.TestCase_DeleteSmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDiskChunk_DeleteBig()
        {
            TestCase.TestCase_DeleteBig();
        }

        [TestMethod]
        public void IntT_MsSql_Blob_LocalDiskChunk_DeletionPolicy_Default()
        {
            TestCase.TestCase_DeletionPolicy_Default();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDiskChunk_DeletionPolicy_Immediately()
        {
            TestCase.TestCase_DeletionPolicy_Immediately();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDiskChunk_DeletionPolicy_BackgroundImmediately()
        {
            TestCase.TestCase_DeletionPolicy_BackgroundImmediately();
        }
    }
}
