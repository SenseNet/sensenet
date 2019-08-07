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
            DataStore.DataProvider.SetExtension(typeof(IPackagingDataProviderExtension), provider);
            return builder;
        }
    }

    public interface IPackagingDataProviderExtension : IDataProviderExtension
    {
        Task<IEnumerable<ComponentInfo>> LoadInstalledComponentsAsync(CancellationToken cancellationToken);
        Task<IEnumerable<Package>> LoadInstalledPackagesAsync(CancellationToken cancellationToken);
        Task SavePackageAsync(Package package, CancellationToken cancellationToken);
        Task UpdatePackageAsync(Package package, CancellationToken cancellationToken);
        Task<bool> IsPackageExistAsync(string componentId, PackageType packageType, Version version, CancellationToken cancellationToken);
        Task DeletePackageAsync(Package package, CancellationToken cancellationToken);
        Task DeleteAllPackagesAsync(CancellationToken cancellationToken);
        Task LoadManifestAsync(Package package, CancellationToken cancellationToken);
    }
}
