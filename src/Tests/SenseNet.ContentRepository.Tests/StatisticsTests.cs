using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.OData;
using SenseNet.Services.Core.Authentication;
using SenseNet.Services.Core.Virtualization;
using SenseNet.Services.Wopi;
using SenseNet.Storage.DataModel.Usage;
using SenseNet.Tests.Core;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class StatisticsTests : TestBase
    {
        private class TestStatisticalDataCollector : IStatisticalDataCollector
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
                Assert.AreEqual(200, data.ResponseStatusCode);
                Assert.AreEqual(node.Binary.GetStream().Length, data.ResponseLength);
                Assert.IsTrue(data.RequestTime <= data.ResponseTime);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task Stat_WopiMiddleware()
        {
            await Test(async () =>
            {
                var serviceProvider = new ServiceCollection()
                    .AddStatisticalDataCollector<TestStatisticalDataCollector>()
                    .BuildServiceProvider();

                var root = new SystemFolder(Repository.Root) {Name = "TestRoot"};
                root.Save();
                var file = new File(root) {Name = "TestFile"};
                file.Binary.SetStream(RepositoryTools.GetStreamFromString(new string('-', 142)));
                file.Save();

                var token = await AccessTokenVault.GetOrAddTokenAsync(1, TimeSpan.FromDays(1), file.Id,
                    WopiMiddleware.AccessTokenFeatureName, CancellationToken.None).ConfigureAwait(false);
                //await AccessTokenVault.DeleteTokenAsync(token.Value, CancellationToken.None).ConfigureAwait(false);

                var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
                var path = $"/wopi/files/{file.Id}/contents";
                var qstr = $"?access_token={token.Value}";
                var url = path + qstr;
                var request = httpContext.Request;
                request.Method = "GET";
                request.Path = path;
                request.QueryString = new QueryString(qstr);

                // ACTION
                var middleware = new WopiMiddleware(null);
                await middleware.InvokeAsync(httpContext).ConfigureAwait(false);
                await STT.Task.Delay(1);

                // ASSERT
                var collector = (TestStatisticalDataCollector)serviceProvider.GetService<IStatisticalDataCollector>();
                Assert.AreEqual(1, collector.StatData.Count);
                var data = (WebTransferStatInput)collector.StatData[0];
                Assert.AreEqual(url, data.Url);
                Assert.AreEqual(url.Length, data.RequestLength);
                Assert.AreEqual(200, data.ResponseStatusCode);
                Assert.AreEqual(file.Binary.GetStream().Length, data.ResponseLength);
                Assert.IsTrue(data.RequestTime <= data.ResponseTime);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task Stat_OdataMiddleware_ServiceDocument()
        {
            await Test(builder =>
            {
                builder.UseResponseLimiter();
            }, async () =>
            {
                var serviceProvider = new ServiceCollection()
                    .AddStatisticalDataCollector<TestStatisticalDataCollector>()
                    .BuildServiceProvider();

                var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
                httpContext.Response.Body = new MemoryStream();
                var path = $"/OData.svc";
                var url = path;
                var request = httpContext.Request;
                request.Method = "GET";
                request.Path = path;
                request.Host = new HostString("host");

                // ACTION
                var middleware = new ODataMiddleware(null, null, null);
                await middleware.InvokeAsync(httpContext).ConfigureAwait(false);
                await STT.Task.Delay(1);

                // ASSERT
                var responseOutput = httpContext.Response.Body;
                responseOutput.Seek(0, SeekOrigin.Begin);
                string output;
                using (var reader = new StreamReader(responseOutput))
                    // {"d":{"EntitySets":["Root"]}}
                    output = await reader.ReadToEndAsync().ConfigureAwait(false);
                var expectedLength = output.Length;

                var collector = (TestStatisticalDataCollector)serviceProvider.GetService<IStatisticalDataCollector>();
                Assert.AreEqual(1, collector.StatData.Count);
                var data = (WebTransferStatInput)collector.StatData[0];
                Assert.AreEqual(url, data.Url);
                Assert.AreEqual(url.Length, data.RequestLength);
                Assert.AreEqual(200, data.ResponseStatusCode);
                Assert.AreEqual(expectedLength, data.ResponseLength);
                Assert.IsTrue(data.RequestTime <= data.ResponseTime);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task Stat_OdataMiddleware_Collection()
        {
            await Test(builder =>
            {
                builder.UseResponseLimiter();
            }, async () =>
            {
                var serviceProvider = new ServiceCollection()
                    .AddStatisticalDataCollector<TestStatisticalDataCollector>()
                    .BuildServiceProvider();

                var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
                httpContext.Response.Body = new MemoryStream();
                var path = $"/OData.svc/Root/System/Schema/ContentTypes/GenericContent";
                var qstr = $"?metadata=no&$select=Id,Name,Type";
                var url = path + qstr;
                var request = httpContext.Request;
                request.Method = "GET";
                request.Path = path;
                request.Host = new HostString("host");
                request.QueryString = new QueryString(qstr);

                // ACTION
                var middleware = new ODataMiddleware(null, null, null);
                await middleware.InvokeAsync(httpContext).ConfigureAwait(false);
                await STT.Task.Delay(1);

                // ASSERT
                var responseOutput = httpContext.Response.Body;
                responseOutput.Seek(0, SeekOrigin.Begin);
                string output;
                using (var reader = new StreamReader(responseOutput))
                    // {"d": {"__count": 9,"results": [{"Id": 1250,"Name": "Application","Type": "ContentType"}, {"Id": .....
                    output = await reader.ReadToEndAsync().ConfigureAwait(false);
                var expectedLength = output.Length;

                var collector = (TestStatisticalDataCollector)serviceProvider.GetService<IStatisticalDataCollector>();
                Assert.AreEqual(1, collector.StatData.Count);
                var data = (WebTransferStatInput)collector.StatData[0];
                Assert.AreEqual(url, data.Url);
                Assert.AreEqual(url.Length, data.RequestLength);
                Assert.AreEqual(200, data.ResponseStatusCode);
                Assert.AreEqual(expectedLength, data.ResponseLength);
                Assert.IsTrue(data.RequestTime <= data.ResponseTime);
            }).ConfigureAwait(false);
        }
    }
}
