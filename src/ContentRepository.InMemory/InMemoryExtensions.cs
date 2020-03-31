using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.Diagnostics;
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.InMemory
{
    public static class InMemoryExtensions
    {
        public static RepositoryInstance StartInMemoryRepository(Action<IRepositoryBuilder> buildRepository = null)
        {
            var dataProvider = new InMemoryDataProvider();

            //TODO:~ missing repo start pieces
            // - configuration
            // - user access provider
            // - packaging provider (in-memory implementation is not accessible here yet)
            
            var repositoryBuilder = new RepositoryBuilder()
                    .UseLogger(new DebugWriteLoggerAdapter())
                    //.UseAccessProvider(new UserAccessProvider())
                    .UseDataProvider(dataProvider)
                    .UseInitialData(InitialData.Load(SenseNetServicesData.Instance))
                    .UseSharedLockDataProviderExtension(new InMemorySharedLockDataProvider())
                    .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dataProvider))
                    .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                    .UseAccessTokenDataProviderExtension(new InMemoryAccessTokenDataProvider())
                    //.UsePackagingDataProviderExtension(new InMemoryPackageStorageProvider())
                    .UseSearchEngine(new InMemorySearchEngine(GetInitialIndex()))
                    .UseSecurityDataProvider(GetSecurityDataProvider(dataProvider))
                    .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                    .StartWorkflowEngine(false)
                    as RepositoryBuilder;

            Providers.Instance.PropertyCollector = new EventPropertyCollector();

            buildRepository?.Invoke(repositoryBuilder);

            var repository = Repository.Start(repositoryBuilder);

            return repository;
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
