using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Events;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Tests.Core;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.WebHooks.Tests
{
    [TestClass]
    public class WebHookProcessorTests : TestBase
    {
        [TestMethod]
        public async Task WebHook_Basic()
        {
            await Test(
                builder => { builder.UseComponent(new WebHookComponent()); },
                async () =>
                {
                    var provider = BuildServiceProvider();
                    var ep = provider.GetRequiredService<IEventProcessor>();
                    var whc = (TestWebHookClient) provider.GetRequiredService<IWebHookClient>();

                    var parent1 = await Node.LoadNodeAsync("/Root/Content", CancellationToken.None);
                    var parent2 = await Node.LoadNodeAsync("/Root/System", CancellationToken.None);
                    var node1 = new Folder(parent1);
                    var node2 = new Folder(parent2);

                    // create mock events
                    var event1 = new NodeCreatedEvent(new TestNodeEventArgs(node1, NodeEvent.Created));
                    var event2 = new NodeCreatedEvent(new TestNodeEventArgs(node2, NodeEvent.Created));
                    var event3 = new NodeForcedDeletedEvent(new TestNodeEventArgs(node1, NodeEvent.DeletedPhysically));

                    // ACTION: fire mock events
                    await ep.ProcessEventAsync(event1, CancellationToken.None);
                    await ep.ProcessEventAsync(event2, CancellationToken.None);
                    await ep.ProcessEventAsync(event3, CancellationToken.None);

                    // test webhook client should contain the event log
                    Assert.AreEqual(2, whc.Requests.Count);

                    var request1 = whc.Requests[0];

                    Assert.AreEqual(node1.Id, request1.NodeId);
                    Assert.AreEqual(node1.Path, request1.Path);
                    Assert.AreEqual("Create", request1.EventName);

                    var request2 = whc.Requests[1];

                    Assert.AreEqual(node1.Id, request2.NodeId);
                    Assert.AreEqual(node1.Path, request2.Path);
                    Assert.AreEqual("Delete", request2.EventName);
                });
        }

        private IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            // add test services one by one
            services.AddLogging()
                .AddSenseNetWebHookClient<TestWebHookClient>()
                .AddSingleton<IEventProcessor, LocalWebHookProcessor>()
                .AddSingleton<IWebHookSubscriptionStore, TestWebHookSubscriptionStore>((s) =>
                    new TestWebHookSubscriptionStore(null));

            var provider = services.BuildServiceProvider();

            return provider;
        }
    }
}
