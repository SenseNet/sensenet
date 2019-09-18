using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Volatile;
using SenseNet.Tests;

namespace SenseNet.ContentRepository.Tests
{
    internal class TestMaintenanceTask : IMaintenanceTask
    {
        public static int Calls { get; set; }
        public static bool Enabled { get; set; }
        public static double WaitingMinutesForTests { get; set; }

        public double WaitingMinutes => WaitingMinutesForTests;

        public void Execute()
        {
            if (Enabled)
                ++Calls;
        }
    }

    [TestClass]
    public class MaintenanceTests : TestBase
    {
        private static readonly PrivateType SnMaintenanceAcc = new PrivateType(typeof(SnMaintenance));
        private static object _adsyncAvailableBackup;

        [ClassInitialize]
        public static void StartTests(TestContext context)
        {
            // prevent accessing the database
            _adsyncAvailableBackup = SnMaintenanceAcc.GetStaticField("_adsyncAvailable");
            SnMaintenanceAcc.SetStaticField("_adsyncAvailable", false);
        }
        [ClassCleanup]
        public static void FinishTests()
        {
            SnMaintenanceAcc.SetStaticField("_adsyncAvailable", _adsyncAvailableBackup);
        }


        [TestMethod]
        public void Maintenance_Frequent_RunningTime31sec()
        {
            // A 10-second-delay task is called two or three times
            // in a half minute length active period (the delay 0.0 means 10 second).
            var calls = MaintenanceTest(0.0, 31 * 1000);
            Assert.IsTrue(calls >= 2);
            Assert.IsTrue(calls <= 3);
        }
        [TestMethod]
        public void Maintenance_Rare_RunningTime32sec()
        {
            // A half-minute-delay task is called one or two times
            // in more than 30 second length active period.
            var calls = MaintenanceTest(3.01 / 6, 32 * 1000);
            Assert.IsTrue(calls >= 1);
            Assert.IsTrue(calls <= 2);
        }
        private int MaintenanceTest(double taskDelayMinutes, int sleep)
        {
            // activate the service if needed
            var running = SnMaintenance.Running();
            var maintenanceService = new SnMaintenance();
            if (!running)
                maintenanceService.Start();

            // activate the test task
            TestMaintenanceTask.Enabled = false;
            TestMaintenanceTask.Calls = 0;
            TestMaintenanceTask.WaitingMinutesForTests = taskDelayMinutes;
            TestMaintenanceTask.Enabled = true;

            Thread.Sleep(sleep);

            TestMaintenanceTask.Enabled = false;
            if (!running)
                maintenanceService.Shutdown();

            return TestMaintenanceTask.Calls;
        }


        private class HackedInMemoryBlobStorageMetaDataProvider : InMemoryBlobStorageMetaDataProvider
        {
            public int CountOfCleanupCalled { get; private set; }

            public override System.Threading.Tasks.Task CleanupFilesSetDeleteFlagAsync(CancellationToken cancellationToken)
            {
                return System.Threading.Tasks.Task.CompletedTask;
            }
            public override Task<bool> CleanupFilesAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Thread.Sleep(100);
                CountOfCleanupCalled++;
                return System.Threading.Tasks.Task.FromResult(true);
            }
        }

        [TestMethod]
        public void Maintenance_Cancellation()
        {
            Test(() =>
            {
                var hackedDataProvider = new HackedInMemoryBlobStorageMetaDataProvider();
                BlobStorageComponents.DataProvider = hackedDataProvider;

                var running = SnMaintenance.Running();
                var maintenanceService = new SnMaintenance();
                if (!running)
                    maintenanceService.Start();

                try
                {
                    // Start the real maintenance immediatelly on a hacked system
                    System.Threading.Tasks.Task.Run(() => SnMaintenanceAcc.InvokeStatic("CleanupFiles"));
                    // Let it work hard
                    Thread.Sleep(1000);
                    // Simulate system's cooldown
                    maintenanceService.Shutdown();

                    // Get count of deletions
                    var count1 = hackedDataProvider.CountOfCleanupCalled;
                    // Wait for a while and get the count again
                    Thread.Sleep(1000);
                    var count2 = hackedDataProvider.CountOfCleanupCalled;
                    // Repeat again
                    Thread.Sleep(1000);
                    var count3 = hackedDataProvider.CountOfCleanupCalled;

                    // Maybe a deletion cycle ran at the moment of shutdown
                    Assert.IsTrue(count2 - count1 < 2);
                    // The system certainly stopped at the second time.
                    Assert.AreEqual(count2, count3);
                }
                finally
                {
                    TestMaintenanceTask.Enabled = false;
                    if (!running)
                        maintenanceService.Shutdown();
                }
            });
        }
    }
}
