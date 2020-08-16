using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.BackgroundOperations
{
    /// <summary>
    /// Internal service for periodically executing maintenance operations (e.g. cleaning up orphaned binary rows in the database).
    /// All operations are executed when the timer ticks, but they can opt out (skip an iteration) by using a dedicated flag
    /// to avoid parallel execution.
    /// </summary>
    internal class SnMaintenance : ISnService
    {
        private static readonly int TIMER_INTERVAL = 10; // in seconds
        private static Timer _maintenanceTimer;
        private static int _currentCycle;
        private static IMaintenanceTask[] _maintenanceTasks = new IMaintenanceTask[0];
        private static CancellationTokenSource _cancellation;

        // ========================================================================================= ISnService implementation

        public bool Start()
        {
            _cancellation = new CancellationTokenSource();

            _maintenanceTimer = new Timer(MaintenanceTimerElapsed, null, TIMER_INTERVAL * 1000, TIMER_INTERVAL * 1000);
            _maintenanceTasks = DiscoverMaintenanceTasks();
            return true;
        }
        private IMaintenanceTask[] DiscoverMaintenanceTasks()
        {
            return TypeResolver.GetTypesByInterface(typeof(IMaintenanceTask)).Select(t =>
                {
                    SnTrace.System.Write("MaintenanceTask found: {0}", t.FullName);
                    return (IMaintenanceTask)Activator.CreateInstance(t);
                }).ToArray();
        }

        public void Shutdown()
        {
            if (_maintenanceTimer == null)
                return;

            _maintenanceTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _maintenanceTimer.Dispose();
            _maintenanceTimer = null;

            try
            {
                _cancellation.Cancel();
            }
            catch
            {
                SnLog.WriteInformation("One or more maintenance tasks are canceled.");
            }
        }

        internal static bool Running()
        {
            return _maintenanceTimer != null;
        }

        // ========================================================================================= Timer event handler

        private static void MaintenanceTimerElapsed(object state)
        {
            // Increment the global cycle counter. Different maintenance tasks may
            // rely on this to decide whether they should be executed in the
            // current cycle.
            Interlocked.Increment(ref _currentCycle);
            
            // preventing the counter from overflow
            if (_currentCycle > 100000)
                _currentCycle = 0;
            
            foreach(var maintenanceTask in _maintenanceTasks)
                if(IsTaskExecutableByTime(maintenanceTask.WaitingSeconds))
                    Task.Run(() => maintenanceTask.ExecuteAsync(CancellationToken.None));
        }
        
        // ========================================================================================= Helper methods

        private static bool IsTaskExecutableByTime(int waitingSeconds)
        {
            return IsTaskExecutable(Convert.ToInt32(Math.Max(0, waitingSeconds) / TIMER_INTERVAL));
        }
        private static bool IsTaskExecutable(int cycleLength)
        {
            // We are in the correct cycle if the current timer cycle is divisible
            // by the cycle length defined by the particular task.
            if (cycleLength < 1)
                return true;
            return _currentCycle%cycleLength == 0;
        }
    }
}
