using System.Diagnostics;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    public enum PatchExecutionErrorType
    {
        DuplicatedInstaller, CannotInstall, MissingVersion, ExecutionErrorOnBefore, ExecutionErrorOnAfter
    }
    [DebuggerDisplay("{ToString()())}")]
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
            FaultyPatches = faultyPatch == null ? new ISnPatch[0] : new[] { faultyPatch };
            Message = message;
        }
        public PatchExecutionError(PatchExecutionErrorType errorType, ISnPatch[] faultyPatches, string message)
        {
            ErrorType = errorType;
            FaultyPatches = faultyPatches;
            FaultyPatch = faultyPatches?.FirstOrDefault();
            Message = message;
        }

        public override string ToString()
        {
            return $"{ErrorType} {FaultyPatch}";
        }
    }
}
