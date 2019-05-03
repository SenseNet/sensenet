using System;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Packaging
{
    /// <summary>
    /// Represents a patch that will be executed only if the current component version
    /// is lower than the supported version of the component and it is between the defined 
    /// minimum and maximum version numbers in this patch. The component version after 
    /// this patch will be the one defined in the Version property.
    /// </summary>
    public class SnPatch
    {
        /// <summary>
        /// Patch target version.
        /// </summary>
        public Version Version { get; set; }
        public Version MinVersion { get; set; }
        public Version MaxVersion { get; set; }

        /// <summary>
        /// If set to true, the patch will not be executed if the current
        /// component version is the same as the minimum version defined
        /// in this patch.
        /// </summary>
        public bool MinVersionIsExclusive { get; set; }
        /// <summary>
        /// If set to true, the patch will not be executed if the current
        /// component version is the same as the maximum version defined
        /// in this patch.
        /// </summary>
        public bool MaxVersionIsExclusive { get; set; }

        //TODO: add more patch definition options (resource, code, Manifest object)

        /// <summary>
        /// Patch definition in a manifest xml format.
        /// </summary>
        public string Contents { get; set; }

        /// <summary>
        /// Patch definition in the form of code.
        /// </summary>
        public Func<PatchContext, ExecutionResult> Execute;
    }

    public class PatchContext
    {
        public RepositoryStartSettings Settings { get; set; }
    }
}
