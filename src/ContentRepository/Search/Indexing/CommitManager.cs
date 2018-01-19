﻿using System;
using System.Threading;
using System.Timers;

namespace SenseNet.ContentRepository.Search.Indexing
{
    /// <summary>
    /// Defines an interface for a singleton implementation that can manage index commits with various algorithms.
    /// </summary>
    public interface ICommitManager
    {
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        void Start();
        /// <summary>
        /// Stops the instance. Designed for the last index commit of the repository's lifecycle.
        /// </summary>
        void ShutDown();
        /// <summary>
        /// Called by the indexing activity manager after execution of every activity.
        /// </summary>
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
            _timer.Elapsed += Timer_Elapsed;
            _timer.Disposed += Timer_Disposed;
            _timer.Enabled = true;

        }
        private void Timer_Disposed(object sender, EventArgs e)
        {
            _timer.Elapsed -= Timer_Elapsed;
            _timer.Disposed -= Timer_Disposed;
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
