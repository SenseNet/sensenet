﻿using System;
// ReSharper disable InconsistentNaming

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository
{
    public interface ICompatibilitySupport
    {
        Uri Request_Url { get; }
        Uri Request_UrlReferrer { get; }

        bool Response_IsClientConnected { get; }
    }

    internal class EmptyCompatibilitySupport : ICompatibilitySupport
    {
        public Uri Request_Url => null;
        public Uri Request_UrlReferrer => null;
        public bool Response_IsClientConnected => true;
    }
}
