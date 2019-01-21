using System;
using System.Collections.Generic;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Storage.Data
{
    public static class Extensions
    {
        public static IRepositoryBuilder UsePackagingDataProvider(this IRepositoryBuilder builder, IPackagingDataProvider provider)
        {
            DataProvider.Instance().SetProvider(typeof(IPackagingDataProvider), provider);
            return builder;
        }
    }

    //UNDONE: Write to new file or rename.
    public interface IPackagingDataProvider : IDataProvider
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
