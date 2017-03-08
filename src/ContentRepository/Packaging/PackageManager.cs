using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics;
using SenseNet.ContentRepository;
using System.Reflection;
using System.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Packaging.Steps;

namespace SenseNet.Packaging
{
    public class PackageManager
    {
        public const string SANDBOXDIRECTORYNAME = "run";

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

        private static PackagingResult ExecuteCurrentPhase(Manifest manifest, ExecutionContext executionContext)
        {
            if (manifest.Type == PackageType.Product && manifest.Level == PackageLevel.Install)
            {
                // In case of product install create initial entry at the beginning of the
                // second phase, after the new db was created in the first phase.
                if (executionContext.CurrentPhase == 1)
                    SaveInitialPackage(manifest);
            }
            else
            {
                if (executionContext.CurrentPhase == 0)
                    SaveInitialPackage(manifest);
            }

            var stepElements = manifest.GetPhase(executionContext.CurrentPhase);

            var stopper = Stopwatch.StartNew();
            Logger.LogMessage("Executing steps");

            Exception phaseException = null;
            var successful = false;
            try
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
            DataProvider.Current.SavePackage(newPack);
        }
        private static void SavePackage(Manifest manifest, ExecutionContext executionContext, bool successful, Exception execError)
        {
            var executionResult = successful ? ExecutionResult.Successful : ExecutionResult.Faulty;
            var isAppPack = manifest.Type== PackageType.Application;

            RepositoryVersionInfo.Reset();
            Package[] oldPacks = null;
            if (manifest.Level == PackageLevel.Tool)
            {
                oldPacks = RepositoryVersionInfo.Instance.InstalledPackages
                    .Where(p => p.AppId == manifest.AppId && p.PackageLevel == PackageLevel.Tool && p.ExecutionResult == ExecutionResult.Unfinished)
                    .OrderBy(p => p.ExecutionDate).ToArray();
            }
            else
            {
                oldPacks = isAppPack
                    ? RepositoryVersionInfo.Instance.InstalledPackages
                        .Where(p => p.AppId == manifest.AppId && p.ApplicationVersion == manifest.VersionControl.Target)
                        .OrderBy(p => p.ExecutionDate).ToArray()
                    : RepositoryVersionInfo.Instance.InstalledPackages
                        .Where(p => p.AppId == manifest.AppId && p.SenseNetVersion == manifest.VersionControl.Target)
                        .OrderBy(p => p.ExecutionDate).ToArray();
            }

            var oldPack = oldPacks.LastOrDefault();
            if (oldPack == null)
            {
                var newPack = CreatePackage(manifest, executionResult, execError);
                DataProvider.Current.SavePackage(newPack);
            }
            else
            {
                UpdatePackage(oldPack, manifest, executionResult, execError);
                DataProvider.Current.UpdatePackage(oldPack);
            }
        }
        private static Package CreatePackage(Manifest manifest, ExecutionResult result, Exception execError)
        {
            var snInfo = RepositoryVersionInfo.Instance.OfficialSenseNetVersion;
            var prodVer = (snInfo == null) ? new Version(42, 42, 42, 42) : snInfo.Version;

            Version appVer = null;

            if (manifest.Level != ContentRepository.Storage.PackageLevel.Tool)
            {
                if (manifest.Type == ContentRepository.Storage.PackageType.Product)
                    prodVer = manifest.VersionControl.Target;
                else
                    appVer = manifest.VersionControl.Target;
            }

            return new ContentRepository.Storage.Package
            {
                Name = manifest.Name,
                Edition = manifest.Edition,
                Description = manifest.Description,
                ReleaseDate = manifest.ReleaseDate,
                PackageLevel = manifest.Level,
                PackageType = manifest.Type,
                AppId = manifest.AppId,
                ExecutionDate = DateTime.UtcNow,
                ExecutionResult = result,
                ApplicationVersion = appVer,
                SenseNetVersion = prodVer,
                ExecutionError = execError
            };
        }
        private static void UpdatePackage(Package package, Manifest manifest, ExecutionResult result, Exception execError)
        {
            var snInfo = RepositoryVersionInfo.Instance.OfficialSenseNetVersion;
            var prodVer = (snInfo == null) ? new Version(42, 42, 42, 42) : snInfo.Version;

            Version appVer = null;

            if (manifest.Level != ContentRepository.Storage.PackageLevel.Tool)
            {
                if (manifest.Type == ContentRepository.Storage.PackageType.Product)
                    prodVer = manifest.VersionControl.Target;
                else
                    appVer = manifest.VersionControl.Target;
            }

            package.Name = manifest.Name;
            package.Edition = manifest.Edition;
            package.Description = manifest.Description;
            package.ReleaseDate = manifest.ReleaseDate;
            package.PackageLevel = manifest.Level;
            package.PackageType = manifest.Type;
            package.AppId = manifest.AppId;
            package.ExecutionDate = DateTime.UtcNow;
            package.ExecutionResult = result;
            package.ExecutionError = execError;
            package.ApplicationVersion = appVer;
            package.SenseNetVersion = prodVer;
        }

