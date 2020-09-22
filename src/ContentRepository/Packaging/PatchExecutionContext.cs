using System;
using SenseNet.ContentRepository;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    public class PatchExecutionContext
    {
        public RepositoryStartSettings Settings { get; }
        public PatchExecutionError[] Errors { get; internal set; } = new PatchExecutionError[0];
        public Action<PatchExecutionLogRecord> LogCallback { get; } = DefaultLogCallback;
        public ISnPatch CurrentPatch { get; internal set; }

        public PatchExecutionContext(RepositoryStartSettings settings, Action<PatchExecutionLogRecord> logCallback)
        {
            Settings = settings;
            LogCallback = logCallback;
        }

        private static void DefaultLogCallback(PatchExecutionLogRecord msg)
        {
            // do nothing
        }
    }
}
