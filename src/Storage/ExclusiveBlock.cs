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

    public class ExclusiveBlockContext
    {
        public string OperationId { get; set; } = Guid.NewGuid().ToString();
        public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(1); //UNDONE:X: configure the default LockTimeout
        public TimeSpan PollingTime { get; set; } = TimeSpan.FromSeconds(0.1); //UNDONE:X: configure the default PollingTime
        public TimeSpan WaitTimeout { get; set; } = TimeSpan.FromSeconds(2); //UNDONE:X: configure the default WaitTimeout
        public IExclusiveLockDataProviderExtension DataProvider { get; set; } =
            DataStore.GetDataProviderExtension<IExclusiveLockDataProviderExtension>();
    }

    public class ExclusiveBlock
    {
        public static Task RunAsync(string key, ExclusiveBlockType lockType, Func<Task> action)
        {
            return RunAsync(new ExclusiveBlockContext(), key, lockType, action);
        }
        public static async Task RunAsync(ExclusiveBlockContext context, string key, ExclusiveBlockType lockType, 
            Func<Task> action)
        {
            var timeLimit = DateTime.UtcNow.Add(context.LockTimeout);
            var dataProvider = DataStore.GetDataProviderExtension<IExclusiveLockDataProviderExtension>();

            var reTryTimeLimit = DateTime.UtcNow.Add(context.WaitTimeout);
            var operationId = context.OperationId;
            switch (lockType)
            {
                case ExclusiveBlockType.SkipIfLocked:
                    using (var exLock = await dataProvider.AcquireAsync(context, key, timeLimit) // ? TryAcquire
                        .ConfigureAwait(false))
                    {
                        if(exLock.Acquired)
                            await action();
                    } // releases the lock
                    break;

                case ExclusiveBlockType.WaitForReleased:
                    using (var exLock = await dataProvider.AcquireAsync(context, key, timeLimit)
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
                            await WaitForRelease(context, key, reTryTimeLimit);
                        }
                    } // releases the lock
                    break;

                case ExclusiveBlockType.WaitAndAcquire:
                    while (true)
                    {
                        using (var exLock = await dataProvider.AcquireAsync(context, key, timeLimit)
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

                        await WaitForRelease(context, key, reTryTimeLimit);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lockType), lockType, null);
            }
        }

        private static async Task WaitForRelease(ExclusiveBlockContext context, string key, DateTime reTryTimeLimit)
        {
            var operationId = context.OperationId;
            SnTrace.Write($"#{operationId} wait for release");
            while (true)
            {
                if (DateTime.UtcNow > reTryTimeLimit)
                {
                    SnTrace.Write($"#{operationId} timeout");
                    throw new TimeoutException(
                        "The exclusive lock was not released within the specified time");
                }
                if (!await context.DataProvider.IsLockedAsync(key))
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
