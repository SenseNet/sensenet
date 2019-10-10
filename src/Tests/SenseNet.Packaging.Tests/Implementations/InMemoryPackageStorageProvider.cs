using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Tasks = System.Threading.Tasks;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.InMemory;
using SenseNet.Tests.Implementations;

namespace SenseNet.Packaging.Tests.Implementations
{
    /// <summary> 
    /// This is an in-memory implementation of the <see cref="IPackagingDataProviderExtension"/> interface.
    /// It requires the main data provider to be an <see cref="InMemoryDataProvider"/>.
    /// </summary>
    public class InMemoryPackageStorageProvider : IPackagingDataProviderExtension
    {
        public DataCollection<PackageDoc> GetPackages()
        {
            return ((InMemoryDataProvider)DataStore.DataProvider).DB.GetCollection<PackageDoc>();
        }

        /* ================================================================================================= IPackageStorageProvider */

        public Tasks.Task<IEnumerable<ComponentInfo>> LoadInstalledComponentsAsync(CancellationToken cancellationToken)
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
            return Tasks.Task.FromResult(componentInfos.Values.AsEnumerable());
        }

        public Tasks.Task<IEnumerable<Package>> LoadInstalledPackagesAsync(CancellationToken cancellationToken)
        {
            return Tasks.Task.FromResult(GetPackages()
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
                .ToArray().AsEnumerable());
        }

        public Tasks.Task SavePackageAsync(Package package, CancellationToken cancellationToken)
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

            return Tasks.Task.CompletedTask;
        }

        public Tasks.Task UpdatePackageAsync(Package package, CancellationToken cancellationToken)
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

            return Tasks.Task.CompletedTask;
        }

        public Tasks.Task<bool> IsPackageExistAsync(string componentId, PackageType packageType, Version version, 
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Tasks.Task DeletePackageAsync(Package package, CancellationToken cancellationToken)
        {
            if (package.Id < 1)
                throw new ApplicationException("Cannot delete unsaved package");
            var storedPackage = GetPackages().FirstOrDefault(p => p.Id == package.Id);
            if (storedPackage != null)
                GetPackages().Remove(storedPackage);

            return Tasks.Task.CompletedTask;
        }

        public Tasks.Task DeleteAllPackagesAsync(CancellationToken cancellationToken)
        {
            GetPackages().Clear();
            return Tasks.Task.CompletedTask;
        }

        public Tasks.Task LoadManifestAsync(Package package, CancellationToken cancellationToken)
        {
            package.Manifest = GetPackages().FirstOrDefault(p => p.Id == package.Id)?.Manifest;
            return Tasks.Task.CompletedTask;
        }
    }
}
