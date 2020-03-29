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
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.Diagnostics;
using SenseNet.OData;
using SenseNet.ODataTests.Responses;
using SenseNet.Packaging.Steps;
using SenseNet.Search;
using SenseNet.Security;
using SenseNet.Security.Data;
using SenseNet.Tests;
using SenseNet.Tests.Accessors;
using Task = System.Threading.Tasks.Task;
// ReSharper disable StringLiteralTypo

namespace SenseNet.ODataTests
{
    #region Additional classes

    public class ODataResponse
    {
        public int StatusCode { get; set; }
        public string Result { get; set; }
    }

    internal class ODataTestsCustomActions
    {
        [ODataAction]
        public static string ParameterEcho(Content content, string testString)
        {
            return testString;
        }
    }
    internal class ODataFilterTestHelper
    {
        public static string TestValue => "Administrators";

        internal class A
        {
            internal class B
            {
                // ReSharper disable once MemberHidesStaticFromOuterClass
                public static string TestValue { get; } = "Administrators";
            }
        }
    }

    [ContentHandler]
    internal class OData_Filter_ThroughReference_ContentHandler : GenericContent
    {
        public const string CTD = @"<?xml version='1.0' encoding='utf-8'?>
    <ContentType name='OData_Filter_ThroughReference_ContentHandler' parentType='GenericContent' handler='SenseNet.ODataTests.OData_Filter_ThroughReference_ContentHandler' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
      <Fields>
        <Field name='References' type='Reference'>
          <Configuration>
            <AllowMultiple>true</AllowMultiple>
            <AllowedTypes>
              <Type>OData_Filter_ThroughReference_ContentHandler</Type>
            </AllowedTypes>
          </Configuration>
        </Field>
      </Fields>
    </ContentType>
    ";
        public OData_Filter_ThroughReference_ContentHandler(Node parent) : this(parent, null) { }
        public OData_Filter_ThroughReference_ContentHandler(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected OData_Filter_ThroughReference_ContentHandler(NodeToken token) : base(token) { }

        public const string REFERENCES = "References";
        [RepositoryProperty(REFERENCES, RepositoryDataType.Reference)]
        public IEnumerable<Node> References
        {
            get { return this.GetReferences(REFERENCES); }
            set { this.SetReferences(REFERENCES, value); }
        }

    }
    [ContentHandler]
    internal class OData_ReferenceTest_ContentHandler : GenericContent
    {
        public const string CTD = @"<?xml version='1.0' encoding='utf-8'?>
    <ContentType name='OData_ReferenceTest_ContentHandler' parentType='GenericContent' handler='SenseNet.ODataTests.OData_ReferenceTest_ContentHandler' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
      <Fields>
        <Field name='Reference' type='Reference'>
          <Configuration>
            <AllowMultiple>false</AllowMultiple>
          </Configuration>
        </Field>
        <Field name='References' type='Reference'>
          <Configuration>
            <AllowMultiple>true</AllowMultiple>
          </Configuration>
        </Field>
        <Field name='Reference2' type='Reference'>
          <Configuration>
            <AllowMultiple>false</AllowMultiple>
          </Configuration>
        </Field>
      </Fields>
    </ContentType>
    ";
        public OData_ReferenceTest_ContentHandler(Node parent) : this(parent, null) { }
        public OData_ReferenceTest_ContentHandler(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected OData_ReferenceTest_ContentHandler(NodeToken token) : base(token) { }

        public const string REFERENCES = "References";
        [RepositoryProperty(REFERENCES, RepositoryDataType.Reference)]
        public IEnumerable<Node> References
        {
            get { return this.GetReferences(REFERENCES); }
            set { this.SetReferences(REFERENCES, value); }
        }

        public const string REFERENCE = "Reference";
        [RepositoryProperty(REFERENCE, RepositoryDataType.Reference)]
        public Node Reference
        {
            get { return this.GetReference<Node>(REFERENCE); }
            set { this.SetReference(REFERENCE, value); }
        }

        public const string REFERENCE2 = "Reference2";
        [RepositoryProperty(REFERENCE2, RepositoryDataType.Reference)]
        public Node Reference2
        {
            get { return this.GetReference<Node>(REFERENCE2); }
            set { this.SetReference(REFERENCE2, value); }
        }

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case REFERENCE: return this.Reference;
                case REFERENCE2: return this.Reference2;
                case REFERENCES: return this.References;
                default: return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case REFERENCE: this.Reference = (Node)value; break;
                case REFERENCE2: this.Reference2 = (Node)value; break;
                case REFERENCES: this.References = (IEnumerable<Node>)value; break;
                default: base.SetProperty(name, value); break;
            }
        }
    }

