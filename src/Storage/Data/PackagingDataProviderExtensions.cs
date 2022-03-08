using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for the packaging feature.
    /// </summary>
    public static class PackagingDataProviderExtensions
    {
        /// <summary>
        /// Sets the current <see cref="IPackagingDataProvider"/> instance that will be responsible
        /// for packaging operations.
        /// </summary>
        /// <param name="builder">The IRepositoryBuilder instance.</param>
        /// <param name="provider">The extension instance to set.</param>
        /// <returns>The updated IRepositoryBuilder.</returns>
        public static IRepositoryBuilder UsePackagingDataProvider(this IRepositoryBuilder builder, IPackagingDataProvider provider)
        {
            Providers.Instance.SetProvider(typeof(IPackagingDataProvider), provider);
            return builder;
        }
    }
}
