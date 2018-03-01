using System;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Search.Indexing
{
    internal static class IndexHealthMonitor
    {
        private static System.Timers.Timer _timer;

        internal static void Start(System.IO.TextWriter consoleOut)
        {
            var pollInterval = Configuration.Indexing.IndexHealthMonitorRunningPeriod * 1000.0;

            _timer = new System.Timers.Timer(pollInterval);
            _timer.Elapsed += Timer_Elapsed;
            _timer.Disposed += Timer_Disposed;
            _timer.Enabled = true;

            consoleOut?.WriteLine("IndexHealthMonitor started. Frequency: {0} s", Configuration.Indexing.IndexHealthMonitorRunningPeriod);
        }
        internal static void ShutDown()
        {
            if (_timer == null)
                return;

            _timer.Enabled = false;
            _timer.Dispose();
        }

        private static void Timer_Disposed(object sender, EventArgs e)
        {
            _timer.Elapsed -= Timer_Elapsed;
            _timer.Disposed -= Timer_Disposed;
        }
        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Timer_Elapsed();
        }
        // for testing purposes we need a parameterless method because ElapsedEventArgs has only internal constructor
        private static void Timer_Elapsed()
        {
            if (SearchManager.ContentQueryIsAllowed)
            {
                var timerEnabled = _timer.Enabled;
                _timer.Enabled = false;
                try
                {
                    DistributedIndexingActivityQueue.HealthCheck();
                }
                catch (Exception ex) // logged
                {
                    SnLog.WriteException(ex);
                }
                finally
                {
                    _timer.Enabled = timerEnabled;
                }
            }
        }
    }
}
