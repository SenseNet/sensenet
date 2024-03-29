﻿using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Configuration;
using SenseNet.Tools.Configuration;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    [OptionsClass(sectionName: "sensenet:ExclusiveLock")]
    public class ExclusiveLockOptions
    {
        /// <summary>
        /// Gets or sets the timeout of the obtained exclusive lock.
        /// If the time runs out, the lock is automatically released.
        /// </summary>
        public int LockTimeoutInSeconds { get; set; } = 30;
        /// <summary>
        /// Gets or sets the length of the lock monitoring period. Used in algorithms
        /// that wait for releasing the lock.
        /// </summary>
        public int PollingTimeInSeconds { get; set; } = 5;
        /// <summary>
        /// Gets or sets the lock monitoring timeout. After this time a <see cref="TimeoutException"/> will be thrown.
        /// </summary>
        public int WaitTimeoutInSeconds { get; set; } = 120;
    }

    /// <summary>
    /// Defines a configuration of the exclusive block's execution.
    /// </summary>
    internal class ExclusiveBlockConfiguration : SnConfig
    {
        public ExclusiveBlockConfiguration(ExclusiveLockOptions options = null)
        {
            var exOptions = options ?? new ExclusiveLockOptions();

            LockTimeout = TimeSpan.FromSeconds(Math.Max(exOptions.LockTimeoutInSeconds, 5));
            PollingTime = TimeSpan.FromSeconds(Math.Max(exOptions.PollingTimeInSeconds, 1));
            WaitTimeout = TimeSpan.FromSeconds(Math.Max(exOptions.WaitTimeoutInSeconds, 20 ));
        }

        /// <summary>
        /// Gets or sets the timeout of the obtained exclusive lock.
        /// If the time is out, the lock is automatically released.
        /// </summary>
        public TimeSpan LockTimeout { get; set; }
        /// <summary>
        /// Gets or sets the length of the lock monitoring period. Used in algorithms that wait for releasing the lock.
        /// </summary>
        public TimeSpan PollingTime { get; set; }
        /// <summary>
        /// Gets or sets the lock monitoring timeout. After this time a <see cref="TimeoutException"/> will be thrown.
        /// </summary>
        public TimeSpan WaitTimeout { get; set; }

        internal IExclusiveLockDataProvider DataProvider { get; } =
            Providers.Instance.Services.GetRequiredService<IExclusiveLockDataProvider>() ??
            DefaultExclusiveLockDataProvider.Instance;

        internal CancellationToken CancellationToken { get; set; }
    }
}
