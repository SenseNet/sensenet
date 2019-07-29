using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Tests.Implementations;
using STT = System.Threading.Tasks;

namespace SenseNet.Tests
{
    [TestClass]
    public class TestBase
    {
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void InitializeTest()
        {
            // workaround for having a half-started repository
            if (RepositoryInstance.Started())
                RepositoryInstance.Shutdown();

            // the original collector default value is a class that is not available in this context
            Providers.PropertyCollectorClassName = typeof(EventPropertyCollector).FullName;

            SnTrace.Test.Enabled = true;
            SnTrace.Test.Write("START test: {0}", TestContext.TestName);
        }

        [TestCleanup]
        public void CleanupTest()
        {
            SnTrace.Test.Enabled = true;
            SnTrace.Test.Write("END test: {0}", TestContext.TestName);
            SnTrace.Flush();
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
            Cache.Reset();
            ContentTypeManager.Reset();
            Providers.Instance.NodeTypeManeger = null;
            
            OnTestInitialize();

            var builder = CreateRepositoryBuilderForTestInstance();

            initialize?.Invoke(builder);

            Indexing.IsOuterSearchEngineEnabled = true;

            Cache.Reset();
            ContentTypeManager.Reset();

            OnBeforeRepositoryStart(builder);

            using (Repository.Start(builder))
            {
                PrepareRepository();

                if (useCurrentUser)
                    callback();
                else
                    using (new SystemAccount())
                        callback();
            }

            OnAfterRepositoryShutdown();
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
        private STT.Task ExecuteTest(bool useCurrentUser, Action<RepositoryBuilder> initialize, Func<STT.Task> callback)
        {
            Cache.Reset();
            ContentTypeManager.Reset();

            var builder = CreateRepositoryBuilderForTestInstance();

            initialize?.Invoke(builder);

            Indexing.IsOuterSearchEngineEnabled = true;

            using (Repository.Start(builder))
            {
                PrepareRepository();

                if (useCurrentUser)
                    return callback();
                using (new SystemAccount())
                    return callback();
            }
        }


        protected static RepositoryBuilder CreateRepositoryBuilderForTest()
        {
            var dataProvider = new InMemoryDataProvider();
            Providers.Instance.DataProvider = dataProvider;
            DataStore.InstallInitialDataAsync(GetInitialData()).Wait();

            return new RepositoryBuilder()
                .UseAccessProvider(new DesktopAccessProvider())
                .UseDataProvider(dataProvider)
                .UseSharedLockDataProviderExtension(new InMemorySharedLockDataProvider())
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dataProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseAccessTokenDataProviderExtension(new InMemoryAccessTokenDataProvider())
                .UseSearchEngine(new InMemorySearchEngine(GetInitialIndex()))
                .UseSecurityDataProvider(GetSecurityDataProvider(dataProvider))
                .UseTestingDataProviderExtension(new InMemoryTestingDataProvider())
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .StartWorkflowEngine(false)
                .DisableNodeObservers()
                .EnableNodeObservers(typeof(SettingsCache))
                .UseTraceCategories("Test", "Event", "Custom") as RepositoryBuilder;
        }

        protected virtual RepositoryBuilder CreateRepositoryBuilderForTestInstance()
        {
            return CreateRepositoryBuilderForTest();
        }

        protected static ISecurityDataProvider GetSecurityDataProvider(InMemoryDataProvider repo)
        {
            return new MemoryDataProvider(new DatabaseStorage
            {
                Aces = new List<StoredAce>
                {
                    new StoredAce {EntityId = 2, IdentityId = 1, LocalOnly = false, AllowBits = 0x0EF, DenyBits = 0x000}
                },
                Entities = repo.LoadEntityTreeAsync().Result.ToDictionary(x => x.Id, x => new StoredSecurityEntity
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

        protected void AssertSequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            var e = string.Join(", ", expected.Select(x => x.ToString()));
            var a = string.Join(", ", actual.Select(x => x.ToString()));
            Assert.AreEqual(e, a);
        }

        protected void SaveInitialIndexDocuments()
        {
            var idSet = DataStore.LoadNotIndexedNodeIdsAsync(0, 11000).Result;
            var nodes = Node.LoadNodes(idSet);

            if (nodes.Count == 0)
                return;

            foreach (var node in nodes)
            {
                DataStore.SaveIndexDocumentAsync(node, false, false).Wait();
            }
        }

        protected string ArrayToString(int[] array)
        {
            return string.Join(",", array.Select(x => x.ToString()));
        }
        protected string ArrayToString(List<int> array)
        {
            return string.Join(",", array.Select(x => x.ToString()));
        }
        protected string ArrayToString(IEnumerable<object> array)
        {
            return string.Join(",", array.Select(x => x.ToString()));
        }

        protected void RebuildIndex()
        {
            // ReSharper disable once CollectionNeverQueried.Local
            var paths = new List<string>();
            var populator = SearchManager.GetIndexPopulator();
            populator.NodeIndexed += (o, e) => { paths.Add(e.Path); };
            populator.ClearAndPopulateAll();
        }

        /// <summary>
        /// Writes the content of the in memory index to disk.
        /// The "directoryName" is expected, the "fileNameWithoutExtension" is optional.
        /// If the fileName is not provided, the caller method name will be used.
        /// </summary>
        protected void SaveIndex(string directoryName, [System.Runtime.CompilerServices.CallerMemberName] string fileNameWithoutExtension = null)
        {
            var fname = Path.Combine(directoryName, fileNameWithoutExtension + ".txt");

            if (SearchManager.SearchEngine is InMemorySearchEngine searchEngine)
                searchEngine.Index.Save(fname);
            else
                throw new NotSupportedException($"Index cannot be saved if the engine is {SearchManager.SearchEngine.GetType().FullName}. Only the InMemorySearchEngine is allowed.");
        }

        /// <summary>
        /// Enables to write every IndexDocument to a local disk directory.
        /// The method is designed for trace index modifications in a whole test method.
        /// But if the trace should be turned off, use null as the parameter value or use the method as using block:
        /// using(SaveIndexDocuments("c:\tracedir")) { }
        /// </summary>
        protected IDisposable SaveIndexDocuments(string directoryName)
        {
            if (SearchManager.SearchEngine is InMemorySearchEngine searchEngine)
                searchEngine.Index.IndexDocumentPath = directoryName;
            else
                throw new NotSupportedException($"IndexDocuments cannot be saved if the engine is {SearchManager.SearchEngine.IndexingEngine.GetType().FullName}. Only the InMemoryIndexingEngine is allowed.");
            return new SaveIndexDocumentsBlock();
        }
        private class SaveIndexDocumentsBlock : IDisposable
        {
            public void Dispose()
            {
                if (SearchManager.SearchEngine is InMemorySearchEngine searchEngine)
                    searchEngine.Index.IndexDocumentPath = null;
            }
        }

        protected static ContentQuery CreateSafeContentQuery(string qtext)
        {
            var cquery = ContentQuery.CreateQuery(qtext, QuerySettings.AdminSettings);
            var cqueryAcc = new PrivateObject(cquery);
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
    <Field name='Description' type='LongText'/>
  </Fields>
</ContentType>
";
        protected static void InstallCarContentType()
        {
            ContentTypeInstaller.InstallContentType(CarContentType);
        }



        protected void PrepareRepository()
        {
            // Improve Debug experience
            new SnMaintenance().Shutdown();

            // Index
            if (Providers.Instance.SearchEngine is InMemorySearchEngine searchEngine)
                if(searchEngine.Index == null)
                    searchEngine.Index = GetInitialIndex();
            //throw new Exception("Only an InMemorySearchEngine is allowed here.");
        }

        private static InitialData _initialData;
        protected static InitialData GetInitialData()
        {
            var dataFile = new InitialTestData();
            if (_initialData == null)
            {
                InitialData initialData;
                using (var ptr = new StringReader(dataFile.PropertyTypes))
                using (var ntr = new StringReader(dataFile.NodeTypes))
                using (var nr = new StringReader(dataFile.Nodes))
                using (var vr = new StringReader(dataFile.Versions))
                using (var dr = new StringReader(dataFile.DynamicData))
                    initialData = InitialData.Load(ptr, ntr, nr, vr, dr);
                initialData.ContentTypeDefinitions = dataFile.ContentTypeDefinitions;
                initialData.Blobs = dataFile.Blobs;
                _initialData = initialData;
            }
            return _initialData;
        }

        private static InMemoryIndex _initialIndex;
        protected static InMemoryIndex GetInitialIndex()
        {
            if (_initialIndex == null)
            {
                var index = new InMemoryIndex();
                index.Load(new StringReader(InitialTestIndex.Index));
                _initialIndex = index;
            }
            return _initialIndex.Clone();
        }
    }
}
