using System;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Represents a software element in the sensenet ecosystem that can be installed and patched automatically.
    /// </summary>
    [DebuggerDisplay("{" + nameof(ToString) + "()}")]
    public class ComponentInfo
    {
        /// <summary>
        /// Gets or sets the unique identifier of the component.
        /// </summary>
        public string ComponentId { get; set; }
        /// <summary>
        /// Gets or sets the last successfully installed version.
        /// </summary>
        public Version Version { get; set; }
        /// <summary>
        /// Gets or sets the description of the component.
        /// </summary>
        public string Description { get; set; }

        //UNDONE: Missing property ComponentInfo.Dependencies

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
