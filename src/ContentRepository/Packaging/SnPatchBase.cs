using System;
using System.Collections.Generic;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    /// <summary>
    /// Defines an interface that describes a modification action (install or patch) for an <see cref="SnComponent"/>.
    /// </summary>
    public interface ISnPatch
    {
        /// <summary>
        /// Gets the Id of the package / component.
        /// </summary>
        string ComponentId { get; }
        /// <summary>
        /// Gets the type of the patch (Install, Patch)
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
        /// Gets an <see cref="Action&lt;PatchExecutionContext&gt;"/> instance that will be executed
        /// before the repository is started.
        /// </summary>
        Action<PatchExecutionContext> ActionBeforeStart { get; }
        /// <summary>
        /// Gets an <see cref="Action&lt;PatchExecutionContext&gt;"/> instance that will be executed.
        /// </summary>
        Action<PatchExecutionContext> Action { get; }

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

    /// <summary>
    /// Defines a base class for a modification action (install or patch) for an <see cref="SnComponent"/>.
    /// </summary>
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
        public Action<PatchExecutionContext> ActionBeforeStart { get; internal set; }
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
}
