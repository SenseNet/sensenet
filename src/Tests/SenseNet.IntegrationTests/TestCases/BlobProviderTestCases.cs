using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.BlobStorage.IntegrationTests;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.IntegrationTests.Common;
using SenseNet.IntegrationTests.Infrastructure;
using IO = System.IO;

namespace SenseNet.IntegrationTests.TestCases
{
    public class BlobProviderTestCases : BlobStorageTestCaseBase
    {
        public void TestCase_CreateFileSmall()
        {
            IntegrationTest((sandbox) =>
            {
                var expectedText = "Lorem ipsum dolo sit amet";
                var dbFile = CreateFileTest(sandbox, expectedText, expectedText.Length + 10);

                Assert_Small(dbFile, expectedText);
            });
        }
        public void TestCase_CreateFileBig()
        {
            IntegrationTest((sandbox) =>
            {
                var expectedText = "Lorem ipsum dolo sit amet";
                var dbFile = CreateFileTest(sandbox, expectedText, expectedText.Length - 10);

                Assert_Big(dbFile, expectedText);
            });
        }
        private DbFile CreateFileTest(Node testRoot, string fileContent, int sizeLimit)
        {
            using (new SystemAccount())
            using (new SizeLimitSwindler(this, sizeLimit))
            {
                var file = new File(testRoot) { Name = "File1.file" };
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(fileContent));

                // action
                file.Save();

                // assert
                var dbFiles = BlobStoragePlatform.LoadDbFiles(file.VersionId);
                Assert.AreEqual(1, dbFiles.Length);
                var dbFile = dbFiles[0];
                if (NeedExternal(BlobStoragePlatform.ExpectedExternalBlobProviderType, fileContent, sizeLimit))
                {
                    Assert.AreEqual(BlobStoragePlatform.ExpectedExternalBlobProviderType.FullName, dbFile.BlobProvider);
                    Assert.IsNotNull(dbFile.BlobProviderData);
                }
                else
                {
                    Assert.IsNull(dbFile.BlobProvider);
                    Assert.IsNull(dbFile.BlobProviderData);
                }
                Assert.AreEqual(false, dbFile.IsDeleted);
                Assert.AreEqual(false, dbFile.Staging);
                Assert.AreEqual(0, dbFile.StagingVersionId);
                Assert.AreEqual(0, dbFile.StagingPropertyTypeId);
                Assert.AreEqual(fileContent.Length + 3, dbFile.Size);

                return dbFile;
            }
        }


        public void TestCase_UpdateFileSmallEmpty()
        {
            IntegrationTest((sandbox) =>
            {
                // 20 chars:       |------------------|
                var initialText = "Lorem ipsum...";
                var updatedText = string.Empty;
                var dbFile = UpdateFileTest(sandbox, initialText, updatedText, 20);

                Assert_Small(dbFile, updatedText);
            });
        }
        public void TestCase_UpdateFileBigEmpty()
        {
            IntegrationTest((sandbox) =>
            {
                // 20 chars:       |------------------|
                var initialText = "Lorem ipsum dolo sit amet...";
                var updatedText = string.Empty;
                var dbFile = UpdateFileTest(sandbox, initialText, updatedText, 20);

                Assert_Small(dbFile, updatedText);
            });
        }
        public void TestCase_UpdateFileSmallSmall()
        {
            IntegrationTest((sandbox) =>
            {
                // 20 chars:       |------------------|
                var initialText = "Lorem ipsum...";
                var updatedText = "Cras lobortis...";
                var dbFile = UpdateFileTest(sandbox, initialText, updatedText, 20);

                Assert_Small(dbFile, updatedText);
            });
        }
        public void TestCase_UpdateFileSmallBig()
        {
            IntegrationTest((sandbox) =>
            {
                // 20 chars:       |------------------|
                var initialText = "Lorem ipsum...";
                var updatedText = "Cras lobortis consequat nisi...";
                var dbFile = UpdateFileTest(sandbox, initialText, updatedText, 20);

                Assert_Big(dbFile, updatedText);
            });
        }
        public void TestCase_UpdateFileBigSmall()
        {
            IntegrationTest((sandbox) =>
            {
                // 20 chars:       |------------------|
                var initialText = "Lorem ipsum dolo sit amet...";
                var updatedText = "Cras lobortis...";
                var dbFile = UpdateFileTest(sandbox, initialText, updatedText, 20);

                Assert_Small(dbFile, updatedText);
            });
        }
        public void TestCase_UpdateFileBigBig()
        {
            IntegrationTest((sandbox) =>
            {
                // 20 chars:       |------------------|
                var initialText = "Lorem ipsum dolo sit amet...";
                var updatedText = "Cras lobortis consequat nisi...";
                var dbFile = UpdateFileTest(sandbox, initialText, updatedText, 20);

                Assert_Big(dbFile, updatedText);
            });
        }
        private DbFile UpdateFileTest(Node testRoot, string initialContent, string updatedContent, int sizeLimit)
        {
            using (new SystemAccount())
            using (new SizeLimitSwindler(this, sizeLimit))
            {
                var file = new File(testRoot) { Name = "File1.file" };
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(initialContent));
                file.Save();
                var fileId = file.Id;
                file = Node.Load<File>(fileId);
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(updatedContent));

