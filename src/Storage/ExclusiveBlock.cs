using System;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    public enum ExclusiveBlockType
    {
        SkipIfLocked,
        WaitForReleased,
        WaitAndAcquire
    }

    public class ExclusiveBlock
    {
        internal static TimeSpan LockTimeout = TimeSpan.FromSeconds(1); //UNDONE:X configure the general LockTimeout
        internal static TimeSpan PollingTime = TimeSpan.FromSeconds(0.1); //UNDONE:X configure the general PollingTime
        internal static TimeSpan WaitTimeout = TimeSpan.FromSeconds(2); //UNDONE:X configure the general WaitTimeout

        public static Task RunAsync(string key, string operationId, ExclusiveBlockType lockType, Func<Task> action)
        {
            return RunAsync(key, operationId, lockType, TimeSpan.Zero, action);
        }
        public static async Task RunAsync(string key, string operationId, ExclusiveBlockType lockType, TimeSpan timeout, Func<Task> action)
        {
            var timeLimit = DateTime.UtcNow.Add(LockTimeout);
            var dataProvider = DataStore.GetDataProviderExtension<IExclusiveLockDataProviderExtension>();

            switch (lockType)
            {
                case ExclusiveBlockType.SkipIfLocked:
                    using (var exLock = await dataProvider.AcquireExclusiveLock(key, operationId, timeLimit) // ? TryAcquire
                        .ConfigureAwait(false))
                    {
                        if(exLock.Acquired)
                            await action();
                    } // releases the lock
                    break;

                case ExclusiveBlockType.WaitForReleased:
                    using (var exLock = await dataProvider.AcquireExclusiveLock(key, operationId, timeLimit)
                        .ConfigureAwait(false))
                    {
                        if (exLock.Acquired)
                        {
                            await action();
                        }
                        else
                        {
                            while (true) //UNDONE:X WaitTimeout + exception?
                            {
                                if (!await dataProvider.IsLockedAsync(key))
                                    break;
                                await Task.Delay(PollingTime);
                            }
                        }
                    } // releases the lock
                    break;

                case ExclusiveBlockType.WaitAndAcquire:
                    var effectiveTimeout = timeout == TimeSpan.Zero ? WaitTimeout : timeout;
                    var reTryTimeLimit = DateTime.UtcNow.Add(effectiveTimeout);
                    while (true)
                    {
                        using (var exLock = await dataProvider.AcquireExclusiveLock(key, operationId, timeLimit)
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
                        while (true)
                        {
                            SnTrace.Write($"#{operationId} wait for release");
                            if (DateTime.UtcNow > reTryTimeLimit)
                            {
                                SnTrace.Write($"#{operationId} exit1");
                                break;
                            }

                            if (!await dataProvider.IsLockedAsync(key))
                            {
                                SnTrace.Write($"#{operationId} exit: unlocked");
                                break;
                            }
                            SnTrace.Write($"#{operationId} wait: locked by another");
                            await Task.Delay(PollingTime);
                        }

                        if (DateTime.UtcNow > reTryTimeLimit)
                        {
                            SnTrace.Write($"#{operationId} exit2"); //UNDONE:X: timeout exception
                            break;
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lockType), lockType, null);
            }
        }
    }
}
