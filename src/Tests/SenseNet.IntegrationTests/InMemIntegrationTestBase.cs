using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Tests.Core.Implementations;

namespace SenseNet.IntegrationTests
{
    public abstract class InMemIntegrationTestBase<T> : IntegrationTestBase where T : TestCaseBase, new()
    {
        public T TestCases { get; }

        protected InMemIntegrationTestBase()
        {
            TestCases = new T();
            TestCases.SetImplementation(this);
        }

        public override RepositoryBuilder GetRepositoryBuilder()
        {
            var builder = new RepositoryBuilder()
                .BuildInMemoryRepository(GetInitialData(), GetInitialIndex())
                .UseTestingDataProviderExtension(new InMemoryTestingDataProvider())
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(SettingsCache))
                .UseTraceCategories("Test", "Event", "Custom") as RepositoryBuilder;

            ConfigureRepository(builder);

            return builder;
        }

        protected virtual RepositoryBuilder ConfigureRepository(RepositoryBuilder builder)
        {
            return builder;
        }

        private static InitialData _initialData;
        protected static InitialData GetInitialData()
        {
            return _initialData ?? (_initialData = InitialData.Load(InMemoryTestData.Instance));
        }

        private static InMemoryIndex _initialIndex;
        protected static InMemoryIndex GetInitialIndex()
        {
            if (_initialIndex == null)
            {
                var index = new InMemoryIndex();
                index.Load(new StringReader(InMemoryTestIndex.Index));
                _initialIndex = index;
            }
            return _initialIndex.Clone();
        }

    }
}
