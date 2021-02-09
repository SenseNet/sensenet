using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
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
            await Test(async () =>
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
            await Test(async () =>
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
                Assert.AreEqual("Folder,File",  string.Join(",", 
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
            await Test(async () =>
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

                Assert.AreEqual(WebHookEventType.Create, wh.GetRelevantEventType(event1));
                Assert.IsNull(wh.GetRelevantEventType(event2));
                Assert.AreEqual(WebHookEventType.Delete, wh.GetRelevantEventType(event3));

                //UNDONE: add tests for more complex events: Publish, CheckIn
            });
        }

        [TestMethod]
        public async Task WebHookSubscription_Invalid_Filter()
        {
            await Test(async () =>
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
            await Test(async () =>
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
                Headers = headers
                //Headers = "{ \"h1-custom\": \"value1\", \"h2-custom\": \"value2\" }"
            };
            wh.Save();

            return Node.LoadAsync<WebHookSubscription>(wh.Id, CancellationToken.None);
        }
    }
}
