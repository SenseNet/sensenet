using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    /// <summary>
    /// Holds context information during patching.
    /// </summary>
    public class PatchExecutionContext
    {
        public RepositoryStartSettings Settings { get; }
        public List<PatchExecutionError> Errors { get; } = new List<PatchExecutionError>();
        internal Action<PatchExecutionLogRecord> LogCallback { get; }
        public ISnPatch CurrentPatch { get; internal set; }
        /// <summary>
        /// Gets whether the repository is running.
        /// </summary>
        public bool RepositoryIsRunning { get; internal set; }

        internal List<ISnPatch> ExecutablePatchesOnAfter { get; set; }
        /// <summary>
        /// Gets the full list of currently installed components. It contains the latest components,
        /// even the ones installed in this iteration.
        /// </summary>
        public List<SnComponentDescriptor> CurrentlyInstalledComponents { get; set; }

        internal PatchExecutionContext(RepositoryStartSettings settings, Action<PatchExecutionLogRecord> logCallback)
        {
            Settings = settings;
            LogCallback = logCallback ?? DefaultLogCallback;
        }

        private static void DefaultLogCallback(PatchExecutionLogRecord msg)
        {
            // do nothing
        }

        /* ============================================================================== API FOR PATCH WRITERS */

        /// <summary>
        /// Logs a message using the system-defined logger.
        /// </summary>
        public void Log(string message)
        {
            LogCallback(new PatchExecutionLogRecord(
                RepositoryIsRunning
                    ? PatchExecutionEventType.ExecutingOnAfter
                    : PatchExecutionEventType.ExecutingOnBefore,
                CurrentPatch,
                message));
        }

        private enum VersionRelation { LowerOrEqual, Lower, Equal, Higher, HigherOrEqual }

        public bool ComponentVersionIsLower(string version)
        {
            return ComponentVersionIsLower(CurrentPatch.ComponentId, version);
        }
        public bool ComponentVersionIsLower(string componentId, string version)
        {
            return CompareComponentVersion(componentId, version, VersionRelation.Lower);
        }
        public bool ComponentVersionIsLowerOrEqual(string version)
        {
            return ComponentVersionIsLowerOrEqual(CurrentPatch.ComponentId, version);
        }
        public bool ComponentVersionIsLowerOrEqual(string componentId, string version)
        {
            return CompareComponentVersion(componentId, version, VersionRelation.LowerOrEqual);
        }
        public bool ComponentVersionIsEqual(string version)
        {
            return ComponentVersionIsEqual(CurrentPatch.ComponentId, version);
        }
        public bool ComponentVersionIsEqual(string componentId, string version)
        {
            return CompareComponentVersion(componentId, version, VersionRelation.Equal);
        }
        public bool ComponentVersionIsHigher(string version)
        {
            return ComponentVersionIsHigher(CurrentPatch.ComponentId, version);
        }
        public bool ComponentVersionIsHigher(string componentId, string version)
        {
            return CompareComponentVersion(componentId, version, VersionRelation.Higher);
        }
        public bool ComponentVersionIsHigherOrEqual(string version)
        {
            return ComponentVersionIsHigherOrEqual(CurrentPatch.ComponentId, version);
        }
        public bool ComponentVersionIsHigherOrEqual(string componentId, string version)
        {
            return CompareComponentVersion(componentId, version, VersionRelation.HigherOrEqual);
        }

        private bool CompareComponentVersion(string componentId, string versionSrc, VersionRelation relation)
        {
            var component = CurrentlyInstalledComponents
                .FirstOrDefault(x => x.ComponentId == componentId);

            if(component == null)
            {
                throw
                    new PatchExecutionException(
                        RepositoryIsRunning
                            ? PatchExecutionErrorType.ExecutionErrorOnBefore
                            : PatchExecutionErrorType.ExecutionErrorOnAfter,
                        CurrentPatch,
                        "Component not found: "+componentId);
            }

            var version = PatchBuilder.ParseVersion(versionSrc, CurrentPatch);

            var componentVersion = RepositoryIsRunning
                ? component.TempVersionAfter ?? component.Version
                : component.TempVersionBefore ?? component.Version;

            switch (relation)
            {
                case VersionRelation.LowerOrEqual:  return version >= componentVersion; 
                case VersionRelation.Lower:         return version >  componentVersion; 
                case VersionRelation.Equal:         return version == componentVersion; 
                case VersionRelation.Higher:        return version <  componentVersion; 
                case VersionRelation.HigherOrEqual: return version <= componentVersion; 
                default:
                    throw new ArgumentOutOfRangeException(nameof(relation), relation, null);
            }
        }
    }
}
