using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Services.Core.Authentication;
using SenseNet.Services.Core.Virtualization;
using SenseNet.Storage.DataModel.Usage;
using SenseNet.Tests.Core;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class StatisticsTests : TestBase
    {
        private class TestStatisticalDataCollector:IStatisticalDataCollector
        {
            public List<object> StatData { get; } = new List<object>();

            public System.Threading.Tasks.Task RegisterWebTransfer(WebTransferStatInput data)
            {
                StatData.Add(data);
                return STT.Task.CompletedTask;
            }
            public System.Threading.Tasks.Task RegisterWebHook(WebHookStatInput data)
            {
                throw new NotImplementedException();
            }
            public System.Threading.Tasks.Task RegisterDatabaseUsage(DatabaseUsage data)
            {
                throw new NotImplementedException();
            }
            public System.Threading.Tasks.Task RegisterGeneralData(GeneralStatInput data)
            {
                throw new NotImplementedException();
            }
        }
        [TestMethod]
        public async STT.Task Stat_BinaryMiddleware()
        {
            await Test(async () =>
            {
                var serviceProvider = new ServiceCollection()
                    .AddStatisticalDataCollector<TestStatisticalDataCollector>()
                    .BuildServiceProvider();

                var node = ContentType.GetByName("File");

                var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
                // $"/binaryhandler.ashx?nodeid={testUser.Id}&propertyname=ImageData"
                var path = "/binaryhandler.ashx";
                var qstr = $"?nodeid={node.Id}&propertyname=Binary";
                var url = path + qstr;
                var request = httpContext.Request;
                request.Method = "GET";
                request.Path = path;
                request.QueryString = new QueryString(qstr);

                // ACTION
                var middleware = new BinaryMiddleware(null);
                await middleware.InvokeAsync(httpContext).ConfigureAwait(false);
                await STT.Task.Delay(1);

                // ASSERT
                var collector = (TestStatisticalDataCollector)serviceProvider.GetService<IStatisticalDataCollector>();
                Assert.AreEqual(1, collector.StatData.Count);
                var data = (WebTransferStatInput) collector.StatData[0];
                Assert.AreEqual(url, data.Url);
                Assert.AreEqual(url.Length, data.RequestLength);
                Assert.AreEqual(node.Binary.GetStream().Length, data.ResponseLength);
                Assert.AreEqual(200, data.ResponseStatusCode);
                Assert.IsTrue(data.RequestTime <= data.ResponseTime);
            }).ConfigureAwait(false);
        }
    }
}
