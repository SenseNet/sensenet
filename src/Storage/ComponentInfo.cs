using System;

namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Represents a software element in the sensenet ecosystem that can be installed and patched automatically.
    /// </summary>
    public class ComponentInfo
    {
        /// <summary>
        /// Gets or sets the unique identifier of the component.
        /// </summary>
        public string ComponentId { get; set; }
        /// <summary>
        /// Gets or sets the last saved version. This value is independent of the success of the installation.
        /// </summary>
        public Version Version { get; set; }
        /// <summary>
        /// Gets or sets the last successfully installed version.
        /// </summary>
        public Version AcceptableVersion { get; set; }
        /// <summary>
        /// Gets or sets the description of the component.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Represents a not installed component.
        /// </summary>
        public static readonly ComponentInfo Empty = new ComponentInfo
        {
            ComponentId = string.Empty,
            Version = new Version(0, 0),
            AcceptableVersion = null,
            Description = string.Empty
        };
    }
}
