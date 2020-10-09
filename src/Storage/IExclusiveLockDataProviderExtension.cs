using System;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    public interface IExclusiveLockDataProviderExtension : IDataProviderExtension
    {
        Task<ExclusiveLock> AcquireExclusiveLock(string key, string operationId, DateTime timeLimit);
        Task RefreshExclusiveLockAsync(string key, DateTime newTimeLimit);
        Task ReleaseExclusiveLockAsync(string key);
        Task<bool> IsLockedAsync(string key);
    }
}
