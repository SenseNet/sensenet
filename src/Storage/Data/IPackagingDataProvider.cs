using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Storage.Data
{
    public static class PackagingDataProviderExtensions
    {
        public static IRepositoryBuilder UsePackagingDataProviderExtension(this IRepositoryBuilder builder, IPackagingDataProviderExtension provider)
        {
            if (DataStore.Enabled)
                DataStore.DataProvider.SetExtension(typeof(IPackagingDataProviderExtension), provider);
            else
                DataProvider.Instance.SetExtension(typeof(IPackagingDataProviderExtension), provider); //DB:ok
            return builder;
        }
    }

    public interface IPackagingDataProviderExtension : IDataProviderExtension
    {
        Task<IEnumerable<ComponentInfo>> LoadInstalledComponentsAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<IEnumerable<Package>> LoadInstalledPackagesAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task SavePackageAsync(Package package, CancellationToken cancellationToken = default(CancellationToken));
        Task UpdatePackageAsync(Package package, CancellationToken cancellationToken = default(CancellationToken));
        Task<bool> IsPackageExistAsync(string componentId, PackageType packageType, Version version, CancellationToken cancellationToken = default(CancellationToken));
        Task DeletePackageAsync(Package package, CancellationToken cancellationToken = default(CancellationToken));
        Task DeleteAllPackagesAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task LoadManifestAsync(Package package, CancellationToken cancellationToken = default(CancellationToken));
    }
}
