using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.InMemory
{
    public class InMemoryExclusiveLockDataProvider : IExclusiveLockDataProviderExtension
    {
        private static readonly object Sync = new object();
        private readonly Dictionary<string, DateTime> _locks = new Dictionary<string, DateTime>();

        /// <inheritdoc/>
        public Task<ExclusiveLock> AcquireAsync(ExclusiveBlockContext context, string key, DateTime timeLimit,
            CancellationToken cancellationToken) //UNDONE:X: cancellationToken
        {
            lock (Sync)
            {
                if (_locks.TryGetValue(key, out var existingTimeLimit) && DateTime.UtcNow < existingTimeLimit)
                    return STT.Task.FromResult(new ExclusiveLock(context, key, false));
                _locks[key] = timeLimit;
                return STT.Task.FromResult(new ExclusiveLock(context, key, true));
            }
        }
        /// <inheritdoc/>
        public STT.Task RefreshAsync(string key, DateTime newTimeLimit, CancellationToken cancellationToken) //UNDONE:X: cancellationToken
        {
            lock (Sync)
            {
                if (_locks.ContainsKey(key))
                    _locks[key] = newTimeLimit;
            }
            return STT.Task.CompletedTask;
        }
        /// <inheritdoc/>
        public STT.Task ReleaseAsync(string key, CancellationToken cancellationToken) //UNDONE:X: cancellationToken
        {
            lock (Sync)
            {
                if (_locks.ContainsKey(key))
                    _locks.Remove(key);
            }
            return STT.Task.CompletedTask;
        }
        /// <inheritdoc/>
        public Task<bool> IsLockedAsync(string key, CancellationToken cancellationToken) //UNDONE:X: cancellationToken
        {
            lock (Sync)
                return STT.Task.FromResult(_locks.ContainsKey(key));
        }
    }
}
