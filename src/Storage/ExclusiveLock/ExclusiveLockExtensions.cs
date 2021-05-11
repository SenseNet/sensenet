using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class ExclusiveLockExtensions
    {
        /// <summary>
        /// Sets the current <see cref="IExclusiveLockDataProviderExtension"/> instance that will be responsible
        /// for managing exclusive locks.
        /// </summary>
        /// <param name="builder">The IRepositoryBuilder instance.</param>
        /// <param name="provider">The extension instance to set.</param>
        /// <returns>The updated IRepositoryBuilder.</returns>
        public static IRepositoryBuilder UseExclusiveLockDataProviderExtension(this IRepositoryBuilder builder, 
            IExclusiveLockDataProviderExtension provider)
        {
            DataStore.DataProvider.SetExtension(typeof(IExclusiveLockDataProviderExtension), provider);
            return builder;
        }
        /// <summary>
        /// Sets an <see cref="MsSqlExclusiveLockDataProvider"/> as the current
        /// <see cref="IExclusiveLockDataProviderExtension"/> instance that will be responsible
        /// for managing exclusive locks.
        /// </summary>
        /// <param name="builder">The IRepositoryBuilder instance.</param>
        /// <returns>The updated IRepositoryBuilder.</returns>
        public static IRepositoryBuilder UseMsSqlExclusiveLockDataProviderExtension(this IRepositoryBuilder builder)
        {
            return UseExclusiveLockDataProviderExtension(builder, new MsSqlExclusiveLockDataProvider());
        }
    }
}
