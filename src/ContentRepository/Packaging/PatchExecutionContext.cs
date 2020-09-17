using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    public enum PatchExecutionErrorType
    {
        DuplicatedInstaller, CannotInstall
    }

    public class PatchExecutionError
    {
        public PatchExecutionErrorType ErrorType { get; }
        public ISnPatch FaultyPatch { get; }
        public ISnPatch[] FaultyPatches { get; }
        public string Message { get; }

        public PatchExecutionError(PatchExecutionErrorType errorType, ISnPatch faultyPatch, string message)
        {
            ErrorType = errorType;
            FaultyPatch = faultyPatch;
            FaultyPatches = faultyPatch == null ? new ISnPatch[0] : new[] {faultyPatch};
            Message = message;
        }
        public PatchExecutionError(PatchExecutionErrorType errorType, ISnPatch[] faultyPatches, string message)
        {
            ErrorType = errorType;
            FaultyPatches = faultyPatches;
            FaultyPatch = faultyPatches?.FirstOrDefault();
            Message = message;
        }
    }

    public class PatchExecutionContext
    {
        public RepositoryStartSettings Settings { get; set; }
        public PatchExecutionError[] Errors { get; internal set; } = new PatchExecutionError[0];
        public Action<string> LogMessage { get; set; } = DefaultLogMessage;

        private static void DefaultLogMessage(string msg)
        {
            // do nothing
        }
    }
}