                // action
                file.Save();

                // assert
                var dbFiles = BlobStoragePlatform.LoadDbFiles(file.VersionId);
                Assert.AreEqual(1, dbFiles.Length);
                var dbFile = dbFiles[0];
                //Assert.AreNotEqual(initialBlobId, file.Binary.FileId);
                if (NeedExternal(BlobStoragePlatform.ExpectedBlobProviderDataType, updatedContent, sizeLimit))
                {
                    Assert.AreEqual(BlobStoragePlatform.ExpectedExternalBlobProviderType.FullName, dbFile.BlobProvider);
                    Assert.IsNotNull(dbFile.BlobProviderData);
                }
                else
                {
                    Assert.IsNull(dbFile.BlobProvider);
                    Assert.IsNull(dbFile.BlobProviderData);
                }
                Assert.AreEqual(false, dbFile.IsDeleted);
                Assert.AreEqual(false, dbFile.Staging);
                Assert.AreEqual(0, dbFile.StagingVersionId);
                Assert.AreEqual(0, dbFile.StagingPropertyTypeId);
                if (updatedContent.Length == 0)
                    Assert.AreEqual(0, dbFile.Size);
                else
                    Assert.AreEqual(updatedContent.Length + 3, dbFile.Size);

                return dbFile;
            }
        }

        private void Assert_Small(DbFile dbFile, string expectedText)
        {
            var buffer = BlobStoragePlatform.CanUseBuiltInBlobProvider ? dbFile.Stream : dbFile.ExternalStream;
            Assert.IsNotNull(buffer);
            Assert.AreEqual(dbFile.Size, buffer.Length);
            Assert.AreEqual(expectedText, GetStringFromBytes(buffer));

            var ctx = BlobStorageBase.GetBlobStorageContextAsync(dbFile.FileId, CancellationToken.None)
                .GetAwaiter().GetResult();
            var expectedDataType = BlobStoragePlatform.CanUseBuiltInBlobProvider
                ? typeof(BuiltinBlobProviderData)
                : BlobStoragePlatform.ExpectedBlobProviderDataType;
            Assert.AreEqual(expectedDataType, ctx.BlobProviderData.GetType());
            Assert.AreEqual(dbFile.FileId, ctx.FileId);
            Assert.AreEqual(dbFile.Size, ctx.Length);
        }
        private void Assert_Big(DbFile dbFile, string expectedText)
        {
            var buffer = !NeedExternal(BlobStoragePlatform.ExpectedBlobProviderDataType) ? dbFile.Stream : dbFile.ExternalStream;
            Assert.IsNotNull(buffer);
            Assert.AreEqual(dbFile.Size, buffer.Length);
            Assert.AreEqual(expectedText, GetStringFromBytes(buffer));

            var ctx = BlobStorageBase.GetBlobStorageContextAsync(dbFile.FileId, CancellationToken.None)
                .GetAwaiter().GetResult();
            var expectedDataType = !NeedExternal(BlobStoragePlatform.ExpectedBlobProviderDataType)
                ? typeof(BuiltinBlobProviderData)
                : BlobStoragePlatform.ExpectedBlobProviderDataType;
            Assert.AreEqual(expectedDataType, ctx.BlobProviderData.GetType());
            Assert.AreEqual(dbFile.FileId, ctx.FileId);
            Assert.AreEqual(dbFile.Size, ctx.Length);
        }


        /* ================================================================================== TOOLS */
        protected Node CreateTestRoot()
        {
            var root = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
            root.Save();
            return root;
        }

        protected string GetStringFromBytes(byte[] bytes)
        {
            using (var stream = new IO.MemoryStream(bytes))
            using (var reader = new IO.StreamReader(stream))
                return reader.ReadToEnd();
        }
    }
}
