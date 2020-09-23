using System;
using System.Diagnostics;
using SenseNet.ContentRepository.Storage;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    /// <summary>
    /// Represents a patch action that will be executed only if the current component version
    /// is lower than the supported version of the component and it is between the defined 
    /// minimum and maximum version numbers in this patch. The component version after 
    /// this patch will be the one defined in the Version property.
    /// </summary>
    [DebuggerDisplay("{ToString()}")]
    public class SnPatch : SnPatchBase
    {
        /// <summary>
        /// Gets the type of the patch. In this case PackageType.Patch
        /// </summary>
        public override PackageType Type => PackageType.Patch;

        /// <summary>
        /// Gets or sets a version interval that specifies the patch's relevance.
        /// </summary>
        public VersionBoundary Boundary { get; internal set; } = new VersionBoundary();

        /// <summary>
        /// Gets or sets the Boundary.MinVersion.
        /// This property is deprecated use the Boundary.MinVersion instead.
        /// </summary>
        [Obsolete("Use Boundary.MinVersion instead.")]
        public Version MinVersion { get => Boundary.MinVersion; internal set => Boundary.MinVersion = value; }

        /// <summary>
        /// Gets or sets the Boundary.MaxVersion.
        /// This property is deprecated use the Boundary.MaxVersion instead.
        /// </summary>
        [Obsolete("Use Boundary.MinVersion instead.")]
        public Version MaxVersion { get => Boundary.MaxVersion; internal set => Boundary.MaxVersion = value; }

        /// <summary>
        /// Gets or sets the Boundary.MinVersionIsExclusive.
        /// This property is deprecated use the Boundary.MinVersionIsExclusive instead.
        /// </summary>
        [Obsolete("Use Boundary.MinVersion instead.")]
        public bool MinVersionIsExclusive
        {
            get => Boundary.MinVersionIsExclusive;
            internal set => Boundary.MinVersionIsExclusive = value;
        }

        /// <summary>
        /// Gets or sets the Boundary.MaxVersionIsExclusive.
        /// This property is deprecated use the Boundary.MaxVersionIsExclusive instead.
        /// </summary>
        [Obsolete("Use Boundary.MaxVersionIsExclusive instead.")]
        public bool MaxVersionIsExclusive
        {
            get => Boundary.MaxVersionIsExclusive;
            internal set => Boundary.MaxVersionIsExclusive = value;
        }

        public override string ToString()
        {
            return $"{ComponentId}: {Boundary} --> {Version}";
        }
    }
}
