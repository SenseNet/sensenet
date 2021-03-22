using System.Collections.Generic;
using System.IO;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Security;
using SenseNet.Tests.Core.Implementations;

namespace SenseNet.IntegrationTests.Infrastructure
{
    public interface IPlatform
    {
        RepositoryBuilder CreateRepositoryBuilder();
    }
    public abstract class Platform : IPlatform
    {
        public RepositoryBuilder CreateRepositoryBuilder()
        {
            Providers.Instance.ResetBlobProviders();

            var builder = new RepositoryBuilder();

            OnBeforeGettingRepositoryBuilder(builder);

            var dataProvider = GetDataProvider();
            builder
                .UseLogger(new DebugWriteLoggerAdapter())
                .UseTracer(new SnDebugViewTracer())
                .UseDataProvider(dataProvider)
                .UseInitialData(Initializer.InitialData)
                .UseTestingDataProviderExtension(GetTestingDataProviderExtension())
                .UseSharedLockDataProviderExtension(GetSharedLockDataProviderExtension())
                .UseExclusiveLockDataProviderExtension(GetExclusiveLockDataProviderExtension())
                .UseBlobMetaDataProvider(GetBlobMetaDataProvider(dataProvider))
                .UseBlobProviderSelector(GetBlobProviderSelector())
                .UseAccessTokenDataProviderExtension(GetAccessTokenDataProviderExtension())
                .UsePackagingDataProviderExtension(GetPackagingDataProviderExtension())
                .UseSearchEngine(GetSearchEngine())
                .UseSecurityDataProvider(GetSecurityDataProvider(dataProvider))
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .StartWorkflowEngine(false)
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(SettingsCache))
                .UseTraceCategories("Test", "Event", "Custom");

            Providers.Instance.PropertyCollector = new EventPropertyCollector();

            OnAfterGettingRepositoryBuilder(builder);

            return builder;
        }

        public virtual void OnBeforeGettingRepositoryBuilder(RepositoryBuilder builder) { }
        public virtual void OnAfterGettingRepositoryBuilder(RepositoryBuilder builder) { }

        public abstract DataProvider GetDataProvider();
        public abstract ISharedLockDataProviderExtension GetSharedLockDataProviderExtension();
        public abstract IExclusiveLockDataProviderExtension GetExclusiveLockDataProviderExtension();
        public abstract IBlobStorageMetaDataProvider GetBlobMetaDataProvider(DataProvider dataProvider);
        public abstract IBlobProviderSelector GetBlobProviderSelector();
        public abstract IAccessTokenDataProviderExtension GetAccessTokenDataProviderExtension();
        public abstract IPackagingDataProviderExtension GetPackagingDataProviderExtension();
        public abstract ISecurityDataProvider GetSecurityDataProvider(DataProvider dataProvider);
        public abstract ITestingDataProviderExtension GetTestingDataProviderExtension();
        public abstract ISearchEngine GetSearchEngine();

    }
}
