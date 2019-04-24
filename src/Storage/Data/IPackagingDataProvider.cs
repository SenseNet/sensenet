using System;
using System.Collections.Generic;
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
