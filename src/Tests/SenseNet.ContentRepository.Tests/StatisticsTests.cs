using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.ApplicationModel;
using SenseNet.BackgroundOperations;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Diagnostics;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Events;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.OData;
using SenseNet.Security;
using SenseNet.Services.Core;
using SenseNet.Services.Core.Authentication;
using SenseNet.Services.Core.Diagnostics;
using SenseNet.Services.Core.Virtualization;
using SenseNet.Services.Wopi;
using SenseNet.Storage.Data.MsSqlClient;
using SenseNet.Storage.DataModel.Usage;
using SenseNet.Testing;
using SenseNet.Tests.Core;
using SenseNet.Tests.Core.Implementations;
using SenseNet.WebHooks;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public partial class StatisticsTests : TestBase
    {
        #region /* ========================================================================= Collecting tests */

        [TestMethod]
        public async STT.Task Stat_Collecting_BinaryMiddleware()
        {
            await Test(async () =>
            {
                var serviceProvider = new ServiceCollection()
                    .AddTransient<WebTransferRegistrator>()
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
                var wtr = serviceProvider.GetService<WebTransferRegistrator>();
                await middleware.InvokeAsync(httpContext, wtr).ConfigureAwait(false);
                await STT.Task.Delay(1);

                // ASSERT
                var collector = (TestStatisticalDataCollector)serviceProvider.GetService<IStatisticalDataCollector>();
                Assert.AreEqual(1, collector.StatData.Count);
                var data = (WebTransferStatInput) collector.StatData[0];
                Assert.AreEqual(path, data.Url);
                Assert.AreEqual("GET", data.HttpMethod);
                Assert.AreEqual(GetRequestLength(request), data.RequestLength);
                Assert.AreEqual(200, data.ResponseStatusCode);
                Assert.IsTrue(node.Binary.GetStream().Length < data.ResponseLength);
                Assert.IsTrue(data.RequestTime <= data.ResponseTime);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task Stat_Collecting_WopiMiddleware()
        {
            await Test(async () =>
            {
                var serviceProvider = new ServiceCollection()
                    .AddTransient<WebTransferRegistrator>()
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
                var wtr = serviceProvider.GetService<WebTransferRegistrator>();
                await middleware.InvokeAsync(httpContext, wtr).ConfigureAwait(false);
                await STT.Task.Delay(1);

                // ASSERT
                var collector = (TestStatisticalDataCollector)serviceProvider.GetService<IStatisticalDataCollector>();
                Assert.AreEqual(1, collector.StatData.Count);
                var data = (WebTransferStatInput)collector.StatData[0];
                Assert.AreEqual(path, data.Url);
                Assert.AreEqual("GET", data.HttpMethod);
                Assert.AreEqual(GetRequestLength(request), data.RequestLength);
                Assert.AreEqual(200, data.ResponseStatusCode);
                Assert.IsTrue(file.Binary.GetStream().Length < data.ResponseLength);
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
                    .AddTransient<WebTransferRegistrator>()
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
                var wtr = serviceProvider.GetService<WebTransferRegistrator>();
                await middleware.InvokeAsync(httpContext, wtr).ConfigureAwait(false);
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
                Assert.AreEqual(GetRequestLength(request), data.RequestLength);
                Assert.AreEqual(200, data.ResponseStatusCode);
                Assert.IsTrue(expectedLength < data.ResponseLength);
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
                    .AddTransient<WebTransferRegistrator>()
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
                var wtr = serviceProvider.GetService<WebTransferRegistrator>();
                await middleware.InvokeAsync(httpContext, wtr).ConfigureAwait(false);
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
                Assert.AreEqual(GetRequestLength(request), data.RequestLength);
                Assert.AreEqual(200, data.ResponseStatusCode);
                Assert.IsTrue(expectedLength < data.ResponseLength);
                Assert.IsTrue(data.RequestTime <= data.ResponseTime);
            }).ConfigureAwait(false);
        }
        private long GetRequestLength(HttpRequest request)
        {
            return (request.Path.Value?.Length ?? 0L) +
                   request.Method.Length +
                   (request.QueryString.Value?.Length ?? 0L) +
                   (request.ContentLength ?? 0L) +
                   GetCookiesLength(request.Cookies) +
                   GetHeadersLength(request.Headers);
        }
        private long GetCookiesLength(IRequestCookieCollection requestCookies)
        {
            var sum = 0L;
            foreach (var cookie in requestCookies)
                sum += cookie.Key.Length + (cookie.Value?.Length ?? 0);
            return sum;
        }
        private long GetHeadersLength(IHeaderDictionary headers)
        {
            var sum = 0L;
            foreach (var header in headers)
            {
                sum += header.Key.Length;
                foreach (var stringValue in header.Value)
                    sum += stringValue.Length;
            }
            return sum;
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

                    Assert.IsNotNull(data[0].Payload);
                    Assert.IsNotNull(data[1].Payload);
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

                Assert.AreEqual(0, collector.StatData.Count);
                Assert.AreEqual(1, collector.Aggregations.Count);
                var aggregation = collector.Aggregations[0];
                Assert.AreEqual("DatabaseUsage", aggregation.DataType);
                Assert.IsTrue(DateTime.UtcNow >= aggregation.Date);
                Assert.AreEqual(TimeResolution.Hour, aggregation.Resolution);
                Assert.IsTrue(RemoveWhitespaces(aggregation.Data).Contains("{\"Content\":{\"Count\""));

            }).ConfigureAwait(false);

        }

        #region Additional classes for "collecting" tests

        private class TestStatisticalDataCollector : IStatisticalDataCollector
        {
            public List<object> StatData { get; } = new List<object>();
            public List<Aggregation> Aggregations { get; } = new List<Aggregation>();

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

            public STT.Task RegisterGeneralData(string dataType, TimeResolution resolution, object data, CancellationToken cancel)
            {
                var aggregation = new Aggregation
                {
                    DataType = dataType,
                    Date = DateTime.UtcNow.Truncate(resolution),
                    Resolution = resolution,
                    Data = StatisticalDataCollector.Serialize(data)
                };

                Aggregations.Add(aggregation);
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
            var time = DateTime.UtcNow.AddDays(-1);
            var duration = TimeSpan.FromSeconds(1);
            var input = new WebTransferStatInput
            {
                Url = "Url1",
                HttpMethod = "GET",
                RequestTime = time,
                ResponseTime = time.Add(duration),
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
            Assert.AreEqual(time, record.CreationTime);
            Assert.AreEqual(duration, record.Duration);
            Assert.AreEqual(42, record.RequestLength);
            Assert.AreEqual(4242, record.ResponseLength);
            Assert.AreEqual(200, record.ResponseStatusCode);
            Assert.IsNull(record.TargetId);
            Assert.IsNull(record.ContentId);
            Assert.IsNull(record.EventName);
            Assert.IsNull(record.ErrorMessage);
        }
        [TestMethod]
        public async STT.Task Stat_Collector_CollectWebHook()
        {
            var sdp = new TestStatisticalDataProvider();
            var collector = new StatisticalDataCollector(sdp);
            var time = DateTime.UtcNow.AddDays(-1);
            var duration = TimeSpan.FromSeconds(1);
            var input = new WebHookStatInput
            {
                Url = "Url1",
                HttpMethod = "POST",
                RequestTime = time,
                ResponseTime = time.Add(duration),
                RequestLength = 42,
                ResponseLength = 4242,
                ResponseStatusCode = 200,
                WebHookId = 1242,
                ContentId = 1342,
                EventName = "Event42",
                ErrorMessage = "ErrorMessage1",
                Payload = new { name1 = "value1", name2 = "value2" }
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
            Assert.AreEqual(time, record.CreationTime);
            Assert.AreEqual(duration, record.Duration);
            Assert.AreEqual(42, record.RequestLength);
            Assert.AreEqual(4242, record.ResponseLength);
            Assert.AreEqual(200, record.ResponseStatusCode);
            Assert.AreEqual(1242, record.TargetId);
            Assert.AreEqual(1342, record.ContentId);
            Assert.AreEqual("Event42", record.EventName);
            Assert.AreEqual("ErrorMessage1", record.ErrorMessage);
            Assert.AreEqual("{\"name1\":\"value1\",\"name2\":\"value2\"}", RemoveWhitespaces(record.GeneralData));
        }
        [TestMethod]
        public async STT.Task Stat_Collector_CollectGeneralData()
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
            Assert.IsNotNull( record.CreationTime);
            Assert.IsNull( record.Duration);
            Assert.IsNull( record.RequestLength);
            Assert.IsNull( record.ResponseLength);
            Assert.IsNull( record.ResponseStatusCode);
            Assert.IsNull( record.TargetId);
            Assert.IsNull( record.ContentId);
            Assert.IsNull( record.EventName);
            Assert.IsNull( record.ErrorMessage);
            Assert.AreEqual("{\"Name\":\"Name1\",\"Value\":42}", RemoveWhitespaces(record.GeneralData));
        }
        [TestMethod]
        public async STT.Task Stat_Collector_CollectDbUsage()
        {
            var sdp = new TestStatisticalDataProvider();
            var collector = new StatisticalDataCollector(sdp);
            var data = new { Name = "Name1", Value = 42 };
            var input = new GeneralStatInput { DataType = "DbUsage", Data = data };
            var now = DateTime.UtcNow;
            var resolution = TimeResolution.Day;

            // ACTION
#pragma warning disable 4014
            collector.RegisterGeneralData("DbUsage", resolution, data, CancellationToken.None);
#pragma warning restore 4014

            // ASSERT
            await STT.Task.Delay(1);
            Assert.AreEqual(0, sdp.Storage.Count);
            Assert.AreEqual(1, sdp.Aggregations.Count);
            var aggregation = sdp.Aggregations[0];
            Assert.AreEqual("DbUsage", aggregation.DataType);
            Assert.IsTrue(now >= aggregation.Date);
            Assert.AreEqual(resolution, aggregation.Resolution);
            Assert.AreEqual("{\"Name\":\"Name1\",\"Value\":42}", RemoveWhitespaces(aggregation.Data));
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

            Assert.IsNull(record.CreationTime);
            Assert.IsNull(record.Duration);
            Assert.IsNull(record.RequestLength);
            Assert.IsNull(record.ResponseLength);
            Assert.IsNull(record.ResponseStatusCode);
            Assert.IsNull(record.Url);
            Assert.IsNull(record.TargetId);
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

            Assert.AreEqual(time1, record.CreationTime);
            Assert.AreEqual(time2 - time1, record.Duration);
            Assert.AreEqual(42, record.RequestLength);
            Assert.AreEqual(4242, record.ResponseLength);
            Assert.AreEqual(200, record.ResponseStatusCode);
            Assert.AreEqual("GET Url1", record.Url);
            Assert.IsNull(record.TargetId);
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
                ErrorMessage = "ErrorMessage1",
                Payload = new {name1 = "value1", name2 = "value2"}
            };

            // ACTION
            var record = new InputStatisticalDataRecord(input);

            // ASSERT
            Assert.AreEqual("WebHook", record.DataType);

            Assert.AreEqual(0, record.Id);
            Assert.AreEqual(DateTime.MinValue, record.WrittenTime);

            Assert.AreEqual(time1, record.CreationTime);
            Assert.AreEqual(time2 - time1, record.Duration);
            Assert.AreEqual(42, record.RequestLength);
            Assert.AreEqual(4242, record.ResponseLength);
            Assert.AreEqual(200, record.ResponseStatusCode);
            Assert.AreEqual("POST Url1", record.Url);
            Assert.AreEqual(1242, record.TargetId);
            Assert.AreEqual(1342, record.ContentId);
            Assert.AreEqual("Event42", record.EventName);
            Assert.AreEqual("ErrorMessage1", record.ErrorMessage);
            Assert.AreEqual("{\"name1\":\"value1\",\"name2\":\"value2\"}", RemoveWhitespaces(record.GeneralData));
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
            StatisticalDataAggregationController CreateAggregator()
            {
                return new StatisticalDataAggregationController(statDataProvider,
                    new[] { new WebHookStatisticalDataAggregator(GetOptions()) }, GetOptions(), 
                    NullLoggerFactory.Instance.CreateLogger<StatisticalDataAggregationController>());
            }
            StatisticalDataAggregationController aggregator;

            var now = new DateTime(2021, 6, 29, 01, 05, 12);
            var record = new StatisticalDataRecord
            {
                DataType = "WebHook",
                TargetId = 1242,
                ContentId = 1342,
                EventName = "Event42",
                Url = "POST https://example.com/api/hook",
                RequestLength = 100,
                ResponseLength = 1000,
            };
            var time1 = now.AddMinutes(-3);
            var duration = TimeSpan.FromMilliseconds(100);
            var count = 0;

            var d = now.AddMinutes(-2);
            var expectedStart = new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0);
            var expectedEnd = expectedStart.AddMinutes(1);
            while (true)
            {
                record.CreationTime = time1;
                record.Duration = duration;
                var error = (++count % 10) == 0;
                var warning = (count % 10) == 1;
                record.ResponseStatusCode = error ? 500 : (warning ? 400 : 200);
                record.ErrorMessage = error ? "ErrorMessage1" : (warning ? "WarningMessage" : null);

                await statDataProvider.WriteDataAsync(record, CancellationToken.None);

                time1 = time1.AddSeconds(1);
                if (time1.Add(duration) > now)
                    break;
            }

            // ACTION
            aggregator = CreateAggregator();
            await aggregator.AggregateAsync(now.AddMinutes(-2), TimeResolution.Minute, CancellationToken.None);

            // ASSERT
            Assert.IsTrue(2 >= statDataProvider.Aggregations.Count);

            Aggregation aggregation = statDataProvider.Aggregations.Last();
            Assert.AreEqual("WebHook", aggregation.DataType);
            Assert.AreEqual(expectedStart, aggregation.Date);
            Assert.AreEqual(TimeResolution.Minute, aggregation.Resolution);

            var deserialized = DeserializeAggregation<WebHookAggregation>(aggregation.Data);
            Assert.AreEqual(60, deserialized.CallCount); // 60 * 6
            Assert.AreEqual(60 * 100, deserialized.RequestLengths);
            Assert.AreEqual(60 * 1000, deserialized.ResponseLengths);
            Assert.AreEqual(0, deserialized.StatusCounts[0]);
            Assert.AreEqual(60 - 2 * 6, deserialized.StatusCounts[1]);
            Assert.AreEqual(0, deserialized.StatusCounts[2]);
            Assert.AreEqual(6, deserialized.StatusCounts[3]);
            Assert.AreEqual(6, deserialized.StatusCounts[4]);

            Assert.AreEqual(1, statDataProvider.CleanupRecordsCalls.Count);
            Assert.AreEqual("WebHook", statDataProvider.CleanupAggregationsCalls[0].DataType);
            Assert.AreEqual(aggregation.Date.AddMinutes(-3), statDataProvider.CleanupRecordsCalls[0].RetentionTime);
            Assert.AreEqual(1, statDataProvider.CleanupAggregationsCalls.Count);
            Assert.AreEqual("WebHook", statDataProvider.CleanupAggregationsCalls[0].DataType);
            Assert.AreEqual(TimeResolution.Minute, statDataProvider.CleanupAggregationsCalls[0].Resolution);
            Assert.AreEqual(aggregation.Date.AddHours(-3), statDataProvider.CleanupAggregationsCalls[0].RetentionTime);
        }
        [TestMethod]
        public async STT.Task Stat_Aggregation_WebHook_RawTo1Hourly()
        {
            var statDataProvider = new TestStatisticalDataProvider();
            StatisticalDataAggregationController CreateAggregator()
            {
                return new StatisticalDataAggregationController(statDataProvider,
                    new[] { new WebHookStatisticalDataAggregator(GetOptions()) }, GetOptions(),
                    NullLoggerFactory.Instance.CreateLogger<StatisticalDataAggregationController>());
            }
            StatisticalDataAggregationController aggregator;

            var now = DateTime.UtcNow;
            var record = new StatisticalDataRecord
            {
                DataType = "WebHook",
                TargetId = 1242,
                ContentId = 1342,
                EventName = "Event42",
                Url = "POST https://example.com/api/hook",
                RequestLength = 100,
                ResponseLength = 1000,
            };
            var time1 = now.AddHours(-3);
            var duration = TimeSpan.FromMilliseconds(100);
            var count = 0;

            var d = now.AddHours(-2);
            var expectedStart = new DateTime(d.Year, d.Month, d.Day, d.Hour, 0, 0);
            var expectedEnd = expectedStart.AddDays(1);
            while (true)
            {
                record.CreationTime = time1;
                record.Duration = duration;
                var error = (++count % 10) == 0;
                var warning = (count % 10) == 1;
                record.ResponseStatusCode = error ? 500 : (warning ? 400 : 200);
                record.ErrorMessage = error ? "ErrorMessage1" : (warning ? "WarningMessage" : null);

                await statDataProvider.WriteDataAsync(record, CancellationToken.None);

                time1 = time1.AddSeconds(10);
                if (time1.Add(duration) > now)
                    break;
            }

            // ACTION
            aggregator = CreateAggregator();
            await aggregator.AggregateAsync(now.AddHours(-2), TimeResolution.Hour, CancellationToken.None);

            // ASSERT
            Assert.AreEqual(1, statDataProvider.Aggregations.Count);

            Aggregation aggregation = statDataProvider.Aggregations[0];
            Assert.AreEqual("WebHook", aggregation.DataType);
            Assert.AreEqual(expectedStart, aggregation.Date);
            Assert.AreEqual(TimeResolution.Hour, aggregation.Resolution);

            var deserialized = DeserializeAggregation<WebHookAggregation>(aggregation.Data);
            Assert.AreEqual(360, deserialized.CallCount); // 60 * 6
            Assert.AreEqual(360 * 100, deserialized.RequestLengths);
            Assert.AreEqual(360 * 1000, deserialized.ResponseLengths);
            Assert.AreEqual(0, deserialized.StatusCounts[0]);
            Assert.AreEqual(360 - 2 * 36, deserialized.StatusCounts[1]);
            Assert.AreEqual(0, deserialized.StatusCounts[2]);
            Assert.AreEqual(36, deserialized.StatusCounts[3]);
            Assert.AreEqual(36, deserialized.StatusCounts[4]);

            Assert.AreEqual(0, statDataProvider.CleanupRecordsCalls.Count);
            Assert.AreEqual(1, statDataProvider.CleanupAggregationsCalls.Count);
            Assert.AreEqual("WebHook", statDataProvider.CleanupAggregationsCalls[0].DataType);
            Assert.AreEqual(TimeResolution.Hour, statDataProvider.CleanupAggregationsCalls[0].Resolution);
            Assert.AreEqual(aggregation.Date.AddDays(-3), statDataProvider.CleanupAggregationsCalls[0].RetentionTime);
        }
        [TestMethod]
        public async STT.Task Stat_Aggregation_WebHook_RawTo1Daily()
        {
            var statDataProvider = new TestStatisticalDataProvider();
            StatisticalDataAggregationController CreateAggregator()
            {
                return new StatisticalDataAggregationController(statDataProvider,
                    new[] { new WebHookStatisticalDataAggregator(GetOptions()) }, GetOptions(),
                    NullLoggerFactory.Instance.CreateLogger<StatisticalDataAggregationController>());
            }
            StatisticalDataAggregationController aggregator;

            var now = DateTime.UtcNow;
            var record = new StatisticalDataRecord
            {
                DataType = "WebHook",
                TargetId = 1242, ContentId = 1342, EventName = "Event42",
                Url = "POST https://example.com/api/hook",
                RequestLength = 100, ResponseLength = 1000,
            };
            var time1 = now.AddDays(-3);
            var duration = TimeSpan.FromMilliseconds(100);
            var count = 0;

            var d = now.AddDays(-2);
            var expectedStart = new DateTime(d.Year, d.Month, d.Day);
            var expectedEnd = expectedStart.AddDays(1);
            while (true)
            {
                record.CreationTime = time1;
                record.Duration = duration;
                var error = (++count % 10) == 0;
                var warning = (count % 10) == 1;
                record.ResponseStatusCode = error ? 500 : (warning ? 400 : 200);
                record.ErrorMessage = error ? "ErrorMessage1" : (warning ? "WarningMessage" : null);

                await statDataProvider.WriteDataAsync(record, CancellationToken.None);

                time1 = time1.AddSeconds(10);
                if (time1.Add(duration) > now)
                    break;
            }

            // ACTION
            aggregator = CreateAggregator();
            await aggregator.AggregateAsync(now.AddDays(-2), TimeResolution.Day, CancellationToken.None);

            // ASSERT
            Assert.AreEqual(1, statDataProvider.Aggregations.Count);

            Aggregation aggregation = statDataProvider.Aggregations[0];
            Assert.AreEqual("WebHook", aggregation.DataType);
            Assert.AreEqual(expectedStart, aggregation.Date);
            Assert.AreEqual(TimeResolution.Day, aggregation.Resolution);

            var deserialized = DeserializeAggregation<WebHookAggregation>(aggregation.Data);
            Assert.AreEqual(8640, deserialized.CallCount); // 24 * 60 * 6
            Assert.AreEqual(8640 * 100, deserialized.RequestLengths);
            Assert.AreEqual(8640 * 1000, deserialized.ResponseLengths);
            Assert.AreEqual(0, deserialized.StatusCounts[0]);
            Assert.AreEqual(8640 - 2 * 864, deserialized.StatusCounts[1]);
            Assert.AreEqual(0, deserialized.StatusCounts[2]);
            Assert.AreEqual(864, deserialized.StatusCounts[3]);
            Assert.AreEqual(864, deserialized.StatusCounts[4]);

            Assert.AreEqual(0, statDataProvider.CleanupRecordsCalls.Count);
            Assert.AreEqual(1, statDataProvider.CleanupAggregationsCalls.Count);
            Assert.AreEqual("WebHook", statDataProvider.CleanupAggregationsCalls[0].DataType);
            Assert.AreEqual(TimeResolution.Day, statDataProvider.CleanupAggregationsCalls[0].Resolution);
            Assert.AreEqual(aggregation.Date.AddMonths(-3), statDataProvider.CleanupAggregationsCalls[0].RetentionTime);
        }
        [TestMethod]
        public async STT.Task Stat_Aggregation_WebHook_RawTo1Monthly()
        {
            var statDataProvider = new TestStatisticalDataProvider();
            StatisticalDataAggregationController CreateAggregator()
            {
                return new StatisticalDataAggregationController(statDataProvider,
                    new[] { new WebHookStatisticalDataAggregator(GetOptions()) }, GetOptions(),
                    NullLoggerFactory.Instance.CreateLogger<StatisticalDataAggregationController>());
            }
            StatisticalDataAggregationController aggregator;

            var now = DateTime.UtcNow;
            var record = new StatisticalDataRecord
            {
                DataType = "WebHook",
                TargetId = 1242,
                ContentId = 1342,
                EventName = "Event42",
                Url = "POST https://example.com/api/hook",
                RequestLength = 100,
                ResponseLength = 1000,
            };
            var time1 = now.AddMonths(-3);
            var duration = TimeSpan.FromMilliseconds(100);
            var count = 0;

            var d = now.AddMonths(-2);
            var expectedStart = new DateTime(d.Year, d.Month, 1);
            var expectedEnd = expectedStart.AddMonths(1);
            var expectedCount = 0;
            var expectedErrorCount = 0;
            while (true)
            {
                record.CreationTime = time1;
                record.Duration = duration;
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
                if (time1.Add(duration) > now)
                    break;
            }

            // ACTION
            aggregator = CreateAggregator();
            await aggregator.AggregateAsync(now.AddMonths(-2), TimeResolution.Month, CancellationToken.None);

            // ASSERT
            Assert.AreEqual(1, statDataProvider.Aggregations.Count);

            Aggregation aggregation = statDataProvider.Aggregations[0];
            Assert.AreEqual("WebHook", aggregation.DataType);
            Assert.AreEqual(expectedStart, aggregation.Date);
            Assert.AreEqual(TimeResolution.Month, aggregation.Resolution);

            var deserialized = DeserializeAggregation<WebHookAggregation>(aggregation.Data);
            Assert.AreEqual(expectedCount, deserialized.CallCount);
            Assert.AreEqual(expectedCount * 100, deserialized.RequestLengths);
            Assert.AreEqual(expectedCount * 1000, deserialized.ResponseLengths);
            Assert.AreEqual(0, deserialized.StatusCounts[0]);
            Assert.AreEqual(expectedCount - expectedErrorCount, deserialized.StatusCounts[1]);
            Assert.AreEqual(0, deserialized.StatusCounts[2]);
            Assert.AreEqual(0, deserialized.StatusCounts[3]);
            Assert.AreEqual(expectedErrorCount, deserialized.StatusCounts[4]);

            Assert.AreEqual(0, statDataProvider.CleanupRecordsCalls.Count);
            Assert.AreEqual(1, statDataProvider.CleanupAggregationsCalls.Count);
            Assert.AreEqual("WebHook", statDataProvider.CleanupAggregationsCalls[0].DataType);
            Assert.AreEqual(TimeResolution.Month, statDataProvider.CleanupAggregationsCalls[0].Resolution);
            Assert.AreEqual(aggregation.Date.AddYears(-3), statDataProvider.CleanupAggregationsCalls[0].RetentionTime);
        }


        [TestMethod]
        public async STT.Task Stat_Aggregation_DoNotDuplicateButOverwrite_Minute()
        {
            var statDataProvider = new TestStatisticalDataProvider();
            var now = DateTime.UtcNow.AddMinutes(-2);

            // ACTION (write twice)
            await GenerateWebHookAggregationAsync(now, TimeResolution.Minute, 10, statDataProvider).ConfigureAwait(false);
            await GenerateWebHookAggregationAsync(now, TimeResolution.Minute, 11, statDataProvider).ConfigureAwait(false);

            // ASSERT (written only once but updated)
            Assert.AreEqual(1, statDataProvider.Aggregations.Count);
            var aggregation = statDataProvider.Aggregations[0];
            var deserialized = DeserializeAggregation<WebHookAggregation>(aggregation.Data);
            Assert.AreEqual(11, deserialized.CallCount);
        }
        [TestMethod]
        public async STT.Task Stat_Aggregation_DoNotDuplicateButOverwrite_Hour()
        {
            var statDataProvider = new TestStatisticalDataProvider();
            var now = DateTime.UtcNow.AddHours(-2);

            // ACTION (write twice)
            await GenerateWebHookAggregationAsync(now, TimeResolution.Hour, 10, statDataProvider).ConfigureAwait(false);
            await GenerateWebHookAggregationAsync(now, TimeResolution.Hour, 11, statDataProvider).ConfigureAwait(false);

            // ASSERT (written only once but updated)
            Assert.AreEqual(1, statDataProvider.Aggregations.Count);
            var aggregation = statDataProvider.Aggregations[0];
            var deserialized = DeserializeAggregation<WebHookAggregation>(aggregation.Data);
            Assert.AreEqual(11, deserialized.CallCount);
        }
        [TestMethod]
        public async STT.Task Stat_Aggregation_DoNotDuplicateButOverwrite_Day()
        {
            var statDataProvider = new TestStatisticalDataProvider();
            var now = DateTime.UtcNow.AddDays(-2);

            // ACTION (write twice)
            await GenerateWebHookAggregationAsync(now, TimeResolution.Day, 10, statDataProvider).ConfigureAwait(false);
            await GenerateWebHookAggregationAsync(now, TimeResolution.Day, 11, statDataProvider).ConfigureAwait(false);

            // ASSERT (written only once but updated)
            Assert.AreEqual(1, statDataProvider.Aggregations.Count);
            var aggregation = statDataProvider.Aggregations[0];
            var deserialized = DeserializeAggregation<WebHookAggregation>(aggregation.Data);
            Assert.AreEqual(11, deserialized.CallCount);
        }
        [TestMethod]
        public async STT.Task Stat_Aggregation_DoNotDuplicateButOverwrite_Month()
        {
            var statDataProvider = new TestStatisticalDataProvider();
            var now = DateTime.UtcNow.AddMonths(-2);

            // ACTION (write twice)
            await GenerateWebHookAggregationAsync(now, TimeResolution.Month, 10, statDataProvider).ConfigureAwait(false);
            await GenerateWebHookAggregationAsync(now, TimeResolution.Month, 11, statDataProvider).ConfigureAwait(false);

            // ASSERT (written only once but updated)
            Assert.AreEqual(1, statDataProvider.Aggregations.Count);
            var aggregation = statDataProvider.Aggregations[0];
            var deserialized = DeserializeAggregation<WebHookAggregation>(aggregation.Data);
            Assert.AreEqual(11, deserialized.CallCount);
        }

        [TestMethod]
        public async STT.Task Stat_Aggregation_WebHook_60MinutelyTo1Hourly()
        {
            var statDataProvider = new TestStatisticalDataProvider();
            StatisticalDataAggregationController CreateAggregator()
            {
                return new StatisticalDataAggregationController(statDataProvider,
                    new[] { new WebHookStatisticalDataAggregator(GetOptions()) }, GetOptions(),
                    NullLoggerFactory.Instance.CreateLogger<StatisticalDataAggregationController>());
            }
            StatisticalDataAggregationController aggregator;

            var now = new DateTime(2021, 6, 29, 01, 02, 12);
            var record = new StatisticalDataRecord
            {
                DataType = "WebHook",
                TargetId = 1242,
                ContentId = 1342,
                EventName = "Event42",
                Url = "POST https://example.com/api/hook",
                RequestLength = 100,
                ResponseLength = 1000,
            };
            var time1 = now.AddHours(-3);
            var duration = TimeSpan.FromMilliseconds(100);
            var count = 0;

            var d = now.AddHours(-2);
            var expectedStart = new DateTime(d.Year, d.Month, d.Day, d.Hour, 0, 0);
            var expectedEnd = expectedStart.AddHours(1);
            while (true)
            {
                record.CreationTime = time1;
                record.Duration = duration;
                var error = (++count % 10) == 0;
                var warning = (count % 10) == 1;
                record.ResponseStatusCode = error ? 500 : (warning ? 400 : 200);
                record.ErrorMessage = error ? "ErrorMessage1" : (warning ? "WarningMessage" : null);

                await statDataProvider.WriteDataAsync(record, CancellationToken.None);

                time1 = time1.AddSeconds(1);
                if (time1.Add(duration) > now)
                    break;
            }

            for (var startTime = now.AddHours(-3); startTime < now.AddMinutes(-2); startTime = startTime.AddMinutes(1))
            {
                aggregator = CreateAggregator();
                await aggregator.AggregateAsync(startTime, TimeResolution.Minute, CancellationToken.None);
            }

            // ensure that the "Hour" aggregation works from minutely aggregations.
            statDataProvider.Storage.Clear();

            // ACTION
            aggregator = CreateAggregator();
            await aggregator.AggregateAsync(now.AddHours(-2), TimeResolution.Hour, CancellationToken.None);

            // ASSERT
            //Assert.AreEqual(61, statDataProvider.Aggregations.Count);

            var aggregations = statDataProvider.Aggregations.Where(x => x.Resolution == TimeResolution.Hour).ToArray();
            var aggregation = aggregations[^1];
            Assert.AreEqual("WebHook", aggregation.DataType);
            Assert.AreEqual(expectedStart, aggregation.Date);
            Assert.AreEqual(TimeResolution.Hour, aggregation.Resolution);

            var deserialized = DeserializeAggregation<WebHookAggregation>(aggregation.Data);

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
            StatisticalDataAggregationController CreateAggregator()
            {
                return new StatisticalDataAggregationController(statDataProvider,
                    new[] { new WebHookStatisticalDataAggregator(GetOptions()) }, GetOptions(),
                    NullLoggerFactory.Instance.CreateLogger<StatisticalDataAggregationController>());
            }
            StatisticalDataAggregationController aggregator;

            var testStart = new DateTime(2019, 12, 31, 23, 59, 59);
            var testEnd = new DateTime(2020, 1, 2, 0, 0, 0);
            var now = testStart;

            while (now <= testEnd)
            {
                await GenerateWebHookRecordAsync(now, statDataProvider, CancellationToken.None);
                if (now.Second == 0)
                {
                    var aggregationTime = now.AddSeconds(-1);

                    aggregator = CreateAggregator();
                    await aggregator.AggregateAsync(aggregationTime, TimeResolution.Minute, CancellationToken.None);
                    if (now.Minute == 0)
                    {
                        aggregator = CreateAggregator();
                        await aggregator.AggregateAsync(aggregationTime, TimeResolution.Hour,
                            CancellationToken.None);
                        if (now.Hour == 0)
                        {
                            aggregator = CreateAggregator();
                            await aggregator.AggregateAsync(aggregationTime, TimeResolution.Day,
                                CancellationToken.None);
                            if (now.Day == 1)
                            {
                                aggregator = CreateAggregator();
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
            var aggregations = new WebHookAggregation[resolutionCount][];
            for (int resolutionValue = 0; resolutionValue < resolutionCount; resolutionValue++)
            {
                aggregations[resolutionValue]= allAggregations
                    .Where(x => x.DataType == dt && x.Resolution == (TimeResolution)resolutionValue)
                    .Take(2)
                    .Select(x => DeserializeAggregation<WebHookAggregation>(x.Data))
                    .ToArray();

                // The first elements are truncated need to contain only one request per any resolution.
                Assert.AreEqual(1, aggregations[resolutionValue][0].CallCount);
            }
            // The additional items contain an amount of items corresponding to the resolution
            Assert.AreEqual(60, aggregations[(int)TimeResolution.Minute][1].CallCount);
            Assert.AreEqual(60 * 60, aggregations[(int)TimeResolution.Hour][1].CallCount);
            Assert.AreEqual(24 * 60 * 60, aggregations[(int)TimeResolution.Day][1].CallCount);
        }

        [TestMethod]
        public async STT.Task Stat_Aggregation_WebHook_Run_1month()
        {
            var statDataProvider = new TestStatisticalDataProvider();
            StatisticalDataAggregationController CreateAggregator()
            {
                return new StatisticalDataAggregationController(statDataProvider,
                    new[] { new WebHookStatisticalDataAggregator(GetOptions()) }, GetOptions(),
                    NullLoggerFactory.Instance.CreateLogger<StatisticalDataAggregationController>());
            }
            StatisticalDataAggregationController aggregator;

            var testStart = new DateTime(2019, 12, 31, 23, 59, 01);
            var testEnd = new DateTime(2020, 2, 1, 0, 0, 0);
            var now = testStart;

            while (now <= testEnd)
            {
                if (now.Second == 0)
                {
                    var aggregationTime = now.AddSeconds(-1);

                    aggregator = CreateAggregator();
                    await GenerateWebHookAggregationAsync(aggregationTime, TimeResolution.Minute, 60, statDataProvider);
                    if (now.Minute == 0)
                    {
                        aggregator = CreateAggregator();
                        await aggregator.AggregateAsync(aggregationTime, TimeResolution.Hour, CancellationToken.None);
                        if (now.Hour == 0)
                        {
                            aggregator = CreateAggregator();
                            await aggregator.AggregateAsync(aggregationTime, TimeResolution.Day, CancellationToken.None);
                            if (now.Day == 1)
                            {
                                aggregator = CreateAggregator();
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
            var aggregations = new WebHookAggregation[resolutionCount][];
            for (int resolutionValue = 0; resolutionValue < resolutionCount; resolutionValue++)
            {
                aggregations[resolutionValue] = allAggregations
                    .Where(x => x.DataType == dt && x.Resolution == (TimeResolution)resolutionValue)
                    .Take(2)
                    .Select(x => DeserializeAggregation<WebHookAggregation>(x.Data))
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
            StatisticalDataAggregationController CreateAggregator()
            {
                return new StatisticalDataAggregationController(statDataProvider,
                    new[] { new WebHookStatisticalDataAggregator(GetOptions()) }, GetOptions(),
                    NullLoggerFactory.Instance.CreateLogger<StatisticalDataAggregationController>());
            }
            StatisticalDataAggregationController aggregator;

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
                                aggregator = CreateAggregator();
                                await aggregator.AggregateAsync(aggregationTime, TimeResolution.Day, CancellationToken.None);
                                if (now.Day == 1)
                                {
                                    aggregator = CreateAggregator();
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
            var aggregations = new WebHookAggregation[resolutionCount][];
            // Start with 1 because the per-minute aggregation was not generated.
            for (int resolutionValue = 1; resolutionValue < resolutionCount; resolutionValue++)
            {
                aggregations[resolutionValue] = allAggregations
                    .Where(x => x.DataType == dt && x.Resolution == (TimeResolution)resolutionValue)
                    .Take(2)
                    .Select(x => DeserializeAggregation<WebHookAggregation>(x.Data))
                    .ToArray();
            }
            // Per-minute aggregations are skipped
            Assert.AreEqual(60 * 60, aggregations[(int)TimeResolution.Hour][0].CallCount);
            Assert.AreEqual(24 * 60 * 60, aggregations[(int)TimeResolution.Day][0].CallCount);
            // 2020 February had 29 days
            Assert.AreEqual(31 * 24 * 60 * 60, aggregations[(int)TimeResolution.Month][0].CallCount);

        }
        private async STT.Task GenerateWebHookAggregationAsync(DateTime date, TimeResolution resolution, int callCount,
            IStatisticalDataProvider statDataProvider)
        {
            var callCountPer10 = callCount / 10;

            var aggregation = new Aggregation
            {
                Date = date,
                DataType = "WebHook",
                Resolution = resolution,
                Data = SerializeAggregation(
                    new WebHookAggregation
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
            StatisticalDataAggregationController CreateAggregator()
            {
                return new StatisticalDataAggregationController(statDataProvider,
                    new[] {new WebHookStatisticalDataAggregator(GetOptions()) }, GetOptions(),
                    NullLoggerFactory.Instance.CreateLogger<StatisticalDataAggregationController>());
            }
            StatisticalDataAggregationController aggregator;
            

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

                        aggregator = CreateAggregator();
                        await aggregator.AggregateAsync(aggregationTime, TimeResolution.Minute, CancellationToken.None);
                        if (now.Minute == 0)
                        {
                            aggregator = CreateAggregator();
                            await aggregator.AggregateAsync(aggregationTime, TimeResolution.Hour, CancellationToken.None);
                            if (now.Hour == 0)
                            {
                                aggregator = CreateAggregator();
                                await aggregator.AggregateAsync(aggregationTime, TimeResolution.Day, CancellationToken.None);
                                if (now.Day == 1)
                                {
                                    aggregator = CreateAggregator();
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
            var aggregations = new WebHookAggregation[resolutionCount][];
            for (int resolutionValue = 0; resolutionValue < resolutionCount; resolutionValue++)
            {
                aggregations[resolutionValue] = allAggregations
                    .Where(x => x.DataType == dt && x.Resolution == (TimeResolution)resolutionValue)
                    .Take(2)
                    .Select(x => DeserializeAggregation<WebHookAggregation>(x.Data))
                    .ToArray();
            }
            Assert.AreEqual(1, aggregations[(int)TimeResolution.Minute][0].CallCount);
            Assert.AreEqual(1, aggregations[(int)TimeResolution.Hour][0].CallCount);
            Assert.AreEqual(1, aggregations[(int)TimeResolution.Day][0].CallCount);
            Assert.AreEqual(31, aggregations[(int)TimeResolution.Month][0].CallCount);

        }
        [TestMethod]
        public async STT.Task Stat_Aggregation_Mixed_Run_()
        {
            var statDataProvider = new TestStatisticalDataProvider();
            StatisticalDataAggregationController CreateAggregator()
            {
                return new StatisticalDataAggregationController(statDataProvider,
                    new IStatisticalDataAggregator[]
                    {
                        new WebHookStatisticalDataAggregator(GetOptions()),
                        new WebTransferStatisticalDataAggregator(GetOptions()),
                        new DatabaseUsageStatisticalDataAggregator(GetOptions())
                    }, GetOptions(),
                    NullLoggerFactory.Instance.CreateLogger<StatisticalDataAggregationController>());
            }
            StatisticalDataAggregationController aggregator;


            var testStart = new DateTime(2020, 1, 1, 0, 0, 1);
            var testEnd = new DateTime(2020, 2, 1, 0, 0, 0);
            var now = testStart;

            while (now <= testEnd)
            {
                if (now.Second == 0)
                {
                    // Generate two api call and a webhook call every day at ten.
                    if (now.Hour == 10 && now.Minute == 0 && now.Second == 0)
                    {
                        await GenerateWebTransferRecordAsync(now, statDataProvider, CancellationToken.None);
                        await GenerateWebTransferRecordAsync(now.AddSeconds(1), statDataProvider, CancellationToken.None);
                        await GenerateWebHookRecordAsync(now.AddSeconds(2), statDataProvider, CancellationToken.None);
                    }
                    // Generate db usage in every hour.
                    if (now.Minute == 0 && now.Second == 0)
                    {
                        await GenerateDbUsageAggregationAsync(now, TimeResolution.Hour, statDataProvider, CancellationToken.None);
                    }

                    if (now.Second == 0)
                    {
                        var aggregationTime = now.AddSeconds(-1);

                        aggregator = CreateAggregator();
                        await aggregator.AggregateAsync(aggregationTime, TimeResolution.Minute, CancellationToken.None);
                        if (now.Minute == 0)
                        {
                            aggregator = CreateAggregator();
                            await aggregator.AggregateAsync(aggregationTime, TimeResolution.Hour, CancellationToken.None);
                            if (now.Hour == 0)
                            {
                                aggregator = CreateAggregator();
                                await aggregator.AggregateAsync(aggregationTime, TimeResolution.Day, CancellationToken.None);
                                if (now.Day == 1)
                                {
                                    aggregator = CreateAggregator();
                                    await aggregator.AggregateAsync(aggregationTime, TimeResolution.Month, CancellationToken.None);
                                }
                            }
                        }
                    }
                }
                now = now.AddSeconds(1);
            }

            var allAggregations = statDataProvider.Aggregations;
            Assert.AreEqual(62, allAggregations.Count(x => x.Resolution == TimeResolution.Minute));
            Assert.AreEqual(31 * 24 + 62, allAggregations.Count(x => x.Resolution == TimeResolution.Hour));
            Assert.AreEqual(93, allAggregations.Count(x => x.Resolution == TimeResolution.Day));
            Assert.AreEqual(3, allAggregations.Count(x => x.Resolution == TimeResolution.Month));

            var dt1 = "WebHook";
            var resolutionCount1 = Enum.GetValues(typeof(TimeResolution)).Length;
            var aggregations1 = new WebHookAggregation[resolutionCount1][];
            for (int resolutionValue = 0; resolutionValue < resolutionCount1; resolutionValue++)
            {
                aggregations1[resolutionValue] = allAggregations
                    .Where(x => x.DataType == dt1 && x.Resolution == (TimeResolution)resolutionValue)
                    .Take(2)
                    .Select(x => DeserializeAggregation<WebHookAggregation>(x.Data))
                    .ToArray();
            }
            Assert.AreEqual(1, aggregations1[(int)TimeResolution.Minute][0].CallCount);
            Assert.AreEqual(1, aggregations1[(int)TimeResolution.Hour][0].CallCount);
            Assert.AreEqual(1, aggregations1[(int)TimeResolution.Day][0].CallCount);
            Assert.AreEqual(31, aggregations1[(int)TimeResolution.Month][0].CallCount);

            var dt2 = "WebTransfer";
            var resolutionCount2 = Enum.GetValues(typeof(TimeResolution)).Length;
            var aggregations2 = new WebTransferStatisticalDataAggregator.WebTransferAggregation[resolutionCount2][];
            for (int resolutionValue = 0; resolutionValue < resolutionCount2; resolutionValue++)
            {
                aggregations2[resolutionValue] = allAggregations
                    .Where(x => x.DataType == dt2 && x.Resolution == (TimeResolution)resolutionValue)
                    .Take(2)
                    .Select(x => DeserializeAggregation<WebTransferStatisticalDataAggregator.WebTransferAggregation>(x.Data))
                    .ToArray();
            }
            Assert.AreEqual(2, aggregations2[(int)TimeResolution.Minute][0].CallCount);
            Assert.AreEqual(2, aggregations2[(int)TimeResolution.Hour][0].CallCount);
            Assert.AreEqual(2, aggregations2[(int)TimeResolution.Day][0].CallCount);
            Assert.AreEqual(62, aggregations2[(int)TimeResolution.Month][0].CallCount);

            var dt3 = "DatabaseUsage";
            var resolutionCount3 = Enum.GetValues(typeof(TimeResolution)).Length;
            var aggregations3 = new string[resolutionCount3][];
            for (int resolutionValue = 0; resolutionValue < resolutionCount3; resolutionValue++)
            {
                aggregations3[resolutionValue] = allAggregations
                    .Where(x => x.DataType == dt3 && x.Resolution == (TimeResolution)resolutionValue)
                    .Take(2)
                    .Select(x => (x.Data))
                    .ToArray();
            }

            var dbUsageAverage = RemoveWhitespaces(SerializeAggregation(new DatabaseUsage
                {
                    Content = new Dimensions         {Count = 101, Blob = 1011, Metadata = 1021, Text = 1031, Index = 1041},
                    OldVersions = new Dimensions     {Count = 111, Blob = 1111, Metadata = 1121, Text = 1131, Index = 1141},
                    Preview = new Dimensions         {Count = 121, Blob = 1211, Metadata = 1221, Text = 1231, Index = 1241},
                    System = new Dimensions          {Count = 131, Blob = 1311, Metadata = 1321, Text = 1331, Index = 1341},
                    OperationLog = new LogDimensions {Count = 141,              Metadata = 1421, Text = 1431},
                    OrphanedBlobs = 43,
                }));
            // Per minute and hourly aggregations are skipped
            Assert.AreEqual(dbUsageAverage, RemoveWhitespaces(aggregations3[(int)TimeResolution.Day][0]));
            Assert.AreEqual(dbUsageAverage, RemoveWhitespaces(aggregations3[(int)TimeResolution.Month][0]));
        }
        [TestMethod]
        public async STT.Task Stat_Aggregation_Mixed_Run_MaintenanceTaskAlgorithm()
        {
            var statDataProvider = new TestStatisticalDataProvider();

            StatisticalDataAggregationController controller = new StatisticalDataAggregationController(statDataProvider,
                new IStatisticalDataAggregator[]
                {
                    new WebHookStatisticalDataAggregator(GetOptions()),
                    new WebTransferStatisticalDataAggregator(GetOptions()),
                    new DatabaseUsageStatisticalDataAggregator(GetOptions())
                }, GetOptions(),
                NullLoggerFactory.Instance.CreateLogger<StatisticalDataAggregationController>());
            var maintenanceTask = new StatisticalDataAggregationMaintenanceTask(controller);
            var maintenanceTaskAcc = new ObjectAccessor(maintenanceTask);

            using (new Swindler<List<ISnTracer>>(
                new List<ISnTracer> { new SnDebugViewTracer() },
                () => SnTrace.SnTracers,
                value => { SnTrace.SnTracers.Clear(); SnTrace.SnTracers.AddRange(value); }))
            {
                SnTrace.Custom.Enabled = true;

                var testStart = new DateTime(2020, 1, 1, 0, 0, 1);
                var testEnd = new DateTime(2020, 2, 1, 0, 0, 0);
                var now = testStart;

                while (now <= testEnd)
                {
                    if (now.Second == 0)
                    {
                        // Generate two api call and a webhook call every day at ten.
                        if (now.Hour == 10 && now.Minute == 0 && now.Second == 0)
                        {
                            await GenerateWebTransferRecordAsync(now, statDataProvider, CancellationToken.None);
                            await GenerateWebTransferRecordAsync(now.AddSeconds(1), statDataProvider, CancellationToken.None);
                            await GenerateWebHookRecordAsync(now.AddSeconds(2), statDataProvider, CancellationToken.None);
                        }
                        // Generate db usage in every hour.
                        if (now.Minute == 0 && now.Second == 0)
                        {
                            await GenerateDbUsageAggregationAsync(now, TimeResolution.Hour, statDataProvider, CancellationToken.None);
                        }

                        if (now.Second == 0)
                        {
                            await (STT.Task)maintenanceTaskAcc.Invoke("ExecuteAsync", now, CancellationToken.None);
                        }
                    }
                    now = now.AddSeconds(1);
                }

                var allAggregations = statDataProvider.Aggregations;
                Assert.AreEqual(62, allAggregations.Count(x => x.Resolution == TimeResolution.Minute));
                Assert.AreEqual(31 * 24 + 62, allAggregations.Count(x => x.Resolution == TimeResolution.Hour));
                Assert.AreEqual(93, allAggregations.Count(x => x.Resolution == TimeResolution.Day));
                Assert.AreEqual(3, allAggregations.Count(x => x.Resolution == TimeResolution.Month));

                var dt1 = "WebHook";
                var resolutionCount1 = Enum.GetValues(typeof(TimeResolution)).Length;
                var aggregations1 = new WebHookAggregation[resolutionCount1][];
                for (int resolutionValue = 0; resolutionValue < resolutionCount1; resolutionValue++)
                {
                    aggregations1[resolutionValue] = allAggregations
                        .Where(x => x.DataType == dt1 && x.Resolution == (TimeResolution)resolutionValue)
                        .Take(2)
                        .Select(x => DeserializeAggregation<WebHookAggregation>(x.Data))
                        .ToArray();
                }
                Assert.AreEqual(1, aggregations1[(int)TimeResolution.Minute][0].CallCount);
                Assert.AreEqual(1, aggregations1[(int)TimeResolution.Hour][0].CallCount);
                Assert.AreEqual(1, aggregations1[(int)TimeResolution.Day][0].CallCount);
                Assert.AreEqual(31, aggregations1[(int)TimeResolution.Month][0].CallCount);

                var dt2 = "WebTransfer";
                var resolutionCount2 = Enum.GetValues(typeof(TimeResolution)).Length;
                var aggregations2 = new WebTransferStatisticalDataAggregator.WebTransferAggregation[resolutionCount2][];
                for (int resolutionValue = 0; resolutionValue < resolutionCount2; resolutionValue++)
                {
                    aggregations2[resolutionValue] = allAggregations
                        .Where(x => x.DataType == dt2 && x.Resolution == (TimeResolution)resolutionValue)
                        .Take(2)
                        .Select(x => DeserializeAggregation<WebTransferStatisticalDataAggregator.WebTransferAggregation>(x.Data))
                        .ToArray();
                }
                Assert.AreEqual(2, aggregations2[(int)TimeResolution.Minute][0].CallCount);
                Assert.AreEqual(2, aggregations2[(int)TimeResolution.Hour][0].CallCount);
                Assert.AreEqual(2, aggregations2[(int)TimeResolution.Day][0].CallCount);
                Assert.AreEqual(62, aggregations2[(int)TimeResolution.Month][0].CallCount);

                var dt3 = "DatabaseUsage";
                var resolutionCount3 = Enum.GetValues(typeof(TimeResolution)).Length;
                var aggregations3 = new string[resolutionCount3][];
                for (int resolutionValue = 0; resolutionValue < resolutionCount3; resolutionValue++)
                {
                    aggregations3[resolutionValue] = allAggregations
                        .Where(x => x.DataType == dt3 && x.Resolution == (TimeResolution)resolutionValue)
                        .Take(2)
                        .Select(x => (x.Data))
                        .ToArray();
                }

                var dbUsageAverage = RemoveWhitespaces(SerializeAggregation(new DatabaseUsage
                {
                    Content = new Dimensions { Count = 101, Blob = 1011, Metadata = 1021, Text = 1031, Index = 1041 },
                    OldVersions = new Dimensions { Count = 111, Blob = 1111, Metadata = 1121, Text = 1131, Index = 1141 },
                    Preview = new Dimensions { Count = 121, Blob = 1211, Metadata = 1221, Text = 1231, Index = 1241 },
                    System = new Dimensions { Count = 131, Blob = 1311, Metadata = 1321, Text = 1331, Index = 1341 },
                    OperationLog = new LogDimensions { Count = 141, Metadata = 1421, Text = 1431 },
                    OrphanedBlobs = 43,
                }));
                // Per minute and hourly aggregations are skipped
                Assert.AreEqual(dbUsageAverage, RemoveWhitespaces(aggregations3[(int)TimeResolution.Day][0]));
                Assert.AreEqual(dbUsageAverage, RemoveWhitespaces(aggregations3[(int)TimeResolution.Month][0]));

            }
        }
        //[TestMethod]
        public async STT.Task Stat_Aggregation_Mixed_Run_MaintenanceTaskAlgorithm_SQL()
        {
            Assert.Fail();

            ConnectionStrings.ConnectionString = "Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=SenseNet.IntegrationTests;Data Source=SNPC007\\SQL2016";
            var statDataProvider = new MsSqlStatisticalDataProvider();

            StatisticalDataAggregationController controller = new StatisticalDataAggregationController(statDataProvider,
                new IStatisticalDataAggregator[]
                {
                    new WebHookStatisticalDataAggregator(GetOptions()),
                    new WebTransferStatisticalDataAggregator(GetOptions()),
                    new DatabaseUsageStatisticalDataAggregator(GetOptions())
                }, GetOptions(),
                NullLoggerFactory.Instance.CreateLogger<StatisticalDataAggregationController>());
            var maintenanceTask = new StatisticalDataAggregationMaintenanceTask(controller);
            var maintenanceTaskAcc = new ObjectAccessor(maintenanceTask);

            using (new Swindler<List<ISnTracer>>(
                new List<ISnTracer> {new SnDebugViewTracer()},
                () => SnTrace.SnTracers,
                value => { SnTrace.SnTracers.Clear(); SnTrace.SnTracers.AddRange(value); }))
            {
                SnTrace.Custom.Enabled = true;
                var testStart = new DateTime(2020, 1, 1, 0, 0, 1);
                var testEnd = new DateTime(2020, 2, 1, 0, 0, 0);
                var now = testStart;

                while (now <= testEnd)
                {
                    if (now.Second == 10)
                    {
                        await GenerateWebTransferRecordAsync(now, statDataProvider, CancellationToken.None);
                    }
                    // call task in every minute
                    if (now.Second == 0)
                    {
                        await (STT.Task)maintenanceTaskAcc.Invoke("ExecuteAsync", now, CancellationToken.None);
                    }
                    now = now.AddSeconds(1);
                }
            }

            Assert.Inconclusive();
        }

        private async STT.Task GenerateWebHookRecordAsync(DateTime date, TestStatisticalDataProvider statDataProvider, CancellationToken cancel)
        {
            var count = statDataProvider.Storage.Count;
            var error = (count % 10) == 0;
            var warning = (count % 10) == 1;
            var record = new StatisticalDataRecord
            {
                DataType = "WebHook",
                TargetId = 1242,
                ContentId = 1342,
                EventName = "Event42",
                Url = "POST https://example.com/api/hook",
                RequestLength = 100,
                ResponseLength = 1000,
                CreationTime = date,
                Duration = TimeSpan.FromMilliseconds(100),
                ResponseStatusCode = error ? 500 : (warning ? 400 : 200),
                ErrorMessage = error ? "ErrorMessage1" : (warning ? "WarningMessage" : null)
            };

            await statDataProvider.WriteDataAsync(record, cancel);
        }
        private async STT.Task GenerateWebTransferRecordAsync(DateTime date, IStatisticalDataProvider statDataProvider, CancellationToken cancel)
        {
            var record = new StatisticalDataRecord
            {
                DataType = "WebTransfer",
                Url = "GET https://example.com/api",
                RequestLength = 100,
                ResponseLength = 1000,
                CreationTime = date,
                Duration = TimeSpan.FromMilliseconds(100),
                ResponseStatusCode = 200,
            };

            await statDataProvider.WriteDataAsync(record, cancel);
        }
        private async STT.Task GenerateDbUsageAggregationAsync(DateTime date, TimeResolution resolution, TestStatisticalDataProvider statDataProvider,
            CancellationToken cancel)
        {
            DatabaseUsage data;
            if (date.Hour % 2 == 0)
            {
                data = new DatabaseUsage
                {
                    Content = new Dimensions         {Count = 100, Blob = 1010, Metadata = 1020, Text = 1030, Index = 1040},
                    OldVersions = new Dimensions     {Count = 110, Blob = 1110, Metadata = 1120, Text = 1130, Index = 1140},
                    Preview = new Dimensions         {Count = 120, Blob = 1210, Metadata = 1220, Text = 1230, Index = 1240},
                    System = new Dimensions          {Count = 130, Blob = 1310, Metadata = 1320, Text = 1330, Index = 1340},
                    OperationLog = new LogDimensions {Count = 140,              Metadata = 1420, Text = 1430},
                    OrphanedBlobs = 42,
                    Executed = date.AddMinutes(-5),
                    ExecutionTime = TimeSpan.FromSeconds(0.5)
                };
            }
            else
            {
                data = new DatabaseUsage
                {
                    Content = new Dimensions         {Count = 102, Blob = 1012, Metadata = 1022, Text = 1032, Index = 1042},
                    OldVersions = new Dimensions     {Count = 112, Blob = 1112, Metadata = 1122, Text = 1132, Index = 1142},
                    Preview = new Dimensions         {Count = 122, Blob = 1212, Metadata = 1222, Text = 1232, Index = 1242},
                    System = new Dimensions          {Count = 132, Blob = 1312, Metadata = 1322, Text = 1332, Index = 1342},
                    OperationLog = new LogDimensions {Count = 142,              Metadata = 1422, Text = 1432},
                    OrphanedBlobs = 44,
                    Executed = date.AddMinutes(-5),
                    ExecutionTime = TimeSpan.FromSeconds(0.5)
                };
            }

            var aggregation = new Aggregation
            {
                DataType = "DatabaseUsage",
                Date = date.Truncate(resolution),
                Resolution = resolution,
                Data = SerializeAggregation(data)
            };

            await statDataProvider.WriteAggregationAsync(aggregation, cancel);
        }

        private string SerializeAggregation(object obj)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
                JsonSerializer.Create().Serialize(writer, obj);
            return sb.ToString();
        }
        private T DeserializeAggregation<T>(string src)
        {
            using (var reader = new StringReader(src))
                return JsonSerializer.Create().Deserialize<T>(new JsonTextReader(reader));
        }

        private IOptions<StatisticsOptions> _options;
        private IOptions<StatisticsOptions> GetOptions()
        {
            return _options ??= new OptionsWrapper<StatisticsOptions>(new StatisticsOptions());
        }
        #endregion

        #region /* ========================================================================= Configuration tests */

        [TestMethod]
        public void Stat_Config()
        {
            var services = new ServiceCollection()
                .AddOptions<StatisticsOptions>()
                .Services.BuildServiceProvider();

            var statOptions = services.GetService<IOptions<StatisticsOptions>>().Value;

            Assert.AreEqual(3, statOptions.Retention.ApiCalls.Momentary);
        }

        #endregion

        #region /* ========================================================================= OData tests */

        [TestMethod]
        public async STT.Task Stat_OData_GetApiUsageList()
        {
            var services = new ServiceCollection()
                .AddTransient<WebTransferRegistrator>()
                .AddStatisticalDataProvider<InMemoryStatisticalDataProvider>()
                .BuildServiceProvider();

            await ODataTestAsync(builder =>
            {
            }, async () =>
            {
                var sdp = services.GetService<IStatisticalDataProvider>();
                for (var i = 20; i > 0; i--)
                {
                    var time1 = DateTime.UtcNow.AddDays(-i * 0.25);
                    var time2 = time1.AddSeconds(0.9);

                    var warning = i % 5 == 4;
                    var error = i % 7 == 6;

                    var message = error ? "Error message" : (warning ? "Warning message" : null);
                    var statusCode = error ? 500 : (warning ? 400 : 200);

                    var input = new WebTransferStatInput
                    {
                        Url = $"https://example.com/hook/{(char)('A'+i)}",
                        HttpMethod = "POST",
                        RequestTime = time1,
                        ResponseTime = time2,
                        RequestLength = 100 + 1,
                        ResponseLength = 1000 + 10 * i,
                        ResponseStatusCode = statusCode,
                    };
                    var record = new InputStatisticalDataRecord(input);
                    await sdp.WriteDataAsync(record, CancellationToken.None).ConfigureAwait(false);
                }

                // ACTION-1 first time window.
                var response1 = await ODataGetAsync($"/OData.svc/('Root')/GetApiUsageList", "", services)
                    .ConfigureAwait(false);
                var lastTimeStr1 = GetLastCreationTime(response1);
                var response2 = await ODataGetAsync($"/OData.svc/('Root')/GetApiUsageList",
                        $"?maxTime={lastTimeStr1}&count=5", services)
                    .ConfigureAwait(false);
                var lastTimeStr2 = GetLastCreationTime(response2);
                var response3 = await ODataGetAsync($"/OData.svc/('Root')/GetApiUsageList",
                        $"?maxTime={lastTimeStr2}", services)
                    .ConfigureAwait(false);

                // ASSERT

                var items1 = JsonSerializer.Create()
                    .Deserialize<ApiUsageListItemViewModel[]>(new JsonTextReader(new StringReader(response1)));
                Assert.AreEqual(10, items1.Length);
                var items2 = JsonSerializer.Create()
                    .Deserialize<ApiUsageListItemViewModel[]>(new JsonTextReader(new StringReader(response2)));
                Assert.AreEqual(5, items2.Length);
                var items3 = JsonSerializer.Create()
                    .Deserialize<ApiUsageListItemViewModel[]>(new JsonTextReader(new StringReader(response3)));
                Assert.AreEqual(5, items3.Length);

                AssertSequenceEqual(
                    Enumerable.Range(1, 20),
                    items1.Union(items2.Union(items3)).Select(x => x.Url.Last()-'A'));

            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task Stat_OData_GetApiUsagePeriod()
        {
            var services = new ServiceCollection()
                .AddTransient<WebTransferRegistrator>()
                .AddStatisticalDataProvider<InMemoryStatisticalDataProvider>()
                .BuildServiceProvider();

            await ODataTestAsync(builder =>
            {
            }, async () =>
            {
                var now = DateTime.UtcNow;
                var testEnd = now.Truncate(TimeResolution.Month).AddMonths(1);
                var testStart = testEnd.AddYears(-1);
                var dataProvider = services.GetService<IStatisticalDataProvider>();
                await GenerateApiCallDataForODataTests(dataProvider, testStart, testEnd, now);

                // ACTION-1 
                var response1 = await ODataGetAsync($"/OData.svc/('Root')/GetApiUsagePeriod",
                    "", services).ConfigureAwait(false);

                // ASSERT-1
                var result1 = (JObject)JsonSerializer.CreateDefault().Deserialize(new JsonTextReader(new StringReader(response1)));
                Assert.AreEqual("WebTransfer", result1["DataType"].Value<string>());
                var start = testEnd.AddMonths(-1);
                var end = testEnd;
                Assert.AreEqual(start, result1["Start"].Value<DateTime>());
                Assert.AreEqual(testEnd, result1["End"].Value<DateTime>());
                Assert.AreEqual("Month", result1["TimeWindow"].Value<string>());
                Assert.AreEqual("Day", result1["Resolution"].Value<string>());
                var days = end.AddDays(-1).Day;
                Assert.AreEqual(days, ((JArray)result1["CallCount"]).Count);
                Assert.AreEqual(86400L, ((JArray)result1["CallCount"]).First().Value<long>());
                Assert.AreEqual(days, ((JArray)result1["RequestLengths"]).Count);
                Assert.AreEqual(8640000L, ((JArray)result1["RequestLengths"]).First().Value<long>());
                Assert.AreEqual(days, ((JArray)result1["ResponseLengths"]).Count);
                Assert.AreEqual(86400000L, ((JArray)result1["ResponseLengths"]).First().Value<long>());

                // ACTION-2
                var startTime2 = now.AddMonths(-1).ToString("yyyy-MM-dd HH:mm:ss");
                var response2 = await ODataGetAsync($"/OData.svc/('Root')/GetApiUsagePeriod",
                    $"?time={startTime2}", services).ConfigureAwait(false);

                // ASSERT-2
                var result2 = (JObject)JsonSerializer.CreateDefault().Deserialize(new JsonTextReader(new StringReader(response2)));
                Assert.AreEqual("WebTransfer", result2["DataType"].Value<string>());
                start = start.AddMonths(-1);
                end = end.AddMonths(-1);
                Assert.AreEqual(start, result2["Start"].Value<DateTime>());
                Assert.AreEqual(end, result2["End"].Value<DateTime>());
                Assert.AreEqual("Month", result2["TimeWindow"].Value<string>());
                Assert.AreEqual("Day", result2["Resolution"].Value<string>());
                days = end.AddDays(-1).Day;
                Assert.AreEqual(days, ((JArray)result2["CallCount"]).Count);
                Assert.AreEqual(86400L, ((JArray)result2["CallCount"]).First().Value<long>());
                Assert.AreEqual(days, ((JArray)result2["RequestLengths"]).Count);
                Assert.AreEqual(8640000L, ((JArray)result2["RequestLengths"]).First().Value<long>());
                Assert.AreEqual(days, ((JArray)result2["ResponseLengths"]).Count);
                Assert.AreEqual(86400000L, ((JArray)result2["ResponseLengths"]).First().Value<long>());

                // ACTION-3
                var response3 = await ODataGetAsync($"/OData.svc/('Root')/GetApiUsagePeriod",
                    "?timewindow=year", services).ConfigureAwait(false);

                // ASSERT-3
                var result3 = (JObject)JsonSerializer.CreateDefault().Deserialize(new JsonTextReader(new StringReader(response3)));
                Assert.AreEqual("WebTransfer", result3["DataType"].Value<string>());
                var start1 = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var end1 = new DateTime(now.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                Assert.AreEqual(start1, result3["Start"].Value<DateTime>());
                Assert.AreEqual(end1, result3["End"].Value<DateTime>());
                Assert.AreEqual("Year", result3["TimeWindow"].Value<string>());
                Assert.AreEqual("Month", result3["Resolution"].Value<string>());
                Assert.AreEqual(12, ((JArray)result3["CallCount"]).Count);
                Assert.AreEqual(86400L * 31, ((JArray)result3["CallCount"]).First().Value<long>());
                Assert.AreEqual(12, ((JArray)result3["RequestLengths"]).Count);
                Assert.AreEqual(8640000L * 31, ((JArray)result3["RequestLengths"]).First().Value<long>());
                Assert.AreEqual(12, ((JArray)result3["ResponseLengths"]).Count);
                Assert.AreEqual(86400000L * 31, ((JArray)result3["ResponseLengths"]).First().Value<long>());

                // ACTION-4
                var startTime = now.AddMonths(-2).ToString("yyyy-MM-dd HH:mm:ss");
                var response4 = await ODataGetAsync($"/OData.svc/('Root')/GetApiUsagePeriod",
                    $"?timewindow=month&time={startTime}", services).ConfigureAwait(false);

                // ASSERT-4
                var result4 = (JObject)JsonSerializer.CreateDefault().Deserialize(new JsonTextReader(new StringReader(response4)));
                Assert.AreEqual("WebTransfer", result4["DataType"].Value<string>());
                var start2 = testEnd.AddMonths(-3);
                var end2 = testEnd.AddMonths(-2);
                Assert.AreEqual(start2, result4["Start"].Value<DateTime>());
                Assert.AreEqual(end2, result4["End"].Value<DateTime>());
                Assert.AreEqual("Month", result4["TimeWindow"].Value<string>());
                Assert.AreEqual("Day", result4["Resolution"].Value<string>());

            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task Stat_OData_GetApiUsagePeriods()
        {
            var services = new ServiceCollection()
                .AddTransient<WebTransferRegistrator>()
                .AddStatisticalDataProvider<InMemoryStatisticalDataProvider>()
                .BuildServiceProvider();

            await ODataTestAsync(builder =>
            {
                builder.UseStatisticalDataProvider(new InMemoryStatisticalDataProvider());
            }, async () =>
            {
                async STT.Task<string> GetApiUsagePeriods(DateTime now, TimeWindow window)
                {
                    var content = Content.Create(Repository.Root);
                    var httpContext = CreateHttpContext("/OData.svc/('Root')/GetApiUsagePeriods", "", services);
                    var result = await StatisticsController.GetApiUsagePeriods(content, httpContext, now, window);
                    var sb = new StringBuilder();
                    var serializer = new JsonSerializer();
                    serializer.Serialize(new JsonTextWriter(new StringWriter(sb)), result);
                    return sb.ToString();
                }


                var now = new DateTime(2021, 6, 15, 3, 18, 28);
                var testEnd = now.Truncate(TimeResolution.Month).AddMonths(1);
                var testStart = testEnd.AddYears(-1);
                var statDp = services.GetService<IStatisticalDataProvider>();
                await GenerateApiCallDataForODataTests(statDp, testStart, testEnd, now);

                // ACTIONS
                // /OData.svc/('Root')/GetApiUsagePeriods?timewindow=hour
                var responseHour = await GetApiUsagePeriods(now, TimeWindow.Hour);
                // /OData.svc/('Root')/GetApiUsagePeriods?timewindow=day
                var responseDay = await GetApiUsagePeriods(now, TimeWindow.Day);
                // /OData.svc/('Root')/GetApiUsagePeriods?timewindow=month
                var responseMonth = await GetApiUsagePeriods(now, TimeWindow.Month);
                // /OData.svc/('Root')/GetApiUsagePeriods?timewindow=year
                var responseYear = await GetApiUsagePeriods(now, TimeWindow.Year);

                // ASSERTS
                Assert.AreEqual(
                    "{\"Window\":\"Hour\",\"Resolution\":\"Minute\"," +
                    "\"First\":\"0001-01-01T00:00:00\",\"Last\":\"0001-01-01T00:00:00\",\"Count\":0}",
                    RemoveWhitespaces(responseHour));
                Assert.AreEqual(
                    "{\"Window\":\"Day\",\"Resolution\":\"Hour\"," +
                    "\"First\":\"2021-06-13T00:00:00Z\",\"Last\":\"2021-06-15T00:00:00Z\",\"Count\":3}",
                    RemoveWhitespaces(responseDay));
                Assert.AreEqual(
                    "{\"Window\":\"Month\",\"Resolution\":\"Day\"," +
                    "\"First\":\"2021-04-01T00:00:00Z\",\"Last\":\"2021-06-01T00:00:00Z\",\"Count\":3}",
                    RemoveWhitespaces(responseMonth));
                Assert.AreEqual(
                    "{\"Window\":\"Year\",\"Resolution\":\"Month\"," +
                    "\"First\":\"2021-01-01T00:00:00Z\",\"Last\":\"2021-01-01T00:00:00Z\",\"Count\":1}",
                    RemoveWhitespaces(responseYear));
            }).ConfigureAwait(false);
        }
        private async STT.Task GenerateApiCallDataForODataTests(IStatisticalDataProvider statDataProvider,
            DateTime testStart, DateTime testEnd, DateTime now)
        {
            StatisticalDataAggregationController CreateAggregator()
            {
                return new StatisticalDataAggregationController(statDataProvider,
                    new[] { new WebTransferStatisticalDataAggregator(GetOptions()) }, GetOptions(),
                    NullLoggerFactory.Instance.CreateLogger<StatisticalDataAggregationController>());
            }
            StatisticalDataAggregationController aggregator;

            var time = testStart;
            while (time <= now)
            {
                if (time.Second == 0)
                {
                    if (time.Second == 0)
                    {
                        var aggregationTime = time.AddSeconds(-1);

                        if (time.Minute == 0)
                        {
                            await GenerateWebTransferAggregationAsync(aggregationTime, TimeResolution.Hour, 60 * 60, statDataProvider);
                            // Does not aggregate but cleans up.
                            aggregator = CreateAggregator();
                            await aggregator.AggregateAsync(aggregationTime, TimeResolution.Hour, CancellationToken.None);
                            if (time.Hour == 0)
                            {
                                aggregator = CreateAggregator();
                                await aggregator.AggregateAsync(aggregationTime, TimeResolution.Day, CancellationToken.None);
                                if (time.Day == 1)
                                {
                                    aggregator = CreateAggregator();
                                    await aggregator.AggregateAsync(aggregationTime, TimeResolution.Month, CancellationToken.None);
                                }
                            }
                        }
                    }
                }
                time = time.AddSeconds(1);
            }
        }
        private async STT.Task GenerateWebTransferAggregationAsync(DateTime date, TimeResolution resolution, int callCount,
            IStatisticalDataProvider statDataProvider)
        {
            var callCountPer10 = callCount / 10;

            var aggregation = new Aggregation
            {
                Date = date,
                DataType = "WebTransfer",
                Resolution = resolution,
                Data = SerializeAggregation(
                    new WebTransferStatisticalDataAggregator.WebTransferAggregation
                    {
                        CallCount = callCount,
                        RequestLengths = callCount * 100,
                        ResponseLengths = callCount * 1000,
                    })
            };

            await statDataProvider.WriteAggregationAsync(aggregation, CancellationToken.None);
        }


        [TestMethod]
        public async STT.Task Stat_OData_GetWebHookUsageList()
        {
            var services = new ServiceCollection()
                .AddTransient<WebTransferRegistrator>()
                .AddStatisticalDataProvider<InMemoryStatisticalDataProvider>()
                .BuildServiceProvider();

            // This test exploits a side effect: if there is no any WebHookSubscription content, the "relatedTargetIds" filter
            // is ignored in the DataProvider's LoadUsageListAsync method.
            await ODataTestAsync(builder =>
            {
            }, async () =>
            {
                var sdp = services.GetService<IStatisticalDataProvider>();
                for (var i = 20; i > 0; i--)
                {
                    var time1 = DateTime.UtcNow.AddDays(-i * 0.25);
                    var time2 = time1.AddSeconds(0.9);

                    var warning = i % 5 == 4;
                    var error = i % 7 == 6;

                    var message = error ? "Error message" : (warning ? "Warning message" : null);
                    var statusCode = error ? 500 : (warning ? 400 : 200);

                    var input = new WebHookStatInput
                    {
                        Url = $"https://example.com/hook/{(i % 5) + 1}",
                        HttpMethod = "POST",
                        RequestTime = time1,
                        ResponseTime = time2,
                        RequestLength = 100 + 1,
                        ResponseLength = 1000 + 10 * i,
                        ResponseStatusCode = statusCode,
                        WebHookId = 1242,
                        ContentId = 10000 + i,
                        EventName = $"Event{(i % 4) + 1}",
                        ErrorMessage = message,
                        Payload = new { name1 = "value1", name2 = "value2" }
                    };
                    var record = new InputStatisticalDataRecord(input);
                    await sdp.WriteDataAsync(record, CancellationToken.None).ConfigureAwait(false);
                }
                
                // ACTION-1 first time window.
                var response1 = await ODataGetAsync($"/OData.svc/('Root')/GetWebHookUsageList", "", services)
                    .ConfigureAwait(false);
                var lastTimeStr1 = GetLastCreationTime(response1);
                var response2 = await ODataGetAsync($"/OData.svc/('Root')/GetWebHookUsageList",
                        $"?maxTime={lastTimeStr1}&count=5", services)
                    .ConfigureAwait(false);
                var lastTimeStr2 = GetLastCreationTime(response2);
                var response3 = await ODataGetAsync($"/OData.svc/('Root')/GetWebHookUsageList",
                        $"?maxTime={lastTimeStr2}", services)
                    .ConfigureAwait(false);

                // ASSERT

                var items1 = JsonSerializer.Create()
                    .Deserialize<WebHookUsageListItemViewModel[]>(new JsonTextReader(new StringReader(response1)));
                Assert.AreEqual(10, items1.Length);
                var items2 = JsonSerializer.Create()
                    .Deserialize<WebHookUsageListItemViewModel[]>(new JsonTextReader(new StringReader(response2)));
                Assert.AreEqual(5, items2.Length);
                var items3 = JsonSerializer.Create()
                    .Deserialize<WebHookUsageListItemViewModel[]>(new JsonTextReader(new StringReader(response3)));
                Assert.AreEqual(5, items3.Length);

                AssertSequenceEqual(
                    Enumerable.Range(10001, 20),
                    items1.Union(items2.Union(items3)).Select(x => x.ContentId));

            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task Stat_OData_GetPermittedWebHookUsageList()
        {
            var services = new ServiceCollection()
                .AddTransient<WebTransferRegistrator>()
                .AddStatisticalDataProvider<InMemoryStatisticalDataProvider>()
                .BuildServiceProvider();

            await ODataTestAsync(1, builder =>
            {
                builder.UseComponent(new WebHookComponent());
            }, async () =>
            {
                var webHooks = CreateWebHooks(3);
                var denied = webHooks[2];
                using (new SystemAccount())
                {
                    SecurityHandler.CreateAclEditor()
                        .BreakInheritance(denied.Id, new EntryType[0])
                        .Apply();
                }

                var sdp = services.GetService<IStatisticalDataProvider>();
                for (var i = 20; i > 0; i--)
                {
                    var time1 = DateTime.UtcNow.AddMinutes(-i - 1);
                    var time2 = time1.AddSeconds(0.9);

                    int webHookId = 0;
                    switch (i % 4)
                    {
                        case 0: webHookId = webHooks[0].Id; break;
                        case 1: webHookId = webHooks[1].Id; break;
                        case 2: webHookId = webHooks[2].Id; break;
                        case 3: webHookId = 9999; break;
                    }

                    var input = new WebHookStatInput
                    {
                        Url = $"https://example.com/hook/{(i % 5) + 1}",
                        HttpMethod = "POST",
                        RequestTime = time1,
                        ResponseTime = time2,
                        RequestLength = 100 + 1,
                        ResponseLength = 1000 + 10 * i,
                        ResponseStatusCode = 200,
                        WebHookId = webHookId,
                        ContentId = 10000 + i,
                        EventName = $"Event{(i % 4) + 1}",
                        ErrorMessage = null,
                        Payload = new { name1 = "value1", name2 = "value2" }
                    };
                    var record = new InputStatisticalDataRecord(input);
                    await sdp.WriteDataAsync(record, CancellationToken.None).ConfigureAwait(false);
                }

                // ACTION get all permitted items without filter
                var response1 = await ODataGetAsync($"/OData.svc/('Root')/GetWebHookUsageList", "", services)
                    .ConfigureAwait(false);

                // ASSERT the denied webhook related items do not exist in result
                var items1 = JsonSerializer.Create()
                    .Deserialize<WebHookUsageListItemViewModel[]>(new JsonTextReader(new StringReader(response1)));
                Assert.AreEqual(10, items1.Length);
                var existingWebHookIds = items1.Select(x => x.WebHookId).Distinct().OrderBy(x => x).ToArray();
                Assert.AreEqual(2, existingWebHookIds.Length);
                Assert.AreEqual(webHooks[0].Id, existingWebHookIds[0]);
                Assert.AreEqual(webHooks[1].Id, existingWebHookIds[1]);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task Stat_OData_GetWebHookUsageListOnWebHook()
        {
            var services = new ServiceCollection()
                .AddTransient<WebTransferRegistrator>()
                .AddStatisticalDataProvider<InMemoryStatisticalDataProvider>()
                .BuildServiceProvider();

            await ODataTestAsync(builder =>
            {
                builder.UseComponent(new WebHookComponent());
            }, async () =>
            {
                var webHooks = CreateWebHooks(3);

                var sdp = services.GetService<IStatisticalDataProvider>();
                for (var i = 20; i > 0; i--)
                {
                    var time1 = DateTime.UtcNow.AddDays(-i * 0.25);
                    var time2 = time1.AddSeconds(0.9);

                    var warning = i % 5 == 4;
                    var error = i % 7 == 6;

                    var message = error ? "Error message" : (warning ? "Warning message" : null);
                    var statusCode = error ? 500 : (warning ? 400 : 200);

                    for (int j = 0; j < 3; j++)
                    {
                        var input = new WebHookStatInput
                        {
                            Url = $"https://example.com/hook{j + 1}/{(i % 5) + 1}",
                            HttpMethod = "POST",
                            RequestTime = time1,
                            ResponseTime = time2,
                            RequestLength = 100 + 1,
                            ResponseLength = 1000 + 10 * i,
                            ResponseStatusCode = statusCode,
                            WebHookId = webHooks[j].Id,
                            ContentId = 10000 + 100 * j + i,
                            EventName = $"Event{j + 1}-{(i % 4) + 1}",
                            ErrorMessage = message,
                            Payload = new { name1 = "value1", name2 = "value2" }
                        };
                        var record = new InputStatisticalDataRecord(input);
                        await sdp.WriteDataAsync(record, CancellationToken.None).ConfigureAwait(false);
                    }
                }

                // ACTION-1 first time window.
                var response1 = await ODataGetAsync($"/OData.svc/Root/System/WebHooks('WebHook1')/GetWebHookUsageList",
                        "", services)
                    .ConfigureAwait(false);
                var lastTimeStr1 = GetLastCreationTime(response1);
                var response2 = await ODataGetAsync($"/OData.svc/Root/System/WebHooks('WebHook1')/GetWebHookUsageList",
                        $"?maxTime={lastTimeStr1}&count=5", services)
                    .ConfigureAwait(false);
                var lastTimeStr2 = GetLastCreationTime(response2);
                var response3 = await ODataGetAsync($"/OData.svc/Root/System/WebHooks('WebHook1')/GetWebHookUsageList",
                        $"?maxTime={lastTimeStr2}", services)
                    .ConfigureAwait(false);

                // ASSERT
                // Filtered by j=1
                var items1 = JsonSerializer.Create()
                    .Deserialize<WebHookUsageListItemViewModel[]>(new JsonTextReader(new StringReader(response1)));
                Assert.AreEqual(10, items1.Length);
                var items2 = JsonSerializer.Create()
                    .Deserialize<WebHookUsageListItemViewModel[]>(new JsonTextReader(new StringReader(response2)));
                Assert.AreEqual(5, items2.Length);
                var items3 = JsonSerializer.Create()
                    .Deserialize<WebHookUsageListItemViewModel[]>(new JsonTextReader(new StringReader(response3)));
                Assert.AreEqual(5, items3.Length);

                AssertSequenceEqual(
                    Enumerable.Range(10101, 20), // 10000 + 100 * j
                    items1.Union(items2.Union(items3)).Select(x => x.ContentId));

            }).ConfigureAwait(false);
        }
        private WebHookSubscription[] CreateWebHooks(int count)
        {
            var container = Node.Load<GenericContent>("/Root/System/WebHooks");
            var webHooks = new WebHookSubscription[count];
            for (int i = 0; i < webHooks.Length; i++)
            {
                webHooks[i] = new WebHookSubscription(container) { Name = $"WebHook{i}" };
                webHooks[i].Save();
            }

            return webHooks;
        }

        [TestMethod]
        public async STT.Task Stat_OData_GetWebHookUsageList_ContentPermission()
        {
            var services = new ServiceCollection()
                .AddTransient<WebTransferRegistrator>()
                .AddStatisticalDataProvider<InMemoryStatisticalDataProvider>()
                .BuildServiceProvider();

            // This test exploits a side effect: if there is no any WebHookSubscription content, the "relatedTargetIds" filter
            // is ignored in the DataProvider's LoadUsageListAsync method.
            await ODataTestAsync(1, builder =>
            {
            }, async () =>
            {
                var nodes = new Node[3];
                for (var i = 0; i < nodes.Length; i++)
                {
                    nodes[i] = new SystemFolder(Repository.Root) {Name = "Test" + i};
                    nodes[i].Save();
                }
                var denied = nodes[2];
                using (new SystemAccount())
                {
                    SecurityHandler.CreateAclEditor()
                        .BreakInheritance(denied.Id, new EntryType[0])
                        .Apply();
                }

                var nodeIds = nodes.Select(x => x.Id).Union(new[] {9999}).ToArray();

                var sdp = services.GetService<IStatisticalDataProvider>();
                for (var i = 20; i > 0; i--)
                {
                    var time1 = DateTime.UtcNow.AddDays(-i * 0.25);
                    var time2 = time1.AddSeconds(0.9);

                    var input = new WebHookStatInput
                    {
                        Url = $"https://example.com/hook/{(i % 5) + 1}",
                        HttpMethod = "POST",
                        RequestTime = time1,
                        ResponseTime = time2,
                        RequestLength = 100 + 1,
                        ResponseLength = 1000 + 10 * i,
                        ResponseStatusCode = 200,
                        WebHookId = 1242,
                        ContentId = nodeIds[i % nodeIds.Length],
                        EventName = $"Event{(i % 4) + 1}",
                        ErrorMessage = null,
                        Payload = new {name = "name1", value = 42}
                    };
                    var record = new InputStatisticalDataRecord(input);
                    await sdp.WriteDataAsync(record, CancellationToken.None).ConfigureAwait(false);
                }

                // ACTION-1 first time window.
                var response = await ODataGetAsync($"/OData.svc/('Root')/GetWebHookUsageList",
                        "?count=1000", services).ConfigureAwait(false);

                // ASSERT
                var items = JsonSerializer.Create()
                    .Deserialize<WebHookUsageListItemViewModel[]>(new JsonTextReader(new StringReader(response)));
                Assert.AreEqual(20, items.Length);

                var contentIds = items.Select(x => x.ContentId).ToArray();
                Assert.IsTrue(contentIds.Contains(nodes[0].Id));
                Assert.IsTrue(contentIds.Contains(nodes[1].Id));
                Assert.IsTrue(contentIds.Contains(nodes[2].Id));
                Assert.IsTrue(contentIds.Contains(9999));
                foreach (var item in items)
                {
                    if (item.ContentId == denied.Id || item.ContentId == 9999)
                        Assert.IsNull(item.Payload);
                    else
                        Assert.AreEqual("{\"name\":\"name1\",\"value\":42}", RemoveWhitespaces(item.Payload));
                }

            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async STT.Task Stat_OData_GetWebHookUsagePeriod()
        {
            var services = new ServiceCollection()
                .AddTransient<WebTransferRegistrator>()
                .AddStatisticalDataProvider<InMemoryStatisticalDataProvider>()
                .BuildServiceProvider();

            await ODataTestAsync(builder =>
            {
            }, async () =>
            {
                var now = DateTime.UtcNow;
                var testEnd = now.Truncate(TimeResolution.Month).AddMonths(1);
                var testStart = testEnd.AddYears(-1);
                var statDp = services.GetService<IStatisticalDataProvider>();
                await GenerateWebHookDataForODataTests(statDp, testStart, testEnd, now);

                // ACTION-1 
                var response1 = await ODataGetAsync($"/OData.svc/('Root')/GetWebHookUsagePeriod",
                    "", services).ConfigureAwait(false);

                // ASSERT-1
                var result1 = (JObject)JsonSerializer.CreateDefault().Deserialize(new JsonTextReader(new StringReader(response1)));
                Assert.AreEqual("WebHook", result1["DataType"].Value<string>());
                var start1 = testEnd.AddMonths(-1);
                var end1 = testEnd;
                var days1 = end1.AddDays(-1).Day;
                Assert.AreEqual(start1, result1["Start"].Value<DateTime>());
                Assert.AreEqual(end1, result1["End"].Value<DateTime>());
                Assert.AreEqual("Month", result1["TimeWindow"].Value<string>());
                Assert.AreEqual("Day", result1["Resolution"].Value<string>());
                Assert.AreEqual(days1, ((JArray)result1["CallCount"]).Count);
                Assert.AreEqual(86400L, ((JArray)result1["CallCount"]).First().Value<long>());
                Assert.AreEqual(days1, ((JArray)result1["RequestLengths"]).Count);
                Assert.AreEqual(8640000L, ((JArray)result1["RequestLengths"]).First().Value<long>());
                Assert.AreEqual(days1, ((JArray)result1["ResponseLengths"]).Count);
                Assert.AreEqual(86400000L, ((JArray)result1["ResponseLengths"]).First().Value<long>());
                Assert.AreEqual(days1, ((JArray)result1["Status100"]).Count);
                Assert.AreEqual(0, ((JArray)result1["Status100"]).First().Value<long>());
                Assert.AreEqual(days1, ((JArray)result1["Status200"]).Count);
                Assert.AreEqual(69120, ((JArray)result1["Status200"]).First().Value<long>());
                Assert.AreEqual(days1, ((JArray)result1["Status300"]).Count);
                Assert.AreEqual(0, ((JArray)result1["Status300"]).First().Value<long>());
                Assert.AreEqual(days1, ((JArray)result1["Status400"]).Count);
                Assert.AreEqual(8640, ((JArray)result1["Status400"]).First().Value<long>());
                Assert.AreEqual(days1, ((JArray)result1["Status500"]).Count);
                Assert.AreEqual(8640, ((JArray)result1["Status500"]).First().Value<long>());

                // ACTION-2
                var startTime2 = now.AddMonths(-1).ToString("yyyy-MM-dd HH:mm:ss");
                var response2 = await ODataGetAsync($"/OData.svc/('Root')/GetWebHookUsagePeriod",
                    $"?time={startTime2}", services).ConfigureAwait(false);

                // ASSERT-2
                var result2 = (JObject)JsonSerializer.CreateDefault().Deserialize(new JsonTextReader(new StringReader(response2)));
                Assert.AreEqual("WebHook", result2["DataType"].Value<string>());
                var start2 = testEnd.AddMonths(-2);
                var end2 = testEnd.AddMonths(-1);
                var days2 = end2.AddDays(-1).Day;
                Assert.AreEqual(start2, result2["Start"].Value<DateTime>());
                Assert.AreEqual(end2, result2["End"].Value<DateTime>());
                Assert.AreEqual("Month", result2["TimeWindow"].Value<string>());
                Assert.AreEqual("Day", result2["Resolution"].Value<string>());
                Assert.AreEqual(days2, ((JArray)result2["CallCount"]).Count);
                Assert.AreEqual(86400L, ((JArray)result2["CallCount"]).First().Value<long>());
                Assert.AreEqual(days2, ((JArray)result2["RequestLengths"]).Count);
                Assert.AreEqual(8640000L, ((JArray)result2["RequestLengths"]).First().Value<long>());
                Assert.AreEqual(days2, ((JArray)result2["ResponseLengths"]).Count);
                Assert.AreEqual(86400000L, ((JArray)result2["ResponseLengths"]).First().Value<long>());
                Assert.AreEqual(days2, ((JArray)result2["Status100"]).Count);
                Assert.AreEqual(0, ((JArray)result2["Status100"]).First().Value<long>());
                Assert.AreEqual(days2, ((JArray)result2["Status200"]).Count);
                Assert.AreEqual(69120, ((JArray)result2["Status200"]).First().Value<long>());
                Assert.AreEqual(days2, ((JArray)result2["Status300"]).Count);
                Assert.AreEqual(0, ((JArray)result2["Status300"]).First().Value<long>());
                Assert.AreEqual(days2, ((JArray)result2["Status400"]).Count);
                Assert.AreEqual(8640, ((JArray)result2["Status400"]).First().Value<long>());
                Assert.AreEqual(days2, ((JArray)result2["Status500"]).Count);
                Assert.AreEqual(8640, ((JArray)result2["Status500"]).First().Value<long>());

                // ACTION-3
                var response3 = await ODataGetAsync($"/OData.svc/('Root')/GetWebHookUsagePeriod",
                    "?timewindow=year", services).ConfigureAwait(false);

                // ASSERT-3
                var result3 = (JObject)JsonSerializer.CreateDefault().Deserialize(new JsonTextReader(new StringReader(response3)));
                Assert.AreEqual("WebHook", result3["DataType"].Value<string>());
                Assert.AreEqual(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc), result3["Start"].Value<DateTime>());
                Assert.AreEqual(new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc), result3["End"].Value<DateTime>());
                Assert.AreEqual("Year", result3["TimeWindow"].Value<string>());
                Assert.AreEqual("Month", result3["Resolution"].Value<string>());
                Assert.AreEqual(12, ((JArray)result3["CallCount"]).Count);
                Assert.AreEqual(86400L * 31, ((JArray)result3["CallCount"]).First().Value<long>());
                Assert.AreEqual(12, ((JArray)result3["RequestLengths"]).Count);
                Assert.AreEqual(8640000L * 31, ((JArray)result3["RequestLengths"]).First().Value<long>());
                Assert.AreEqual(12, ((JArray)result3["ResponseLengths"]).Count);
                Assert.AreEqual(86400000L * 31, ((JArray)result3["ResponseLengths"]).First().Value<long>());
                Assert.AreEqual(12, ((JArray)result3["Status100"]).Count);
                Assert.AreEqual(0, ((JArray)result3["Status100"]).First().Value<long>());
                Assert.AreEqual(12, ((JArray)result3["Status200"]).Count);
                Assert.AreEqual(69120 * 31, ((JArray)result3["Status200"]).First().Value<long>());
                Assert.AreEqual(12, ((JArray)result3["Status300"]).Count);
                Assert.AreEqual(0, ((JArray)result3["Status300"]).First().Value<long>());
                Assert.AreEqual(12, ((JArray)result3["Status400"]).Count);
                Assert.AreEqual(8640 * 31, ((JArray)result3["Status400"]).First().Value<long>());
                Assert.AreEqual(12, ((JArray)result3["Status500"]).Count);
                Assert.AreEqual(8640 * 31, ((JArray)result3["Status500"]).First().Value<long>());

                // ACTION-4
                var startTime = now.AddMonths(-1).ToString("yyyy-MM-dd HH:mm:ss");
                var response4 = await ODataGetAsync($"/OData.svc/('Root')/GetWebHookUsagePeriod",
                    $"?timewindow=month&time={startTime}", services).ConfigureAwait(false);

                // ASSERT-4
                var result4 = (JObject)JsonSerializer.CreateDefault().Deserialize(new JsonTextReader(new StringReader(response4)));
                Assert.AreEqual("WebHook", result4["DataType"].Value<string>());
                var start4 = testEnd.AddMonths(-2);
                var end4 = testEnd.AddMonths(-1);
                Assert.AreEqual(start4, result4["Start"].Value<DateTime>());
                Assert.AreEqual(end4, result4["End"].Value<DateTime>());
                Assert.AreEqual("Month", result4["TimeWindow"].Value<string>());
                Assert.AreEqual("Day", result4["Resolution"].Value<string>());

            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async STT.Task Stat_OData_GetWebHookUsagePeriods()
        {
            var services = new ServiceCollection()
                .AddTransient<WebTransferRegistrator>()
                .AddStatisticalDataProvider<InMemoryStatisticalDataProvider>()
                .BuildServiceProvider();

            await ODataTestAsync(builder =>
            {
            }, async () =>
            {
                async STT.Task<string> GetWebHookUsagePeriods(DateTime now, TimeWindow window)
                {
                    var content = Content.Create(Repository.Root);
                    var httpContext = CreateHttpContext("/OData.svc/('Root')/GetWebHookUsagePeriods", "", services);
                    var result = await SenseNet.WebHooks.StatisticsOperations.GetWebHookUsagePeriods(content, httpContext, now, window);
                    var sb = new StringBuilder();
                    var serializer = new JsonSerializer();
                    serializer.Serialize(new JsonTextWriter(new StringWriter(sb)), result);
                    return sb.ToString();
                }


                var now = new DateTime(2021, 6, 15, 3, 18, 28);
                var testEnd = now.Truncate(TimeResolution.Month).AddMonths(1);
                var statDp = services.GetService<IStatisticalDataProvider>();
                var testStart = testEnd.AddYears(-1);
                await GenerateWebHookDataForODataTests(statDp, testStart, testEnd, now);

                //var responseDefault = await ODataGetAsync($"/OData.svc/('Root')/GetWebHookUsagePeriods",
                //    "").ConfigureAwait(false);

                //var responseHour = await ODataGetAsync($"/OData.svc/('Root')/GetWebHookUsagePeriods",
                //    "?timewindow=hour").ConfigureAwait(false);

                ////var responseDay = await ODataGetAsync($"/OData.svc/('Root')/GetWebHookUsagePeriods",
                ////    "?timewindow=day").ConfigureAwait(false);

                //var responseMonth = await ODataGetAsync($"/OData.svc/('Root')/GetWebHookUsagePeriods",
                //    "?timewindow=month").ConfigureAwait(false);

                //var responseYear = await ODataGetAsync($"/OData.svc/('Root')/GetWebHookUsagePeriods",
                //    "?timewindow=year").ConfigureAwait(false);

                //Assert.AreEqual(responseDefault, responseMonth);

                var responseHour = await GetWebHookUsagePeriods(now, TimeWindow.Hour);
                var responseDay = await GetWebHookUsagePeriods(now, TimeWindow.Day);
                var responseMonth = await GetWebHookUsagePeriods(now, TimeWindow.Month);
                var responseYear = await GetWebHookUsagePeriods(now, TimeWindow.Year);
                Assert.AreEqual(
                    "{\"Window\":\"Hour\",\"Resolution\":\"Minute\"," +
                    "\"First\":\"0001-01-01T00:00:00\",\"Last\":\"0001-01-01T00:00:00\",\"Count\":0}",
                    RemoveWhitespaces(responseHour));
                Assert.AreEqual(
                    "{\"Window\":\"Day\",\"Resolution\":\"Hour\"," +
                    "\"First\":\"2021-06-13T00:00:00Z\",\"Last\":\"2021-06-15T00:00:00Z\",\"Count\":3}",
                    RemoveWhitespaces(responseDay));
                Assert.AreEqual(
                    "{\"Window\":\"Month\",\"Resolution\":\"Day\"," +
                    "\"First\":\"2021-04-01T00:00:00Z\",\"Last\":\"2021-06-01T00:00:00Z\",\"Count\":3}",
                    RemoveWhitespaces(responseMonth));
                Assert.AreEqual(
                    "{\"Window\":\"Year\",\"Resolution\":\"Month\"," +
                    "\"First\":\"2021-01-01T00:00:00Z\",\"Last\":\"2021-01-01T00:00:00Z\",\"Count\":1}",
                    RemoveWhitespaces(responseYear));
            }).ConfigureAwait(false);
        }
        private async STT.Task GenerateWebHookDataForODataTests(IStatisticalDataProvider statDataProvider,
            DateTime testStart, DateTime testEnd, DateTime now)
        {
            StatisticalDataAggregationController CreateAggregator()
            {
                return new StatisticalDataAggregationController(statDataProvider,
                    new[] { new WebHookStatisticalDataAggregator(GetOptions()) }, GetOptions(),
                    NullLoggerFactory.Instance.CreateLogger<StatisticalDataAggregationController>());
            }
            StatisticalDataAggregationController aggregator;

            var time = testStart;
            while (time <= now)
            {
                if (time.Second == 0)
                {
                    if (time.Second == 0)
                    {
                        var aggregationTime = time.AddSeconds(-1);

                        if (time.Minute == 0)
                        {
                            await GenerateWebHookAggregationAsync(aggregationTime, TimeResolution.Hour, 60 * 60, statDataProvider);
                            // Does not aggregate but cleans up.
                            aggregator = CreateAggregator();
                            await aggregator.AggregateAsync(aggregationTime, TimeResolution.Hour, CancellationToken.None);
                            if (time.Hour == 0)
                            {
                                aggregator = CreateAggregator();
                                await aggregator.AggregateAsync(aggregationTime, TimeResolution.Day, CancellationToken.None);
                                if (time.Day == 1)
                                {
                                    aggregator = CreateAggregator();
                                    await aggregator.AggregateAsync(aggregationTime, TimeResolution.Month, CancellationToken.None);
                                }
                            }
                        }
                    }
                }
                time = time.AddSeconds(1);
            }
        }


        private string GetLastCreationTime(string response)
        {
            var p = response.LastIndexOf("\"CreationTime\":");
            var p1 = response.IndexOf("Z", p);
            var value = response.Substring(p + 17, p1 - p - 16);
            return value;
        }

        protected STT.Task ODataTestAsync(Action<RepositoryBuilder> initialize, Func<STT.Task> callback)
        {
            return ODataTestAsync(0, initialize, callback);
        }
        private async STT.Task ODataTestAsync(int userId, Action<RepositoryBuilder> initialize, Func<STT.Task> callback)
        {
            Providers.Instance.ResetBlobProviders();

            OnTestInitialize();

            var builder = base.CreateRepositoryBuilderForTestInstance(); //CreateRepositoryBuilder();

            //UNDONE:<?:   do not call discovery and providers setting in the static ctor of ODataMiddleware
            var _ = new ODataMiddleware(null, null, null); // Ensure running the first-touch discover in the static ctor
            OperationCenter.Operations.Clear();
            OperationCenter.Discover();
            Providers.Instance.SetProvider(typeof(IOperationMethodStorage), new OperationMethodStorage());

            initialize?.Invoke(builder);

            Indexing.IsOuterSearchEngineEnabled = true;

            Cache.Reset();
            ResetContentTypeManager();

            using (var repo = Repository.Start(builder))
            {
                if (userId == 0)
                {
                    User.Current = User.Administrator;
                    using(new SystemAccount())
                        await callback().ConfigureAwait(false);
                }
                else
                {
                    User user = null;
                    using (new SystemAccount())
                        user = Node.Load<User>(userId);
                    if (user == null)
                        throw new ApplicationException("User not found: " + userId);
                    User.Current = user;
                    await callback().ConfigureAwait(false);
                }
            }
        }


        internal static STT.Task<string> ODataGetAsync(string resource, string queryString, IServiceProvider services = null, IConfiguration config = null)
        {
            return ODataProcessRequestAsync(resource, queryString, null, "GET", services, config);
        }
        private static async STT.Task<string> ODataProcessRequestAsync(string resource, string queryString,
            string requestBodyJson, string httpMethod, IServiceProvider services, IConfiguration config)
        {
            var httpContext = CreateHttpContext(resource, queryString, services);
            var request = httpContext.Request;
            request.Method = httpMethod;
            request.Path = resource;
            request.QueryString = new QueryString(queryString);
            if (requestBodyJson != null)
                request.Body = CreateRequestStream(requestBodyJson);

            httpContext.Response.Body = new MemoryStream();

            var odata = new ODataMiddleware(null, config, null);
            var odataRequest = ODataRequest.Parse(httpContext);
            await odata.ProcessRequestAsync(httpContext, odataRequest).ConfigureAwait(false);

            var responseOutput = httpContext.Response.Body;
            responseOutput.Seek(0, SeekOrigin.Begin);
            string output;
            using (var reader = new StreamReader(responseOutput))
                output = await reader.ReadToEndAsync().ConfigureAwait(false);

            return output;
        }
        internal static HttpContext CreateHttpContext(string resource, string queryString, IServiceProvider services = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = services;
            var request = httpContext.Request;
            request.Method = "GET";
            request.Path = resource;
            request.QueryString = new QueryString(queryString);
            httpContext.Response.Body = new MemoryStream();
            return httpContext;
        }
        private static Stream CreateRequestStream(string request)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(request);
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        #endregion


        #region private class TestStatisticalDataProvider : IStatisticalDataProvider
        private class TestStatisticalDataProvider : IStatisticalDataProvider
        {
            public List<IStatisticalDataRecord> Storage { get; } = new();
            public List<Aggregation> Aggregations { get; } = new();
            public List<(string DataType, DateTime RetentionTime)> CleanupRecordsCalls { get; } = new();
            public List<(string DataType, TimeResolution Resolution, DateTime RetentionTime)> CleanupAggregationsCalls { get; } = new();

            public STT.Task WriteDataAsync(IStatisticalDataRecord data, CancellationToken cancel)
            {
                var now = DateTime.UtcNow;

                Storage.Add(new StatisticalDataRecord
                {
                    Id = 0,
                    DataType = data.DataType,
                    WrittenTime = now,
                    CreationTime = data.CreationTime ?? now,
                    Duration = data.Duration,
                    RequestLength = data.RequestLength,
                    ResponseLength = data.ResponseLength,
                    ResponseStatusCode = data.ResponseStatusCode,
                    Url = data.Url,
                    TargetId = data.TargetId,
                    ContentId = data.ContentId,
                    EventName = data.EventName,
                    ErrorMessage = data.ErrorMessage,
                    GeneralData = data.GeneralData
                });
                return STT.Task.CompletedTask;
            }

            public STT.Task<IEnumerable<IStatisticalDataRecord>> LoadUsageListAsync(string dataType, int[] relatedTargetIds, DateTime endTimeExclusive, int count, CancellationToken cancel)
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

            public STT.Task<DateTime?[]> LoadFirstAggregationTimesByResolutionsAsync(string dataType, CancellationToken httpContextRequestAborted)
            {
                throw new NotImplementedException();
            }

            public STT.Task<DateTime?[]> LoadLastAggregationTimesByResolutionsAsync(CancellationToken cancel)
            {
                var result = new DateTime?[4];
                for (var resolution = TimeResolution.Minute; resolution <= TimeResolution.Month; resolution++)
                {
                    result[(int) resolution] = Aggregations.OrderByDescending(x => x.Date)
                        .FirstOrDefault(x => x.Resolution == resolution)?.Date;
                }

                return STT.Task.FromResult(result);
            }

            public STT.Task EnumerateDataAsync(string dataType, DateTime startTime, DateTime endTimeExclusive,
                Action<IStatisticalDataRecord> aggregatorCallback, CancellationToken cancel)
            {
                var result = new List<Aggregation>();

                var relatedItems = Storage.Where(
                    x => x.DataType == dataType && x.CreationTime >= startTime && x.CreationTime < endTimeExclusive);

                foreach (var item in relatedItems)
                {
                    cancel.ThrowIfCancellationRequested();
                    aggregatorCallback(item);
                }

                return STT.Task.CompletedTask;
            }

            public STT.Task WriteAggregationAsync(Aggregation aggregation, CancellationToken cancel)
            {
                var existing = Aggregations.FirstOrDefault(x =>
                    x.DataType == aggregation.DataType && x.Date == aggregation.Date &&
                    x.Resolution == aggregation.Resolution);
                if (existing == null)
                    Aggregations.Add(aggregation);
                else
                    existing.Data = aggregation.Data;
                return STT.Task.CompletedTask;
            }

            public STT.Task CleanupRecordsAsync(string dataType, DateTime retentionTime, CancellationToken cancel)
            {
                CleanupRecordsCalls.Add((dataType, retentionTime));
                return STT.Task.CompletedTask;
            }

            public STT.Task CleanupAggregationsAsync(string dataType, TimeResolution resolution, DateTime retentionTime,
                CancellationToken cancel)
            {
                CleanupAggregationsCalls.Add((dataType, resolution, retentionTime));
                return STT.Task.CompletedTask;
            }
        }
        #endregion
    }
}
