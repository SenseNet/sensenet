﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nito.AsyncEx;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Search;
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Security.Messaging;
using SenseNet.Testing;
using SenseNet.Tests.Core.Implementations;
using STT = System.Threading.Tasks;

namespace SenseNet.Tests.Core
{
    [TestClass]
    public class TestBase
    {
        public TestContext TestContext { get; set; }

        protected virtual bool ReusesRepository => false;

        protected virtual void InitializeTest()
        {
            // do nothing;
        }
        [TestInitialize]
        public virtual void _initializeTest()
        {
            //// workaround for having a half-started repository
            //if (RepositoryInstance.Started())
            //    RepositoryInstance.Shutdown();

            TestContext.StartTest(traceToFile: false, reusesRepository: ReusesRepository);
            InitializeTest();
        }

        protected virtual void CleanupTest()
        {
            // do nothing;
        }
        [TestCleanup]
        public void _cleanupTest()
        {
            CleanupTest();
            TestContext.FinishTestTest();
        }

        protected void Test(Action callback)
        {
            Test(false, null, callback);
        }
        protected void Test(bool useCurrentUser, Action callback)
        {
            Test(useCurrentUser, null, callback);
        }
        protected void Test(Action<RepositoryBuilder> initialize, Action callback)
        {
            Test(false, initialize, callback);
        }
        protected void Test(bool useCurrentUser, Action<RepositoryBuilder> initialize, Action callback)
        {
            ExecuteTest(useCurrentUser, initialize, callback);
        }
        private void ExecuteTest(bool useCurrentUser, Action<RepositoryBuilder> initialize, Action callback)
        {
            OnTestInitialize();

            var builder = CreateRepositoryBuilderForTest();

            initialize?.Invoke(builder);

            Indexing.IsOuterSearchEngineEnabled = true;

            Cache.Reset();
            ResetContentTypeManager();

            OnBeforeRepositoryStart(builder);

            using (Repository.Start(builder))
            {
                PrepareRepository();

                User.Current = User.Administrator;
                if (useCurrentUser)
                    callback();
                else
                    using (new SystemAccount())
                        callback();
            }

            OnAfterRepositoryShutdown();
        }

        protected void Test2(Action<IServiceCollection> initializeServices, Action callback)
        {
            Test2(false, initializeServices, callback);
        }
        protected void Test2(bool useCurrentUser, Action<IServiceCollection> initializeServices, Action callback)
        {
            ExecuteTest2(useCurrentUser, initializeServices, null, callback);
        }
        protected void Test2(Action<IServiceCollection> initializeServices, Action<RepositoryBuilder> initialize, Action callback)
        {
            Test2(false, initializeServices, initialize, callback);
        }
        protected void Test2(bool useCurrentUser, Action<IServiceCollection> initializeServices, Action<RepositoryBuilder> initialize, Action callback)
        {
            ExecuteTest2(useCurrentUser, initializeServices, initialize, callback);
        }
        private void ExecuteTest2(bool useCurrentUser, Action<IServiceCollection> initializeServices, Action<RepositoryBuilder> initialize, Action callback)
        {
            OnTestInitialize();

            var builder = CreateRepositoryBuilderForTest(initializeServices);
            initialize?.Invoke(builder);

            Indexing.IsOuterSearchEngineEnabled = true;

            Cache.Reset();
            ResetContentTypeManager();

            OnBeforeRepositoryStart(builder);

            using (Repository.Start(builder))
            {
                PrepareRepository();

                User.Current = User.Administrator;
                if (useCurrentUser)
                    callback();
                else
                    using (new SystemAccount())
                        callback();
            }

            OnAfterRepositoryShutdown();
        }

        protected void ResetContentTypeManager()
        {
            var acc = new TypeAccessor(typeof(ContentTypeManager));
            acc.InvokeStatic("ResetPrivate");
        }

        protected virtual void OnTestInitialize() { }
        protected virtual void OnBeforeRepositoryStart(RepositoryBuilder builder) { }
        protected virtual void OnAfterRepositoryShutdown() { }

        // ==========================================================

        protected STT.Task Test(Func<STT.Task> callback)
        {
            return Test(false, null, callback);
        }
        protected STT.Task Test(bool useCurrentUser, Func<STT.Task> callback)
        {
            return ExecuteTest(useCurrentUser, null, callback);
        }
        protected STT.Task Test(Action<RepositoryBuilder> initialize, Func<STT.Task> callback)
        {
            return ExecuteTest(false, initialize, callback);
        }
        protected STT.Task Test(bool useCurrentUser, Action<RepositoryBuilder> initialize, Func<STT.Task> callback)
        {
            return ExecuteTest(useCurrentUser, initialize, callback);
        }
        private async STT.Task ExecuteTest(bool useCurrentUser, Action<RepositoryBuilder> initialize, Func<STT.Task> callback)
        {
            OnTestInitialize();

            var builder = CreateRepositoryBuilderForTest();

            initialize?.Invoke(builder);

            Indexing.IsOuterSearchEngineEnabled = true;

            Cache.Reset();
            ResetContentTypeManager();

            OnBeforeRepositoryStart(builder);

            using (Repository.Start(builder))
            {
                PrepareRepository();

                User.Current = User.Administrator;
                if (useCurrentUser)
                    await callback();
                else
                    using (new SystemAccount())
                        await callback();
            }
        }

