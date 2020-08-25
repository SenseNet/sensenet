using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Security;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataGetAclFunctionTests : ODataTestBase
    {
        [TestMethod]
        public async Task OD_Security_GetAcl_InheritedAndLocalOnly()
        {
            await IsolatedODataTestAsync(async () =>
            {
                // ARRANGE
                var contentNode = Node.LoadNode("/Root/Content");

                var permissionData = new List<string>
                {
                    " +               2|Normal|+1:_____________________________________________+++++++++++++++++++",
                    " +               2|Normal|+7:_____________________________________________+++++++++++++++++++",
                    " +               2|Normal|-6:_______________________________________________________________+",
                    $"+{contentNode.Id}|Normal|-6:___________________________________________________________+++++",
                };
                var categoriesToNormalize = new[] { EntryType.Normal };
                using (new SystemAccount())
                {
                    var aclEditor = SecurityHandler.CreateAclEditor();
                    aclEditor
                        .UnbreakInheritance(2, categoriesToNormalize)
                        .UnbreakInheritance(contentNode.Id, categoriesToNormalize)
                        .Apply();
                    aclEditor
                        .RemoveExplicitEntries(2)
                        .RemoveExplicitEntries(contentNode.Id)
                        .Apply(SecurityHandler.ParseInitialPermissions(aclEditor.Context, permissionData));
                }

                // ACTION
                var response = await ODataGetAsync(
                        $"/OData.svc/Root('Content')/GetAcl", null)
                    .ConfigureAwait(false);

                // ASSERT
                AssertNoError(response);
                var result = ODataTestBase.GetObject(response);

                Assert.AreEqual(contentNode.Id, result.SelectToken("id").Value<int>());
                Assert.AreEqual(contentNode.Path, result.SelectToken("path").Value<string>());
                Assert.IsTrue(result.SelectToken("inherits").Value<bool>());
                Assert.IsTrue(result.SelectToken("isPublic").Value<bool>());

                Assert.AreEqual(3, ((JArray)result["entries"]).Count);

                Assert.AreEqual(Identifiers.AdministratorUserId, result.SelectToken("entries[0].identity.id").Value<int>());
                Assert.AreEqual("/Root", result.SelectToken("entries[0].ancestor").Value<string>());
                Assert.IsTrue(result.SelectToken("entries[0].inherited").Value<bool>());
                Assert.IsTrue(result.SelectToken("entries[0].propagates").Value<bool>());
                Assert.AreEqual("allow", result.SelectToken("entries[0].permissions.See.value").Value<string>());
                Assert.AreEqual("/Root", result.SelectToken("entries[0].permissions.See.from").Value<string>());
                Assert.AreEqual("allow", result.SelectToken("entries[0].permissions.Open.value").Value<string>());
                Assert.AreEqual("/Root", result.SelectToken("entries[0].permissions.Open.from").Value<string>());
                Assert.AreEqual("allow", result.SelectToken("entries[0].permissions.TakeOwnership.value").Value<string>());
                Assert.AreEqual("/Root", result.SelectToken("entries[0].permissions.TakeOwnership.from").Value<string>());

                Assert.AreEqual(Identifiers.VisitorUserId, result.SelectToken("entries[1].identity.id").Value<int>());
                Assert.AreEqual(null, result.SelectToken("entries[1].ancestor").Value<string>());
                Assert.IsFalse(result.SelectToken("entries[1].inherited").Value<bool>());
                Assert.IsFalse(result.SelectToken("entries[1].propagates").Value<bool>());
                Assert.AreEqual("allow", result.SelectToken("entries[1].permissions.See.value").Value<string>());
                Assert.AreEqual(null, result.SelectToken("entries[1].permissions.See.from").Value<string>());
                Assert.AreEqual("allow", result.SelectToken("entries[1].permissions.Open.value").Value<string>());
                Assert.AreEqual(null, result.SelectToken("entries[1].permissions.Open.from").Value<string>());
                Assert.AreEqual(null, result.SelectToken("entries[1].permissions.TakeOwnership").Value<string>());

                Assert.AreEqual(Identifiers.AdministratorsGroupId, result.SelectToken("entries[2].identity.id").Value<int>());
                Assert.AreEqual("/Root", result.SelectToken("entries[2].ancestor").Value<string>());
                Assert.IsTrue(result.SelectToken("entries[2].inherited").Value<bool>());
                Assert.IsTrue(result.SelectToken("entries[2].propagates").Value<bool>());
                Assert.AreEqual("allow", result.SelectToken("entries[2].permissions.See.value").Value<string>());
                Assert.AreEqual("/Root", result.SelectToken("entries[2].permissions.See.from").Value<string>());
                Assert.AreEqual("allow", result.SelectToken("entries[2].permissions.Open.value").Value<string>());
                Assert.AreEqual("/Root", result.SelectToken("entries[2].permissions.Open.from").Value<string>());
                Assert.AreEqual("allow", result.SelectToken("entries[2].permissions.TakeOwnership.value").Value<string>());
                Assert.AreEqual("/Root", result.SelectToken("entries[2].permissions.TakeOwnership.from").Value<string>());

            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_Security_GetAcl_InheritedCombined()
        {
            await IsolatedODataTestAsync(async () =>
            {
                // ARRANGE
                var contentNode = Node.LoadNode("/Root/Content");
                var itNode = new SystemFolder(contentNode) {Name = "IT"};
                itNode.Save();

                var permissionData = new List<string>
                {
                    " +               2|Normal|+1:_____________________________________________+++++++++++++++++++",
                    " +               2|Normal|+7:_____________________________________________+++++++++++++++++++",
                    " +               2|Normal|+6:___________________________________________________________+++++",
                    $"+{contentNode.Id}|Normal|+6:_____________________________________________+__________________",
                };
                var categoriesToNormalize = new[] { EntryType.Normal };
                using (new SystemAccount())
                {
                    var aclEditor = SecurityHandler.CreateAclEditor();
                    aclEditor
                        .UnbreakInheritance(2, categoriesToNormalize)
                        .UnbreakInheritance(contentNode.Id, categoriesToNormalize)
                        .Apply();
                    aclEditor
                        .RemoveExplicitEntries(2)
                        .RemoveExplicitEntries(contentNode.Id)
                        .Apply(SecurityHandler.ParseInitialPermissions(aclEditor.Context, permissionData));
                }

                // ACTION
                var response = await ODataGetAsync(
                        $"/OData.svc/Root/Content('IT')/GetAcl", null)
                    .ConfigureAwait(false);

                // ASSERT
                AssertNoError(response);
                var result = ODataTestBase.GetObject(response);

                Assert.AreEqual(itNode.Id, result.SelectToken("id").Value<int>());
                Assert.AreEqual(itNode.Path, result.SelectToken("path").Value<string>());
                Assert.IsTrue(result.SelectToken("inherits").Value<bool>());
                Assert.IsTrue(result.SelectToken("isPublic").Value<bool>());

                Assert.AreEqual(3, ((JArray)result["entries"]).Count);
                var entry = result.SelectToken("entries[?(@identity.id == 6)]");

                Assert.AreEqual(Identifiers.VisitorUserId, entry.SelectToken("identity.id").Value<int>());
                Assert.AreEqual("/Root/Content", entry.SelectToken("ancestor").Value<string>());
                Assert.IsTrue(entry.SelectToken("inherited").Value<bool>());
                Assert.IsTrue(entry.SelectToken("propagates").Value<bool>());
                Assert.AreEqual("allow", entry.SelectToken("permissions.See.value").Value<string>());
                Assert.AreEqual("/Root/Content", entry.SelectToken("permissions.See.from").Value<string>());
                Assert.AreEqual("allow", entry.SelectToken("permissions.Open.value").Value<string>());
                Assert.AreEqual("/Root", entry.SelectToken("permissions.Open.from").Value<string>());
                Assert.AreEqual("allow", entry.SelectToken("permissions.TakeOwnership.value").Value<string>());
                Assert.AreEqual("/Root/Content", entry.SelectToken("permissions.TakeOwnership.from").Value<string>());

            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_Security_GetAcl_Break()
        {
            await IsolatedODataTestAsync(async () =>
            {
                // ARRANGE
                var contentNode = Node.LoadNode("/Root/Content");
                var itNode = new SystemFolder(contentNode) { Name = "IT" };
                itNode.Save();

                var permissionData = new List<string>
                {
                    " +               2|Normal|+1:_____________________________________________+++++++++++++++++++",
                    " +               2|Normal|+7:_____________________________________________+++++++++++++++++++",
                    $"-{contentNode.Id}|Normal|+1:_____________________________________________+++++++++++++++++++",
                    $"-{contentNode.Id}|Normal|+6:_____________________________________________+_____________+++++",
                };
                var categoriesToNormalize = new[] { EntryType.Normal };
                using (new SystemAccount())
                {
                    var aclEditor = SecurityHandler.CreateAclEditor();
                    aclEditor
                        .UnbreakInheritance(2, categoriesToNormalize)
                        .UnbreakInheritance(contentNode.Id, categoriesToNormalize)
                        .Apply();
                    aclEditor
                        .RemoveExplicitEntries(2)
                        .RemoveExplicitEntries(contentNode.Id)
                        .Apply(SecurityHandler.ParseInitialPermissions(aclEditor.Context, permissionData));
                }

                // ACTION
                var response = await ODataGetAsync(
                        $"/OData.svc/Root/Content('IT')/GetAcl", null)
                    .ConfigureAwait(false);

                // ASSERT
                AssertNoError(response);
                var result = ODataTestBase.GetObject(response);

                Assert.AreEqual(itNode.Id, result.SelectToken("id").Value<int>());
                Assert.AreEqual(itNode.Path, result.SelectToken("path").Value<string>());
                Assert.IsTrue(result.SelectToken("inherits").Value<bool>());
                Assert.IsTrue(result.SelectToken("isPublic").Value<bool>());

                Assert.AreEqual(2, ((JArray)result["entries"]).Count);

                var entry = result.SelectToken("entries[?(@identity.id == 1)]");
                Assert.AreEqual(Identifiers.AdministratorUserId, entry.SelectToken("identity.id").Value<int>());
                Assert.AreEqual("/Root/Content", entry.SelectToken("ancestor").Value<string>());
                Assert.IsTrue(entry.SelectToken("inherited").Value<bool>());
                Assert.IsTrue(entry.SelectToken("propagates").Value<bool>());
                Assert.AreEqual("allow", entry.SelectToken("permissions.See.value").Value<string>());
                Assert.AreEqual("/Root/Content", entry.SelectToken("permissions.See.from").Value<string>());
                Assert.AreEqual("allow", entry.SelectToken("permissions.Open.value").Value<string>());
                Assert.AreEqual("/Root/Content", entry.SelectToken("permissions.Open.from").Value<string>());
                Assert.AreEqual("allow", entry.SelectToken("permissions.TakeOwnership.value").Value<string>());
                Assert.AreEqual("/Root/Content", entry.SelectToken("permissions.TakeOwnership.from").Value<string>());

                entry = result.SelectToken("entries[?(@identity.id == 6)]");
                Assert.AreEqual(Identifiers.VisitorUserId, entry.SelectToken("identity.id").Value<int>());
                Assert.AreEqual("/Root/Content", entry.SelectToken("ancestor").Value<string>());
                Assert.IsTrue(entry.SelectToken("inherited").Value<bool>());
                Assert.IsTrue(entry.SelectToken("propagates").Value<bool>());
                Assert.AreEqual("allow", entry.SelectToken("permissions.See.value").Value<string>());
                Assert.AreEqual("/Root/Content", entry.SelectToken("permissions.See.from").Value<string>());
                Assert.AreEqual("allow", entry.SelectToken("permissions.Open.value").Value<string>());
                Assert.AreEqual("/Root/Content", entry.SelectToken("permissions.Open.from").Value<string>());
                Assert.AreEqual("allow", entry.SelectToken("permissions.TakeOwnership.value").Value<string>());
                Assert.AreEqual("/Root/Content", entry.SelectToken("permissions.TakeOwnership.from").Value<string>());

            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_Security_GetAcl_SeeOnlyIdentities()
        {
            await IsolatedODataTestAsync(async () =>
            {
                // ARRANGE
                var contentNode = Node.LoadNode("/Root/Content");
                //var itNode = new SystemFolder(contentNode) { Name = "IT" };
                //itNode.Save();
                var user = new User(User.Administrator.Parent) { Name = "U1", Email = "u1@example.com", Enabled = true };
                user.Save();

                var permissionData = new List<string>
                {
                    " +               2|Normal|+        1:_____________________________________________+++++++++++++++++++",
                    " +               2|Normal|+        7:_____________________________________________+++++++++++++++++++",
                    $"+               2|Normal|+{user.Id}:_________________________________________________+_____________+",
                    $"+{contentNode.Id}|Normal|+{user.Id}:_____________________________________________+__________________",
                };
                var categoriesToNormalize = new[] { EntryType.Normal };
                using (new SystemAccount())
                {
                    var aclEditor = SecurityHandler.CreateAclEditor();
                    aclEditor
                        .UnbreakInheritance(2, categoriesToNormalize)
                        .UnbreakInheritance(contentNode.Id, categoriesToNormalize)
                        .Apply();
                    aclEditor
                        .RemoveExplicitEntries(2)
                        .RemoveExplicitEntries(contentNode.Id)
                        .Apply(SecurityHandler.ParseInitialPermissions(aclEditor.Context, permissionData));
                }
                Assert.IsTrue(SecurityHandler.HasPermission(user, contentNode, PermissionType.SeePermissions));


                // ACTION
                ODataResponse response;
                using (new CurrentUserBlock(user))
                    response = await ODataGetAsync(
                            $"/OData.svc/Root('Content')/GetAcl", null)
                        .ConfigureAwait(false);

                // ASSERT
                AssertNoError(response);
                var result = ODataTestBase.GetObject(response);

                Assert.AreEqual(contentNode.Id, result.SelectToken("id").Value<int>());
                Assert.AreEqual(contentNode.Path, result.SelectToken("path").Value<string>());
                Assert.IsTrue(result.SelectToken("inherits").Value<bool>());
                Assert.IsFalse(result.SelectToken("isPublic").Value<bool>());

                Assert.AreEqual(3, ((JArray)result["entries"]).Count);

                var admin = result.SelectToken("entries[?(@identity.id == 1)].identity");
                Assert.AreEqual(Identifiers.AdministratorUserId, admin.SelectToken("id").Value<int>());
                Assert.AreEqual("Admin", admin.SelectToken("name").Value<string>());
                Assert.AreEqual("user", admin.SelectToken("kind").Value<string>());
                Assert.AreEqual(null, admin.SelectToken("domain").Value<string>());
                Assert.AreEqual(null, admin.SelectToken("avatar").Value<string>());

                var admins = result.SelectToken("entries[?(@identity.id == 7)].identity");
                Assert.AreEqual(Identifiers.AdministratorsGroupId, admins.SelectToken("id").Value<int>());
                Assert.AreEqual("Administrators", admins.SelectToken("name").Value<string>());
                Assert.AreEqual("group", admins.SelectToken("kind").Value<string>());
                Assert.AreEqual(null, admins.SelectToken("domain").Value<string>());
                Assert.AreEqual(null, admins.SelectToken("avatar").Value<string>());

                var u1 = result.SelectToken($"entries[?(@identity.id == {user.Id})].identity");
                Assert.AreEqual(user.Id, u1.SelectToken("id").Value<int>());
                Assert.AreEqual("U1", u1.SelectToken("name").Value<string>());
                Assert.AreEqual("user", u1.SelectToken("kind").Value<string>());
                Assert.AreEqual("BuiltIn", u1.SelectToken("domain").Value<string>());
                Assert.AreEqual("", u1.SelectToken("avatar").Value<string>());

            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_Security_GetAcl_InvisibleSource()
        {
            await IsolatedODataTestAsync(async () =>
            {
                // ARRANGE
                var contentNode = Node.LoadNode("/Root/Content");
                //var itNode = new SystemFolder(contentNode) { Name = "IT" };
                //itNode.Save();
                var user = new User(User.Administrator.Parent) {Name = "U1", Email = "u1@example.com", Enabled = true};
                user.Save();

                var permissionData = new List<string>
                {
                    " +               2|Normal|+        1:_____________________________________________+++++++++++++++++++",
                    " +               2|Normal|+        7:_____________________________________________+++++++++++++++++++",
                    $"+               2|Normal|+{user.Id}:_________________________________________________+______________",
                    $"+{contentNode.Id}|Normal|+{user.Id}:_____________________________________________+__________________",
                };
                var categoriesToNormalize = new[] { EntryType.Normal };
                using (new SystemAccount())
                {
                    var aclEditor = SecurityHandler.CreateAclEditor();
                    aclEditor
                        .UnbreakInheritance(2, categoriesToNormalize)
                        .UnbreakInheritance(contentNode.Id, categoriesToNormalize)
                        .Apply();
                    aclEditor
                        .RemoveExplicitEntries(2)
                        .RemoveExplicitEntries(contentNode.Id)
                        .Apply();
                    aclEditor
                        .Apply(SecurityHandler.ParseInitialPermissions(aclEditor.Context, permissionData));
                }
                Assert.IsTrue(SecurityHandler.HasPermission(user, contentNode, PermissionType.SeePermissions));


                // ACTION
                ODataResponse response;
                using(new CurrentUserBlock(user))
                    response = await ODataGetAsync(
                            $"/OData.svc/Root('Content')/GetAcl", null)
                        .ConfigureAwait(false);

                // ASSERT
                AssertNoError(response);
                var result = ODataTestBase.GetObject(response);

                Assert.AreEqual(contentNode.Id, result.SelectToken("id").Value<int>());
                Assert.AreEqual(contentNode.Path, result.SelectToken("path").Value<string>());
                Assert.IsTrue(result.SelectToken("inherits").Value<bool>());
                Assert.IsFalse(result.SelectToken("isPublic").Value<bool>());

                Assert.AreEqual(1, ((JArray)result["entries"]).Count);

                var entry = result.SelectToken("entries[0]");
                Assert.AreEqual(user.Id, entry.SelectToken("identity.id").Value<int>());
                Assert.AreEqual("Somewhere", entry.SelectToken("ancestor").Value<string>());
                Assert.IsFalse(entry.SelectToken("inherited").Value<bool>());
                Assert.IsTrue(entry.SelectToken("propagates").Value<bool>());
                Assert.AreEqual("allow", entry.SelectToken("permissions.See.value").Value<string>());
                Assert.AreEqual(null, entry.SelectToken("permissions.See.from").Value<string>());
                Assert.AreEqual(null, entry.SelectToken("permissions.Open").Value<string>());
                Assert.AreEqual("allow", entry.SelectToken("permissions.SeePermissions.value").Value<string>());
                Assert.AreEqual("Somewhere", entry.SelectToken("permissions.SeePermissions.from").Value<string>());
                Assert.AreEqual("allow", entry.SelectToken("permissions.TakeOwnership.value").Value<string>());
                Assert.AreEqual(null, entry.SelectToken("permissions.TakeOwnership.from").Value<string>());

            }).ConfigureAwait(false);
        }
    }
}
