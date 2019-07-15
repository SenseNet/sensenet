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

        Task DeleteAllSharedLocksAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task CreateSharedLockAsync(int contentId, string @lock, CancellationToken cancellationToken = default(CancellationToken));
        Task<string> RefreshSharedLockAsync(int contentId, string @lock, CancellationToken cancellationToken = default(CancellationToken));
        Task<string> ModifySharedLockAsync(int contentId, string @lock, string newLock, CancellationToken cancellationToken = default(CancellationToken));
        Task<string> GetSharedLockAsync(int contentId, CancellationToken cancellationToken = default(CancellationToken));
        Task<string> DeleteSharedLockAsync(int contentId, string @lock, CancellationToken cancellationToken = default(CancellationToken));
        Task CleanupSharedLocksAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Designed for test purposes. DO NOT USE THIS METHOD IN YOUR CODE.
        /// </summary>
        void SetSharedLockCreationDate(int nodeId, DateTime value);
        /// <summary>
        /// Designed for test purposes. DO NOT USE THIS METHOD IN YOUR CODE.
        /// </summary>
        DateTime GetSharedLockCreationDate(int nodeId);
    }
}
