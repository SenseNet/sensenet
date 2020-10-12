﻿using System;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Defines methods for handling the persistent, distributed application wide exclusive lock.
    /// </summary>
    public interface IExclusiveLockDataProviderExtension : IDataProviderExtension
    {
        /// <summary>
        /// Tries to achieve a named exclusive lock and returns its representation. The returning object stores
        /// the success of the request.
        /// </summary>
        /// <param name="context">The configuration of the exclusive block's execution.</param>
        /// <param name="key">The unique name of the exclusive lock.</param>
        /// <param name="timeLimit">The expiration date of the obtained exclusive lock.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the new ExclusiveLock instance.</returns>
        Task<bool> AcquireAsync(ExclusiveBlockContext context, string key, DateTime timeLimit,
            CancellationToken cancellationToken);
        /// <summary>
        /// Updates the expiration date of an existing lock.
        /// If the lock does not exist, the operation is skipped.
        /// </summary>
        /// <param name="key">The unique name of the exclusive lock.</param>
        /// <param name="newTimeLimit">The new expiration date of the exclusive lock.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task RefreshAsync(string key, DateTime newTimeLimit, CancellationToken cancellationToken);
        /// <summary>
        /// Releases an existing lock by the given unique name (<paramref name="key"/>).
        /// If the lock does not exist, the operation is skipped.
        /// </summary>
        /// <param name="key">The unique name of the exclusive lock.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task ReleaseAsync(string key, CancellationToken cancellationToken);
        /// <summary>
        /// Returns true if the named exclusive lock exists.
        /// </summary>
        /// <param name="key">The unique name of the exclusive lock.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the query result.</returns>
        Task<bool> IsLockedAsync(string key, CancellationToken cancellationToken);
    }
}
