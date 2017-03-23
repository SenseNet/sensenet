using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using SenseNet.Packaging.Steps;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository;

namespace SenseNet.Packaging
{
    public class Manifest
    {
        public PackageLevel Level { get; private set; }
        public PackageType Type { get; private set; }
        public string Name { get; private set; }
        public string AppId { get; private set; }
        public string Description { get; private set; }
        public DateTime ReleaseDate { get; private set; }
        public VersionControl VersionControl { get; private set; }
        internal Dictionary<string, string> Parameters { get; private set; }

        private List<List<XmlElement>> _phases;
        public int CountOfPhases { get { return _phases.Count; } }

        internal static Manifest Parse(string path, int phase, bool log)
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
            return Parse(xml, phase, log);
        }
        /// <summary>Test entry</summary>
        private static Manifest Parse(XmlDocument xml, int currentPhase, bool log)
        {
            var manifest = new Manifest();

            ParseHead(xml, manifest);
            manifest.CheckPrerequisits(log);
            ParseParameters(xml, manifest);
            ParseSteps(xml, manifest, currentPhase);

            return manifest;
        }

        private static void ParseHead(XmlDocument xml, Manifest manifest)
        {
            XmlElement e;
            XmlAttribute attr;

            // root element inspection (required element name)
            e = xml.DocumentElement;
            if(e.Name != "Package")
                throw new InvalidPackageException(SR.Errors.Manifest.WrongRootName);

            // parsing type (required, product or application)
            attr = e.Attributes["type"];
            if (attr == null)
                attr = e.Attributes["Type"];
            if (attr == null)
                throw new InvalidPackageException(SR.Errors.Manifest.MissingType);
            PackageType type;
            if (!Enum.TryParse<PackageType>(attr.Value, true, out type))
                throw new InvalidPackageException(SR.Errors.Manifest.InvalidType);
            manifest.Type = type;

            // parsing level (required, one of the tool, patch, servicepack or upgrade)
            attr = e.Attributes["level"];
            if (attr == null)
                attr = e.Attributes["Level"];
            if (attr == null)
                throw new InvalidPackageException(SR.Errors.Manifest.MissingLevel);
            PackageLevel level;
            if(!Enum.TryParse<PackageLevel>(attr.Value, true, out level))
                throw new InvalidPackageException(SR.Errors.Manifest.InvalidLevel);
            manifest.Level = level;

            // parsing application name (required if the "type" is "application")
            e = (XmlElement)xml.DocumentElement.SelectSingleNode("AppId");
            if (e != null)
            {
                if (e.InnerText.Length == 0)
                    throw new InvalidPackageException(SR.Errors.Manifest.InvalidAppId);
                else
                    manifest.AppId = e.InnerText;
            }
            else
            {
                if(type == PackageType.Application)
                    throw new InvalidPackageException(SR.Errors.Manifest.MissingAppId);
            }

            // parsing name (required)
            e = (XmlElement)xml.DocumentElement.SelectSingleNode("Name");
            if (e == null)
                throw new InvalidPackageException(SR.Errors.Manifest.MissingName);
            manifest.Name = e.InnerText;
            if (String.IsNullOrEmpty(manifest.Name))
                throw new InvalidPackageException(SR.Errors.Manifest.InvalidName);

            // parsing description (optional)
            e = (XmlElement)xml.DocumentElement.SelectSingleNode("Description");
            if (e != null)
                manifest.Description = e.InnerText;

            // parsing version control
            e = (XmlElement)xml.DocumentElement.SelectSingleNode("VersionControl");
            if (level != PackageLevel.Tool && e == null)
                throw new InvalidPackageException(SR.Errors.Manifest.MissingVersionControl);
            manifest.VersionControl = VersionControl.Initialize(e, level, type);

            // parsing release date (required)
            e = (XmlElement)xml.DocumentElement.SelectSingleNode("ReleaseDate");
            if (e == null)
                throw new InvalidPackageException(SR.Errors.Manifest.MissingReleaseDate);
            DateTime releaseDate;
            if (!DateTime.TryParse(e.InnerText, out releaseDate))
                throw new InvalidPackageException(SR.Errors.Manifest.InvalidReleaseDate);
            if(releaseDate > DateTime.UtcNow)
                throw new InvalidPackageException(SR.Errors.Manifest.InvalidReleaseDate);
            manifest.ReleaseDate = releaseDate;
        }

