using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.OData;
using Task = System.Threading.Tasks.Task;
// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo

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
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_Security_GetPermissions_ACE()
        {
            await IsolatedODataTestAsync(async () =>
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
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_Security_HasPermission_Administrator()
        {
            await IsolatedODataTestAsync(async () =>
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
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_Security_HasPermission_Visitor()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataPostAsync(
                        "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission",
                        "",
                        $"{{user:\"{User.Visitor.Path}\", permissions:[\"Open\",\"Save\"] }}")
                    .ConfigureAwait(false);

                // ASSERT
                Assert.AreEqual("false", response.Result);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_Security_HasPermission_NullUser()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataPostAsync(
                        "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission",
                        "",
                        $"{{user:null, permissions:[\"Open\",\"Save\"] }}")
                    .ConfigureAwait(false);

                // ASSERT
                Assert.AreEqual("true", response.Result);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_Security_HasPermission_WithoutUser()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataPostAsync(
                        "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission",
                        "",
                        $"{{permissions:[\"Open\",\"Save\"] }}")
                    .ConfigureAwait(false);

                // ASSERT
                Assert.AreEqual("true", response.Result);
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_Security_HasPermission_Error_IdentityNotFound()
        {
            await ODataTestAsync(async () =>
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
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_Security_HasPermission_Error_UnknownPermission()
        {
            await ODataTestAsync(async () =>
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
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_Security_HasPermission_Error_MissingParameter()
        {
            await ODataTestAsync(async () =>
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
                Assert.AreEqual("Operation not found: HasPermission()", error.Message);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_Security_SetPermissions()
        {
            await IsolatedODataTestAsync(async () =>
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
                AssertNoError(response);
                var entity = GetEntity(response);
                Assert.AreEqual(content.Id, entity.Id);
                Assert.AreEqual(200, response.StatusCode);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_Security_SetPermissions_NotPropagates()
        {
            await IsolatedODataTestAsync(async () =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                var content = Content.CreateNew("Folder", testRoot, "Folder1");
                content.Save();
                var folderPath = ODataMiddleware.GetEntityUrl(content.Path);
                var folderRepoPath = content.Path;
                var folderId = content.Id;
                content = Content.CreateNew("Car", content.ContentHandler, "Car1");
                content.Save();
                var carRepoPath = content.Path;

                // ACTION
                var response = await ODataPostAsync(
                        $"/OData.svc/{folderPath}/SetPermissions",
                        "",
                        "{r:[{identity:\"/Root/IMS/BuiltIn/Portal/Visitor\"," +
                        " OpenMinor:\"allow\", localOnly:true}]}")
                    .ConfigureAwait(false);

                // ASSERT
                AssertNoError(response);
                var entity = GetEntity(response);
                Assert.AreEqual(folderId, entity.Id);
                Assert.AreEqual(200, response.StatusCode);

                var folder = Node.LoadNode(folderRepoPath);
                var car = Node.LoadNode(carRepoPath);
                Assert.IsTrue(folder.Security.HasPermission(User.Visitor as IUser, PermissionType.OpenMinor));
                Assert.IsFalse(car.Security.HasPermission(User.Visitor as IUser, PermissionType.OpenMinor));
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_Security_Break()
        {
            await IsolatedODataTestAsync(async () =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                var content = Content.CreateNew("Car", testRoot, Guid.NewGuid().ToString());
                content.Save();
                var resourcePath = ODataMiddleware.GetEntityUrl(content.Path);

                // ACTION 1: Break
                var response = await ODataPostAsync(
                        $"/OData.svc/{resourcePath}/SetPermissions",
                        "",
                        "{inheritance:\"break\"}")
                    .ConfigureAwait(false);

                // ASSERT 1: Not inherited
                AssertNoError(response);
                var entity = GetEntity(response);
                Assert.AreEqual(content.Id, entity.Id);
                Assert.AreEqual(200, response.StatusCode);
                var node = Node.LoadNode(content.Id);
                Assert.IsFalse(node.Security.IsInherited);

                // ACTION 2: Unbreak
                response = await ODataPostAsync(
                        $"/OData.svc/{resourcePath}/SetPermissions",
                        "",
                        "{inheritance:\"unbreak\"}")
                    .ConfigureAwait(false);

                // ASSERT 2: Inherited
                AssertNoError(response);
                entity = GetEntity(response);
                Assert.AreEqual(content.Id, entity.Id);
                Assert.AreEqual(200, response.StatusCode);
                node = Node.LoadNode(content.Id);
                Assert.IsTrue(node.Security.IsInherited);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_Security_Error_MissingStream()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataPostAsync(
                        $"/OData.svc/Root('IMS')/SetPermissions",
                        "",
                        null)
                    .ConfigureAwait(false);

                // ASSERT
                var error = GetError(response);
                Assert.AreEqual(ODataExceptionCode.NotSpecified, error.Code);
                Assert.AreEqual(nameof(InvalidContentActionException), error.ExceptionType);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_Security_Error_BothParameters()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataPostAsync(
                        $"/OData.svc/Root('IMS')/SetPermissions",
                        "",
                        "{r:[{identity:\"/Root/IMS/BuiltIn/Portal/Visitor\", OpenMinor:\"allow\"}], inheritance:\"break\"}")
                    .ConfigureAwait(false);

                // ASSERT
                var error = GetError(response);
                var expectedMessage = "Ambiguous call: SetPermissions(r,inheritance) --> " +
                                      "SetPermissions(string inheritance), SetPermissions(SetPermissionsRequest r)";
                Assert.AreEqual(ODataExceptionCode.NotSpecified, error.Code);
                Assert.AreEqual(expectedMessage, error.Message);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OD_Security_Error_InvalidInheritanceParam()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataPostAsync(
                            $"/OData.svc/Root('IMS')/SetPermissions",
                            "",
                            "{inheritance:\"dance\"}")
                        .ConfigureAwait(false);

                // ASSERT
                var error = GetError(response);
                var expectedMessage = "The value of the  inheritance  must be  break  or  unbreak .";
                Assert.AreEqual(ODataExceptionCode.NotSpecified, error.Code);
                Assert.AreEqual(expectedMessage, error.Message);
            }).ConfigureAwait(false);
        }

    }
}
