using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Packaging;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.OData;
using SenseNet.ODataTests.Accessors;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;
using SenseNet.Storage;
using File = SenseNet.ContentRepository.File;
using Task = System.Threading.Tasks.Task;
using SSCO = SenseNet.Services.Core.Operations;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataBuiltInOperationMethodTests : ODataTestBase
    {
        private class TestLatestComponentStore : ILatestComponentStore
        {
            private readonly IEnumerable<ReleaseInfo> _releaseData;
            private readonly IDictionary<string, Version> _componentData; 
            public TestLatestComponentStore(IEnumerable<ReleaseInfo> releaseData, IDictionary<string, Version> componentData)
            {
                _releaseData = releaseData;
                _componentData = componentData;
            }

            public Task<IEnumerable<ReleaseInfo>> GetLatestReleases(CancellationToken cancel)
            {
                return Task.FromResult(_releaseData);
            }
            public Task<IDictionary<string, Version>> GetLatestComponentVersions(CancellationToken cancel)
            {
                return Task.FromResult(_componentData);
            }
        }

        /* ====================================================================== RepositoryTools */

        [TestMethod]
        public void OD_MBO_BuiltIn_GetVersionInfo()
        {
            ODataTest(() =>
            {
                var container = new ServiceCollection();
                var latestComponentStore = new TestLatestComponentStore(new[]
                    {
                        new ReleaseInfo
                        {
                            ProductName = "Product1", DisplayName = "Product 1",
                            Version = new Version(1, 2), ReleaseData = DateTime.Today.AddDays(-2.0)
                        },
                        new ReleaseInfo
                        {
                            ProductName = "Product2", DisplayName = "Product 2",
                            Version = new Version(2, 3), ReleaseData = DateTime.Today.AddDays(-1.0)
                        },
                    },
                    new Dictionary<string, Version>
                    {
                        {"Component1", new Version(7, 8)},
                        {"Component2", new Version(6, 7)},
                        {"SenseNet.Services", new Version(7, 8)},
                    });

                container.AddSingleton<ILatestComponentStore>(latestComponentStore);
                var services = container.BuildServiceProvider();

                // ACTION
                var response = ODataGetAsync($"/OData.svc/('Root')/GetVersionInfo", "", services)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                // ASSERT
                AssertNoError(response);
                Assert.AreEqual(200, response.StatusCode);
                var result = GetObject(response);
                Assert.IsNotNull(result["LatestReleases"]);
                Assert.IsNotNull(result["Components"]);
                Assert.IsNotNull(result["Assemblies"]);
                Assert.IsNotNull(result["InstalledPackages"]);
                Assert.IsNotNull(result["DatabaseAvailable"]);
                Assert.AreEqual("Product1", result["LatestReleases"][0]["ProductName"].ToString());
                Assert.AreEqual("Product2", result["LatestReleases"][1]["ProductName"].ToString());
                Assert.AreEqual("1.2", result["LatestReleases"][0]["Version"].ToString());
                Assert.AreEqual("2.3", result["LatestReleases"][1]["Version"].ToString());
                Assert.AreEqual("7.8", result["Components"][0]["LatestVersion"].ToString());
            });
        }

        /* ====================================================================== RepositoryTools */

        [TestMethod]
        public void OD_MBO_BuiltIn_Ancestors()
        {
            ODataTest(() =>
            {
                var response = ODataGetAsync($"/OData.svc/Root/IMS('BuiltIn')/Ancestors",
                        "?metadata=no&$select=Name")
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                AssertNoError(response);
                Assert.AreEqual(200, response.StatusCode);
                var result = GetEntities(response);
                var names = string.Join(",", result.Select(x => x.Name));
                Assert.AreEqual("IMS,Root", names);
            });
        }

        [TestMethod]
        public void OD_MBO_BuiltIn_CheckSecurityConsistency()
        {
            ODataTest(() =>
            {
                var response = ODataGetAsync($"/OData.svc/Root('IMS')/CheckSecurityConsistency", "")
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                AssertNoError(response);
                Assert.AreEqual(200, response.StatusCode);
                var result = GetObject(response);
                Assert.IsNotNull(result["IsConsistent"]);
            });
        }

        [TestMethod]
        public void OD_MBO_BuiltIn_GetAllContentTypes()
        {
            ODataTest(() =>
            {
                var response = ODataGetAsync($"/OData.svc/('Root')/GetAllContentTypes",
                        "?metadata=no")
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                AssertNoError(response);
                Assert.AreEqual(200, response.StatusCode);
                var result = GetEntities(response);
                var names = string.Join(",", result.Select(x => x.Name).OrderBy(x => x));
                var expected = string.Join(",", ContentType.GetContentTypeNames().OrderBy(x => x));
                Assert.AreEqual(expected, names);
            });
        }

        [TestMethod]
        public void OD_MBO_BuiltIn_GetAllowedChildTypesFromCTD()
        {
            ODataTest(() =>
            {
                var response = ODataGetAsync($"/OData.svc/Root('IMS')/GetAllowedChildTypesFromCTD",
                        "?metadata=no&$select=Name")
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                AssertNoError(response);
                Assert.AreEqual(200, response.StatusCode);
                var result = GetEntities(response).ToArray();
                Assert.AreEqual(1, result.Length);
                Assert.AreEqual("Domain", result[0].Name);
            });
        }

        [TestMethod]
        public void OD_MBO_BuiltIn_GetRecentSecurityActivities()
        {
            ODataTest(() =>
            {
                var response = ODataGetAsync($"/OData.svc/('Root')/GetRecentSecurityActivities", "")
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                AssertNoError(response);
                Assert.AreEqual(200, response.StatusCode);
                var result = GetObject(response);
                Assert.IsNotNull(result["RecentLength"]);
                Assert.IsNotNull(result["Recent"]);
                Assert.IsNotNull(result["State"]);
            });
        }

        [TestMethod]
        public void OD_MBO_BuiltIn_GetRecentIndexingActivities()
        {
            ODataTest(() =>
            {
                var response = ODataGetAsync($"/OData.svc/('Root')/GetRecentIndexingActivities", "")
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                AssertNoError(response);
                Assert.AreEqual(200, response.StatusCode);
                var result = GetObject(response);
                Assert.IsNotNull(result["RecentLength"]);
                Assert.IsNotNull(result["Recent"]);
                Assert.IsNotNull(result["State"]);
            });
        }

        [TestMethod]
        public void OD_MBO_BuiltIn_ResetRecentIndexingActivities()
        {
            ODataTest(() =>
            {
                var response = ODataPostAsync($"/OData.svc/('Root')/ResetRecentIndexingActivities",
                        "", "")
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                AssertNoError(response);
                Assert.AreEqual(200, response.StatusCode);
                var result = GetObject(response);
                Assert.IsNotNull(result["RecentLength"]);
                Assert.IsNotNull(result["Recent"]);
                Assert.IsNotNull(result["State"]);
            });
        }

        [TestMethod]
        public void OD_MBO_BuiltIn_TakeLockOver()
        {
            IsolatedODataTest(() =>
            {
                var systemFolderCtdId = ContentType.GetByName("SystemFolder").Id;
                var user = CreateUser("xy@email.com");
                SecurityHandler.CreateAclEditor()
                    .Allow(2, user.Id, false, PermissionType.PermissionTypes)
                    .Allow(systemFolderCtdId, user.Id, false, PermissionType.See)
                    .Apply();

                File file;
                using (new CurrentUserBlock(user))
                {
                    file = new File(CreateTestRoot("TestFiles")) { Name = "File-1" };
                    file.Save();
                    file.CheckOut();
                }

                Assert.AreEqual(user.Id, file.LockedById);

                var url = ODataTools.GetODataUrl(Content.Create(file));
                var response = ODataPostAsync($"{url}/TakeLockOver", "",
                        "models=[{'user':'/Root/IMS/BuiltIn/Portal/Admin'}]")
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                Assert.AreEqual(200, response.StatusCode);
                Assert.AreEqual("Ok", response.Result);
                file = Node.Load<File>(file.Id);
                Assert.AreEqual(Identifiers.AdministratorUserId, file.LockedById);
            });
        }

        [TestMethod]
        public void OD_MBO_BuiltIn_TakeOwnership()
        {
            IsolatedODataTest(() =>
            {
                File file;
                using (new CurrentUserBlock(User.Administrator))
                {
                    file = new File(CreateTestRoot("TestFiles")) { Name = Guid.NewGuid().ToString() };
                    file.Save();
                    Assert.AreEqual(Identifiers.AdministratorUserId, file.OwnerId);
                }

                var user = CreateUser("xy@email.com");

                var url = ODataTools.GetODataUrl(Content.Create(file));
                var response = ODataPostAsync($"{url}/TakeOwnership", "",
                        $"models=[{{'userOrGroup':'{user.Path}'}}]")
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                Assert.AreEqual(204, response.StatusCode);
                file = Node.Load<File>(file.Id);
                Assert.AreEqual(user.Id, file.OwnerId);
            });
        }

        /* ====================================================================== PermissionQueryForRest */

        [TestMethod]
        public void OD_MBO_BuiltIn_GetPermissionInfo()
        {
            ODataTest(() =>
            {
                var response = ODataPostAsync($"/OData.svc/Root('IMS')/GetPermissionInfo", "",
                        "models=[{'identity':'/Root/IMS/BuiltIn/Portal/Admin'}]")
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                AssertNoError(response);
                Assert.AreEqual(200, response.StatusCode);
                var result = GetObject(response);
                Assert.IsNotNull(result["d"]);
                Assert.AreEqual("Admin", result["d"]["identity"]["name"].Value<string>());
            });
        }

        /* ====================================================================== SharingActions */

        [TestMethod]
        public void OD_MBO_BuiltIn_GetSharing()
        {
            ODataTest(() =>
            {
                var response = ODataGetAsync($"/OData.svc/Root('IMS')/GetSharing", "")
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                AssertNoError(response);
                Assert.AreEqual(200, response.StatusCode);
                var result = GetObject(response);
                Assert.IsNotNull(result["d"]);
                Assert.AreEqual(0, result["d"]["__count"].Value<int>());
            });
        }

        /* ====================================================================== DocumentPreviewProvider */

        [TestMethod]
        public void OD_MBO_BuiltIn_GetPreviewImages()
        {
            ODataTest(() =>
            {
                using (var op = new FileOperation())
                {
                    var response = ODataGetAsync($"{op.Url}/GetPreviewImages", "")
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                    AssertNoError(response);
                    Assert.AreEqual(200, response.StatusCode);
                    Assert.IsTrue(response.Result.Contains("\"d\""));
                    Assert.IsTrue(response.Result.Contains("\"__count\""));
                    Assert.IsTrue(response.Result.Contains("\"results\""));
                }
            });
        }
        [TestMethod]
        public void OD_MBO_BuiltIn_GetExistingPreviewImages()
        {
            ODataTest(() =>
            {
                using (var op = new FileOperation())
                {
                    var response = ODataGetAsync($"{op.Url}/GetExistingPreviewImages", "")
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                    AssertNoError(response);
                    Assert.AreEqual(200, response.StatusCode);
                    Assert.AreEqual("[]", response.Result);
                }
            });
        }
        [TestMethod]
        public void OD_MBO_BuiltIn_GetPageCount()
        {
            ODataTest(() =>
            {
                using (var op = new FileOperation())
                {
                    var response = ODataPostAsync($"{op.Url}/GetPageCount", "", "")
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                    Assert.AreEqual(200, response.StatusCode);
                    Assert.AreEqual("-5", response.Result);
                }
            });
        }
        [TestMethod]
        public void OD_MBO_BuiltIn_SetPageCount()
        {
            ODataTest(() =>
            {
                using (var op = new FileOperation(Guid.NewGuid().ToString()))
                {
                    var response = ODataPostAsync($"{op.Url}/SetPageCount", "",
                            "models=[{'pageCount':42}]")
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                    AssertNoError(response);
                    Assert.AreEqual(204, response.StatusCode);
                    Assert.AreEqual(42, Node.Load<File>(op.TheFile.Id).PageCount);
                }
            });
        }
        [TestMethod]
        public void OD_MBO_BuiltIn_GetPreviewsFolder()
        {
            ODataTest(() =>
            {
                using (var op = new FileOperation("File-1"))
                {
                    var response = ODataPostAsync($"{op.Url}/GetPreviewsFolder", "",
                            "models=[{'empty':'false'}]")
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                    AssertNoError(response);
                    Assert.AreEqual(200, response.StatusCode);
                    var result = GetObject(response);
                    Assert.AreEqual("/Root/TestFiles/File-1/Previews/V1.0.A", result["Path"]);
                    Assert.IsNotNull(result["Id"]);
                }
            });
        }

        /* ====================================================================== Index Backup */

        #region Classes for Index Backup tests: Swindler, SearchEngine, IndexingEngine
        internal class SearchEngineSwindler : Swindler<ISearchEngine>
        {
            public SearchEngineSwindler(ISearchEngine searchEngine)
                : base(searchEngine,
                    () => Providers.Instance.SearchEngine,
                    (x) => Providers.Instance.SearchEngine = x)
            {
            }
        }
        private class IndexingEngineForIndexBackupTests : IIndexingEngine
        {
            public bool Running { get; }
            public bool IndexIsCentralized { get; }
            public Task StartAsync(TextWriter consoleOut, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
            public Task ShutDownAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
            public Task<BackupResponse> BackupAsync(string target, CancellationToken cancellationToken)
            {
                return Task.FromResult(new BackupResponse
                {
                    State = BackupState.Started,
                    Current = new BackupInfo
                    {
                        StartedAt = DateTime.UtcNow,
                        TotalBytes = 123456L,
                        CountOfFiles = 42,
                    },
                    History = new BackupInfo[0]
                });
            }
            public Task<BackupResponse> QueryBackupAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(new BackupResponse
                {
                    State = BackupState.Executing,
                    Current = new BackupInfo
                    {
                        StartedAt = DateTime.UtcNow,
                        TotalBytes = 123456L,
                        CountOfFiles = 42,
                        CopiedBytes = 12345L,
                        CopiedFiles = 4,
                        CurrentlyCopiedFile = "File5"
                    },
                    History = new BackupInfo[0]
                });
            }
            public Task<BackupResponse> CancelBackupAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(new BackupResponse
                {
                    State = BackupState.CancelRequested,
                    Current = new BackupInfo
                    {
                        StartedAt = DateTime.UtcNow,
                        TotalBytes = 123456L,
                        CountOfFiles = 42,
                        CopiedBytes = 12345L,
                        CopiedFiles = 4,
                        CurrentlyCopiedFile = "File5",
                        Message = "Canceled"
                    },
                    History = new BackupInfo[0]
                });
            }
            public Task ClearIndexAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
            public Task<IndexingActivityStatus> ReadActivityStatusFromIndexAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
            public Task WriteActivityStatusToIndexAsync(IndexingActivityStatus state, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
            public Task WriteIndexAsync(IEnumerable<SnTerm> deletions, IEnumerable<DocumentUpdate> updates, IEnumerable<IndexDocument> additions,
                CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
        private class SearchEngineForIndexBackupTests:ISearchEngine
        {
            private ISearchEngine _wrapped = Providers.Instance.SearchEngine;

            public IIndexingEngine IndexingEngine { get; } = new IndexingEngineForIndexBackupTests();
            public IQueryEngine QueryEngine => _wrapped.QueryEngine;
            public IDictionary<string, IndexFieldAnalyzer> GetAnalyzers()
            {
                return _wrapped.GetAnalyzers();
            }
            public void SetIndexingInfo(IDictionary<string, IPerFieldIndexingInfo> indexingInfo)
            {
                _wrapped.SetIndexingInfo(indexingInfo);
            }
        }
        #endregion

        [TestMethod]
        public void OD_MBO_BuiltIn_BackupIndex()
        {
            ODataTest(() =>
            {
                using (new SearchEngineSwindler(new SearchEngineForIndexBackupTests()))
                {
                    var response = ODataPostAsync(
                            $"/OData.svc/('Root')/{nameof(RepositoryTools.BackupIndex)}",
                            "",
                            "models=[{'target':'Q:\\\\BackupDir'}]")
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                    AssertNoError(response);
                    Assert.AreEqual(200, response.StatusCode);
                    var result = GetObject(response);
                    Assert.AreEqual(BackupState.Started.ToString(), result["State"].Value<string>());
                    Assert.AreEqual(42, result["Current"]["CountOfFiles"].Value<int>());
                }
            });
        }
        [TestMethod]
        public void OD_MBO_BuiltIn_QueryIndexBackup()
        {
            ODataTest(() =>
            {
                using (new SearchEngineSwindler(new SearchEngineForIndexBackupTests()))
                {
                    var response = ODataPostAsync(
                            $"/OData.svc/('Root')/{nameof(RepositoryTools.QueryIndexBackup)}",
                            "",
                            "")
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                    AssertNoError(response);
                    Assert.AreEqual(200, response.StatusCode);
                    var result = GetObject(response);
                    Assert.AreEqual(BackupState.Executing.ToString(), result["State"].Value<string>());
                    Assert.AreEqual(4, result["Current"]["CopiedFiles"].Value<int>());
                }
            });
        }
        [TestMethod]
        public void OD_MBO_BuiltIn_CancelIndexBackup()
        {
            ODataTest(() =>
            {
                using (new SearchEngineSwindler(new SearchEngineForIndexBackupTests()))
                {
                    var response = ODataPostAsync(
                            $"/OData.svc/('Root')/{nameof(RepositoryTools.CancelIndexBackup)}",
                            "",
                            "")
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                    AssertNoError(response);
                    Assert.AreEqual(200, response.StatusCode);
                    var result = GetObject(response);
                    Assert.AreEqual(BackupState.CancelRequested.ToString(), result["State"].Value<string>());
                    Assert.AreEqual("Canceled", result["Current"]["Message"].Value<string>());
                }
            });
        }

        /* ======================================================================  */

        [TestMethod]
        public void OD_MBO_BuiltIn_HasPermissions()
        {
            ODataTest(() =>
            {
                using (new SearchEngineSwindler(new SearchEngineForIndexBackupTests()))
                {
                    var response = ODataGetAsync(
                            $"/OData.svc/('Root')/{nameof(SSCO.ContentOperations.HasPermission)}",
                            "?permissions=See&permissions=Open")
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                    Assert.AreEqual(200, response.StatusCode);
                    Assert.AreEqual("true", response.Result);
                }
            });
        }

        /* ====================================================================== TOOLS */

        private User CreateUser(string email, string username = null)
        {
            var user = new User(Node.LoadNode("/Root/IMS/BuiltIn/Portal"))
            {
                Name = username ?? Guid.NewGuid().ToString(),
                Enabled = true,
                Email = email
            };
            user.Save();
            return user;
        }

    #region Nested classes

    private class FileOperation : IDisposable
        {
            public File TheFile { get; }
            public string Url => ODataTools.GetODataUrl(Content.Create(TheFile));

            public FileOperation(string fileName = null)
            {
                var fileContainer = Node.Load<SystemFolder>("/Root/TestFiles");
                if (fileContainer == null)
                {
                    fileContainer = new SystemFolder(Repository.Root) { Name = "TestFiles" };
                    fileContainer.Save();
                }

                TheFile = new File(fileContainer) { Name = fileName ?? Guid.NewGuid().ToString() };
                TheFile.Binary.SetStream(RepositoryTools.GetStreamFromString("Lorem ipsum..."));
                TheFile.Save();
            }

            public void Dispose()
            {
                TheFile.ForceDelete();
            }
        }

        #endregion
    }
}
