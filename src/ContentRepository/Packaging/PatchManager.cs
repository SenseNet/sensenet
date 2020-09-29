using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;

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

        internal List<PatchExecutionError> Errors => _context.Errors;

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

        /* ======================================================================================= NEW */
        /* ---------------------------------------------------------------------------------- OnBefore */

        public void ExecutePatchesOnBeforeStart(bool isSimulation = false)
        {
            var candidates = CollectCandidates();
            var installed = LoadComponents();
            ExecuteOnBefore(candidates, installed, isSimulation);
            _context.ExecutablePatchesOnAfter = candidates;
            _context.CurrentlyInstalledComponents = installed;
        }
        internal void ExecuteOnBefore(List<ISnPatch> candidates, List<SnComponentDescriptor> installed, bool isSimulation)
        {
            var toExec = candidates.ToList(); // Copy
            var executed = new List<ISnPatch>();
            var isActive = true;
            while (true)
            {
                isActive = false;
                foreach (var patch in toExec)
                {
                    if (patch.ActionBeforeStart == null)
                        continue;

                    if (CheckPrerequisitesBefore(patch, candidates, installed, out var isIrrelevant))
                    {
                        if (!isSimulation)
                            WriteInitialStateToDb(patch); //UNDONE:PATCH: try-catch-rethrow
                        CreateInitialState(patch, installed);

                        try
                        {
                            Log(patch, PatchExecutionEventType.OnBeforeActionStarts);
                            if (!isSimulation)
                            {
                                patch.ActionBeforeStart(_context);
                                ModifyStateInDb(patch, ExecutionResult.SuccessfulBefore);
                            }
                            ModifyState(patch, installed, ExecutionResult.SuccessfulBefore);
                            Log(patch, PatchExecutionEventType.OnBeforeActionFinished);
                        }
                        catch (Exception e)
                        {
                            if (!isSimulation)
                                ModifyStateInDb(patch, ExecutionResult.FaultyBefore);
                            ModifyState(patch, installed, ExecutionResult.FaultyBefore);
                            Log(patch, PatchExecutionEventType.ExecutionErrorOnBefore);
                            Error(patch, PatchExecutionErrorType.ExecutionErrorOnBefore, e.Message);
                            candidates.Remove(patch);
                        }
                        finally
                        {
                            isActive = true;
                            executed.Add(patch);
                        }
                    }
                    else
                    {
                        if (isIrrelevant)
                        {
                            isActive = true;
                            executed.Add(patch);
                        }
                    }
                }

                foreach (var item in executed)
                    toExec.Remove(item);
                executed.Clear();

                if(toExec.Count == 0)
                    break;
                if (!isActive)
                    break;
            }
            if (toExec.Count > 0)
                RecognizeErrors(toExec, candidates, installed);
        }

        private bool CheckPrerequisitesBefore(ISnPatch patch, List<ISnPatch> candidates, List<SnComponentDescriptor> installed, out bool isIrrelevant)
        {
            var component = installed.FirstOrDefault(x => x.ComponentId == patch.ComponentId);
            isIrrelevant = false;
            if (patch is ComponentInstaller installer)
            {
                if (!ValidInstaller(installer)) { isIrrelevant = true; return false; }
                if (HasDuplicates(installer, candidates)) { return false; }
                if (!HaveCorrectDependencies(installer, installed)) { return false; }
                if (component == null) { return true; }
                if (component.Version != null) { isIrrelevant = true; return false; }
                if (component.FaultyAfterVersion != null) { isIrrelevant = true; return false; }
                if (component.FaultyBeforeVersion > patch.Version) { isIrrelevant = true; return false; }
                return true;
            }
            if (patch.Type == PackageType.Patch)
            {
                throw new NotImplementedException();
                //# If not Valid(snPatch)                                    Return false, isIrrelevant = true
                //# If not HaveCorrectDependencies(patch, currentComponents) Return false
                //# If component is null                                     Return false
                //# If component's SuccessfulVersion is null                 Return false
                //# If component's SuccessfulVersion >= patch.Version        Return false, isIrrelevant = true
                //# If component's FaultyAfterVersion >= patch.Version       Return false, isIrrelevant = true
                //# If component's FaultyBeforeVersion > patch.Version       Return false, isIrrelevant = true
                //# Return true
            }
            throw new NotSupportedException(
                $"Manage this patch is not supported. ComponentId: {patch.ComponentId}, " +
                $"Version: {patch.Version}, PackageType: {patch.Type}");
        }

        private bool ValidInstaller(ComponentInstaller installer)
        {
            return true;
        }


        /* ---------------------------------------------------------------------------------- OnAfter */

        public void ExecutePatchesOnAfterStart(bool isSimulation = false)
        {
            var candidates = _context.ExecutablePatchesOnAfter;
            var installed = _context.CurrentlyInstalledComponents;
            ExecuteOnAfter(candidates, installed, isSimulation);
            _context.ExecutablePatchesOnAfter = candidates;
            _context.CurrentlyInstalledComponents = installed;
        }
        internal void ExecuteOnAfter(List<ISnPatch> candidates, List<SnComponentDescriptor> installed, bool isSimulation)
        {
            var toExec = candidates; //UNDONE:PATCH refactor, remove and rename
            var executed = new List<ISnPatch>();
            var isActive = true;
            while (true)
            {
                isActive = false;
                foreach (var patch in toExec)
                {
                    if (CheckPrerequisitesAfter(patch, candidates, installed, out var isIrrelevant))
                    {
                        if (patch.ActionBeforeStart == null)
                        {
                            if (!isSimulation)
                                WriteInitialStateToDb(patch);
                            CreateInitialState(patch, installed);
                        }

                        try
                        {
                            Log(patch, PatchExecutionEventType.OnAfterActionStarts);
                            if (!isSimulation)
                            {
                                patch.Action?.Invoke(_context);
                                ModifyStateInDb(patch, ExecutionResult.Successful);
                            }
                            ModifyState(patch, installed, ExecutionResult.Successful);
                            Log(patch, PatchExecutionEventType.OnAfterActionFinished);
                        }
                        catch (Exception e)
                        {
                            if (!isSimulation)
                                ModifyStateInDb(patch, ExecutionResult.Faulty);
                            ModifyState(patch, installed, ExecutionResult.Faulty);
                            Log(patch, PatchExecutionEventType.ExecutionError);
                            Error(patch, PatchExecutionErrorType.ExecutionErrorOnAfter, e.Message);
                        }
                        finally
                        {
                            isActive = true;
                            executed.Add(patch);
                        }
                    }
                    else
                    {
                        if (isIrrelevant)
                        {
                            isActive = true;
                            executed.Add(patch);
                        }
                    }
                }
                foreach (var item in executed)
                    toExec.Remove(item);
                executed.Clear();

                if (toExec.Count == 0)
                    break;
                if (!isActive)
                    break;
            }
            if (toExec.Count > 0)
                RecognizeErrors(toExec, candidates, installed);
        }

        private bool CheckPrerequisitesAfter(ISnPatch patch, List<ISnPatch> candidates, List<SnComponentDescriptor> installed, out bool isIrrelevant)
        {
            var component = installed.FirstOrDefault(x => x.ComponentId == patch.ComponentId);
            isIrrelevant = false;
            if (patch is ComponentInstaller installer)
            {
                if (HasDuplicates(installer, candidates)) { return false; }
                if (!HaveCorrectDependencies(installer, installed)) { return false; }
                if (component == null) { return true; }
                if (component.Version != null) { isIrrelevant = true; return false; }
                if (component.FaultyBeforeVersion >= patch.Version) { isIrrelevant = true; return false; }
                if (component.FaultyAfterVersion > patch.Version) { isIrrelevant = true; return false; }
                return true;
            }
            if (patch.Type == PackageType.Patch)
            {
                throw new NotImplementedException();
                //# If not HaveCorrectDependencies(patch, currentComponents) Return false
                //# If component is null                                     Return false
                //# If component's SuccessfulVersion is null                 Return false
                //# If component's SuccessfulVersion >= patch.Version        Return false, isIrrelevant = true
                //# If component's FaultyBeforeVersion >= patch.Version      Return false, isIrrelevant = true
                //# If component's FaultyAfterVersion > patch.Version        Return false, isIrrelevant = true
                //# Return true
            }
            throw new NotSupportedException(
                $"Manage this patch is not supported. ComponentId: {patch.ComponentId}, " +
                $"Version: {patch.Version}, PackageType: {patch.Type}");
        }
        
        /* ---------------------------------------------------------------------------------- Common */

        private List<ISnPatch> CollectCandidates()
        {
            return Providers.Instance
                .Components
                .Cast<ISnComponent>()
                .SelectMany(component =>
                {
                    var builder = new PatchBuilder(component);
                    component.AddPatches(builder);
                    return builder.GetPatches();
                })
                .ToList();
        }

        private List<SnComponentDescriptor> LoadComponents()
        {
            var installed = PackageManager.Storage?
                .LoadInstalledComponentsAsync(CancellationToken.None)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            var faulty = PackageManager.Storage?
                .LoadIncompleteComponentsAsync(CancellationToken.None)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            return SnComponentDescriptor.CreateComponents(installed, faulty);
        }

        private bool HasDuplicates(ComponentInstaller installer, List<ISnPatch> candidates)
        {
            return candidates.Count(patch => patch.Type == PackageType.Install &&
                                             patch.ComponentId == installer.ComponentId) > 1;
        }

        private bool HaveCorrectDependencies(ComponentInstaller patch, List<SnComponentDescriptor> installed)
        {
            if (patch.Dependencies == null)
                return true;
            throw new NotImplementedException();
        }

        private void WriteInitialStateToDb(ISnPatch patch)
        {
            throw new NotImplementedException();
        }
        private void ModifyStateInDb(ISnPatch patch, ExecutionResult result)
        {
            throw new NotImplementedException();
        }

        private void CreateInitialState(ISnPatch patch, List<SnComponentDescriptor> installed)
        {
            var current = installed.FirstOrDefault(x => x.ComponentId == patch.ComponentId);
            if (current == null)
                installed.Add(new SnComponentDescriptor(patch.ComponentId, null, patch.Description,
                    patch.Dependencies?.ToArray()));
        }
        private void ModifyState(ISnPatch patch, List<SnComponentDescriptor> installed, ExecutionResult result)
        {
            var current = installed.First(x => x.ComponentId == patch.ComponentId);

            switch (result)
            {
                case ExecutionResult.Successful:
                    current.Version = patch.Version;
                    current.FaultyBeforeVersion = null;
                    current.FaultyAfterVersion = null;
                    break;
                case ExecutionResult.SuccessfulBefore:
                case ExecutionResult.Faulty:
                    current.FaultyBeforeVersion = null;
                    current.FaultyAfterVersion = patch.Version;
                    break;
                case ExecutionResult.Unfinished:
                case ExecutionResult.FaultyBefore:
                    current.FaultyBeforeVersion = patch.Version;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        private void Log(ISnPatch patch, PatchExecutionEventType eventType)
        {
            _context.LogCallback?.Invoke(new PatchExecutionLogRecord(eventType, patch));
        }

        private void Error(ISnPatch patch, PatchExecutionErrorType errorType, string message)
        {
            _context.Errors.Add(new PatchExecutionError(errorType, patch, message));
        }

        private void RecognizeErrors(List<ISnPatch> notExecutables, List<ISnPatch> candidates, List<SnComponentDescriptor> installed)
        {
            // Recognize duplicated installers
            var installers = notExecutables
                .Where(x => x.Type == PackageType.Install)
                .OrderBy(x => x.ComponentId)
                .ToArray();
            var installerGroups = installers.GroupBy(x => x.ComponentId);
            var duplicates = installerGroups.Where(x => x.Count() > 1).Select(x => x.Key).ToArray();
            if (duplicates.Length > 0)
            {
                // Duplicates are not allowed
                foreach (var id in duplicates)
                {
                    var message = "There is a duplicated installer for the component " + id;
                    var faultyPatches = installers.Where(inst => inst.ComponentId == id).ToArray();
                    var error = new PatchExecutionError(PatchExecutionErrorType.DuplicatedInstaller, faultyPatches, message);
                    var logRecord = new PatchExecutionLogRecord(PatchExecutionEventType.DuplicatedInstaller, faultyPatches.First(), message);
                    _context.Errors.Add(error);
                    _context.LogCallback(logRecord);
                    notExecutables = notExecutables.Except(faultyPatches).ToList();
                    foreach (var item in faultyPatches)
                    {
                        // notExecutables and candidates maybe the same
                        notExecutables.Remove(item);
                        candidates.Remove(item);
                    }
                }
            }

            // Recognize remaining items
            foreach (var patch in notExecutables)
            {
                if (patch is SnPatch snPatch)
                {
                    if (!installed.Any(comp => comp.ComponentId == snPatch.ComponentId &&
                                                         comp.Version < patch.Version &&
                                                         snPatch.Boundary.IsInInterval(comp.Version)))
                    {
                        _context.LogCallback(new PatchExecutionLogRecord(PatchExecutionEventType.CannotExecuteMissingVersion, patch));
                        _context.Errors.Add(new PatchExecutionError(PatchExecutionErrorType.MissingVersion, patch,
                            "Cannot execute the patch " + patch));
                    }
                }

                _context.LogCallback(new PatchExecutionLogRecord(PatchExecutionEventType.CannotExecute, patch));
                _context.Errors.Add(new PatchExecutionError(PatchExecutionErrorType.CannotInstall, patch,
                    "Cannot execute the patch " + patch));

            }
        }








        /* ======================================================================================= OLD */
        /* ---------------------------------------------------------------------------------- OnBefore */

        public void ExecutePatchesBeforeStart_OLD()
        {
            var candidates = Providers.Instance
                .Components
                .Cast<ISnComponent>()
                .SelectMany(component =>
                {
                    var builder = new PatchBuilder(component);
                    component.AddPatches(builder);
                    return builder.GetPatches();
                })
                .ToArray(); 

            var executables = GetExecutablePatches(candidates);

            _context.ExecutablePatchesOnAfter = ExecuteRelevantPatchesBefore(executables).ToList();
        }
        internal IEnumerable<ISnPatch> ExecuteRelevantPatchesBefore(IEnumerable<ISnPatch> candidates)
        {
            var patchesToExec = candidates.ToList();
            while (true)
            {
                var installedComponents = LoadInstalledComponents();
                var executables = GetExecutablePatches(patchesToExec, installedComponents, out _);
                if (executables.Length == 0)
                    break;

                var faulty = CallOnBeforeActions(executables);

                if (faulty == null)
                    break;

                RemoveNotExecutables(patchesToExec);
                if (patchesToExec.Count == 0)
                    break;
            }

            return patchesToExec;
        }
        private ISnPatch CallOnBeforeActions(IEnumerable<ISnPatch> patches)
        {
            try
            {
                foreach (var patch in patches)
                {
                    if (patch.ActionBeforeStart == null)
                        continue;

                    var manifest = Manifest.Create(patch);

                    // Write an "unfinished" record
                    PackageManager.SaveInitialPackage(manifest); //UNDONE:PATCH: try catch

                    // Log after save: the execution is in started state when the callback called
                    // so the callback can see the real state in the database.
                    _context.LogCallback(new PatchExecutionLogRecord(PatchExecutionEventType.OnBeforeActionStarts, patch));

                    // PATCH EXECUTION
                    _context.CurrentPatch = patch;
                    var successful = false;
                    Exception executionError = null;
                    try
                    {
                        patch.ActionBeforeStart?.Invoke(_context);
                        successful = true;
                    }
                    catch (Exception e)
                    {
                        executionError = e;
                        _context.Errors.Add(new PatchExecutionError(PatchExecutionErrorType.ExecutionErrorOnBefore, patch, e.Message));
                        _context.LogCallback(new PatchExecutionLogRecord(PatchExecutionEventType.ExecutionError, patch, e.Message));
                    }

                    try
                    {
                        // Save the execution result but only if faulty. If execution is successful the state
                        // remains unfinished.
                        if (!successful)
                            PackageManager.SavePackage(manifest, null, successful, executionError);

                        // Log after save: the execution is in completed database state when the callback called.
                        _context.LogCallback(new PatchExecutionLogRecord(PatchExecutionEventType.OnBeforeActionFinished,
                            patch,
                            $"{(successful ? ExecutionResult.Successful : ExecutionResult.Faulty)}"));
                    }
                    catch (Exception e)
                    {
                        _context.LogCallback(new PatchExecutionLogRecord(PatchExecutionEventType.PackageNotSaved,
                            patch));
                        throw new PackagingException("Cannot save the package.", e);
                    }

                    // Return the current patch in case of faulty execution.
                    if (executionError != null)
                        return patch;
                }
            }
            finally
            {
                RepositoryVersionInfo.Reset();//UNDONE:PATCH: Determine final place of the RepositoryVersionInfo.Reset()
            }

            // There is no faulty patch.
            return null;
        }

        /* ---------------------------------------------------------------------------------- OnAfter */

        public void ExecutePatchesAfterStart_OLD()
        {
            ExecuteRelevantPatchesAfter(_context.ExecutablePatchesOnAfter);
        }
        internal void ExecuteRelevantPatchesAfter(IEnumerable<ISnPatch> candidates)
        {
            var patchesToExec = candidates.ToList();
            while (true)
            {
                var installedComponents = LoadInstalledComponents();
                var executables = GetExecutablePatches(patchesToExec, installedComponents, out _);
                if (executables.Length == 0)
                    break;

                var faulty = CallOnAfterActions(executables);

                if (faulty == null)
                    break;

                RemoveNotExecutables(patchesToExec);
                if (patchesToExec.Count == 0)
                    break;
            }
        }
        /// <summary>For tests only.</summary>
        internal ISnPatch ExecuteRelevantPatchesAfter(IEnumerable<ISnPatch> candidates, 
            SnComponentDescriptor[] installedComponents)
        {
            return CallOnAfterActions(
                GetExecutablePatches(candidates, installedComponents, out _));
        }
        private ISnPatch CallOnAfterActions(IEnumerable<ISnPatch> patches)
        {
            try
            {
                foreach (var patch in patches)
                {
                    var manifest = Manifest.Create(patch);

                    // Write an "unfinished" record
                    PackageManager.SaveInitialPackage(manifest);

                    // Log after save: the execution is in started state when the callback called
                    // so the callback can see the real state in the database.
                    _context.LogCallback(new PatchExecutionLogRecord(PatchExecutionEventType.OnAfterActionStarts, patch));

                    // PATCH EXECUTION
                    _context.CurrentPatch = patch;
                    var successful = false;
                    Exception executionError = null;
                    try
                    {
                        if (Repository.Started())
                            using (new SystemAccount())
                                patch.Action?.Invoke(_context);
                        else
                            patch.Action?.Invoke(_context);

                        successful = true;
                    }
                    catch (Exception e)
                    {
                        executionError = e;
                        _context.Errors.Add(new PatchExecutionError(PatchExecutionErrorType.ExecutionErrorOnAfter, patch, e.Message));
                        _context.LogCallback(new PatchExecutionLogRecord(PatchExecutionEventType.ExecutionError, patch, e.Message));
                    }

                    try
                    {
                        // Save the execution result
                        PackageManager.SavePackage(manifest, null, successful, executionError);
                        // Log after save: the execution is in completed database state when the callback called.
                        _context.LogCallback(new PatchExecutionLogRecord(PatchExecutionEventType.OnAfterActionFinished,
                            patch,
                            $"{(successful ? ExecutionResult.Successful : ExecutionResult.Faulty)}"));
                    }
                    catch (Exception e)
                    {
                        _context.LogCallback(new PatchExecutionLogRecord(PatchExecutionEventType.PackageNotSaved,
                            patch));
                        throw new PackagingException("Cannot save the package.", e);
                    }

                    // Return the current patch in case of faulty execution.
                    if (executionError != null)
                        return patch;
                }
            }
            finally
            {
                RepositoryVersionInfo.Reset();//UNDONE:PATCH: Determine final place of the RepositoryVersionInfo.Reset()
            }

            // There is no faulty patch.
            return null;
        }

        /* ---------------------------------------------------------------------------------- Common */

        private void RemoveNotExecutables(List<ISnPatch> patchesToExec)
        {
            var patches = _context.Errors.SelectMany(x => x.FaultyPatches);
            foreach (var patch in patches)
                patchesToExec.Remove(patch);
        }

        internal IEnumerable<ISnPatch> GetExecutablePatches(IEnumerable<ISnPatch> candidates)
        {
            return GetExecutablePatches(candidates, LoadInstalledComponents(), out _);
        }

        internal ISnPatch[] GetExecutablePatches(IEnumerable<ISnPatch> candidates,
            SnComponentDescriptor[] installedComponents, out SnComponentDescriptor[] componentsAfter)
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
                foreach (var id in duplicates)
                {
                    var message = "There is a duplicated installer for the component " + id;
                    var faultyPatches = installers.Where(inst => inst.ComponentId == id).ToArray();
                    var error = new PatchExecutionError(PatchExecutionErrorType.DuplicatedInstaller, faultyPatches, message);
                    var logRecord = new PatchExecutionLogRecord(PatchExecutionEventType.DuplicatedInstaller, faultyPatches.First(), message);
                    _context.Errors.Add(error);
                    _context.LogCallback(logRecord);
                }

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
                    if (CheckPrerequisites(item, installed, out var skipExecution, out var error))
                    {
                        currentlyManaged.Add(item);
                        outputList.Add(item);

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
                foreach (var patch in inputList)
                {
                    var error = RecognizeDiscoveryProblem(patch, installed, out var logRecord);
                    _context.Errors.Add(error);
                    _context.LogCallback(logRecord);
                }
            }

            componentsAfter = installed.ToArray();

            return outputList.ToArray();
        }

        private SnComponentDescriptor[] LoadInstalledComponents()
        {
            return PackageManager.Storage?.LoadInstalledComponentsAsync(CancellationToken.None)
                .ConfigureAwait(false).GetAwaiter().GetResult()
                .Select(x => new SnComponentDescriptor(x)).ToArray() ?? Array.Empty<SnComponentDescriptor>();
        }

        private PatchExecutionError RecognizeDiscoveryProblem(ISnPatch patch,
            List<SnComponentDescriptor> installedComponents, out PatchExecutionLogRecord logRecord)
        {
            if (patch is SnPatch snPatch)
            {
                if (!installedComponents.Any(comp => comp.ComponentId == snPatch.ComponentId &&
                                                     comp.Version < patch.Version &&
                                                     snPatch.Boundary.IsInInterval(comp.Version)))
                {
                    logRecord = new PatchExecutionLogRecord(PatchExecutionEventType.CannotExecuteMissingVersion, patch);
                    return new PatchExecutionError(PatchExecutionErrorType.MissingVersion, patch,
                        "Cannot execute the patch " + patch);
                }
            }

            logRecord = new PatchExecutionLogRecord(PatchExecutionEventType.CannotExecute, patch);
            return new PatchExecutionError(PatchExecutionErrorType.CannotInstall, patch,
                "Cannot execute the patch " + patch);
        }

        /// <summary>
        /// Returns true if the given <see cref="ISnPatch"/> is installable.
        /// </summary>
        private bool CheckPrerequisites(ISnPatch patch, List<SnComponentDescriptor> installed, out bool skipExecution, out PatchExecutionError error)
        {
            if (patch is SnPatch snPatch)
                return CheckPrerequisites(snPatch, installed, out skipExecution, out error);
            if (patch is ComponentInstaller installer)
                return CheckPrerequisites(installer, installed, out skipExecution, out error);
            throw new SnNotSupportedException();
        }
        /// <summary>
        /// Returns true if the given <see cref="ComponentInstaller"/> is installable.
        /// </summary>
        private bool CheckPrerequisites(ComponentInstaller installer, List<SnComponentDescriptor> installed,
            out bool skipExecution, out PatchExecutionError error)
        {
            skipExecution = false;
            error = null;
            // Installable if the dependent components exist.
            return CheckDependencies(installer, installed);
        }
        /// <summary>
        /// Returns true if the given <see cref="SnPatch"/> is installable.
        /// </summary>
        private bool CheckPrerequisites(SnPatch snPatch, List<SnComponentDescriptor> installed,
            out bool skipExecution, out PatchExecutionError error)
        {
            skipExecution = false;
            error = null;

            // Search the installed component.
            var target = installed.FirstOrDefault(x => x.ComponentId == snPatch.ComponentId);

            // Not executable if not installed.
            if (target == null)
                return false;

            // Not executable but skipped if the target version is greater than the patch's version.
            if (target.Version >= snPatch.Version)
            {
                //UNDONE:PATCH:LOG: ? Skipped patch ?
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
                //UNDONE:PATCH:LOG: ? Self-dependency ?
                return false;

            // Not installable if there is any dependency but installed nothing.
            if (installed.Count == 0)
                return false;

            // Installable if all dependencies exist.
            return deps.All(dep =>
                installed.Any(i => i.ComponentId == dep.Id && dep.Boundary.IsInInterval(i.Version)));
        }
    }
}
