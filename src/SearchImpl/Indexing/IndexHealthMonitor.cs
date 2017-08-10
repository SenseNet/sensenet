using System;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Search.Lucene29;

namespace SenseNet.Search.Indexing
{
    internal static class IndexHealthMonitor
    {
        private static System.Timers.Timer _timer;

        internal static void Start(System.IO.TextWriter consoleOut)
        {
            var pollInterval = SenseNet.Configuration.Indexing.IndexHealthMonitorRunningPeriod * 1000.0;

            _timer = new System.Timers.Timer(pollInterval);
            _timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed);
            _timer.Disposed += new EventHandler(Timer_Disposed);
            _timer.Enabled = true;

            if (consoleOut == null)
                return;
            consoleOut.WriteLine("IndexHealthMonitor started. Frequency: {0} s", SenseNet.Configuration.Indexing.IndexHealthMonitorRunningPeriod);
        }

        private static void Timer_Disposed(object sender, EventArgs e)
        {
            _timer.Elapsed -= new System.Timers.ElapsedEventHandler(Timer_Elapsed);
            _timer.Disposed -= new EventHandler(Timer_Disposed);
        }
        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Timer_Elapsed();
        }
        // for testing purposes we need a parameterless method because ElapsedEventArgs has only internal constructor
        private static void Timer_Elapsed()
        {
            if (StorageContext.Search.ContentQueryIsAllowed)
            {
                var timerEnabled = _timer.Enabled;
                _timer.Enabled = false;
                try
                {
                    IndexingActivityQueue.HealthCheck();
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
