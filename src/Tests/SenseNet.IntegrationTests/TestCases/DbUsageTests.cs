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
        public async Task DbUsage_Previews()
        {
            await IsolatedIntegrationTestAsync(async () =>
            {
                var user1 = new User(Node.LoadNode("/Root/IMS/Public"))
                {
                    Name = $"User1",
                    Enabled = true,
                    Email = $"user1@example.com",
                };
                user1.Save();

                File file;
                using (CurrentUserBlock(user1))
                {
                    using (new SystemAccount())
                    {
                        var contentFolder = Node.LoadNode("/Root/Content");

                        var folder = new SystemFolder(contentFolder)
                        {
                            Name = "MyFolder",
                            Description = "Sample folder...",
                            InheritableVersioningMode = InheritableVersioningType.MajorAndMinor,
                            Version = new VersionNumber(1,0,VersionStatus.Approved)
                        };
                        folder.Save();

                        file = new File(folder) { Name = "MyFile.txt", Description = "Sample file...." };
                        file.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 42)));
                        file.Save();
                        file.Publish(); // 1.0.A

                        file.CheckOut();
                        file.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 43)));
                        file.Save();
                        file.CheckIn();

                        file.CheckOut();
                        file.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 44)));
                        file.Save();
                        file.CheckIn();

                        file.Publish(); // 2.0.A

                        file.CheckOut();
                        file.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 45)));
                        file.Save();
                        file.CheckIn();

                        file.CheckOut();
                        file.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 46)));
                        file.Save();
                        file.CheckIn();

                        file.CheckOut();
                        file.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 47)));
                        file.Save();
                        file.CheckIn();
                    }
                }

                var previewRoot = new SystemFolder(file)
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

                // ACTION
                var profile = new DatabaseUsageProfile(Providers.Instance.DataProvider);
                await profile.BuildProfileAsync();

                // ASSERT
                Assert.AreEqual(6 * 4, profile.Preview.Count);
                Assert.AreEqual(6 * (144L + 145L + 146L + 147L + 4 * 3), profile.Preview.Blob);
            });
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
