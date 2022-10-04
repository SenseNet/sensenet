using Microsoft.Extensions.DependencyInjection;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search.Indexing.Activities;
using SenseNet.Storage.DistributedApplication.Messaging;
using SenseNet.ContentRepository.i18n;
using SenseNet.ApplicationModel;
using SenseNet.Communication.Messaging;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.ContentRepository.Storage.Caching.DistributedActions;

namespace SenseNet.ContentRepository
{
    public static class SnMessageFormatterExtensions
    {
        public static IServiceCollection AddClusterMessageTypes(this IServiceCollection services)
        {
            return services
                    // caching
                    .AddClusterMessageType<CacheCleanAction>()
                    .AddClusterMessageType<CleanupNodeCacheAction>()
                    .AddClusterMessageType<NodeIdDependency.FireChangedDistributedAction> ()
                    .AddClusterMessageType<NodeTypeDependency.FireChangedDistributedAction>()
                    .AddClusterMessageType<PathDependency.FireChangedDistributedAction>()

                    // reset
                    .AddClusterMessageType<ContentTypeManager.ContentTypeManagerResetDistributedAction>()
                    .AddClusterMessageType<StorageSchema.NodeTypeManagerRestartDistributedAction>()
                    .AddClusterMessageType<SenseNetResourceManager.ResourceManagerResetDistributedAction>()
                    .AddClusterMessageType<RepositoryVersionInfo.RepositoryVersionInfoResetDistributedAction>()
                    .AddClusterMessageType<ApplicationStorage.ApplicationStorageInvalidateDistributedAction>()
                    .AddClusterMessageType<DeviceManager.DeviceManagerResetDistributedAction>()
                    .AddClusterMessageType<TreeCache<Settings>.TreeCacheInvalidatorDistributedAction<Settings>>()

                    // indexing
                    .AddClusterMessageType<AddDocumentActivity>()
                    .AddClusterMessageType<UpdateDocumentActivity>()
                    .AddClusterMessageType<AddTreeActivity>()
                    .AddClusterMessageType<RemoveTreeActivity>()
                    .AddClusterMessageType<RebuildActivity>()
                    .AddClusterMessageType<RestoreActivity>()

                    // debug
                    .AddClusterMessageType<DebugMessage>()
                    .AddClusterMessageType<PingMessage>()
                    .AddClusterMessageType<PongMessage>()

                    // other
                    .AddClusterMessageType<LoggingSettings.UpdateCategoriesDistributedAction>()
                    .AddClusterMessageType<WakeUp>()
                ;
        }
    }
}
