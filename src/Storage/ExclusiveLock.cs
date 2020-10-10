using System;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    public class ExclusiveLock : IDisposable
    {
        private class LockGuard : IDisposable
        {
            private readonly string _key;
            private readonly string _operationId;
            private readonly TimeSpan _refreshPeriod;
            private readonly IExclusiveLockDataProviderExtension _dataProvider;
            private readonly CancellationTokenSource _finisher;

            public static LockGuard Create(ExclusiveBlockContext context, string key)
            {
                var guard = new LockGuard(context, key);
                //#pragma warning disable 4014
                //                guard.StartAsync();
                //#pragma warning restore 4014
                Task.Run(() => guard.StartAsync());
                return guard;
            }

            private LockGuard(ExclusiveBlockContext context, string key)
            {
                _key = key;
                _operationId = context.OperationId;
                _refreshPeriod = new TimeSpan(context.LockTimeout.Ticks / 2 - 1);
                _dataProvider = context.DataProvider;
                _finisher = new CancellationTokenSource();
                SnTrace.System.Write("ExclusiveLock guard created for {0} {1}. RefreshPeriod: {2}", key, _operationId, _refreshPeriod);
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
                SnTrace.System.Write("ExclusiveLock guard {0} {1}. Refresh lock", _key, _operationId);
                await _dataProvider.RefreshAsync(_key, DateTime.UtcNow.Add(_refreshPeriod));
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
                SnTrace.System.Write("ExclusiveLock guard {0} {1}. Disposed", _key, _operationId);
            }
        }

        private readonly LockGuard _guard;
        public ExclusiveBlockContext _context;

        public string Key { get; }
        public bool Acquired { get; }

        public ExclusiveLock(ExclusiveBlockContext context, string key, bool acquired)
        {
            Key = key;
            _context = context;
            // ReSharper disable once AssignmentInConditionalExpression
            if (Acquired = acquired)
            {
                _guard = LockGuard.Create(context, key);
            }
            SnTrace.System.Write("ExclusiveLock {0} {1}. Created. Acquired = {2}", Key,
                _context.OperationId, acquired);
        }

        //TODO: Implement AsyncDispose pattern if the framework fixes the "Microsoft.Bcl.AsyncInterfaces" assembly load problem.
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
            _context.DataProvider?.ReleaseAsync(Key).ConfigureAwait(false).GetAwaiter().GetResult();
            SnTrace.System.Write("ExclusiveLock {0} {1}. Released", Key, _context.OperationId);
            SnTrace.System.Write("ExclusiveLock {0} {1}. Disposed", Key, _context.OperationId);
        }
    }
}
