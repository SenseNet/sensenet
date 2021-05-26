using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.Diagnostics;
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class InMemoryExtensions
    {
        public static RepositoryInstance StartInMemoryRepository(Action<IRepositoryBuilder> buildRepository = null)
        {
            //TODO:~ missing repo start pieces
            // - user access provider
            
            var repositoryBuilder = new RepositoryBuilder()
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

            if (services?.GetService<DataProvider>() is InMemoryDataProvider dataProvider)
            {
                // If there is an instance in the container, use that. We have to set
                // these instances manually instead of using the extension method so that
                // we do not overwrite the store instance.
                Providers.Instance.DataProvider = dataProvider;
                Providers.Instance.DataStore = services.GetService<IDataStore>();
            }
            else
            {
                dataProvider = new InMemoryDataProvider();
                repositoryBuilder.UseDataProvider(dataProvider);
            }
            
            Providers.Instance.ResetBlobProviders();

            repositoryBuilder
                .UseLogger(new DebugWriteLoggerAdapter())
                .UseTracer(new SnDebugViewTracer())
                .UseInitialData(initialData)
                .UseSharedLockDataProviderExtension(new InMemorySharedLockDataProvider())
                .UseExclusiveLockDataProviderExtension(new InMemoryExclusiveLockDataProvider())
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dataProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .AddBlobProvider(new InMemoryBlobProvider())
                .UseAccessTokenDataProviderExtension(new InMemoryAccessTokenDataProvider())
                .UsePackagingDataProviderExtension(new InMemoryPackageStorageProvider())
                .UseSearchEngine(new InMemorySearchEngine(initialIndex))
                .UseSecurityDataProvider(GetSecurityDataProvider(dataProvider))
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .StartWorkflowEngine(false);

            Providers.Instance.PropertyCollector = new EventPropertyCollector();
            
            return repositoryBuilder;
        }

        public static IServiceCollection AddSenseNetInMemoryDataProvider(this IServiceCollection services)
        {
            return services.AddSenseNetDataProvider<InMemoryDataProvider>();
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
