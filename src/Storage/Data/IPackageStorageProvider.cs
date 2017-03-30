using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Storage.Data
{
    public interface IDataProcedureFactory
    {
        IDataProcedure CreateProcedure();
    }

    public interface IPackageStorageProvider
    {
        IDataProcedureFactory DataProcedureFactory { get; set; }

        IEnumerable<ApplicationInfo> LoadInstalledApplications();
        IEnumerable<Package> LoadInstalledPackages();
        void SavePackage(Package package);
        void UpdatePackage(Package package);
        bool IsPackageExist(string appId, PackageLevel packageLevel, Version version);
        void DeletePackage(Package package);
        void DeletePackagesExceptFirst();
    }
}
