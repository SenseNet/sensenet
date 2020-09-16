using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Xml;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.Packaging
{
    public class Manifest
    {
        public static readonly string SystemComponentId = "SenseNet.Services";

        public PackageType PackageType { get; private set; }
        public bool SystemInstall { get; private set; }
        public bool MultipleExecutionAllowed { get; private set; } = true;
        public string ComponentId { get; private set; }
        public string Description { get; private set; }
        public DateTime ReleaseDate { get; private set; }
        public IEnumerable<Dependency> Dependencies { get; private set; }
        public Version Version { get; private set; }
        internal Dictionary<string, string> Parameters { get; private set; }
        internal XmlDocument ManifestXml { get; private set; }

        private List<List<XmlElement>> _phases;
        public int CountOfPhases { get { return _phases.Count; } }

        internal static Manifest Parse(string path, int phase, bool log, PackageParameter[] packageParameters, bool forcedReinstall = false, bool editConnectionString = false)
        {
            var xml = new XmlDocument();
            try
            {
                xml.Load(path);
            }
            catch (Exception e)
            {
                throw new PackagingException("Manifest parse error", e);
            }
            return Parse(xml, phase, log, packageParameters, forcedReinstall, editConnectionString);
        }
        /// <summary>Test entry</summary>
        internal static Manifest Parse(XmlDocument xml, int currentPhase, bool log, PackageParameter[] packageParameters, bool forcedReinstall = false, bool editConnectionString = false)
        {
            var manifest = new Manifest();
            manifest.ManifestXml = xml;

            ParseHead(xml, manifest);
            ParseParameters(xml, manifest);
            manifest.CheckPrerequisits(packageParameters, forcedReinstall, log, editConnectionString);
            ParseSteps(xml, manifest, currentPhase);

            return manifest;
        }
        /// <summary>
        /// For patching.
        /// </summary>
        public static Manifest Parse(XmlDocument xml)
        {
            var manifest = new Manifest {ManifestXml = xml};

            ParseHead(xml, manifest);

            manifest.Parameters = new Dictionary<string, string>();
            manifest._phases = new List<List<XmlElement>>();

            return manifest;
        }

        internal static void ParseHead(XmlDocument xml, Manifest manifest)
        {
            XmlElement e;
            XmlAttribute attr;

            // root element inspection (required element name)
            e = xml.DocumentElement;
            if (e == null || e.Name != "Package")
                throw new InvalidPackageException(SR.Errors.Manifest.WrongRootName,
                    PackagingExceptionType.WrongRootName);

            // parsing type (required, one of the tool, patch, or install)
            attr = e.Attributes["type"];
            if (attr == null)
                attr = e.Attributes["Type"];
            if (attr == null)
                throw new InvalidPackageException(SR.Errors.Manifest.MissingType,
                     PackagingExceptionType.MissingPackageType);
            PackageType packageType;
            if (!Enum.TryParse<PackageType>(attr.Value, true, out packageType))
                throw new InvalidPackageException(SR.Errors.Manifest.InvalidType,
                    PackagingExceptionType.InvalidPackageType);
            manifest.PackageType = packageType;

            // parsing multiple execution switch
            var multipleAttr = e.Attributes.Cast<XmlAttribute>()
                .SingleOrDefault(a => 
                string.Compare(a.Name, "multipleExecution", StringComparison.InvariantCultureIgnoreCase) == 0);
            var multipleText = multipleAttr?.Value;
            bool multipleValue;
            if (!string.IsNullOrWhiteSpace(multipleText) && bool.TryParse(multipleText, out multipleValue))
                manifest.MultipleExecutionAllowed = multipleValue;

            // parsing ComponentId
            e = (XmlElement)xml.DocumentElement.SelectSingleNode("Id");
            if (e != null)
            {
                if (e.InnerText.Length == 0)
                    throw new InvalidPackageException(SR.Errors.Manifest.InvalidComponentId,
                    PackagingExceptionType.InvalidComponentId);
                manifest.ComponentId = e.InnerText;
            }
            else
            {
                throw new InvalidPackageException(SR.Errors.Manifest.MissingComponentId,
                    PackagingExceptionType.MissingComponentId);
            }

            // parsing system install
            manifest.SystemInstall = manifest.ComponentId == SystemComponentId &&
                                     manifest.PackageType == PackageType.Install;

            // parsing description (optional)
            e = (XmlElement)xml.DocumentElement.SelectSingleNode("Description");
            if (e != null)
                manifest.Description = e.InnerText;

            // parsing version
            e = (XmlElement)xml.DocumentElement.SelectSingleNode("Version");
            if (e == null)
                throw new InvalidPackageException(SR.Errors.Manifest.MissingVersion,
                    PackagingExceptionType.MissingVersion);
            manifest.Version = Dependency.ParseVersion(e.InnerText);

            // parsing release date (required)
            e = (XmlElement)xml.DocumentElement.SelectSingleNode("ReleaseDate");
            if (e == null)
                throw new InvalidPackageException(SR.Errors.Manifest.MissingReleaseDate,
                    PackagingExceptionType.MissingReleaseDate);
            DateTime releaseDate;
            if (!DateTime.TryParse(e.InnerText, out releaseDate))
                throw new InvalidPackageException(SR.Errors.Manifest.InvalidReleaseDate,
                    PackagingExceptionType.InvalidReleaseDate);
            if (releaseDate > DateTime.UtcNow)
                throw new InvalidPackageException(SR.Errors.Manifest.TooBigReleaseDate,
                    PackagingExceptionType.TooBigReleaseDate);
            manifest.ReleaseDate = releaseDate;

            // parsing dependencies
            var dependencies = new List<Dependency>();
            e = (XmlElement)xml.DocumentElement.SelectSingleNode("Dependencies");
            if (e != null)
                foreach (XmlElement dependencyElement in e.SelectNodes("Dependency"))
                    dependencies.Add(Dependency.Parse(dependencyElement));
            manifest.Dependencies = dependencies.ToArray();
        }

        internal static Dependency[] ParseDependencies(XmlDocument xml)
        {
            var dependencies = new List<Dependency>();
            var e = (XmlElement)xml.DocumentElement.SelectSingleNode("Dependencies");
            if (e != null)
                foreach (XmlElement dependencyElement in e.SelectNodes("Dependency"))
                    dependencies.Add(Dependency.Parse(dependencyElement));
            return dependencies.ToArray();
        }

        private static void ParseParameters(XmlDocument xml, Manifest manifest)
        {
            var parameters = new Dictionary<string, string>();
            foreach (XmlElement parameterElement in xml.SelectNodes("/Package/Parameters/Parameter"))
            {
                var parameterName = parameterElement.Attributes["name"]?.Value;
                if (parameterName == null)
                    throw new InvalidParameterException("Missing parameter name.",
                        PackagingExceptionType.MissingParameterName);
                if (!parameterName.StartsWith("@"))
                    throw new InvalidParameterException("Parameter names must start with @.",
                        PackagingExceptionType.InvalidParameterName);

                var lowerCaseParameterName = parameterName.ToLowerInvariant();
                if (parameters.ContainsKey(lowerCaseParameterName))
                    throw new InvalidParameterException($"Duplicated parameter name:{parameterName}",
                        PackagingExceptionType.DuplicatedParameter);

                var defaultValue = parameterElement.InnerXml;

                // This is necessary to prevent the parse mechanism override default
                // values hardcoded into the source code. An empty Parameter xml
                // node means: "use the default value from the source code".
                if (defaultValue == string.Empty)
                    defaultValue = null;

                parameters.Add(lowerCaseParameterName, defaultValue);
            }

            manifest.Parameters = parameters;
        }

        private static void ParseSteps(XmlDocument xml, Manifest manifest, int currentPhase)
        {
            var stepsElement = (XmlElement)xml.DocumentElement.SelectSingleNode("Steps");
            var phases = new List<List<XmlElement>>();
            if (stepsElement != null)
            {
                var explicitPhases = stepsElement.SelectNodes("Phase");
                if (explicitPhases.Count == 0)
                {
                    phases.Add(ParsePhase(stepsElement));
                }
                else
                {
                    int p = 0;
                    foreach (XmlElement phaseElement in explicitPhases)
                    {
                        if (p++ == currentPhase)
                            phases.Add(ParsePhase(phaseElement));
                        else
                            phases.Add(null);
                    }
                }
            }
            if (phases.Count == 0)
                phases.Add(new List<XmlElement>());
            manifest._phases = phases;
        }
        private static List<XmlElement> ParsePhase(XmlElement phaseElement)
        {
            return phaseElement.SelectNodes("*").Cast<XmlElement>().ToList();
        }

        public List<XmlElement> GetPhase(int index)
        {
            if (index < 0 || index > _phases.Count)
                throw new PackagingException(String.Format(SR.Errors.InvalidPhaseIndex_2, _phases.Count, index),
                    PackagingExceptionType.InvalidPhase);
            return _phases[index];
        }

        private void CheckPrerequisits(PackageParameter[] packageParameters, bool forcedReinstall, bool log, bool editConnectionString)
        {
            if (log)
            {
                Logger.LogMessage("ComponentId: {0}", this.ComponentId);
                Logger.LogMessage("PackageType:   " + this.PackageType);
                Logger.LogMessage("Package version: " + this.Version);
                if (SystemInstall)
                    Logger.LogMessage(forcedReinstall ? "FORCED REINSTALL" : "SYSTEM INSTALL");
            }

            if (SystemInstall && editConnectionString)
            {
                EditConnectionString(this.Parameters, packageParameters);
                RepositoryVersionInfo.Reset();
            }

            var versionInfo = RepositoryVersionInfo.Instance;
            var existingComponentInfo = versionInfo.Components.FirstOrDefault(a => a.ComponentId == ComponentId);

            if (PackageType == PackageType.Install)
            {
                if (!(forcedReinstall && SystemInstall) && existingComponentInfo != null)
                {
                    // Install packages can be executed multiple times only if it is 
                    // allowed in the package AND the version in the manifest is the
                    // same as in the db.
                    //UNDONE: PACKAGING Check this case by installed packages (that are unfinished or faulty)
                    if (!this.MultipleExecutionAllowed || existingComponentInfo.Version != this.Version)
                        throw new PackagePreconditionException(
                            string.Format(SR.Errors.Precondition.CannotInstallExistingComponent1, this.ComponentId),
                            PackagingExceptionType.CannotInstallExistingComponent);
                }
            }
            else if (PackageType != PackageType.Tool)
            {
                if (existingComponentInfo == null)
                    throw new PackagePreconditionException(string.Format(SR.Errors.Precondition.CannotUpdateMissingComponent1, this.ComponentId),
                        PackagingExceptionType.CannotUpdateMissingComponent);
                if (existingComponentInfo.Version >= this.Version)
                    throw new PackagePreconditionException(string.Format(SR.Errors.Precondition.TargetVersionTooSmall2, this.Version, existingComponentInfo.Version),
                        PackagingExceptionType.TargetVersionTooSmall);
            }

            if (log && this.Dependencies.Any())
                Logger.LogMessage("Dependencies:");
            foreach (var dependency in this.Dependencies)
                CheckDependency(dependency, versionInfo, log);
        }

        internal static void EditConnectionString(Dictionary<string, string> parameters, PackageParameter[] packageParameters)
        {
            string dataSource;
            if (!parameters.TryGetValue("@datasource", out dataSource))
                throw new PackagingException("Missing manifest parameter in system install: 'dataSource'");

            string initialCatalog;
            if (!parameters.TryGetValue("@initialcatalog", out initialCatalog))
                throw new PackagingException("Missing manifest parameter in system install: 'initialCatalog'");

            string userName;
            if (!parameters.TryGetValue("@username", out userName))
                throw new PackagingException("Missing manifest parameter in system install: 'userName'");

            string password ;
            if (!parameters.TryGetValue("@password", out password))
                throw new PackagingException("Missing manifest parameter in system install: 'password'");

            var defaultCnInfo = new ConnectionInfo
            {
                DataSource = dataSource,
                InitialCatalogName = initialCatalog,
                UserName = userName,
                Password = password
            };
            var inputCnInfo = new ConnectionInfo
            {
                DataSource = packageParameters.FirstOrDefault(x => string.Compare(x.PropertyName, "datasource", StringComparison.InvariantCultureIgnoreCase) == 0)?.Value,
                InitialCatalogName = packageParameters.FirstOrDefault(x => string.Compare(x.PropertyName, "initialcatalog", StringComparison.InvariantCultureIgnoreCase) == 0)?.Value,
                UserName = packageParameters.FirstOrDefault(x => string.Compare(x.PropertyName, "username", StringComparison.InvariantCultureIgnoreCase) == 0)?.Value,
                Password = packageParameters.FirstOrDefault(x => string.Compare(x.PropertyName, "password", StringComparison.InvariantCultureIgnoreCase) == 0)?.Value
            };

            var origCnStr = Configuration.ConnectionStrings.ConnectionString;
            var newCnStr = EditConnectionString(origCnStr, inputCnInfo, defaultCnInfo);
            if (newCnStr != origCnStr)
                Configuration.ConnectionStrings.ConnectionString = newCnStr;
        }

        internal static string EditConnectionString(string cnStr, ConnectionInfo inputCnInfo, ConnectionInfo defaultInfo)
        {
            var dataSource = inputCnInfo.DataSource ?? defaultInfo.DataSource;
            var initialCatalog = inputCnInfo.InitialCatalogName ?? defaultInfo.InitialCatalogName;
            var userName = inputCnInfo.UserName ?? defaultInfo.UserName;
            var password = inputCnInfo.Password ?? defaultInfo.Password;

            var connection = new SqlConnectionStringBuilder(cnStr);

            var changed = false;
            if (string.Compare(connection.UserID, userName, StringComparison.InvariantCulture) != 0)
            {
                if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
                {
                    connection.UserID = userName;
                    connection.Password = password;
                    connection.IntegratedSecurity = false;
                    changed = true;
                }
                else
                {
                    connection.Remove("User ID");
                    connection.Remove("Password");
                    connection.IntegratedSecurity = true;
                    changed = true;
                }
            }

            if (connection.DataSource != dataSource)
            {
                connection.DataSource = dataSource;
                changed = true;
            }

            if (connection.InitialCatalog != initialCatalog)
            {
                connection.InitialCatalog = initialCatalog;
                changed = true;
            }

            return changed ? connection.ConnectionString : cnStr;
        }

        internal void CheckDependency(Dependency dependency, RepositoryVersionInfo versionInfo, bool log)
        {
            var existingComponent = versionInfo.Components.FirstOrDefault(a => a.ComponentId == dependency.Id);
            if (existingComponent == null)
                throw new PackagePreconditionException(string.Format(SR.Errors.Precondition.DependencyNotFound1, dependency.Id),
                    PackagingExceptionType.DependencyNotFound);

            var current = existingComponent.Version;
            var min = dependency.MinVersion;
            var max = dependency.MaxVersion;
            var minEx = dependency.MinVersionIsExclusive;
            var maxEx = dependency.MaxVersionIsExclusive;

            if (log)
            {
                if (min != null && min == max)
                {
                    Logger.LogMessage($"  {dependency.Id}: {min} = {current} (current)");
                }
                else
                {
                    var minStr = "";
                    if (min != null)
                        minStr = $"{min} <{(minEx ? "" : "=")} ";
                    var maxStr = "";
                    if (max != null)
                        maxStr = $" <{(minEx ? "" : "=")} {max}";
                    Logger.LogMessage($"  {dependency.Id}: {minStr}{current} (current){maxStr}");
                }
            }

            if (min != null)
            {
                if (minEx && min >= current || !minEx && min > current)
                    throw new PackagePreconditionException(string.Format(SR.Errors.Precondition.MinimumVersion1, dependency.Id),
                        min == max ? PackagingExceptionType.DependencyVersion : PackagingExceptionType.DependencyMinimumVersion);
            }
            if (max != null)
            {
                if (maxEx && max <= current || !maxEx && max < current)
                    throw new PackagePreconditionException(string.Format(SR.Errors.Precondition.MaximumVersion1, dependency.Id),
                        min == max ? PackagingExceptionType.DependencyVersion : PackagingExceptionType.DependencyMaximumVersion);
            }

        }

        public string ToXmlString()
        {
            var sb = new StringBuilder();
            using (var xmlWriter = XmlWriter.Create(sb, new XmlWriterSettings {Indent = true, Async = false}))
                ManifestXml.WriteTo(xmlWriter);
            return sb.ToString();
        }
        public XmlDocument ToXml()
        {
            var cr = Environment.NewLine;

            var src = string.Empty;
            if (Dependencies != null && Dependencies.Any())
            {
                var depSrc = Dependencies.Select(d => $"    <Dependency id='{d.Id}'{GenerateVersionAttrs(d)} />");
                src =
                    $"  <Dependencies>" + cr +
                    string.Join(cr, depSrc) + cr +
                    $"  </Dependencies>";
            }

            src =
                $"<?xml version='1.0' encoding='utf-8'?>" + cr +
                $"<Package type='{PackageType}'>" + cr +
                $"  <Id>{ComponentId}</Id>" + cr +
                $"  <ReleaseDate>{ReleaseDate:yyyy-MM-dd}</ReleaseDate>" + cr +
                $"  <Version>{Version}</Version>" + cr +
                (string.IsNullOrEmpty(Description) ? "" : $"  <Description>{Description}</Description>") + cr +
                src + cr +
                $"</Package>" + cr;

            var xml = new XmlDocument();
            xml.LoadXml(src);
            return xml;
        }
        private string GenerateVersionAttrs(Dependency dep)
        {
            var b = dep.Boundary;

            var minVersion = b.MinVersion ?? VersionBoundary.DefaultMinVersion;
            var maxVersion = b.MaxVersion ?? VersionBoundary.DefaultMaxVersion;

            if (minVersion == maxVersion && !b.MinVersionIsExclusive && !b.MaxVersionIsExclusive)
                return $" version='{minVersion}'";

            var sb = new  StringBuilder();

            if (minVersion > VersionBoundary.DefaultMinVersion || b.MinVersionIsExclusive)
                sb.Append($" {(b.MinVersionIsExclusive ? "minVersionExclusive" : "minVersion")} ='{minVersion}'");
            if (maxVersion < VersionBoundary.DefaultMaxVersion || b.MaxVersionIsExclusive)
                sb.Append($" {(b.MaxVersionIsExclusive ? "maxVersionExclusive" : "maxVersion")} ='{maxVersion}'");

            return sb.ToString();
        }

        /// <summary>
        /// Creates a <c>Manifest</c> instance from the given <paramref name="package"/>
        /// </summary>
        /// <param name="package">Source <c>Package</c>.</param>
        /// <param name="dependencies">Dependency array. Default: null.</param>
        /// <param name="multipleExecutionAllowed">Dependency array. Default: null.</param>
        /// <returns></returns>
        public static Manifest Create(Package package, IEnumerable<Dependency> dependencies, bool multipleExecutionAllowed)
        {
            var manifest = new Manifest
            {
                PackageType = package.PackageType,
                MultipleExecutionAllowed = multipleExecutionAllowed,
                ComponentId = package.ComponentId,
                Description = package.Description,
                ReleaseDate = package.ReleaseDate,
                Dependencies = dependencies,
                Version = package.ComponentVersion,
                Parameters = new Dictionary<string, string>()
            };

            manifest.SystemInstall = manifest.ComponentId == SystemComponentId &&
                                     manifest.PackageType == PackageType.Install;

            manifest.ManifestXml = manifest.ToXml();

            return manifest;
        }

        /// <summary>
        /// Creates a <c>Manifest</c> instance from the given <paramref name="package"/>
        /// </summary>
        /// <param name="patch">Source <c>ISnPatch</c>.</param>
        /// <returns></returns>
        public static Manifest Create(ISnPatch patch)
        {
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

            var manifest = new Manifest
            {
                PackageType = patch.Type,
                ComponentId = patch.ComponentId,
                Description = patch.Description,
                ReleaseDate = patch.ReleaseDate,
                Dependencies = dependencies,
                Version = patch.Version,
                Parameters = new Dictionary<string, string>()
            };

            manifest.SystemInstall = manifest.ComponentId == SystemComponentId &&
                                     manifest.PackageType == PackageType.Install;

            manifest.ManifestXml = manifest.ToXml();

            return manifest;
        }
    }
}
