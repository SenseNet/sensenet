﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Security.Messaging;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class InMemoryExtensions
    {
        public static RepositoryInstance StartInMemoryRepository(IServiceProvider services, Action<IRepositoryBuilder> buildRepository = null)
        {
            //TODO:~ missing repo start pieces
            // - user access provider
            
            var repositoryBuilder = new RepositoryBuilder(services)
                .BuildInMemoryRepository();

            buildRepository?.Invoke(repositoryBuilder);

            return Repository.Start(repositoryBuilder);
        }

        public static IRepositoryBuilder BuildInMemoryRepository(this IRepositoryBuilder repositoryBuilder)
        {
            return BuildInMemoryRepository(repositoryBuilder,
                InitialData.Load(SenseNetServicesData.Instance, null),
                GetInitialIndex());
        }
        public static IRepositoryBuilder BuildInMemoryRepository(this IRepositoryBuilder repositoryBuilder,
            InitialData initialData, InMemoryIndex initialIndex)
        {
            if (initialData == null) throw new ArgumentNullException(nameof(initialData));
            if (initialIndex == null) throw new ArgumentNullException(nameof(initialIndex));

            // Precedence: if a service is registered in the collection, use that
            // instead of creating instances locally.
            var services = (repositoryBuilder as RepositoryBuilder)?.Services;
            if (services == null)
                throw new ApplicationException("IServiceProvider cannot be found");

            var dataProvider = services.GetRequiredService<DataProvider>();
            Providers.Instance.ResetBlobProviders(new ConnectionStringOptions());

            repositoryBuilder
                .UseLogger(new DebugWriteLoggerAdapter())
                .UseInitialData(initialData)
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dataProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .AddBlobProvider(new InMemoryBlobProvider())
                .UseSearchEngine(services.GetRequiredService<ISearchEngine>())
                .StartWorkflowEngine(false);

            var statDp = services?.GetService<IStatisticalDataProvider>() as InMemoryStatisticalDataProvider
                       ?? new InMemoryStatisticalDataProvider();
            repositoryBuilder.UseStatisticalDataProvider(statDp);

            return repositoryBuilder;
        }

        public static IServiceCollection AddSenseNetInMemoryDataProvider(this IServiceCollection services)
        {
            return services.AddSenseNetDataProvider<InMemoryDataProvider>();
        }

        /// <summary>
        /// Adds the in-memory statistical data provider to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetInMemoryStatisticalDataProvider(this IServiceCollection services)
        {
            return services.AddStatisticalDataProvider<InMemoryStatisticalDataProvider>();
        }

        /// <summary>
        /// Adds the in-memory implementation of the most important sensenet providers to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetInMemoryProviders(this IServiceCollection services)
        {
            return services
                .AddSenseNetInMemoryDataProvider()
                .AddInMemorySecurityDataProviderExperimental()
                .AddSingleton<ISharedLockDataProvider, InMemorySharedLockDataProvider>()
                .AddSingleton<IExclusiveLockDataProvider, InMemoryExclusiveLockDataProvider>()
                .AddSingleton<IBlobProvider, InMemoryBlobProvider>()
                .AddSingleton<IBlobProviderSelector, InMemoryBlobProviderSelector>()
                .AddSingleton<IAccessTokenDataProvider, InMemoryAccessTokenDataProvider>()
                .AddSingleton<ElevatedModificationVisibilityRule>()
                .AddSingleton<IPackagingDataProvider, InMemoryPackageStorageProvider>()
                .AddSenseNetBlobStorageMetaDataProvider<InMemoryBlobStorageMetaDataProvider>()
                .AddSenseNetInMemoryStatisticalDataProvider()
                .AddInactiveAuditEventWriter()
                .AddSenseNetInMemoryClientStoreDataProvider()
                .AddSenseNetSearchEngine(new InMemorySearchEngine(GetInitialIndex()))
                .AddSenseNetTracer<SnDebugViewTracer>();
        }

        public static IServiceCollection AddInMemorySecurityDataProviderExperimental(this IServiceCollection services)
        {
            return services.AddSingleton<ISecurityDataProvider>(provider =>
            {
                var dp = (InMemoryDataProvider)provider.GetRequiredService<DataProvider>();
                return GetSecurityDataProvider(dp);
            });
        }
        private static ISecurityDataProvider GetSecurityDataProvider(DataProvider repo)
        {
            return new MemoryDataProvider(new DatabaseStorage
            {
                Aces = new List<StoredAce>
                {
                    new StoredAce {EntityId = 2, IdentityId = 1, LocalOnly = false, AllowBits = 0x0EF, DenyBits = 0x000}
                },
                Entities = repo.LoadEntityTreeAsync(CancellationToken.None).GetAwaiter().GetResult()
                    .ToDictionary(x => x.Id, x => new StoredSecurityEntity
                    {
                        Id = x.Id,
                        OwnerId = x.OwnerId,
                        ParentId = x.ParentId,
                        IsInherited = true,
                        HasExplicitEntry = x.Id == 2
                    }),
                Memberships = new List<Membership>
                {
                    new Membership
                    {
                        GroupId = Identifiers.AdministratorsGroupId,
                        MemberId = Identifiers.AdministratorUserId,
                        IsUser = true
                    }
                },
                Messages = new List<Tuple<int, DateTime, byte[]>>()
            });
        }

        private static InMemoryIndex GetInitialIndex()
        {
            var index = new InMemoryIndex();
            index.Load(new StringReader(SenseNetServicesIndex.Index));

            return index;
        }
    }
}
