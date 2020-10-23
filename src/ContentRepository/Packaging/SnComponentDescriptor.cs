using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    [DebuggerDisplay("{ToString()}")]
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
        /// Gets or sets the temporary version when the state is "Before". Used by patching operation.
        /// </summary>
        [JsonIgnore]
        internal Version TempVersionBefore { get; set; }
        /// <summary>
        /// Gets or sets the temporary version when the state is "After". Used by patching operation.
        /// </summary>
        [JsonIgnore]
        internal Version TempVersionAfter { get; set; }
        /// <summary>
        /// Gets or sets the installation state of the temporary version used by patching operation.
        /// </summary>
        [JsonIgnore]
        internal ExecutionResult State { get; set; }

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
                else
                {
                    // 1 - If a component is "existing" the actual version is: existing.Version.
                    // 2 - The "faulty" is a ComponentInfo so its version is: faulty.Version.
                    // 3 - Only newer faulty is relevant.
                    if (existing.Version >= faulty.Version)
                        continue;
                }

                switch (faulty.ExecutionResult)
                {
                    case ExecutionResult.Successful:
                    case ExecutionResult.Faulty:
                        existing.TempVersionAfter = faulty.Version;
                        break;
                    case ExecutionResult.Unfinished:
                    case ExecutionResult.FaultyBefore:
                    case ExecutionResult.SuccessfulBefore:
                        existing.TempVersionBefore = faulty.Version;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                existing.State = faulty.ExecutionResult;
            }

            return result;
        }

        public override string ToString()
        {
            return $"{ComponentId}v{Version}({TempVersionBefore},{TempVersionAfter},{State})";
        }
    }
}
