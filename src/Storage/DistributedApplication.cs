using SenseNet.ContentRepository.Storage.Caching;
using SenseNet.Communication.Messaging;
using SenseNet.Configuration;
using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository
{

    public static class DistributedApplication
    {
        [Obsolete("Use static Cache class instead", true)]
        public static ISnCache Cache => Providers.Instance.CacheProvider;
        public static IClusterChannel ClusterChannel => Providers.Instance.ClusterChannelProvider;
    }
}