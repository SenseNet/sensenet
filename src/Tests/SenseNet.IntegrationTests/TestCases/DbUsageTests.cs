using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Versioning;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.Storage.DataModel;
using SenseNet.Testing;
using SenseNet.Tests.Core;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.IntegrationTests.TestCases
{
    public class DbUsageTests : TestCaseBase
    {
        public async Task DbUsage_PreviewsVersionsBlobsTexts()
        {
            await IsolatedIntegrationTestAsync(async () =>
            {
                var user1 = new User(Node.LoadNode("/Root/IMS/Public"))
                {
                    Name = "User1",
                    Enabled = true,
                    Email = "user1@example.com",
                };
                user1.Save();

                var dataProvider = Providers.Instance.DataProvider;
                var profileBefore = new DatabaseUsageProfile(dataProvider);
                await profileBefore.BuildProfileAsync();

                CreateStructure(user1, out var folder, out var file);

                // ACTION
                var profile = new DatabaseUsageProfile(dataProvider);
                await profile.BuildProfileAsync();

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
                    myFolder.Save();

                    myFile = new File(myFolder) { Name = "MyFile.txt", Description = "Sample file...." };
                    myFile.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 42)));
                    myFile.Save();
                    myFile.Publish(); // 1.0.A

                    myFile.CheckOut();
                    myFile.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 43)));
                    myFile.Save();
                    myFile.CheckIn(); // 1.1.D

                    myFile.CheckOut();
                    myFile.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 44)));
                    myFile.Save();
                    myFile.CheckIn(); // 1.2.D

                    myFile.Publish(); // 2.0.A

                    myFile.CheckOut();
                    myFile.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 45)));
                    myFile.Save();
                    myFile.CheckIn(); // 2.1.D

                    myFile.CheckOut();
                    myFile.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 46)));
                    myFile.Save();
                    myFile.CheckIn(); // 2.2.D

                    myFile.CheckOut();
                    myFile.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 47)));
                    myFile.Save();
                    myFile.CheckIn(); // 2.3.D
                }
            }

            var previewRoot = new SystemFolder(myFile)
            {
                Name = "Previews",
                VersioningMode = VersioningType.None,
                InheritableVersioningMode = InheritableVersioningType.None
            };
            previewRoot.Save();

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
            imgContainer.Save();

            var img = new Image(imgContainer, "PreviewImage") { Name = "thumbnail1.png" };
            img.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 144)));
            img.Save();

            img = new Image(imgContainer, "PreviewImage") { Name = "preview1.png" };
            img.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 145)));
            img.Save();

            img = new Image(imgContainer, "PreviewImage") { Name = "thumbnail2.png" };
            img.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 146)));
            img.Save();

            img = new Image(imgContainer, "PreviewImage") { Name = "preview2.png" };
            img.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 147)));
            img.Save();
        }

    }
}
