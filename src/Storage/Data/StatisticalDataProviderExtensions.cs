using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
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
        public static IRepositoryBuilder UseStatisticalDataProvider(this IRepositoryBuilder builder, IStatisticalDataProvider provider)
        {
            Providers.Instance.DataProvider.SetExtension(typeof(IStatisticalDataProvider), provider);
            return builder;
        }
    }
}
