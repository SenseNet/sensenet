using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using MailKit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.ApplicationModel;
using SenseNet.Communication.Messaging;
using SenseNet.Configuration;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Search.Indexing.Activities;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.ContentRepository.Storage.Caching.DistributedActions;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search.Indexing;
using SenseNet.Storage.DistributedApplication.Messaging;
using SenseNet.Testing;
using SenseNet.Tests.Core;
using SenseNet.Tools;
using static SenseNet.ContentRepository.Schema.ContentTypeManager;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ClusterMessageTests : TestBase
    {
        // Note that the UnknownMessageType is not tested here because it is only a placeholder for error handling and
        // is not designed for serializing and sending away.

        private class TestIndexManager : IndexManager
        {
            public TestIndexManager() : base(null, null, null) { }
            public override IndexDocument CompleteIndexDocument(IndexDocumentData docData)
            {
                return docData.IndexDocument;
            }
        }

        #region private class TestClusterChannel
        private class TestClusterChannel : ClusterChannel
        {
            public ClusterMessage ReceivedMessage { get; private set; }
            public List<ClusterMessage> ReceivedMessages { get; } = new();

            public TestClusterChannel(IClusterMessageFormatter formatter, ClusterMemberInfo clusterMemberInfo)
                : base(formatter, clusterMemberInfo) { }

            protected override STT.Task InternalSendAsync(Stream messageBody, bool isDebugMessage, CancellationToken cancellationToken)
            {
                this.OnMessageReceived(messageBody);
                return STT.Task.CompletedTask;
            }

            protected internal override void OnMessageReceived(Stream messageBody)
            {
                base.OnMessageReceived(messageBody);

                var baseAcc = new TypeAccessor(typeof(ClusterChannel));
                var incomingMessages = (List<ClusterMessage>)baseAcc.GetStaticField("_incomingMessages");
                ReceivedMessage = incomingMessages.FirstOrDefault();
                ReceivedMessages.Add(ReceivedMessage);
                incomingMessages.Clear();
            }

            public override bool RestartingAllChannels => false;
            public override STT.Task RestartAllChannelsAsync(CancellationToken cancellationToken) => STT.Task.CompletedTask;
        }
        #endregion

        private async STT.Task SerializationTest<T>(T message, Action<T> assertion) where T : ClusterMessage
        {
            var types = new List<Type>(TypeResolver.GetTypesByBaseType(typeof(ClusterMessage)));
            types.Add(typeof(TreeCache<Settings>.TreeCacheInvalidatorDistributedAction<Settings>));

            var services = new ServiceCollection()
                .AddSingleton<IEnumerable<JsonConverter>>(new JsonConverter[] {new IndexFieldJsonConverter()})
                .AddSingleton(ClusterMemberInfo.Current)
                .AddSingleton(new ClusterMessageTypes {Types = types })
                .AddSingleton<IClusterMessageFormatter, SnMessageFormatter>()
                .AddSingleton<IClusterChannel, TestClusterChannel>()
                .AddSingleton<IIndexManager, TestIndexManager>()
                .BuildServiceProvider();
            Providers.Instance = new Providers(services);

            var channel = (TestClusterChannel)services.GetRequiredService<IClusterChannel>();

            // ACTION
            await channel.SendAsync(message, CancellationToken.None).ConfigureAwait(false);

            // ASSERT (serialized and deserialized but the type remains the same)
            var received = channel.ReceivedMessage;
            Assert.IsNotNull(received);
            Assert.AreNotSame(message, received);
            Assert.AreEqual(message.GetType(), received.GetType());
            assertion((T) received);
        }

        [TestMethod]
        public async STT.Task Messaging_Serialization_DebugMessageAndClusterMemberInfo()
        {
            var message = new DebugMessage {Message = "Serialization test", SenderInfo = ClusterMemberInfo.Current };

            ClusterMemberInfo.Current = new ClusterMemberInfo {ClusterID = "Cluster1"};
            var services = new ServiceCollection()
                .AddSingleton(ClusterMemberInfo.Current)
                .AddSingleton(new ClusterMessageTypes { Types = TypeResolver.GetTypesByBaseType(typeof(ClusterMessage)) })
                //.AddSingleton<IClusterMessage, MyClMsg>()
                .AddSingleton<IClusterMessageFormatter, SnMessageFormatter>()
                .AddSingleton<IClusterChannel, TestClusterChannel>()
                .BuildServiceProvider();

            var channel = (TestClusterChannel)services.GetRequiredService<IClusterChannel>();

            // ACTION
            await channel.SendAsync(message, CancellationToken.None).ConfigureAwait(false);

            // ASSERT (serialized and deserialized but the type remains the same)
            var received = channel.ReceivedMessage;
            Assert.IsNotNull(received);
            Assert.AreNotSame(message, received);
            Assert.AreEqual(message.GetType(), received.GetType());
            Assert.AreEqual("Serialization test", ((DebugMessage)received).Message);
            Assert.AreEqual(true, received.SenderInfo.IsMe);
            Assert.AreEqual("Cluster1", received.SenderInfo.ClusterID);
        }
        [TestMethod]
        public async STT.Task Messaging_Serialization_AllParameterless()
        {
            var messages = new ClusterMessage[]
            {
                new PingMessage(),
                new PongMessage(),
                new CacheCleanAction(),
                new CleanupNodeCacheAction(),
                new StorageSchema.NodeTypeManagerRestartDistributedAction(),
                new ApplicationStorage.ApplicationStorageInvalidateDistributedAction(),
                new DeviceManager.DeviceManagerResetDistributedAction(),
                new RepositoryVersionInfo.RepositoryVersionInfoResetDistributedAction(),
                new LoggingSettings.UpdateCategoriesDistributedAction(),
                new ContentTypeManagerResetDistributedAction(),
                new SenseNetResourceManager.ResourceManagerResetDistributedAction(),
            };

            var services = new ServiceCollection()
                .AddSingleton(ClusterMemberInfo.Current)
                .AddSingleton(new ClusterMessageTypes { Types = TypeResolver.GetTypesByBaseType(typeof(ClusterMessage)) })
                .AddSingleton<IClusterMessageFormatter, SnMessageFormatter>()
                .AddSingleton<IClusterChannel, TestClusterChannel>()
                .BuildServiceProvider();

            var channel = (TestClusterChannel)services.GetRequiredService<IClusterChannel>();

            foreach (var message in messages)
            {
                // ACTION
                await channel.SendAsync(message, CancellationToken.None).ConfigureAwait(false);
                
                // ASSERT (serialized and deserialized but the type remains the same)
                var received = channel.ReceivedMessage;
                Assert.IsNotNull(received);
                Assert.AreNotSame(message, received);
                Assert.AreEqual(message.GetType(), received.GetType());
                Assert.AreEqual(true, received.SenderInfo.IsMe);
            }
        }

        [TestMethod]
        public async STT.Task Messaging_Serialization_WakeUp()
        {
            await SerializationTest<WakeUp>(
                new WakeUp("Target1"),
                received =>
                {
                    Assert.AreEqual("Target1", received.Target);
                });
        }

        [TestMethod]
        public async STT.Task Messaging_Serialization_NodeIdDependency()
        {
            await SerializationTest<NodeIdDependency.FireChangedDistributedAction>(
                new NodeIdDependency.FireChangedDistributedAction(42),
                received =>
                {
                    Assert.AreEqual(42, received.NodeId);
                });
        }
        [TestMethod]
        public async STT.Task Messaging_Serialization_NodeTypeDependency()
        {
            await SerializationTest<NodeTypeDependency.FireChangedDistributedAction>(
                new NodeTypeDependency.FireChangedDistributedAction(423),
                received =>
                {
                    Assert.AreEqual(423, received.NodeTypeId);
                });
        }
        [TestMethod]
        public async STT.Task Messaging_Serialization_PathDependency()
        {
            await SerializationTest<PathDependency.FireChangedDistributedAction>(
                new PathDependency.FireChangedDistributedAction("/Root/MyContent"),
                received =>
                {
                    Assert.AreEqual("/Root/MyContent", received.Path);
                });
        }


        [TestMethod]
        public async STT.Task Messaging_Serialization_AddDocumentActivity()
        {
            var indexDocument = new IndexDocument();
            indexDocument.Add(new IndexField("Integer1", 123, IndexingMode.NotAnalyzed, IndexStoringMode.No, IndexTermVector.Default));
            var activity = new AddDocumentActivity
            {
                Id = 42,
                ActivityType = IndexingActivityType.AddDocument,
                CreationDate = new DateTime(2022, 09, 28, 01, 07, 28),
                RunningState = IndexingActivityRunningState.Waiting,
                NodeId = 43,
                VersionId = 44,
                Path = "/Root/Path1",
                VersionTimestamp = 42424242,
                Extension = "{\"LastPublicVersionId\":159,\"LastDraftVersionId\":159,\"Delete\":[],\"Reindex\":[]}",
            };
            activity.SetDocument(indexDocument);
            activity.IndexDocumentData = new IndexDocumentData(indexDocument, null)
            {
                NodeTypeId = 42,
                VersionId = 44,
                NodeId = 43,
                ParentId = 45,
                Path = "/Root/Path1",
                IsSystem = true,
                IsLastDraft = true,
                IsLastPublic = true,
                NodeTimestamp = 43434343,
                VersionTimestamp = 42424242,
            };


            await SerializationTest<AddDocumentActivity>(activity, received =>
            {
                Assert.AreEqual(42, received.Id);
                Assert.IsNotNull(received.IndexDocumentData);
                Assert.AreEqual(123, received.Document.Fields["Integer1"].IntegerValue);
            });
        }
        [TestMethod]
        public async STT.Task Messaging_Serialization_UpdateDocumentActivity()
        {
            var indexDocument = new IndexDocument();
            indexDocument.Add(new IndexField("Integer1", 123, IndexingMode.NotAnalyzed, IndexStoringMode.No, IndexTermVector.Default));
            var activity = new UpdateDocumentActivity
            {
                Id = 42,
                ActivityType = IndexingActivityType.UpdateDocument,
                CreationDate = new DateTime(2022, 09, 28, 01, 07, 28),
                RunningState = IndexingActivityRunningState.Waiting,
                NodeId = 43,
                VersionId = 44,
                Path = "/Root/Path1",
                VersionTimestamp = 42424242,
                Extension = "{\"LastPublicVersionId\":159,\"LastDraftVersionId\":159,\"Delete\":[],\"Reindex\":[]}",
            };
            activity.SetDocument(indexDocument);
            activity.IndexDocumentData = new IndexDocumentData(indexDocument, null)
            {
                NodeTypeId = 42,
                VersionId = 44,
                NodeId = 43,
                ParentId = 45,
                Path = "/Root/Path1",
                IsSystem = true,
                IsLastDraft = true,
                IsLastPublic = true,
                NodeTimestamp = 43434343,
                VersionTimestamp = 42424242,
            };


            await SerializationTest<UpdateDocumentActivity>(activity, received =>
            {
                Assert.AreEqual(42, received.Id);
                Assert.IsNotNull(received.IndexDocumentData);
                Assert.AreEqual(123, received.Document.Fields["Integer1"].IntegerValue);
            });
        }
        [TestMethod]
        public async STT.Task Messaging_Serialization_AddTreeActivity()
        {
            var activity = new AddTreeActivity
            {
                Id = 42,
                ActivityType = IndexingActivityType.AddTree,
                Path = "/Root/MyTree"
            };

            await SerializationTest<AddTreeActivity>(activity, received =>
            {
                Assert.AreEqual(42, received.Id);
                Assert.AreEqual("/Root/MyTree", received.Path);
            });
        }
        [TestMethod]
        public async STT.Task Messaging_Serialization_RemoveTreeActivity()
        {
            var activity = new RemoveTreeActivity
            {
                Id = 42,
                ActivityType = IndexingActivityType.RemoveTree,
                Path = "/Root/MyTree"
            };

            await SerializationTest<RemoveTreeActivity>(activity, received =>
            {
                Assert.AreEqual(42, received.Id);
                Assert.AreEqual("/Root/MyTree", received.Path);
            });
        }
        [TestMethod]
        public async STT.Task Messaging_Serialization_RebuildActivity()
        {
            var activity = new RebuildActivity
            {
                Id = 42,
                ActivityType = IndexingActivityType.Rebuild,
                Path = "/Root/MyTree"
            };

            await SerializationTest<RebuildActivity>(activity, received =>
            {
                Assert.AreEqual(42, received.Id);
                Assert.AreEqual("/Root/MyTree", received.Path);
            });
        }
        [TestMethod]
        public async STT.Task Messaging_Serialization_RestoreActivity()
        {
            var activity = new RestoreActivity
            {
                Id = 42,
                ActivityType = IndexingActivityType.Restore,
            };

            await SerializationTest<RestoreActivity>(activity, received =>
            {
                Assert.AreEqual(42, received.Id);
            });
        }
        [TestMethod]
        public async STT.Task Messaging_Serialization_TreeCache()
        {
            var activity = new TreeCache<Settings>.TreeCacheInvalidatorDistributedAction<Settings>();

            await SerializationTest<TreeCache<Settings>.TreeCacheInvalidatorDistributedAction<Settings>>(activity, activity =>
            {
                Assert.IsNotNull(activity);
            });
        }

        [TestMethod]
        public void Messaging_Serialization_IndexDocument()
        {
            var indexFields = new[]
            {
                new IndexField("String1", "Value1", IndexingMode.Analyzed, IndexStoringMode.No, IndexTermVector.Default),
                new IndexField("String2", "Value2", IndexingMode.Analyzed, IndexStoringMode.No, IndexTermVector.Yes),
                new IndexField("String3", "Value3", IndexingMode.Analyzed, IndexStoringMode.Yes, IndexTermVector.Default),
                new IndexField("String4", "Value4", IndexingMode.Analyzed, IndexStoringMode.No, IndexTermVector.Default),
                new IndexField("String5", "Value5", IndexingMode.NotAnalyzed, IndexStoringMode.No, IndexTermVector.Default),
                new IndexField("String6", "Value6", IndexingMode.No, IndexStoringMode.Yes, IndexTermVector.No),
                new IndexField("Integer1", 123, IndexingMode.Analyzed, IndexStoringMode.No, IndexTermVector.Default),
                new IndexField("Long1", 1234L, IndexingMode.Analyzed, IndexStoringMode.No, IndexTermVector.Default),
                new IndexField("Bool1", true, IndexingMode.Analyzed, IndexStoringMode.No, IndexTermVector.Default),
                new IndexField("Single1", 3.14159f, IndexingMode.Analyzed, IndexStoringMode.No, IndexTermVector.Default),
                new IndexField("Double1", Math.PI, IndexingMode.Analyzed, IndexStoringMode.No, IndexTermVector.Default),
                new IndexField("Date1", DateTime.UtcNow, IndexingMode.Analyzed, IndexStoringMode.No, IndexTermVector.Default),
                new IndexField("Strings1", new []{"a", "b"}, IndexingMode.Analyzed, IndexStoringMode.No, IndexTermVector.Default),
                new IndexField("Integers1", new []{1, 2, 42}, IndexingMode.Analyzed, IndexStoringMode.No, IndexTermVector.Default),
            };
            var indexDocument = new IndexDocument();
            foreach (IndexField indexField in indexFields)
                indexDocument.Add(indexField);

            var text = JsonConvert.SerializeObject(indexDocument, new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new IndexFieldJsonConverter() },
                NullValueHandling = NullValueHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                Formatting = Formatting.Indented
            });

            var deserialized = JsonConvert.DeserializeObject<IndexDocument>(text, new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new IndexFieldJsonConverter() },
            });

            var i = 0;
            foreach (var indexField in deserialized)
            {
                var message = CompareIndexFields(indexFields[i++], indexField);
                Assert.AreEqual("ok", message);
            }
        }
        private string CompareIndexFields(IndexField expected, IndexField actual)
        {
            if (expected.Name != actual.Name)
                return $"Name: {expected.Name} != {actual.Name}";
            if (expected.Type != actual.Type)
                return $"Type: {expected.Type} != {actual.Type}";
            if (expected.Mode != actual.Mode)
                return $"Mode: {expected.Mode} != {actual.Mode}";
            if (expected.Store != actual.Store)
                return $"Store: {expected.Store} != {actual.Store}";
            if (expected.TermVector != actual.TermVector)
                return $"TermVector: {expected.TermVector} != {actual.TermVector}";
            if (expected.StringValue != actual.StringValue)
                return $"StringValue: {expected.StringValue} != {actual.StringValue}";
            if (string.Join(",", expected.StringArrayValue ?? Array.Empty<string>()) !=
                string.Join(",", actual.StringArrayValue ?? Array.Empty<string>()))
                return $"StringArrayValue: {expected.StringArrayValue} != {actual.StringArrayValue}";
            if (expected.BooleanValue != actual.BooleanValue)
                return $"BooleanValue: {expected.BooleanValue} != {actual.BooleanValue}";
            if (expected.IntegerValue != actual.IntegerValue)
                return $"IntegerValue: {expected.IntegerValue} != {actual.IntegerValue}";
            if (string.Join(",", expected.IntegerArrayValue?.Select(x=>x.ToString()) ?? Array.Empty<string>()) !=
                string.Join(",", actual.IntegerArrayValue?.Select(x => x.ToString()) ?? Array.Empty<string>()))
                return $"IntegerArrayValue: {expected.IntegerArrayValue} != {actual.IntegerArrayValue}";
            if (expected.LongValue != actual.LongValue)
                return $"LongValue: {expected.LongValue} != {actual.LongValue}";
            if (expected.SingleValue != actual.SingleValue)
                return $"SingleValue: {expected.SingleValue} != {actual.SingleValue}";
            if (expected.DoubleValue != actual.DoubleValue)
                return $"DoubleValue: {expected.DoubleValue} != {actual.DoubleValue}";
            if (expected.DateTimeValue != actual.DateTimeValue)
                return $"DateTimeValue: {expected.DateTimeValue} != {actual.DateTimeValue}";
            if (expected.ValueAsString != actual.ValueAsString)
                return $"ValueAsString: {expected.ValueAsString} != {actual.ValueAsString}";
            return "ok";
        }

        [TestMethod]
        public void Messaging_Serialization_WhenSaveNode()
        {
            Test2(services =>
            {
                services
                    .AddSingleton<IEnumerable<JsonConverter>>(new JsonConverter[] {new IndexFieldJsonConverter()})
                    .AddSingleton(ClusterMemberInfo.Current)
                    .AddSingleton(new ClusterMessageTypes
                        {Types = TypeResolver.GetTypesByBaseType(typeof(ClusterMessage))})
                    .AddSingleton<IClusterMessageFormatter, SnMessageFormatter>()
                    .AddSingleton<IClusterChannel, TestClusterChannel>();
            }, () =>
            {
                var folder = new SystemFolder(Repository.Root) {Name = "TestFolder1", Index = 42};

                // ACTION
                folder.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                var channel = (TestClusterChannel)Providers.Instance.Services.GetRequiredService<IClusterChannel>();
                var receivedMessages = channel.ReceivedMessages;
                var addDocActivity = receivedMessages.FirstOrDefault(x => x is AddDocumentActivity);
                Assert.IsNotNull(addDocActivity);
            });
        }
        [TestMethod]
        public void Messaging_Serialization_WhenSaveNode_OnAnotherNlbNode()
        {
            Test2(services =>
            {
                services
                    .AddSingleton<IEnumerable<JsonConverter>>(new JsonConverter[] { new IndexFieldJsonConverter() })
                    .AddSingleton(ClusterMemberInfo.Current)
                    .AddSingleton(new ClusterMessageTypes
                        { Types = TypeResolver.GetTypesByBaseType(typeof(ClusterMessage)) })
                    .AddSingleton<IClusterMessageFormatter, SnMessageFormatter>()
                    .AddSingleton<IClusterChannel, TestClusterChannel>();
            }, () =>
            {
                // ALIGN (simulates the receiver in the nlb cluster)
                var payload = @"{
  ""Type"": ""SenseNet.ContentRepository.Search.Indexing.Activities.AddDocumentActivity"",
  ""Msg"": {
    ""Versioning"": {
      ""LastPublicVersionId"": 403,
      ""LastDraftVersionId"": 403,
      ""Delete"": [],
      ""Reindex"": []
    },
    ""Id"": 1,
    ""ActivityType"": 1,
    ""CreationDate"": ""0001-01-01T00:00:00Z"",
    ""RunningState"": 0,
    ""NodeId"": 1390,
    ""VersionId"": 403,
    ""Path"": ""/root/testfolder1"",
    ""VersionTimestamp"": 1269,
    ""Extension"": ""{\""LastPublicVersionId\"":403,\""LastDraftVersionId\"":403,\""Delete\"":[],\""Reindex\"":[]}"",
    ""IndexDocumentData"": {
      ""IndexDocument"": [
        {
          ""Name"": ""DisplayName"",
          ""Type"": ""String"",
          ""Value"": ""testfolder1""
        },
        {
          ""Name"": ""Description"",
          ""Type"": ""String"",
          ""Value"": """"
        },
        {
          ""Name"": ""Version"",
          ""Type"": ""String"",
          ""Store"": ""Yes"",
          ""Value"": ""v1.0.a""
        },
        {
          ""Name"": ""TrashDisabled"",
          ""Type"": ""Bool"",
          ""Value"": false
        },
        {
          ""Name"": ""PreviewEnabled"",
          ""Type"": ""StringArray"",
          ""Value"": [""$0""]
        },
        {
          ""Name"": ""PreviewEnabled_sort"",
          ""Type"": ""String"",
          ""Mode"": ""Analyzed"",
          ""Store"": ""No"",
          ""TermVector"": ""No"",
          ""Value"": ""$0""
        },
        {
          ""Name"": ""Id"",
          ""Type"": ""Int"",
          ""Store"": ""Yes"",
          ""Value"": 1390
        },
        {
          ""Name"": ""OwnerId"",
          ""Type"": ""Int"",
          ""Store"": ""Yes"",
          ""Value"": 1
        },
        {
          ""Name"": ""Owner"",
          ""Type"": ""IntArray"",
          ""Value"": [1]
        },
        {
          ""Name"": ""VersionId"",
          ""Type"": ""Int"",
          ""Store"": ""Yes"",
          ""Value"": 403
        },
        {
          ""Name"": ""Type"",
          ""Type"": ""String"",
          ""Store"": ""Yes"",
          ""Value"": ""systemfolder""
        },
        {
          ""Name"": ""TypeIs"",
          ""Type"": ""StringArray"",
          ""Store"": ""No"",
          ""Value"": [""genericcontent"",""folder"",""systemfolder""]
        },
        {
          ""Name"": ""Icon"",
          ""Type"": ""String"",
          ""Value"": ""systemfolder""
        },
        {
          ""Name"": ""CreatedById"",
          ""Type"": ""Int"",
          ""Store"": ""Yes"",
          ""Value"": 1
        },
        {
          ""Name"": ""ModifiedById"",
          ""Type"": ""Int"",
          ""Store"": ""Yes"",
          ""Value"": 1
        },
        {
          ""Name"": ""IsFolder"",
          ""Type"": ""Bool"",
          ""Value"": true
        },
        {
          ""Name"": ""Hidden"",
          ""Type"": ""Bool"",
          ""Value"": false
        },
        {
          ""Name"": ""Index"",
          ""Type"": ""Int"",
          ""Value"": 42
        },
        {
          ""Name"": ""EnableLifespan"",
          ""Type"": ""Bool"",
          ""Value"": false
        },
        {
          ""Name"": ""ValidFrom"",
          ""Type"": ""DateTime"",
          ""Value"": ""2022-10-03T05:22:00Z""
        },
        {
          ""Name"": ""ValidTill"",
          ""Type"": ""DateTime"",
          ""Value"": ""2022-10-03T05:22:00Z""
        },
        {
          ""Name"": ""AllowedChildTypes"",
          ""Type"": ""StringArray"",
          ""Value"": [""""]
        },
        {
          ""Name"": ""EffectiveAllowedChildTypes"",
          ""Type"": ""StringArray"",
          ""Value"": [""ContentType"",""GenericContent"",""Application"",""ApplicationOverride"",""ClientApplication"",""GenericODataApplication"",""WebServiceApplication"",""ContentLink"",""EmailTemplate"",""FieldSettingContent"",""BinaryFieldSetting"",""DateTimeFieldSetting"",""HyperLinkFieldSetting"",""IntegerFieldSetting"",""NullFieldSetting"",""NumberFieldSetting"",""CurrencyFieldSetting"",""ReferenceFieldSetting"",""TextFieldSetting"",""LongTextFieldSetting"",""ShortTextFieldSetting"",""ChoiceFieldSetting"",""PermissionChoiceFieldSetting"",""YesNoFieldSetting"",""PasswordFieldSetting"",""XmlFieldSetting"",""File"",""ExecutableFile"",""Image"",""PreviewImage"",""Settings"",""IndexingSettings"",""LoggingSettings"",""SystemFile"",""Resource"",""Folder"",""ContentList"",""Aspect"",""ItemList"",""CustomList"",""EventList"",""LinkList"",""MemoList"",""TaskList"",""Library"",""DocumentLibrary"",""ImageLibrary"",""Device"",""Domain"",""Domains"",""Email"",""OrganizationalUnit"",""PortalRoot"",""ProfileDomain"",""Profiles"",""RuntimeContentContainer"",""Sites"",""SmartFolder"",""SystemFolder"",""Resources"",""TrashBag"",""Workspace"",""TrashBin"",""UserProfile"",""Group"",""SharingGroup"",""ListItem"",""CalendarEvent"",""CustomListItem"",""Link"",""Memo"",""Task"",""Query"",""User""]
        },
        {
          ""Name"": ""VersioningMode"",
          ""Type"": ""StringArray"",
          ""Value"": [""$0""]
        },
        {
          ""Name"": ""VersioningMode_sort"",
          ""Type"": ""String"",
          ""Mode"": ""Analyzed"",
          ""Store"": ""No"",
          ""TermVector"": ""No"",
          ""Value"": ""$0""
        },
        {
          ""Name"": ""InheritableVersioningMode"",
          ""Type"": ""StringArray"",
          ""Value"": [""$0""]
        },
        {
          ""Name"": ""InheritableVersioningMode_sort"",
          ""Type"": ""String"",
          ""Mode"": ""Analyzed"",
          ""Store"": ""No"",
          ""TermVector"": ""No"",
          ""Value"": ""$0""
        },
        {
          ""Name"": ""CreatedBy"",
          ""Type"": ""IntArray"",
          ""Value"": [1]
        },
        {
          ""Name"": ""VersionCreatedBy"",
          ""Type"": ""IntArray"",
          ""Value"": [1]
        },
        {
          ""Name"": ""CreationDate"",
          ""Type"": ""DateTime"",
          ""Value"": ""2022-10-03T05:22:00Z""
        },
        {
          ""Name"": ""VersionCreationDate"",
          ""Type"": ""DateTime"",
          ""Value"": ""2022-10-03T05:22:00Z""
        },
        {
          ""Name"": ""ModifiedBy"",
          ""Type"": ""IntArray"",
          ""Value"": [1]
        },
        {
          ""Name"": ""VersionModifiedBy"",
          ""Type"": ""IntArray"",
          ""Value"": [1]
        },
        {
          ""Name"": ""ModificationDate"",
          ""Type"": ""DateTime"",
          ""Store"": ""Yes"",
          ""Value"": ""2022-10-03T05:22:00Z""
        },
        {
          ""Name"": ""VersionModificationDate"",
          ""Type"": ""DateTime"",
          ""Value"": ""2022-10-03T05:22:00Z""
        },
        {
          ""Name"": ""ApprovingMode"",
          ""Type"": ""StringArray"",
          ""Value"": [""$0""]
        },
        {
          ""Name"": ""ApprovingMode_sort"",
          ""Type"": ""String"",
          ""Mode"": ""Analyzed"",
          ""Store"": ""No"",
          ""TermVector"": ""No"",
          ""Value"": ""$0""
        },
        {
          ""Name"": ""InheritableApprovingMode"",
          ""Type"": ""StringArray"",
          ""Value"": [""$0""]
        },
        {
          ""Name"": ""InheritableApprovingMode_sort"",
          ""Type"": ""String"",
          ""Mode"": ""Analyzed"",
          ""Store"": ""No"",
          ""TermVector"": ""No"",
          ""Value"": ""$0""
        },
        {
          ""Name"": ""Locked"",
          ""Type"": ""Bool"",
          ""Value"": false
        },
        {
          ""Name"": ""CheckedOutTo"",
          ""Type"": ""String"",
          ""Value"": ""null""
        },
        {
          ""Name"": ""SavingState"",
          ""Type"": ""StringArray"",
          ""Value"": [""$0""]
        },
        {
          ""Name"": ""SavingState_sort"",
          ""Type"": ""String"",
          ""Mode"": ""Analyzed"",
          ""Store"": ""No"",
          ""TermVector"": ""No"",
          ""Value"": ""$0""
        },
        {
          ""Name"": ""ExtensionData"",
          ""Type"": ""String"",
          ""Value"": """"
        },
        {
          ""Name"": ""BrowseApplication"",
          ""Type"": ""String"",
          ""Value"": ""null""
        },
        {
          ""Name"": ""Approvable"",
          ""Type"": ""Bool"",
          ""Value"": false
        },
        {
          ""Name"": ""IsTaggable"",
          ""Type"": ""Bool"",
          ""Value"": false
        },
        {
          ""Name"": ""IsRateable"",
          ""Type"": ""Bool"",
          ""Value"": false
        },
        {
          ""Name"": ""RateStr"",
          ""Type"": ""String"",
          ""Value"": """"
        },
        {
          ""Name"": ""RateAvg"",
          ""Type"": ""Double"",
          ""Value"": 0.0
        },
        {
          ""Name"": ""RateCount"",
          ""Type"": ""Int"",
          ""Value"": 0
        },
        {
          ""Name"": ""Rate"",
          ""Type"": ""String"",
          ""Value"": ""sensenet.contentrepository.fields.votedata""
        },
        {
          ""Name"": ""Publishable"",
          ""Type"": ""Bool"",
          ""Value"": false
        },
        {
          ""Name"": ""CheckInComments"",
          ""Type"": ""String"",
          ""Value"": """"
        },
        {
          ""Name"": ""RejectReason"",
          ""Type"": ""String"",
          ""Value"": """"
        },
        {
          ""Name"": ""Workspace"",
          ""Type"": ""String"",
          ""Store"": ""Yes"",
          ""Value"": ""null""
        },
        {
          ""Name"": ""Sharing"",
          ""Type"": ""StringArray"",
          ""Value"": [""""]
        },
        {
          ""Name"": ""IsInherited"",
          ""Type"": ""Bool"",
          ""Mode"": ""Analyzed"",
          ""Store"": ""Yes"",
          ""Value"": true
        },
        {
          ""Name"": ""IsMajor"",
          ""Type"": ""Bool"",
          ""Mode"": ""Analyzed"",
          ""Store"": ""Yes"",
          ""Value"": true
        },
        {
          ""Name"": ""IsPublic"",
          ""Type"": ""Bool"",
          ""Mode"": ""Analyzed"",
          ""Store"": ""Yes"",
          ""Value"": true
        },
        {
          ""Name"": ""_Text"",
          ""Type"": ""String"",
          ""Mode"": ""Analyzed"",
          ""Store"": ""No"",
          ""Value"": ""testfolder1\r\nv1.0.a\r\n1390\r\n1\r\n403\r\nsystemfolder\r\nsystemfolder\r\n1\r\n1\r\n42\r\n0\r\n0\r\nsensenet.contentrepository.fields.votedata\r\n""
        }
      ],
      ""IndexDocumentSize"": 7341,
      ""NodeTypeId"": 5,
      ""VersionId"": 403,
      ""NodeId"": 1390,
      ""Path"": ""/Root/TestFolder1"",
      ""ParentId"": 2,
      ""IsSystem"": true,
      ""IsLastDraft"": true,
      ""IsLastPublic"": true,
      ""NodeTimestamp"": 3807,
      ""VersionTimestamp"": 1269
    },
    ""SenderInfo"": {
      ""InstanceID"": ""150bf279-9c69-4796-b58f-843fac327251"",
      ""Machine"": ""169.254.144.229"",
      ""NeedToRecover"": true,
      ""IsMe"": true
    }
  }
}";
                var stream = RepositoryTools.GetStreamFromString(payload);
                var formatter = Providers.Instance.Services.GetRequiredService<IClusterMessageFormatter>();
                var message = formatter.Deserialize(stream);
                var action = (DistributedAction) message;

                // ACTION
                action.DoActionAsync(true, false, CancellationToken.None).GetAwaiter().GetResult();

                // ASSERT (node does not exist but the received activity is executed)
                STT.Task.Delay(10).GetAwaiter().GetResult();
                var nodeId = 1390; // see payload.Msg.NodeId
                var versionId = 403; // see payload.Msg.VersionId
                Assert.IsNull(Node.LoadNodeAsync(nodeId, CancellationToken.None).GetAwaiter().GetResult());
                var hitId = CreateSafeContentQuery("+Name:TestFolder1 +Index:42 .AUTOFILTERS:OFF")
                    .ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult().Identifiers.FirstOrDefault();
                Assert.AreEqual(nodeId, hitId);

                var indexDocFields = Providers.Instance.SearchEngine.IndexingEngine
                    .GetIndexDocumentByVersionId(versionId);
                Assert.AreEqual("testfolder1", indexDocFields["DisplayName"]);
                Assert.AreEqual("systemfolder", indexDocFields["Type"]);
                Assert.AreEqual("/root/testfolder1", indexDocFields["Path"]);
                Assert.AreEqual("2", indexDocFields["ParentId"]);
                Assert.AreEqual("yes", indexDocFields["IsSystemContent"]);
            });
        }
    }
}
