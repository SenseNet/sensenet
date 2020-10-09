using System;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    public interface IExclusiveLockDataProviderExtension : IDataProviderExtension
    {
        Task<ExclusiveLock> AcquireAsync(string key, string operationId, DateTime timeLimit);
        Task RefreshAsync(string key, DateTime newTimeLimit);
        Task ReleaseAsync(string key);
        Task<bool> IsLockedAsync(string key);
    }
}
