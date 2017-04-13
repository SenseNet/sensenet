﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository;

namespace SenseNet.Packaging
{
    public class Manifest
    {
        public static readonly string SystemComponentId = "SenseNet.Services";

        public PackageType PackageType { get; private set; }
        public bool SystemInstall { get; private set; }
        public string ComponentId { get; private set; }
        public string Description { get; private set; }
        public DateTime ReleaseDate { get; private set; }
        public IEnumerable<Dependency> Dependencies { get; private set; }
        public Version Version { get; private set; }
        internal Dictionary<string, string> Parameters { get; private set; }
        internal XmlDocument ManifestXml { get; private set; }

        private List<List<XmlElement>> _phases;
        public int CountOfPhases { get { return _phases.Count; } }

        internal static Manifest Parse(string path, int phase, bool log, bool forcedReinstall = false)
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
            return Parse(xml, phase, log, forcedReinstall);
        }
        /// <summary>Test entry</summary>
        internal static Manifest Parse(XmlDocument xml, int currentPhase, bool log, bool forcedReinstall = false)
        {
            var manifest = new Manifest();
            manifest.ManifestXml = xml;

            ParseHead(xml, manifest);
            manifest.CheckPrerequisits(forcedReinstall, log);
            ParseParameters(xml, manifest);
            ParseSteps(xml, manifest, currentPhase);

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

        private void CheckPrerequisits(bool forcedReinstall, bool log)
        {
            if (log)
            {
                Logger.LogMessage("ComponentId: {0}", this.ComponentId);
                Logger.LogMessage("PackageType:   " + this.PackageType);
                Logger.LogMessage("Package version: " + this.Version);
                if (SystemInstall)
                    Logger.LogMessage(forcedReinstall ? "FORCED REINSTALL" : "SYSTEM INSTALL");
            }

            var versionInfo = RepositoryVersionInfo.Instance;
            var existingComponentInfo = versionInfo.Components.FirstOrDefault(a => a.ComponentId == ComponentId && a.AcceptableVersion != null);

            if (PackageType == PackageType.Install)
            {
                if (!(forcedReinstall && SystemInstall) && existingComponentInfo != null)
                    throw new PackagePreconditionException(string.Format(SR.Errors.Precondition.CannotInstallExistingComponent1, this.ComponentId),
                        PackagingExceptionType.CannotInstallExistingComponent);
            }
            else if (PackageType != PackageType.Tool)
            {
                if (existingComponentInfo == null)
                    throw new PackagePreconditionException(string.Format(SR.Errors.Precondition.CannotUpdateMissingComponent1, this.ComponentId),
                        PackagingExceptionType.CannotUpdateMissingComponent);
                if (existingComponentInfo.AcceptableVersion >= this.Version)
                    throw new PackagePreconditionException(string.Format(SR.Errors.Precondition.TargetVersionTooSmall2, this.Version, existingComponentInfo.Version),
                        PackagingExceptionType.TargetVersionTooSmall);
            }

            if (log && this.Dependencies.Any())
                Logger.LogMessage("Dependencies:");
            foreach (var dependency in this.Dependencies)
                CheckDependency(dependency, versionInfo, log);
        }
        private void CheckDependency(Dependency dependency, RepositoryVersionInfo versionInfo, bool log)
        {
            var existingComponent = versionInfo.Components.FirstOrDefault(a => a.ComponentId == dependency.Id);
            if (existingComponent == null)
                throw new PackagePreconditionException(string.Format(SR.Errors.Precondition.DependencyNotFound1, dependency.Id),
                    PackagingExceptionType.DependencyNotFound);

            var current = existingComponent.AcceptableVersion;
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
    }
}
