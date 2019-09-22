using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Volatile;
using SenseNet.OData;
using SenseNet.Search;
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Tests;
using SenseNet.Tests.Accessors;

// ReSharper disable InconsistentNaming

namespace SenseNet.ODataTests
{
    public class ODataTestBase
    {
        #region Infrastructure

        private static RepositoryInstance _repositoryForGet;

        protected static RepositoryBuilder CreateRepositoryBuilder()
        {
            var dataProvider = new InMemoryDataProvider();
            Providers.Instance.DataProvider = dataProvider;

            return new RepositoryBuilder()
                .UseAccessProvider(new DesktopAccessProvider())
                .UseDataProvider(dataProvider)
                .UseSharedLockDataProviderExtension(new InMemorySharedLockDataProvider())
                .UseBlobMetaDataProvider(new InMemoryBlobStorageMetaDataProvider(dataProvider))
                .UseBlobProviderSelector(new InMemoryBlobProviderSelector())
                .UseAccessTokenDataProviderExtension(new InMemoryAccessTokenDataProvider())
                .UseSearchEngine(new InMemorySearchEngine(GetInitialIndex()))
                .UseSecurityDataProvider(GetSecurityDataProvider(dataProvider))
                .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
                .StartWorkflowEngine(false)
                //.DisableNodeObservers()
                //.EnableNodeObservers(typeof(SettingsCache))
                .UseTraceCategories("Test", "Event", "Custom") as RepositoryBuilder;
        }
        protected static ISecurityDataProvider GetSecurityDataProvider(InMemoryDataProvider repo)
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

        private static InitialData _initialData;
        protected static InitialData GetInitialData()
        {
            return _initialData ?? (_initialData = InitialData.Load(InitialTestData.Instance));
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

        [ClassCleanup]
        public void CleanupClass()
        {
            _repositoryForGet?.Dispose();
        }
        #endregion

        protected void ODataTest(Action callback)
        {
            if (_repositoryForGet == null)
            {
                var repoBuilder = CreateRepositoryBuilder();
                DataStore.InstallInitialDataAsync(GetInitialData(), CancellationToken.None).GetAwaiter().GetResult();
                Indexing.IsOuterSearchEngineEnabled = true;
                _repositoryForGet = Repository.Start(repoBuilder);
            }

            using (new SystemAccount())
                callback();
        }

        protected void IsolatedODataTest(Action callback)
        {
            var repoBuilder = CreateRepositoryBuilder();
            DataStore.InstallInitialDataAsync(GetInitialData(), CancellationToken.None).GetAwaiter().GetResult();
            Indexing.IsOuterSearchEngineEnabled = true;
            using (Repository.Start(repoBuilder))
            using (new SystemAccount())
                callback();
        }

        internal static T ODataGET<T>(string resource, string queryString) where T : ODataResponse
        {
            var httpContext = new DefaultHttpContext();
            var request = httpContext.Request;
            request.Method = "GET";
            request.Path = resource;
            request.QueryString = new QueryString(queryString);

            var odata = new ODataMiddleware(null);

            var odataRequest = ODataRequest.Parse(httpContext);
            return (T)odata.ProcessRequest(httpContext, odataRequest);
        }

        /* ========================================================================= TOOLS */

        protected static ContentQuery CreateSafeContentQuery(string qtext)
        {
            var cquery = ContentQuery.CreateQuery(qtext, QuerySettings.AdminSettings);
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
    <Field name='Description' type='LongText'/>
  </Fields>
</ContentType>
";
        protected static void InstallCarContentType()
        {
            ContentTypeInstaller.InstallContentType(CarContentType);
        }

    }
}
