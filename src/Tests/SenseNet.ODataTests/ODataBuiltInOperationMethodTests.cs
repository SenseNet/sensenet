using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.OData;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataBuiltInOperationMethodTests : ODataTestBase
    {
        /* ====================================================================== RepositoryTools */

        [TestMethod]
        public void OD_MBO_BuiltIn_GetVersionInfo()
        {
            ODataTest(() =>
            {
                var response = ODataGetAsync($"/OData.svc/('Root')/GetVersionInfo", "")
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                AssertNoError(response);
                Assert.AreEqual(200, response.StatusCode);
                var result = GetObject(response);
                Assert.IsNotNull(result["Components"]);
                Assert.IsNotNull(result["Assemblies"]);
                Assert.IsNotNull(result["InstalledPackages"]);
                Assert.IsNotNull(result["DatabaseAvailable"]);
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
                    file = new File(CreateTestRoot("TestFiles")) { Name = "File-1" };
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
                using (var op = new FileOperation("File-1"))
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

        /* ======================================================================  */

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
