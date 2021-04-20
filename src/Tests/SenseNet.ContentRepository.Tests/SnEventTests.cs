using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Diagnostics;
using SenseNet.Events;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Tests.Core;
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
            public async T.Task ProcessEventAsync(ISnEvent snEvent, CancellationToken cancel)
            {
                using (var op = SnTrace.Test.StartOperation($"ProcessAuditEvent {GetType().Name} {snEvent.GetType().Name}"))
                {
                    await T.Task.Delay(5, cancel).ConfigureAwait(false);
                    op.Successful = true;
                }
            }
        }

        private abstract class TestEventProcessor : IEventProcessor
        {
            public async T.Task ProcessEventAsync(ISnEvent snEvent, CancellationToken cancel)
            {
                using (var op = SnTrace.Test.StartOperation($"ProcessEvent {GetType().Name} {snEvent.GetType().Name}"))
                {
                    await T.Task.Delay(50, cancel).ConfigureAwait(false);
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

        #endregion
        # region private void EventProcessorTest(Action callback)
        private void EventProcessorTest(string eventHeadInLog, string eventName, string cancellableEventName, Action align, Action action)
        {
            Test(builder =>
            {
                builder.EnableNodeObservers(typeof(TestObserver1), typeof(TestObserver2), typeof(TestObserver3));
                builder.UseEventDistributor(new TestEventDistributor());
                builder.UseAuditLogEventProcessor(new TestAuditLogEventProcessor());
                builder.UseAsyncEventProcessors(new IEventProcessor[]
                {
                    new TestPushNotificationEventProcessor(),
                    new TestWebHookEventProcessor(),
                    new TestEmailSenderEventProcessor()
                });
            }, () =>
            {
                EnsureCleanTestSnTracer();

                align();

                // ACTION
                using (var op = SnTrace.Test.StartOperation($"-------- TEST: NODE.{eventHeadInLog}"))
                {
                    action();
                    op.Successful = true;
                }

                Thread.Sleep(300);

                // ASSERT
                var tracer = GetTestTracer();
                var lines = tracer.Log.Where(x => x != null && x.Contains("\tTest\t"))
                    .Select(x =>
                    {
                        var fields = x.Split('\t');
                        return $"{fields[6],-6} {fields[8]}";
                    })
                    //.SkipWhile(x => !x.StartsWith("Start  -------- TEST: NODE.DELETE"))
                    .ToList();

                if (!((EventDistributor)Providers.Instance.EventDistributor).IsFeatureEnabled(0))
                    Assert.Inconclusive();

                // All cancellable event, and state need to exist in the log for all NodeObserver types
                foreach (var @event in new[] { cancellableEventName })
                {
                    foreach (var state in new[] { "Start", "End" })
                    {
                        foreach (var type in new[] {"SettingsCache", "TestObserver1", "TestObserver2", "TestObserver3"})
                        {
                            var line = lines.FirstOrDefault(x =>
                                x.Contains(state) && x.Contains(@event) && x.Contains(type));
                            Assert.IsNotNull(line, $"Missing line {state}, {@event}, {type}");
                        }
                    }
                }

                // All not cancellable event, and state need to exist in the log for all types
                foreach (var @event in new[] { eventName })
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
                var p0 = lines.IndexOf($"End    -------- TEST: NODE.{eventHeadInLog}");
                var p1 = lines.IndexOf($"End    ProcessEvent TestEmailSenderEventProcessor {eventName}");
                var p2 = lines.IndexOf($"End    ProcessEvent TestWebHookEventProcessor {eventName}");
                var p3 = lines.IndexOf($"End    ProcessEvent TestPushNotificationEventProcessor {eventName}");
                Assert.IsTrue(p0 > 0);
                Assert.IsTrue(p1 > p0);
                Assert.IsTrue(p2 > p0);
                Assert.IsTrue(p3 > p0);
            });
        }
        #endregion

        [TestMethod]
        public void Event_EventProcessor_1()
        {
            Node node = null;
            EventProcessorTest("SAVE", "NodeModifiedEvent", "NodeModifyingEvent",
                () =>
                {
                    node = new SystemFolder(Repository.Root) {Name = Guid.NewGuid().ToString()};
                    node.Save();
                },
                () =>
                {
                    node.Index++;
                    node.Save();
                });
        }
        [TestMethod]
        public void Event_EventProcessor_Delete()
        {
            Node node = null;
            EventProcessorTest("DELETE", "NodeDeletedEvent", "NodeDeletingEvent",
                () =>
                {
                    node = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
                    node.Save();
                },
                () =>
                {
                    node.Delete();
                });
        }
        [TestMethod]
        public void Event_EventProcessor_ForceDelete()
        {
            Node node = null;
            EventProcessorTest("FORCED-DELETE", "NodeForcedDeletedEvent", "NodeForcedDeletingEvent",
                () =>
                {
                    node = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
                    node.Save();
                },
                () =>
                {
                    node.ForceDelete();
                });
        }

    }
}
