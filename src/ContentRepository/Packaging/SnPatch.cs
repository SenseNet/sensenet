using System;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Packaging
{
    public class SnPatch
    {
        public Version Version { get; set; }
        public Version MinVersion { get; set; }
        public Version MaxVersion { get; set; }
        public bool MinVersionIsExclusive { get; set; }
        public bool MaxVersionIsExclusive { get; set; }

        //UNDONE: finalize patch definition options (resource, code, Manifest object)
        public string Contents { get; set; }

        public Func<PatchContext, ExecutionResult> Execute;
    }

    public class PatchContext
    {
        public RepositoryStartSettings Settings { get; set; }
    }
}
