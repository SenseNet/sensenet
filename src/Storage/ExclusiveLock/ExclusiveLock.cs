﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Represents a response object of a persistent, distributed application wide exclusive lock.
    /// Only the persistence layer (data provider) can effectively create it. If the instance of this object
    /// means an obtained lock (Acquired is true), it keeps alive the persistent lock by updating
    /// it periodically.
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

            public static LockGuard Create(string key, string operationId, ExclusiveBlockConfiguration config)
            {
                var guard = new LockGuard(key, operationId, config);
                Task.Run(() => guard.StartAsync());
                return guard;
            }

            private LockGuard(string key, string operationId, ExclusiveBlockConfiguration config)
            {
                _key = key;
                _operationId = operationId;
                _refreshPeriod = new TimeSpan(config.LockTimeout.Ticks / 2 - 1);
                _dataProvider = config.DataProvider;
                _finisher = new CancellationTokenSource();
                _cancellationToken = config.CancellationToken;
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
        private readonly ExclusiveBlockConfiguration _config;
        private readonly bool _isFeatureAvailable;

        /// <summary>
        /// Gets the unique name of the action.
        /// </summary>
        public string Key { get; }
        /// <summary>
        /// Gets the unique identifier of the caller thread, process or appdomain.
        /// </summary>
        public string OperationId { get; }
        /// <summary>
        /// Gets a value that is true if the lock is obtained otherwise false.
        /// </summary>
        public bool Acquired { get; }

        /// <summary>
        /// Initializes an instance of the <see cref="ExclusiveLock"/>.
        /// </summary>
        /// <param name="key">The unique name of the exclusive lock.</param>
        /// <param name="operationId">Unique identifier of the caller thread, process or appdomain.</param>
        /// <param name="acquired">True if the lock is obtained otherwise false.</param>
        /// <param name="isFeatureAvailable">True if the feature is installed otherwise false.</param>
        /// <param name="config">The configuration of the exclusive execution.</param>
        public ExclusiveLock(string key, string operationId, bool acquired, bool isFeatureAvailable,
            ExclusiveBlockConfiguration config)
        {
            Key = key;
            OperationId = operationId;
            Acquired = acquired;
            _config = config;
            _isFeatureAvailable = isFeatureAvailable;
            if (Acquired && isFeatureAvailable)
                _guard = LockGuard.Create(key, operationId, config);

            Trace.WriteLine($"SnTrace: System: ExclusiveLock {key} #{operationId}. Created. Acquired = {acquired}. IsFeatureAvailable = {isFeatureAvailable}");
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
                _config.DataProvider?.ReleaseAsync(Key, OperationId, _config.CancellationToken).ConfigureAwait(false).GetAwaiter()
                    .GetResult();
                Trace.WriteLine(
                    $"SnTrace: System: ExclusiveLock {Key} #{OperationId}: Released.");
            }
            Trace.WriteLine($"SnTrace: System: ExclusiveLock {Key} #{OperationId}: Disposed");
        }
    }
}
