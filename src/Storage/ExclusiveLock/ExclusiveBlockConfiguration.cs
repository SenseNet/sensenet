using System;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Defines a configuration of the exclusive block's execution.
    /// </summary>
    public class ExclusiveBlockConfiguration : SnConfig
    {
        private const string SectionName = "sensenet/exclusiveLock";

        internal static double LockTimeoutInSeconds { get; set; }
            = GetDouble(SectionName, nameof(LockTimeoutInSeconds), 30.0, 5.0);
        internal static double PollingTimeInSeconds { get; set; }
            = GetDouble(SectionName, nameof(PollingTimeInSeconds), 5.0, 1.0);
        internal static double WaitTimeoutInSeconds { get; set; }
            = GetDouble(SectionName, nameof(WaitTimeoutInSeconds), 120.0, 20.0);

        /// <summary>
        /// Gets or sets the timeout of the obtained exclusive lock.
        /// If the time is out, the lock is automatically released.
        /// </summary>
        public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(LockTimeoutInSeconds);
        /// <summary>
        /// Gets or sets the length of the lock monitoring period. Used in algorithms that wait for the releasing the lock.
        /// </summary>
        public TimeSpan PollingTime { get; set; } = TimeSpan.FromSeconds(PollingTimeInSeconds);
        /// <summary>
        /// Gets or sets the lock monitoring timeout. After this time a <see cref="TimeoutException"/> will be thrown.
        /// </summary>
        public TimeSpan WaitTimeout { get; set; } = TimeSpan.FromSeconds(WaitTimeoutInSeconds);
        /// <summary>
        /// Gets the current implementation of the <see cref="IExclusiveLockDataProviderExtension"/>.
        /// </summary>
        internal IExclusiveLockDataProviderExtension DataProvider { get; set; } =
            DataStore.GetDataProviderExtension<IExclusiveLockDataProviderExtension>();
        internal CancellationToken CancellationToken { get; set; }
    }
}
