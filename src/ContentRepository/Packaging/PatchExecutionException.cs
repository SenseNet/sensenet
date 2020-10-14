using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using SenseNet.Packaging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    [Serializable]
    public class PatchExecutionException : Exception
    {
        private PatchExecutionErrorType ErrorType { get; }
        ISnPatch Patch { get; }

        public PatchExecutionException(PatchExecutionErrorType errorType, ISnPatch patch)
            : base(GetDefaultMessage(errorType, patch, null))
        {
            ErrorType = errorType;
            Patch = patch;
        }

        public PatchExecutionException(PatchExecutionErrorType errorType, ISnPatch patch, string message)
            : base(GetDefaultMessage(errorType, patch, message))
        {
            ErrorType = errorType;
            Patch = patch;
        }

        public PatchExecutionException(PatchExecutionErrorType errorType, ISnPatch patch, string message, Exception inner)
            : base(GetDefaultMessage(errorType, patch, message), inner)
        {
            ErrorType = errorType;
            Patch = patch;
        }

        protected PatchExecutionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }


        private static string GetDefaultMessage(PatchExecutionErrorType errorType, ISnPatch patch, string message)
        {
            var msg = message ?? "An error occured during a patch execution.";
            return $"{msg} Patch: {patch}, error type: {errorType}";
        }
    }
}
