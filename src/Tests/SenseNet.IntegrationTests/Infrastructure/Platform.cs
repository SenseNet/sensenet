using System.Collections.Generic;
using System.IO;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.IntegrationTests.Common;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Security;
using SenseNet.Security.Messaging;
using SenseNet.Tests.Core.Implementations;

namespace SenseNet.IntegrationTests.Infrastructure
{
    public interface IPlatform
    {
        RepositoryBuilder CreateRepositoryBuilder();
        void OnAfterRepositoryStart(RepositoryInstance repository);
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
                .UseSharedLockDataProvider(GetSharedLockDataProvider())
                .UseExclusiveLockDataProvider(GetExclusiveLockDataProvider())
                .AddBlobProviders(GetBlobProviders()) // extension for platforms
                .UseBlobMetaDataProvider(GetBlobMetaDataProvider(dataProvider))
                .UseBlobProviderSelector(GetBlobProviderSelector())
                .UseAccessTokenDataProvider(GetAccessTokenDataProvider())
                .UsePackagingDataProviderExtension(GetPackagingDataProviderExtension())
                .UseStatisticalDataProvider(GetStatisticalDataProvider())
                .UseSearchManager(new SearchManager(Providers.Instance.DataStore))
                .UseIndexManager(new IndexManager(Providers.Instance.DataStore, Providers.Instance.SearchManager))
                .UseIndexPopulator(new DocumentPopulator(Providers.Instance.DataStore, Providers.Instance.IndexManager))
                .UseSearchEngine(GetSearchEngine())
                .UseSecurityDataProvider(GetSecurityDataProvider(dataProvider))
                .UseSecurityMessageProvider(new DefaultMessageProvider(new MessageSenderManager()))
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
        public virtual void OnAfterRepositoryStart(RepositoryInstance repository) { }

        public abstract DataProvider GetDataProvider();
        public abstract ISharedLockDataProvider GetSharedLockDataProvider();
        public abstract IEnumerable<IBlobProvider> GetBlobProviders();
        public abstract IExclusiveLockDataProvider GetExclusiveLockDataProvider();
        public abstract IBlobStorageMetaDataProvider GetBlobMetaDataProvider(DataProvider dataProvider);
        public abstract IBlobProviderSelector GetBlobProviderSelector();
        public abstract IAccessTokenDataProvider GetAccessTokenDataProvider();
        public abstract IPackagingDataProviderExtension GetPackagingDataProviderExtension();
        public abstract ISecurityDataProvider GetSecurityDataProvider(DataProvider dataProvider);
        public abstract ITestingDataProviderExtension GetTestingDataProviderExtension();
        public abstract ISearchEngine GetSearchEngine();
        public abstract IStatisticalDataProvider GetStatisticalDataProvider();
    }
}
