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

            //UNDONE: missing repo start pieces
            // - configuration
            // - user access provider
            // - packaging provider (in-memory implementation is not accessible here yet)
            
            //UNDONE: disabling all node observers will result in malfunction in the Settings feature
            // because the SettingsCache is a node observer itself.

            var repositoryBuilder = new RepositoryBuilder()
                    .UseLogger(new DebugWriteLoggerAdapter())
                    //.UseLogger(new SnFileSystemEventLogger())
                    //.UseTracer(new SnFileSystemTracer())
                    //.UseAccessProvider(new UserAccessProvider())
                    .UseDataProvider(dataProvider)
                    .UseInitialData(InitialData.Load(DefaultDatabase.Instance))
                    //.UseInitialData(InitialData.Load(new SenseNetServicesInitialData()))
                    .UseSharedLockDataProviderExtension(new InMemorySharedLockDataProvider())
                    .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dataProvider))
                    .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                    .UseAccessTokenDataProviderExtension(new InMemoryAccessTokenDataProvider())
                    //.UsePackagingDataProviderExtension(new InMemoryPackageStorageProvider())
                    .UseSearchEngine(new InMemorySearchEngine(GetInitialIndex()))
                    .UseSecurityDataProvider(GetSecurityDataProvider(dataProvider))
                    .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                    .StartWorkflowEngine(false)
                    //.DisableNodeObservers()
                    //.UseTraceCategories("Test", "Event", "Custom", "Repository", "Query")
                    as RepositoryBuilder;

            Providers.Instance.PropertyCollector = new EventPropertyCollector();

            buildRepository?.Invoke(repositoryBuilder);

            return Repository.Start(repositoryBuilder);
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
            index.Load(new StringReader(DefaultIndex.Index));

            return index;
        }
    }
}
