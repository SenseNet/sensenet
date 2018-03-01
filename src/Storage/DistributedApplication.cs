using SenseNet.ContentRepository.Storage.Caching;
using SenseNet.Communication.Messaging;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository
{
    public static class DistributedApplication
    {
        public static ICache Cache => Providers.Instance.CacheProvider;
        public static IClusterChannel ClusterChannel => Providers.Instance.ClusterChannelProvider;
    }
}