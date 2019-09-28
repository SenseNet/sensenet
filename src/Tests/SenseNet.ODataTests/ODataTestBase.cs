using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
using SenseNet.ODataTests.Responses;
using SenseNet.Search;
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Tests;
using SenseNet.Tests.Accessors;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests
{
    public class ODataResponse
    {
        public int StatusCode { get; set; }
        public string Result { get; set; }
    }
    public class ODataTestBase
    {
        #region Infrastructure

        private static RepositoryInstance _repository;

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
            //UNDONE:ODATA: TEST:BUG: Commented out lines maybe wrong
            //if (_initialIndex == null)
            //{
            //    var index = new InMemoryIndex();
            //    index.Load(new StringReader(InitialTestIndex.Index));
            //    _initialIndex = index;
            //}
            //return _initialIndex.Clone();
            var index = new InMemoryIndex();
            index.Load(new StringReader(InitialTestIndex.Index));
            _initialIndex = index;
            return _initialIndex;
        }

        [ClassCleanup]
        public void CleanupClass()
        {
            _repository?.Dispose();
        }
        #endregion

        protected Task ODataTestAsync(Func<Task> callback)
        {
            return ODataTestAsync(callback, true);
        }

        protected Task IsolatedODataTestAsync(Func<Task> callback)
        {
            return ODataTestAsync(callback, false);
        }

        private async Task ODataTestAsync(Func<Task> callback, bool reused)
        {
            Cache.Reset();

            if (!reused || _repository == null)
            {
                var repoBuilder = CreateRepositoryBuilder();
                await DataStore.InstallInitialDataAsync(GetInitialData(), CancellationToken.None).ConfigureAwait(false);
                Indexing.IsOuterSearchEngineEnabled = true;
                _repository = Repository.Start(repoBuilder);
            }

            using (new SystemAccount())
                await callback().ConfigureAwait(false);

            if (!reused)
            {
                _repository?.Dispose();
                _repository = null;
            }
        }

        internal static async Task<ODataResponse> ODataGetAsync(string resource, string queryString)
        {
            var httpContext = new DefaultHttpContext();
            var request = httpContext.Request;
            request.Method = "GET";
            request.Path = resource;
            request.QueryString = new QueryString(queryString);
            httpContext.Response.Body = new MemoryStream();

            var odata = new ODataMiddleware(null);
            var odataRequest = ODataRequest.Parse(httpContext);
            await odata.ProcessRequestAsync(httpContext, odataRequest).ConfigureAwait(false);

            var responseOutput = httpContext.Response.Body;
            responseOutput.Seek(0, SeekOrigin.Begin);
            string output;
            using (var reader = new StreamReader(responseOutput))
                output = await reader.ReadToEndAsync().ConfigureAwait(false);

            return new ODataResponse {Result = output, StatusCode = httpContext.Response.StatusCode};
        }
        //internal static T ODataGET<T>(string resource, string queryString) where T : ODataResponse
        //{
        //    var httpContext = new DefaultHttpContext();
        //    var request = httpContext.Request;
        //    request.Method = "GET";
        //    request.Path = resource;
        //    request.QueryString = new QueryString(queryString);

        //    var responseOutput = httpContext.Response.Body;
        //    responseOutput.Seek(0, SeekOrigin.Begin);
        //    string output;
        //    using (var reader = new StreamReader(responseOutput))
        //        output = reader.ReadToEndAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        //    var odata = new ODataMiddleware(null);

        //    var odataRequest = ODataRequest.Parse(httpContext);
        //    return (T)odata.ProcessRequest(httpContext, odataRequest);
        //}

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

        protected static SystemFolder CreateTestRoot(string name = null)
        {
            return CreateTestRoot(null, name);
        }
        protected static SystemFolder CreateTestRoot(Node parent, string name = null)
        {
            var systemFolder = new SystemFolder(parent ?? Repository.Root) { Name = name ?? Guid.NewGuid().ToString() };
            systemFolder.Save();
            return systemFolder;
        }


        protected static ODataEntityResponse GetEntity(string text)
        {
            var result = new Dictionary<string, object>();
            var jo = (JObject)Deserialize(text);
            return ODataEntityResponse.Create((JObject)jo["d"]);
        }
        protected static JContainer Deserialize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            JContainer json;
            using (var reader = new StringReader(text))
                json = Deserialize(reader);
            return json;
        }
        protected static JContainer Deserialize(TextReader reader)
        {
            var models = reader?.ReadToEnd() ?? string.Empty;
            var settings = new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.IsoDateFormat };
            var serializer = JsonSerializer.Create(settings);
            if (serializer == null)
                throw new InvalidOperationException("Serializer could not be created from settings.");

            var jreader = new JsonTextReader(new StringReader(models));
            var x = (JContainer)serializer.Deserialize(jreader);
            return x;
        }

    }
}
