using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Diagnostics;
using SenseNet.Events;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Tests.Core;
using SenseNet.Tools.Diagnostics;
using T = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class SnEventTests : TestBase
    {
        #region Additional classes

        private class TestObserver1 : NodeObserver { }
        private class TestObserver2 : NodeObserver { }
        private class TestObserver3 : NodeObserver { }

        private class TestAuditLogEventProcessor : IEventProcessor
        {
            public async T.Task ProcessEventAsync(ISnEvent snEvent)
            {
                using (var op = SnTrace.Test.StartOperation($"ProcessAuditEvent {GetType().Name} {snEvent.GetType().Name}"))
                {
                    await T.Task.Delay(5).ConfigureAwait(false);
                    op.Successful = true;
                }
            }
        }

        private abstract class TestEventProcessor : IEventProcessor
        {
            public async T.Task ProcessEventAsync(ISnEvent snEvent)
            {
                using (var op = SnTrace.Test.StartOperation($"ProcessEvent {GetType().Name} {snEvent.GetType().Name}"))
                {
                    await T.Task.Delay(50).ConfigureAwait(false);
                    op.Successful = true;
                }
            }
        }
        private class TestPushNotificationEventProcessor : TestEventProcessor { }
        private class TestWebHookEventProcessor : TestEventProcessor { }
        private class TestEmailSenderEventProcessor : TestEventProcessor { }
        
        private class TestInternalEvent : ISnEvent, IInternalEvent //UNDONE:<?event: write test to this event.
        {
            public INodeEventArgs NodeEventArgs { get; }

            public TestInternalEvent(INodeEventArgs args)
            {
                this.NodeEventArgs = args;
            }
        }

        private class TestEventDistributor : EventDistributor
        {
            protected override async T.Task<bool> FireCancellableNodeObserverEventAsync(ISnCancellableEvent snEvent, NodeObserver nodeObserver,
                CancellationToken cancel = default)
            {
                using (var op = SnTrace.Test.StartOperation(
                    $"NodeObserverAction simulation: {snEvent.GetType().Name} {nodeObserver.GetType().Name}"))
                {
                    await T.Task.Delay(10, cancel).ConfigureAwait(false);
                    op.Successful = true;
                }

                var result =  snEvent.CancellableEventArgs.Cancel;
                result |= await base.FireCancellableNodeObserverEventAsync(snEvent, nodeObserver, cancel);
                return result;
            }

            protected override async T.Task FireNodeObserverEventAsync(INodeObserverEvent snEvent, NodeObserver nodeObserver,
                CancellationToken cancel = default)
            {
                using (var op = SnTrace.Test.StartOperation(
                    $"NodeObserverAction simulation: {snEvent.GetType().Name} {nodeObserver.GetType().Name}"))
                {
                    await T.Task.Delay(20, cancel).ConfigureAwait(false);
                    op.Successful = true;
                }

                await base.FireNodeObserverEventAsync(snEvent, nodeObserver, cancel);
            }

            protected override async T.Task SaveEventAsync(ISnEvent snEvent)
            {
                using (var op = SnTrace.Test.StartOperation("Save event"))
                {
                    await T.Task.Delay(10);
                    op.Successful = true;
                }

                await base.SaveEventAsync(snEvent);
            }
        }

        #endregion

        public class SnEventTestsTestSnTracer : ISnTracer
        {
            public List<string> Log { get; } = new List<string>();
            public void Write(string line)
            {
                this.Log.Add(line);
            }
            public void Flush()
            {
                // do nothing
            }
            public void Clear()
            {
                this.Log.Clear();
            }
        }
        private void EnsureCleanTestSnTracer()
        {
            var existing = GetTestTracer();
            if (existing == null)
                SnTrace.SnTracers.Add(new SnEventTestsTestSnTracer());
            else
                existing.Clear();
        }

        private SnEventTestsTestSnTracer GetTestTracer()
        {
            return (SnEventTestsTestSnTracer)SnTrace.SnTracers.FirstOrDefault(
                x => x.GetType() == typeof(SnEventTestsTestSnTracer));
        }

        [TestMethod]
        public void Event_1()
        {
            Test(builder =>
            {
                builder.EnableNodeObservers(typeof(TestObserver1), typeof(TestObserver2), typeof(TestObserver3));
                Providers.Instance.EventDistributor = new TestEventDistributor
                {
                    AuditLogEventProcessor = new TestAuditLogEventProcessor(),
                    AsyncEventProcessors = new IEventProcessor[]
                    {
                        new TestPushNotificationEventProcessor(), new TestWebHookEventProcessor(),
                        new TestEmailSenderEventProcessor()
                    }
                };
                EnsureCleanTestSnTracer();
            }, () =>
            {
                var node = new SystemFolder(Repository.Root) {Name = Guid.NewGuid().ToString()};
                node.Save();

                // ACTION
                node.Index++;
                using (var op = SnTrace.Test.StartOperation("-------- TEST: NODE.SAVE"))
                {
                    node.Save();
                    op.Successful = true;
                }

                Thread.Sleep(2000);

                // ASSERT
                var tracer = GetTestTracer();
                var lines = tracer.Log.Where(x => x.Contains("\tTest\t"))
                    .Select(x =>
                    {
                        var fields = x.Split('\t');
                        return $"{fields[6],-6} {fields[8]}";
                    })
                    .SkipWhile(x => !x.StartsWith("Start  -------- TEST: NODE.SAVE"))
                    .ToList();

                if(!((EventDistributor)Providers.Instance.EventDistributor).IsFeatureEnabled(0))
                    Assert.Inconclusive();

                // All cancellable event, and state need to exist in the log for all NodeObserver types
                foreach (var @event in new[] { "NodeModifyingEvent"/*, "NodeModifiedEvent"*/ })
                {
                    foreach (var state in new[] { "Start", "End" })
                    {
                        foreach (var type in new[] {"SettingsCache", "TestObserver1", "TestObserver2",
                            "TestObserver3"})
                        {
                            var line = lines.FirstOrDefault(x =>
                                x.Contains(state) && x.Contains(@event) && x.Contains(type));
                            Assert.IsNotNull(line, $"Missing line {state}, {@event}, {type}");
                        }
                    }
                }

                // All not cancellable event, and state need to exist in the log for all types
                foreach (var @event in new[] { /*"NodeModifyingEvent", */"NodeModifiedEvent" })
                {
                    foreach (var state in new[] { "Start", "End" })
                    {
                        foreach (var type in new[] {"TestPushNotificationEventProcessor",
                            "TestWebHookEventProcessor", "TestEmailSenderEventProcessor", "TestAuditLogEventProcessor",
                            "SettingsCache", "TestObserver1", "TestObserver2", "TestObserver3"})
                        {
                            var line = lines.FirstOrDefault(x =>
                                x.Contains(state) && x.Contains(@event) && x.Contains(type));
                            Assert.IsNotNull(line, $"Missing line {state}, {@event}, {type}");
                        }
                    }
                }

                // Async processors finished after end of the test
                var p0 = lines.IndexOf("End    -------- TEST: NODE.SAVE");
                var p1 = lines.IndexOf("End    ProcessEvent TestEmailSenderEventProcessor NodeModifiedEvent");
                var p2 = lines.IndexOf("End    ProcessEvent TestWebHookEventProcessor NodeModifiedEvent");
                var p3 = lines.IndexOf("End    ProcessEvent TestPushNotificationEventProcessor NodeModifiedEvent");
                Assert.IsTrue(p0 > 0);
                Assert.IsTrue(p1 > p0);
                Assert.IsTrue(p2 > p0);
                Assert.IsTrue(p3 > p0);
            });
        }
    }
}

