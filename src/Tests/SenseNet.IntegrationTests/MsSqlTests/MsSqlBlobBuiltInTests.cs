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
            TestCase.TestCase_WriteChunksBig();
        }

    }
}
