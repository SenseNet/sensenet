using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    public class PatchManager
    {
        private readonly PatchExecutionContext _context;

        /* =========================================================================================== */
        
        public PatchManager(RepositoryStartSettings settings, Action<PatchExecutionLogRecord> logCallback)
        {
            _context = new PatchExecutionContext(settings, logCallback);
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
                dependencies = patch.Dependencies?.ToArray();
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
            _context.CurrentlyInstalledComponents = installed;

            SortCandidates(candidates);

            var toExec = candidates.ToList(); // Copy
            var executed = new List<ISnPatch>();

            // Try to execute patches one after the other until there is nothing more to do.
            // If a patch is skipped in an iteration, it may still be executed later,
            // if other patches change the environment.
            while (true)
            {
                var isActive = false;
                foreach (var patch in toExec)
                {
                    if (CheckPrerequisitesBefore(patch, candidates, installed, out var isIrrelevant))
                    {
                        if (!isSimulation)
                            if (patch.ActionBeforeStart != null)
                                WriteInitialStateToDb(patch);
                        CreateInitialState(patch, installed);

                        try
                        {
                            Log(patch, PatchExecutionEventType.OnBeforeActionStarts);
                            if (!isSimulation)
                            {
                                _context.CurrentPatch = patch;
                                _context.RepositoryIsRunning = false;
                                if (patch.ActionBeforeStart != null)
                                {
                                    patch.ActionBeforeStart?.Invoke(_context);
                                    ModifyStateInDb(patch, ExecutionResult.SuccessfulBefore, null);
                                }
                            }
                            ModifyState(patch, installed, ExecutionResult.SuccessfulBefore);
                            Log(patch, PatchExecutionEventType.OnBeforeActionFinished);
                        }
                        catch (Exception e)
                        {
                            if (!isSimulation)
                                ModifyStateInDb(patch, ExecutionResult.FaultyBefore, e);
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
                RecognizeErrors(toExec, candidates, installed, true);
        }

        internal static void SortCandidates(List<ISnPatch> candidates)
        {
            candidates.Sort((x, y) =>
            {
                int q;

                // first sort by component Id
                if (0 != (q = string.Compare(x.ComponentId, y.ComponentId, StringComparison.Ordinal)))
                    return q;
                // Installer comes before SnPatch
                if (0 != (q = (x.Type == PackageType.Install ? 0 : 1).CompareTo(y.Type == PackageType.Install ? 0 : 1)))
                    return q;
                // sort by the final version that the patch sets
                if (0 != (q = x.Version.CompareTo(y.Version)))
                    return q;

                if (!(x is SnPatch xP) || !(y is SnPatch yP))
                    return 0;

                if (0 != (q = xP.Boundary.MinVersion.CompareTo(yP.Boundary.MinVersion)))
                    return q;
                if (0 != (q = xP.Boundary.MinVersionIsExclusive.CompareTo(yP.Boundary.MinVersionIsExclusive)))
                    return q;
                if (0 != (q = xP.Boundary.MaxVersion.CompareTo(yP.Boundary.MaxVersion)))
                    return q;
                if (0 != (q = xP.Boundary.MaxVersionIsExclusive.CompareTo(yP.Boundary.MaxVersionIsExclusive)))
                    return -q;

                return 0;
            });
        }

        private bool CheckPrerequisitesBefore(ISnPatch patch, List<ISnPatch> candidates, List<SnComponentDescriptor> installed, out bool isIrrelevant)
        {
            var component = installed.FirstOrDefault(x => x.ComponentId == patch.ComponentId);
            
            // a patch becomes irrelevant if we know for sure that it cannot be executed
            isIrrelevant = false;

            if (patch is ComponentInstaller installer)
            {
                if (!ValidInstaller(installer))                           { isIrrelevant = true; return false; }
                if (HasDuplicates(installer, candidates))                 {                      return false; }
                if (!HasCorrectDependencies(installer, installed, true))  {                      return false; }
                if (component == null)                                    {                       return true; }
                if (component.State == ExecutionResult.Unfinished)        {                       return true; }
                if (component.State == ExecutionResult.FaultyBefore &&
                    component.TempVersionBefore == patch.Version)         {                       return true; }
                isIrrelevant = true;
                return false;
            }

            if (patch is SnPatch snPatch)
            {
                if (!ValidSnPatch(snPatch))                                { isIrrelevant = true; return false; }
                if (!HasCorrectDependencies(snPatch, installed, true))     {                      return false; }
                if (component == null)                                     {                      return false; }
                if (component.Version >= patch.Version)                    { isIrrelevant = true; return false; }
                if (component.State == ExecutionResult.SuccessfulBefore &&
                    component.TempVersionBefore >= patch.Version)          { isIrrelevant = true; return false; }
                if (!IsInInterval(snPatch, component, true))               {                      return false; }
                if (component.State == ExecutionResult.Faulty &&
                    component.TempVersionAfter == patch.Version)           { isIrrelevant = true; return false; }
                if (component.State == ExecutionResult.Unfinished && 
                    component.TempVersionBefore == patch.Version)          {                       return true; }
                if (component.State == ExecutionResult.FaultyBefore &&
                    component.TempVersionBefore == patch.Version)          {                       return true; }
                if (component.State == ExecutionResult.SuccessfulBefore &&
                    component.TempVersionBefore < patch.Version)           {                       return true; }
                if (component.State == ExecutionResult.Successful &&
                    component.Version < patch.Version)                     {                       return true; }
                isIrrelevant = true;
                return false;
            }
            throw new NotSupportedException(
                $"Manage this patch is not supported. ComponentId: {patch.ComponentId}, " +
                $"Version: {patch.Version}, PackageType: {patch.Type}");
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
            var toExec = candidates;
            var executed = new List<ISnPatch>();

            // Try to execute patches one after the other until there is nothing more to do.
            // If a patch is skipped in an iteration, it may still be executed later,
            // if other patches change the environment.
            while (true)
            {
                var isActive = false;
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
                                _context.CurrentPatch = patch;
                                _context.RepositoryIsRunning = true;

                                // execute patch in elevated mode so that developers do not have to elevate every time
                            if (patch.Action != null)
                            {
                                if (Repository.Started())
                                    using (new SystemAccount())
                                        patch.Action.Invoke(_context);
                                else
                                    patch.Action.Invoke(_context);
                            }
                                ModifyStateInDb(patch, ExecutionResult.Successful, null);
                            }
                            ModifyState(patch, installed, ExecutionResult.Successful);
                            Log(patch, PatchExecutionEventType.OnAfterActionFinished);
                        }
                        catch (Exception e)
                        {
                            if (!isSimulation)
                                ModifyStateInDb(patch, ExecutionResult.Faulty, e);
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
                RecognizeErrors(toExec, candidates, installed, false);
        }

        private bool CheckPrerequisitesAfter(ISnPatch patch, List<ISnPatch> candidates, List<SnComponentDescriptor> installed, out bool isIrrelevant)
        {
            var component = installed.FirstOrDefault(x => x.ComponentId == patch.ComponentId);

            // a patch becomes irrelevant if we know for sure that it cannot be executed
            isIrrelevant = false;

            if (patch is ComponentInstaller installer)
            {
                if (!ValidInstaller(installer))                            { isIrrelevant = true; return false; }
                if (HasDuplicates(installer, candidates))                  {                      return false; }
                if (!HasCorrectDependencies(installer, installed, false))  {                      return false; }
                if (component == null)                                     {                       return true; }
                if (component.Version != null)                             { isIrrelevant = true; return false; }
                if (component.State == ExecutionResult.Unfinished)         {                       return true; }
                if (component.State == ExecutionResult.FaultyBefore)       {                       return true; }
                if (component.State == ExecutionResult.SuccessfulBefore)   {                       return true; }
                if (component.State == ExecutionResult.Faulty &&
                    component.TempVersionAfter == patch.Version)           {                       return true; }
                isIrrelevant = true;
                return false;
            }

            if (patch is SnPatch snPatch)
            {
                if (!ValidSnPatch(snPatch))                                { isIrrelevant = true; return false; }
                if (!HasCorrectDependencies(snPatch, installed, false))    {                      return false; }
                if (component == null)                                     {                      return false; }
                if (component.Version >= patch.Version)                    { isIrrelevant = true; return false; }
                if (!IsInInterval(snPatch, component, false))              {                      return false; }
                if (component.State == ExecutionResult.Unfinished &&
                    (component.TempVersionBefore == null || 
                     component.TempVersionBefore == patch.Version))          {                       return true; }
                if (component.State == ExecutionResult.FaultyBefore &&
                    (component.TempVersionBefore == null || 
                     component.TempVersionBefore == patch.Version))          {                       return true; }
                if (component.State == ExecutionResult.Faulty &&
                    (component.TempVersionAfter == null || 
                     component.TempVersionAfter == patch.Version))           {                       return true; }
                if (component.State == ExecutionResult.SuccessfulBefore &&
                    (component.TempVersionAfter == null ||
                     component.TempVersionAfter <= patch.Version))           {                       return true; }
                if (component.State == ExecutionResult.Successful && 
                    component.Version < patch.Version)                     {                       return true; }
                isIrrelevant = true;
                return false;
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
            if(PackageManager.Storage == null)
                return new List<SnComponentDescriptor>();

            var installed = PackageManager.Storage?
                .LoadInstalledComponentsAsync(CancellationToken.None)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            var faulty = PackageManager.Storage?
                .LoadIncompleteComponentsAsync(CancellationToken.None)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            return SnComponentDescriptor.CreateComponents(installed, faulty);
        }

        private bool ValidInstaller(ComponentInstaller installer)
        {
            if (!CheckSelfDependency(installer))
                return false;
            return true;
        }
        private bool ValidSnPatch(SnPatch snPatch)
        {
            if (!CheckSelfDependency(snPatch))
                return false;
            return true;
        }
        private bool CheckSelfDependency(ISnPatch patch)
        {
            if (patch.Dependencies == null || patch.Dependencies.All(d => d.Id != patch.ComponentId))
                return true;
            _context.Errors.Add(new PatchExecutionError(PatchExecutionErrorType.SelfDependencyForbidden, patch, "Self dependency is forbidden."));
            return false;
        }

        private bool HasDuplicates(ComponentInstaller installer, List<ISnPatch> candidates)
        {
            return candidates.Count(patch => patch.Type == PackageType.Install &&
                                             patch.ComponentId == installer.ComponentId) > 1;
        }

        private bool HasCorrectDependencies(ISnPatch patch, List<SnComponentDescriptor> installed, bool onBefore)
        {
            // All right if there is no any dependency.
            if (patch.Dependencies == null)
                return true;
            var deps = patch.Dependencies.ToArray();
            if (deps.Length == 0)
                return true;

            // Not installable if there is any dependency but installed nothing.
            if (installed.Count == 0)
                return false;

            // Installable if all dependencies exist.
            return deps.All(dep =>
                installed.Any(c => c.ComponentId == dep.Id && 
                                   dep.Boundary.ContainsVersion(GetDependencyTargetVersion(c, onBefore))));
        }
        private Version GetDependencyTargetVersion(SnComponentDescriptor target, bool onBefore)
        {
            if (onBefore)
            {
                if (target.State == ExecutionResult.SuccessfulBefore)
                    return target.TempVersionBefore;
                return target.Version;
            }
            return target.Version;
        }
        private bool IsInInterval(SnPatch snPatch, SnComponentDescriptor target, bool onBefore)
        {
            Version version;
            if (onBefore)
            {
                if (target.State == ExecutionResult.Unfinished ||
                    target.State == ExecutionResult.FaultyBefore ||
                    target.State == ExecutionResult.Faulty)
                    version = target.Version;
                else
                    version = target.TempVersionBefore ?? target.Version;
            }
            else
            {
                if(target.State == ExecutionResult.Faulty || target.State == ExecutionResult.SuccessfulBefore)
                    version = target.Version;
                else
                    version = target.TempVersionAfter ?? target.Version;
            }
            return snPatch.Boundary.ContainsVersion(version);
        }

        private void WriteInitialStateToDb(ISnPatch patch)
        {
             PackageManager.SaveInitialPackage(Manifest.Create(patch));
        }
        private void ModifyStateInDb(ISnPatch patch, ExecutionResult result, Exception exception)
        {
            PackageManager.SavePackage(Manifest.Create(patch), result, exception);
        }

        private void CreateInitialState(ISnPatch patch, List<SnComponentDescriptor> installed)
        {
            var current = installed.FirstOrDefault(x => x.ComponentId == patch.ComponentId);
            if (current == null)
                installed.Add(new SnComponentDescriptor(patch.ComponentId, null, patch.Description,
                    patch.Dependencies?.ToArray()) { State = ExecutionResult.Unfinished, TempVersionBefore = patch.Version });
        }
        private void ModifyState(ISnPatch patch, List<SnComponentDescriptor> installed, ExecutionResult result)
        {
            var target = installed.First(x => x.ComponentId == patch.ComponentId);

            switch (result)
            {
                case ExecutionResult.Successful:
                    target.Version = patch.Version;
                    target.TempVersionAfter = patch.Version;
                    target.State = result;
                    break;
                case ExecutionResult.Faulty:
                    target.TempVersionAfter = patch.Version;
                    target.State = result;
                    break;
                case ExecutionResult.Unfinished:
                case ExecutionResult.FaultyBefore:
                case ExecutionResult.SuccessfulBefore:
                    target.TempVersionBefore = patch.Version;
                    target.State = result;
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

        private void RecognizeErrors(List<ISnPatch> notExecutables, List<ISnPatch> candidates, List<SnComponentDescriptor> installed, bool onBefore)
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
            var toRemove = new List<ISnPatch>();
            foreach (var patch in notExecutables)
            {
                if(!onBefore)
                    toRemove.Add(patch);

                if (patch is SnPatch snPatch)
                {
                    if (!installed.Any(comp => comp.ComponentId == snPatch.ComponentId &&
                                                         (comp.Version == null || comp.Version < patch.Version) &&
                                                         snPatch.Boundary.ContainsVersion(comp.Version)))
                    {
                        var component = installed.FirstOrDefault(c => c.ComponentId == patch.ComponentId);
                        var message = component == null
                            ? $"Cannot execute the patch [{patch}] {(onBefore ? "before" : "after")} " +
                              "repository start because the component is not yet installed."
                            : $"Cannot execute the patch [{patch}] {(onBefore ? "before" : "after")} " +
                              $"repository start because the component's version is small: [{component}].";
                        _context.LogCallback(
                            new PatchExecutionLogRecord(PatchExecutionEventType.CannotExecuteMissingVersion,
                                patch, message));
                        _context.Errors.Add(new PatchExecutionError(PatchExecutionErrorType.MissingVersion,
                            patch, message));
                    }
                }
                else
                {
                    var message = $"Cannot execute the patch [{patch}] {(onBefore ? "before" : "after")} repository start.";
                    _context.LogCallback(new PatchExecutionLogRecord(
                        onBefore
                            ? PatchExecutionEventType.CannotExecuteOnBefore
                            : PatchExecutionEventType.CannotExecuteOnAfter,
                        patch, message));
                    _context.Errors.Add(new PatchExecutionError(
                        onBefore
                            ? PatchExecutionErrorType.CannotExecuteOnBefore
                            : PatchExecutionErrorType.CannotExecuteOnAfter,
                        patch, message + $" [{patch}]"));
                }

            }
            foreach (var item in toRemove)
                candidates.Remove(item);
        }
    }
}
