using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics;
using SenseNet.ContentRepository;
using System.Reflection;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.Packaging.Steps;

namespace SenseNet.Packaging
{
    public class PackageManager
    {
        public const string SANDBOXDIRECTORYNAME = "run";

        internal static IPackagingDataProviderExtension Storage => DataProvider.GetExtension<IPackagingDataProviderExtension>();

        public static PackagingResult Execute(string packagePath, string targetPath, int currentPhase, string[] parameters, TextWriter console)
        {
            // Workaround for setting the packaging db provider: in normal cases this happens
            // when the repository starts, but in case of package execution the repository 
            // is not yet started sometimes.
            if (null == DataProvider.GetExtension<IPackagingDataProviderExtension>())
                DataProvider.Instance.SetExtension(typeof(IPackagingDataProviderExtension), new SqlPackagingDataProvider());

            var packageParameters = parameters?.Select(PackageParameter.Parse).ToArray() ?? new PackageParameter[0];
            var forcedReinstall = "true" == (packageParameters
                .FirstOrDefault(p => p.PropertyName.ToLowerInvariant() == "forcedreinstall")?
                .Value?.ToLowerInvariant() ?? "");

            var phaseCount = 1;

            var files = Directory.GetFiles(packagePath);

            Manifest manifest = null;
            Exception manifestParsingException = null;
            if (files.Length == 1)
            {
                try
                {
                    manifest = Manifest.Parse(files[0], currentPhase, currentPhase == 0, packageParameters, forcedReinstall);
                    phaseCount = manifest.CountOfPhases;
                }
                catch (Exception e)
                {
                    manifestParsingException = e;
                }
            }

            if (files.Length == 0)
                throw new InvalidPackageException(SR.Errors.ManifestNotFound);
            if (files.Length > 1)
                throw new InvalidPackageException(SR.Errors.PackageCanContainOnlyOneFileInTheRoot);
            if (manifestParsingException != null)
                throw new PackagingException("Manifest parsing error. See inner exception.", manifestParsingException);
            if (manifest == null)
                throw new PackagingException("Manifest was not found.");

            Logger.LogTitle(String.Format("Executing phase {0}/{1}", currentPhase + 1, phaseCount));

            var sandboxDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var executionContext = new ExecutionContext(packagePath, targetPath, Configuration.Packaging.NetworkTargets,
                sandboxDirectory, manifest, currentPhase, manifest.CountOfPhases, packageParameters, console);

            executionContext.LogVariables();

            PackagingResult result;
            try
            {
                result = ExecuteCurrentPhase(manifest, executionContext);
            }
            finally
            {
                if (Repository.Started())
                {
                    console.WriteLine("-------------------------------------------------------------");
                    console.Write("Stopping repository ... ");
                    Repository.Shutdown();
                    console.WriteLine("Ok.");
                }
            }

            return result;
        }

        internal static PackagingResult ExecuteCurrentPhase(Manifest manifest, ExecutionContext executionContext)
        {
            var sysInstall = manifest.SystemInstall;
            var currentPhase = executionContext.CurrentPhase;
            if (0 == currentPhase - (sysInstall ? 1 : 0))
                SaveInitialPackage(manifest);

            var stepElements = manifest.GetPhase(executionContext.CurrentPhase);

            var stopper = Stopwatch.StartNew();
            Logger.LogMessage("Executing steps");

            Exception phaseException = null;
            var successful = false;
            try
            {
                var maxStepId = stepElements.Count;
                for (int i = 0; i < maxStepId; i++)
                {
                    var stepElement = stepElements[i];
                    var step = Step.Parse(stepElement, i, executionContext);

                    var stepStopper = Stopwatch.StartNew();
                    Logger.LogStep(step, maxStepId);
                    step.Execute(executionContext);
                    stepStopper.Stop();
                    Logger.LogMessage("-------------------------------------------------------------");
                    Logger.LogMessage("Time: " + stepStopper.Elapsed);
                    if (executionContext.Terminated)
                    {
                        LogTermination(executionContext);
                        break;
                    }
                }
                stopper.Stop();
                Logger.LogMessage("=============================================================");
                Logger.LogMessage("All steps were executed.");
                Logger.LogMessage("Aggregated time: " + stopper.Elapsed);
                Logger.LogMessage("Errors: " + Logger.Errors);
                successful = true;
            }
            catch (Exception e)
            {
                phaseException = e;
            }

            var finished = executionContext.Terminated || (executionContext.CurrentPhase == manifest.CountOfPhases - 1);

            if (successful && !finished)
                return new PackagingResult { NeedRestart = true, Successful = true, Errors = Logger.Errors };

            if (executionContext.Terminated && executionContext.TerminationReason == TerminationReason.Warning)
            {
                successful = false;
                phaseException = new PackageTerminatedException(executionContext.TerminationMessage);
            }

            try
            {
                SavePackage(manifest, executionContext, successful, phaseException);
            }
            catch(Exception e)
            {
                if (phaseException != null)
                    Logger.LogException(phaseException);
                throw new PackagingException("Cannot save the package.", e);
            }
            finally
            {
                RepositoryVersionInfo.Reset();

                // we need to shut down messaging, because the line above uses it
                if (!executionContext.Test)
                    DistributedApplication.ClusterChannel.ShutDown();
                else
                    Diagnostics.SnTrace.Test.Write("DistributedApplication.ClusterChannel.ShutDown SKIPPED because it is a test context.");
            }
            if (!successful && !executionContext.Terminated)
                throw new ApplicationException(String.Format(SR.Errors.PhaseFinishedWithError_1, phaseException.Message), phaseException);

            return new PackagingResult { NeedRestart = false, Successful = successful, Terminated = executionContext.Terminated && !successful, Errors = Logger.Errors };
        }
        public static void ExecuteSteps(List<XmlElement> stepElements, ExecutionContext executionContext)
        {
            var maxStepId = stepElements.Count();
            for (int i = 0; i < maxStepId; i++)
            {
                var stepElement = stepElements[i];
                var step = Step.Parse(stepElement, i, executionContext);

                var stepStopper = Stopwatch.StartNew();
                Logger.LogStep(step, maxStepId);
                step.Execute(executionContext);
                stepStopper.Stop();
                Logger.LogMessage("-------------------------------------------------------------");
                Logger.LogMessage("Time: " + stepStopper.Elapsed);
                if (executionContext.Terminated)
                {
                    LogTermination(executionContext);
                    break;
                }
            }
        }
        private static void LogTermination(ExecutionContext executionContext)
        {
            var message = ((executionContext.TerminationReason == TerminationReason.Warning) ? "WARNING. " : string.Empty)
                + "Execution terminated. " + executionContext.TerminationMessage;
            Logger.LogMessage(message);
        }

