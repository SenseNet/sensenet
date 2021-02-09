using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
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
            await Test(async () =>
            {
                var provider = BuildServiceProvider();
                var ep = provider.GetRequiredService<IEventProcessor>();
                var whc = (TestWebHookClient)provider.GetRequiredService<IWebHookClient>();

                var parent1 = await Node.LoadNodeAsync("/Root/Content", CancellationToken.None);
                var parent2 = await Node.LoadNodeAsync("/Root/System", CancellationToken.None);
                var node1 = new Folder(parent1);
                var node2 = new Folder(parent2);

                // create mock events
                var event1 = new NodeCreatedEvent(new TestNodeEventArgs(node1));
                var event2 = new NodeCreatedEvent(new TestNodeEventArgs(node2));
                var event3 = new NodeForcedDeletedEvent(new TestNodeEventArgs(node1));

                // ACTION: fire mock events
                await ep.ProcessEventAsync(event1, CancellationToken.None);
                await ep.ProcessEventAsync(event2, CancellationToken.None);
                await ep.ProcessEventAsync(event3, CancellationToken.None);

                // test webhook client should contain the even log
                Assert.AreEqual(2, whc.Requests.Count);

                var postObject1 = GetPostObject(whc.Requests[0].PostData);

                Assert.AreEqual(node1.Id, ((JsonElement)postObject1["nodeId"]).GetInt32());
                Assert.AreEqual(node1.Path, ((JsonElement)postObject1["path"]).GetString());
                Assert.AreEqual("Create", ((JsonElement)postObject1["eventName"]).GetString());

                var postObject2 = GetPostObject(whc.Requests[1].PostData);

                Assert.AreEqual(node1.Id, ((JsonElement)postObject2["nodeId"]).GetInt32());
                Assert.AreEqual(node1.Path, ((JsonElement)postObject2["path"]).GetString());
                Assert.AreEqual("Delete", ((JsonElement)postObject2["eventName"]).GetString());
            });
        }

        private static IDictionary<string, object> GetPostObject(object postData)
        {
            var postJson = JsonSerializer.Serialize(postData);
            var postObject = JsonSerializer.Deserialize<ExpandoObject>(postJson) as IDictionary<string, object>;
            return postObject;
        }

        private IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            // add test services one by one
            services.AddLogging()
                .AddSenseNetWebHookClient<TestWebHookClient>()
                .AddSingleton<IEventProcessor, LocalWebHookProcessor>()
                .AddSingleton<IWebHookSubscriptionStore, TestWebHookSubscriptionStore>();

            var provider = services.BuildServiceProvider();

            return provider;
        }
    }
}
