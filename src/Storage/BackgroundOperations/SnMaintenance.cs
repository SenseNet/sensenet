using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace SenseNet.BackgroundOperations
{
    /// <summary>
    /// Internal service for periodically executing maintenance operations (e.g. cleaning up orphaned binary rows in the database).
    /// All operations are executed when the timer ticks, but they can opt out (skip an iteration) by using a dedicated flag
    /// to avoid parallel execution.
    /// </summary>
    public class SnMaintenance : SnBackgroundService
    {
        public int TimerInterval { get; set; } = 10; // in seconds

        private int _currentCycle;
        internal const string TracePrefix = "#SnMaintenance> ";
        private readonly IMaintenanceTask[] _maintenanceTasks;
        private readonly ILogger<SnMaintenance> _logger;

        public SnMaintenance(IEnumerable<IMaintenanceTask> tasks, ILogger<SnMaintenance> logger)
        {
            _maintenanceTasks = tasks.ToArray();
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger?.LogInformation("SnMaintenance Service is starting. Tasks: " +
                                    string.Join(", ", _maintenanceTasks.Select(mt => mt.GetType().FullName)));

            stoppingToken.Register(() => _logger?.LogDebug(" SnMaintenance background task is stopping."));

            // Wait one cycle at the beginning. This will allow the Start service method to finish quickly.
            await Task.Delay(TimerInterval * 1000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                // start tasks, but do not wait for them to finish
                var _ = _maintenanceTasks
                    .Where(mt => IsTaskExecutable(mt.WaitingSeconds))
                    .Select(mt => mt.ExecuteAsync(stoppingToken)).ToArray();

                // Increment the global cycle counter.
                Interlocked.Increment(ref _currentCycle);

                // Protecting the counter from overflow.
                if (_currentCycle > 100000)
                    _currentCycle = 0;

                // wait one cycle
                await Task.Delay(TimerInterval * 1000, stoppingToken);
            }

            _logger?.LogDebug("SnMaintenance background task is stopping.");
        }
        
        // ========================================================================================= Helper methods

        private bool IsTaskExecutable(int waitingSeconds)
        {
            // the defined waiting time is too short
            if (waitingSeconds < TimerInterval)
                return true;

            // count of cycles to wait for this task to execute
            var cycleLength = Convert.ToInt32(Math.Max(0, waitingSeconds) / TimerInterval);

            // We are in the correct cycle if the current timer cycle is divisible
            // by the cycle length defined by the particular task.
            return _currentCycle % cycleLength == 0;
        }
    }
}
