using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Represents a response object of a persistent, distributed application wide exclusive lock.
    /// Only the persistence layer (data provider) can effectively create it. If the instance of this object
    /// means an obtained lock (Acquired is true), it keeps alive the persistent lock with a periodically
    /// update operation.
    /// </summary>
    internal class ExclusiveLock : IDisposable
    {
        private class LockGuard : IDisposable
        {
            private readonly string _key;
            private readonly string _operationId;
            private readonly TimeSpan _refreshPeriod;
            private readonly IExclusiveLockDataProviderExtension _dataProvider;
            private readonly CancellationTokenSource _finisher;
            private readonly CancellationToken _cancellationToken;

            public static LockGuard Create(ExclusiveBlockContext context, string key, CancellationToken cancellationToken)
            {
                var guard = new LockGuard(context, key, cancellationToken);
                //#pragma warning disable 4014
                //                guard.StartAsync();
                //#pragma warning restore 4014
                Task.Run(() => guard.StartAsync());
                return guard;
            }

            private LockGuard(ExclusiveBlockContext context, string key, CancellationToken cancellationToken)
            {
                _key = key;
                _operationId = context.OperationId;
                _refreshPeriod = new TimeSpan(context.LockTimeout.Ticks / 2 - 1);
                _dataProvider = context.DataProvider;
                _finisher = new CancellationTokenSource();
                _cancellationToken = cancellationToken;
                Trace.WriteLine($"SnTrace: System: ExclusiveLock guard created for {key} #{_operationId}. RefreshPeriod: {_refreshPeriod}");
            }

            private async Task StartAsync()
            {
                while (!_finisher.IsCancellationRequested)
                {
                    await Task.Delay(_refreshPeriod, _finisher.Token);

                    if (!_finisher.IsCancellationRequested)
                        await RefreshAsync();
                }
            }

            private async Task RefreshAsync()
            {
                Trace.WriteLine($"SnTrace: System: ExclusiveLock guard {_key} #{_operationId}. Refresh lock");
                await _dataProvider.RefreshAsync(_key, _operationId, DateTime.UtcNow.Add(_refreshPeriod), _cancellationToken);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (!disposing)
                    return;
                _finisher.Cancel();
                Trace.WriteLine($"SnTrace: System: ExclusiveLock guard {_key} #{_operationId}. Disposed");
            }
        }

        private readonly LockGuard _guard;
        private readonly ExclusiveBlockContext _context;
        private readonly CancellationToken _cancellationToken;
        private readonly bool _isFeatureAvailable;

        /// <summary>
        /// Gets the unique name of the action.
        /// </summary>
        public string Key { get; }
        /// <summary>
        /// Gets a value that is true if the lock is obtained otherwise false.
        /// </summary>
        public bool Acquired { get; }

        /// <summary>
        /// Initializes an instance of the <see cref="ExclusiveLock"/>.
        /// </summary>
        /// <param name="context">The configuration of the exclusive execution.</param>
        /// <param name="key">The unique name of the exclusive lock.</param>
        /// <param name="acquired">True if the lock is obtained otherwise false.</param>
        public ExclusiveLock(ExclusiveBlockContext context, string key, bool acquired, bool isFeatureAvailable,
            CancellationToken cancellationToken)
        {
            Key = key;
            Acquired = acquired; 
            _context = context;
            _cancellationToken = cancellationToken;
            _isFeatureAvailable = isFeatureAvailable;
            if (Acquired && isFeatureAvailable)
                _guard = LockGuard.Create(context, key, _cancellationToken);

            Trace.WriteLine($"SnTrace: System: ExclusiveLock {Key} #{_context.OperationId}. Created. Acquired = {acquired}. IsFeatureAvailable = {isFeatureAvailable}");
        }

        //TODO: Implement AsyncDispose pattern if the framework fixes the "Microsoft.Bcl.AsyncInterfaces" assembly load problem.
        /// <summary>Releases all resources used by the <see cref="ExclusiveLock"/>.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            _guard?.Dispose();
            if (_isFeatureAvailable && Acquired)
            {
                _context.DataProvider?.ReleaseAsync(Key, _context.OperationId, _cancellationToken).ConfigureAwait(false).GetAwaiter()
                    .GetResult();
                Trace.WriteLine(
                    $"SnTrace: System: ExclusiveLock {Key} #{_context.OperationId}: Released.");
            }
            Trace.WriteLine($"SnTrace: System: ExclusiveLock {Key} #{_context.OperationId}: Disposed");
        }
    }
}
