using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Diagnostics;
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
        public override void BuildServices(IConfiguration configuration, IServiceCollection services)
        {
            services
                .AddSenseNet(configuration, (repositoryBuilder, provider) =>
                {
                    repositoryBuilder
                        .BuildInMemoryRepository()
                        .UseLogger(provider)
                        .UseAccessProvider(new UserAccessProvider())
                        .UseInactiveAuditEventWriter();
                })
                .AddSenseNetInMemoryProviders()

                .AddSingleton<ISharedLockDataProvider, InMemorySharedLockDataProvider>() //UNDONE:TEST: generalize service addition
                .AddSingleton<IExclusiveLockDataProvider, InMemoryExclusiveLockDataProvider>() //UNDONE:TEST: generalize service addition
                .AddSingleton<IBlobProviderSelector, InMemoryBlobProviderSelector>() //UNDONE:TEST: generalize service addition
                .AddSingleton<IAccessTokenDataProvider, InMemoryAccessTokenDataProvider>() //UNDONE:TEST: generalize service addition
                .AddSingleton<IPackagingDataProvider, InMemoryPackageStorageProvider>() //UNDONE:TEST: generalize service addition
                .AddSingleton<ITestingDataProvider, InMemoryTestingDataProvider>() //UNDONE:TEST: generalize service addition
                ;
        }

        //public override void OnBeforeGettingRepositoryBuilder(RepositoryBuilder builder)
        //{
        //    // in-memory provider works as a regular provider
        //    builder.AddBlobProvider(new InMemoryBlobProvider());

        //    base.OnBeforeGettingRepositoryBuilder(builder);
        //}

        public override DataProvider GetDataProvider(IServiceProvider services) => services.GetRequiredService<DataProvider>();
        public override ISharedLockDataProvider GetSharedLockDataProvider(IServiceProvider services) => services.GetRequiredService<ISharedLockDataProvider>();
        public override IExclusiveLockDataProvider GetExclusiveLockDataProvider(IServiceProvider services) => services.GetRequiredService<IExclusiveLockDataProvider>();

        public override IBlobStorageMetaDataProvider GetBlobMetaDataProvider(DataProvider dataProvider, IServiceProvider services) => services.GetRequiredService<IBlobStorageMetaDataProvider>();
        public override IBlobProviderSelector GetBlobProviderSelector(IServiceProvider services) => services.GetRequiredService<IBlobProviderSelector>();

        public override IEnumerable<IBlobProvider> GetBlobProviders()
        {
            return new[] { new InMemoryBlobProvider() };
        }

        public override IAccessTokenDataProvider GetAccessTokenDataProvider(IServiceProvider services) => services.GetRequiredService<IAccessTokenDataProvider>();
        public override IPackagingDataProvider GetPackagingDataProvider(IServiceProvider services) => services.GetRequiredService<IPackagingDataProvider>();

        public override ISecurityDataProvider GetSecurityDataProvider(DataProvider dataProvider, IServiceProvider services)
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

        public override ITestingDataProvider GetTestingDataProvider(IServiceProvider services) => services.GetRequiredService<ITestingDataProvider>();

        public override ISearchEngine GetSearchEngine()
        {
            return new InMemorySearchEngine(new InMemoryIndex());
        }

        public override IStatisticalDataProvider GetStatisticalDataProvider(IServiceProvider services) => services.GetRequiredService<IStatisticalDataProvider>();

    }
}
