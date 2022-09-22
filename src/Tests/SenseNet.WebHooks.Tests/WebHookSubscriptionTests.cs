using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security.Clients;
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

        [TestMethod, TestCategory("Services")]
        public async Task WebHookSubscription_Complex_CSrv()
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
                    file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();    // --> 2.0.L    --> no event on checkout
                    file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();     // --> 1.0.A    --> Modify,Approve

                    file.Index = 41;
                    file.SaveAsync(SavingMode.KeepVersion, CancellationToken.None)
                        .GetAwaiter().GetResult();  // --> Modify

                    file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();    // --> 2.0.L    --> no event on checkout

                    file.Index = 42;
                    file.SaveAsync(SavingMode.KeepVersion, CancellationToken.None)
                        .GetAwaiter().GetResult();  // no event on modification when locked 

                    file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();     // --> 1.0.A    --> Modify,Approve

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
                    file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();    // --> 2.0.L    --> no event on checkout
                    file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();     // --> 1.0.P    --> Modify,Pending

                    file.Index = 41;
                    file.SaveAsync(SavingMode.KeepVersion, CancellationToken.None).GetAwaiter().GetResult();  // --> 1.0.P    --> Modify

                    file.ApproveAsync(CancellationToken.None).GetAwaiter().GetResult();     // --> 1.0.A    --> Modify,Approve

                    file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();    // --> 2.0.L    --> no event on checkout
                    file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();     // --> 2.0.P    --> Modify,Pending
                    file.RejectAsync(CancellationToken.None).GetAwaiter().GetResult();      // --> 2.0.R    --> Modify,Reject

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
                    file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();    // --> 2.0.L    --> no event on checkout
                    file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();     // --> 2.0.A    --> Modify,Approve

                    file.Index = 41;
                    file.SaveAsync(SavingMode.KeepVersion, CancellationToken.None).GetAwaiter().GetResult();  // --> 2.0.A    --> Modify

                    file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();    // --> 3.0.L    --> no event on checkout

                    file.Index = 42;
                    file.SaveAsync(SavingMode.KeepVersion, CancellationToken.None).GetAwaiter().GetResult();  // no event on modification when locked 

                    file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();     // --> 3.0.A    --> Modify,Approve

                    file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();        // --> 4.0.A    --> Modify,Approve

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
                    file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();    // --> 2.0.L    --> no event on checkout
                    file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();     // --> 2.0.P    --> Modify,Pending

                    file.Index = 42;
                    file.SaveAsync(SavingMode.KeepVersion, CancellationToken.None).GetAwaiter().GetResult();  // --> Modify

                    file.ApproveAsync(CancellationToken.None).GetAwaiter().GetResult();     // --> 1.0.A    --> Modify,Approve

                    file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();    // --> 2.0.L    --> no event on checkout
                    file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();     // --> 2.0.P    --> Modify,Pending
                    file.RejectAsync(CancellationToken.None).GetAwaiter().GetResult();      // --> 2.0.R    --> Modify,Reject
                    file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();        // --> 3.0.P    --> Modify,Pending

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
                    file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();    // --> 0.2.L    --> no event on checkout
                    file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();     // --> 0.2.D    --> Modify,Draft

                    file.Index = 41;
                    file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();        // --> 0.3.D    --> Modify,Draft

                    file.PublishAsync(CancellationToken.None).GetAwaiter().GetResult();     // --> 1.0.A    --> Modify,Approve

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
                    file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();    // --> 0.2.L    --> no event on checkout
                    file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();     // --> 0.2.D    --> Modify,Draft

                    file.Index = 41;
                    file.SaveAsync(SavingMode.KeepVersion, CancellationToken.None).GetAwaiter().GetResult();  // --> Modify

                    file.PublishAsync(CancellationToken.None).GetAwaiter().GetResult();     // --> 0.2.P    --> Modify,Pending
                    file.ApproveAsync(CancellationToken.None).GetAwaiter().GetResult();     // --> 1.0.A    --> Modify,Approve

                    file.CheckOutAsync(CancellationToken.None).GetAwaiter().GetResult();    // --> 1.1.L    --> no event on checkout
                    file.CheckInAsync(CancellationToken.None).GetAwaiter().GetResult();     // --> 1.1.D    --> Modify,Draft

                    file.Index = 42;
                    file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();        // --> 1.2.D    --> Modify,Draft

                    file.PublishAsync(CancellationToken.None).GetAwaiter().GetResult();     // --> 1.2.P    --> Modify,Pending
                    file.RejectAsync(CancellationToken.None).GetAwaiter().GetResult();      // --> 1.2.R    --> Modify,Reject

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

                    // default properties are still there (merged with the custom json)
                    Assert.AreEqual(((Node)file).Id, request.NodeId);
                    Assert.AreEqual("Modify", request.EventName);
                }, payload: "{ \"text\": \"hello\" }");
        }

        [TestMethod]
        public async Task WebHookSubscription_Payload_Custom_WithParameters()
        {
            await Test_WebHook((file, client, subscription) =>
            {
                var node = (Node)file;
                var request = client.Requests.First();

                // custom properties are evaluated
                Assert.IsTrue(string.Equals(request.GetPostPropertyString("text"), "hello", StringComparison.InvariantCulture));
                Assert.AreEqual(DateTime.UtcNow.Date, request.GetPostPropertyDate("dateprop"));
                Assert.AreEqual(User.Current.Id, request.GetPostPropertyInt("currentuser"));
                Assert.AreEqual(node.Path, request.GetPostPropertyString("filepath"));
                Assert.AreEqual(node.Index, request.GetPostPropertyInt("index"));

                // default properties are still there (merged with the custom json)
                Assert.AreEqual(node.Id, request.NodeId);
                Assert.AreEqual("Modify", request.EventName);
                Assert.AreEqual(subscription.Id, request.GetPostPropertyInt("subscriptionId"));
            }, payload: @"
{ 
    ""text"": ""hello"",
    ""dateprop"": ""@@today@@"",
    ""currentuser"": @@currentuser@@,
    ""filepath"": ""@@content.Path@@"",
    ""index"": @@content.Index@@
}",
                useCurrentUser: true); // this is necessary for a real current user instead of a system account
        }

        internal async Task Test_WebHook(Action<object, TestWebHookClient, WebHookSubscription> assertAction,
            string subscribedEvents = null, string headers = null, string payload = null, bool useCurrentUser = false)
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
                    file.SaveAsync(SavingMode.KeepVersion, CancellationToken.None).GetAwaiter().GetResult();
                    return Task.FromResult((object)file);
                }, 
                assertAction,
                subscribedEvents,
                headers,
                payload,
                useCurrentUser);
        }

        internal async Task Test_WebHook(Func<Task> actionBeforeSubscription, Func<Task<object>> actionAfterSubscription,
            Action<object, TestWebHookClient, WebHookSubscription> assertAction, 
            string subscribedEvents = null, string headers = null, string payload = null, bool useCurrentUser = false)
        {
            // subscribe to all events by default
            subscribedEvents ??= @"""All""";

            var store = new TestWebHookSubscriptionStore(new WebHookSubscription[0]);
            var webHookClient = new TestWebHookClient();

            await Test(useCurrentUser, (builder) =>
                {
                    builder
                        .UseComponent(new WebHookComponent())
                        .UseEventDistributor(new EventDistributor())
                        .AddAsyncEventProcessors(new LocalWebHookProcessor(
                            store,
                            webHookClient,
                            Options.Create(new ClientStoreOptions()),
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

        protected override RepositoryBuilder CreateRepositoryBuilderForTest(Action<IServiceCollection> modifyServices = null)
        {
            return base.CreateRepositoryBuilderForTest(services =>
            {
                services.AddSenseNetWebHooks();
                modifyServices?.Invoke(services);
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
            wh.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

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
            file.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            return file;
        }
    }
}