/*
Start  TESTMETHOD: Event_1 
Start  NodeObserverAction simulation: NodePermissionChangingEvent SettingsCache 
Start  NodeObserverAction simulation: NodePermissionChangingEvent TestObserver1 
Start  NodeObserverAction simulation: NodePermissionChangingEvent TestObserver2 
Start  NodeObserverAction simulation: NodePermissionChangingEvent TestObserver3 
End    NodeObserverAction simulation: NodePermissionChangingEvent TestObserver3 
End    NodeObserverAction simulation: NodePermissionChangingEvent TestObserver2 
End    NodeObserverAction simulation: NodePermissionChangingEvent SettingsCache 
End    NodeObserverAction simulation: NodePermissionChangingEvent TestObserver1 
Start  NodeObserverAction simulation: NodePermissionChangedEvent SettingsCache 
Start  NodeObserverAction simulation: NodePermissionChangedEvent TestObserver1 
Start  NodeObserverAction simulation: NodePermissionChangedEvent TestObserver2 
Start  NodeObserverAction simulation: NodePermissionChangedEvent TestObserver3 
Start  ProcessAuditEvent TestAuditLogEventProcessor NodePermissionChangedEvent 
Start  Save event 
End    Save event 
End    ProcessAuditEvent TestAuditLogEventProcessor NodePermissionChangedEvent 
Start  ProcessEvent TestPushNotificationEventProcessor NodePermissionChangedEvent 
Start  ProcessEvent TestWebHookEventProcessor NodePermissionChangedEvent 
Start  ProcessEvent TestEmailSenderEventProcessor NodePermissionChangedEvent 
End    NodeObserverAction simulation: NodePermissionChangedEvent TestObserver1 
End    NodeObserverAction simulation: NodePermissionChangedEvent TestObserver3 
End    NodeObserverAction simulation: NodePermissionChangedEvent SettingsCache 
End    NodeObserverAction simulation: NodePermissionChangedEvent TestObserver2 
End    ProcessEvent TestEmailSenderEventProcessor NodePermissionChangedEvent 
End    ProcessEvent TestWebHookEventProcessor NodePermissionChangedEvent 
End    ProcessEvent TestPushNotificationEventProcessor NodePermissionChangedEvent 
Start  NodeObserverAction simulation: NodeCreatingEvent SettingsCache 
Start  NodeObserverAction simulation: NodeCreatingEvent TestObserver1 
Start  NodeObserverAction simulation: NodeCreatingEvent TestObserver2 
Start  NodeObserverAction simulation: NodeCreatingEvent TestObserver3 
End    NodeObserverAction simulation: NodeCreatingEvent TestObserver1 
End    NodeObserverAction simulation: NodeCreatingEvent TestObserver3 
End    NodeObserverAction simulation: NodeCreatingEvent SettingsCache 
End    NodeObserverAction simulation: NodeCreatingEvent TestObserver2 
Start  NodeObserverAction simulation: NodeCreatedEvent SettingsCache 
Start  NodeObserverAction simulation: NodeCreatedEvent TestObserver1 
Start  NodeObserverAction simulation: NodeCreatedEvent TestObserver2 
Start  NodeObserverAction simulation: NodeCreatedEvent TestObserver3 
Start  ProcessAuditEvent TestAuditLogEventProcessor NodeCreatedEvent 
Start  Save event 
End    ProcessAuditEvent TestAuditLogEventProcessor NodeCreatedEvent 
End    Save event 
Start  ProcessEvent TestPushNotificationEventProcessor NodeCreatedEvent 
End    NodeObserverAction simulation: NodeCreatedEvent SettingsCache 
End    NodeObserverAction simulation: NodeCreatedEvent TestObserver2 
End    NodeObserverAction simulation: NodeCreatedEvent TestObserver3 
End    NodeObserverAction simulation: NodeCreatedEvent TestObserver1 
Start  ProcessEvent TestWebHookEventProcessor NodeCreatedEvent 
Start  ProcessEvent TestEmailSenderEventProcessor NodeCreatedEvent 
Start  -------- TEST: NODE.SAVE 
End    ProcessEvent TestPushNotificationEventProcessor NodeCreatedEvent 
Start  NodeObserverAction simulation: NodeModifyingEvent SettingsCache 
Start  NodeObserverAction simulation: NodeModifyingEvent TestObserver1 
Start  NodeObserverAction simulation: NodeModifyingEvent TestObserver2 
End    ProcessEvent TestWebHookEventProcessor NodeCreatedEvent 
End    NodeObserverAction simulation: NodeModifyingEvent SettingsCache 
End    ProcessEvent TestEmailSenderEventProcessor NodeCreatedEvent 
Start  NodeObserverAction simulation: NodeModifyingEvent TestObserver3 
End    NodeObserverAction simulation: NodeModifyingEvent TestObserver2 
End    NodeObserverAction simulation: NodeModifyingEvent TestObserver1 
End    NodeObserverAction simulation: NodeModifyingEvent TestObserver3 
Start  NodeObserverAction simulation: NodeModifiedEvent SettingsCache 
Start  NodeObserverAction simulation: NodeModifiedEvent TestObserver1 
Start  NodeObserverAction simulation: NodeModifiedEvent TestObserver2 
Start  NodeObserverAction simulation: NodeModifiedEvent TestObserver3 
Start  ProcessAuditEvent TestAuditLogEventProcessor NodeModifiedEvent 
Start  Save event 
End    Save event 
Start  ProcessEvent TestPushNotificationEventProcessor NodeModifiedEvent 
Start  ProcessEvent TestWebHookEventProcessor NodeModifiedEvent 
Start  ProcessEvent TestEmailSenderEventProcessor NodeModifiedEvent 
End    ProcessAuditEvent TestAuditLogEventProcessor NodeModifiedEvent 
End    NodeObserverAction simulation: NodeModifiedEvent TestObserver2 
End    NodeObserverAction simulation: NodeModifiedEvent TestObserver3 
End    NodeObserverAction simulation: NodeModifiedEvent TestObserver1 
End    NodeObserverAction simulation: NodeModifiedEvent SettingsCache 
End    -------- TEST: NODE.SAVE 
End    ProcessEvent TestEmailSenderEventProcessor NodeModifiedEvent 
End    ProcessEvent TestWebHookEventProcessor NodeModifiedEvent 
End    ProcessEvent TestPushNotificationEventProcessor NodeModifiedEvent 
       Event_1: Inconclusive 
End    TESTMETHOD: Event_1 
*/

