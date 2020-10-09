using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.InMemory
{
    public class InMemoryExclusiveLockDataProvider : IExclusiveLockDataProviderExtension
    {
        private static readonly object Sync = new object();
        private readonly Dictionary<string, DateTime> _locks = new Dictionary<string, DateTime>();

        public Task<ExclusiveLock> AcquireAsync(string key, string operationId, DateTime timeLimit)
        {
            lock (Sync)
            {
                if (_locks.TryGetValue(key, out var existingTimeLimit) && DateTime.UtcNow < existingTimeLimit)
                    return STT.Task.FromResult(new ExclusiveLock(key, operationId, false, this));
                _locks[key] = timeLimit;
                return STT.Task.FromResult(new ExclusiveLock(key, operationId, true, this));
            }
        }
        public STT.Task RefreshAsync(string key, DateTime newTimeLimit)
        {
            lock (Sync)
            {
                if (_locks.ContainsKey(key))
                    _locks[key] = newTimeLimit;
            }
            return STT.Task.CompletedTask;
        }
        public STT.Task ReleaseAsync(string key)
        {
            lock (Sync)
            {
                if (_locks.ContainsKey(key))
                    _locks.Remove(key);
            }
            return STT.Task.CompletedTask;
        }
        public Task<bool> IsLockedAsync(string key)
        {
            lock (Sync)
                return STT.Task.FromResult(_locks.ContainsKey(key));
        }
    }
}