        public static string GetHelp()
        {
            var memory = new List<string>();

            var sb = new StringBuilder();
            sb.AppendLine("Available step types and parameters");
            sb.AppendLine("-----------------------------------");
            foreach (var item in SenseNet.Packaging.Steps.Step.StepTypes)
            {
                var stepType = item.Value;
                if (memory.Contains(stepType.FullName))
                    continue;
                memory.Add(stepType.FullName);

                var step = (SenseNet.Packaging.Steps.Step)Activator.CreateInstance(stepType);
                sb.AppendLine(step.ElementName + " (" + stepType.FullName + ")");
                foreach (var property in stepType.GetProperties())
                {
                    if (property.Name == "StepId" || property.Name == "ElementName")
                        continue;
                    var isDefault = property.GetCustomAttributes(true).Any(x => x is SenseNet.Packaging.Steps.DefaultPropertyAttribute);
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
    internal class PackageSchemaGenerator
    {
        #region xml source

        private const string XSD = @"<?xml version=""1.0"" encoding=""utf-8""?>
<xs:schema id=""PackageSchema""
    elementFormDefault=""unqualified""
    xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""Package"">
    <xs:complexType>
      <xs:all>
        <xs:element name=""Name"" type=""xs:string"" minOccurs=""1"" maxOccurs=""1""/>
        <xs:element name=""Description"" type=""xs:string"" minOccurs=""0"" maxOccurs=""1""/>
        <xs:element name=""Edition"" type=""xs:string"" minOccurs=""0"" maxOccurs=""1""/>
        <xs:element name=""AppId"" type=""xs:string"" minOccurs=""0"" maxOccurs=""1""/>
        <xs:element name=""ReleaseDate"" type=""xs:string"" minOccurs=""1"" maxOccurs=""1""/>
        <xs:element name=""VersionControl"" type=""VersionControl"" minOccurs=""0"" maxOccurs=""1""/>
        <xs:element name=""SuccessMessage"" type=""xs:string"" minOccurs=""0"" maxOccurs=""1""/>
        <xs:element name=""WarningMessage"" type=""xs:string"" minOccurs=""0"" maxOccurs=""1""/>
        <xs:element name=""ErrorMessage"" type=""xs:string"" minOccurs=""0"" maxOccurs=""1""/>
        <xs:element name=""Steps"" type=""MainBlock"" minOccurs=""0"" maxOccurs=""1""/>
      </xs:all>
      <xs:attribute name=""type"" type=""PackageType"" use=""required"" />
      <xs:attribute name=""level"" type=""PackageLevel"" use=""required"" />
    </xs:complexType>
  </xs:element>
  <xs:simpleType name=""PackageType"">
    <xs:restriction base=""xs:string"">
      <xs:enumeration value=""Product"" />
      <xs:enumeration value=""Application"" />
    </xs:restriction>
  </xs:simpleType>
  <xs:simpleType name=""PackageLevel"">
    <xs:restriction base=""xs:string"">
      <xs:enumeration value=""Tool"" />
      <xs:enumeration value=""Patch"" />
      <xs:enumeration value=""ServicePack"" />
      <xs:enumeration value=""Upgrade"" />
      <xs:enumeration value=""Install"" />
    </xs:restriction>
  </xs:simpleType>
  <xs:complexType name=""VersionControl"">
    <xs:attribute name=""target"" type=""VersionNumber"" use=""required""/>
    <xs:attribute name=""expected"" type=""VersionNumber"" use=""optional""/>
    <xs:attribute name=""expectedMin"" type=""VersionNumber"" use=""optional""/>
    <xs:attribute name=""expectedMax"" type=""VersionNumber"" use=""optional""/>
  </xs:complexType>
  <xs:simpleType name=""VersionNumber"">
    <xs:restriction base=""xs:string"">
      <xs:pattern value=""{0}""/>
    </xs:restriction>
  </xs:simpleType>
  <xs:complexType name=""AnyXml"">
    <xs:sequence>
      <xs:any minOccurs=""0"" maxOccurs=""unbounded"" processContents=""skip"" />
    </xs:sequence>
  </xs:complexType>
  <xs:complexType name=""MainBlock"">
    <xs:complexContent mixed=""false"">
      <xs:extension base=""StepBlock"">
        <xs:sequence>
          <xs:element name=""Phase"" minOccurs=""0"" maxOccurs=""unbounded"" type=""StepBlock""/>
        </xs:sequence>
      </xs:extension>
    </xs:complexContent>
  </xs:complexType>
  <xs:complexType name=""StepBlock"">
    <xs:choice minOccurs=""0"" maxOccurs=""unbounded"">
{1}
    </xs:choice>
  </xs:complexType>
  <xs:complexType name=""Empty"">
  </xs:complexType>
  <!-- Enum types -->
{2}  <!-- Step types -->
{3}
</xs:schema>
";

        private const string STEPHEADER = @"      <xs:element name=""{0}"" type=""{1}"" />";

        private const string STEPTEMPLATE = @"  <xs:complexType name=""{0}"">
{1}    <xs:complexContent mixed=""true"">
      <xs:extension base=""Empty"">
        <xs:choice minOccurs=""0"" maxOccurs=""unbounded"">
{2}        </xs:choice>
{3}      </xs:extension>
    </xs:complexContent>
  </xs:complexType>";

        private const string STEPANNOTATION = @"    <xs:annotation>
      <xs:documentation>{0}</xs:documentation>
    </xs:annotation>
";

        private const string PROPERTYELEMENTTEMPLATE = @"          <xs:element name=""{0}"" type=""{1}"">
{2}          </xs:element>";

        private const string ELEMENTANNOTATION = @"            <xs:annotation>
              <xs:documentation>{0}</xs:documentation>
            </xs:annotation>
";

        private const string PROPERTYATTRIBUTETEMPLATE = @"        <xs:attribute name=""{0}"" type=""{1}"">
{2}        </xs:attribute>";

        private const string ATTRIBUTEANNOTATION = @"          <xs:annotation>
            <xs:documentation>{0}</xs:documentation>
          </xs:annotation>
";

        private const string ENUMTEMPLATE = @"  <xs:simpleType name=""{0}"">
    <xs:restriction base=""xs:string"">
{1}    </xs:restriction>
  </xs:simpleType>
";

        private const string ENUMOPTIONTEMPLATE = @"      <xs:enumeration value=""{0}"" />
";

        #endregion

        [DebuggerDisplay("{Name} : {FullName}")]
        private class StepDescriptor
        {
            public string Name;
            public string FullName;
            public IEnumerable<StepPropertyDescriptor> Properties;
            public string Documentation;
        }
        [DebuggerDisplay("{Name} : {DataType} (isDefault: {IsDefault})")]
        private class StepPropertyDescriptor
        {
            public bool IsDefault;
            public string Name;
            public Type DataType;
            public bool CanBeAttribute;
            public bool CanBeElement;
            public string Documentation;
        }

        internal static string GenerateSchema()
        {
            var steps = GetStepDescriptors();
            var schema = GenerateSchema(steps);
            return schema;
        }
        private static IEnumerable<StepDescriptor> GetStepDescriptors()
        {
            var memory = new List<string>();
            var steps = new List<StepDescriptor>();
            foreach (var item in SenseNet.Packaging.Steps.Step.StepTypes)
            {
                var stepType = item.Value;
                if (memory.Contains(stepType.FullName))
                    continue;
                memory.Add(stepType.FullName);

                var step = (SenseNet.Packaging.Steps.Step)Activator.CreateInstance(stepType);

                string classDoc = null;
                var docAttr = (AnnotationAttribute)stepType.GetCustomAttributes(true).FirstOrDefault(x => x is AnnotationAttribute);
                if (docAttr != null)
                    classDoc = docAttr.Documentation;

                var properties = new List<StepPropertyDescriptor>();
                foreach (var property in stepType.GetProperties())
                {
                    if (property.Name == "StepId" || property.Name == "ElementName")
                        continue;

                    var isStepBlock = property.PropertyType == typeof(IEnumerable<XmlElement>);

                    var attrs = property.GetCustomAttributes(true);
                    var isDefault = attrs.Any(x => x is DefaultPropertyAttribute);

                    string propDoc = null;
                    docAttr = (AnnotationAttribute)attrs.FirstOrDefault(x => x is AnnotationAttribute);
                    if (docAttr != null)
                        propDoc = docAttr.Documentation;

                    var isXmlFragment = attrs.Any(x => x is XmlFragmentAttribute);

                    properties.Add(new StepPropertyDescriptor
                    {
                        Name = property.Name,
                        DataType = isXmlFragment ? typeof(XmlFragmentAttribute) : property.PropertyType,
                        IsDefault = isDefault,
                        CanBeAttribute = !isStepBlock && !isXmlFragment,
                        CanBeElement = true,
                        Documentation = propDoc
                    });
                }
                steps.Add(new StepDescriptor { Name = item.Key, FullName = stepType.FullName, Properties = properties, Documentation = classDoc });
            }
            return steps;
        }

        private static string GenerateSchema(IEnumerable<StepDescriptor> steps)
        {
            var stepHeaders = string.Join(Environment.NewLine, steps.Select(s => String.Format(STEPHEADER, s.Name, s.Properties.Count() == 0 ? "Empty" : s.Name)));
            var stepTypes = string.Join(Environment.NewLine, steps.Select(s=>String.Format(STEPTEMPLATE
                , s.Name
                , String.IsNullOrEmpty(s.Documentation)?"":String.Format(STEPANNOTATION, s.Documentation)
                , GetStepElements(s)
                , GetStepAttributes(s)
                )));
            var enumTypes = string.Join(string.Empty, _enumTypes.Select(t => String.Format(ENUMTEMPLATE
                , t.Name
                , string.Join(string.Empty, Enum.GetNames(t).Select(o => String.Format(ENUMOPTIONTEMPLATE, o)))
                )));
            var schema = String.Format(PackageSchemaGenerator.XSD, @"\d+(\.\d+){0,3}", stepHeaders, enumTypes, stepTypes);
            return schema;
        }
        private static string GetStepElements(StepDescriptor step)
        {
            var s = String.Join(Environment.NewLine, step.Properties
                .Where(p => p.CanBeElement)
                .Select(p => String.Format(PROPERTYELEMENTTEMPLATE
                    , p.Name
                    , GetDataType(p.DataType)
                    , String.IsNullOrEmpty(p.Documentation) ? "" : String.Format(ELEMENTANNOTATION, p.Documentation)
                )));
            return s;
        }
        private static string GetStepAttributes(StepDescriptor step)
        {
            var s = String.Join(Environment.NewLine, step.Properties
                .Where(p => p.CanBeAttribute)
                .Select(p => String.Format(PROPERTYATTRIBUTETEMPLATE
                    , PackageManager.ToCamelCase(p.Name)
                    , GetDataType(p.DataType)
                    , String.IsNullOrEmpty(p.Documentation) ? "" : String.Format(ATTRIBUTEANNOTATION, p.Documentation)
                )));
            return s;
        }
        private static List<Type> _enumTypes = new List<Type>();
        private static string GetDataType(Type type)
        {
            if (type == typeof(Int32))
                return "xs:integer";
            if (type == typeof(bool))
                return "xs:boolean";
            if (type == typeof(IEnumerable<XmlElement>))
                return "StepBlock";
            if (type == typeof(XmlFragmentAttribute))
                return "AnyXml";
            if (type.IsEnum)
            {
                if (!_enumTypes.Any(t => t == type))
                    _enumTypes.Add(type);
                return type.Name;
            }
            return "xs:string";
        }
    }
}
