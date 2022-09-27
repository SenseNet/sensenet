using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ApplicationModel;
using SenseNet.Communication.Messaging;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.ContentRepository.Storage.Caching.DistributedActions;
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
        #region private class TestClusterChannel
        private class TestClusterChannel : ClusterChannel
        {
            public ClusterMessage ReceivedMessage { get; private set; }

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
                incomingMessages.Clear();
            }

            public override bool RestartingAllChannels => false;
            public override STT.Task RestartAllChannelsAsync(CancellationToken cancellationToken) => STT.Task.CompletedTask;
        }
        #endregion


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
        public async STT.Task Messaging_Serialization_DebugMessage()
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
        public async STT.Task Messaging_Serialization_WakeUp()
        {
            var message = new WakeUp("Target1");

            ClusterMemberInfo.Current = new ClusterMemberInfo { ClusterID = "Cluster1" };
            var services = new ServiceCollection()
                .AddSingleton(ClusterMemberInfo.Current)
                .AddSingleton(new ClusterMessageTypes { Types = TypeResolver.GetTypesByBaseType(typeof(ClusterMessage)) })
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
            Assert.AreEqual("WAKEUP", ((DebugMessage)received).Message);
            Assert.AreEqual("Target1", ((WakeUp)received).Target);
            Assert.AreEqual(true, received.SenderInfo.IsMe);
            Assert.AreEqual("Cluster1", received.SenderInfo.ClusterID);
        }

        [TestMethod]
        public async STT.Task Messaging_Serialization_NodeIdDependency()
        {
            var message = new NodeIdDependency.FireChangedDistributedAction(42);

            ClusterMemberInfo.Current = new ClusterMemberInfo { ClusterID = "Cluster1" };
            var services = new ServiceCollection()
                .AddSingleton(ClusterMemberInfo.Current)
                .AddSingleton(new ClusterMessageTypes { Types = TypeResolver.GetTypesByBaseType(typeof(ClusterMessage)) })
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
            Assert.AreEqual(42, ((NodeIdDependency.FireChangedDistributedAction)received).NodeId);
        }
        [TestMethod]
        public async STT.Task Messaging_Serialization_NodeTypeDependency()
        {
            var message = new NodeTypeDependency.FireChangedDistributedAction(43);

            ClusterMemberInfo.Current = new ClusterMemberInfo { ClusterID = "Cluster1" };
            var services = new ServiceCollection()
                .AddSingleton(ClusterMemberInfo.Current)
                .AddSingleton(new ClusterMessageTypes { Types = TypeResolver.GetTypesByBaseType(typeof(ClusterMessage)) })
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
            Assert.AreEqual(43, ((NodeTypeDependency.FireChangedDistributedAction)received).NodeTypeId);
        }
        [TestMethod]
        public async STT.Task Messaging_Serialization_PathDependency()
        {
            var message = new PathDependency.FireChangedDistributedAction("/Root/MyContent");

            ClusterMemberInfo.Current = new ClusterMemberInfo { ClusterID = "Cluster1" };
            var services = new ServiceCollection()
                .AddSingleton(ClusterMemberInfo.Current)
                .AddSingleton(new ClusterMessageTypes { Types = TypeResolver.GetTypesByBaseType(typeof(ClusterMessage)) })
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
            Assert.AreEqual("/Root/MyContent", ((PathDependency.FireChangedDistributedAction)received).Path);
        }

        //UnknownMessageType

        //SenseNet.ContentRepository.Storage.Caching.Dependency
        //	NodeIdDependency+FireChangedDistributedAction
        //	NodeTypeDependency+FireChangedDistributedAction
        //	PathDependency+FireChangedDistributedAction

        //SenseNet.ContentRepository.Search.Indexing.Activities
        //	DistributedIndexingActivity
        //		IndexingActivityBase
        //			DocumentIndexingActivity
        //				AddDocumentActivity
        //				UpdateDocumentActivity
        //			TreeIndexingActivity
        //				AddTreeActivity
        //				RemoveTreeActivity
        //			RebuildActivity
        //			RestoreActivity

        //TreeCache<Settings>.TreeCacheInvalidatorDistributedAction<Settings>()

    }
}
