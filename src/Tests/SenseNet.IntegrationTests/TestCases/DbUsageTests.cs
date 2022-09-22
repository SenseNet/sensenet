﻿using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Versioning;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.Storage.DataModel.Usage;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.IntegrationTests.TestCases
{
    public class DbUsageTests : TestCaseBase
    {
        public async Task DbUsage_PreviewsVersionsBlobsTexts()
        {
            // Use ExclusiveBlock component in the DbUsage integration tests?
            await IntegrationTestAsync(async () =>
            {
                var user1 = new User(Node.LoadNode("/Root/IMS/Public"))
                {
                    Name = "User1",
                    Enabled = true,
                    Email = "user1@example.com",
                };
                user1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                var dataProvider = Providers.Instance.DataProvider;
                var loader = new DatabaseUsageLoader(dataProvider);
                var profileBefore = await loader.LoadAsync();

                CreateStructure(user1, out var folder, out var file);

                // ACTION
                loader = new DatabaseUsageLoader(dataProvider);
                var profile = await loader.LoadAsync();

                // ASSERT
                var versionCount = await dataProvider.GetVersionCountAsync("/Root", CancellationToken.None);
                Assert.AreEqual(6 * 4, profile.Preview.Count);
                Assert.AreEqual(6, profile.Content.Count);
                Assert.AreEqual(4, profile.OldVersions.Count);
                Assert.AreEqual(versionCount - profile.Preview.Count - profile.Content.Count - profile.OldVersions.Count,
                    profile.System.Count);

                Assert.AreEqual(6 * (144L + 145L + 146L + 147L + 4 * 3), profile.Preview.Blob);
                Assert.AreEqual(47 + 44 + 2 * 3, profile.Content.Blob);
                Assert.AreEqual(42 + 43 + 45 + 46 + 4 * 3, profile.OldVersions.Blob);
                Assert.AreEqual(profileBefore.System.Blob, profile.System.Blob);

                var folderText = 2 * folder.Description.Length;
                var oneText = 2 * file.Description.Length;
                Assert.AreEqual(0, profile.Preview.Text);
                Assert.AreEqual(profileBefore.Content.Text + 2 * oneText + folderText, profile.Content.Text);
                Assert.AreEqual(4 * oneText, profile.OldVersions.Text);
                Assert.AreEqual(profileBefore.System.Text, profile.System.Text);

                Assert.IsTrue(DateTime.UtcNow - profile.Executed < TimeSpan.FromSeconds(1),
                    "Expectation: DateTime.UtcNow - profile.Executed < TimeSpan.FromSeconds(1).");
                Assert.IsTrue(profile.ExecutionTime > TimeSpan.Zero,
                    "Expectation: profile.ExecutionTime > TimeSpan.Zero");
                Assert.IsTrue(profile.ExecutionTime < TimeSpan.MaxValue,
                    "Expectation: profile.ExecutionTime < TimeSpan.MaxValue");
            });
        }

        private void CreateStructure(User user, out SystemFolder myFolder, out File myFile)
        {
            using (CurrentUserBlock(user))
            {
                using (new SystemAccount())
                {
                    var contentFolder = Node.LoadNode("/Root/Content");

                    myFolder = new SystemFolder(contentFolder)
                    {
                        Name = "MyFolder",
                        Description = "Sample folder...",
                        InheritableVersioningMode = InheritableVersioningType.MajorAndMinor,
                        Version = new VersionNumber(1, 0, VersionStatus.Approved)
                    };
                    myFolder.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                    myFile = new File(myFolder) { Name = "MyFile.txt", Description = "Sample file...." };
                    myFile.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 42)));
                    myFile.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                    myFile.PublishAsync(CancellationToken.None).GetAwaiter().GetResult(); // 1.0.A

                    myFile.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                    myFile.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 43)));
                    myFile.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                    myFile.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult(); // 1.1.D

                    myFile.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                    myFile.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 44)));
                    myFile.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                    myFile.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult(); // 1.2.D

                    myFile.PublishAsync(CancellationToken.None).GetAwaiter().GetResult(); // 2.0.A

                    myFile.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                    myFile.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 45)));
                    myFile.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                    myFile.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult(); // 2.1.D

                    myFile.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                    myFile.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 46)));
                    myFile.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                    myFile.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult(); // 2.2.D

                    myFile.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();
                    myFile.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 47)));
                    myFile.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                    myFile.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult(); // 2.3.D
                }
            }

            var previewRoot = new SystemFolder(myFile)
            {
                Name = "Previews",
                VersioningMode = VersioningType.None,
                InheritableVersioningMode = InheritableVersioningType.None
            };
            previewRoot.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            CreateSamplePreviewImages(previewRoot, "V1.0.A");
            CreateSamplePreviewImages(previewRoot, "V1.1.D");
            CreateSamplePreviewImages(previewRoot, "V2.0.A");
            CreateSamplePreviewImages(previewRoot, "V2.1.D");
            CreateSamplePreviewImages(previewRoot, "V2.2.D");
            CreateSamplePreviewImages(previewRoot, "V2.3.D");
        }
        private void CreateSamplePreviewImages(SystemFolder root, string version)
        {
            var imgContainer = new SystemFolder(root) { Name = version };
            imgContainer.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            var img = new Image(imgContainer, "PreviewImage") { Name = "thumbnail1.png" };
            img.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 144)));
            img.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            img = new Image(imgContainer, "PreviewImage") { Name = "preview1.png" };
            img.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 145)));
            img.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            img = new Image(imgContainer, "PreviewImage") { Name = "thumbnail2.png" };
            img.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 146)));
            img.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            img = new Image(imgContainer, "PreviewImage") { Name = "preview2.png" };
            img.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 147)));
            img.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

    }
}
