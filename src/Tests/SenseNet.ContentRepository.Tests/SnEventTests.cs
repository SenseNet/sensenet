using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Events;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Security;
using SenseNet.Testing;
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
                    $"NodeObserverAction simulation: {nodeObserver.GetType().Name} {snEvent.GetType().Name}"))
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
                    $"NodeObserverAction simulation: {nodeObserver.GetType().Name} {snEvent.GetType().Name}"))
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

        private void EventProcessorAsynchronyTest(string eventName, string cancellableEventName, Action align, Action action)
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
                align();

                Thread.Sleep(300);

                SnTrace.Flush();
                EnsureCleanTestSnTracer();
                // ACTION
                using (var op = SnTrace.Test.StartOperation($"-------- TEST: NODE-ACTION"))
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
                    //.SkipWhile(x => !x.StartsWith("Start  -------- TEST: NODE-ACTION"))
                    .ToList();

                if (!((EventDistributor) Providers.Instance.EventDistributor).IsFeatureEnabled(0))
                    Assert.Inconclusive();

                // All cancellable event, and state need to exist in the log for all NodeObserver types
                foreach (var @event in new[] {cancellableEventName})
                {
                    foreach (var state in new[] {"Start", "End"})
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
                foreach (var @event in new[] {eventName})
                {
                    foreach (var state in new[] {"Start", "End"})
                    {
                        foreach (var type in new[]
                        {
                            "TestPushNotificationEventProcessor",
                            "TestWebHookEventProcessor", "TestEmailSenderEventProcessor", "TestAuditLogEventProcessor",
                            "SettingsCache", "TestObserver1", "TestObserver2", "TestObserver3"
                        })
                        {
                            var line = lines.FirstOrDefault(x =>
                                x.Contains(state) && x.Contains(@event) && x.Contains(type));
                            Assert.IsNotNull(line, $"Missing line {state}, {type}, {@event}");
                        }
                    }
                }

                // Async processors finished after end of the test
                var p0 = lines.IndexOf($"End    -------- TEST: NODE-ACTION");
                var p1 = lines.IndexOf($"End    ProcessEvent TestEmailSenderEventProcessor {eventName}");
                var p2 = lines.IndexOf($"End    ProcessEvent TestWebHookEventProcessor {eventName}");
                var p3 = lines.IndexOf($"End    ProcessEvent TestPushNotificationEventProcessor {eventName}");
                Assert.IsTrue(p0 > 0);
                Assert.IsTrue(p1 > p0);
                Assert.IsTrue(p2 > p0);
                Assert.IsTrue(p3 > p0);
            });
        }

        private void EventProcessorTest(string[] expectedLogLines, Action align, Action action)
        {
            Test(builder =>
            {
                builder.UseEventDistributor(new TestEventDistributor());
                builder.UseAsyncEventProcessors(new IEventProcessor[] {new TestWebHookEventProcessor()});
            }, () =>
            {
                if (!((EventDistributor)Providers.Instance.EventDistributor).IsFeatureEnabled(0))
                    Assert.Inconclusive();

                align();

                Thread.Sleep(300);
                SnTrace.Flush();
                EnsureCleanTestSnTracer();
                // ACTION
                using (var op = SnTrace.Test.StartOperation($"-------- TEST: NODE-ACTION"))
                {
                    action();
                    op.Successful = true;
                }

                Thread.Sleep(300);

                // ASSERT
                var tracer = GetTestTracer();
                var lines = tracer.Log
                    //.SkipWhile(x => !x.Contains($"-------- Test: NODE-ACTION"))
                    .SkipWhile(x => !x.Contains("-------- TEST: NODE-ACTION"))
                    .Where(x => x != null && x.Contains("\tTest\t") && x.Contains("\tStart\t") &&
                                (x.Contains("NodeObserverAction") || x.Contains("ProcessEvent")))
                    .Select(x =>
                    {
                        var fields = x.Split('\t');
                        return fields[8]
                            .Replace("NodeObserverAction simulation: SettingsCache", "NodeObserver")
                            .Replace("ProcessEvent TestWebHookEventProcessor", "EventProcessor");
                    })
                    .ToList();

                var expected = string.Join(",", expectedLogLines);
                var actual = string.Join(",", lines);
                
                Assert.AreEqual(expected, actual);
            });
        }

        #endregion

        [TestMethod]
        public void Event_EventProcessor_Asynchrony()
        {
            Node node = null;
            EventProcessorAsynchronyTest("NodeModifiedEvent", "NodeModifyingEvent",
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
        public void Event_EventProcessor_Create()
        {
            EventProcessorTest(new[]
                {
                    "NodeObserver NodeCreatingEvent", "NodeObserver NodeCreatedEvent", "EventProcessor NodeCreatedEvent"
                },
                () =>
                {
                },
                () =>
                {
                    var node = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
                    node.Save();
                });
        }
        [TestMethod]
        public void Event_EventProcessor_Modify()
        {
            Node node = null;
            EventProcessorTest(new[]
                {
                    "NodeObserver NodeModifyingEvent", "NodeObserver NodeModifiedEvent", "EventProcessor NodeModifiedEvent"
                },
                () =>
                {
                    node = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
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
            EventProcessorTest(new[]
                {
                    "NodeObserver NodeCreatingEvent", "NodeObserver NodeCreatedEvent", "EventProcessor NodeCreatedEvent",
                    "NodeObserver NodePermissionChangingEvent", "NodeObserver NodePermissionChangedEvent", "EventProcessor NodePermissionChangedEvent",
                    "NodeObserver NodePermissionChangingEvent", "NodeObserver NodePermissionChangedEvent", "EventProcessor NodePermissionChangedEvent",
                    "NodeObserver NodeDeletingEvent", "NodeObserver NodeDeletedEvent", "EventProcessor NodeDeletedEvent"
                },
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
        public void Event_EventProcessor_Restore()
        {
            TrashBag trashBag = null;
            EventProcessorTest(new[]
                {
                    "NodeObserver NodeRestoringEvent", "NodeObserver NodeRestoredEvent", "EventProcessor NodeRestoredEvent",
                    "NodeObserver NodeForcedDeletingEvent", "NodeObserver NodeForcedDeletedEvent", "EventProcessor NodeForcedDeletedEvent"
                },
                () =>
                {
                    var node = new SystemFolder(Repository.Root) { Name = Guid.NewGuid().ToString() };
                    node.Save();
                    node.Delete();

                    trashBag = (TrashBag)Node.Load<TrashBin>("/Root/Trash").Children.First();
                },
                () =>
                {
                    TrashBin.Restore(trashBag);
                });
        }
        [TestMethod]
        public void Event_EventProcessor_ForceDelete()
        {
            Node node = null;
            EventProcessorTest(new[] {
                    "NodeObserver NodeForcedDeletingEvent", "NodeObserver NodeForcedDeletedEvent", "EventProcessor NodeForcedDeletedEvent"
                },
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
        [TestMethod]
        public void Event_EventProcessor_Move()
        {
            Node source = null;
            Node target = null;
            EventProcessorTest(new[]
                {
                    "NodeObserver NodeMovingEvent", "NodeObserver NodeMovedEvent", "EventProcessor NodeMovedEvent",
                },
                () =>
                {
                    source = new SystemFolder(Repository.Root) { Name = "Source" }; source.Save();
                    target = new SystemFolder(Repository.Root) { Name = "Target" }; target.Save();
                },
                () =>
                {
                    source.MoveTo(target);
                });
        }
        [TestMethod]
        public void Event_EventProcessor_Copy()
        {
            Node source = null;
            Node target = null;
            EventProcessorTest(new[]
                {
                    "NodeObserver NodeCopyingEvent",
                    "NodeObserver NodeCreatedEvent", "EventProcessor NodeCreatedEvent",
                    "NodeObserver NodeCopiedEvent", "EventProcessor NodeCopiedEvent"
                },
                () =>
                {
                    source = new SystemFolder(Repository.Root) { Name = "Source" }; source.Save();
                    target = new SystemFolder(Repository.Root) { Name = "Target" }; target.Save();
                },
                () =>
                {
                    source.CopyTo(target);
                });
        }
        [TestMethod]
        public void Event_EventProcessor_ChangePermission()
        {
            Node node = null;
            EventProcessorTest(new[]
                {
                    "NodeObserver NodePermissionChangingEvent", "NodeObserver NodePermissionChangedEvent", "EventProcessor NodePermissionChangedEvent",
                },
                () =>
                {
                    node = new SystemFolder(Repository.Root) { Name = "Source" }; node.Save();
                },
                () =>
                {
                    SecurityHandler.CreateAclEditor()
                        .BreakInheritance(node.Id, new [] {EntryType.Normal})
                        .Apply();
                });
        }

    }
}
