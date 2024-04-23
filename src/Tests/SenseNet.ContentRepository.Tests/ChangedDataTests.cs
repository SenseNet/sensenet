using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Tests.Core;
using System.Threading;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Storage;
using SenseNet.Testing;
using STT = System.Threading.Tasks;
using System.Globalization;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.ContentRepository.Tests;

[TestClass]
public class ChangedDataTests : TestBase
{
    private readonly CancellationToken _cancel = new CancellationTokenSource().Token;

    [TestMethod]
    public async STT.Task ChangedData_AggregatedChangedData()
    {
        await Test(async () =>
        {
            var content = Content.Create(await CreateTestRoot());
            ChangedData[] changedData = null;

            // ACTION
            await content.CheckOutAsync(_cancel);
            content.Index++;
            await content.SaveAsync(_cancel);
            content.Index++;
            await content.SaveAsync(_cancel);
            content.ContentHandler.Modified += (sender, args) =>
            {
                changedData = args.ChangedData.ToArray();
            };
            await content.CheckInAsync(_cancel);

            // ASSERT
            Assert.IsNotNull(changedData);
            Assert.IsTrue(changedData.Length > 0);
            Assert.IsTrue(changedData.Any(x => x.Name == "Index"));

        }).ConfigureAwait(false);
    }

    private class ChangedDataTestsObserver : NodeObserver
    {
        public List<string> Log { get; } = new List<string>();

        protected internal override void OnNodeCreated(object sender, NodeEventArgs e)
        {
            Log.Add($"Created");
        }

        protected internal override void OnNodeModified(object sender, NodeEventArgs e)
        {
            Log.Add($"Modified {e.SourceNode.Version}, Index: {e.SourceNode.Index}");
        }
    }

    [TestMethod]
    public async STT.Task ChangedData_CheckOutTriggeredLockedNotTriggered()
    {
        await Test(builder =>
        {
            builder.EnableNodeObservers(typeof(ChangedDataTestsObserver));
        }, async () =>
        {

            var content = Content.Create(await CreateTestRoot());

            // ACTION
            await content.CheckOutAsync(_cancel);
            content.Index++;
            await content.SaveAsync(_cancel);
            content.Index++;
            await content.SaveAsync(_cancel);
            await content.CheckInAsync(_cancel);

            // ASSERT
            var observer = (ChangedDataTestsObserver)NodeObserver.GetInstance(typeof(ChangedDataTestsObserver));
            Assert.AreEqual(3, observer.Log.Count);
            Assert.AreEqual("Created", observer.Log[0]);
            Assert.AreEqual("Modified V2.0.L, Index: 0", observer.Log[1]);
            Assert.AreEqual("Modified V1.0.A, Index: 2", observer.Log[2]);

        }).ConfigureAwait(false);
    }

    private async STT.Task<SystemFolder> CreateTestRoot()
    {
        var node = new SystemFolder(Repository.Root) { Name = "_ChangedDataTests" };
        await node.SaveAsync(CancellationToken.None).ConfigureAwait(false);
        return node;
    }

}