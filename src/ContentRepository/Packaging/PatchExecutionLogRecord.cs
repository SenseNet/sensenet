using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    public enum PatchExecutionEventType
    {
        DuplicatedInstaller, CannotExecuteOnBefore, CannotExecuteOnAfter, CannotExecuteMissingVersion, PackageNotSaved,
        OnBeforeActionStarts, OnBeforeActionFinished, OnAfterActionStarts, OnAfterActionFinished, ExecutionError,
        ExecutionErrorOnBefore,
        ExecutingOnBefore, ExecutingOnAfter
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

        public string ToString(bool withMessage = true)
        {
            return Message == null || !withMessage
                ? $"[{Patch}] {EventType}."
                : $"[{Patch}] {EventType}. {Message}";
        }
    }
}
