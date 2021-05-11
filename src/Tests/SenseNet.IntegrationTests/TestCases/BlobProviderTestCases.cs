using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.IntegrationTests.Common;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.Testing;
using SenseNet.Tests.Core.Implementations;
using IO = System.IO;
using STT = System.Threading.Tasks;

namespace SenseNet.IntegrationTests.TestCases
{
    public class BlobProviderTestCases : BlobStorageTestCaseBase
    {
        private IBlobStorage BlobStorage => Providers.Instance.BlobStorage;

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
                var blobProviderBefore = file.Binary.BlobProvider;
                var fileRowIdBefore = file.Binary.FileId;

                file = Node.Load<File>(fileId);
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(updatedContent));

                // action
                file.Save();

                // assert
                var blobProviderAfter = file.Binary.BlobProvider;
                var fileRowIdAfter = file.Binary.FileId;
                // if blob provider before and after is built-in, the existing file row is updated, else re-created.
                if(blobProviderAfter == null && blobProviderBefore == null)
                    Assert.AreEqual(fileRowIdBefore, fileRowIdAfter);
                else
                    Assert.AreNotEqual(fileRowIdBefore, fileRowIdAfter);

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


        public void TestCase_WriteChunksSmall()
        {
            IntegrationTest((sandbox) =>
            {
                // 20 chars:       |------------------|
                // 10 chars:       |--------|---------|---------|
                var initialText = "Lorem ipsum dolo sit amet..";
                var updatedText = "Cras lobortis consequat nisi..";
                var dbFile = UpdateByChunksTest(initialText, updatedText, 222, 10);

                var stream = BlobStoragePlatform.CanUseBuiltInBlobProvider ? dbFile.Stream : dbFile.ExternalStream;
                Assert.IsNotNull(stream);
                Assert.AreEqual(dbFile.Size, stream.Length);
                Assert.AreEqual(updatedText, GetStringFromBytes(stream));
            });
        }
        public void TestCase_WriteChunksBig()
        {
            IntegrationTest((sandbox) =>
            {
                // 20 chars:       |------------------|
                // 10 chars:       |--------|---------|---------|
                var initialText = "Lorem ipsum dolo sit amet..";
                var updatedText = "Cras lobortis consequat nisi..";
                var dbFile = UpdateByChunksTest(initialText, updatedText, 20, 10);

                if (NeedExternal(BlobStoragePlatform.ExpectedBlobProviderDataType, updatedText, 20))
                {
                    Assert.IsNull(dbFile.Stream);
                    Assert.AreEqual(dbFile.Size, dbFile.ExternalStream.Length);
                    Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.ExternalStream));
                }
                else
                {
                    Assert.IsNotNull(dbFile.Stream);
                    Assert.AreEqual(dbFile.Size, dbFile.Stream.Length);
                    Assert.AreEqual(updatedText, GetStringFromBytes(dbFile.Stream));
                }
            });
        }
        private DbFile UpdateByChunksTest(string initialContent, string updatedText, int sizeLimit, int chunkSize)
        {
            using (new SystemAccount())
            using (new SizeLimitSwindler(this, sizeLimit))
            {
                var testRoot = CreateTestRoot();

                var file = new File(testRoot) { Name = "File1.file" };
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(initialContent));
                file.Save();
                var fileId = file.Id;

                var chunks = SplitFile(updatedText, chunkSize, out var fullSize);

                file = Node.Load<File>(fileId);
                file.Save(SavingMode.StartMultistepSave);
                var token = BinaryData.StartChunk(fileId, fullSize);

                var offset = 0;
                foreach (var chunk in chunks)
                {
                    BinaryData.WriteChunk(fileId, token, fullSize, chunk, offset);
                    offset += chunkSize;
                }

                BinaryData.CommitChunk(fileId, token, fullSize);

                file = Node.Load<File>(fileId);
                file.FinalizeContent();


                // assert
                var dbFiles = BlobStoragePlatform.LoadDbFiles(file.VersionId);
                Assert.AreEqual(1, dbFiles.Length);
                var dbFile = dbFiles[0];
                if (NeedExternal(BlobStoragePlatform.ExpectedBlobProviderDataType, updatedText, sizeLimit))
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
                Assert.AreEqual(fullSize, dbFile.Size);

                return dbFile;
            }
        }


        public void TestCase_DeleteBinaryPropertySmall()
        {
            IntegrationTest((sandbox) =>
            {
            });
            var initialText = "Lorem ipsum dolo sit amet..";
            DeleteBinaryPropertyTest(initialText, 222);
        }
        public void TestCase_DeleteBinaryPropertyBig()
        {
            IntegrationTest((sandbox) =>
            {
            });
            var initialText = "Lorem ipsum dolo sit amet..";
            DeleteBinaryPropertyTest(initialText, 20);
        }
        private void DeleteBinaryPropertyTest(string fileContent, int sizeLimit)
        {
            using (new SystemAccount())
            using (new SizeLimitSwindler(this, sizeLimit))
            {
                var testRoot = CreateTestRoot();

                var file = new File(testRoot) { Name = "File1.file" };
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(fileContent));
                file.Save();
                var fileId = file.Id;

                file = Node.Load<File>(fileId);
                // action
                file.Binary = null;
                file.Save();

                // assert
                var dbFiles = BlobStoragePlatform.LoadDbFiles(file.VersionId);
                Assert.AreEqual(0, dbFiles.Length);
            }
        }


        public void TestCase_CopyFileRowSmall()
        {
            IntegrationTest((sandbox) =>
            {
            });
            // 20 chars:       |------------------|
            // 10 chars:       |--------|---------|---------|
            var initialText = "Lorem ipsum dolo sit amet..";
            var updatedText = "Cras lobortis consequat nisi..";
            var dbFiles = CopyFileRowTest(initialText, updatedText, 222);

            var stream0 = BlobStoragePlatform.CanUseBuiltInBlobProvider ? dbFiles[0].Stream : dbFiles[0].ExternalStream;
            Assert.IsNotNull(stream0);
            Assert.AreEqual(dbFiles[0].Size, stream0.Length);
            Assert.AreEqual(initialText, GetStringFromBytes(stream0));

            var stream1 = BlobStoragePlatform.CanUseBuiltInBlobProvider ? dbFiles[1].Stream : dbFiles[1].ExternalStream;
            Assert.IsNotNull(stream1);
            Assert.AreEqual(dbFiles[1].Size, stream1.Length);
            Assert.AreEqual(updatedText, GetStringFromBytes(stream1));
        }
        public void TestCase_CopyFileRowBig()
        {
            IntegrationTest((sandbox) =>
            {
            });
            // 20 chars:       |------------------|
            // 10 chars:       |--------|---------|---------|
            var initialText = "Lorem ipsum dolo sit amet..";
            var updatedText = "Cras lobortis consequat nisi..";
            var dbFiles = CopyFileRowTest(initialText, updatedText, 20);

            if (NeedExternal(BlobStoragePlatform.ExpectedExternalBlobProviderType))
            {
                Assert.IsNull(dbFiles[0].Stream);
                Assert.AreEqual(dbFiles[0].Size, dbFiles[0].ExternalStream.Length);
                Assert.AreEqual(initialText, GetStringFromBytes(dbFiles[0].ExternalStream));

                Assert.IsTrue(dbFiles[1].Stream == null || dbFiles[1].Stream.Length == 0);
                Assert.AreEqual(dbFiles[1].Size, dbFiles[1].ExternalStream.Length);
                Assert.AreEqual(updatedText, GetStringFromBytes(dbFiles[1].ExternalStream));

            }
            else
            {
                Assert.IsNotNull(dbFiles[0].Stream);
                Assert.AreEqual(dbFiles[0].Size, dbFiles[0].Stream.Length);
                Assert.AreEqual(initialText, GetStringFromBytes(dbFiles[0].Stream));

                Assert.IsNotNull(dbFiles[1].Stream);
                Assert.AreEqual(dbFiles[1].Size, dbFiles[1].Stream.Length);
                Assert.AreEqual(updatedText, GetStringFromBytes(dbFiles[1].Stream));
            }
        }
        private DbFile[] CopyFileRowTest(string initialContent, string updatedText, int sizeLimit)
        {
            using (new SystemAccount())
            using (new SizeLimitSwindler(this, sizeLimit))
            {
                var testRoot = CreateTestRoot();
                var target = new SystemFolder(testRoot) { Name = "Target" };
                target.Save();

                var file = new File(testRoot) { Name = "File1.file" };
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(initialContent));
                file.Save();

                // action
                file.CopyTo(target);

                // assert
                var copy = Node.Load<File>(RepositoryPath.Combine(target.Path, file.Name));
                Assert.AreNotEqual(file.Id, copy.Id);
                Assert.AreNotEqual(file.VersionId, copy.VersionId);
                Assert.AreEqual(file.Binary.FileId, copy.Binary.FileId);

                // action 2
                copy.Binary.SetStream(RepositoryTools.GetStreamFromString(updatedText));
                copy.Save();

                // assert 2
                Assert.AreNotEqual(file.Binary.FileId, copy.Binary.FileId);

                var dbFiles = new DbFile[2];

                var loadedDbFiles = BlobStoragePlatform.LoadDbFiles(file.VersionId);
                Assert.AreEqual(1, loadedDbFiles.Length);
                dbFiles[0] = loadedDbFiles[0];

                loadedDbFiles = BlobStoragePlatform.LoadDbFiles(copy.VersionId);
                Assert.AreEqual(1, loadedDbFiles.Length);
                dbFiles[1] = loadedDbFiles[0];

                if (NeedExternal(BlobStoragePlatform.ExpectedBlobProviderDataType, initialContent, sizeLimit))
                {
                    Assert.AreEqual(BlobStoragePlatform.ExpectedExternalBlobProviderType.FullName, dbFiles[0].BlobProvider);
                    Assert.IsNotNull(dbFiles[0].BlobProviderData);
                }
                else
                {
                    Assert.IsNull(dbFiles[0].BlobProvider);
                    Assert.IsNull(dbFiles[0].BlobProviderData);
                }
                Assert.AreEqual(false, dbFiles[0].IsDeleted);
                Assert.AreEqual(false, dbFiles[0].Staging);
                Assert.AreEqual(0, dbFiles[0].StagingVersionId);
                Assert.AreEqual(0, dbFiles[0].StagingPropertyTypeId);
                Assert.AreEqual(initialContent.Length + 3, dbFiles[0].Size);

                if (NeedExternal(BlobStoragePlatform.ExpectedBlobProviderDataType, updatedText, sizeLimit))
                {
                    Assert.AreEqual(BlobStoragePlatform.ExpectedExternalBlobProviderType.FullName, dbFiles[1].BlobProvider);
                    Assert.IsNotNull(dbFiles[1].BlobProviderData);
                }
                else
                {
                    Assert.IsNull(dbFiles[1].BlobProvider);
                    Assert.IsNull(dbFiles[1].BlobProviderData);
                }
                Assert.AreEqual(false, dbFiles[1].IsDeleted);
                Assert.AreEqual(false, dbFiles[1].Staging);
                Assert.AreEqual(0, dbFiles[1].StagingVersionId);
                Assert.AreEqual(0, dbFiles[1].StagingPropertyTypeId);
                Assert.AreEqual(updatedText.Length + 3, dbFiles[1].Size);

                return dbFiles;
            }
        }


        public void TestCase_BinaryCacheEntitySmall()
        {
            IntegrationTest((sandbox) =>
            {
                var expectedText = "Lorem ipsum dolo sit amet";
                BinaryCacheEntityTest(expectedText, expectedText.Length + 10);
            });
        }
        public void TestCase_BinaryCacheEntityBig()
        {
            IntegrationTest((sandbox) =>
            {
                var expectedText = "Lorem ipsum dolo sit amet";
                BinaryCacheEntityTest(expectedText, expectedText.Length - 10);
            });
        }
        private void BinaryCacheEntityTest(string fileContent, int sizeLimit)
        {
            using (new SystemAccount())
            using (new SizeLimitSwindler(this, sizeLimit))
            {
                var testRoot = CreateTestRoot();

                var file = new File(testRoot) { Name = "File1.file" };
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(fileContent));
                file.Save();
                var versionId = file.VersionId;
                var binaryPropertyId = file.Binary.Id;
                var fileId = file.Binary.FileId;
                var propertyTypeId = PropertyType.GetByName("Binary").Id;

                // action
                var binaryCacheEntity = BlobStorage.LoadBinaryCacheEntityAsync(
                    file.VersionId, propertyTypeId, CancellationToken.None).GetAwaiter().GetResult();

                // assert
                Assert.AreEqual(binaryPropertyId, binaryCacheEntity.BinaryPropertyId);
                Assert.AreEqual(fileId, binaryCacheEntity.FileId);
                Assert.AreEqual(fileContent.Length + 3, binaryCacheEntity.Length);

                Assert.AreEqual(versionId, binaryCacheEntity.Context.VersionId);
                Assert.AreEqual(propertyTypeId, binaryCacheEntity.Context.PropertyTypeId);
                Assert.AreEqual(fileId, binaryCacheEntity.Context.FileId);
                Assert.AreEqual(fileContent.Length + 3, binaryCacheEntity.Context.Length);

                if (NeedExternal(BlobStoragePlatform.ExpectedBlobProviderDataType, fileContent, sizeLimit))
                {
                    Assert.IsTrue(binaryCacheEntity.Context.Provider.GetType() == BlobStoragePlatform.ExpectedExternalBlobProviderType);
                    Assert.IsTrue(binaryCacheEntity.Context.BlobProviderData.GetType() == BlobStoragePlatform.ExpectedBlobProviderDataType);
                    Assert.AreEqual(fileContent, GetStringFromBytes(BlobStoragePlatform.GetExternalData(binaryCacheEntity.Context)));
                }
                else
                {
                    Assert.AreEqual(fileContent, GetStringFromBytes(binaryCacheEntity.RawData));
                }
            }
        }


        public void TestCase_DeleteSmall()
        {
            IntegrationTest((sandbox) =>
            {
                var expectedText = "Lorem ipsum dolo sit amet";
                DeleteTest(expectedText, expectedText.Length + 10);
            });
        }
        public void TestCase_DeleteBig()
        {
            IntegrationTest((sandbox) =>
            {
                var expectedText = "Lorem ipsum dolo sit amet";
                DeleteTest(expectedText, expectedText.Length - 10);
            });
        }
        private void DeleteTest(string fileContent, int sizeLimit)
        {
            using (new SystemAccount())
            using (new SizeLimitSwindler(this, sizeLimit))
            {
                var propertyTypeId = PropertyType.GetByName("Binary").Id;
                var external = NeedExternal(BlobStoragePlatform.ExpectedBlobProviderDataType, fileContent, sizeLimit);

                var testRoot = CreateTestRoot();

                var file = new File(testRoot) { Name = "File1.file" };
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(fileContent));
                file.Save();
                var fileId = file.Binary.FileId;
                // memorize blob storage context for further check
                var ctx = BlobStorage.GetBlobStorageContextAsync(file.Binary.FileId, false,
                    file.VersionId, propertyTypeId, CancellationToken.None).GetAwaiter().GetResult();
                BlobStoragePlatform.UpdateFileCreationDate(file.Binary.FileId, DateTime.UtcNow.AddDays(-1));

                // Action #1
                file.ForceDelete();

                // Assert #1
                var dbFile = BlobStoragePlatform.LoadDbFile(fileId);
                Assert.IsNotNull(dbFile);
                Assert.AreEqual(false, dbFile.IsDeleted);
                Assert.IsFalse(IsDeleted(ctx, external));

                // Action #2
                BlobStorage.CleanupFilesSetFlagAsync(CancellationToken.None)
                    .GetAwaiter().GetResult();

                // Assert #2
                dbFile = BlobStoragePlatform.LoadDbFile(fileId);
                Assert.IsNotNull(dbFile);
                Assert.AreEqual(true, dbFile.IsDeleted);
                Assert.IsFalse(IsDeleted(ctx, external));

                // Action #3
                var _ = BlobStorage.CleanupFilesAsync(CancellationToken.None)
                    .GetAwaiter().GetResult();

                // Assert #3
                dbFile = BlobStoragePlatform.LoadDbFile(fileId);
                Assert.IsNull(dbFile);
                Assert.IsTrue(IsDeleted(ctx, external));
            }
        }


        public void TestCase_DeletionPolicy_Default()
        {
            IntegrationTest((sandbox) =>
            {
                var dp = DataStore.DataProvider;
                var tdp = DataStore.DataProvider.GetExtension<ITestingDataProviderExtension>();

                Assert.AreEqual(BlobDeletionPolicy.BackgroundDelayed, Configuration.BlobStorage.BlobDeletionPolicy);
                var countsBefore = GetDbObjectCountsAsync(null, dp, tdp).ConfigureAwait(false).GetAwaiter().GetResult();

                DeletionPolicy_TheTest();

                var countsAfter = GetDbObjectCountsAsync(null, dp, tdp).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.AreEqual(countsBefore.AllCountsExceptFiles, countsAfter.AllCountsExceptFiles);
                Assert.AreNotEqual(countsBefore.Files, countsAfter.Files);
                Thread.Sleep(500);
                countsAfter = GetDbObjectCountsAsync(null, dp, tdp).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.AreNotEqual(countsBefore.Files, countsAfter.Files);
            });
        }
        public void TestCase_DeletionPolicy_Immediately()
        {
            IntegrationTest((sandbox) =>
            {
                var dp = DataStore.DataProvider;
                var tdp = DataStore.DataProvider.GetExtension<ITestingDataProviderExtension>();
                var countsBefore = GetDbObjectCountsAsync(null, dp, tdp).ConfigureAwait(false).GetAwaiter().GetResult();

                using (new BlobDeletionPolicySwindler(BlobDeletionPolicy.Immediately))
                    DeletionPolicy_TheTest();

                var countsAfter = GetDbObjectCountsAsync(null, dp, tdp).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.AreEqual(countsBefore.Files, countsAfter.Files);
                Assert.AreEqual(countsBefore.AllCounts, countsAfter.AllCounts);
            });
        }
        public void TestCase_DeletionPolicy_BackgroundImmediately()
        {
            IntegrationTest((sandbox) =>
            {
                var dp = DataStore.DataProvider;
                var tdp = DataStore.DataProvider.GetExtension<ITestingDataProviderExtension>();
                var countsBefore = GetDbObjectCountsAsync(null, dp, tdp).ConfigureAwait(false).GetAwaiter().GetResult();

                using (BlobStoragePlatform.SwindleWaitingBetweenCleanupFiles(10))
                {
                    using (new BlobDeletionPolicySwindler(BlobDeletionPolicy.BackgroundImmediately))
                        DeletionPolicy_TheTest();

                    var countsAfter = GetDbObjectCountsAsync(null, dp, tdp).ConfigureAwait(false).GetAwaiter().GetResult();
                    Assert.AreEqual(countsBefore.AllCountsExceptFiles, countsAfter.AllCountsExceptFiles);
                    Assert.AreNotEqual(countsBefore.Files, countsAfter.Files);
                    Thread.Sleep(500);
                    countsAfter = GetDbObjectCountsAsync(null, dp, tdp).ConfigureAwait(false).GetAwaiter().GetResult();
                    Assert.AreEqual(countsBefore.Files, countsAfter.Files);
                }
            });
        }
        private void DeletionPolicy_TheTest()
        {
            using (new SystemAccount())
            {
                // Create a small subtree
                var root = new SystemFolder(Repository.Root) { Name = "TestRoot" };
                root.Save();
                var f1 = new SystemFolder(root) { Name = "F1" };
                f1.Save();
                for (int i = 0; i < 5; i++)
                {
                    var f2 = new File(root) { Name = $"F2-{i}" };
                    f2.Binary.SetStream(RepositoryTools.GetStreamFromString("filecontent"));
                    f2.Save();
                }
                var f3 = new SystemFolder(f1) { Name = "F3" };
                f3.Save();
                for (int i = 0; i < 5; i++)
                {
                    var f4 = new File(root) { Name = $"F4-{i}" };
                    f4.Binary.SetStream(RepositoryTools.GetStreamFromString("filecontent"));
                    f4.Save();
                }

                // ACTION
                Node.ForceDelete(root.Path);

                // ASSERT
                //Assert.IsNull(Node.Load<SystemFolder>(root.Id));
                //Assert.IsNull(Node.Load<SystemFolder>(f1.Id));
                //Assert.IsNull(Node.Load<File>(f2.Id));
                //Assert.IsNull(Node.Load<SystemFolder>(f3.Id));
                //Assert.IsNull(Node.Load<File>(f4.Id));
            }
        }
        private async STT.Task<(int Nodes, int Versions, int Binaries, int Files, int LongTexts, string AllCounts, string AllCountsExceptFiles)> GetDbObjectCountsAsync(string path, DataProvider dp, ITestingDataProviderExtension tdp)
        {
            var nodesTask = dp.GetNodeCountAsync(path, CancellationToken.None);
            var versionsTask = dp.GetVersionCountAsync(path, CancellationToken.None);
            var binariesTasks = tdp.GetBinaryPropertyCountAsync(path);
            var filesTask = tdp.GetFileCountAsync(path);
            var longTextsTask = tdp.GetLongTextCountAsync(path);

            STT.Task.WaitAll(nodesTask, versionsTask, binariesTasks, filesTask, longTextsTask);

            var nodes = nodesTask.Result;
            var versions = versionsTask.Result;
            var binaries = binariesTasks.Result;
            var files = filesTask.Result;
            var longTexts = longTextsTask.Result;

            var all = $"{nodes},{versions},{binaries},{files},{longTexts}";
            var allExceptFiles = $"{nodes},{versions},{binaries},{longTexts}";

            var result = (Nodes: nodes, Versions: versions, Binaries: binaries, Files: files, LongTexts: longTexts, AllCounts: all, AllCountsExceptFiles: allExceptFiles);
            return await STT.Task.FromResult(result);
        }

        //TODO: [DIBLOB] replace this swindler technology with local service and options instances
        private class BlobDeletionPolicySwindler : Swindler<BlobDeletionPolicy>
        {
            public BlobDeletionPolicySwindler(BlobDeletionPolicy hack) : base(
                hack,
                () => ((ContentRepository.Storage.Data.BlobStorage)Providers.Instance.BlobStorage).BlobStorageConfig.BlobDeletionPolicy,
                (value) =>
                {
                    ((ContentRepository.Storage.Data.BlobStorage)Providers.Instance.BlobStorage).BlobStorageConfig.BlobDeletionPolicy =
                        value;
                })
            {
            }
        }

        /* ==================================================================================================== */

        private void Assert_Small(DbFile dbFile, string expectedText)
        {
            var buffer = BlobStoragePlatform.CanUseBuiltInBlobProvider ? dbFile.Stream : dbFile.ExternalStream;
            Assert.IsNotNull(buffer);
            Assert.AreEqual(dbFile.Size, buffer.Length);
            Assert.AreEqual(expectedText, GetStringFromBytes(buffer));

            var ctx = BlobStorage.GetBlobStorageContextAsync(dbFile.FileId, CancellationToken.None)
                .GetAwaiter().GetResult();
            var expectedDataType = BlobStoragePlatform.CanUseBuiltInBlobProvider
                ? typeof(BuiltinBlobProviderData)
                : BlobStoragePlatform.ExpectedBlobProviderDataType;
            Assert.AreEqual(expectedDataType, ctx.BlobProviderData.GetType());
            Assert.AreEqual(dbFile.FileId, ctx.FileId);
            Assert.AreEqual(dbFile.Size, ctx.Length);
            AssertRawData(dbFile, BlobStoragePlatform.UseChunk, expectedText);
        }
        private void Assert_Big(DbFile dbFile, string expectedText)
        {
            var buffer = !NeedExternal(BlobStoragePlatform.ExpectedBlobProviderDataType) ? dbFile.Stream : dbFile.ExternalStream;
            Assert.IsNotNull(buffer);
            Assert.AreEqual(dbFile.Size, buffer.Length);
            Assert.AreEqual(expectedText, GetStringFromBytes(buffer));

            var ctx = BlobStorage.GetBlobStorageContextAsync(dbFile.FileId, CancellationToken.None)
                .GetAwaiter().GetResult();
            var expectedDataType = !NeedExternal(BlobStoragePlatform.ExpectedBlobProviderDataType)
                ? typeof(BuiltinBlobProviderData)
                : BlobStoragePlatform.ExpectedBlobProviderDataType;
            Assert.AreEqual(expectedDataType, ctx.BlobProviderData.GetType());
            Assert.AreEqual(dbFile.FileId, ctx.FileId);
            Assert.AreEqual(dbFile.Size, ctx.Length);
            AssertRawData(dbFile, BlobStoragePlatform.UseChunk, expectedText);
        }
        private void AssertRawData(DbFile dbFile, bool useChunk, string expectedText)
        {
            byte[][] data = BlobStoragePlatform.GetRawData(dbFile.FileId);

            if (dbFile.Size == 0L)
                Assert.AreEqual(0, data.Length);
            else if (useChunk && dbFile.BlobProvider == null)
                Assert.AreEqual(1, data.Length);
            else if (useChunk && dbFile.BlobProvider != null)
                Assert.AreNotEqual(1, data.Length);
            else
                Assert.AreEqual(1, data.Length);

            var length = data.Select(d=>d.Length).Sum();
            var buffer = new byte[length];
            var offset = 0;
            foreach (var item in data)
            {
                Array.Copy(item, 0, buffer, offset, item.Length);
                offset += item.Length;
            }

            string actualText;
            using (var stream = new IO.MemoryStream(buffer))
                actualText = RepositoryTools.GetStreamString(stream);

            Assert.AreEqual(expectedText, actualText);
        }

        private List<byte[]> SplitFile(string text, int chunkSize, out int fullSize)
        {
            var stream = (IO.MemoryStream)RepositoryTools.GetStreamFromString(text);
            var buffer = stream.GetBuffer();
            var bytes = new byte[text.Length + 3];
            fullSize = bytes.Length;

            Array.Copy(buffer, 0, bytes, 0, bytes.Length);

            var chunks = new List<byte[]>();
            //var bytes = Encoding.UTF8.GetBytes(text);
            var p = 0;
            while (p < bytes.Length)
            {
                var size = Math.Min(chunkSize, bytes.Length - p);
                var chunk = new byte[size];
                Array.Copy(bytes, p, chunk, 0, size);
                chunks.Add(chunk);
                p += chunkSize;
            }
            return chunks;
        }

        private bool IsDeleted(BlobStorageContext context, bool external)
        {
            return external
                ? BlobStoragePlatform.GetExternalData(context) == null
                : BlobStoragePlatform.LoadDbFile(context.FileId) == null;
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
