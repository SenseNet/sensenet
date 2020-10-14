using System;
using System.Threading;
using SenseNet.ContentRepository.Storage.Data;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Defines a configuration of the exclusive block's execution.
    /// </summary>
    public class ExclusiveBlockConfiguration
    {
        /// <summary>
        /// Gets or sets the timeout of the obtained exclusive lock.
        /// If the time is out, the lock is automatically released.
        /// </summary>
        public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(/*1*/30); //UNDONE:X: configure the default LockTimeout
        /// <summary>
        /// Gets or sets the length of the lock monitoring period. Used in algorithms that wait for the releasing the lock.
        /// </summary>
        public TimeSpan PollingTime { get; set; } = TimeSpan.FromSeconds(/*0.1*/5); //UNDONE:X: configure the default PollingTime
        /// <summary>
        /// Gets or sets the lock monitoring timeout. After this time a <see cref="TimeoutException"/> will be thrown.
        /// </summary>
        public TimeSpan WaitTimeout { get; set; } = TimeSpan.FromMinutes(2); //UNDONE:X: configure the default WaitTimeout
        /// <summary>
        /// Gets the current implementation of the <see cref="IExclusiveLockDataProviderExtension"/>.
        /// </summary>
        internal IExclusiveLockDataProviderExtension DataProvider { get; set; } =
            DataStore.GetDataProviderExtension<IExclusiveLockDataProviderExtension>();
        internal CancellationToken CancellationToken { get; set; }
    }
}
