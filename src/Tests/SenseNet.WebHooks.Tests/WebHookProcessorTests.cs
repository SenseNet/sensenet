using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Tests.Core;

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

                var node1 = await Node.LoadNodeAsync("/Root/Content", CancellationToken.None);
                var node2 = await Node.LoadNodeAsync("/Root/System", CancellationToken.None);

                await ep.ExecuteAsync(node1, "modify");
                await ep.ExecuteAsync(node2, "modify");
                
                Assert.AreEqual(1, whc.Requests.Count);

                object postData = whc.Requests[0].PostData;
                var postJson = JsonSerializer.Serialize(postData);
                var postObject = JsonSerializer.Deserialize<ExpandoObject>(postJson) as IDictionary<string, object>;

                Assert.AreEqual(node1.Id, ((JsonElement)postObject["nodeId"]).GetInt32());
                Assert.AreEqual(node1.Path, ((JsonElement)postObject["path"]).GetString());
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
