﻿using System;
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
                        .UseAccessProvider(new UserAccessProvider());
                })
                .AddSenseNetInMemoryProviders()

                .AddSingleton<ITestingDataProvider, InMemoryTestingDataProvider>()
                ;
        }

        //public override void OnBeforeGettingRepositoryBuilder(RepositoryBuilder builder)
        //{
        //    // in-memory provider works as a regular provider
        //    builder.AddBlobProvider(new InMemoryBlobProvider());

        //    base.OnBeforeGettingRepositoryBuilder(builder);
        //}

        public override DataProvider GetDataProvider(IServiceProvider services) => services.GetRequiredService<DataProvider>();

        public override ISearchEngine GetSearchEngine()
        {
            return new InMemorySearchEngine(new InMemoryIndex());
        }
    }
}