/*
Start  -------- TEST: NODE.SAVE 
End    ProcessEvent TestPushNotificationEventProcessor NodeCreatedEvent 
Start  NodeObserverAction simulation: NodeModifyingEvent SettingsCache 
Start  NodeObserverAction simulation: NodeModifyingEvent TestObserver1 
Start  NodeObserverAction simulation: NodeModifyingEvent TestObserver2 
End    ProcessEvent TestWebHookEventProcessor NodeCreatedEvent 
End    NodeObserverAction simulation: NodeModifyingEvent SettingsCache 
End    ProcessEvent TestEmailSenderEventProcessor NodeCreatedEvent 
Start  NodeObserverAction simulation: NodeModifyingEvent TestObserver3 
End    NodeObserverAction simulation: NodeModifyingEvent TestObserver2 
End    NodeObserverAction simulation: NodeModifyingEvent TestObserver1 
End    NodeObserverAction simulation: NodeModifyingEvent TestObserver3 
Start  NodeObserverAction simulation: NodeModifiedEvent SettingsCache 
Start  NodeObserverAction simulation: NodeModifiedEvent TestObserver1 
Start  NodeObserverAction simulation: NodeModifiedEvent TestObserver2 
Start  NodeObserverAction simulation: NodeModifiedEvent TestObserver3 
Start  ProcessAuditEvent TestAuditLogEventProcessor NodeModifiedEvent 
Start  Save event 
End    Save event 
Start  ProcessEvent TestPushNotificationEventProcessor NodeModifiedEvent 
Start  ProcessEvent TestWebHookEventProcessor NodeModifiedEvent 
Start  ProcessEvent TestEmailSenderEventProcessor NodeModifiedEvent 
End    ProcessAuditEvent TestAuditLogEventProcessor NodeModifiedEvent 
End    NodeObserverAction simulation: NodeModifiedEvent TestObserver2 
End    NodeObserverAction simulation: NodeModifiedEvent TestObserver3 
End    NodeObserverAction simulation: NodeModifiedEvent TestObserver1 
End    NodeObserverAction simulation: NodeModifiedEvent SettingsCache 
End    -------- TEST: NODE.SAVE 
End    ProcessEvent TestEmailSenderEventProcessor NodeModifiedEvent 
End    ProcessEvent TestWebHookEventProcessor NodeModifiedEvent 
End    ProcessEvent TestPushNotificationEventProcessor NodeModifiedEvent 
       Event_1: Inconclusive 
End    TESTMETHOD: Event_1
*/

/*
EventDistributor INACTIVATED (1). 
EventDistributor INACTIVATED (2). 
EventDistributor INACTIVATED (3). 
*/