        private static void ParseParameters(XmlDocument xml, Manifest manifest)
        {
            var parameters = new Dictionary<string, string>();
            foreach (XmlElement parameterElement in xml.SelectNodes("/Package/Parameters/Parameter"))
            {
                var parameterName = parameterElement.Attributes["name"]?.Value;
                if(parameterName == null)
                    throw new InvalidParameterException("Missing parameter name.");
                if (!parameterName.StartsWith("@"))
                    throw new InvalidParameterException("Parameter names must start with @.");

                var lowerCaseParameterName = parameterName.ToLowerInvariant();
                if(parameters.ContainsKey(lowerCaseParameterName))
                    throw new InvalidParameterException($"Duplicated parameter name:{parameterName}");

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

        public List<XmlElement > GetPhase(int index)
        {
            if (index < 0 || index > _phases.Count)
                throw new PackagingException(String.Format(SR.Errors.InvalidPhaseIndex_2, _phases.Count, index));
            return _phases[index];
        }

        private void CheckPrerequisits(bool log)
        {
            if (log)
            {
                Logger.LogMessage("Name:    " + this.Name);
                Logger.LogMessage("Type:    " + this.Type);
                Logger.LogMessage("Level:   " + this.Level);
                if (this.Level != PackageLevel.Tool)
                    Logger.LogMessage("Package version: " + this.VersionControl.Target);
                if (this.Type == PackageType.Application)
                    Logger.LogMessage("AppId: {0}", this.AppId);
            }

            if (Level == PackageLevel.Install)
            {
                // Workaround for creating an in-memory version info in case the
                // database does not exist yet (or will be overwritten anyway).
                if (Type == PackageType.Product)
                    RepositoryVersionInfo.SetInitialVersion(new ApplicationInfo
                    {
                        Name = this.Name,
                        Version = this.VersionControl.Target,
                        Description = this.Description
                    });

                CheckInstall(RepositoryVersionInfo.Instance, log);
            }
            else
            {
                CheckUpdate(RepositoryVersionInfo.Instance, log);
            }
        }
        private void CheckInstall(RepositoryVersionInfo versionInfo, bool log)
        {
            if (versionInfo.Applications.FirstOrDefault(a => a.AppId == AppId) != null)
                throw new PackagePreconditionException(SR.Errors.Precondition.CannotInstallExistingApp);
        }
        private void CheckUpdate(RepositoryVersionInfo versionInfo, bool log)
        {
            Version current = null;
            Version min = null;
            Version max = null;
            switch (this.Type)
            {
                case PackageType.Product:
                    if (null != this.AppId)
                        throw new InvalidPackageException(SR.Errors.Manifest.UnexpectedAppId);
                    current = versionInfo.OfficialSenseNetVersion.AcceptableVersion;
                    min = VersionControl.ExpectedProductMinimum;
                    max = VersionControl.ExpectedProductMaximum;
                    break;
                case PackageType.Application:
                    var existingApplication = versionInfo.Applications.FirstOrDefault(a => a.AppId == this.AppId);
                    if (existingApplication == null)
                        throw new PackagePreconditionException(SR.Errors.Precondition.AppIdDoesNotMatch);
                    current = existingApplication.AcceptableVersion;
                    min = VersionControl.ExpectedApplicationMinimum;
                    max = VersionControl.ExpectedApplicationMaximum;
                    break;
                default:
                    throw new SnNotSupportedException("Unknown PackageType: " + this.Type);
            }

            if (log)
            {
                Logger.LogMessage("Current version: {0}", current);
                if (min != null && min == max)
                {
                    Logger.LogMessage("Expected version: {0}", min);
                }
                else
                {
                    if (min != null)
                        Logger.LogMessage("Expected minimum version: {0}", min);
                    if (max != null)
                        Logger.LogMessage("Expected maximum version: {0}", max);
                }
            }

            if (min != null && min > current)
                throw new PackagePreconditionException(String.Format(SR.Errors.Precondition.MinimumVersion_1, this.Type.ToString().ToLower()));
            if (max != null && max < current)
                throw new PackagePreconditionException(String.Format(SR.Errors.Precondition.MaximumVersion_1, this.Type.ToString().ToLower()));

            if(Level != PackageLevel.Tool)
                if (current >= VersionControl.Target)
                    throw new PackagePreconditionException(String.Format(SR.Errors.Precondition.TargetVersionTooSmall_3, this.Type.ToString().ToLower(), VersionControl.Target, current));
        }
    }
}
