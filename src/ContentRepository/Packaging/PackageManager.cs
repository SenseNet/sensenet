using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics;
using SenseNet.ContentRepository;
using System.Reflection;
using System.Threading;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Diagnostics;
using SenseNet.Packaging.Steps;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    public class PackageManager
    {
        public const string SANDBOXDIRECTORYNAME = "run";

        internal static IPackagingDataProviderExtension Storage => DataStore.GetDataProviderExtension<IPackagingDataProviderExtension>();

        public static PackagingResult Execute(string packagePath, string targetPath, int currentPhase,
            string[] parameters, TextWriter console, RepositoryBuilder builder = null,
            bool editConnectionString = false)
        {
            var packageParameters = parameters?.Select(PackageParameter.Parse).ToArray() ?? new PackageParameter[0];

            return Execute(packagePath, targetPath, currentPhase, packageParameters, console, builder,
                editConnectionString);
        }
        public static PackagingResult Execute(string packagePath, string targetPath, int currentPhase, 
            PackageParameter[] parameters, TextWriter console, 
            RepositoryBuilder builder = null, bool editConnectionString = false)
        {
            // Workaround for setting the packaging db provider: in normal cases this happens
            // when the repository starts, but in case of package execution the repository 
            // is not yet started sometimes.
            if (null == DataStore.GetDataProviderExtension<IPackagingDataProviderExtension>())
                DataStore.SetDataProviderExtension(typeof(IPackagingDataProviderExtension), new MsSqlPackagingDataProvider());

            var packageParameters = parameters ?? new PackageParameter[0];
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
                    manifest = Manifest.Parse(files[0], currentPhase, currentPhase == 0, packageParameters,
                        forcedReinstall, editConnectionString);
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
            var executionContext = ExecutionContext.Create(packagePath, targetPath, Configuration.Packaging.NetworkTargets,
                sandboxDirectory, manifest, currentPhase, manifest.CountOfPhases, packageParameters, console, builder);

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
                    DistributedApplication.ClusterChannel.ShutDownAsync(CancellationToken.None).GetAwaiter().GetResult();
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

        internal static void SaveInitialPackage(Manifest manifest)
        {
            var newPack = CreatePackage(manifest, ExecutionResult.Unfinished, null);
            Storage.SavePackageAsync(newPack, CancellationToken.None).GetAwaiter().GetResult();
        }
        internal static void SavePackage(Manifest manifest, ExecutionContext executionContext, bool successful, Exception execError)
        {
            SavePackage(manifest, successful ? ExecutionResult.Successful : ExecutionResult.Faulty, execError);
        }
        internal static void SavePackage(Manifest manifest, ExecutionResult executionResult, Exception execError, bool insertOnly = false)
        {
            RepositoryVersionInfo.Reset(true);
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
            if (oldPack == null || insertOnly)
            {
                var newPack = CreatePackage(manifest, executionResult, execError);
                Storage.SavePackageAsync(newPack, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                UpdatePackage(oldPack, manifest, executionResult, execError);
                Storage.UpdatePackageAsync(oldPack, CancellationToken.None).GetAwaiter().GetResult();
            }

            RepositoryVersionInfo.Reset(true);
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

        internal static PackagingResult ExecutePatch(string manifestXml, RepositoryStartSettings settings = null)
        {
            try
            {
                var xml = new XmlDocument();
                xml.LoadXml(manifestXml);
                return ExecutePatch(xml, settings);
            }
            catch (XmlException ex)
            {
                throw new InvalidPackageException("Invalid manifest xml.", ex);
            }
        }
        internal static PackagingResult ExecutePatch(XmlDocument manifestXml, RepositoryStartSettings settings = null)
        {
            var phase = -1;
            var errors = 0;
            PackagingResult result;

            do
            {
                result = ExecutePhase(manifestXml, ++phase, settings);
                errors += result.Errors;
            } while (result.NeedRestart);

            result.Errors = errors;
            return result;
        }
        internal static PackagingResult ExecutePhase(XmlDocument manifestXml, int phase, RepositoryStartSettings settings = null)
        {
            var manifest = Manifest.Parse(manifestXml, phase, true, new PackageParameter[0]);

            // Fill context with indexing folder, repo start settings, providers and other 
            // parameters necessary for on-the-fly steps to run.
            var executionContext = ExecutionContext.Create("packagePath", "targetPath", 
                new string[0], "sandboxPath", manifest, phase, manifest.CountOfPhases, 
                null, null, settings);

            PackagingResult result; 

            try
            {
                result = ExecuteCurrentPhase(manifest, executionContext);
            }
            finally
            {
                if (Repository.Started())
                {
                    SnTrace.System.Write("PackageManager: stopping repository ... ");
                    Repository.Shutdown();
                }
            }

            RepositoryVersionInfo.Reset();

            return result;
        }
    }
}
