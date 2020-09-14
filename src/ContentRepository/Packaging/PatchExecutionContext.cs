using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    public enum PatchExecutionErrorType
    {
        DuplicatedInstaller
    }

    public class PatchExecutionError
    {
        public PatchExecutionErrorType ErrorType { get; }
        public string Message { get; }

        public PatchExecutionError(PatchExecutionErrorType errorType, string message)
        {
            ErrorType = errorType;
            Message = message;
        }
    }

    public class PatchExecutionContext
    {
        public RepositoryStartSettings Settings { get; set; }
        public PatchExecutionError[] Errors { get; internal set; }
        public Action<string> LogMessage { get; set; } = DefaultLogMessage;

        private static void DefaultLogMessage(string msg)
        {
            // do nothing
        }
    }
}
