using System.Diagnostics;
using Microsoft.Extensions.Logging;

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

        internal void WriteTo(ILogger logger)
        {
            if (logger == null)
                return;

            switch (EventType)
            {
                case PatchExecutionEventType.DuplicatedInstaller:
                case PatchExecutionEventType.CannotExecuteOnBefore:
                case PatchExecutionEventType.CannotExecuteOnAfter:
                case PatchExecutionEventType.CannotExecuteMissingVersion:
                    logger.LogWarning(ToString());
                    break;
                case PatchExecutionEventType.PackageNotSaved:
                case PatchExecutionEventType.ExecutionError:
                case PatchExecutionEventType.ExecutionErrorOnBefore:
                    logger.LogError(ToString());
                    break;
                default:
                    logger.LogInformation(ToString());
                    break;
            }
        }
    }
}
