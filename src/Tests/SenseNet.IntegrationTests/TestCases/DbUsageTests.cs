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
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.Storage.DataModel;
using SenseNet.Testing;
using SenseNet.Tests.Core;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.IntegrationTests.TestCases
{
    public class DbUsageTests : TestCaseBase
    {
        public async Task DbUsage_1()
        {
            await IsolatedIntegrationTestAsync(async () =>
            {
                var before = (await new DatabaseUsageProfileAccessor().LoadDatabaseUsageModelAsync()).Nodes;

                // ACTION-1 Create a folder and a file
                var folder = new SystemFolder(Repository.Root) {Name = Guid.NewGuid().ToString()};
                folder.Description = "Sample folder...";
                folder.Save();
                var file = new File(folder) { Name = Guid.NewGuid().ToString() };
                file.Description = "Sample file.....";
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 42)));
                file.Save();

                var after = (await new DatabaseUsageProfileAccessor().LoadDatabaseUsageModelAsync()).Nodes;

                // ASSERT-1
                // new id-s
                Assert.AreEqual(before.Length + 2, after.Length);
                var actual = string.Join(",", after.Select(x => x.Id)
                    .Except(before.Select(x => x.Id))
                    .OrderBy(x => x));
                Assert.AreEqual($"{folder.Id},{file.Id}", actual);

                // longtext sizes are increased 16 + 16 unicode chars.
                Assert.AreEqual(2*2*16 + before.Select(x => x.LastDraftVersion).Sum(x => x.LongTextSizes),
                after.Select(x => x.LastDraftVersion).Sum(x => x.LongTextSizes));

                // check size of file's blob (bom + text size)
                Assert.AreEqual(42 + 3 + before.Select(x => x.LastDraftVersion).Sum(x => x.BlobSizes),
                    after.Select(x => x.LastDraftVersion).Sum(x => x.BlobSizes));

                // ACTION-2 Delete the folder and a file
                Node.ForceDelete(folder.Id);

                var after2 = (await new DatabaseUsageProfileAccessor().LoadDatabaseUsageModelAsync()).Nodes;

                // ASSERT-2 back to the starting field.
                Assert.AreEqual(before.Length, after2.Length);

                Assert.AreEqual(before.Select(x => x.LastDraftVersion).Sum(x => x.LongTextSizes),
                    after2.Select(x => x.LastDraftVersion).Sum(x => x.LongTextSizes));

                Assert.AreEqual(before.Select(x => x.LastDraftVersion).Sum(x => x.BlobSizes),
                    after2.Select(x => x.LastDraftVersion).Sum(x => x.BlobSizes));

            });
        }
        public async Task DbUsage_CheckPreviewStructure()
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
                Group.Administrators.AddMember(user1);

                File file;
                using (CurrentUserBlock(user1))
                {
                    var contentFolder = Node.LoadNode("/Root/Content");

                    var folder = new SystemFolder(contentFolder) { Name = "MyFolder", Description = "Sample folder..."};
                    folder.Save();
                    
                    file = new File(folder) { Name = "MyFile", Description = "Sample file...."};
                    file.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 42)));
                    file.Save();
                }

                var pf = new SystemFolder(file) { Name = "Previews" };
                pf.Save();
                
                var imgContainer = new SystemFolder(pf) { Name = "V1.0.A" };
                imgContainer.Save();

                var img = new Image(imgContainer, "PreviewImage") { Name = "1.png" };
                img.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 142)));
                img.Save();

                img = new Image(imgContainer, "PreviewImage") { Name = "2.png" };
                img.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 143)));
                img.Save();

                imgContainer = new SystemFolder(pf) { Name = "V2.0.L" };
                imgContainer.Save();

                img = new Image(imgContainer, "PreviewImage") { Name = "1.png" };
                img.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 144)));
                img.Save();

                img = new Image(imgContainer, "PreviewImage") { Name = "2.png" };
                img.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 145)));
                img.Save();

                // -------------------------------------------------------------------------

                var profile = new DatabaseUsageProfileAccessor();
                var model = await profile.LoadDatabaseUsageModelAsync();
                var previewNodes = profile.GetPreviewNodes(model);

                Assert.AreEqual(7, previewNodes.Length);
                Assert.Inconclusive();
            });
        }
        public async Task DbUsage_2()
        {
            await IsolatedIntegrationTestAsync(async () =>
            {
                var before = new DatabaseUsageProfile(Providers.Instance.DataProvider);

                var user1 = new User(Node.LoadNode("/Root/IMS/Public"))
                {
                    Name = $"User1",
                    Enabled = true,
                    Email = $"user1@example.com",
                };
                user1.Save();
                Group.Administrators.AddMember(user1);

                File file;
                using (CurrentUserBlock(user1))
                {
                    var contentFolder = Node.LoadNode("/Root/Content");

                    var folder = new SystemFolder(contentFolder) { Name = "MyFolder", Description = "Sample folder..." };
                    folder.Save();

                    file = new File(folder) { Name = "MyFile", Description = "Sample file...." };
                    file.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 42)));
                    file.Save();
                }

                var pf = new SystemFolder(file) { Name = "Previews" };
                pf.Save();

                var imgContainer = new SystemFolder(pf) { Name = "V1.0.A" };
                imgContainer.Save();

                var img = new Image(imgContainer, "PreviewImage") { Name = "1.png" };
                img.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 142)));
                img.Save();

                img = new Image(imgContainer, "PreviewImage") { Name = "2.png" };
                img.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 143)));
                img.Save();

                imgContainer = new SystemFolder(pf) { Name = "V2.0.L" };
                imgContainer.Save();

                img = new Image(imgContainer, "PreviewImage") { Name = "1.png" };
                img.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 144)));
                img.Save();

                img = new Image(imgContainer, "PreviewImage") { Name = "2.png" };
                img.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 145)));
                img.Save();

                // -------------------------------------------------------------------------

                var after = new DatabaseUsageProfile(Providers.Instance.DataProvider);
                await after.BuildProfileAsync().ConfigureAwait(false);

                Assert.AreEqual(142L + 143L + 144L + 145L, after.Preview.Blob);

                Assert.Inconclusive();
            });
        }

        private class DatabaseUsageProfileAccessor : Accessor
        {
            public DatabaseUsageProfileAccessor() : this(new DatabaseUsageProfile(Providers.Instance.DataProvider))
            {
            }
            public DatabaseUsageProfileAccessor(DatabaseUsageProfile instance) : base (instance)
            {
            }

            public async Task<DatabaseUsageProfile.DatabaseUsageModel> LoadDatabaseUsageModelAsync()
            {
                return await ((Task<DatabaseUsageProfile.DatabaseUsageModel>)base
                        .CallPrivateMethod("LoadDatabaseUsageModelAsync", CancellationToken.None))
                    .ConfigureAwait(false);
            }
            public DatabaseUsageProfile.NodeModel[] GetPreviewNodes(DatabaseUsageProfile.DatabaseUsageModel model)
            {
                return (DatabaseUsageProfile.NodeModel[])base.CallPrivateMethod("GetPreviewNodes", model);
            }
        }



        //private async Task<DatabaseUsageProfile.NodeModel[]> LoadDatabaseUsageModelAsync(DatabaseUsageProfile profile = null)
        //{
        //    profile ??= new DatabaseUsageProfile(Providers.Instance.DataProvider);

        //    var acc = new ObjectAccessor(profile);
        //    var model = await ((Task<DatabaseUsageProfile.DatabaseUsageModel>)acc
        //            .Invoke("LoadDatabaseUsageModelAsync", CancellationToken.None))
        //        .ConfigureAwait(false);
        //    return model.Nodes.ToArray();
        //}
        //private async Task<DatabaseUsageProfile.NodeModel[]> GetPreviewNodes(DatabaseUsageProfile profile = null)
        //{
        //    profile ??= new DatabaseUsageProfile(Providers.Instance.DataProvider);

        //    var acc = new ObjectAccessor(profile);
        //    var model = await ((Task<DatabaseUsageProfile.DatabaseUsageModel>)acc
        //            .Invoke("LoadDatabaseUsageModelAsync", CancellationToken.None))
        //        .ConfigureAwait(false);
        //    return model.Nodes.ToArray();
        //}
    }
}
