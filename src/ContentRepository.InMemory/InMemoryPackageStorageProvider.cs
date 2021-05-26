using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.ContentRepository.InMemory
{
    /// <summary> 
    /// This is an in-memory implementation of the <see cref="IPackagingDataProviderExtension"/> interface.
    /// It requires the main data provider to be an <see cref="InMemoryDataProvider"/>.
    /// </summary>
    public class InMemoryPackageStorageProvider : IPackagingDataProviderExtension
    {
        public DataCollection<PackageDoc> GetPackages()
        {
            return ((InMemoryDataProvider)Providers.Instance.DataProvider).DB.GetCollection<PackageDoc>();
        }

        /* ================================================================================================= IPackageStorageProvider */

        public System.Threading.Tasks.Task<IEnumerable<ComponentInfo>> LoadInstalledComponentsAsync(CancellationToken cancellationToken)
        {
            var componentInfos = new Dictionary<string, ComponentInfo>();
            var descriptions = new Dictionary<string, string>();
            foreach (var package in GetPackages()
                .Where(p => (p.PackageType == PackageType.Install || p.PackageType == PackageType.Patch) 
                            && p.ExecutionResult == ExecutionResult.Successful)
                .OrderBy(x=>x.ComponentId).ThenBy(x=>x.ComponentVersion).ThenBy(x=>x.ExecutionDate))
            {
                var componentId = package.ComponentId;

                if (package.PackageType == PackageType.Install)
                    descriptions[componentId] = package.Description;

                componentInfos[componentId] = new ComponentInfo
                {
                    ComponentId = package.ComponentId,
                    Version = package.ComponentVersion,
                    Description = package.Description,
                    Manifest = package.Manifest,
                    ExecutionResult = package.ExecutionResult
                };
            }

            foreach (var item in descriptions)
                componentInfos[item.Key].Description = item.Value;

            return System.Threading.Tasks.Task.FromResult(componentInfos.Values.AsEnumerable());
        }

        public Task<IEnumerable<ComponentInfo>> LoadIncompleteComponentsAsync(CancellationToken cancellationToken)
        {
            var componentInfos = new Dictionary<string, ComponentInfo>();
            foreach (var package in GetPackages()
                .Where(p => (p.PackageType == PackageType.Install || p.PackageType == PackageType.Patch)
                            && p.ExecutionResult != ExecutionResult.Successful)
                .OrderBy(x => x.ComponentId).ThenBy(x => x.ComponentVersion).ThenBy(x => x.ExecutionDate))
            {
                var componentId = package.ComponentId;

                componentInfos[componentId] = new ComponentInfo
                {
                    ComponentId = package.ComponentId,
                    Version = package.ComponentVersion,
                    Description = package.Description,
                    Manifest = package.Manifest,
                    ExecutionResult = package.ExecutionResult
                };
            }

            return System.Threading.Tasks.Task.FromResult(componentInfos.Values.AsEnumerable());
        }

        public System.Threading.Tasks.Task<IEnumerable<Package>> LoadInstalledPackagesAsync(CancellationToken cancellationToken)
        {
            return System.Threading.Tasks.Task.FromResult(GetPackages()
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
                    Manifest = p.Manifest,
                })
                .ToArray().AsEnumerable());
        }

        public System.Threading.Tasks.Task SavePackageAsync(Package package, CancellationToken cancellationToken)
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

            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task UpdatePackageAsync(Package package, CancellationToken cancellationToken)
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

            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task<bool> IsPackageExistAsync(string componentId, PackageType packageType, Version version,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task DeletePackageAsync(Package package, CancellationToken cancellationToken)
        {
            if (package.Id < 1)
                throw new ApplicationException("Cannot delete unsaved package");
            var storedPackage = GetPackages().FirstOrDefault(p => p.Id == package.Id);
            if (storedPackage != null)
                GetPackages().Remove(storedPackage);

            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task DeleteAllPackagesAsync(CancellationToken cancellationToken)
        {
            GetPackages().Clear();
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task LoadManifestAsync(Package package, CancellationToken cancellationToken)
        {
            package.Manifest = GetPackages().FirstOrDefault(p => p.Id == package.Id)?.Manifest;
            return System.Threading.Tasks.Task.CompletedTask;
        }

        /* =============================================================================== Methods for Steps */

        public Dictionary<string, string> GetContentPathsWhereTheyAreAllowedChildren(List<string> names)
        {
            var db = ((InMemoryDataProvider) Providers.Instance.DataProvider).DB;
            var propTypeId = ActiveSchema.PropertyTypes["AllowedChildTypes"].Id;
            var rows = db.LongTextProperties
                .Where(x => x.PropertyTypeId == propTypeId && x.Value.Split(' ').Intersect(names).Any())
                .ToArray();

            var result = new Dictionary<string, string>();
            foreach (var row in rows)
            {
                var nodeId = db.Versions.FirstOrDefault(x => x.VersionId == row.VersionId)?.NodeId ?? 0;
                var path = db.Nodes.FirstOrDefault(x => x.NodeId == nodeId)?.Path;
                if (path != null)
                    result.Add(path, row.Value);
            }

            return result;
        }
    }
}
