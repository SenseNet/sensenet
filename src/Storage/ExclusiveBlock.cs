﻿using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Determines the exclusive lock algorithm
    /// </summary>
    public enum ExclusiveBlockType
    {
        /// <summary>
        /// Requests an exclusive lock and executes the action if the lock is acquired
        /// otherwise returns immediately.
        /// </summary>
        SkipIfLocked,
        /// <summary>
        /// Requests an exclusive lock and executes the action if the lock is acquired
        /// otherwise waits for the lock is released then returns without executes the action.
        /// </summary>
        WaitForReleased,
        /// <summary>
        /// Requests an exclusive lock and executes the action if the lock is acquired
        /// otherwise waits for the lock is released then requests again.
        /// </summary>
        WaitAndAcquire
    }

    /// <summary>
    /// Defines a code block that can run only one instance at a time in the entire distributed application.
    /// </summary>
    public class ExclusiveBlock
    {
        /// <summary>
        /// Executes a named action depending on the chosen algorithm.
        /// </summary>
        /// <param name="key">The unique name of the action.</param>
        /// <param name="blockType">The algorithm of the exclusive execution.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <param name="action">The code block that will be executed exclusively.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        /// <exception cref="DataException">The operation causes a database-related error.</exception>
        /// <exception cref="TimeoutException">The exclusive lock was not released within the specified time</exception>
        /// <exception cref="OperationCanceledException">The operation has been cancelled.</exception>
        public static Task RunAsync(string key, ExclusiveBlockType blockType,
            CancellationToken cancellationToken, Func<Task> action)
        {
            return RunAsync(new ExclusiveBlockContext(), key, blockType, cancellationToken, action);
        }

        /// <summary>
        /// Executes a named action depending on the chosen algorithm. The default behavior can be modified by the
        /// given <see cref="ExclusiveBlockContext"/>.
        /// </summary>
        /// <param name="context">The configuration of the exclusive execution.</param>
        /// <param name="key">The unique name of the action.</param>
        /// <param name="blockType">The algorithm of the exclusive execution.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <param name="action">The code block that will be executed exclusively.</param>
        /// <returns></returns>
        public static async Task RunAsync(ExclusiveBlockContext context, string key, ExclusiveBlockType blockType,
            CancellationToken cancellationToken, Func<Task> action)
        {
            //UNDONE:X: Pass and handle cancellationToken
            context.CancellationToken = cancellationToken;
            var timeLimit = DateTime.UtcNow.Add(context.LockTimeout);

            var reTryTimeLimit = DateTime.UtcNow.Add(context.WaitTimeout);
            var operationId = context.OperationId;
            switch (blockType)
            {
                case ExclusiveBlockType.SkipIfLocked:
                    using (var exLock = await GetLock(context, key, timeLimit, cancellationToken)
                        .ConfigureAwait(false))
                    {
                        if(exLock.Acquired)
                            await action();
                    } // releases the lock
                    break;

                case ExclusiveBlockType.WaitForReleased:
                    using (var exLock = await GetLock(context, key, timeLimit, cancellationToken)
                        .ConfigureAwait(false))
                    {
                        SnTrace.Write($"#{operationId} acquire");
                        if (exLock.Acquired)
                        {
                            SnTrace.Write($"#{operationId} executing");
                            await action();
                            SnTrace.Write($"#{operationId} executed");
                        }
                        else
                        {
                            await WaitForRelease(context, key, reTryTimeLimit, cancellationToken);
                        }
                    } // releases the lock
                    break;

                case ExclusiveBlockType.WaitAndAcquire:
                    while (true)
                    {
                        using (var exLock = await GetLock(context, key, timeLimit, cancellationToken)
                            .ConfigureAwait(false))
                        {
                            SnTrace.Write($"#{operationId} acquire");
                            if (exLock.Acquired)
                            {
                                SnTrace.Write($"#{operationId} executing");
                                await action();
                                SnTrace.Write($"#{operationId} executed");
                                break;
                            }
                        } // releases the lock

                        await WaitForRelease(context, key, reTryTimeLimit, cancellationToken);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(blockType), blockType, null);
            }
        }

        private static async Task<ExclusiveLock> GetLock(ExclusiveBlockContext context, string key, DateTime timeLimit,
            CancellationToken cancellationToken)
        {
            var acquired = await context.DataProvider.AcquireAsync(context, key, timeLimit, cancellationToken)
                .ConfigureAwait(false);
            return new ExclusiveLock(context, key, acquired, cancellationToken);
        }

        private static async Task WaitForRelease(ExclusiveBlockContext context, string key, DateTime reTryTimeLimit,
            CancellationToken cancellationToken)
        {
            var operationId = context.OperationId;
            SnTrace.Write($"#{operationId} wait for release");
            while (true)
            {
                if (DateTime.UtcNow > reTryTimeLimit)
                {
                    SnTrace.Write($"#{operationId} timeout");
                    throw new TimeoutException(
                        "The exclusive lock was not released within the specified time.");
                }
                if (!await context.DataProvider.IsLockedAsync(key, cancellationToken))
                {
                    SnTrace.Write($"#{operationId} exit: unlocked");
                    break;
                }
                SnTrace.Write($"#{operationId} wait: locked by another");
                await Task.Delay(context.PollingTime);
            }
        }
    }
}
