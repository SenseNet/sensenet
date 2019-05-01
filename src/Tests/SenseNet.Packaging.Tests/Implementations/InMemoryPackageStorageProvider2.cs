using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tests.Implementations2;

namespace SenseNet.Packaging.Tests.Implementations
{
    public class InMemoryPackageStorageProvider2 : IPackagingDataProviderExtension
    {
        public DataCollection<PackageDoc> GetPackages()
        {
            return ((InMemoryDataProvider2)DataStore.DataProvider).DB.GetCollection<PackageDoc>();
        }

        /* ================================================================================================= IPackageStorageProvider */

        public IEnumerable<ComponentInfo> LoadInstalledComponents()
        {
            var nullVersion = new Version(0, 0);
            var componentInfos = new Dictionary<string, ComponentInfo>();
            foreach (var package in GetPackages()
                .Where(p => p.PackageType == PackageType.Install
                    && p.ExecutionResult == ExecutionResult.Successful))
            {
                var componentId = package.ComponentId;
                if (!componentInfos.TryGetValue(componentId, out var component))
                {
                    component = new ComponentInfo
                    {
                        ComponentId = package.ComponentId,
                        Version = package.ComponentVersion,
                        AcceptableVersion = package.ComponentVersion,
                        Description = package.Description
                    };
                    componentInfos.Add(componentId, component);
                }

                if (package.ComponentVersion > (component.AcceptableVersion ?? nullVersion))
                    component.AcceptableVersion = package.ComponentVersion;
            }

            foreach (var package in GetPackages()
                .Where(p => (p.PackageType == PackageType.Install || p.PackageType == PackageType.Patch)))
            {
                var componentId = package.ComponentId;
                if (componentInfos.TryGetValue(componentId, out var component))
                {
                    if ((package.ComponentVersion > (component.AcceptableVersion ?? nullVersion))
                        && package.ExecutionResult == ExecutionResult.Successful)
                        component.AcceptableVersion = package.ComponentVersion;
                    if (package.ComponentVersion > (component.Version ?? nullVersion))
                        component.Version = package.ComponentVersion;
                }
            }
            return componentInfos.Values.ToArray();
        }

        public IEnumerable<Package> LoadInstalledPackages()
        {
            return GetPackages()
                //.Where(p => p.ExecutionResult != ExecutionResult.Unfinished)
                .Select(p => new Package
                {
                    Id = p.Id,
                    Description = p.Description,
                    ComponentId = p.ComponentId,
                    PackageType = p.PackageType,
                    ReleaseDate = p.ReleaseDate,
                    ExecutionDate = p.ExecutionDate,
                    ExecutionResult = p.ExecutionResult,
                    ComponentVersion = p.ComponentVersion,
                    ExecutionError = p.ExecutionError,
                    //Manifest = p.Manifest, // Not loaded to increase performance.
                })
                .ToArray();
        }

        public void SavePackage(Package package)
        {
            if (package.Id > 0)
                throw new InvalidOperationException("Only new package can be saved.");
            var collection = GetPackages();
            var newId = collection.Count == 0 ? 1 : collection.Max(t => t.Id) + 1;

            var packageDoc = new PackageDoc
            {
                Id = newId,
                Description = package.Description,
                ComponentId = package.ComponentId,
                PackageType = package.PackageType,
                ReleaseDate = package.ReleaseDate,
                ExecutionDate = package.ExecutionDate,
                ExecutionResult = package.ExecutionResult,
                ComponentVersion = package.ComponentVersion,
                ExecutionError = package.ExecutionError,
                Manifest = package.Manifest,
            };

            package.Id = newId;
            collection.Insert(packageDoc);

            RepositoryVersionInfo.Reset();
        }

        public void UpdatePackage(Package package)
        {
            var existingDoc = GetPackages().FirstOrDefault(p => p.Id == package.Id);
            if (existingDoc == null)
                throw new InvalidOperationException("Package does not exist. Id: " + package.Id);
            existingDoc.Id = package.Id;
            existingDoc.Description = package.Description;
            existingDoc.ComponentId = package.ComponentId;
            existingDoc.PackageType = package.PackageType;
            existingDoc.ReleaseDate = package.ReleaseDate;
            existingDoc.ExecutionDate = package.ExecutionDate;
            existingDoc.ExecutionResult = package.ExecutionResult;
            existingDoc.ComponentVersion = package.ComponentVersion;
            existingDoc.ExecutionError = package.ExecutionError;
            // Manifest is not updated
        }

        public bool IsPackageExist(string componentId, PackageType packageType, Version version)
        {
            throw new NotImplementedException();
        }

        public void DeletePackage(Package package)
        {
            if (package.Id < 1)
                throw new ApplicationException("Cannot delete unsaved package");
            var storedPackage = GetPackages().FirstOrDefault(p => p.Id == package.Id);
            if (storedPackage != null)
                GetPackages().Remove(storedPackage);
        }

        public void DeleteAllPackages()
        {
            GetPackages().Clear();
        }

        public void LoadManifest(Package package)
        {
            package.Manifest = GetPackages().FirstOrDefault(p => p.Id == package.Id)?.Manifest;
        }

    }
}
