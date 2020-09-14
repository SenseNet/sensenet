using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Packaging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    public class PatchManager
    {
        private static Version NullVersion = new Version(0, 0);

        internal static Package CreatePackage(ISnPatch patch)
        {
            var package = new Package
            {
                ComponentId = patch.ComponentId,
                ComponentVersion = patch.Version,
                Description = patch.Description,
                ReleaseDate = patch.ReleaseDate,
                PackageType = patch.Type,
                ExecutionDate = patch.ExecutionDate,
                ExecutionResult = patch.ExecutionResult,
                ExecutionError = patch.ExecutionError
            };

            Dependency[] dependencies;
            if (patch is SnPatch snPatch)
            {
                var selfDependency = new Dependency { Id = snPatch.ComponentId, Boundary = snPatch.Boundary };
                if (patch.Dependencies == null)
                {
                    dependencies = new[] { selfDependency };
                }
                else
                {
                    var list = patch.Dependencies.ToList();
                    list.Insert(0, selfDependency);
                    dependencies = list.ToArray();
                }
            }
            else
            {
                dependencies = patch.Dependencies.ToArray();
            }

            package.Manifest = Manifest.Create(package, dependencies, false).ToXmlString();

            return package;
        }
        internal static ISnPatch CreatePatch(Package package)
        {
            if (package.PackageType == PackageType.Tool)
                return null;
            if (package.PackageType == PackageType.Install)
                return CreateInstaller(package);
            if (package.PackageType == PackageType.Patch)
                return CreateSnPatch(package);
            throw new ArgumentOutOfRangeException("Unknown PackageType: " + package.PackageType);
        }
        private static ComponentInstaller CreateInstaller(Package package)
        {
            var xml = new XmlDocument();
            xml.LoadXml(package.Manifest);
            var manifest = Manifest.Parse(xml);

            var dependencies = manifest.Dependencies.ToList();

            return new ComponentInstaller
            {
                Id = package.Id,
                ComponentId = package.ComponentId,
                Description = package.Description,
                ReleaseDate = package.ReleaseDate,
                Version = package.ComponentVersion,
                Dependencies = dependencies,
                ExecutionDate = package.ExecutionDate,
                ExecutionResult = package.ExecutionResult,
                ExecutionError = package.ExecutionError
            };
        }
        private static SnPatch CreateSnPatch(Package package)
        {
            var xml = new XmlDocument();
            xml.LoadXml(package.Manifest);
            var manifest = Manifest.Parse(xml);

            var dependencies = manifest.Dependencies.ToList();
            var selfDependency = dependencies.First(x => x.Id == package.ComponentId);
            dependencies.Remove(selfDependency);

            return new SnPatch
            {
                Id = package.Id,
                ComponentId = package.ComponentId,
                Description = package.Description,
                ReleaseDate = package.ReleaseDate,
                Version = package.ComponentVersion,
                Boundary = selfDependency.Boundary,
                Dependencies = dependencies,
                ExecutionDate = package.ExecutionDate,
                ExecutionResult = package.ExecutionResult,
                ExecutionError = package.ExecutionError
            };
        }

        /* =========================================================================================== */

        public void ExecuteRelevantPatches(IEnumerable<ISnPatch> candidates,
            PatchExecutionContext context)
        {
            var installedComponents = PackageManager.Storage.LoadInstalledComponentsAsync(CancellationToken.None)
                .ConfigureAwait(false).GetAwaiter().GetResult().ToArray();
            ExecuteRelevantPatches(candidates, installedComponents, context);
        }

        public void ExecuteRelevantPatches(IEnumerable<ISnPatch> candidates,
            ComponentInfo[] installedComponents, PatchExecutionContext context)
        {
            ExecutePatches(GetExecutablePatches(candidates, installedComponents, context, out var after), context);
        }

        public IEnumerable<ISnPatch> GetExecutablePatches(IEnumerable<ISnPatch> candidates, PatchExecutionContext context)
        {
            var installedComponents = PackageManager.Storage.LoadInstalledComponentsAsync(CancellationToken.None)
                .ConfigureAwait(false).GetAwaiter().GetResult().ToArray();
            return GetExecutablePatches(candidates, installedComponents, context, out var after);
        }

        public IEnumerable<ISnPatch> GetExecutablePatches(IEnumerable<ISnPatch> candidates,
            ComponentInfo[] installedComponents, PatchExecutionContext context, out ComponentInfo[] componentsAfter)
        {
            var patches = candidates.ToArray();

            var installedIds = installedComponents
                .Where(x => x.AcceptableVersion != null && x.AcceptableVersion > NullVersion)
                .Select(x=>x.ComponentId)
                .ToArray();

            var installers = patches
                .Where(x => x.Type == PackageType.Install && !installedIds.Contains(x.ComponentId))
                .ToArray();
            var installerGroups = installers.GroupBy(x => x.ComponentId);
            var duplicates = installerGroups.Where(x => x.Count() > 1).Select(x => x.Key).ToArray();
            if (duplicates.Length > 0)
            {
                context.Errors = duplicates
                    .Select(x => new PatchExecutionError(PatchExecutionErrorType.DuplicatedInstaller,
                        "There is a duplicated installer for the component " + x))
                    .ToArray();

                componentsAfter = installedComponents.ToArray();
                return new ISnPatch[0];
            }

            //UNDONE: Sort installers by dependencies. Be carefully with circular references

            componentsAfter = installedComponents.Union(installers.Select(x => new ComponentInfo
            {
                ComponentId = x.ComponentId,
                Version = x.Version,
                AcceptableVersion = x.Version,
                Description = x.Description
            })).ToArray();

            return installers;

            //UNDONE: Don't skip' SnPatches.
        }

        private void ExecutePatches(IEnumerable<ISnPatch> patches, PatchExecutionContext context)
        {
            throw new NotImplementedException();
        }

    }
}
