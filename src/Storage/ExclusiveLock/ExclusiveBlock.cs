using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
        /// <param name="operationId">Unique identifier of the caller thread, process or appdomain.</param>
        /// <param name="blockType">The algorithm of the exclusive execution.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <param name="action">The code block that will be executed exclusively.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        /// <exception cref="DataException">The operation causes a database-related error.</exception>
        /// <exception cref="TimeoutException">The exclusive lock was not released within the specified time</exception>
        /// <exception cref="OperationCanceledException">The operation has been cancelled.</exception>
        public static Task RunAsync(string key, string operationId, ExclusiveBlockType blockType,
            CancellationToken cancellationToken, Func<Task> action)
        {
            return RunAsync(key, operationId, blockType, new ExclusiveBlockConfiguration(), cancellationToken, action);
        }

        /// <summary>
        /// Executes a named action depending on the chosen algorithm. The default behavior can be modified by the
        /// given <see cref="ExclusiveBlockConfiguration"/>.
        /// </summary>
        /// <param name="key">The unique name of the action.</param>
        /// <param name="operationId">Unique identifier of the caller thread, process or appdomain.</param>
        /// <param name="blockType">The algorithm of the exclusive execution.</param>
        /// <param name="config">The configuration of the exclusive execution.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <param name="action">The code block that will be executed exclusively.</param>
        /// <returns></returns>
        public static async Task RunAsync(string key, string operationId, ExclusiveBlockType blockType,
            ExclusiveBlockConfiguration config, CancellationToken cancellationToken, Func<Task> action)
        {
            config.CancellationToken = cancellationToken;

            var reTryTimeLimit = DateTime.UtcNow.Add(config.WaitTimeout);
            switch (blockType)
            {
                case ExclusiveBlockType.SkipIfLocked:
                    using (var exLock = await GetLock(key, operationId, config)
                        .ConfigureAwait(false))
                    {
                        if(exLock.Acquired)
                            await action();
                    } // releases the lock
                    break;

                case ExclusiveBlockType.WaitForReleased:
                    using (var exLock = await GetLock(key, operationId, config)
                        .ConfigureAwait(false))
                    {
                        if (exLock.Acquired)
                        {
                            await action();
                        }
                        else
                        {
                            await WaitForRelease(key, operationId, reTryTimeLimit, config);
                        }
                    } // releases the lock
                    break;

                case ExclusiveBlockType.WaitAndAcquire:
                    while (true)
                    {
                        using (var exLock = await GetLock(key, operationId, config)
                            .ConfigureAwait(false))
                        {
                            if (exLock.Acquired)
                            {
                                await action();
                                break;
                            }
                        } // releases the lock

                        await WaitForRelease(key, operationId, reTryTimeLimit, config);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(blockType), blockType, null);
            }
        }

        private static async Task<ExclusiveLock> GetLock(string key, string operationId,
            ExclusiveBlockConfiguration config)
        {
            try
            {
                var timeLimit = DateTime.UtcNow.Add(config.LockTimeout);
                var acquired = await config.DataProvider.AcquireAsync(key, operationId, timeLimit, config.CancellationToken)
                    .ConfigureAwait(false);
                return new ExclusiveLock(key, operationId, acquired, true, config);
            }
            catch
            {
                if (await config.DataProvider.IsFeatureAvailable(config.CancellationToken))
                    throw;
                WriteFeatureWarning();
                return new ExclusiveLock(key, operationId, true, false, config);
            }
        }

        private static bool _isFeatureWarningWritten;
        private static void WriteFeatureWarning()
        {
            if (_isFeatureWarningWritten)
                return;
            SnLog.WriteWarning("Exclusive lock feature is not available.");
            _isFeatureWarningWritten = true;
        }

        private static async Task WaitForRelease(string key, string operationId, DateTime reTryTimeLimit,
            ExclusiveBlockConfiguration config)
        {
            Trace.WriteLine($"SnTrace: {key} #{operationId} wait for release");
            while (true)
            {
                if (DateTime.UtcNow > reTryTimeLimit)
                {
                    Trace.WriteLine($"SnTrace: {key} #{operationId} timeout");
                    throw new TimeoutException(
                        "The exclusive lock was not released within the specified time.");
                }
                if (!await config.DataProvider.IsLockedAsync(key, operationId, config.CancellationToken))
                {
                    Trace.WriteLine($"SnTrace: {key} #{operationId} exit: unlocked");
                    break;
                }
                Trace.WriteLine($"SnTrace: {key} #{operationId} wait: locked by another");
                await Task.Delay(config.PollingTime, config.CancellationToken);
            }
        }
    }
}