        private static void SaveInitialPackage(Manifest manifest)
        {
            var newPack = CreatePackage(manifest, ExecutionResult.Unfinished, null);
            Storage.SavePackage(newPack);
        }
        private static void SavePackage(Manifest manifest, ExecutionContext executionContext, bool successful, Exception execError)
        {
            var executionResult = successful ? ExecutionResult.Successful : ExecutionResult.Faulty;

            RepositoryVersionInfo.Reset();
            var oldPacks = RepositoryVersionInfo.Instance.InstalledPackages;
            if (manifest.PackageType == PackageType.Tool)
                oldPacks = oldPacks
                    .Where(p => p.ComponentId == manifest.ComponentId && p.PackageType == PackageType.Tool
                    && p.ExecutionResult == ExecutionResult.Unfinished);
            else
                oldPacks = oldPacks
                    .Where(p => p.ComponentId == manifest.ComponentId && p.ComponentVersion == manifest.Version);
            oldPacks = oldPacks.OrderBy(p => p.ExecutionDate).ToArray();

            var oldPack = oldPacks.LastOrDefault();
            if (oldPack == null)
            {
                var newPack = CreatePackage(manifest, executionResult, execError);
                Storage.SavePackage(newPack);
            }
            else
            {
                UpdatePackage(oldPack, manifest, executionResult, execError);
                Storage.UpdatePackage(oldPack);
            }
        }
        private static Package CreatePackage(Manifest manifest, ExecutionResult result, Exception execError)
        {
            return new Package
            {
                Description = manifest.Description,
                ReleaseDate = manifest.ReleaseDate,
                PackageType = manifest.PackageType,
                ComponentId = manifest.ComponentId,
                ExecutionDate = DateTime.UtcNow,
                ExecutionResult = result,
                ComponentVersion = manifest.Version,
                ExecutionError = execError,
                Manifest = manifest.ManifestXml.OuterXml
            };
        }
        private static void UpdatePackage(Package package, Manifest manifest, ExecutionResult result, Exception execError)
        {
            package.Description = manifest.Description;
            package.ReleaseDate = manifest.ReleaseDate;
            package.PackageType = manifest.PackageType;
            package.ComponentId = manifest.ComponentId;
            package.ExecutionDate = DateTime.UtcNow;
            package.ExecutionResult = result;
            package.ExecutionError = execError;
            package.ComponentVersion = manifest.Version;
        }

