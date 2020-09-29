using System;
using System.Collections.Generic;
using SenseNet.ContentRepository;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    public class PatchExecutionContext
    {
        public RepositoryStartSettings Settings { get; }
        public List<PatchExecutionError> Errors { get; } = new List<PatchExecutionError>();
        public Action<PatchExecutionLogRecord> LogCallback { get; }
        public ISnPatch CurrentPatch { get; internal set; }

        internal IEnumerable<ISnPatch> ExecutablePatchesOnAfter { get; set; }
        public List<SnComponentDescriptor> CurrentlyInstalledComponents { get; set; }

        public PatchExecutionContext(RepositoryStartSettings settings, Action<PatchExecutionLogRecord> logCallback)
        {
            Settings = settings;
            LogCallback = logCallback ?? DefaultLogCallback;
        }

        private static void DefaultLogCallback(PatchExecutionLogRecord msg)
        {
            // do nothing
        }
    }
}
