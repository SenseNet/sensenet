using System;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Packaging
{
    public class SnPatch
    {
        /// <summary>
        /// Patch target version.
        /// </summary>
        public Version Version { get; set; }
        public Version MinVersion { get; set; }
        public Version MaxVersion { get; set; }
        public bool MinVersionIsExclusive { get; set; }
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
