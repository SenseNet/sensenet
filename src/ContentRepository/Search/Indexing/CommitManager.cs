using System;
using System.Threading;
using System.Timers;
using STT = System.Threading.Tasks;

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
        STT.Task ActivityFinishedAsync();
    }

    internal class NoDelayCommitManager : ICommitManager
    {
        private IndexManager_INSTANCE _indexManager;

        public NoDelayCommitManager(IndexManager_INSTANCE indexManager)
        {
            _indexManager = indexManager;
        }

        public void Start()
        {
            // do nothing
        }
        public void ShutDown()
        {
            // do nothing
        }
        public STT.Task ActivityFinishedAsync()
        {
            return _indexManager.CommitAsync(CancellationToken.None);
        }
    }

    internal class NearRealTimeCommitManager : ICommitManager
    {
        private IndexManager_INSTANCE _indexManager;

        private static readonly TimeSpan MaxWaitTime = TimeSpan.FromSeconds(10);
        private DateTime _lastCommitTime;
        private int _uncommittedActivityCount;

        private static System.Timers.Timer _timer;
        private static readonly int HearthBeatMilliseconds = 1000;

        public NearRealTimeCommitManager(IndexManager_INSTANCE indexManager)
        {
            _indexManager = indexManager;
        }

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
                CommitAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public void ShutDown()
        {
            _timer.Enabled = false;
            _timer.Dispose();
        }

        public async STT.Task ActivityFinishedAsync()
        {
            Interlocked.Increment(ref _uncommittedActivityCount);

            if (_uncommittedActivityCount == 1 || DateTime.UtcNow - _lastCommitTime > MaxWaitTime)
                await CommitAsync().ConfigureAwait(false);
        }

        private async STT.Task CommitAsync()
        {
            await _indexManager.CommitAsync(CancellationToken.None).ConfigureAwait(false);
            Interlocked.Exchange(ref _uncommittedActivityCount, 0);
            _lastCommitTime = DateTime.UtcNow;
        }
    }
}
