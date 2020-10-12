using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.InMemory
{
    /// <summary>
    /// In memory implementation of the <see cref="IExclusiveLockDataProviderExtension"/> interface.
    /// </summary>
    public class InMemoryExclusiveLockDataProvider : IExclusiveLockDataProviderExtension
    {
        private static readonly object Sync = new object();
        private readonly Dictionary<string, DateTime> _locks = new Dictionary<string, DateTime>();

        /// <inheritdoc/>
        public Task<bool> AcquireAsync(ExclusiveBlockContext context, string key, DateTime timeLimit,
            CancellationToken cancellationToken)
        {
            lock (Sync)
            {
                var exists = _locks.TryGetValue(key, out var existingTimeLimit) && DateTime.UtcNow < existingTimeLimit;
                if (!exists)
                    _locks[key] = timeLimit;
                return STT.Task.FromResult(!exists);
            }
        }
        /// <inheritdoc/>
        public STT.Task RefreshAsync(string key, DateTime newTimeLimit, CancellationToken cancellationToken)
        {
            lock (Sync)
            {
                if (_locks.ContainsKey(key))
                    _locks[key] = newTimeLimit;
            }
            return STT.Task.CompletedTask;
        }
        /// <inheritdoc/>
        public STT.Task ReleaseAsync(string key, CancellationToken cancellationToken)
        {
            lock (Sync)
            {
                if (_locks.ContainsKey(key))
                    _locks.Remove(key);
            }
            return STT.Task.CompletedTask;
        }
        /// <inheritdoc/>
        public Task<bool> IsLockedAsync(string key, CancellationToken cancellationToken)
        {
            lock (Sync)
                return STT.Task.FromResult(_locks.ContainsKey(key));
        }
    }
}
