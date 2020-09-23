using System;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Represents a software element in the sensenet ecosystem that can be installed and patched automatically.
    /// This is a storage level class.
    /// </summary>
    [DebuggerDisplay("{" + nameof(ToString) + "()}")]
    public class ComponentInfo
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public string ComponentId { get; set; }
        /// <summary>
        /// Gets or sets the last version after successful execution of the installer or patch.
        /// </summary>
        public Version Version { get; set; }
        /// <summary>
        /// Gets or sets the description after successful execution of the installer.
        /// The descriptions of patches do not appear here.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Gets or sets the last manifest after successful execution of the installer or patch.
        /// </summary>
        public string Manifest { get; set; }

        /// <summary>
        /// Represents a not installed component.
        /// </summary>
        public static readonly ComponentInfo Empty = new ComponentInfo
        {
            ComponentId = string.Empty,
            Version = null,
            Description = string.Empty
        };

        /// <summary>
        /// String representation of this ComponentInfo instance.
        /// </summary>
        public override string ToString()
        {
            return $"{ComponentId} v{Version}";
        }
    }
}
