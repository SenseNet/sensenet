using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Events;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Tests.Core;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.WebHooks.Tests
{
    [TestClass]
    public class WebHookSubscriptionTests : TestBase
    {
        [TestMethod]
        public async Task WebHookSubscription_OneType()
        {
            await Test(
                builder => { builder.UseComponent(new WebHookComponent()); },
                async () =>
                {
                    var wh = await CreateWebHookSubscriptionAsync(@"{
    ""Path"": ""/Root/Content"",
    ""ContentTypes"": [ 
        {
            ""Name"": ""Folder"", 
            ""Events"": [ ""Create"" ] 
        }
    ] 
}");

                    Assert.IsTrue(wh.IsValid);
                    Assert.AreEqual("/Root/Content", wh.FilterData.Path);
                    Assert.AreEqual(1, wh.FilterData.ContentTypes.Length);
                    Assert.AreEqual("Folder", wh.FilterData.ContentTypes[0].Name);
                    Assert.AreEqual(1, wh.FilterData.ContentTypes[0].Events.Length);
                    Assert.AreEqual(WebHookEventType.Create, wh.FilterData.ContentTypes[0].Events[0]);

                    Assert.AreEqual("+InTree:'/Root/Content' +Type:(Folder)", wh.FilterQuery);
                });
        }

        [TestMethod]
        public async Task WebHookSubscription_Complex()
        {
            await Test(
                builder => { builder.UseComponent(new WebHookComponent()); },
                async () =>
                {
                    var wh = await CreateWebHookSubscriptionAsync(@"{
    ""Path"": ""/Root/Content"",
    ""ContentTypes"": [ 
        {
            ""Name"": ""Folder"", 
            ""Events"": [ ""Create"" ] 
        },
{
            ""Name"": ""File"", 
            ""Events"": [ ""Create"", ""Modify"", ""Publish"" ] 
        }
    ] 
}",
                        @"{
""h1"":  ""value1"",
""h2"":  ""value2""
}");

                    Assert.IsTrue(wh.IsValid);
                    Assert.AreEqual("/Root/Content", wh.FilterData.Path);
                    Assert.AreEqual("Folder,File", string.Join(",",
                        wh.FilterData.ContentTypes.Select(ct => ct.Name)));
                    Assert.AreEqual("Create,Modify,Publish", string.Join(",",
                        wh.FilterData.ContentTypes[1].Events.Select(ev => ev.ToString())));

                    Assert.AreEqual("+InTree:'/Root/Content' +Type:(Folder File)", wh.FilterQuery);

                    Assert.AreEqual(2, wh.HttpHeaders.Count);
                    Assert.AreEqual("h1", wh.HttpHeaders.Keys.First());
                    Assert.AreEqual("value1", wh.HttpHeaders.Values.First());
                    Assert.AreEqual("h2", wh.HttpHeaders.Keys.Last());
                    Assert.AreEqual("value2", wh.HttpHeaders.Values.Last());
                });
        }

        [TestMethod]
        public async Task WebHookSubscription_RelevantEvent()
        {
            await Test(
                builder => { builder.UseComponent(new WebHookComponent()); },
                async () =>
                {
                    var wh = await CreateWebHookSubscriptionAsync(@"{
    ""Path"": ""/Root/Content"",
    ""ContentTypes"": [ 
        {
            ""Name"": ""Folder"", 
            ""Events"": [ ""Create"", ""Delete"" ] 
        }
    ] 
}");

                    var parent1 = await Node.LoadNodeAsync("/Root/Content", CancellationToken.None);
                    var node1 = new Folder(parent1);
                    var event1 = new NodeCreatedEvent(new TestNodeEventArgs(node1));
                    var event2 = new TestEvent1(new TestNodeEventArgs(node1));
                    var event3 = new NodeForcedDeletedEvent(new TestNodeEventArgs(node1));

                    Assert.AreEqual(WebHookEventType.Create, wh.GetRelevantEventTypes(event1).Single());
                    Assert.AreEqual(0, wh.GetRelevantEventTypes(event2).Length);
                    Assert.AreEqual(WebHookEventType.Delete, wh.GetRelevantEventTypes(event3).Single());
                });
        }
        [TestMethod]
        public async Task WebHookSubscription_RelevantEvent_All()
        {
            await Test(
                builder => { builder.UseComponent(new WebHookComponent()); },
                async () =>
                {
                    // TriggersForAllEvents is TRUE
                    var wh = await CreateWebHookSubscriptionAsync(@"{
    ""Path"": ""/Root/Content"",
""TriggersForAllEvents"": true,
    ""ContentTypes"": [ 
        {
            ""Name"": ""Folder"", 
            ""Events"": [ ""Publish"" ] 
        }
    ] 
}");

                    var parent1 = await Node.LoadNodeAsync("/Root/Content", CancellationToken.None);
                    var node1 = new Folder(parent1);
                    var event1 = new NodeCreatedEvent(new TestNodeEventArgs(node1));
                    var event2 = new NodeForcedDeletedEvent(new TestNodeEventArgs(node1));

                    // triggered for ALL events: only the appropriate events should be returned
                    Assert.AreEqual(WebHookEventType.Create, wh.GetRelevantEventTypes(event1).Single());
                    Assert.AreEqual(WebHookEventType.Delete, wh.GetRelevantEventTypes(event2).Single());

                    // TriggersForAllEvents is FALSE, but All is selected for the type.
                    wh = await CreateWebHookSubscriptionAsync(@"{
    ""Path"": ""/Root/Content"",
""TriggersForAllEvents"": false,
    ""ContentTypes"": [ 
        {
            ""Name"": ""Folder"", 
            ""Events"": [ ""All"" ] 
        }
    ] 
}");
                    // triggered for ALL events of the type: only the appropriate events should be returned
                    Assert.AreEqual(WebHookEventType.Create, wh.GetRelevantEventTypes(event1).Single());
                    Assert.AreEqual(WebHookEventType.Delete, wh.GetRelevantEventTypes(event2).Single());
                });
        }
        [TestMethod]
        public async Task WebHookSubscription_RelevantEvent_Versioning()
        {
            var store = new TestWebHookSubscriptionStore(new WebHookSubscription[0]);
            var webHookClient = new TestWebHookClient();

            await Test((builder) =>
                {
                    builder
                        .UseComponent(new WebHookComponent())
                        .UseEventDistributor(new EventDistributor())
                        .AddAsyncEventProcessors(new LocalWebHookProcessor(
                        store,
                        webHookClient, 
                        new NullLogger<LocalWebHookProcessor>()));
                },
                async () =>
            {
                var file = CreateFile();

                var wh = await CreateWebHookSubscriptionAsync(@"{
    ""Path"": ""/Root/Content"",
    ""ContentTypes"": [ 
        {
            ""Name"": ""File"", 
            ""Events"": [ ""Create"", ""Delete"", ""CheckOut"", ""CheckIn"", ""Publish"", ""Approve"", ""Reject"" ] 
        }
    ] 
}");
                store.Subscriptions.Add(wh);

                //UNDONE: CheckOut does not trigger an event :(
                file.CheckOut();

                file.CheckIn();

                Assert.AreEqual(2, webHookClient.Requests.Count);
                Assert.IsTrue(webHookClient.Requests.Any(r => r.EventName == "CheckIn"));
                Assert.IsTrue(webHookClient.Requests.Any(r => r.EventName == "Modify"));
                
                webHookClient.Requests.Clear();

                file.Publish();

                Assert.AreEqual(2, webHookClient.Requests.Count);
                Assert.IsTrue(webHookClient.Requests.Any(r => r.EventName == "Publish"));
                Assert.IsTrue(webHookClient.Requests.Any(r => r.EventName == "Modify"));

                //UNDONE: Add tests for Approve and Reject
            });
        }

        [TestMethod]
        public async Task WebHookSubscription_Invalid_Filter()
        {
            await Test(
                builder => { builder.UseComponent(new WebHookComponent()); },
                async () =>
                {
                    var wh = await CreateWebHookSubscriptionAsync(@"{
    ""Path"": ""/Root/Content"",
    ""ContentTypes"": [ 
        {
            ""Name"": 
    ] 
}",
                        @"{
""h1"":  ""value1"",
""h2"":  ""value2""
}");

                    Assert.IsFalse(wh.IsValid);
                    Assert.AreEqual("WebHookFilter", wh.InvalidFields);
                });
        }
        [TestMethod]
        public async Task WebHookSubscription_Invalid_FilterAndHeader()
        {
            await Test(
                builder => { builder.UseComponent(new WebHookComponent()); },
                async () =>
                {
                    var wh = await CreateWebHookSubscriptionAsync(@"{
    ""Path"": ""/Root/Content"",
    ""ContentTypes"": [ 
        {
            ""Name"": 
    ] 
}",
                        @"{""h1"":  ""value1""");

                    Assert.IsFalse(wh.IsValid);
                    Assert.AreEqual("WebHookFilter;WebHookHeaders", wh.InvalidFields);
                });
        }

        private Task<WebHookSubscription> CreateWebHookSubscriptionAsync(string filter, string headers = null)
        {
            var container = RepositoryTools.CreateStructure("/Root/System/WebHooks", "SystemFolder") ??
                Content.Load("/Root/System/WebHooks");

            var wh = new WebHookSubscription(container.ContentHandler)
            {
                Url = "https://localhost:44393/webhooks/test",
                Filter = filter,
                //Filter = "{ \"Path\": \"/Root/Content\", \"ContentTypes\": [ { \"Name\": \"Folder\", \"Events\": [ \"Create\", \"Publish\" ] } ] }",
                Headers = headers,
                //Headers = "{ \"h1-custom\": \"value1\", \"h2-custom\": \"value2\" }",
                AllowIncrementalNaming = true
            };
            wh.Save();

            return Node.LoadAsync<WebHookSubscription>(wh.Id, CancellationToken.None);
        }

        private File CreateFile()
        {
            var parent = (RepositoryTools.CreateStructure("/Root/Content/Docs", "DocumentLibrary")
                          ?? Content.Load("/Root/Content/Docs")).ContentHandler;
            var file = new File(parent) {VersioningMode = VersioningType.MajorAndMinor};
            file.Save();

            return file;
        }
    }
}
