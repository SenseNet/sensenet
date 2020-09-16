using System;
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
        private ComponentInfo _data;

        /// <summary>
        /// Gets the unique identifier.
        /// </summary>
        public string ComponentId => _data?.ComponentId;

        /// <summary>
        /// Gets the last version after successful execution of the installer or patch.
        /// </summary>
        public Version Version => _data?.Version;

        /// <summary>
        /// Gets the description after successful execution of the installer.
        /// The descriptions of patches do not appear here.
        /// </summary>
        public string Description => _data?.Description;

        /// <summary>
        /// Gets the component's dependencies.
        /// </summary>
        public Dependency[] Dependencies => _dependencies ?? (_dependencies = ExtractDependencies(_data?.Manifest));

        /// <summary>
        /// Initializes a new instance of the <see cref="SnComponentDescriptor"/> class.
        /// Extracts the dependencies from the manifest of the the given <paramref name="componentInfo"/>.
        /// </summary>
        /// <param name="componentInfo">Contains base data of he component.</param>
        public SnComponentDescriptor(ComponentInfo componentInfo)
        {
            _data = componentInfo;
        }

        private Dependency[] _dependencies;
        private Dependency[] ExtractDependencies(string manifest)
        {
            if (string.IsNullOrEmpty(manifest))
                return null;
            var xml = new XmlDocument();
            xml.LoadXml(manifest);
            return Manifest.ParseDependencies(xml).Where(x => x.Id != ComponentId).ToArray();
        }

    }
}
