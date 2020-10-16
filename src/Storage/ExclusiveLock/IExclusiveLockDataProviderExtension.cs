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
        /// Tries to acquire a named exclusive lock. Returns true if successful or false if the lock is used by another
        /// thread, process or application domain.
        /// </summary>
        /// <param name="key">The unique name of the exclusive lock.</param>
        /// <param name="operationId">Unique identifier of the caller thread, process or appdomain.</param>
        /// <param name="timeLimit">The expiration date of the obtained exclusive lock.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps a boolean value.</returns>
        Task<bool> AcquireAsync(string key, string operationId, DateTime timeLimit,
            CancellationToken cancellationToken);
        /// <summary>
        /// Updates the expiration date of an existing lock.
        /// If the lock does not exist, the operation is skipped.
        /// </summary>
        /// <param name="key">The unique name of the exclusive lock.</param>
        /// <param name="operationId">Unique identifier of the caller thread, process or appdomain.</param>
        /// <param name="newTimeLimit">The new expiration date of the exclusive lock.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task RefreshAsync(string key, string operationId, DateTime newTimeLimit, CancellationToken cancellationToken);
        /// <summary>
        /// Releases an existing lock by the given unique name (<paramref name="key"/>).
        /// If the lock does not exist, the operation is skipped.
        /// </summary>
        /// <param name="key">The unique name of the exclusive lock.</param>
        /// <param name="operationId">Unique identifier of the caller thread, process or appdomain.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task ReleaseAsync(string key, string operationId, CancellationToken cancellationToken);
        /// <summary>
        /// Returns true if the named exclusive lock exists.
        /// </summary>
        /// <param name="key">The unique name of the exclusive lock.</param>
        /// <param name="operationId">Unique identifier of the caller thread, process or appdomain.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the query result.</returns>
        Task<bool> IsLockedAsync(string key, string operationId, CancellationToken cancellationToken);
        /// <summary>
        /// Checks whether the exclusive lock feature can be used or not.
        /// </summary>
        /// <remarks>
        /// Normally the sensenet patch system can install a feature before the first usage of the component. 
        /// Because this feature is used by patch system, if the exclusive lock persistence object is missing,
        /// an error will be thrown. This method is called after catching these errors to help detect the real
        /// reason of the problem.
        /// </remarks>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation and wraps the query result.</returns>
        Task<bool> IsFeatureAvailable(CancellationToken cancellationToken);
        /// <summary>
        /// Releases all locks immediately.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task ReleaseAllAsync(CancellationToken cancellationToken);
    }
}
