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

        public string RepositoryConnectionString => AppConfig.GetConnectionString("SnCrMsSql");

        public RepositoryBuilder CreateRepositoryBuilder()
        {
            var serviceCollection = new ServiceCollection();
            BuildServices(AppConfig, serviceCollection);
            var services = serviceCollection.BuildServiceProvider();

            Providers.Instance = new Providers(services);

            var connectionString = AppConfig.GetConnectionString("SnCrMsSql");
            Providers.Instance.ResetBlobProviders(new ConnectionStringOptions { Repository = connectionString });

            var builder = new RepositoryBuilder(services);

            OnBeforeGettingRepositoryBuilder(builder);

            builder
                .UseLogger(new DebugWriteLoggerAdapter())
                //.UseAccessProvider(new DesktopAccessProvider())
                .UseInitialData(Initializer.InitialData)
                .UseTestingDataProvider(services.GetRequiredService<ITestingDataProvider>())
                .UseBlobProviderStore(services.GetRequiredService<IBlobProviderStore>())
                .UseBlobMetaDataProvider(services.GetRequiredService<IBlobStorageMetaDataProvider>())
                .UseBlobProviderSelector(services.GetRequiredService<IBlobProviderSelector>())
                .UseStatisticalDataProvider(services.GetRequiredService<IStatisticalDataProvider>())
                .UseSearchEngine(GetSearchEngine())
                .StartWorkflowEngine(false)
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(SettingsCache))
                .UseTraceCategories("Test", "Event", "Custom");

            OnAfterGettingRepositoryBuilder(builder);

            return builder;
        }

        public abstract void BuildServices(IConfiguration configuration, IServiceCollection services);

        public virtual void OnBeforeGettingRepositoryBuilder(RepositoryBuilder builder) { }
        public virtual void OnAfterGettingRepositoryBuilder(RepositoryBuilder builder) { }
        public virtual void OnAfterRepositoryStart(RepositoryInstance repository) { }

        public abstract DataProvider GetDataProvider(IServiceProvider services);
        public abstract ISearchEngine GetSearchEngine();
    }
}
