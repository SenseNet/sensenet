using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    public class PatchManager
    {
        private static Version NullVersion = new Version(0, 0);

        /* =========================================================================================== */

        private PatchExecutionContext _context;
        
        public PatchManager(RepositoryStartSettings settings, Action<PatchExecutionLogRecord> logCallback)
        {
            _context = new PatchExecutionContext(settings, logCallback);
        }

        internal PatchManager(PatchExecutionContext context)
        {
            _context = context;
        }

        /* =========================================================================================== */


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
            ExecutePatches(GetExecutablePatches(candidates, installedComponents, context, out _), context);
        }

        public IEnumerable<ISnPatch> GetExecutablePatches(IEnumerable<ISnPatch> candidates, PatchExecutionContext context)
        {
            return GetExecutablePatches(candidates, LoadInstalledComponents(), context, out _);
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
                .OrderBy(x => x.ComponentId)
                .ToArray();
            var installerGroups = installers.GroupBy(x => x.ComponentId);
            var duplicates = installerGroups.Where(x => x.Count() > 1).Select(x => x.Key).ToArray();
            if (duplicates.Length > 0)
            {
                // Duplicates are not allowed
                context.Errors = duplicates
                    .Select(id => new PatchExecutionError(PatchExecutionErrorType.DuplicatedInstaller,
                        installers.Where(inst => inst.ComponentId == id).ToArray(),
                        "There is a duplicated installer for the component " + id))
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
            var inputList = installers.Union(orderedSnPatches).ToList();
            var outputList = new List<ISnPatch>(); // patches in right order to execute
            var installed = new List<SnComponentDescriptor>(installedComponents); // all simulated components.
            var currentlyManaged = new List<ISnPatch>(); // temporary list in one iteration.
            while (true)
            {
                foreach (var item in inputList)
                {
                    if (CheckPrerequisites(item, installed, out var skipExecution))
                    {
                        currentlyManaged.Add(item);
                        outputList.Add(item);

                        // The check SnPatch needs to precede the ComponentInstaller
                        // because the SnPatch IS A ComponentInstaller
                        if (item is SnPatch snPatch)
                        {
                            // Modify version and dependencies of the installed components.
                            var patchedComponent = installed.Single(x => x.ComponentId == snPatch.ComponentId);
                            patchedComponent.Version = (Version)snPatch.Version.Clone();
                            patchedComponent.Dependencies = snPatch.Dependencies?.ToArray();
                        }
                        else if (item is ComponentInstaller installer)
                        {
                            // Add to installed components.
                            installed.Add(new SnComponentDescriptor(installer.ComponentId, installer.Version,
                                installer.Description, installer.Dependencies?.ToArray()));
                        }
                        else
                        {
                            throw new SnNotSupportedException();
                        }
                    }
                    else
                    {
                        if (skipExecution)
                            currentlyManaged.Add(item);
                    }
                }

                // Remove currently managed items from the to-do list.
                inputList = inputList.Except(currentlyManaged).ToList();

                // Exit if all items are managed.
                if (inputList.Count == 0)
                    break;

                // Exit if there is no changes, avoid the infinite loop.
                if (currentlyManaged.Count == 0)
                    break;
                currentlyManaged.Clear();
            }

            // If exited but there is any remaining item, generate error(s).
            if (inputList.Count > 0)
            {
                context.Errors = inputList
                    .Select(patch => RecognizeDiscoveryProblem(patch, installedComponents))
                    .ToArray();

                // Any installation is skipped.
                componentsAfter = installedComponents.ToArray();
                return new ISnPatch[0];
            }

            componentsAfter = installed.ToArray();

            return outputList;
        }

        private PatchExecutionError RecognizeDiscoveryProblem(ISnPatch patch, SnComponentDescriptor[] installedComponents)
        {
            if (patch is SnPatch snPatch)
            {
                if (!installedComponents.Any(comp => comp.ComponentId == snPatch.ComponentId &&
                                                     comp.Version < patch.Version &&
                                                     snPatch.Boundary.IsInInterval(comp.Version)))
                    return new PatchExecutionError(PatchExecutionErrorType.MissingVersion, patch,
                        "Cannot execute the patch " + patch);
            }

            return new PatchExecutionError(PatchExecutionErrorType.CannotInstall, patch,
                "Cannot execute the patch " + patch);
        }

        /// <summary>
        /// Returns true if the given <see cref="ISnPatch"/> is installable.
        /// </summary>
        private bool CheckPrerequisites(ISnPatch patch, List<SnComponentDescriptor> installed, out bool skipExecution)
        {
            if (patch is SnPatch snPatch)
                return CheckPrerequisites(snPatch, installed, out skipExecution);
            if (patch is ComponentInstaller installer)
                return CheckPrerequisites(installer, installed, out skipExecution);
            throw new SnNotSupportedException();
        }
        /// <summary>
        /// Returns true if the given <see cref="ComponentInstaller"/> is installable.
        /// </summary>
        private bool CheckPrerequisites(ComponentInstaller installer, List<SnComponentDescriptor> installed,
            out bool skipExecution)
        {
            skipExecution = false;
            // Installable if the dependent components exist.
            return CheckDependencies(installer, installed);
        }
        /// <summary>
        /// Returns true if the given <see cref="SnPatch"/> is installable.
        /// </summary>
        private bool CheckPrerequisites(SnPatch snPatch, List<SnComponentDescriptor> installed,
            out bool skipExecution)
        {
            skipExecution = false;

            // Search the installed component.
            var target = installed.FirstOrDefault(x => x.ComponentId == snPatch.ComponentId);

            // Not executable if not installed.
            if (target == null)
                return false;

            // Not executable but skipped if the target version is greater than the patch's version.
            if (target.Version >= snPatch.Version)
            {
                skipExecution = true;
                return false;
            }

            // Not executable if the installed component version is not in the expected boundary.
            if (!snPatch.Boundary.IsInInterval(target.Version))
                return false;

            // Executable if the dependent components exist.
            return CheckDependencies(snPatch, installed);
        }
        private bool CheckDependencies(ISnPatch patch, List<SnComponentDescriptor> installed)
        {
            // All right if there is no any dependency.
            if (patch.Dependencies == null)
                return true;
            var deps = patch.Dependencies.ToArray();
            if (deps.Length == 0)
                return true;

            // Self-dependency is forbidden
            if (deps.Any(dep => dep.Id == patch.ComponentId))
                return false;

            // Not installable if there is any dependency but installed nothing.
            if (installed.Count == 0)
                return false;

            // Installable if all dependencies exist.
            return deps.All(dep =>
                installed.Any(i => i.ComponentId == dep.Id && dep.Boundary.IsInInterval(i.Version)));
        }

        private void ExecutePatches(IEnumerable<ISnPatch> patches, PatchExecutionContext context)
        {
            foreach (var patch in patches)
            {
                var manifest = Manifest.Create(patch);

                // Write an "unfinished" record
                PackageManager.SaveInitialPackage(manifest);

                // Log after save: the execution is in started state when the callback called
                // so the callback can see the real state in the database.
                context.LogCallback(new PatchExecutionLogRecord(PatchExecutionEventType.ExecutionStart, patch));

                // PATCH EXECUTION
                context.CurrentPatch = patch;
                var successful = false;
                Exception executionError = null;
                try
                {
                    patch.Execute?.Invoke(context);
                    successful = true;
                }
                catch (Exception e)
                {
                    executionError = e;
                }

                // Save the execution result
                PackageManager.SavePackage(manifest, null, successful, executionError);
                // Log after save: the execution is in completed database state when the callback called.
                context.LogCallback(new PatchExecutionLogRecord(PatchExecutionEventType.ExecutionFinished, patch,
                    $"{(successful ? ExecutionResult.Successful : ExecutionResult.Faulty)}"));

            }
        }

        /* ================================================================================= EXPERIMENTAL */

        private IEnumerable<ISnPatch> _executablePatches;

        public void ExecutePatchesBeforeStart()
        {
            var candidates = CollectPatches();
            var executables = GetExecutablePatches(candidates, _context);

            //UNDONE: Execute patches onBefore is not implemented
            //ExecutePatchesOnBefore(executables, _context);

            _executablePatches = executables;
        }
        public void ExecutePatchesAfterStart()
        {
            ExecutePatches(_executablePatches, _context);
        }

        private ISnPatch[] CollectPatches()
        {
            var candidates2 = Providers.Instance
                .Components
                .Cast<ISnComponent>()
                .SelectMany(component =>
                {
                    var builder = new PatchBuilder(component);
                    component.AddPatches(builder);
                    return builder.GetPatches();
                })
                .ToArray();

            return candidates2;
        }
    }
}