    #endregion

    [TestClass]
    public class ODataTestBase
    {
        public TestContext TestContext { get; set; }

        private SnTrace.Operation _testMethodOperation;

        [TestInitialize]
        public void InitializeTest()
        {
            //// workaround for having a half-started repository
            //if (RepositoryInstance.Started())
            //    RepositoryInstance.Shutdown();

            SnTrace.Test.Enabled = true;
            //SnTrace.Test.Write("START test: {0}", TestContext.TestName);
            if (_testMethodOperation != null)
            {
                SnTrace.Test.Write("The operation was forced to close.");
                _testMethodOperation.Successful = false;
                _testMethodOperation.Dispose();
            }
            _testMethodOperation = SnTrace.Test.StartOperation("TESTMETHOD: " + TestContext.TestName);
        }

        [TestCleanup]
        public void CleanupTest()
        {
            SnTrace.Test.Enabled = true;
            //SnTrace.Test.Write("END test: {0}", TestContext.TestName);

            if (_testMethodOperation != null)
            {
                _testMethodOperation.Successful = true;
                _testMethodOperation.Dispose();
            }

            SnTrace.Flush();
        }

        #region Infrastructure

        private static RepositoryInstance _repository;

        protected static RepositoryBuilder CreateRepositoryBuilder()
        {
            var dataProvider = new InMemoryDataProvider();

            return new RepositoryBuilder()
                .UseAccessProvider(new DesktopAccessProvider())
                .UseDataProvider(dataProvider)
                .UseInitialData(GetInitialData())
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
            return _initialData ?? (_initialData = InitialData.Load(InMemoryTestData.Instance));
        }

        private static InMemoryIndex _initialIndex;
        protected static InMemoryIndex GetInitialIndex()
        {
            var index = new InMemoryIndex();
            index.Load(new StringReader(InMemoryTestIndex.Index));
            _initialIndex = index;
            return _initialIndex;
        }

        [ClassCleanup]
        public void CleanupClass()
        {
            _repository?.Dispose();
        }
        #endregion

