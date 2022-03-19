using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.IntegrationTests.Common;
using SenseNet.Search;
using SenseNet.Security;
using SenseNet.Security.Messaging;
using SenseNet.Tests.Core.Implementations;

namespace SenseNet.IntegrationTests.Infrastructure
{
    public interface IPlatform
    {
        IConfiguration AppConfig { get; set; }
        void BuildServices(IConfiguration configuration, IServiceCollection services);
        RepositoryBuilder CreateRepositoryBuilder();
        void OnAfterRepositoryStart(RepositoryInstance repository);
    }
    public abstract class Platform : IPlatform
    {
        public IConfiguration AppConfig { get; set; }

        public RepositoryBuilder CreateRepositoryBuilder()
        {
            var serviceCollection = new ServiceCollection();
            BuildServices(AppConfig, serviceCollection);
            var services = serviceCollection.BuildServiceProvider();

            Providers.Instance.ResetBlobProviders();

            var builder = new RepositoryBuilder(services);

            OnBeforeGettingRepositoryBuilder(builder);

            var dataProvider = GetDataProvider(services);

            builder
                .UseLogger(new DebugWriteLoggerAdapter())
                .UseTracer(new SnDebugViewTracer())
                .UseDataProvider(dataProvider)
                .UseInitialData(Initializer.InitialData)
                .UseTestingDataProvider(GetTestingDataProvider(services))
                .UseSharedLockDataProvider(GetSharedLockDataProvider(services))
                .UseExclusiveLockDataProvider(GetExclusiveLockDataProvider(services))
                .AddBlobProviders(GetBlobProviders()) // extension for platforms
                .UseBlobMetaDataProvider(GetBlobMetaDataProvider(dataProvider, services))
                .UseBlobProviderSelector(GetBlobProviderSelector(services))
                .UseAccessTokenDataProvider(GetAccessTokenDataProvider(services))
                .UsePackagingDataProvider(GetPackagingDataProvider(services))
                .UseStatisticalDataProvider(GetStatisticalDataProvider(services))
                .UseSearchManager(new SearchManager(Providers.Instance.DataStore))
                .UseIndexManager(new IndexManager(Providers.Instance.DataStore, Providers.Instance.SearchManager))
                .UseIndexPopulator(new DocumentPopulator(Providers.Instance.DataStore, Providers.Instance.IndexManager))
                .UseSearchEngine(GetSearchEngine())
                .UseSecurityDataProvider(GetSecurityDataProvider(dataProvider, services))
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

        public abstract void BuildServices(IConfiguration configuration, IServiceCollection services);

        public virtual void OnBeforeGettingRepositoryBuilder(RepositoryBuilder builder) { }
        public virtual void OnAfterGettingRepositoryBuilder(RepositoryBuilder builder) { }
        public virtual void OnAfterRepositoryStart(RepositoryInstance repository) { }

        public abstract DataProvider GetDataProvider(IServiceProvider services);
        public abstract ISharedLockDataProvider GetSharedLockDataProvider(IServiceProvider services);
        public abstract IEnumerable<IBlobProvider> GetBlobProviders();
        public abstract IExclusiveLockDataProvider GetExclusiveLockDataProvider(IServiceProvider services);
        public abstract IBlobStorageMetaDataProvider GetBlobMetaDataProvider(DataProvider dataProvider, IServiceProvider services);
        public abstract IBlobProviderSelector GetBlobProviderSelector(IServiceProvider services);
        public abstract IAccessTokenDataProvider GetAccessTokenDataProvider(IServiceProvider services);
        public abstract IPackagingDataProvider GetPackagingDataProvider(IServiceProvider services);
        public abstract ISecurityDataProvider GetSecurityDataProvider(DataProvider dataProvider, IServiceProvider services);
        public abstract ITestingDataProvider GetTestingDataProvider(IServiceProvider services);
        public abstract ISearchEngine GetSearchEngine();
        public abstract IStatisticalDataProvider GetStatisticalDataProvider(IServiceProvider services);
    }
}
