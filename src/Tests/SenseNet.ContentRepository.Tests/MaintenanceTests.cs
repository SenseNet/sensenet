using System;
using System.Threading;
using Tasks = System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.BackgroundOperations;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    internal class TestMaintenanceTask : IMaintenanceTask
    {
        public int Calls { get; set; }
        public int WaitingSeconds { get; set; }

        public Tasks.Task ExecuteAsync(CancellationToken cancellationToken)
        {
            ++Calls;

            return Tasks.Task.Delay(100, cancellationToken);
        }
    }

    [TestClass]
    public class MaintenanceTests : TestBase
    {
        [TestMethod]
        public void Maintenance_Frequent_RunningTime()
        {
            // A 10-second-delay task is called two or three times
            // in a half minute length active period.
            var calls = MaintenanceTest(5, 11);
            Assert.IsTrue(calls >= 2 && calls <= 3);
        }
        [TestMethod]
        public void Maintenance_Rare_RunningTime()
        {
            // A half-minute-delay task is called one or two times
            // in more than 30 second length active period.
            var calls = MaintenanceTest(11, 12);
            Assert.IsTrue(calls >= 1 && calls <= 2);
        }
        private int MaintenanceTest(int taskDelaySeconds, int sleepSeconds)
        {
            var testTask = new TestMaintenanceTask
            {
                WaitingSeconds = taskDelaySeconds
            };

            // define tasks and start the service
            var maintenanceService = new SnMaintenance(new[] {testTask}, null)
            {
                // very short cycle
                TimerInterval = 3
            };
            maintenanceService.StartAsync(CancellationToken.None).Wait();

            // wait for a predefined period
            Thread.Sleep(sleepSeconds * 1000);

            maintenanceService.StopAsync(CancellationToken.None).Wait(TimeSpan.FromSeconds(30));

            return testTask.Calls;
        }
        
        [TestMethod]
        public void Maintenance_Cancellation()
        {
            var testTask = new TestMaintenanceTask
            {
                WaitingSeconds = 6
            };

            // define tasks and start the service
            var maintenanceService = new SnMaintenance(new[] { testTask }, null)
            {
                // very short cycle
                TimerInterval = 3
            };
            maintenanceService.StartAsync(CancellationToken.None).Wait();

            // wait for a few cycles (a 6-second task should be executed 2 times)
            Thread.Sleep(14 * 1000);

            maintenanceService.StopAsync(CancellationToken.None).Wait(TimeSpan.FromSeconds(30));

            var callCount1 = testTask.Calls;

            // wait for a while and get the count again
            Thread.Sleep(7000);
            var callCount2 = testTask.Calls;

            // the task should have been executed at least a few times
            Assert.IsTrue(callCount1 >= 2 && callCount1 < 4, $"call count: {callCount1}");
            // there may be a small difference, but not bigger than 1
            Assert.IsTrue(callCount2 - callCount1 < 2);
        }
    }
}
