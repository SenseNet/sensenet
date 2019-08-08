using System;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Tools;
// ReSharper disable CheckNamespace

namespace SenseNet.ContentRepository.Storage.Data
{
    public static class SharedLockDataProviderExtensions
    {
        public static IRepositoryBuilder UseSharedLockDataProviderExtension(this IRepositoryBuilder builder, ISharedLockDataProviderExtension provider)
        {
            DataStore.DataProvider.SetExtension(typeof(ISharedLockDataProviderExtension), provider);
            return builder;
        }
    }

    /// <summary>
    /// Defines methods for handling shared lock storage.
    /// </summary>
    public interface ISharedLockDataProviderExtension : IDataProviderExtension
    {
        TimeSpan SharedLockTimeout { get; }

        Task DeleteAllSharedLocksAsync(CancellationToken cancellationToken);
        Task CreateSharedLockAsync(int contentId, string @lock, CancellationToken cancellationToken);
        Task<string> RefreshSharedLockAsync(int contentId, string @lock, CancellationToken cancellationToken);
        Task<string> ModifySharedLockAsync(int contentId, string @lock, string newLock, CancellationToken cancellationToken);
        Task<string> GetSharedLockAsync(int contentId, CancellationToken cancellationToken);
        Task<string> DeleteSharedLockAsync(int contentId, string @lock, CancellationToken cancellationToken);
        Task CleanupSharedLocksAsync(CancellationToken cancellationToken);
    }
}