        protected virtual IServiceProvider CreateServiceProviderForTest(Action<IConfigurationBuilder> modifyConfig = null,
            Action<IServiceCollection> modifyServices = null)
        {
            var configurationBuilder = new ConfigurationBuilder();
            //configuration.AddJsonFile("appSettings.json")
            modifyConfig?.Invoke(configurationBuilder);
            var configuration = configurationBuilder.Build();

            var services = new ServiceCollection()
                    .AddSenseNet(configuration, (repositoryBuilder, provider) =>
                    {
                        repositoryBuilder
                            .BuildInMemoryRepository()
                            .UseLogger(provider)
                            .UseAccessProvider(new UserAccessProvider());
                    })
                    .AddSenseNetInMemoryProviders()

                    //.AddSenseNetWebHooks()
                    .AddSingleton<ITestingDataProvider, InMemoryTestingDataProvider>()
                ;
            modifyServices?.Invoke(services);
            return services.BuildServiceProvider();
        }
        protected virtual RepositoryBuilder CreateRepositoryBuilderForTest(Action<IServiceCollection> modifyServices = null)
        {
            IServiceProvider services;

            var reusesRepository = (bool)(TestContext.Properties["ReusesRepository"] ?? false);
            if (!reusesRepository || Providers.Instance == null)
            {
                services = CreateServiceProviderForTest(modifyServices: modifyServices);
                Providers.Instance = new Providers(services);
            }
            else
            {
                services = Providers.Instance.Services;
            }

            Cache.Reset();
            Providers.Instance.ResetBlobProviders(new ConnectionStringOptions());

            var builder = new RepositoryBuilder(services)
                .UseLogger(new DebugWriteLoggerAdapter())
                .UseAccessProvider(new DesktopAccessProvider())
                .UseInitialData(GetInitialData())
                .UseTestingDataProvider(services.GetRequiredService<ITestingDataProvider>())
                .UseBlobProviderStore(services.GetRequiredService<IBlobProviderStore>())
                .UseBlobMetaDataProvider(services.GetRequiredService<IBlobStorageMetaDataProvider>())
                .UseBlobProviderSelector(services.GetRequiredService<IBlobProviderSelector>())
                .UseStatisticalDataProvider(services.GetRequiredService<IStatisticalDataProvider>())
                .UseSearchEngine(new InMemorySearchEngine(GetInitialIndex()))
                .StartWorkflowEngine(false)
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(SettingsCache))
                .UseTraceCategories("Test", "Event", "Custom") as RepositoryBuilder;

            ContentTypeManager.Reset();

            return builder;
        }

