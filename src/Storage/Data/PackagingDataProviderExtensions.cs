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
        /// Sets the current <see cref="IPackagingDataProviderExtension"/> instance that will be responsible
        /// for packaging operations.
        /// </summary>
        /// <param name="builder">The IRepositoryBuilder instance.</param>
        /// <param name="provider">The extension instance to set.</param>
        /// <returns>The updated IRepositoryBuilder.</returns>
        public static IRepositoryBuilder UsePackagingDataProviderExtension(this IRepositoryBuilder builder, IPackagingDataProviderExtension provider)
        {
            DataStore.DataProvider.SetExtension(typeof(IPackagingDataProviderExtension), provider);
            return builder;
        }
    }
}
