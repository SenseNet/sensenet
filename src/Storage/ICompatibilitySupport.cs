using System;
// ReSharper disable InconsistentNaming

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository
{
    public interface ICompatibilitySupport
    {
        Uri Request_Url { get; }
        Uri Request_UrlReferrer { get; }
    }

    internal class EmptyCompatibilitySupport : ICompatibilitySupport
    {
        public Uri Request_Url => null;
        public Uri Request_UrlReferrer => null;
    }
}
