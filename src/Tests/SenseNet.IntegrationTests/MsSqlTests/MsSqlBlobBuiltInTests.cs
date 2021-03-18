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
    public class MsSqlBlobBuiltInTests : BlobStorageIntegrationTest<MsSqlBuiltInBlobStoragePlatform, BlobProviderTestCases>
    {
        // Name convention: IntT_MsSql_Blob_BuiltIn_
        //   IntT: integration test
        //   MsSql: main database platform
        //   Blob: test category
        //   BuiltIn: blobs in the Files table.

        [TestMethod]
        public void IntT_MsSql_Blob_BuiltIn_CreateFileSmall()
        {
            TestCase.TestCase_CreateFileSmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_BuiltIn_CreateFileBig()
        {
            TestCase.TestCase_CreateFileBig();
        }

        [TestMethod]
        public void IntT_MsSql_Blob_BuiltIn_UpdateFileSmallEmpty()
        {
            TestCase.TestCase_UpdateFileSmallEmpty();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_BuiltIn_UpdateFileBigEmpty()
        {
            TestCase.TestCase_UpdateFileBigEmpty();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_BuiltIn_UpdateFileSmallSmall()
        {
            TestCase.TestCase_UpdateFileSmallSmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_BuiltIn_UpdateFileSmallBig()
        {
            TestCase.TestCase_UpdateFileSmallBig();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_BuiltIn_UpdateFileBigSmall()
        {
            TestCase.TestCase_UpdateFileBigSmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_BuiltIn_UpdateFileBigBig()
        {
            TestCase.TestCase_UpdateFileBigBig();
        }

        [TestMethod]
        public void IntT_MsSql_Blob_WriteChunksSmall()
        {
            TestCase.TestCase_WriteChunksSmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_WriteChunksBig()
        {
            //UNDONE:<?Blob: Check database after this test
            TestCase.TestCase_WriteChunksBig();
        }

        [TestMethod]
        public void IntT_MsSql_Blob_DeleteBinaryPropertySmall()
        {
            TestCase.TestCase_DeleteBinaryPropertySmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_DeleteBinaryPropertyBig()
        {
            TestCase.TestCase_DeleteBinaryPropertyBig();
        }

        [TestMethod]
        public void IntT_MsSql_Blob_CopyFileRowSmall()
        {
            TestCase.TestCase_CopyFileRowSmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_CopyFileRowBig()
        {
            TestCase.TestCase_CopyFileRowBig();
        }

        [TestMethod]
        public void IntT_MsSql_Blob_BinaryCacheEntitySmall()
        {
            TestCase.TestCase_BinaryCacheEntitySmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_BinaryCacheEntityBig()
        {
            TestCase.TestCase_BinaryCacheEntityBig();
        }

        [TestMethod]
        public void IntT_MsSql_Blob_DeleteSmall()
        {
            TestCase.TestCase_DeleteSmall();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_DeleteBig()
        {
            TestCase.TestCase_DeleteBig();
        }

        [TestMethod]
        public void IntT_MsSql_Blob_DeletionPolicy_Default()
        {
            TestCase.TestCase_DeletionPolicy_Default();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_DeletionPolicy_Immediately()
        {
            TestCase.TestCase_DeletionPolicy_Immediately();
        }
        [TestMethod]
        public void IntT_MsSql_Blob_DeletionPolicy_BackgroundImmediately()
        {
            TestCase.TestCase_DeletionPolicy_BackgroundImmediately();
        }
    }
}