        protected void ODataTest(Action callback)
        {
            ODataTestAsync(null, null, () =>
            {
                callback();
                return Task.CompletedTask;
            }, true).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        protected void ODataTest(IUser user, Action callback)
        {
            ODataTestAsync(user, null, () =>
            {
                callback();
                return Task.CompletedTask;
            }, true).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        protected void ODataTest(Action<RepositoryBuilder> initialize, Action callback)
        {
            ODataTestAsync(null, initialize, () =>
            {
                callback();
                return Task.CompletedTask;
            }, true).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        protected Task ODataTestAsync(Func<Task> callback)
        {
            return ODataTestAsync(null, null, callback, true);
        }
        protected Task ODataTestAsync(IUser user, Func<Task> callback)
        {
            return ODataTestAsync(user, null, callback, true);
        }

        protected void IsolatedODataTest(Action callback)
        {
            IsolatedODataTestAsync(null, null, () =>
            {
                callback();
                return Task.CompletedTask;
            }).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        protected void IsolatedODataTest(Action<RepositoryBuilder> initialize, Action callback)
        {
            IsolatedODataTestAsync(null, initialize, () =>
            {
                callback();
                return Task.CompletedTask;
            }).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        protected Task IsolatedODataTestAsync(Func<Task> callback)
        {
            return IsolatedODataTestAsync(null, null, callback);
        }
        protected Task IsolatedODataTestAsync(Action<RepositoryBuilder> initialize, Func<Task> callback)
        {
            return IsolatedODataTestAsync(null, initialize, callback);
        }
        protected Task IsolatedODataTestAsync(IUser user, Func<Task> callback)
        {
            return IsolatedODataTestAsync(user, null, callback);
        }
        protected Task IsolatedODataTestAsync(IUser user, Action<RepositoryBuilder> initialize, Func<Task> callback)
        {
            return ODataTestAsync(user, initialize, callback, false);
        }

        private async Task ODataTestAsync(IUser user, Action<RepositoryBuilder> initialize, Func<Task> callback, bool reused)
        {
            Cache.Reset();

            if (!reused || _repository == null)
            {
                _repository?.Dispose();
                _repository = null;

                var repoBuilder = CreateRepositoryBuilder();
                if (initialize != null)
                    initialize(repoBuilder);

                Indexing.IsOuterSearchEngineEnabled = true;
                _repository = Repository.Start(repoBuilder);
            }

            if (user == null)
            {
                using (new SystemAccount())
                    await callback().ConfigureAwait(false);
            }
            else
            {
                IUser backup = null;
                try
                {
                    backup = User.Current;
                    User.Current = user;
                    await callback().ConfigureAwait(false);
                }
                finally
                {
                    User.Current = backup;
                }
            }

            if (!reused)
            {
                _repository?.Dispose();
                _repository = null;
            }
        }

        internal static Task<ODataResponse> ODataGetAsync(string resource, string queryString)
        {
            return ODataProcessRequestAsync(resource, queryString, null, "GET");
        }
        internal static Task<ODataResponse> ODataDeleteAsync(string resource, string queryString)
        {
            return ODataProcessRequestAsync(resource, queryString, null, "DELETE");
        }
        internal static Task<ODataResponse> ODataPutAsync(string resource, string queryString, string requestBodyJson)
        {
            return ODataProcessRequestAsync(resource, queryString, requestBodyJson, "PUT");
        }
        internal static Task<ODataResponse> ODataPatchAsync(string resource, string queryString, string requestBodyJson)
        {
            return ODataProcessRequestAsync(resource, queryString, requestBodyJson, "PATCH");
        }
        internal static Task<ODataResponse> ODataMergeAsync(string resource, string queryString, string requestBodyJson)
        {
            return ODataProcessRequestAsync(resource, queryString, requestBodyJson, "MERGE");
        }
        internal static Task<ODataResponse> ODataPostAsync(string resource, string queryString, string requestBodyJson)
        {
            return ODataProcessRequestAsync(resource, queryString, requestBodyJson, "POST");
        }
        internal static Task<ODataResponse> ODataCallAsync(string resource, string queryString, string requestBodyJson, string httpMethod)
        {
            return ODataProcessRequestAsync(resource, queryString, requestBodyJson, httpMethod);
        }
        private static async Task<ODataResponse> ODataProcessRequestAsync(string resource, string queryString,
            string requestBodyJson, string httpMethod)
        {
            var httpContext = CreateHttpContext(resource, queryString);
            var request = httpContext.Request;
            request.Method = httpMethod;
            request.Path = resource;
            request.QueryString = new QueryString(queryString);
            if(requestBodyJson != null)
                request.Body = CreateRequestStream(requestBodyJson);

            httpContext.Response.Body = new MemoryStream();

            var odata = new ODataMiddleware(null);
            var odataRequest = ODataRequest.Parse(httpContext);
            await odata.ProcessRequestAsync(httpContext, odataRequest).ConfigureAwait(false);

            var responseOutput = httpContext.Response.Body;
            responseOutput.Seek(0, SeekOrigin.Begin);
            string output;
            using (var reader = new StreamReader(responseOutput))
                output = await reader.ReadToEndAsync().ConfigureAwait(false);

            return new ODataResponse { Result = output, StatusCode = httpContext.Response.StatusCode };
        }

        internal static HttpContext CreateHttpContext(string resource, string queryString)
        {
            var httpContext = new DefaultHttpContext();
            var request = httpContext.Request;
            request.Method = "GET";
            request.Path = resource;
            request.QueryString = new QueryString(queryString);
            httpContext.Response.Body = new MemoryStream();
            return httpContext;
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

        protected static void EnsureManagerOfAdmin()
        {
            Cache.Reset();
            var content = Content.Create(User.Administrator);
            if (((IEnumerable<Node>)content["Manager"]).Any())
                return;
            content["Manager"] = User.Administrator;
            content["Email"] = "anybody@somewhere.com";
            content.Save();
        }


        protected static Workspace CreateWorkspace(string name = null)
        {
            var workspaces = Node.LoadNode("/Root/Workspaces");
            if (workspaces == null)
            {
                workspaces = new Folder(Repository.Root) { Name = "Workspaces" };
                workspaces.Save();
            }

            var workspace = new Workspace(workspaces) { Name = name ?? Guid.NewGuid().ToString() };
            workspace.Save();

            return workspace;
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


        protected static JObject GetObject(ODataResponse response)
        {
            return (JObject)Deserialize(response.Result);
        }
        protected static ODataEntityResponse GetEntity(ODataResponse response)
        {
            var text = response.Result;
            var jo = (JObject)Deserialize(text);
            return ODataEntityResponse.Create((JObject)jo["d"]);
        }
        protected static ODataEntitiesResponse GetEntities(ODataResponse response)
        {
            var text = response.Result;

            var result = new List<ODataEntityResponse>();
            var jo = (JObject)Deserialize(text);
            var d = (JObject)jo["d"];
            var count = d["__count"].Value<int>();
            var jarray = (JArray)d["results"];
            for (int i = 0; i < jarray.Count; i++)
                result.Add(ODataEntityResponse.Create((JObject)jarray[i]));
            return new ODataEntitiesResponse(result.ToList(), count);
        }

        protected static ODataErrorResponse GetError(ODataResponse response, bool throwOnError = true)
        {
            var text = response.Result;
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            var json = Deserialize(text);
            if (json == null)
            {
                if (throwOnError)
                    throw new InvalidOperationException("Deserialized text is null.");
                return null;
            }

            if(json is JArray)
            {
                if (throwOnError)
                    throw new InvalidOperationException("Object is not an error");
                return null;
            }

            if (!(json["error"] is JObject error))
            {
                if (throwOnError)
                    throw new Exception("Object is not an error");
                return null;
            }

            var code = error["code"]?.Value<string>() ?? string.Empty;
            var exceptionType = error["exceptiontype"]?.Value<string>() ?? string.Empty;
            var message = error["message"] as JObject;
            var value = message?["value"]?.Value<string>() ?? string.Empty;
            var innerError = error["innererror"] as JObject;
            var trace = innerError?["trace"]?.Value<string>() ?? string.Empty;
            Enum.TryParse<ODataExceptionCode>(code, out var oeCode);
            return new ODataErrorResponse { Code = oeCode, ExceptionType = exceptionType, Message = value, StackTrace = trace };
        }
        protected void AssertNoError(ODataResponse response)
        {
            var error = GetError(response, false);
            if (error != null)
                Assert.Fail(error.Message);
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

        private static Stream CreateRequestStream(string request)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(request);
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        protected string RemoveWhitespaces(string input)
        {
            return input
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace("\t", "")
                .Replace(" ", "");
        }

        protected string ArrayToString(int[] array)
        {
            return string.Join(",", array.Select(x => x.ToString()));
        }
        protected string ArrayToString(List<int> array)
        {
            return string.Join(",", array.Select(x => x.ToString()));
        }
        protected string ArrayToString(IEnumerable<object> array, bool sort = false)
        {
            var strings = (IEnumerable<string>)array.Select(x => x.ToString()).ToArray();
            if (sort)
                strings = strings.OrderBy(x => x);
            return string.Join(",", strings);
        }

        protected class CurrentUserBlock : IDisposable
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

        protected class AllowPermissionBlock : IDisposable
        {
            private int _entityId;
            private int _identityId;
            private bool _localOnly;
            PermissionType[] _permissions;
            public AllowPermissionBlock(int entityId, int identityId, bool localOnly, params PermissionType[] permissions)
            {
                _entityId = entityId;
                _identityId = identityId;
                _localOnly = localOnly;
                _permissions = permissions;

                SecurityHandler.CreateAclEditor()
                    .Allow(entityId, identityId, localOnly, permissions)
                    .Apply();
            }
            public void Dispose()
            {
                SecurityHandler.CreateAclEditor()
                    .ClearPermission(_entityId, _identityId, _localOnly, _permissions)
                    .Apply();
            }
        }

    }
}
