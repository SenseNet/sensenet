using System;
using Compatibility.SenseNet.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.OData;
using SenseNet.Security;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataSecurityTests : ODataTestBase
    {
        [TestMethod]
        public async Task OD_Security_GetPermissions_ACL()
        {
            await IsolatedODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                    //Result: {
                    //    "id": 42,
                    //    "path": "/Root/....",
                    //    "inherits": true,
                    //    "entries": [
                    //        {
                    //            "identity": { "path": "/Root/...", ...
                    //            "permissions": {
                    //                "See": {...
                    SnAclEditor.Create(new SnSecurityContext(User.Current))
                        .Allow(2, 1, false, PermissionType.Custom01)
                        .Apply();

                    // ACTION
                    var response = await ODataPostAsync(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/GetPermissions",
                            "",
                            null)
                        .ConfigureAwait(false);

                    // ASSERT
                    AssertNoError(response);
                    var json = Deserialize(response.Result);
                    var entries = json["entries"];
                    Assert.IsNotNull(entries);

                    var expected = "Custom01:{value:allow,from:/Root,identity:/Root/IMS/BuiltIn/Portal/Admin}";
                    var actual = response.Result
                        .Replace("\r", "")
                        .Replace("\n", "")
                        .Replace("\t", "")
                        .Replace(" ", "")
                        .Replace("\"", "");
                    Assert.IsTrue(actual.Contains(expected));
                }
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_Security_GetPermissions_ACE()
        {
            await IsolatedODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                    SnAclEditor.Create(new SnSecurityContext(User.Current))
                        .Allow(2, Identifiers.AdministratorUserId, false, PermissionType.Custom01)
                        .Allow(2, Identifiers.VisitorUserId, false, PermissionType.Custom02)
                        .Apply();

                    // ACTION
                    var response = await ODataPostAsync(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/GetPermissions",
                            "",
                            "{identity:\"/root/ims/builtin/portal/visitor\"}")
                        .ConfigureAwait(false);

                    // ASSERT
                    AssertNoError(response);
                    var json = Deserialize(response.Result);
                    var entries = json as JArray;
                    Assert.IsNotNull(entries);

                    var expected = "Custom02:{value:allow,from:/Root,identity:/Root/IMS/BuiltIn/Portal/Visitor}";
                    var actual = response.Result
                        .Replace("\r", "")
                        .Replace("\n", "")
                        .Replace("\t", "")
                        .Replace(" ", "")
                        .Replace("\"", "");
                    Assert.IsTrue(actual.Contains(expected));
                    Assert.IsTrue(actual.Contains("Custom01:null"));
                }
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_Security_HasPermission_Administrator()
        {
            await IsolatedODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                    SecurityHandler.CreateAclEditor()
                        .Allow(Repository.Root.Id, Group.Administrators.Id, false, PermissionType.Open)
                        .Allow(Repository.Root.Id, Group.Administrators.Id, false, PermissionType.Save)
                        .Apply();

                    var hasPermission = SecurityHandler.HasPermission(
                        User.Administrator, Group.Administrators, PermissionType.Open, PermissionType.Save);
                    Assert.IsTrue(hasPermission);

                    // ACTION
                    var response = await ODataPostAsync(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission",
                            "",
                            $"{{user:\"{User.Administrator.Path}\", permissions:[\"Open\",\"Save\"] }}")
                        .ConfigureAwait(false);

                    // ASSERT
                    Assert.AreEqual("true", response.Result);
                }
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_Security_HasPermission_Visitor()
        {
            await IsolatedODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                    // ACTION
                    var response = await ODataPostAsync(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission",
                            "",
                            $"{{user:\"{User.Visitor.Path}\", permissions:[\"Open\",\"Save\"] }}")
                        .ConfigureAwait(false);

                    // ASSERT
                    Assert.AreEqual("false", response.Result);
                }
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_Security_HasPermission_NullUser()
        {
            await IsolatedODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                    // ACTION
                    var response = await ODataPostAsync(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission",
                            "",
                            $"{{user:null, permissions:[\"Open\",\"Save\"] }}")
                        .ConfigureAwait(false);

                    // ASSERT
                    Assert.AreEqual("true", response.Result);
                }
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_Security_HasPermission_WithoutUser()
        {
            await IsolatedODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                    // ACTION
                    var response = await ODataPostAsync(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission",
                            "",
                            $"{{permissions:[\"Open\",\"Save\"] }}")
                        .ConfigureAwait(false);

                    // ASSERT
                    Assert.AreEqual("true", response.Result);
                }
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_Security_HasPermission_Error_IdentityNotFound()
        {
            await IsolatedODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                    // ACTION
                    var response = await ODataPostAsync(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission",
                            "",
                            "{user:\"/root/ims/builtin/portal/nobody\", permissions:[\"Open\",\"Save\"] }")
                        .ConfigureAwait(false);

                    // ASSERT
                    var error = GetError(response);
                    Assert.AreEqual(ODataExceptionCode.ResourceNotFound, error.Code);
                    Assert.AreEqual("Identity not found: /root/ims/builtin/portal/nobody", error.Message);
                }
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_Security_HasPermission_Error_UnknownPermission()
        {
            await IsolatedODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                    // ACTION
                    var response = await ODataPostAsync(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission",
                            "",
                            "{permissions:[\"Open\",\"Save1\"] }")
                        .ConfigureAwait(false);

                    // ASSERT
                    var error = GetError(response);
                    Assert.AreEqual(ODataExceptionCode.NotSpecified, error.Code);
                    Assert.AreEqual("Unknown permission: Save1", error.Message);
                }
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_Security_HasPermission_Error_MissingParameter()
        {
            await IsolatedODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                    // ACTION
                    var response = await ODataPostAsync(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission",
                            "",
                            null)
                        .ConfigureAwait(false);

                    // ASSERT
                    var error = GetError(response);
                    Assert.AreEqual(ODataExceptionCode.NotSpecified, error.Code);
                    Assert.AreEqual("Value cannot be null.\\nParameter name: permissions", error.Message);
                }
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_Security_SetPermissions()
        {
            await IsolatedODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                    InstallCarContentType();
                    var testRoot = CreateTestRoot("ODataTestRoot");
                    var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                    content.Save();
                    var resourcePath = ODataMiddleware.GetEntityUrl(content.Path);

                    // ACTION
                    var response = await ODataPostAsync(
                            string.Concat("/OData.svc/", resourcePath, "/SetPermissions"),
                            "",
                            "{r:[" +
                            "{identity:\"/Root/IMS/BuiltIn/Portal/Visitor\", OpenMinor:\"allow\", Save:\"deny\"}," +
                            "{identity:\"/Root/IMS/BuiltIn/Portal/Owners\", Custom16:\"A\", Custom17:\"1\"}]}")
                        .ConfigureAwait(false);

                    // ASSERT
                    Assert.AreEqual(0, response.Result.Length);
                    Assert.AreEqual(204, response.StatusCode); // 204 No Content
                }
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_Security_SetPermissions_NotPropagates()
        {
            await IsolatedODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                    InstallCarContentType();
                    var testRoot = CreateTestRoot("ODataTestRoot");
                    var content = Content.CreateNew("Folder", testRoot, Guid.NewGuid().ToString());
                    content.Save();
                    var folderPath = ODataMiddleware.GetEntityUrl(content.Path);
                    var folderRepoPath = content.Path;
                    content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                    content.Save();
                    var carRepoPath = content.Path;

                    // ACTION
                    var response = await ODataPostAsync(
                            $"/OData.svc/{folderPath}/SetPermissions",
                            "",
                            "{r:[{identity:\"/Root/IMS/BuiltIn/Portal/Visitor\"," +
                            " OpenMinor:\"allow\", propagates:false}]}")
                        .ConfigureAwait(false);

                    // ASSERT
                    Assert.AreEqual(0, response.Result.Length);
                    Assert.AreEqual(204, response.StatusCode);
                    var folder = Node.LoadNode(folderRepoPath);
                    var car = Node.LoadNode(carRepoPath);

                    Assert.IsTrue(folder.Security.HasPermission((IUser) User.Visitor, PermissionType.OpenMinor));
                    Assert.IsFalse(car.Security.HasPermission((IUser) User.Visitor, PermissionType.OpenMinor));
                }
            }).ConfigureAwait(false);
        }
        /*[TestMethod]*/
        /*public async Task OD_Security_Break()
        {
            await IsolatedODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                content.Save();
                var resourcePath = ODataHandler.GetEntityUrl(content.Path);


                
                    string result;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "",
                            output);
                        var handler = new ODataHandler();
                        var stream = CreateRequestStream("{inheritance:\"break\"}");
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                        result = GetStringResult(output);
                    }
                    Assert.IsTrue(result.Length == 0);
                }
            }).ConfigureAwait(false);
        }*/
        /*[TestMethod]*/
        /*public async Task OD_Security_Unbreak()
        {
            await IsolatedODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                content.Save();
                var resourcePath = ODataHandler.GetEntityUrl(content.Path);


                
                    string result;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "",
                            output);
                        var handler = new ODataHandler();
                        var stream = CreateRequestStream("{inheritance:\"unbreak\"}");
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                        result = GetStringResult(output);
                    }
                    Assert.IsTrue(result.Length == 0);
                }
            }).ConfigureAwait(false);
        }*/
        /*[TestMethod]*/
        /*public async Task OD_Security_Error_MissingStream()
        {
            await IsolatedODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                content.Save();
                var resourcePath = ODataHandler.GetEntityUrl(content.Path);


                    ODataError error;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "",
                            output);
                        var handler = new ODataHandler();
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", null);
                        error = GetError(output);
                    }
                    var expectedMessage = "Value cannot be null.\\nParameter name: stream";
                    Assert.IsTrue(error.Code == ODataExceptionCode.NotSpecified);
                    Assert.IsTrue(error.Message == expectedMessage);
                }
            }).ConfigureAwait(false);
        }*/
        /*[TestMethod]*/
        /*public async Task OD_Security_Error_BothParameters()
        {
            await IsolatedODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                content.Save();
                var resourcePath = ODataHandler.GetEntityUrl(content.Path);


                    ODataError error;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "",
                            output);
                        var handler = new ODataHandler();
                        var stream =
                            CreateRequestStream(
                                "{r:[{identity:\"/Root/IMS/BuiltIn/Portal/Visitor\", OpenMinor:\"allow\"}], inheritance:\"break\"}");
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                        error = GetError(output);
                    }
                    var expectedMessage = "Cannot use  r  and  inheritance  parameters at the same time.";
                    Assert.IsTrue(error.Code == ODataExceptionCode.NotSpecified);
                    Assert.IsTrue(error.Message == expectedMessage);
                }
            }).ConfigureAwait(false);
        }*/
        /*[TestMethod]*/
        /*public async Task OD_Security_Error_InvalidInheritanceParam()
        {
            await IsolatedODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                content.Save();
                var resourcePath = ODataHandler.GetEntityUrl(content.Path);


                
                    ODataError error;
                    using (var output = new StringWriter())
                    {
                        var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "",
                            output);
                        var handler = new ODataHandler();
                        var stream = CreateRequestStream("{inheritance:\"dance\"}");
                        handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                        error = GetError(output);
                    }
                    var expectedMessage = "The value of the  inheritance  must be  break  or  unbreak .";
                    Assert.IsTrue(error.Code == ODataExceptionCode.NotSpecified);
                    Assert.IsTrue(error.Message == expectedMessage);
                }
            }).ConfigureAwait(false);
        }*/

    }
}
