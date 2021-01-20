using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.WebHooks.Common;

namespace SenseNet.WebHooks.Tests
{
    [TestClass]
    public class HttpWebHookClientTests
    {
        //[TestMethod]
        public async Task Send_Get()
        {
            var services = new ServiceCollection();

            services.AddLogging()
                .AddSenseNetWebHookClient<HttpWebHookClient>();

            var provider = services.BuildServiceProvider();
            var whc = provider.GetRequiredService<IWebHookClient>();

            await whc.SendAsync("https://localhost:44362", "get");
        }
        //[TestMethod]
        public async Task Send_Post()
        {
            var services = new ServiceCollection();

            services.AddLogging()
                .AddSenseNetWebHookClient<HttpWebHookClient>();

            var provider = services.BuildServiceProvider();
            var whc = provider.GetRequiredService<IWebHookClient>();

            await whc.SendAsync("https://localhost:44362/odata.svc/('Root')/WebHookTest", 
                "post",
                new { p1 = "aaa"},
                new Dictionary<string, string>
                {
                    { "sn-h-k1", "v1" },
                    { "sn-h-k2", "v2" }
                });
        }
    }
}
