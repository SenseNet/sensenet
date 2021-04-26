using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
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
            ""Events"": [ ""Create"", ""Modify"", ""Approve"" ] 
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
                    Assert.AreEqual("Create,Modify,Approve", string.Join(",",
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
            ""Events"": [ ""Create"", ""Delete"", ""MoveToTrash"", ""RestoreFromTrash"" ] 
        }
    ] 
}");

                    var parent1 = await Node.LoadNodeAsync("/Root/Content", CancellationToken.None);
                    var node1 = new Folder(parent1);
                    var event1 = new NodeCreatedEvent(new TestNodeEventArgs(node1, NodeEvent.Created));
                    var event2 = new TestEvent1(new TestNodeEventArgs(node1, NodeEvent.Created));
                    var event3 = new NodeForcedDeletedEvent(new TestNodeEventArgs(node1, NodeEvent.DeletedPhysically));
                    var event4 = new NodeDeletedEvent(new TestNodeEventArgs(node1, NodeEvent.Deleted));
                    var event5 = new NodeRestoredEvent(new TestNodeEventArgs(node1, NodeEvent.Restored));

                    Assert.AreEqual(WebHookEventType.Create, wh.GetRelevantEventTypes(event1).Single());
                    Assert.AreEqual(0, wh.GetRelevantEventTypes(event2).Length);
                    Assert.AreEqual(WebHookEventType.Delete, wh.GetRelevantEventTypes(event3).Single());
                    Assert.AreEqual(WebHookEventType.MoveToTrash, wh.GetRelevantEventTypes(event4).Single());
                    Assert.AreEqual(WebHookEventType.RestoreFromTrash, wh.GetRelevantEventTypes(event5).Single());
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
            ""Events"": [ ""Pending"" ] 
        }
    ] 
}");

                    var parent1 = await Node.LoadNodeAsync("/Root/Content", CancellationToken.None);
                    var node1 = new Folder(parent1);
                    var event1 = new NodeCreatedEvent(new TestNodeEventArgs(node1, NodeEvent.Created));
                    var event2 = new NodeForcedDeletedEvent(new TestNodeEventArgs(node1, NodeEvent.DeletedPhysically));

                    // triggered for ALL events: only the appropriate events should be returned
                    var re1 = wh.GetRelevantEventTypes(event1);
                    Assert.AreEqual(2, re1.Length);
                    Assert.IsTrue(re1.Contains(WebHookEventType.Create));
                    Assert.IsTrue(re1.Contains(WebHookEventType.Approve)); // automatic approve

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
                    re1 = wh.GetRelevantEventTypes(event1);
                    Assert.AreEqual(2, re1.Length);
                    Assert.IsTrue(re1.Contains(WebHookEventType.Create));
                    Assert.IsTrue(re1.Contains(WebHookEventType.Approve)); // automatic approve

                    Assert.AreEqual(WebHookEventType.Delete, wh.GetRelevantEventTypes(event2).Single());
                });
        }

        [TestMethod]
        public async Task WebHookSubscription_Delete()
        {
            await Test_Versioning(VersioningType.None, ApprovingType.False,
                file =>
                {
                    file.ForceDelete();

                    return Task.CompletedTask;
                },
                "Delete");
        }

        [TestMethod]
        public async Task WebHookSubscription_Versioning_None_Approving_False()
        {
            await Test_Versioning(VersioningType.None, ApprovingType.False,
                file =>
                {
                    // Initial version: 1.0.A
                    file.CheckOut();    // --> 2.0.L    --> no event on checkout
                    file.CheckIn();     // --> 1.0.A    --> Modify,Approve

                    file.Index = 41;
                    file.Save(SavingMode.KeepVersion);  // --> Modify

                    file.CheckOut();    // --> 2.0.L    --> no event on checkout

                    file.Index = 42;
                    file.Save(SavingMode.KeepVersion);  // no event on modification when locked 

                    file.CheckIn();     // --> 1.0.A    --> Modify,Approve

                    return Task.CompletedTask;
                },
                "Modify,Approve,Modify,Modify,Approve");
        }
        [TestMethod]
        public async Task WebHookSubscription_Versioning_None_Approving_True()
        {
            await Test_Versioning(VersioningType.None, ApprovingType.True,
                file =>
                {
                    // Initial version: 1.0.P
                    file.CheckOut();    // --> 2.0.L    --> no event on checkout
                    file.CheckIn();     // --> 1.0.P    --> Modify,Pending

                    file.Index = 41;
                    file.Save(SavingMode.KeepVersion);  // --> 1.0.P    --> Modify

                    file.Approve();     // --> 1.0.A    --> Modify,Approve

                    file.CheckOut();    // --> 2.0.L    --> no event on checkout
                    file.CheckIn();     // --> 2.0.P    --> Modify,Pending
                    file.Reject();      // --> 2.0.R    --> Modify,Reject

                    return Task.CompletedTask;
                },
                "Modify,Pending,Modify,Modify,Approve,Modify,Pending,Modify,Reject");
        }
        [TestMethod]
        public async Task WebHookSubscription_Versioning_Major_Approving_False()
        {
            await Test_Versioning(VersioningType.MajorOnly, ApprovingType.False,
                file =>
                {
                    // Initial version: 1.0.A
                    file.CheckOut();    // --> 2.0.L    --> no event on checkout
                    file.CheckIn();     // --> 2.0.A    --> Modify,Approve

                    file.Index = 41;
                    file.Save(SavingMode.KeepVersion);  // --> 2.0.A    --> Modify

                    file.CheckOut();    // --> 3.0.L    --> no event on checkout

                    file.Index = 42;
                    file.Save(SavingMode.KeepVersion);  // no event on modification when locked 

                    file.CheckIn();     // --> 3.0.A    --> Modify,Approve

                    file.Save();        // --> 4.0.A    --> Modify,Approve

                    return Task.CompletedTask;
                },
                "Modify,Approve,Modify,Modify,Approve,Modify,Approve");
        }
        [TestMethod]
        public async Task WebHookSubscription_Versioning_Major_Approving_True()
        {
            await Test_Versioning(VersioningType.MajorOnly, ApprovingType.True,
                file =>
                {
                    // Initial version: 1.0.P
                    file.CheckOut();    // --> 2.0.L    --> no event on checkout
                    file.CheckIn();     // --> 2.0.P    --> Modify,Pending

                    file.Index = 42;
                    file.Save(SavingMode.KeepVersion);  // --> Modify

                    file.Approve();     // --> 1.0.A    --> Modify,Approve

                    file.CheckOut();    // --> 2.0.L    --> no event on checkout
                    file.CheckIn();     // --> 2.0.P    --> Modify,Pending
                    file.Reject();      // --> 2.0.R    --> Modify,Reject
                    file.Save();        // --> 3.0.P    --> Modify,Pending

                    return Task.CompletedTask;
                },
                "Modify,Pending,Modify,Modify,Approve,Modify,Pending,Modify,Reject,Modify,Pending");
        }
        [TestMethod]
        public async Task WebHookSubscription_Versioning_Full_Approving_False()
        {
            await Test_Versioning(VersioningType.MajorAndMinor, ApprovingType.False,
                file =>
                {
                    // Initial version: 0.1.D
                    file.CheckOut();    // --> 0.2.L    --> no event on checkout
                    file.CheckIn();     // --> 0.2.D    --> Modify,Draft

                    file.Index = 41;
                    file.Save();        // --> 0.3.D    --> Modify,Draft

                    file.Publish();     // --> 1.0.A    --> Modify,Approve

                    return Task.CompletedTask;
                },
                "Modify,Draft,Modify,Draft,Modify,Approve");
        }
        [TestMethod]
        public async Task WebHookSubscription_Versioning_Full_Approving_True()
        {
            await Test_Versioning(VersioningType.MajorAndMinor, ApprovingType.True,
                file =>
                {
                    // Initial version: 0.1.D
                    file.CheckOut();    // --> 0.2.L    --> no event on checkout
                    file.CheckIn();     // --> 0.2.D    --> Modify,Draft

                    file.Index = 41;
                    file.Save(SavingMode.KeepVersion);  // --> Modify

                    file.Publish();     // --> 0.2.P    --> Modify,Pending
                    file.Approve();     // --> 1.0.A    --> Modify,Approve

                    file.CheckOut();    // --> 1.1.L    --> no event on checkout
                    file.CheckIn();     // --> 1.1.D    --> Modify,Draft

                    file.Index = 42;
                    file.Save();        // --> 1.2.D    --> Modify,Draft

                    file.Publish();     // --> 1.2.P    --> Modify,Pending
                    file.Reject();      // --> 1.2.R    --> Modify,Reject

                    return Task.CompletedTask;
                },
                "Modify,Draft,Modify,Modify,Pending,Modify,Approve,Modify,Draft," +
                "Modify,Draft,Modify,Pending,Modify,Reject");
        }

        public async Task Test_Versioning(VersioningType versioningType, ApprovingType approvingType,
            Func<File, Task> action, string expectedEventLog, string subscribedEvents = null)
        {
            File file = null;

            await Test_WebHook(() =>
                {
                    file = CreateFile(versioningType, approvingType);
                    return Task.CompletedTask;
                },
                async () =>
                {
                    await action(file);
                    return file;
                },
                (result, client, subscription) =>
                {
                    var eventLog = string.Join(",", client.Requests.Select(r => r.EventName));

                    Assert.AreEqual(expectedEventLog, eventLog);
                },
                subscribedEvents);
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
        
        [TestMethod]
        public async Task WebHookSubscription_Payload_Default()
        {
            await Test_WebHook((file, client, subscription) =>
                {
                    var request = client.Requests.First();

                    // default properties are there
                    Assert.AreEqual(((File)file).Id, request.NodeId);
                    Assert.AreEqual("Modify", request.EventName);
                });
        }
        [TestMethod]
        public async Task WebHookSubscription_Payload_Custom()
        {
            await Test_WebHook((file, client, subscription) =>
                {
                    var request = client.Requests.First();

                    // custom property is sent
                    Assert.IsTrue(string.Equals(request.GetPostPropertyString("text"), "hello", StringComparison.InvariantCulture));

                    // default properties are NOT there
                    Assert.AreEqual(0, request.NodeId);
                    Assert.AreEqual(null, request.EventName);
                }, payload: "{ \"text\": \"hello\" }");
        }

        internal async Task Test_WebHook(Action<object, TestWebHookClient, WebHookSubscription> assertAction,
            string subscribedEvents = null, string headers = null, string payload = null)
        {
            File file = null;

            await Test_WebHook(() =>
                {
                    file = CreateFile();
                    return Task.CompletedTask;
                },
                () =>
                {
                    file.Index = 42;
                    file.Save(SavingMode.KeepVersion);
                    return Task.FromResult((object)file);
                }, 
                assertAction,
                subscribedEvents,
                headers,
                payload);
        }

        internal async Task Test_WebHook(Func<Task> actionBeforeSubscription, Func<Task<object>> actionAfterSubscription,
            Action<object, TestWebHookClient, WebHookSubscription> assertAction, 
            string subscribedEvents = null, string headers = null, string payload = null)
        {
            // subscribe to all events by default
            if (subscribedEvents == null)
                subscribedEvents = @"""All""";

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
                    if (actionBeforeSubscription != null)
                        await actionBeforeSubscription();

                    // create a new subscription
                    var wh = await CreateWebHookSubscriptionAsync(@"{
    ""Path"": ""/Root/Content"",
    ""ContentTypes"": [ 
        {
            ""Name"": ""File"", 
            ""Events"": [ " + subscribedEvents + @"  ] 
        }
    ] 
}", 
                        headers, payload);

                    store.Subscriptions.Add(wh);

                    object result = null;

                    if (actionAfterSubscription != null)
                        result = await actionAfterSubscription();

                    assertAction?.Invoke(result, webHookClient, wh);
                });
        }

        private Task<WebHookSubscription> CreateWebHookSubscriptionAsync(string filter, string headers = null, string payload = null)
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
                Payload = payload,
                AllowIncrementalNaming = true
            };
            wh.Save();

            return Node.LoadAsync<WebHookSubscription>(wh.Id, CancellationToken.None);
        }

        private File CreateFile(VersioningType versioningType = VersioningType.Inherited, ApprovingType approvingType = ApprovingType.False)
        {
            var parent = (RepositoryTools.CreateStructure("/Root/Content/Docs", "DocumentLibrary")
                          ?? Content.Load("/Root/Content/Docs")).ContentHandler;
            var file = new File(parent)
            {
                VersioningMode = versioningType,
                ApprovingMode = approvingType
            };
            file.Save();

            return file;
        }
    }
}
