using System;
using System.Collections.Generic;
using System.Diagnostics;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    public class PatchContext
    {
        public RepositoryStartSettings Settings { get; set; }
    }

    public interface ISnPatch
    {
        /// <summary>
        /// Gets or sets the Id of the package / component.
        /// </summary>
        string Id { get; set; }
        /// <summary>
        /// Gets or sets the release date of the patch.
        /// </summary>
        DateTime ReleaseDate { get; set; }
        /// <summary>
        /// Gets or sets the version of the patch.
        /// </summary>
        Version Version { get; set; }
        /// <summary>
        /// Gets or sets a dependency array if there is any. Otherwise null.
        /// </summary>
        IEnumerable<Dependency> Dependencies { get; set; }
        /// <summary>
        /// Gets or sets the function that will be executed if allowed.
        /// </summary>
        Func<PatchContext, ExecutionResult> Execute { get; set; }
    }
    public class ComponentInstaller : ISnPatch
    {
        /// <inheritdoc/>
        public string Id { get; set; }
        /// <inheritdoc/>
        public DateTime ReleaseDate { get; set; }
        /// <inheritdoc/>
        public Version Version { get; set; }
        /// <inheritdoc/>
        public IEnumerable<Dependency> Dependencies { get; set; }
        /// <inheritdoc/>
        public Func<PatchContext, ExecutionResult> Execute { get; set; }
    }

    /// <summary>
    /// Represents a patch that will be executed only if the current component version
    /// is lower than the supported version of the component and it is between the defined 
    /// minimum and maximum version numbers in this patch. The component version after 
    /// this patch will be the one defined in the Version property.
    /// </summary>
    [DebuggerDisplay("{Id} {Version}")]
    public class SnPatch : ComponentInstaller
    {
        /// <summary>
        /// Gets or sets a version interval that specifies the patch's relevance.
        /// </summary>
        public VersionBoundary Boundary { get; set; } = new VersionBoundary();

        /// <summary>
        /// Gets or sets the Boundary.MinVersion.
        /// This property is deprecated use the Boundary.MinVersion instead.
        /// </summary>
        [Obsolete("Use Boundary.MinVersion instead.")]
        public Version MinVersion { get => Boundary.MinVersion; set => Boundary.MinVersion = value; }

        /// <summary>
        /// Gets or sets the Boundary.MaxVersion.
        /// This property is deprecated use the Boundary.MaxVersion instead.
        /// </summary>
        [Obsolete("Use Boundary.MinVersion instead.")]
        public Version MaxVersion { get => Boundary.MaxVersion; set => Boundary.MaxVersion = value; }

        /// <summary>
        /// Gets or sets the Boundary.MinVersionIsExclusive.
        /// This property is deprecated use the Boundary.MinVersionIsExclusive instead.
        /// </summary>
        [Obsolete("Use Boundary.MinVersion instead.")]
        public bool MinVersionIsExclusive
        {
            get => Boundary.MinVersionIsExclusive;
            set => Boundary.MinVersionIsExclusive = value;
        }

        /// <summary>
        /// Gets or sets the Boundary.MaxVersionIsExclusive.
        /// This property is deprecated use the Boundary.MaxVersionIsExclusive instead.
        /// </summary>
        [Obsolete("Use Boundary.MaxVersionIsExclusive instead.")]
        public bool MaxVersionIsExclusive
        {
            get => Boundary.MaxVersionIsExclusive;
            set => Boundary.MaxVersionIsExclusive = value;
        }

        /// <summary>
        /// Patch definition in a manifest xml format.
        /// </summary>
        [Obsolete("Delete this functionality.")]
        public string Contents { get; set; }
    }

}
