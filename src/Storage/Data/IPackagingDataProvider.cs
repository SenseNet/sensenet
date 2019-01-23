using System;
using System.Collections.Generic;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Storage.Data
{
    public static class Extensions
    {
        public static IRepositoryBuilder UsePackagingDataProviderExtension(this IRepositoryBuilder builder, IPackagingDataProviderExtension provider)
        {
            DataProvider.Instance.SetExtension(typeof(IPackagingDataProviderExtension), provider);
            return builder;
        }
    }

    public interface IPackagingDataProviderExtension : IDataProviderExtension
    {
        IEnumerable<ComponentInfo> LoadInstalledComponents();
        IEnumerable<Package> LoadInstalledPackages();
        void SavePackage(Package package);
        void UpdatePackage(Package package);
        bool IsPackageExist(string componentId, PackageType packageType, Version version);
        void DeletePackage(Package package);
        void DeleteAllPackages();
        void LoadManifest(Package package);
    }
}
