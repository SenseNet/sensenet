using System;
using SenseNet.Diagnostics;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for the StatisticalDataProvider.
    /// </summary>
    public static class StatisticalDataProviderExtensions
    {
        /// <summary>
        /// Sets the current <see cref="IStatisticalDataProvider"/> instance.
        /// </summary>
        /// <param name="builder">The IRepositoryBuilder instance.</param>
        /// <param name="provider">The extension instance to set.</param>
        /// <returns>The updated IRepositoryBuilder.</returns>
        [Obsolete("Use this IServiceCollection extension: AddStatisticalDataProvider<T>()", true)]
        public static IRepositoryBuilder UseStatisticalDataProvider(this IRepositoryBuilder builder, IStatisticalDataProvider provider)
        {
            return builder;
        }
    }
}
