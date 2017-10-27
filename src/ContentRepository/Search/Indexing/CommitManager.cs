using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using SenseNet.Diagnostics;

namespace SenseNet.Search.Indexing
{
    public interface ICommitManager
    {
        void Start();
        void ShutDown();
        void ActivityFinished();
    }

    internal class NoDelayCommitManager : ICommitManager
    {
        public void Start()
        {
            // do nothing
        }
        public void ShutDown()
        {
            // do nothing
        }
        public void ActivityFinished()
        {
            IndexManager.Commit();
        }
    }

    [Obsolete("", true)]
    internal class NRTCommitManager : ICommitManager
    {
        private volatile int _uncommittedActivityCount;          // committer thread sets 0 other threads increment
        private volatile int _delayCycle;          // committer thread uses
        private bool _stopCommitWorker;

        public void Start()
        {
            var commitStart = new ThreadStart(CommitWorker);
            var t = new Thread(commitStart);
            t.Start();
            SnTrace.Index.Write("LM: 'CommitWorker' thread started. ManagedThreadId: {0}", t.ManagedThreadId);
        }

        public void ShutDown()
        {
            _stopCommitWorker = true;
        }

        public void ActivityFinished()
        {
            // compiler warning here is not a problem, Interlocked 
            // class can work with a volatile variable
#pragma warning disable 420
            Interlocked.Increment(ref _uncommittedActivityCount);
#pragma warning restore 420
        }

        internal void CommitOrDelay()
        {
            var act = _uncommittedActivityCount;
            if (act == 0 && _delayCycle == 0)
                return;

            if (act < 2)
            {
                Commit();
            }
            else
            {
                _delayCycle++;
                if (_delayCycle > SenseNet.Configuration.Indexing.DelayedCommitCycleMaxCount)
                {
                    Commit();
                }
            }

#pragma warning disable 420
            Interlocked.Exchange(ref _uncommittedActivityCount, 0);
#pragma warning restore 420
        }

        private void Commit()
        {
            IndexManager.Commit();

#pragma warning disable 420
            Interlocked.Exchange(ref _uncommittedActivityCount, 0);
#pragma warning restore 420
            _delayCycle = 0;
        }

        private void CommitWorker()
        {
            int wait = (int)(SenseNet.Configuration.Indexing.CommitDelayInSeconds * 1000.0);
            for (;;)
            {
                // check if commit worker instructed to stop
                if (_stopCommitWorker)
                {
                    _stopCommitWorker = false;
                    return;
                }

                try
                {
                    CommitOrDelay();
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex);
                }

                Thread.Sleep(wait);
            }
        }

    }

    internal class NearRealTimeCommitManager : ICommitManager
    {
        private static readonly TimeSpan MaxWaitTime = TimeSpan.FromSeconds(10);
        private DateTime _lastCommitTime;
        private int _uncommittedActivityCount;

        private static System.Timers.Timer _timer;
        private static readonly int HearthBeatMilliseconds = 1000;

        public void Start()
        {
            _lastCommitTime = DateTime.UtcNow;
            _uncommittedActivityCount = 0;

            _timer = new System.Timers.Timer(HearthBeatMilliseconds);
            _timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed);
            _timer.Disposed += new EventHandler(Timer_Disposed);
            _timer.Enabled = true;

        }
        private void Timer_Disposed(object sender, EventArgs e)
        {
            _timer.Elapsed -= new System.Timers.ElapsedEventHandler(Timer_Elapsed);
            _timer.Disposed -= new EventHandler(Timer_Disposed);
        }
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_uncommittedActivityCount > 0 && DateTime.UtcNow - _lastCommitTime > MaxWaitTime)
                Commit();
        }

        public void ShutDown()
        {
            _timer.Enabled = false;
            _timer.Dispose();
        }

        public void ActivityFinished()
        {
            Interlocked.Increment(ref _uncommittedActivityCount);

            if (_uncommittedActivityCount == 1 || DateTime.UtcNow - _lastCommitTime > MaxWaitTime)
                Commit();
        }


        private void Commit()
        {
            IndexManager.Commit();
            Interlocked.Exchange(ref _uncommittedActivityCount, 0);
            _lastCommitTime = DateTime.UtcNow;
        }
    }
}
