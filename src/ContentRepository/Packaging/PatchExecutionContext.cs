//UNDONE: separate classes to different files
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    public enum PatchExecutionEventType
    {
        ExecutionStart, ExecutionFinished
    }
    [DebuggerDisplay("{ToString()}")]
    public struct PatchExecutionLogRecord
    {
        public PatchExecutionEventType EventType { get; }
        public ISnPatch Patch { get; }
        public string Message { get; }

        public PatchExecutionLogRecord(PatchExecutionEventType eventType, ISnPatch patch, string message = null)
        {
            EventType = eventType;
            Patch = patch;
            Message = message;
        }

        public override string ToString()
        {
            return Message == null
                ? $"[{Patch}] {EventType}."
                : $"[{Patch}] {EventType}. {Message}";
        }
    }

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
        public Action<PatchExecutionLogRecord> LogMessage { get; set; } = DefaultLogMessage;
        public ISnPatch CurrentPatch { get; internal set; }

        private static void DefaultLogMessage(PatchExecutionLogRecord msg)
        {
            // do nothing
        }
    }
}
