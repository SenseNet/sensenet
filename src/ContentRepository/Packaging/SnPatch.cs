using System;
using System.Collections.Generic;
using System.Diagnostics;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    public interface ISnPatch
    {
        /// <summary>
        /// Gets the Id of the package / component.
        /// </summary>
        string ComponentId { get; }
        /// <summary>
        /// Gets the type of the patch (Install, Patch, Tool)
        /// </summary>
        PackageType Type { get; }
        /// <summary>
        /// Gets the description of the patch.
        /// </summary>
        string Description { get; }
        /// <summary>
        /// Gets the release date of the patch.
        /// </summary>
        DateTime ReleaseDate { get; }
        /// <summary>
        /// Gets the version of the patch.
        /// </summary>
        Version Version { get; }
        /// <summary>
        /// Gets a dependency array if there is any. Otherwise null.
        /// </summary>
        IEnumerable<Dependency> Dependencies { get; }
        /// <summary>
        /// Gets an <see cref="Action&lt;PatchExecutionContext&gt;"/> instance that will be executed if allowed.
        /// </summary>
        Action<PatchExecutionContext> Action { get;  }

        /* ======================================================== INTERNALS FOR STORAGE */
        /// <summary>
        /// Gets os sets the database id of the related package.
        /// </summary>
        int Id { get; }
        /// <summary>
        /// Gets or sets the time of the execution.
        /// </summary>
        DateTime ExecutionDate { get; }
        /// <summary>
        /// Gets or sets the final state of the execution.
        /// </summary>
        ExecutionResult ExecutionResult { get; }
        /// <summary>
        /// Gets or sets the exception that was thrown when executing the package.
        /// </summary>
        Exception ExecutionError { get; }
    }

    public abstract class SnPatchBase : ISnPatch
    {
        /// <inheritdoc/>
        public string ComponentId { get; internal set; }
        /// <inheritdoc/>
        public abstract PackageType Type { get; }
        /// <inheritdoc/>
        public string Description { get; internal set; }
        /// <inheritdoc/>
        public DateTime ReleaseDate { get; internal set; }
        /// <inheritdoc/>
        public Version Version { get; internal set; }
        /// <inheritdoc/>
        public IEnumerable<Dependency> Dependencies { get; internal set; }
        /// <inheritdoc/>
        public Action<PatchExecutionContext> Action { get; internal set; }
        /// <inheritdoc/>
        public int Id { get; internal set; }
        /// <inheritdoc/>
        public DateTime ExecutionDate { get; internal set; }
        /// <inheritdoc/>
        public ExecutionResult ExecutionResult { get; internal set; }
        /// <inheritdoc/>
        public Exception ExecutionError { get; internal set; }
    }

    [DebuggerDisplay("{ToString()}")]
    public class ComponentInstaller : SnPatchBase
    {
        /// <summary>
        /// Gets the type of the patch. In this case PackageType.Install
        /// </summary>
        public override PackageType Type => PackageType.Install;

        public override string ToString()
        {
            return $"{ComponentId}: {Version}";
        }
    }

    /// <summary>
    /// Represents a patch that will be executed only if the current component version
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
