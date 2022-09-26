﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.MsSql.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.MsSql.MsSqlTests
{
    [TestClass]
    public class MsSqlBlobLocalDiskTests : BlobStorageIntegrationTest<MsSqlLocalDiskBlobStoragePlatform, BlobProviderTestCases>
    {
        // Name convention: IntT_MsSql_Blob_LocalDisk_
        //   IntT: integration test
        //   MsSql: main database platform
        //   Blob: test category
        //   LocalDisk: blob provider.

        [TestMethod]
        public void IntT_MsSql_Blob_LocalDisk_CreateFileSmall()
        {
            TestCase.TestCase_CreateFileSmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDisk_CreateFileBig()
        {
            TestCase.TestCase_CreateFileBig();
        }

        [TestMethod]
        public void IntT_MsSql_Blob_LocalDisk_UpdateFileSmallEmpty()
        {
            TestCase.TestCase_UpdateFileSmallEmpty();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDisk_UpdateFileBigEmpty()
        {
            TestCase.TestCase_UpdateFileBigEmpty();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDisk_UpdateFileSmallSmall()
        {
            TestCase.TestCase_UpdateFileSmallSmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDisk_UpdateFileSmallBig()
        {
            TestCase.TestCase_UpdateFileSmallBig();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDisk_UpdateFileBigSmall()
        {
            TestCase.TestCase_UpdateFileBigSmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDisk_UpdateFileBigBig()
        {
            TestCase.TestCase_UpdateFileBigBig();
        }

        [TestMethod]
        public void IntT_MsSql_Blob_LocalDisk_WriteChunksSmall()
        {
            TestCase.TestCase_WriteChunksSmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDisk_WriteChunksBig()
        {
            TestCase.TestCase_WriteChunksBig();
        }

        [TestMethod]
        public void IntT_MsSql_Blob_LocalDisk_DeleteBinaryPropertySmall()
        {
            TestCase.TestCase_DeleteBinaryPropertySmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDisk_DeleteBinaryPropertyBig()
        {
            TestCase.TestCase_DeleteBinaryPropertyBig();
        }

        [TestMethod]
        public void IntT_MsSql_Blob_LocalDisk_CopyFileRowSmall()
        {
            TestCase.TestCase_CopyFileRowSmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDisk_CopyFileRowBig()
        {
            TestCase.TestCase_CopyFileRowBig();
        }

        [TestMethod]
        public void IntT_MsSql_Blob_LocalDisk_BinaryCacheEntitySmall()
        {
            TestCase.TestCase_BinaryCacheEntitySmall();
        }
        [TestMethod, TestCategory("Services")]
        public void IntT_MsSql_Blob_LocalDisk_BinaryCacheEntityBig_CSrv()
        {
            TestCase.TestCase_BinaryCacheEntityBig();
        }

        [TestMethod]
        public void IntT_MsSql_Blob_LocalDisk_DeleteSmall()
        {
            TestCase.TestCase_DeleteSmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDisk_DeleteBig()
        {
            TestCase.TestCase_DeleteBig();
        }

        [TestMethod]
        public void IntT_MsSql_Blob_LocalDisk_DeletionPolicy_Default()
        {
            TestCase.TestCase_DeletionPolicy_Default();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDisk_DeletionPolicy_Immediately()
        {
            TestCase.TestCase_DeletionPolicy_Immediately();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_LocalDisk_DeletionPolicy_BackgroundImmediately()
        {
            TestCase.TestCase_DeletionPolicy_BackgroundImmediately();
        }
    }
}
