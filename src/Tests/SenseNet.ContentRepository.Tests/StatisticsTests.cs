using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Events;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.OData;
using SenseNet.Services.Core.Authentication;
using SenseNet.Services.Core.Virtualization;
using SenseNet.Services.Wopi;
using SenseNet.Storage.DataModel.Usage;
using SenseNet.Tests.Core;
using SenseNet.WebHooks;
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
                StatData.Add(data);
                return STT.Task.CompletedTask;
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

        [TestMethod]
        public async STT.Task Stat_Webhook()
        {
            await Test(
                builder => { builder.UseComponent(new WebHookComponent()); },
                async () =>
                {
                    var provider = BuildServiceProvider();
                    var ep = provider.GetRequiredService<IEventProcessor>();
                    //var whc = (HttpWebHookClient)provider.GetRequiredService<IWebHookClient>();

                    var parent1 = await Node.LoadNodeAsync("/Root/Content", CancellationToken.None);
                    var parent2 = await Node.LoadNodeAsync("/Root/System", CancellationToken.None);
                    var node1 = new Folder(parent1);
                    var node2 = new Folder(parent2);
                    node1.Save();
                    node2.Save();

                    // create mock events (nodes are not saved)
                    var event1 = new NodeCreatedEvent(new TestNodeEventArgs(node1, NodeEvent.Created));
                    var event2 = new NodeCreatedEvent(new TestNodeEventArgs(node2, NodeEvent.Created));
                    var event3 = new NodeForcedDeletedEvent(new TestNodeEventArgs(node1, NodeEvent.DeletedPhysically));

                    // ACTION: fire mock events
                    var now = DateTime.UtcNow;
                    await ep.ProcessEventAsync(event1, CancellationToken.None);
                    await ep.ProcessEventAsync(event2, CancellationToken.None);
                    await ep.ProcessEventAsync(event3, CancellationToken.None);

                    // ASSERT
                    var collector = (TestStatisticalDataCollector)provider.GetService<IStatisticalDataCollector>();
                    var data = collector.StatData.Cast<WebHookStatInput>().ToArray();
                    Assert.AreEqual(2, data.Length);

                    Assert.AreEqual("https://example.com", data[0].Url);
                    Assert.AreEqual(data[0].Url, data[1].Url);

                    Assert.AreEqual(200, data[0].ResponseStatusCode);
                    Assert.AreEqual(200, data[1].ResponseStatusCode);

                    Assert.AreEqual(99942, data[0].WebHookId);
                    Assert.AreEqual(99942, data[1].WebHookId);

                    Assert.AreEqual(node1.Id, data[0].ContentId);
                    Assert.AreEqual(node1.Id, data[1].ContentId);

                    Assert.IsNull(data[0].ErrorMessage);
                    Assert.IsNull(data[1].ErrorMessage);

                    Assert.IsTrue(data[0].RequestTime > now.AddSeconds(-10));
                    Assert.IsTrue(data[0].ResponseTime > data[0].RequestTime);

                    Assert.IsTrue(data[1].RequestTime > now.AddSeconds(-10));
                    Assert.IsTrue(data[1].ResponseTime > data[1].RequestTime);

                    Assert.AreEqual("Create", data[0].EventName);
                    Assert.AreEqual("Delete", data[1].EventName);
                });
        }
        private IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            // add test services one by one
            services.AddLogging()
                .AddSenseNetWebHookClient<HttpWebHookClient>()
                .AddStatisticalDataCollector<TestStatisticalDataCollector>()
                .AddSingleton<IEventProcessor, LocalWebHookProcessor>()
                .AddSingleton<IWebHookSubscriptionStore, TestWebHookSubscriptionStore>((s) =>
                    new TestWebHookSubscriptionStore(null))
                .AddSingleton<IHttpClientFactory, TestHttpClientFactory>();

            var provider = services.BuildServiceProvider();

            return provider;
        }

        #region Additional classes for WebHook test.
        private class TestEvent1 : ISnEvent
        {
            public INodeEventArgs NodeEventArgs { get; set; }

            public TestEvent1(INodeEventArgs e)
            {
                NodeEventArgs = e;
            }
        }

        private class TestNodeEventArgs : NodeEventArgs
        {
            public TestNodeEventArgs(Node node, NodeEvent eventType) : base(node, eventType, null) { }
        }

        private class TestHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name)
            {
                return new HttpClient(new TestHttpClientHandler());
            }
        }

        private class TestHttpClientHandler : HttpClientHandler
        {
            protected override async STT.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, 
                CancellationToken cancellationToken)
            {
                await STT.Task.Delay(10, cancellationToken);
                return new HttpResponseMessage
                {
                    Content = new StringContent("Ok"),
                    StatusCode = HttpStatusCode.OK,
                };
            }
        }

        private class TestWebHookSubscriptionStore : IWebHookSubscriptionStore
        {
            public TestWebHookSubscriptionStore(IEnumerable<WebHookSubscription> subscriptions = null)
            {
                Subscriptions = subscriptions?.ToList() ?? CreateDefaultSubscriptions();
            }
            /// <summary>
            /// Hardcoded subscription for items in the /Root/Content subtree.
            /// </summary>
            public List<WebHookSubscription> Subscriptions { get; }

            private List<WebHookSubscription> CreateDefaultSubscriptions()
            {
                var subscriptions = new List<WebHookSubscription>(new[]
                {
                    new WebHookSubscription(Repository.Root)
                    {
                        FilterQuery = "+InTree:/Root/Content",
                        FilterData = new WebHookFilterData
                        {
                            Path = "/Root/Content",
                            ContentTypes = new[]
                            {
                                new ContentTypeFilterData
                                {
                                    Name = "Folder",
                                    Events = new[]
                                    {
                                        WebHookEventType.Create,
                                        WebHookEventType.Delete
                                    }
                                },
                            }
                        },
                        Url = "https://example.com",
                        Enabled = true
                    }
                });

                subscriptions[0].Data.Id = 99942;

                return subscriptions;
            }

            public IEnumerable<WebHookSubscriptionInfo> GetRelevantSubscriptions(ISnEvent snEvent)
            {
                var node = snEvent.NodeEventArgs.SourceNode;
                if (node == null)
                    return Array.Empty<WebHookSubscriptionInfo>();

                var pe = new PredicationEngine(Content.Create(node));

                // filter the hardcoded subscription list: return the ones that
                // match the current content
                return Subscriptions.SelectMany(sub =>
                {
                    var eventTypes = sub.GetRelevantEventTypes(snEvent);

                    // handle multiple relevant event types by adding the subscription multiple times
                    return eventTypes.Select(et => new WebHookSubscriptionInfo(sub, et));
                }).Where(si => si != null &&
                               pe.IsTrue(si.Subscription.FilterQuery) &&
                               si.Subscription.Enabled &&
                               si.Subscription.IsValid);
            }
        }
        #endregion
    }
}
