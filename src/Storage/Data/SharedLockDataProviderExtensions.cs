using System;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for the shared lock feature.
    /// </summary>
    public static class SharedLockDataProviderExtensions
    {
        /// <summary>
        /// Sets the current <see cref="ISharedLockDataProvider"/> instance that will be responsible
        /// for managing shared locks.
        /// </summary>
        /// <param name="builder">The IRepositoryBuilder instance.</param>
        /// <param name="provider">The extension instance to set.</param>
        /// <returns>The updated IRepositoryBuilder.</returns>
        [Obsolete("Do not use this method anymore. Register ISharedLockDataProvider as a service instead.", true)]
        public static IRepositoryBuilder UseSharedLockDataProvider(this IRepositoryBuilder builder, ISharedLockDataProvider provider)
        {
            return builder;
        }
    }
}
