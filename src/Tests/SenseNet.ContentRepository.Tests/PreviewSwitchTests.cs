using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Preview;
using SenseNet.TaskManagement.Core;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class PreviewSwitchTests : TestBase
    {
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

        private class TestTaskManager : ITaskManager
        {
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

        private class FileOperation : IDisposable
        {
            public File TheFile { get; }

            public FileOperation(string fileName = null)
            {

                var fileContainer = Node.LoadNode("/Root/Content/TestFiles/Folder1");
                if (fileContainer == null)
                {
                    var containerContent = Content.CreateNew("DocumentLibrary", Node.LoadNode("/Root/Content"), "TestFiles");
                    containerContent.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
                    fileContainer = containerContent.ContentHandler;
                }

                TheFile = new File(fileContainer) { Name = fileName ?? RepositoryTools.GetRandomString(8) + ".txt" };
                TheFile.Binary.SetStream(RepositoryTools.GetStreamFromString("Lorem ipsum..."));
                TheFile.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            }

            public void Dispose()
            {
                TheFile.ForceDeleteAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
        }

    }
}
