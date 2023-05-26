using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Frameworks;
using Org.BouncyCastle.Utilities.Zlib;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Preview;
using SenseNet.TaskManagement.Core;
using SenseNet.Tests.Core;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class PreviewSwitchTests : TestBase
    {
        //[TestMethod]
        public async System.Threading.Tasks.Task Xxxx_()
        {
            var cancel = new CancellationTokenSource().Token;
            await Test(async () =>
            {
                var testRoot = new SystemFolder(Node.LoadNode("/Root/Content")) {Name = "TestRoot"};
                await testRoot.SaveAsync(cancel);

                var file1 = new File(testRoot) {Name = "File1.txt"}; await file1.SaveAsync(cancel);
                var previews = new SystemFolder(file1) {Name = "Previews"}; await previews.SaveAsync(cancel);
                var v10A = new SystemFolder(previews) {Name = "1.0.A"}; await v10A.SaveAsync(cancel);

                var preview1_png = new Image(v10A, "PreviewImage") {Name = "preview1.png" };
                await using (var loadingStream = new FileStream(@"C:\Users\kavics\Desktop\preview\800x500.png", FileMode.Open,
                           FileAccess.Read))
                {
                    var imgBuffer = new byte[loadingStream.Length];
                    await loadingStream.ReadAsync(imgBuffer, 0, imgBuffer.Length, cancel);
                    using (var memStream = new MemoryStream(imgBuffer))
                    {
                        preview1_png.Binary.SetStream(memStream);
                        await preview1_png.SaveAsync(cancel);
                    }
                }

                // ACT
                preview1_png = Node.Load<Image>(preview1_png.Id);
                var options = new PreviewImageOptions {RestrictionType = RestrictionType.Watermark};
                await using var outputStream = DocumentPreviewProvider.Current.GetRestrictedImage(preview1_png, options);
                await using var savingStream = new FileStream(@"C:\Users\kavics\Desktop\preview\output.png", FileMode.OpenOrCreate, FileAccess.Write);
                await outputStream.CopyToAsync(savingStream, cancel);

                // ASSERT
                Assert.Inconclusive();
            });
        }



        [TestMethod]
        public void Preview_Switch_()
        {
            Test2(services =>
                {
                    services
                        .AddSenseNetDocumentPreviewProvider<TestPreviewProvider>()
                        .AddSenseNetTaskManager<TestTaskManager>();
                },
                () =>
                {
                    var contentFolder = Node.LoadNode("/Root/Content");

                    var docLib = Content.CreateNew("DocumentLibrary", contentFolder, "DocLib").ContentHandler;
                    docLib.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                    var folder1 = new Folder(docLib) {Name = "Folder1"};
                    folder1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                    var folder2 = new Folder(folder1) { Name = "Folder2" };
                    folder2.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                    var file = new File(folder2) { Name = RepositoryTools.GetRandomString(8) + ".txt" };
                    file.Binary.SetStream(RepositoryTools.GetStreamFromString("Lorem ipsum..."));
                    file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                    bool IsPreviewStarted(PreviewEnabled level1, PreviewEnabled level2)
                    {
                        folder1.PreviewEnabled = level1; folder1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                        folder2.PreviewEnabled = level2; folder2.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                        DocumentPreviewProvider.StartPreviewGeneration(file);
                        System.Threading.Tasks.Task.Delay(1).Wait();

                        var taskMan = (TestTaskManager)Providers.Instance.Services.GetRequiredService<ITaskManager>();
                        var req = taskMan.RegisterTask_requestData;
                        taskMan.RegisterTask_requestData = null;
                        return req != null;
                    }

                    // Actions and asserts
                    Assert.AreEqual(true, IsPreviewStarted(PreviewEnabled.Inherited, PreviewEnabled.Inherited));
                    Assert.AreEqual(false, IsPreviewStarted(PreviewEnabled.Inherited, PreviewEnabled.No));
                    Assert.AreEqual(true, IsPreviewStarted(PreviewEnabled.Inherited, PreviewEnabled.Yes));

                    Assert.AreEqual(false, IsPreviewStarted(PreviewEnabled.No, PreviewEnabled.Inherited));
                    Assert.AreEqual(false, IsPreviewStarted(PreviewEnabled.No, PreviewEnabled.No));
                    Assert.AreEqual(true, IsPreviewStarted(PreviewEnabled.No, PreviewEnabled.Yes));

                    Assert.AreEqual(true, IsPreviewStarted(PreviewEnabled.Yes, PreviewEnabled.Inherited));
                    Assert.AreEqual(false, IsPreviewStarted(PreviewEnabled.Yes, PreviewEnabled.No));
                    Assert.AreEqual(true, IsPreviewStarted(PreviewEnabled.Yes, PreviewEnabled.Yes));

                    //var taskData = JsonConvert.DeserializeObject<Dictionary<string, object>>(taskMan.RegisterTask_requestData.TaskData);
                    //Assert.AreEqual(file.Path, taskData["ContextPath"]);
                });
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class TestTaskManager : ITaskManager
        {
            // ReSharper disable once InconsistentNaming
            public RegisterTaskRequest RegisterTask_requestData { get; set; }
            public Task<RegisterTaskResult> RegisterTaskAsync(RegisterTaskRequest requestData, CancellationToken cancellationToken)
            {
                RegisterTask_requestData = requestData;
                return System.Threading.Tasks.Task.FromResult(new RegisterTaskResult());
            }
            public Task<bool> RegisterApplicationAsync(CancellationToken cancellationToken)
            {
                return System.Threading.Tasks.Task.FromResult(true);
            }
            public System.Threading.Tasks.Task OnTaskFinishedAsync(SnTaskResult result, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class TestPreviewProvider : DocumentPreviewProvider
        {
            public TestPreviewProvider(IOptions<TaskManagementOptions> taskManagementOptions, ITaskManager taskManager) : base(taskManagementOptions, taskManager)
            {
            }

            public override bool IsContentSupported(Node content)
            {
                if (content is File file)
                    return file.ContentListId > 0/* && file.IsPreviewEnabled*/;
                return false;
            }

            public override string GetPreviewGeneratorTaskName(string contentPath)
            {
                return "TestTaskName1";
            }

            public override string GetPreviewGeneratorTaskTitle(string contentPath)
            {
                return "TestTaskTitle1";
            }

            public override string[] GetSupportedTaskNames()
            {
                throw new NotImplementedException();
            }
        }


        [TestMethod]
        public async STT.Task Preview_Switch_SeeOnlyAndInvisible()
        {
            await Test(async () =>
            {
                var testRoot = Content.CreateNew(nameof(SystemFolder), Repository.Root, "TestRoot");
                await testRoot.SaveAsync(CancellationToken.None).ConfigureAwait(false);

                var genericContent1 = Content.CreateNew(nameof(GenericContent), testRoot.ContentHandler, "GenericContent1");
                ((GenericContent)genericContent1.ContentHandler).AllowChildType(nameof(Folder));
                ((GenericContent)genericContent1.ContentHandler).AllowChildType(nameof(File));
                await genericContent1.SaveAsync(CancellationToken.None).ConfigureAwait(false);

                var folder1 = Content.CreateNew(nameof(Folder), genericContent1.ContentHandler, "Folder1");
                await folder1.SaveAsync(CancellationToken.None).ConfigureAwait(false);

                var file1 = Content.CreateNew(nameof(File), folder1.ContentHandler, "File1");
                await file1.SaveAsync(CancellationToken.None).ConfigureAwait(false);

                // TEST-1: Getting IsPreviewEnabled of the file1 cause InvalidOperationException id the permission is only See.
                await Providers.Instance.SecurityHandler.CreateAclEditor()
                    .Allow(file1.Id, User.PublicAdministrator.Id, false, PermissionType.See)
                    .ApplyAsync(CancellationToken.None);
                using (new CurrentUserBlock(User.PublicAdministrator))
                {
                    var node = Node.LoadNode(file1.Id); // reload to right node.IsHeadOnly value

                    Assert.IsTrue(Repository.Root.Security.HasPermission(PermissionType.See));
                    Assert.IsFalse(testRoot.Security.HasPermission(PermissionType.See));
                    Assert.IsFalse(genericContent1.Security.HasPermission(PermissionType.See));
                    Assert.IsFalse(folder1.Security.HasPermission(PermissionType.See));
                    Assert.IsTrue(file1.Security.HasPermission(PermissionType.See));

                    try
                    {
                        Assert.IsTrue(node.IsPreviewEnabled);
                        Assert.Fail("The expected InvalidOperationException was not thrown.");
                    }
                    catch (InvalidOperationException e)
                    {
                        Assert.IsTrue(e.Message.Contains("Invalid property access attempt on a See-only node"));
                    }
                }

                // TEST-2: Getting IsPreviewEnabled of the file1 walks to /Root on the ancestor axis but does not throw any exception.
                // (/Root has local only see permission for everyone)
                // Root / TestRoot / GenericContent1 / Folder1 / File1
                // See    nothing    nothing           nothing   Open
                await Providers.Instance.SecurityHandler.CreateAclEditor()
                    .Allow(file1.Id, User.PublicAdministrator.Id, false, PermissionType.Open)
                    .ApplyAsync(CancellationToken.None);
                using (new CurrentUserBlock(User.PublicAdministrator))
                {
                    var node = Node.LoadNode(file1.Id);

                    Assert.IsTrue(Repository.Root.Security.HasPermission(PermissionType.See));
                    Assert.IsFalse(testRoot.Security.HasPermission(PermissionType.See));
                    Assert.IsFalse(genericContent1.Security.HasPermission(PermissionType.See));
                    Assert.IsFalse(folder1.Security.HasPermission(PermissionType.See));
                    Assert.IsTrue(file1.Security.HasPermission(PermissionType.Open));

                    Assert.IsTrue(node.IsPreviewEnabled);
                }

                // TEST-2: Same as test-1 but getting IsPreviewEnabled on a folder
                await Providers.Instance.SecurityHandler.CreateAclEditor()
                    .Allow(folder1.Id, User.PublicAdministrator.Id, false, PermissionType.Open)
                    .ApplyAsync(CancellationToken.None);
                using (new CurrentUserBlock(User.PublicAdministrator))
                {
                    var node = Node.LoadNode(folder1.Id);

                    Assert.IsTrue(Repository.Root.Security.HasPermission(PermissionType.See));
                    Assert.IsFalse(testRoot.Security.HasPermission(PermissionType.See));
                    Assert.IsFalse(genericContent1.Security.HasPermission(PermissionType.See));
                    Assert.IsTrue(folder1.Security.HasPermission(PermissionType.Open));

                    Assert.IsTrue(node.IsPreviewEnabled);
                }

            }).ConfigureAwait(false);
        }

    }
}
