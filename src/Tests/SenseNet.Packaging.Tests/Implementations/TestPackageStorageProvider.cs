using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Packaging.Tests.Implementations
{
    //UNDONE:DB: CancellationToken is not used in this class.
    public class TestPackageStorageProvider : IPackagingDataProviderExtension
    {
        private int _id;

        private List<Package> Storage { get; } = new List<Package>();

        private Package ClonePackage(Package source, bool withManifest)
        {
            var target = new Package();
            UpdatePackage(source, target, withManifest);
            return target;
        }
        private void UpdatePackage(Package source, Package target, bool withManifest)
        {
            target.Id = source.Id;
            target.Description = source.Description;
            target.ComponentId = source.ComponentId;
            target.PackageType = source.PackageType;
            target.ReleaseDate = source.ReleaseDate;
            target.ExecutionDate = source.ExecutionDate;
            target.ExecutionResult = source.ExecutionResult;
            target.ComponentVersion = source.ComponentVersion;
            target.ExecutionError = source.ExecutionError;
            if (withManifest)
                target.Manifest = source.Manifest;
        }

        /* ================================================================================================= IPackageStorageProvider */

        public Task<IEnumerable<ComponentInfo>> LoadInstalledComponentsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var nullVersion = new Version(0, 0);
            var componentInfos = new Dictionary<string, ComponentInfo>();
            foreach (var package in Storage
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

            foreach (var package in Storage
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

            return Task.FromResult(componentInfos.Values.AsEnumerable());
        }

        public Task<IEnumerable<Package>> LoadInstalledPackagesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(Storage
                //.Where(p => p.ExecutionResult != ExecutionResult.Unfinished)
                .Select(p => ClonePackage(p, false)));
        }

        public Task SavePackageAsync(Package package, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (package.Id > 0)
                throw new InvalidOperationException("Only new package can be saved.");

            package.Id = ++_id;
            Storage.Add(ClonePackage(package, true));

            RepositoryVersionInfo.Reset();

            return Task.CompletedTask;
        }

        public Task UpdatePackageAsync(Package package, CancellationToken cancellationToken = default(CancellationToken))
        {
            var existing = Storage.FirstOrDefault(p => p.Id == package.Id);
            if (existing == null)
                throw new InvalidOperationException("Package does not exist. Id: " + package.Id);
            UpdatePackage(package, existing, false);

            return Task.CompletedTask;
        }

        public Task<bool> IsPackageExistAsync(string componentId, PackageType packageType, Version version, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task DeletePackageAsync(Package package, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (package.Id < 1)
                throw new ApplicationException("Cannot delete unsaved package");
            var storedPackage = Storage.FirstOrDefault(p => p.Id == package.Id);
            if (storedPackage != null)
                Storage.Remove(storedPackage);

            return Task.CompletedTask;
        }

        public Task DeleteAllPackagesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Storage.Clear();
            return Task.CompletedTask;
        }

        public Task LoadManifestAsync(Package package, CancellationToken cancellationToken = default(CancellationToken))
        {
            package.Manifest = Storage.FirstOrDefault(p => p.Id == package.Id)?.Manifest;
            return Task.CompletedTask;
        }

        // ================================================================================================= Test tools

        public int GetRecordCount()
        {
            return Storage.Count;
        }

    }
}
