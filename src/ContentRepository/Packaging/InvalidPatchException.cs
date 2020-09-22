using System;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    public enum PatchErrorCode
    {
        InvalidVersion, InvalidDate, MissingDescription, TooSmallTargetVersion,
        SelfDependency, DuplicatedDependency
    }

    [Serializable]
    public class InvalidPatchException : Exception
    {
        public PatchErrorCode ErrorCode { get; }
        public ISnPatch Patch { get; }

        public InvalidPatchException(PatchErrorCode code, ISnPatch patch)
        {
            ErrorCode = code;
            Patch = patch;
        }
        public InvalidPatchException(PatchErrorCode code, ISnPatch patch, string message) : base(message)
        {
            ErrorCode = code;
            Patch = patch;
        }
        public InvalidPatchException(PatchErrorCode code, ISnPatch patch, string message, Exception inner) : base(message, inner)
        {
            ErrorCode = code;
            Patch = patch;
        }
        protected InvalidPatchException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