        protected void AssertSequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            var e = string.Join(", ", expected.Select(x => x.ToString()));
            var a = string.Join(", ", actual.Select(x => x.ToString()));
            Assert.AreEqual(e, a);
        }

        protected async STT.Task SaveInitialIndexDocumentsAsync(CancellationToken cancellationToken)
        {
            var idSet = await Providers.Instance.DataStore.LoadNotIndexedNodeIdsAsync(0, 11000, cancellationToken).ConfigureAwait(false);
            var nodes = Node.LoadNodes(idSet);

            if (nodes.Count == 0)
                return;

            var tasks = nodes.Select(async node =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Providers.Instance.DataStore.SaveIndexDocumentAsync(node, false, false, cancellationToken).ConfigureAwait(false);
            });
            
            await tasks.WhenAll();
        }

        protected string RemoveWhitespaces(string input)
        {
            return input
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace("\t", "")
                .Replace(" ", "");
        }

        protected string ArrayToString(int[] array, bool sort = false)
        {
            var strings = (IEnumerable<string>)array.Select(x => x.ToString()).ToArray();
            if (sort)
                strings = strings.OrderBy(x => x);
            return string.Join(",", strings);
        }
        protected string ArrayToString(IEnumerable<object> array, bool sort = false)
        {
            var strings = (IEnumerable<string>)array.Select(x => x.ToString()).ToArray();
            if (sort)
                strings = strings.OrderBy(x => x);
            return string.Join(",", strings);
        }

        protected void RebuildIndex()
        {
            // ReSharper disable once CollectionNeverQueried.Local
            var paths = new List<string>();
            var populator = Providers.Instance.SearchManager.GetIndexPopulator();
            populator.NodeIndexed += (o, e) => { paths.Add(e.Path); };
            populator.ClearAndPopulateAllAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Writes the content of the in memory index to disk.
        /// The "directoryName" is expected, the "fileNameWithoutExtension" is optional.
        /// If the fileName is not provided, the caller method name will be used.
        /// </summary>
        protected void SaveIndex(string directoryName, [System.Runtime.CompilerServices.CallerMemberName] string fileNameWithoutExtension = null)
        {
            var fname = Path.Combine(directoryName, fileNameWithoutExtension + ".txt");

            if (Providers.Instance.SearchManager.SearchEngine is InMemorySearchEngine searchEngine)
                searchEngine.Index.Save(fname);
            else
                throw new NotSupportedException($"Index cannot be saved if the engine is " +
                                                $"{Providers.Instance.SearchManager.SearchEngine.GetType().FullName}. " +
                                                $"Only the InMemorySearchEngine is allowed.");
        }

        /// <summary>
        /// Enables to write every IndexDocument to a local disk directory.
        /// The method is designed for trace index modifications in a whole test method.
        /// But if the trace should be turned off, use null as the parameter value or use the method as using block:
        /// using(SaveIndexDocuments("c:\tracedir")) { }
        /// </summary>
        protected IDisposable SaveIndexDocuments(string directoryName)
        {
            if (Providers.Instance.SearchManager.SearchEngine is InMemorySearchEngine searchEngine)
                searchEngine.Index.IndexDocumentPath = directoryName;
            else
                throw new NotSupportedException($"IndexDocuments cannot be saved if the engine is " +
                                                $"{Providers.Instance.SearchManager.SearchEngine.IndexingEngine.GetType().FullName}. " +
                                                $"Only the InMemoryIndexingEngine is allowed.");
            return new SaveIndexDocumentsBlock();
        }
        private class SaveIndexDocumentsBlock : IDisposable
        {
            public void Dispose()
            {
                if (Providers.Instance.SearchManager.SearchEngine is InMemorySearchEngine searchEngine)
                    searchEngine.Index.IndexDocumentPath = null;
            }
        }

        protected sealed class CurrentUserBlock : IDisposable
        {
            private readonly IUser _backup;
            public CurrentUserBlock(IUser user)
            {
                _backup = User.Current;
                User.Current = user;
            }
            public void Dispose()
            {
                User.Current = _backup;
            }
        }

        protected static ContentQuery CreateSafeContentQuery(string qtext, QuerySettings settings = null)
        {
            var cquery = ContentQuery.CreateQuery(qtext, settings ?? QuerySettings.AdminSettings);
            var cqueryAcc = new ObjectAccessor(cquery);
            cqueryAcc.SetFieldOrProperty("IsSafe", true);
            return cquery;
        }

        protected static readonly string CarContentType = @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='Car' parentType='ListItem' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <DisplayName>Car,DisplayName</DisplayName>
  <Description>Car,Description</Description>
  <Icon>Car</Icon>
  <AllowIncrementalNaming>true</AllowIncrementalNaming>
  <Fields>
    <Field name='Name' type='ShortText'/>
    <Field name='Make' type='ShortText'/>
    <Field name='Model' type='ShortText'/>
    <Field name='Style' type='Choice'>
      <Configuration>
        <AllowMultiple>false</AllowMultiple>
        <AllowExtraValue>true</AllowExtraValue>
        <Options>
          <Option value='Sedan' selected='true'>Sedan</Option>
          <Option value='Coupe'>Coupe</Option>
          <Option value='Cabrio'>Cabrio</Option>
          <Option value='Roadster'>Roadster</Option>
          <Option value='SUV'>SUV</Option>
          <Option value='Van'>Van</Option>
        </Options>
      </Configuration>
    </Field>
    <Field name='StartingDate' type='DateTime'/>
    <Field name='Color' type='Color'>
      <Configuration>
        <DefaultValue>#ff0000</DefaultValue>
        <Palette>#ff0000;#f0d0c9;#e2a293;#d4735e;#65281a</Palette>
      </Configuration>
    </Field>
    <Field name='EngineSize' type='ShortText'/>
    <Field name='Power' type='ShortText'/>
    <Field name='Price' type='Number'/>
    <Field name='Description' type='RichText'/>
  </Fields>
</ContentType>
";
        protected static void InstallCarContentType()
        {
            ContentTypeInstaller.InstallContentType(CarContentType);
        }


        protected void PrepareRepository()
        {
            // Index
            if (Providers.Instance.SearchEngine is InMemorySearchEngine searchEngine)
                if(searchEngine.Index == null)
                    searchEngine.Index = GetInitialIndex();
            //throw new Exception("Only an InMemorySearchEngine is allowed here.");
        }

        private static InitialData _initialData;
        protected static InitialData GetInitialData()
        {
            return _initialData ?? (_initialData = InitialData.Load(InMemoryTestData.Instance, null));
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
