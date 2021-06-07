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
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SenseNet.BackgroundOperations;
using SenseNet.Configuration;
using SenseNet.ContentRepository.InMemory;
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
using SenseNet.Services.Core.Diagnostics;
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
        #region /* ========================================================================= Collecting tests */

        [TestMethod]
        public async STT.Task Stat_Collecting_BinaryMiddleware()
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
                request.Scheme = "https://";
                request.Host = new HostString("localhost", 8080);
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
                Assert.AreEqual(path, data.Url);
                Assert.AreEqual("GET", data.HttpMethod);
                Assert.AreEqual(path.Length, data.RequestLength);
                Assert.AreEqual(200, data.ResponseStatusCode);
                Assert.AreEqual(node.Binary.GetStream().Length, data.ResponseLength);
                Assert.IsTrue(data.RequestTime <= data.ResponseTime);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task Stat_Collecting_WopiMiddleware()
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
                request.Scheme = "https://";
                request.Host = new HostString("localhost", 8080);
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
                Assert.AreEqual(path, data.Url);
                Assert.AreEqual("GET", data.HttpMethod);
                Assert.AreEqual(path.Length, data.RequestLength);
                Assert.AreEqual(200, data.ResponseStatusCode);
                Assert.AreEqual(file.Binary.GetStream().Length, data.ResponseLength);
                Assert.IsTrue(data.RequestTime <= data.ResponseTime);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task Stat_Collecting_OdataMiddleware_ServiceDocument()
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
                request.Scheme = "https://";
                request.Host = new HostString("localhost", 8080);
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
                Assert.AreEqual(path, data.Url);
                Assert.AreEqual("GET", data.HttpMethod);
                Assert.AreEqual(url.Length, data.RequestLength);
                Assert.AreEqual(200, data.ResponseStatusCode);
                Assert.AreEqual(expectedLength, data.ResponseLength);
                Assert.IsTrue(data.RequestTime <= data.ResponseTime);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task Stat_Collecting_OdataMiddleware_Collection()
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
                request.Scheme = "https://";
                request.Host = new HostString("localhost", 8080);
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
                Assert.AreEqual(path, data.Url);
                Assert.AreEqual("GET", data.HttpMethod);
                Assert.AreEqual(path.Length, data.RequestLength);
                Assert.AreEqual(200, data.ResponseStatusCode);
                Assert.AreEqual(expectedLength, data.ResponseLength);
                Assert.IsTrue(data.RequestTime <= data.ResponseTime);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async STT.Task Stat_Collecting_WebHook()
        {
            await Test(
                builder => { builder.UseComponent(new WebHookComponent()); },
                async () =>
                {
                    var provider = BuildServiceProvider_WebHook();
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

                    Assert.AreEqual("POST", data[0].HttpMethod);
                    Assert.AreEqual(data[0].HttpMethod, data[1].HttpMethod);

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
        private IServiceProvider BuildServiceProvider_WebHook()
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

        [TestMethod]
        public async STT.Task Stat_Collecting_DbUsage()
        {
            await Test(async () =>
            {
                var logger = new TestDbUsageLogger();
                var collector = new TestStatisticalDataCollector();
                var dbUsageHandler = new DatabaseUsageHandler(logger);
                var maintenanceTask = new StatisticalDataCollectorMaintenanceTask(collector, dbUsageHandler);

                // The first load creates a persistent cache and its container (2 system content) but caches the count without cache.
                var _ = await dbUsageHandler.GetDatabaseUsageAsync(true, CancellationToken.None)
                    .ConfigureAwait(false);
                // The second load can see the persistent cache.
                _ = await dbUsageHandler.GetDatabaseUsageAsync(true, CancellationToken.None)
                    .ConfigureAwait(false);

                // ACTION
                await maintenanceTask.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                // ASSERT
                var expected = await dbUsageHandler.GetDatabaseUsageAsync(true, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(1, collector.StatData.Count);
                var data = collector.StatData[0] as GeneralStatInput;
                Assert.IsNotNull(data);
                Assert.AreEqual("DatabaseUsage", data.DataType);
                var dbUsage = data.Data as DatabaseUsage;
                Assert.IsNotNull(dbUsage);
                Assert.AreEqual(expected.Content.Count, dbUsage.Content.Count);
                Assert.AreEqual(expected.System.Count, dbUsage.System.Count);
            }).ConfigureAwait(false);

        }

        #region Additional classes for "collecting" tests

        private class TestStatisticalDataCollector : IStatisticalDataCollector
        {
            public List<object> StatData { get; } = new List<object>();

            public System.Threading.Tasks.Task RegisterWebTransfer(WebTransferStatInput data, CancellationToken cancel)
            {
                StatData.Add(data);
                return STT.Task.CompletedTask;
            }
            public System.Threading.Tasks.Task RegisterWebHook(WebHookStatInput data, CancellationToken cancel)
            {
                StatData.Add(data);
                return STT.Task.CompletedTask;
            }
            public System.Threading.Tasks.Task RegisterGeneralData(GeneralStatInput data, CancellationToken cancel)
            {
                StatData.Add(data);
                return STT.Task.CompletedTask;
            }
        }

        private class TestEvent1 : ISnEvent
        {
            public INodeEventArgs NodeEventArgs { get; }

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
        #endregion

        #region /* ========================================================================= StatisticalDataCollector tests */

        [TestMethod]
        public async STT.Task Stat_Collector_CollectWebTransfer()
        {
            var sdp = new TestStatisticalDataProvider();
            var collector = new StatisticalDataCollector(sdp);
            var time1 = DateTime.UtcNow.AddDays(-1);
            var time2 = time1.AddSeconds(1);
            var input = new WebTransferStatInput
            {
                Url = "Url1",
                HttpMethod = "GET",
                RequestTime = time1,
                ResponseTime = time2,
                RequestLength = 42,
                ResponseLength = 4242,
                ResponseStatusCode = 200,
            };

            // ACTION
#pragma warning disable 4014
            collector.RegisterWebTransfer(input, CancellationToken.None);
#pragma warning restore 4014

            // ASSERT
            await STT.Task.Delay(1);
            Assert.AreEqual(1, sdp.Storage.Count);
            var record = sdp.Storage[0];
            Assert.AreEqual("WebTransfer", record.DataType);
            Assert.AreEqual("GET Url1", record.Url);
            Assert.AreEqual(time1, record.RequestTime);
            Assert.AreEqual(time2, record.ResponseTime);
            Assert.AreEqual(42, record.RequestLength);
            Assert.AreEqual(4242, record.ResponseLength);
            Assert.AreEqual(200, record.ResponseStatusCode);
            Assert.IsNull(record.WebHookId);
            Assert.IsNull(record.ContentId);
            Assert.IsNull(record.EventName);
            Assert.IsNull(record.ErrorMessage);
        }
        [TestMethod]
        public async STT.Task Stat_Collector_CollectWebHook()
        {
            var sdp = new TestStatisticalDataProvider();
            var collector = new StatisticalDataCollector(sdp);
            var time1 = DateTime.UtcNow.AddDays(-1);
            var time2 = time1.AddSeconds(1);
            var input = new WebHookStatInput
            {
                Url = "Url1",
                HttpMethod = "POST",
                RequestTime = time1,
                ResponseTime = time2,
                RequestLength = 42,
                ResponseLength = 4242,
                ResponseStatusCode = 200,
                WebHookId = 1242,
                ContentId = 1342,
                EventName = "Event42",
                ErrorMessage = "ErrorMessage1"
            };

            // ACTION
#pragma warning disable 4014
            collector.RegisterWebHook(input, CancellationToken.None);
#pragma warning restore 4014

            // ASSERT
            await STT.Task.Delay(1);
            Assert.AreEqual(1, sdp.Storage.Count);
            var record = sdp.Storage[0];
            Assert.AreEqual("WebHook", record.DataType);
            Assert.AreEqual("POST Url1", record.Url);
            Assert.AreEqual(time1, record.RequestTime);
            Assert.AreEqual(time2, record.ResponseTime);
            Assert.AreEqual(42, record.RequestLength);
            Assert.AreEqual(4242, record.ResponseLength);
            Assert.AreEqual(200, record.ResponseStatusCode);
            Assert.AreEqual(1242, record.WebHookId);
            Assert.AreEqual(1342, record.ContentId);
            Assert.AreEqual("Event42", record.EventName);
            Assert.AreEqual("ErrorMessage1", record.ErrorMessage);
        }
        [TestMethod]
        public async STT.Task Stat_Collector_CollectDbUsage()
        {
            var sdp = new TestStatisticalDataProvider();
            var collector = new StatisticalDataCollector(sdp);
            var data = new { Name = "Name1", Value = 42 };
            var input = new GeneralStatInput { DataType = "DataType1", Data = data };

            // ACTION
#pragma warning disable 4014
            collector.RegisterGeneralData(input, CancellationToken.None);
#pragma warning restore 4014

            // ASSERT
            await STT.Task.Delay(1);
            Assert.AreEqual(1, sdp.Storage.Count);
            var record = sdp.Storage[0];
            Assert.AreEqual("DataType1", record.DataType);
            Assert.IsNull( record.RequestTime);
            Assert.IsNull( record.ResponseTime);
            Assert.IsNull( record.RequestLength);
            Assert.IsNull( record.ResponseLength);
            Assert.IsNull( record.ResponseStatusCode);
            Assert.IsNull( record.WebHookId);
            Assert.IsNull( record.ContentId);
            Assert.IsNull( record.EventName);
            Assert.IsNull( record.ErrorMessage);
            Assert.AreEqual("{\"Name\":\"Name1\",\"Value\":42}", RemoveWhitespaces(record.GeneralData));
        }

        private class TestStatisticalDataProvider : IStatisticalDataProvider
        {
            public List<IStatisticalDataRecord> Storage { get; } = new List<IStatisticalDataRecord>();
            public List<Aggregation> Aggregations { get; } = new List<Aggregation>();

            public STT.Task WriteDataAsync(IStatisticalDataRecord data, CancellationToken cancel)
            {
                Storage.Add(new StatisticalDataRecord
                {
                    Id = 0,
                    DataType = data.DataType,
                    WrittenTime = DateTime.UtcNow,
                    RequestTime = data.RequestTime,
                    ResponseTime = data.ResponseTime,
                    RequestLength = data.RequestLength,
                    ResponseLength = data.ResponseLength,
                    ResponseStatusCode = data.ResponseStatusCode,
                    Url = data.Url,
                    WebHookId = data.WebHookId,
                    ContentId = data.ContentId,
                    EventName = data.EventName,
                    ErrorMessage = data.ErrorMessage,
                    GeneralData = data.GeneralData
                });
                return STT.Task.CompletedTask;
            }

            public STT.Task CleanupAsync(DateTime timeMax, CancellationToken cancel)
            {
                throw new NotImplementedException();
            }
            public STT.Task LoadUsageListAsync(string dataType, DateTime startTime, TimeResolution resolution, CancellationToken cancel)
            {
                throw new NotImplementedException();
            }
            public STT.Task<IEnumerable<Aggregation>> LoadAggregatedUsageAsync(string dataType, TimeResolution resolution,
                DateTime startTime, DateTime endTimeExclusive, CancellationToken cancel)
            {
                var result = Aggregations.Where(x =>
                    x.DataType == dataType &&
                    x.Resolution == resolution &&
                    x.Date >= startTime &&
                    x.Date < endTimeExclusive).ToArray();
                return STT.Task.FromResult((IEnumerable<Aggregation>)result);
            }

            public STT.Task EnumerateDataAsync(string dataType, DateTime startTime, DateTime endTimeExclusive,
                TimeResolution resolution, Action<IStatisticalDataRecord> aggregatorCallback, CancellationToken cancel)
            {
                var result = new List<Aggregation>();

                var relatedItems = Storage
                    .Where(x =>
                    {
                        var requestTime = x.RequestTime ?? x.WrittenTime;
                        return (requestTime >= startTime && requestTime < endTimeExclusive);
                    });

                foreach (var item in relatedItems)
                {
                    cancel.ThrowIfCancellationRequested();
                    aggregatorCallback(item);
                }

                return STT.Task.CompletedTask;
            }

            public STT.Task WriteAggregationAsync(Aggregation aggregation, CancellationToken cancel)
            {
                Aggregations.Add(aggregation);
                return STT.Task.CompletedTask;
            }
        }
        #endregion

        #region /* ========================================================================= InputStatisticalDataRecord tests */

        [TestMethod]
        public void Stat_InputRecord_CreateFromGeneral()
        {
            var data = new {Name = "Name1", Value = 42};
            var input = new GeneralStatInput {DataType = "DataType1", Data = data};

            // ACTION
            var record = new InputStatisticalDataRecord(input);

            // ASSERT
            Assert.AreEqual("DataType1", record.DataType);
            Assert.AreEqual("{\"Name\":\"Name1\",\"Value\":42}", RemoveWhitespaces(record.GeneralData));

            Assert.AreEqual(0, record.Id);
            Assert.AreEqual(DateTime.MinValue, record.WrittenTime);

            Assert.IsNull(record.RequestTime);
            Assert.IsNull(record.ResponseTime);
            Assert.IsNull(record.RequestLength);
            Assert.IsNull(record.ResponseLength);
            Assert.IsNull(record.ResponseStatusCode);
            Assert.IsNull(record.Url);
            Assert.IsNull(record.WebHookId);
            Assert.IsNull(record.ContentId);
            Assert.IsNull(record.EventName);
            Assert.IsNull(record.ErrorMessage);
        }
        [TestMethod]
        public void Stat_InputRecord_CreateFromWebTransfer()
        {
            var time1 = DateTime.UtcNow.AddDays(-1);
            var time2 = time1.AddSeconds(1);
            var input = new WebTransferStatInput
            {
                Url = "Url1",
                HttpMethod = "GET",
                RequestTime = time1,
                ResponseTime = time2,
                RequestLength = 42,
                ResponseLength = 4242,
                ResponseStatusCode = 200
            };

            // ACTION
            var record = new InputStatisticalDataRecord(input);

            // ASSERT
            Assert.AreEqual("WebTransfer", record.DataType);
            Assert.AreEqual(0, record.GeneralData.Length);

            Assert.AreEqual(0, record.Id);
            Assert.AreEqual(DateTime.MinValue, record.WrittenTime);

            Assert.AreEqual(time1, record.RequestTime);
            Assert.AreEqual(time2, record.ResponseTime);
            Assert.AreEqual(42, record.RequestLength);
            Assert.AreEqual(4242, record.ResponseLength);
            Assert.AreEqual(200, record.ResponseStatusCode);
            Assert.AreEqual("GET Url1", record.Url);
            Assert.IsNull(record.WebHookId);
            Assert.IsNull(record.ContentId);
            Assert.IsNull(record.EventName);
            Assert.IsNull(record.ErrorMessage);
        }
        [TestMethod]
        public void Stat_InputRecord_CreateFromWebHook()
        {
            var time1 = DateTime.UtcNow.AddDays(-1);
            var time2 = time1.AddSeconds(1);
            var input = new WebHookStatInput
            {
                Url = "Url1",
                HttpMethod = "POST",
                RequestTime = time1,
                ResponseTime = time2,
                RequestLength = 42,
                ResponseLength = 4242,
                ResponseStatusCode = 200,
                WebHookId = 1242,
                ContentId = 1342,
                EventName = "Event42",
                ErrorMessage = "ErrorMessage1"
            };

            // ACTION
            var record = new InputStatisticalDataRecord(input);

            // ASSERT
            Assert.AreEqual("WebHook", record.DataType);
            Assert.AreEqual(0, record.GeneralData.Length);

            Assert.AreEqual(0, record.Id);
            Assert.AreEqual(DateTime.MinValue, record.WrittenTime);

            Assert.AreEqual(time1, record.RequestTime);
            Assert.AreEqual(time2, record.ResponseTime);
            Assert.AreEqual(42, record.RequestLength);
            Assert.AreEqual(4242, record.ResponseLength);
            Assert.AreEqual(200, record.ResponseStatusCode);
            Assert.AreEqual("POST Url1", record.Url);
            Assert.AreEqual(1242, record.WebHookId);
            Assert.AreEqual(1342, record.ContentId);
            Assert.AreEqual("Event42", record.EventName);
            Assert.AreEqual("ErrorMessage1", record.ErrorMessage);
        }
        #endregion

        #region /* ========================================================================= DateTime extensions */

        [TestMethod]
        public void Stat_DateTimeExt_Truncate()
        {
            Assert.AreEqual(D("2009-10-01 00:00:00"), D("2009-10-01 00:00:00").Truncate(TimeResolution.Minute));
            Assert.AreEqual(D("2009-10-01 00:00:00"), D("2009-10-01 00:00:00").Truncate(TimeResolution.Hour));
            Assert.AreEqual(D("2009-10-01 00:00:00"), D("2009-10-01 00:00:00").Truncate(TimeResolution.Day));
            Assert.AreEqual(D("2009-10-01 00:00:00"), D("2009-10-01 00:00:00").Truncate(TimeResolution.Month));

            Assert.AreEqual(D("2009-10-11 12:13:00"), D("2009-10-11 12:13:14").Truncate(TimeResolution.Minute));
            Assert.AreEqual(D("2009-10-11 12:00:00"), D("2009-10-11 12:13:14").Truncate(TimeResolution.Hour));
            Assert.AreEqual(D("2009-10-11 00:00:00"), D("2009-10-11 12:13:14").Truncate(TimeResolution.Day));
            Assert.AreEqual(D("2009-10-01 00:00:00"), D("2009-10-11 12:13:14").Truncate(TimeResolution.Month));
        }
        [TestMethod]
        public void Stat_DateTimeExt_Next()
        {
            Assert.AreEqual(D("2009-10-01 01:00:00"), D("2009-10-01 00:00:00").Next(TimeWindow.Hour));
            Assert.AreEqual(D("2009-10-02 00:00:00"), D("2009-10-01 00:00:00").Next(TimeWindow.Day));
            Assert.AreEqual(D("2009-11-01 00:00:00"), D("2009-10-01 00:00:00").Next(TimeWindow.Month));
            Assert.AreEqual(D("2010-01-01 00:00:00"), D("2009-10-01 00:00:00").Next(TimeWindow.Year));

            Assert.AreEqual(D("2009-10-11 13:00:00"), D("2009-10-11 12:13:14").Next(TimeWindow.Hour));
            Assert.AreEqual(D("2009-10-12 00:00:00"), D("2009-10-11 12:13:14").Next(TimeWindow.Day));
            Assert.AreEqual(D("2009-11-01 00:00:00"), D("2009-10-11 12:13:14").Next(TimeWindow.Month));
            Assert.AreEqual(D("2010-01-01 00:00:00"), D("2009-10-11 12:13:14").Next(TimeWindow.Year));

            Assert.AreEqual(D("2000-01-01 00:00:00"), D("1999-12-31 23:59:59").Next(TimeWindow.Hour));
            Assert.AreEqual(D("2000-01-01 00:00:00"), D("1999-12-31 23:59:59").Next(TimeWindow.Day));
            Assert.AreEqual(D("2000-01-01 00:00:00"), D("1999-12-31 23:59:59").Next(TimeWindow.Month));
            Assert.AreEqual(D("2000-01-01 00:00:00"), D("1999-12-31 23:59:59").Next(TimeWindow.Year));
        }
        private DateTime D(string src)
        {
            return DateTime.Parse(src);
        }
        #endregion

        #region /* ========================================================================= Aggregation tests */

        [TestMethod]
        public async STT.Task Stat_Aggregation_WebHook_RawTo1Minutely()
        {
            var statDataProvider = new TestStatisticalDataProvider();
            var aggregator = new WebHookStatisticalDataAggregator(statDataProvider);

            var now = DateTime.UtcNow;
            var record = new StatisticalDataRecord
            {
                DataType = "WebHook",
                WebHookId = 1242,
                ContentId = 1342,
                EventName = "Event42",
                Url = "POST https://example.com/api/hook",
                RequestLength = 100,
                ResponseLength = 1000,
            };
            var time1 = now.AddMinutes(-3);
            var time2 = time1.AddMilliseconds(100);
            var count = 0;

            var d = now.AddMinutes(-2);
            var expectedStart = new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0);
            var expectedEnd = expectedStart.AddMinutes(1);
            while (true)
            {
                record.RequestTime = time1;
                record.ResponseTime = time2;
                var error = (++count % 10) == 0;
                var warning = (count % 10) == 1;
                record.ResponseStatusCode = error ? 500 : (warning ? 400 : 200);
                record.ErrorMessage = error ? "ErrorMessage1" : (warning ? "WarningMessage" : null);

                await statDataProvider.WriteDataAsync(record, CancellationToken.None);

                time1 = time1.AddSeconds(1);
                time2 = time1.AddMilliseconds(100);
                if (time2 > now)
                    break;
            }

            // ACTION
            await aggregator.AggregateAsync(now.AddMinutes(-2), TimeResolution.Minute, CancellationToken.None);

            // ASSERT
            Assert.AreEqual(1, statDataProvider.Aggregations.Count);

            Aggregation aggregation = statDataProvider.Aggregations[0];
            Assert.AreEqual("WebHook", aggregation.DataType);
            Assert.AreEqual(expectedStart, aggregation.Date);
            Assert.AreEqual(TimeResolution.Minute, aggregation.Resolution);

            WebHookStatisticalDataAggregator.WebHookAggregation deserialized;
            using (var reader = new StringReader(aggregation.Data))
                deserialized = JsonSerializer.Create().Deserialize<WebHookStatisticalDataAggregator.WebHookAggregation>(new JsonTextReader(reader));
            Assert.AreEqual(60, deserialized.CallCount); // 60 * 6
            Assert.AreEqual(60 * 100, deserialized.RequestLengths);
            Assert.AreEqual(60 * 1000, deserialized.ResponseLengths);
            Assert.AreEqual(0, deserialized.StatusCounts[0]);
            Assert.AreEqual(60 - 2 * 6, deserialized.StatusCounts[1]);
            Assert.AreEqual(0, deserialized.StatusCounts[2]);
            Assert.AreEqual(6, deserialized.StatusCounts[3]);
            Assert.AreEqual(6, deserialized.StatusCounts[4]);
        }
        [TestMethod]
        public async STT.Task Stat_Aggregation_WebHook_RawTo1Hourly()
        {
            var statDataProvider = new TestStatisticalDataProvider();
            var aggregator = new WebHookStatisticalDataAggregator(statDataProvider);

            var now = DateTime.UtcNow;
            var record = new StatisticalDataRecord
            {
                DataType = "WebHook",
                WebHookId = 1242,
                ContentId = 1342,
                EventName = "Event42",
                Url = "POST https://example.com/api/hook",
                RequestLength = 100,
                ResponseLength = 1000,
            };
            var time1 = now.AddHours(-3);
            var time2 = time1.AddMilliseconds(100);
            var count = 0;

            var d = now.AddHours(-2);
            var expectedStart = new DateTime(d.Year, d.Month, d.Day, d.Hour, 0, 0);
            var expectedEnd = expectedStart.AddDays(1);
            while (true)
            {
                record.RequestTime = time1;
                record.ResponseTime = time2;
                var error = (++count % 10) == 0;
                var warning = (count % 10) == 1;
                record.ResponseStatusCode = error ? 500 : (warning ? 400 : 200);
                record.ErrorMessage = error ? "ErrorMessage1" : (warning ? "WarningMessage" : null);

                await statDataProvider.WriteDataAsync(record, CancellationToken.None);

                time1 = time1.AddSeconds(10);
                time2 = time1.AddMilliseconds(100);
                if (time2 > now)
                    break;
            }

            // ACTION
            await aggregator.AggregateAsync(now.AddHours(-2), TimeResolution.Hour, CancellationToken.None);

            // ASSERT
            Assert.AreEqual(1, statDataProvider.Aggregations.Count);

            Aggregation aggregation = statDataProvider.Aggregations[0];
            Assert.AreEqual("WebHook", aggregation.DataType);
            Assert.AreEqual(expectedStart, aggregation.Date);
            Assert.AreEqual(TimeResolution.Hour, aggregation.Resolution);

            WebHookStatisticalDataAggregator.WebHookAggregation deserialized;
            using (var reader = new StringReader(aggregation.Data))
                deserialized = JsonSerializer.Create().Deserialize<WebHookStatisticalDataAggregator.WebHookAggregation>(new JsonTextReader(reader));
            Assert.AreEqual(360, deserialized.CallCount); // 60 * 6
            Assert.AreEqual(360 * 100, deserialized.RequestLengths);
            Assert.AreEqual(360 * 1000, deserialized.ResponseLengths);
            Assert.AreEqual(0, deserialized.StatusCounts[0]);
            Assert.AreEqual(360 - 2 * 36, deserialized.StatusCounts[1]);
            Assert.AreEqual(0, deserialized.StatusCounts[2]);
            Assert.AreEqual(36, deserialized.StatusCounts[3]);
            Assert.AreEqual(36, deserialized.StatusCounts[4]);
        }
        [TestMethod]
        public async STT.Task Stat_Aggregation_WebHook_RawTo1Daily()
        {
            var statDataProvider = new TestStatisticalDataProvider();
            var aggregator = new WebHookStatisticalDataAggregator(statDataProvider);

            var now = DateTime.UtcNow;
            var record = new StatisticalDataRecord
            {
                DataType = "WebHook",
                WebHookId = 1242, ContentId = 1342, EventName = "Event42",
                Url = "POST https://example.com/api/hook",
                RequestLength = 100, ResponseLength = 1000,
            };
            var time1 = now.AddDays(-3);
            var time2 = time1.AddMilliseconds(100);
            var count = 0;

            var d = now.AddDays(-2);
            var expectedStart = new DateTime(d.Year, d.Month, d.Day);
            var expectedEnd = expectedStart.AddDays(1);
            while (true)
            {
                record.RequestTime = time1;
                record.ResponseTime = time2;
                var error = (++count % 10) == 0;
                var warning = (count % 10) == 1;
                record.ResponseStatusCode = error ? 500 : (warning ? 400 : 200);
                record.ErrorMessage = error ? "ErrorMessage1" : (warning ? "WarningMessage" : null);

                await statDataProvider.WriteDataAsync(record, CancellationToken.None);

                time1 = time1.AddSeconds(10);
                time2 = time1.AddMilliseconds(100);
                if (time2 > now)
                    break;
            }

            // ACTION
            await aggregator.AggregateAsync(now.AddDays(-2), TimeResolution.Day, CancellationToken.None);

            // ASSERT
            Assert.AreEqual(1, statDataProvider.Aggregations.Count);

            Aggregation aggregation = statDataProvider.Aggregations[0];
            Assert.AreEqual("WebHook", aggregation.DataType);
            Assert.AreEqual(expectedStart, aggregation.Date);
            Assert.AreEqual(TimeResolution.Day, aggregation.Resolution);

            WebHookStatisticalDataAggregator.WebHookAggregation deserialized;
            using (var reader = new StringReader(aggregation.Data))
                deserialized = JsonSerializer.Create().Deserialize<WebHookStatisticalDataAggregator.WebHookAggregation>(new JsonTextReader(reader));
            Assert.AreEqual(8640, deserialized.CallCount); // 24 * 60 * 6
            Assert.AreEqual(8640 * 100, deserialized.RequestLengths);
            Assert.AreEqual(8640 * 1000, deserialized.ResponseLengths);
            Assert.AreEqual(0, deserialized.StatusCounts[0]);
            Assert.AreEqual(8640 - 2 * 864, deserialized.StatusCounts[1]);
            Assert.AreEqual(0, deserialized.StatusCounts[2]);
            Assert.AreEqual(864, deserialized.StatusCounts[3]);
            Assert.AreEqual(864, deserialized.StatusCounts[4]);
        }
        [TestMethod]
        public async STT.Task Stat_Aggregation_WebHook_RawTo1Monthly()
        {
            var statDataProvider = new TestStatisticalDataProvider();
            var aggregator = new WebHookStatisticalDataAggregator(statDataProvider);

            var now = DateTime.UtcNow;
            var record = new StatisticalDataRecord
            {
                DataType = "WebHook",
                WebHookId = 1242,
                ContentId = 1342,
                EventName = "Event42",
                Url = "POST https://example.com/api/hook",
                RequestLength = 100,
                ResponseLength = 1000,
            };
            var time1 = now.AddMonths(-3);
            var time2 = time1.AddMilliseconds(100);
            var count = 0;

            var d = now.AddMonths(-2);
            var expectedStart = new DateTime(d.Year, d.Month, d.Day);
            var expectedEnd = expectedStart.AddDays(1);
            var expectedCount = 0;
            var expectedErrorCount = 0;
            while (true)
            {
                record.RequestTime = time1;
                record.ResponseTime = time2;
                var error = (++count % 10) == 0;
                record.ResponseStatusCode = error ? 500 : 200;
                record.ErrorMessage = error ? "ErrorMessage1" : null;

                await statDataProvider.WriteDataAsync(record, CancellationToken.None);

                if (time1 >= expectedStart && time1 < expectedEnd)
                {
                    expectedCount++;
                    if (error)
                        expectedErrorCount++;
                }

                time1 = time1.AddMinutes(10);
                time2 = time1.AddMilliseconds(100);
                if (time2 > now)
                    break;
            }

            // ACTION
            await aggregator.AggregateAsync(now.AddMonths(-2), TimeResolution.Day, CancellationToken.None);

            // ASSERT
            Assert.AreEqual(1, statDataProvider.Aggregations.Count);

            Aggregation aggregation = statDataProvider.Aggregations[0];
            Assert.AreEqual("WebHook", aggregation.DataType);
            Assert.AreEqual(expectedStart, aggregation.Date);
            Assert.AreEqual(TimeResolution.Day, aggregation.Resolution);

            WebHookStatisticalDataAggregator.WebHookAggregation deserialized;
            using (var reader = new StringReader(aggregation.Data))
                deserialized = JsonSerializer.Create().Deserialize<WebHookStatisticalDataAggregator.WebHookAggregation>(new JsonTextReader(reader));
            Assert.AreEqual(expectedCount, deserialized.CallCount);
            Assert.AreEqual(expectedCount * 100, deserialized.RequestLengths);
            Assert.AreEqual(expectedCount * 1000, deserialized.ResponseLengths);
            Assert.AreEqual(0, deserialized.StatusCounts[0]);
            Assert.AreEqual(expectedCount - expectedErrorCount, deserialized.StatusCounts[1]);
            Assert.AreEqual(0, deserialized.StatusCounts[2]);
            Assert.AreEqual(0, deserialized.StatusCounts[3]);
            Assert.AreEqual(expectedErrorCount, deserialized.StatusCounts[4]);
        }

        [TestMethod]
        public async STT.Task Stat_Aggregation_WebHook_60MinutelyTo1Hourly()
        {
            var statDataProvider = new TestStatisticalDataProvider();
            var aggregator = new WebHookStatisticalDataAggregator(statDataProvider);

            var now = DateTime.UtcNow;
            var record = new StatisticalDataRecord
            {
                DataType = "WebHook",
                WebHookId = 1242,
                ContentId = 1342,
                EventName = "Event42",
                Url = "POST https://example.com/api/hook",
                RequestLength = 100,
                ResponseLength = 1000,
            };
            var time1 = now.AddHours(-3);
            var time2 = time1.AddMilliseconds(100);
            var count = 0;

            var d = now.AddHours(-2);
            var expectedStart = new DateTime(d.Year, d.Month, d.Day, d.Hour, 0, 0);
            var expectedEnd = expectedStart.AddHours(1);
            while (true)
            {
                record.RequestTime = time1;
                record.ResponseTime = time2;
                var error = (++count % 10) == 0;
                var warning = (count % 10) == 1;
                record.ResponseStatusCode = error ? 500 : (warning ? 400 : 200);
                record.ErrorMessage = error ? "ErrorMessage1" : (warning ? "WarningMessage" : null);

                await statDataProvider.WriteDataAsync(record, CancellationToken.None);

                time1 = time1.AddSeconds(1);
                time2 = time1.AddMilliseconds(100);
                if (time2 > now)
                    break;
            }

            for (var startTime = now.AddHours(-3); startTime < now.AddMinutes(-2); startTime = startTime.AddMinutes(1))
                await aggregator.AggregateAsync(startTime, TimeResolution.Minute, CancellationToken.None);

            // ensure that the "Hour" aggregation works from minutely aggregations.
            statDataProvider.Storage.Clear();

            // ACTION
            await aggregator.AggregateAsync(now.AddHours(-2), TimeResolution.Hour, CancellationToken.None);

            // ASSERT
            //Assert.AreEqual(61, statDataProvider.Aggregations.Count);

            Aggregation aggregation = statDataProvider.Aggregations[^1];
            Assert.AreEqual("WebHook", aggregation.DataType);
            Assert.AreEqual(expectedStart, aggregation.Date);
            Assert.AreEqual(TimeResolution.Hour, aggregation.Resolution);

            WebHookStatisticalDataAggregator.WebHookAggregation deserialized;
            using (var reader = new StringReader(aggregation.Data))
                deserialized = JsonSerializer.Create().Deserialize<WebHookStatisticalDataAggregator.WebHookAggregation>(new JsonTextReader(reader));

            Assert.AreEqual(3600, deserialized.CallCount); // 60 * 60
            Assert.AreEqual(3600 * 100, deserialized.RequestLengths);
            Assert.AreEqual(3600 * 1000, deserialized.ResponseLengths);
            Assert.AreEqual(0, deserialized.StatusCounts[0]);
            Assert.AreEqual(3600 - 2 * 360, deserialized.StatusCounts[1]);
            Assert.AreEqual(0, deserialized.StatusCounts[2]);
            Assert.AreEqual(360, deserialized.StatusCounts[3]);
            Assert.AreEqual(360, deserialized.StatusCounts[4]);
        }

        [TestMethod]
        public async STT.Task Stat_Aggregation_WebHook_Run_1day()
        {
            var statDataProvider = new TestStatisticalDataProvider();
            WebHookStatisticalDataAggregator aggregator;

            var testStart = new DateTime(2019, 12, 31, 23, 59, 59);
            var testEnd = new DateTime(2020, 1, 2, 0, 0, 0);
            var now = testStart;

            while (now <= testEnd)
            {
                await GenerateWebHookRecordAsync(now, statDataProvider, CancellationToken.None);
                if (now.Second == 0)
                {
                    var aggregationTime = now.AddSeconds(-1);

                    aggregator = new WebHookStatisticalDataAggregator(statDataProvider);
                    await aggregator.AggregateAsync(aggregationTime, TimeResolution.Minute, CancellationToken.None);
                    if (now.Minute == 0)
                    {
                        aggregator = new WebHookStatisticalDataAggregator(statDataProvider);
                        await aggregator.AggregateAsync(aggregationTime, TimeResolution.Hour,
                            CancellationToken.None);
                        if (now.Hour == 0)
                        {
                            aggregator = new WebHookStatisticalDataAggregator(statDataProvider);
                            await aggregator.AggregateAsync(aggregationTime, TimeResolution.Day,
                                CancellationToken.None);
                            if (now.Day == 1)
                            {
                                aggregator = new WebHookStatisticalDataAggregator(statDataProvider);
                                await aggregator.AggregateAsync(aggregationTime, TimeResolution.Month,
                                    CancellationToken.None);
                            }
                        }
                    }
                }
                now = now.AddSeconds(1);
            }

            var allAggregations = statDataProvider.Aggregations;
            Assert.AreEqual(24 * 60 * 60 + 2, statDataProvider.Storage.Count);
            Assert.AreEqual(24 * 60 + 1, allAggregations.Count(x => x.Resolution == TimeResolution.Minute));
            Assert.AreEqual(24 + 1, allAggregations.Count(x => x.Resolution == TimeResolution.Hour));
            Assert.AreEqual(1 + 1, allAggregations.Count(x => x.Resolution == TimeResolution.Day));
            Assert.AreEqual(0 + 1, allAggregations.Count(x => x.Resolution == TimeResolution.Month));

            var dt = "WebHook";

            var resolutionCount = Enum.GetValues(typeof(TimeResolution)).Length;
            var aggregations = new WebHookStatisticalDataAggregator.WebHookAggregation[resolutionCount][];
            for (int resolutionValue = 0; resolutionValue < resolutionCount; resolutionValue++)
            {
                aggregations[resolutionValue]= allAggregations
                    .Where(x => x.DataType == dt && x.Resolution == (TimeResolution)resolutionValue)
                    .Take(2)
                    .Select(x => WebHookStatisticalDataAggregator.Deserialize(x.Data))
                    .ToArray();

                // The first elements are truncated need to contain only one request per any resolution.
                Assert.AreEqual(1, aggregations[resolutionValue][0].CallCount);
            }
            // The additional items contain an amount of items corresponding to the resolution
            Assert.AreEqual(60, aggregations[(int)TimeResolution.Minute][1].CallCount);
            Assert.AreEqual(60 * 60, aggregations[(int)TimeResolution.Hour][1].CallCount);
            Assert.AreEqual(24 * 60 * 60, aggregations[(int)TimeResolution.Day][1].CallCount);
        }
        private async STT.Task GenerateWebHookRecordAsync(DateTime date, TestStatisticalDataProvider statDataProvider, CancellationToken cancel)
        {
            var count = statDataProvider.Storage.Count;
            var error = (count % 10) == 0;
            var warning = (count % 10) == 1;
            var record = new StatisticalDataRecord
            {
                DataType = "WebHook",
                WebHookId = 1242,
                ContentId = 1342,
                EventName = "Event42",
                Url = "POST https://example.com/api/hook",
                RequestLength = 100,
                ResponseLength = 1000,
                RequestTime = date,
                ResponseTime = date.AddMilliseconds(100),
                ResponseStatusCode = error ? 500 : (warning ? 400 : 200),
                ErrorMessage = error ? "ErrorMessage1" : (warning ? "WarningMessage" : null)
            };

            await statDataProvider.WriteDataAsync(record, cancel);
        }

        [TestMethod]
        public async STT.Task Stat_Aggregation_WebHook_Run_1month()
        {
            var statDataProvider = new TestStatisticalDataProvider();
            WebHookStatisticalDataAggregator aggregator;

            var testStart = new DateTime(2019, 12, 31, 23, 59, 01);
            var testEnd = new DateTime(2020, 2, 1, 0, 0, 0);
            var now = testStart;

            while (now <= testEnd)
            {
                if (now.Second == 0)
                {
                    var aggregationTime = now.AddSeconds(-1);

                    aggregator = new WebHookStatisticalDataAggregator(statDataProvider);
                    await GenerateWebHookAggregationAsync(aggregationTime, TimeResolution.Minute, 60, statDataProvider);
                    if (now.Minute == 0)
                    {
                        aggregator = new WebHookStatisticalDataAggregator(statDataProvider);
                        await aggregator.AggregateAsync(aggregationTime, TimeResolution.Hour, CancellationToken.None);
                        if (now.Hour == 0)
                        {
                            aggregator = new WebHookStatisticalDataAggregator(statDataProvider);
                            await aggregator.AggregateAsync(aggregationTime, TimeResolution.Day, CancellationToken.None);
                            if (now.Day == 1)
                            {
                                aggregator = new WebHookStatisticalDataAggregator(statDataProvider);
                                await aggregator.AggregateAsync(aggregationTime, TimeResolution.Month, CancellationToken.None);
                            }
                        }
                    }
                }
                now = now.AddSeconds(1);
            }

            var allAggregations = statDataProvider.Aggregations;
            Assert.AreEqual(31 * 24 * 60 + 1, allAggregations.Count(x => x.Resolution == TimeResolution.Minute));
            Assert.AreEqual(31 * 24 + 1, allAggregations.Count(x => x.Resolution == TimeResolution.Hour));
            Assert.AreEqual(31 + 1, allAggregations.Count(x => x.Resolution == TimeResolution.Day));
            Assert.AreEqual(1 + 1, allAggregations.Count(x => x.Resolution == TimeResolution.Month));

            var dt = "WebHook";

            var resolutionCount = Enum.GetValues(typeof(TimeResolution)).Length;
            var aggregations = new WebHookStatisticalDataAggregator.WebHookAggregation[resolutionCount][];
            for (int resolutionValue = 0; resolutionValue < resolutionCount; resolutionValue++)
            {
                aggregations[resolutionValue] = allAggregations
                    .Where(x => x.DataType == dt && x.Resolution == (TimeResolution)resolutionValue)
                    .Take(2)
                    .Select(x => WebHookStatisticalDataAggregator.Deserialize(x.Data))
                    .ToArray();

                // The first elements are truncated need to contain only one request per any resolution.
                Assert.AreEqual(60, aggregations[resolutionValue][0].CallCount);
            }
            // The additional items contain an amount of items corresponding to the resolution
            Assert.AreEqual(60, aggregations[(int)TimeResolution.Minute][1].CallCount);
            Assert.AreEqual(60 * 60, aggregations[(int)TimeResolution.Hour][1].CallCount);
            Assert.AreEqual(24 * 60 * 60, aggregations[(int)TimeResolution.Day][1].CallCount);
            Assert.AreEqual(31 * 24 * 60 * 60, aggregations[(int)TimeResolution.Month][1].CallCount);

        }
        [TestMethod]
        public async STT.Task Stat_Aggregation_WebHook_Run_1year()
        {
            var statDataProvider = new TestStatisticalDataProvider();
            WebHookStatisticalDataAggregator aggregator;

            var testStart = new DateTime(2020, 1, 1, 0, 0, 1);
            var testEnd = new DateTime(2021, 1, 1, 0, 0, 0);
            var now = testStart;

            while (now <= testEnd)
            {
                if (now.Second == 0)
                {
                    //await GenerateWebHookRecordAsync(now, statDataProvider, CancellationToken.None);
                    if (now.Second == 0)
                    {
                        var aggregationTime = now.AddSeconds(-1);

                        if (now.Minute == 0)
                        {
                            await GenerateWebHookAggregationAsync(aggregationTime, TimeResolution.Hour, 60 * 60, statDataProvider);
                            if (now.Hour == 0)
                            {
                                aggregator = new WebHookStatisticalDataAggregator(statDataProvider);
                                await aggregator.AggregateAsync(aggregationTime, TimeResolution.Day, CancellationToken.None);
                                if (now.Day == 1)
                                {
                                    aggregator = new WebHookStatisticalDataAggregator(statDataProvider);
                                    await aggregator.AggregateAsync(aggregationTime, TimeResolution.Month, CancellationToken.None);
                                }
                            }
                        }
                    }
                }
                now = now.AddSeconds(1);
            }

            var allAggregations = statDataProvider.Aggregations;
            // not generated
            Assert.AreEqual(0, allAggregations.Count(x => x.Resolution == TimeResolution.Minute));
            // 2020 was leap year
            Assert.AreEqual(366 * 24, allAggregations.Count(x => x.Resolution == TimeResolution.Hour));
            Assert.AreEqual(366, allAggregations.Count(x => x.Resolution == TimeResolution.Day));
            Assert.AreEqual(12, allAggregations.Count(x => x.Resolution == TimeResolution.Month));

            var dt = "WebHook";

            var resolutionCount = Enum.GetValues(typeof(TimeResolution)).Length;
            var aggregations = new WebHookStatisticalDataAggregator.WebHookAggregation[resolutionCount][];
            // Start with 1 because the per-minute aggregation was not generated.
            for (int resolutionValue = 1; resolutionValue < resolutionCount; resolutionValue++)
            {
                aggregations[resolutionValue] = allAggregations
                    .Where(x => x.DataType == dt && x.Resolution == (TimeResolution)resolutionValue)
                    .Take(2)
                    .Select(x => WebHookStatisticalDataAggregator.Deserialize(x.Data))
                    .ToArray();
            }
            // Per-minute aggregations are skipped
            Assert.AreEqual(60 * 60, aggregations[(int)TimeResolution.Hour][0].CallCount);
            Assert.AreEqual(24 * 60 * 60, aggregations[(int)TimeResolution.Day][0].CallCount);
            // 2020 February had 29 days
            Assert.AreEqual(31 * 24 * 60 * 60, aggregations[(int)TimeResolution.Month][0].CallCount);

        }
        private async STT.Task GenerateWebHookAggregationAsync(DateTime date, TimeResolution resolution, int callCount,
            TestStatisticalDataProvider statDataProvider)
        {
            var callCountPer10 = callCount / 10;

            var aggregation = new Aggregation
            {
                Date = date,
                DataType = "WebHook",
                Resolution = resolution,
                Data = WebHookStatisticalDataAggregator.Serialize(
                    new WebHookStatisticalDataAggregator.WebHookAggregation
                    {
                        CallCount = callCount,
                        RequestLengths = callCount * 100,
                        ResponseLengths = callCount * 1000,
                        StatusCounts = new[] {0, callCount - 2 * callCountPer10, 0, callCountPer10, callCountPer10}
                    })
            };

            await statDataProvider.WriteAggregationAsync(aggregation, CancellationToken.None);
        }

        [TestMethod]
        public async STT.Task Stat_Aggregation_WebHook_Run_DoNotWriteEmpty()
        {
            var statDataProvider = new TestStatisticalDataProvider();
            WebHookStatisticalDataAggregator aggregator;

            var testStart = new DateTime(2020, 1, 1, 0, 0, 1);
            var testEnd = new DateTime(2020, 2, 1, 0, 0, 0);
            var now = testStart;

            while (now <= testEnd)
            {
                if (now.Second == 0)
                {
                    // Generate a webhook call every day at ten.
                    if (now.Hour == 10 && now.Minute == 0 && now.Second == 0)
                        await GenerateWebHookRecordAsync(now, statDataProvider, CancellationToken.None);

                    if (now.Second == 0)
                    {
                        var aggregationTime = now.AddSeconds(-1);

                        aggregator = new WebHookStatisticalDataAggregator(statDataProvider);
                        await aggregator.AggregateAsync(aggregationTime, TimeResolution.Minute, CancellationToken.None);
                        if (now.Minute == 0)
                        {
                            aggregator = new WebHookStatisticalDataAggregator(statDataProvider);
                            await aggregator.AggregateAsync(aggregationTime, TimeResolution.Hour, CancellationToken.None);
                            if (now.Hour == 0)
                            {
                                aggregator = new WebHookStatisticalDataAggregator(statDataProvider);
                                await aggregator.AggregateAsync(aggregationTime, TimeResolution.Day, CancellationToken.None);
                                if (now.Day == 1)
                                {
                                    aggregator = new WebHookStatisticalDataAggregator(statDataProvider);
                                    await aggregator.AggregateAsync(aggregationTime, TimeResolution.Month, CancellationToken.None);
                                }
                            }
                        }
                    }
                }
                now = now.AddSeconds(1);
            }

            var allAggregations = statDataProvider.Aggregations;
            Assert.AreEqual(31, allAggregations.Count(x => x.Resolution == TimeResolution.Minute));
            Assert.AreEqual(31, allAggregations.Count(x => x.Resolution == TimeResolution.Hour));
            Assert.AreEqual(31, allAggregations.Count(x => x.Resolution == TimeResolution.Day));
            Assert.AreEqual(1, allAggregations.Count(x => x.Resolution == TimeResolution.Month));

            var dt = "WebHook";

            var resolutionCount = Enum.GetValues(typeof(TimeResolution)).Length;
            var aggregations = new WebHookStatisticalDataAggregator.WebHookAggregation[resolutionCount][];
            for (int resolutionValue = 0; resolutionValue < resolutionCount; resolutionValue++)
            {
                aggregations[resolutionValue] = allAggregations
                    .Where(x => x.DataType == dt && x.Resolution == (TimeResolution)resolutionValue)
                    .Take(2)
                    .Select(x => WebHookStatisticalDataAggregator.Deserialize(x.Data))
                    .ToArray();
            }
            Assert.AreEqual(1, aggregations[(int)TimeResolution.Minute][0].CallCount);
            Assert.AreEqual(1, aggregations[(int)TimeResolution.Hour][0].CallCount);
            Assert.AreEqual(1, aggregations[(int)TimeResolution.Day][0].CallCount);
            Assert.AreEqual(31, aggregations[(int)TimeResolution.Month][0].CallCount);

        }

        #endregion

    }
}
