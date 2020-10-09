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

            public static LockGuard Create(string key, string operationId, TimeSpan timeOut,
                IExclusiveLockDataProviderExtension dataProvider)
            {
                var guard = new LockGuard(key, operationId, timeOut, dataProvider);
                //#pragma warning disable 4014
                //                guard.StartAsync();
                //#pragma warning restore 4014
                Task.Run(() => guard.StartAsync());
                return guard;
            }

            private LockGuard(string key, string operationId, TimeSpan timeOut, IExclusiveLockDataProviderExtension dataProvider)
            {
                _key = key;
                _operationId = operationId;
                _refreshPeriod = new TimeSpan(timeOut.Ticks / 2 - 1);
                _dataProvider = dataProvider;
                _finisher = new CancellationTokenSource();
                SnTrace.System.Write("ExclusiveLock guard created for {0} {1}. RefreshPeriod: {2}", key, operationId, _refreshPeriod);
            }

            private async Task StartAsync()
            {
                while (!_finisher.IsCancellationRequested)
                {
                    await Task.Delay(_refreshPeriod, _finisher.Token);

                    if (!_finisher.IsCancellationRequested)
                        Refresh();
                }
            }

            private void Refresh() //UNDONE:X: async
            {
                SnTrace.System.Write("ExclusiveLock guard {0} {1}. Refresh lock", _key, _operationId);
                _dataProvider.RefreshExclusiveLockAsync(_key, DateTime.UtcNow.Add(_refreshPeriod));
            }

            //UNDONE:X: AsyncDispose !?
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
        private readonly IExclusiveLockDataProviderExtension _dataProvider;

        public string Key { get; }
        public string OperationId { get; }
        public bool Acquired { get; }

        public ExclusiveLock(string key, string operationId, bool acquired,
            IExclusiveLockDataProviderExtension dataProvider)
        {
            Key = key;
            OperationId = operationId;
            // ReSharper disable once AssignmentInConditionalExpression
            if (Acquired = acquired)
            {
                _dataProvider = dataProvider;
                _guard = LockGuard.Create(key, operationId, ExclusiveBlock.LockTimeout, dataProvider);
            }
            SnTrace.System.Write("ExclusiveLock {0} {1}. Created. Acquired = {2}", Key, OperationId, acquired);
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
            _guard?.Dispose();
            _dataProvider?.ReleaseExclusiveLockAsync(Key).ConfigureAwait(false).GetAwaiter().GetResult();
            SnTrace.System.Write("ExclusiveLock {0} {1}. Released", Key, OperationId);
            SnTrace.System.Write("ExclusiveLock {0} {1}. Disposed", Key, OperationId);
        }
    }
}
