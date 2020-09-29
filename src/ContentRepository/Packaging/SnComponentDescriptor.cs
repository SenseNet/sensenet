using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Storage;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    /// <summary>
    /// Represents a software element in the sensenet ecosystem that can be installed and patched automatically.
    /// This is a business logic level class that wraps a <see cref="ComponentInfo"/> instance.
    /// </summary>
    public class SnComponentDescriptor
    {
        /// <summary>
        /// Gets the unique identifier.
        /// </summary>
        public string ComponentId { get; }

        /// <summary>
        /// Gets the last version after successful execution of the installer or patch.
        /// </summary>
        public Version Version { get; internal set; }

        /// <summary>
        /// Gets the description after successful execution of the installer.
        /// The descriptions of patches do not appear here.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the component's dependencies.
        /// </summary>
        public Dependency[] Dependencies { get; internal set; }

        /// <summary>
        /// Gets or sets temporary version in execution before repository start.
        /// </summary>
        [JsonIgnore]
        internal Version FaultyBeforeVersion { get; set; }
        /// <summary>
        /// Gets or sets temporary version in execution after repository start.
        /// </summary>
        [JsonIgnore]
        internal Version FaultyAfterVersion { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnComponentDescriptor"/> class.
        /// Extracts the dependencies from the manifest of the the given <paramref name="componentInfo"/>.
        /// </summary>
        /// <param name="componentInfo">Contains base data of he component.</param>
        public SnComponentDescriptor(ComponentInfo componentInfo)
        {
            ComponentId = componentInfo.ComponentId;
            Version = componentInfo.Version;
            Description = componentInfo.Description;
            Dependencies = ExtractDependencies(componentInfo.Manifest);
        }
        internal SnComponentDescriptor(string componentId, Version version, string description, Dependency[] dependencies)
        {
            ComponentId = componentId;
            Version = version;
            Description = description;
            Dependencies = dependencies;
        }

        private Dependency[] ExtractDependencies(string manifest)
        {
            if (string.IsNullOrEmpty(manifest))
                return null;
            var xml = new XmlDocument();
            xml.LoadXml(manifest);
            return Manifest.ParseDependencies(xml).Where(x => x.Id != ComponentId).ToArray();
        }

        internal static List<SnComponentDescriptor> CreateComponents(IEnumerable<ComponentInfo> installed, IEnumerable<ComponentInfo> faultyList)
        {
            var result = installed.Select(x=>new SnComponentDescriptor(x)).ToList();

            foreach (var faulty in faultyList)
            {
                var existing = result.FirstOrDefault(x => x.ComponentId == faulty.ComponentId);
                if (existing == null)
                {
                    existing = new SnComponentDescriptor(faulty);
                    result.Add(existing);
                    existing.Version = null;
                }

                switch (faulty.ExecutionResult)
                {
                    case ExecutionResult.Successful:
                        break;
                    case ExecutionResult.SuccessfulBefore:
                    case ExecutionResult.Faulty:
                        existing.FaultyAfterVersion = faulty.Version;
                        break;
                    case ExecutionResult.Unfinished:
                    case ExecutionResult.FaultyBefore:
                        existing.FaultyBeforeVersion = faulty.Version;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return result;
        }
    }
}
