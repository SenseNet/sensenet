using System;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Data;

// ReSharper disable CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Defines methods for handling shared lock storage.
    /// </summary>
    public interface ISharedLockDataProviderExtension : IDataProviderExtension
    {
        TimeSpan SharedLockTimeout { get; }

        /// <summary>
        /// Deletes all shared locks from the system. Not intended for external callers.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task DeleteAllSharedLocksAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Sets a shared lock for a content. If the lock already exists, the shared lock data provider
        /// may refresh the creation date.
        /// </summary>
        /// <param name="contentId">Content id.</param>
        /// <param name="lock">Lock token.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task CreateSharedLockAsync(int contentId, string @lock, CancellationToken cancellationToken);
        /// <summary>
        /// Updates an existing shared lock. If the lock already exists, the shared lock data provider
        /// may refresh the creation date.
        /// </summary>
        /// <param name="contentId">Content id.</param>
        /// <param name="lock">Lock token.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task<string> RefreshSharedLockAsync(int contentId, string @lock, CancellationToken cancellationToken);

        /// <summary>
        /// Replaces an existing shared lock value with a new one.
        /// </summary>
        /// <param name="contentId">Content id.</param>
        /// <param name="lock">Lock token.</param>
        /// <param name="newLock"></param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the original lock value if exists.</returns>
        Task<string> ModifySharedLockAsync(int contentId, string @lock, string newLock, CancellationToken cancellationToken);
        /// <summary>
        /// Loads a shared lock value for the specified content id.
        /// </summary>
        /// <param name="contentId">Content id.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the loaded lock token.</returns>
        Task<string> GetSharedLockAsync(int contentId, CancellationToken cancellationToken);
        /// <summary>
        /// Deletes a shared lock from a content if exists.
        /// </summary>
        /// <param name="contentId">Content id.</param>
        /// <param name="lock">Lock token.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the original lock value if exists.</returns>
        Task<string> DeleteSharedLockAsync(int contentId, string @lock, CancellationToken cancellationToken);
        /// <summary>
        /// Deletes expired shared locks. Called by the maintenance task.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task CleanupSharedLocksAsync(CancellationToken cancellationToken);
    }
}
