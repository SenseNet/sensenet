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
        public async Task WebHook_Modify()
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

                var event1 = new TestEvent1(new TestNodeEventArgs(node1));
                var event2 = new TestEvent1(new TestNodeEventArgs(node2));
                
                await ep.ProcessEventAsync(event1, CancellationToken.None);
                await ep.ProcessEventAsync(event2, CancellationToken.None);

                Assert.AreEqual(1, whc.Requests.Count);

                var postData = whc.Requests[0].PostData;
                var postJson = JsonSerializer.Serialize(postData);
                var postObject = JsonSerializer.Deserialize<ExpandoObject>(postJson) as IDictionary<string, object>;

                Assert.AreEqual(node1.Id, ((JsonElement)postObject["nodeId"]).GetInt32());
                Assert.AreEqual(node1.Path, ((JsonElement)postObject["path"]).GetString());
                Assert.AreEqual("TestEvent1", ((JsonElement)postObject["eventName"]).GetString());
            });
        }

        private IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            // add test services one by one
            services.AddLogging()
                .AddSenseNetWebHookClient<TestWebHookClient>()
                .AddSingleton<IEventProcessor, LocalWebHookProcessor>()
                .AddSingleton<IWebHookFilter, TestWebHookFilter>();

            var provider = services.BuildServiceProvider();

            return provider;
        }
    }
}
