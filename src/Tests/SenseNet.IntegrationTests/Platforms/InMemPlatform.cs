using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Tests.Core.Implementations;

namespace SenseNet.IntegrationTests.Platforms
{
    public class InMemPlatform : Platform
    {
        //public override void OnBeforeGettingRepositoryBuilder(RepositoryBuilder builder)
        //{
        //    // in-memory provider works as a regular provider
        //    builder.AddBlobProvider(new InMemoryBlobProvider());

        //    base.OnBeforeGettingRepositoryBuilder(builder);
        //}

        public override DataProvider GetDataProvider()
        {
            return new InMemoryDataProvider();
        }
        public override ISharedLockDataProviderExtension GetSharedLockDataProviderExtension()
        {
            return new InMemorySharedLockDataProvider();
        }

        public override IExclusiveLockDataProviderExtension GetExclusiveLockDataProviderExtension()
        {
            return new InMemoryExclusiveLockDataProvider();
        }
        public override IBlobStorageMetaDataProvider GetBlobMetaDataProvider(DataProvider dataProvider)
        {
            return new InMemoryBlobStorageMetaDataProvider((InMemoryDataProvider)dataProvider);
        }
        public override IBlobProviderSelector GetBlobProviderSelector()
        {
            return new InMemoryBlobProviderSelector();
        }
        public override IEnumerable<IBlobProvider> GetBlobProviders()
        {
            return new[] { new InMemoryBlobProvider() };
        }

        public override IAccessTokenDataProviderExtension GetAccessTokenDataProviderExtension()
        {
            return new InMemoryAccessTokenDataProvider();
        }
        public override IPackagingDataProviderExtension GetPackagingDataProviderExtension()
        {
            return new InMemoryPackageStorageProvider();
        }
        public override ISecurityDataProvider GetSecurityDataProvider(DataProvider dataProvider)
        {
            return new MemoryDataProvider(new DatabaseStorage
            {
                Aces = new List<StoredAce>
                {
                    new StoredAce {EntityId = 2, IdentityId = 1, LocalOnly = false, AllowBits = 0x0EF, DenyBits = 0x000}
                },
                Entities = dataProvider.LoadEntityTreeAsync(CancellationToken.None).GetAwaiter().GetResult()
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
        public override ITestingDataProviderExtension GetTestingDataProviderExtension()
        {
            return new InMemoryTestingDataProvider();
        }
        public override ISearchEngine GetSearchEngine()
        {
            return new InMemorySearchEngine(new InMemoryIndex());
        }
    }
}