        public static string GetHelp()
        {
            var memory = new List<string>();

            var sb = new StringBuilder();
            sb.AppendLine("Available step types and parameters");
            sb.AppendLine("-----------------------------------");
            foreach (var item in Step.StepTypes)
            {
                var stepType = item.Value;
                if (memory.Contains(stepType.FullName))
                    continue;
                memory.Add(stepType.FullName);

                var step = (Step)Activator.CreateInstance(stepType);
                sb.AppendLine(step.ElementName + " (" + stepType.FullName + ")");
                foreach (var property in stepType.GetProperties())
                {
                    if (property.Name == "StepId" || property.Name == "ElementName")
                        continue;
                    var isDefault = property.GetCustomAttributes(true).Any(x => x is DefaultPropertyAttribute);
                    sb.AppendFormat("  {0} : {1} {2}", property.Name, property.PropertyType.Name, isDefault ? "(Default)" : "");
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }
        public static string GetXmlSchema()
        {
            return PackageSchemaGenerator.GenerateSchema();
        }
        internal static string ToPascalCase(string propertyName)
        {
            if (Char.IsLower(propertyName[0]))
            {
                var rewrittenName = Char.ToUpper(propertyName[0]).ToString();
                if (propertyName.Length > 1)
                    rewrittenName += propertyName.Substring(1);
                propertyName = rewrittenName;
            }
            return propertyName;
        }
        internal static string ToCamelCase(string propertyName)
        {
            if (Char.IsUpper(propertyName[0]))
            {
                var rewrittenName = Char.ToLower(propertyName[0]).ToString();
                if (propertyName.Length > 1)
                    rewrittenName += propertyName.Substring(1);
                propertyName = rewrittenName;
            }
            return propertyName;
        }

        /// <summary>
        /// Executes all relevant patches in all known components. A patch is relevant only
        /// if its min and max versions encompass the currently installed version and the
        /// supported component version in the assembly is higher than the one in the database.
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, Dictionary<Version, PackagingResult>> ExecuteAssemblyPatches()
        {
            return ExecuteAssemblyPatches(RepositoryVersionInfo.GetAssemblyComponents());
        }

        internal static Dictionary<string, Dictionary<Version, PackagingResult>> ExecuteAssemblyPatches(IEnumerable<SnComponentInfo> assemblyComponents)
        {
            var results = new Dictionary<string, Dictionary<Version, PackagingResult>>();

            foreach (var assemblyComponent in assemblyComponents)
            {
                var patchResults = ExecuteAssemblyPatch(assemblyComponent);

                if (patchResults?.Any() ?? false)
                    results[assemblyComponent.ComponentId] = patchResults;
            }

            return results;
        }
        internal static Dictionary<Version, PackagingResult> ExecuteAssemblyPatch(SnComponentInfo assemblyComponent)
        {
            var patchResults = new Dictionary<Version, PackagingResult>();

            // If there is no installed component for this id, skip patching.
            var installedComponent = RepositoryVersionInfo.Instance.Components.FirstOrDefault(c => c.ComponentId == assemblyComponent.ComponentId);
            if (installedComponent == null)
                return patchResults;

            // check which db version is supported by the assembly
            if (assemblyComponent.SupportedVersion == null ||
                assemblyComponent.SupportedVersion <= installedComponent.Version)
                return patchResults;

            // Supported version in the assembly is higher than 
            // the physical version: there should be a patch.
            if (assemblyComponent.Patches?.Any() ?? false)
            {
                foreach (var patch in assemblyComponent.Patches)
                {
                    if (patch.MinVersion > installedComponent.Version ||
                        patch.MinVersionIsExclusive && patch.MinVersion == installedComponent.Version ||
                        patch.MaxVersion < installedComponent.Version ||
                        patch.MaxVersionIsExclusive && patch.MaxVersion == installedComponent.Version)
                        continue;

                    //UNDONE: handle other patch formats (resource or filesystem path)
                    if (patch.Contents.StartsWith("<?xml", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var patchResult = ExecutePatch(patch.Contents);
                        patchResults[patch.Version] = patchResult;

                        //UNDONE: what if there are multiple patches and one fails: cleanup?
                        if (!patchResult.Successful || patchResult.Errors > 0)
                        {
                            //TODO: log and throw error
                        }
                    }

                    // reload
                    installedComponent = RepositoryVersionInfo.Instance.Components.FirstOrDefault(c => c.ComponentId == assemblyComponent.ComponentId);
                }
            }
            else
            {
                throw new InvalidOperationException($"Missing patch for component {installedComponent.ComponentId}. " +
                                                    $"Installed version is {installedComponent.Version}. " +
                                                    $"The assembly requires at least version {assemblyComponent.SupportedVersion}.");
            }

            return patchResults;
        }

        //UNDONE: check if these methods are necessary 
        // or can be refactored to use existing methods.
        internal static PackagingResult ExecutePatch(string manifestXml, TextWriter console = null)
        {
            try
            {
                var xml = new XmlDocument();
                xml.LoadXml(manifestXml);
                return ExecutePatch(xml, console);
            }
            catch (XmlException ex)
            {
                throw new InvalidPackageException("Invalid manifest xml.", ex);
            }
        }
        internal static PackagingResult ExecutePatch(XmlDocument manifestXml, TextWriter console = null)
        {
            var phase = -1;
            var errors = 0;
            PackagingResult result;

            do
            {
                result = ExecutePhase(manifestXml, ++phase, console ?? new StringWriter());
                errors += result.Errors;
            } while (result.NeedRestart);

            result.Errors = errors;
            return result;
        }
        internal static PackagingResult ExecutePhase(XmlDocument manifestXml, int phase, TextWriter console = null)
        {
            var manifest = Manifest.Parse(manifestXml, phase, true, new PackageParameter[0]);

            //UNDONE: CreateForTest?
            var executionContext = ExecutionContext.CreateForTest("packagePath", "targetPath", 
                new string[0], "sandboxPath", manifest, phase, manifest.CountOfPhases, 
                null, console ?? new StringWriter());

            var result = ExecuteCurrentPhase(manifest, executionContext);
            RepositoryVersionInfo.Reset();

            return result;
        }
    }
}
