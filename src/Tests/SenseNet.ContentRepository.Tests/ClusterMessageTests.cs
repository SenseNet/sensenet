using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Communication.Messaging;
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
        public async STT.Task Messaging_Serialization_()
        {
            var services = new ServiceCollection()
                .AddSingleton(ClusterMemberInfo.Current)
                //.AddSingleton<IClusterMessageFormatter, BinaryMessageFormatter>()
                .AddSingleton(new ClusterMessageTypes { Types = TypeResolver.GetTypesByBaseType(typeof(ClusterMessage)) })
                .AddSingleton<IClusterMessageFormatter, SnMessageFormatter>()
                .AddSingleton<IClusterChannel, TestClusterChannel>()
                .BuildServiceProvider();

            var message = new ContentTypeManagerResetDistributedAction();
            var channel = (TestClusterChannel)services.GetRequiredService<IClusterChannel>();

            // ACTION
            await channel.SendAsync(message, CancellationToken.None).ConfigureAwait(false);

            // ASSERT (serialized and deserialized but the type remains the same)
            var received = channel.ReceivedMessage;
            Assert.IsNotNull(received);
            Assert.AreNotSame(message, received);
            Assert.AreEqual(message.GetType(), received.GetType());
        }
    }
}
