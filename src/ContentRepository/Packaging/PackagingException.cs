using System;
using SenseNet.Diagnostics;

namespace SenseNet.Packaging
{
    public enum PackagingExceptionType
    {
        NotDefined,
        InvalidParameter, InvalidStepParameter,
        DependencyNotFound, DependencyMismatch,
        // head parsing
        WrongRootName, MissingPackageType, InvalidPackageType, MissingComponentId, InvalidComponentId,
        MissingVersion, MissingReleaseDate, InvalidReleaseDate, TooBigReleaseDate,
        // dependency parsing
        MissingDependencyId, EmptyDependencyId,
        MissingDependencyVersion, InvalidVersion, UnexpectedVersionAttribute, DoubleMinVersionAttribute, DoubleMaxVersionAttribute,
        CannotInstallExistingComponent, CannotUpdateMissingComponent, TargetVersionTooSmall,
        DependencyVersion, DependencyMinimumVersion, DependencyMaximumVersion,
        InvalidPhase,
        // parameters
        MissingParameterName, InvalidParameterName, DuplicatedParameter,
        // others
        InvalidInterval, MaxLessThanMin
    }

    [Serializable]
    public class PackagingException : ApplicationException
    {
        public PackagingExceptionType ErrorType { get; private set; }

        public PackagingException(PackagingExceptionType errorType = PackagingExceptionType.NotDefined) { Initialize(EventId.Packaging, errorType); }
        public PackagingException(string message, PackagingExceptionType errorType = PackagingExceptionType.NotDefined) : base(message) { Initialize(EventId.Packaging, errorType); }
        public PackagingException(string message, Exception inner, PackagingExceptionType errorType = PackagingExceptionType.NotDefined) : base(message, inner) { Initialize(EventId.Packaging, errorType); }
        public PackagingException(int eventId, PackagingExceptionType errorType = PackagingExceptionType.NotDefined) { Initialize(eventId, errorType); }
        public PackagingException(int eventId, string message, PackagingExceptionType errorType = PackagingExceptionType.NotDefined) : base(message) { Initialize(eventId, errorType); }
        public PackagingException(int eventId, string message, Exception inner, PackagingExceptionType errorType = PackagingExceptionType.NotDefined) : base(message, inner) { Initialize(eventId, errorType); }
        protected PackagingException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        private void Initialize(int eventId, PackagingExceptionType errorType)
        {
            this.Data.Add("EventId", eventId);
            this.ErrorType = errorType;
        }
    }
}
