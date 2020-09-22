using System;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    public enum PatchErrorCode
    {
        InvalidVersion, InvalidDate, MissingDescription, TooSmallTargetVersion
    }

    [Serializable]
    public class InvalidPatchException : Exception
    {
        public PatchErrorCode ErrorCode { get; }

        public InvalidPatchException(PatchErrorCode code)
        {
            ErrorCode = code;
        }
        public InvalidPatchException(PatchErrorCode code, string message) : base(message)
        {
            ErrorCode = code;
        }
        public InvalidPatchException(PatchErrorCode code, string message, Exception inner) : base(message, inner)
        {
            ErrorCode = code;
        }
        protected InvalidPatchException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
