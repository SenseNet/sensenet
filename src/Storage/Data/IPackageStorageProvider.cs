using System;
using System.Collections.Generic;

namespace SenseNet.ContentRepository.Storage.Data
{
    public interface IDataProcedureFactory
    {
        IDataProcedure CreateProcedure();
    }

    public interface IPackageStorageProvider
    {
        IDataProcedureFactory DataProcedureFactory { get; set; }

        IEnumerable<ComponentInfo> LoadInstalledComponents();
        IEnumerable<Package> LoadInstalledPackages();
        void SavePackage(Package package);
        void UpdatePackage(Package package);
        bool IsPackageExist(string componentId, PackageType packageType, Version version);
        void DeletePackage(Package package);
        void DeletePackagesExceptFirst(); //UNDONE: DeletePackagesExceptFirst --> DeleteAllPackages
        void LoadManifest(Package package);
    }
}
