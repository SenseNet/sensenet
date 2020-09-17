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

        public void ExecuteRelevantPatches(IEnumerable<ISnPatch> candidates, PatchExecutionContext context)
        {
            ExecuteRelevantPatches(candidates, LoadInstalledComponents(), context);
        }

        public void ExecuteRelevantPatches(IEnumerable<ISnPatch> candidates,
            SnComponentDescriptor[] installedComponents, PatchExecutionContext context)
        {
            ExecutePatches(GetExecutablePatches(candidates, installedComponents, context, out var after), context);
        }

        public IEnumerable<ISnPatch> GetExecutablePatches(IEnumerable<ISnPatch> candidates, PatchExecutionContext context)
        {
            return GetExecutablePatches(candidates, LoadInstalledComponents(), context, out var after);
        }

        private SnComponentDescriptor[] LoadInstalledComponents()
        {
            return PackageManager.Storage.LoadInstalledComponentsAsync(CancellationToken.None)
                .ConfigureAwait(false).GetAwaiter().GetResult()
                .Select(x => new SnComponentDescriptor(x)).ToArray();
        }

        public IEnumerable<ISnPatch> GetExecutablePatches(IEnumerable<ISnPatch> candidates,
            SnComponentDescriptor[] installedComponents, PatchExecutionContext context, out SnComponentDescriptor[] componentsAfter)
        {
            var patches = candidates.ToArray();

            var installedIds = installedComponents
                .Where(x => x.Version != null && x.Version > NullVersion)
                .Select(x=>x.ComponentId)
                .ToArray();

            var installers = patches
                .Where(x => x.Type == PackageType.Install && !installedIds.Contains(x.ComponentId))
                .ToArray();
            var installerGroups = installers.GroupBy(x => x.ComponentId);
            var duplicates = installerGroups.Where(x => x.Count() > 1).Select(x => x.Key).ToArray();
            if (duplicates.Length > 0)
            {
                // Duplicates are not allowed
                context.Errors = duplicates
                    .Select(x => new PatchExecutionError(PatchExecutionErrorType.DuplicatedInstaller,
                        "There is a duplicated installer for the component " + x))
                    .ToArray();

                // Set unchanged list as output
                componentsAfter = installedComponents.ToArray();
                return new ISnPatch[0];
            }

            // Order patches by componentId and versions
            var orderedSnPatches = patches
                .Where(x => x.Type == PackageType.Patch)
                .OrderBy(x => x.ComponentId).ThenBy(x => x.Version)
                .ToArray();

            // ------------------------------------------------------------ sorting by dependencies
            var toInstall = installers.Union(orderedSnPatches).ToList(); // to-do list
            var sortedPatches = new List<ISnPatch>(); // patches in right order (output)
            var installed = new List<SnComponentDescriptor>(installedComponents); // all installed and simulated items.
            var currentlyInstalled = new List<ISnPatch>(); // temporary list.
            while (true)
            {
                foreach (var item in toInstall)
                {
                    if (item.Type == PackageType.Install)
                    {
                        var installer = (ComponentInstaller) item;
                        if (CheckPrerequisites(installer, installed))
                        {
                            currentlyInstalled.Add(installer);
                            sortedPatches.Add(installer);
                            installed.Add(new SnComponentDescriptor(installer.ComponentId, installer.Version,
                                installer.Description, installer.Dependencies?.ToArray()));
                        }
                    }
                    else if (item.Type == PackageType.Patch)
                    {
                        var snPatch = (SnPatch) item;
                        if (CheckPrerequisites(snPatch, installed))
                        {
                            currentlyInstalled.Add(snPatch);
                            sortedPatches.Add(snPatch);
                            var patchedComponent = installed.First(x => x.ComponentId == snPatch.ComponentId);
                            patchedComponent.Version = (Version) snPatch.Version.Clone();
                            patchedComponent.Dependencies = snPatch.Dependencies?.ToArray();
                        }
                    }
                    else
                    {
                        // Do nothing
                        context.LogMessage($"Patch is skipped: {item.ComponentId} ({item.Type})");
                    }
                }

                // Remove currently installed items from the to-do list.
                toInstall = toInstall.Except(currentlyInstalled).ToList();

                // Exit if all installers are in the sortedInstallers.
                if (toInstall.Count == 0)
                    break;
                
                // Exit if there is no changes, avoid the infinite loop.
                if (currentlyInstalled.Count == 0)
                    break;
                currentlyInstalled.Clear();
            }

            // If exited but there is any remaining item, generate error(s).
            if (toInstall.Count > 0)
            {
                //UNDONE: PACKAGING Recognize circular dependencies.
                context.Errors = toInstall
                    .Select(x => new PatchExecutionError(PatchExecutionErrorType.CannotInstall,
                        "Cannot execute the installer " + x))
                    .ToArray();

                componentsAfter = installedComponents.ToArray();
                return new ISnPatch[0];
            }

            componentsAfter = installed.ToArray();

            return sortedPatches;
        }

        /// <summary>
        /// Returns true if the given <see cref="ComponentInstaller"/> is installable.
        /// </summary>
        private bool CheckPrerequisites(ComponentInstaller installer, List<SnComponentDescriptor> installed)
        {
            // Installable if the dependent components exist.
            return CheckDependencies(installer.Dependencies, installed);
        }
        /// <summary>
        /// Returns true if the given <see cref="SnPatch"/> is installable.
        /// </summary>
        private bool CheckPrerequisites(SnPatch snPatch, List<SnComponentDescriptor> installed)
        {
            // Search the installed component.
            var target = installed.FirstOrDefault(x => x.ComponentId == snPatch.ComponentId);

            // Not executable if not installed.
            if (target == null)
                return false;

            // Not executable if the installed component version is not in the expected boundary.
            if(!snPatch.Boundary.IsInInterval(target.Version))
                return false;

            // Executable if the dependent components exist.
            return CheckDependencies(snPatch.Dependencies, installed);
        }
        private bool CheckDependencies(IEnumerable<Dependency> dependencies, List<SnComponentDescriptor> installed)
        {
            // All right if there is no any dependency.
            if (dependencies == null)
                return true;
            var deps = dependencies.ToArray();
            if (deps.Length == 0)
                return true;

            // Not installable if there is any dependency but installed nothing.
            if (installed.Count == 0)
                return false;

            // Installable if all dependencies exist.
            return deps.All(dep =>
                installed.Any(i => i.ComponentId == dep.Id && dep.Boundary.IsInInterval(i.Version)));
        }

        private void ExecutePatches(IEnumerable<ISnPatch> patches, PatchExecutionContext context)
        {
            throw new NotImplementedException();
        }

    }
}
