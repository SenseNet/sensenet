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
using SenseNet.Packaging.Steps;

namespace SenseNet.Packaging
{
    public class PackageManager
    {
        public const string SANDBOXDIRECTORYNAME = "run";

        internal static IPackageStorageProviderFactory StorageFactory { get; set; } =
            new BuiltinPackageStorageProviderFactory();

        internal static IPackageStorageProvider Storage => StorageFactory.CreateProvider();

        public static PackagingResult Execute(string packagePath, string targetPath, int currentPhase, string[] parameters, TextWriter console)
        {
            var phaseCount = 1;

            var files = Directory.GetFiles(packagePath);

            Manifest manifest = null;
            Exception manifestParsingException = null;
            if (files.Length == 1)
            {
                try
                {
                    manifest = Manifest.Parse(files[0], currentPhase, currentPhase == 0);
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
                sandboxDirectory, manifest, currentPhase, manifest.CountOfPhases, parameters, console);

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
    }
}
