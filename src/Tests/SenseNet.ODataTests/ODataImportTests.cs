using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MimeKit.IO.Filters;
using Newtonsoft.Json;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.OData.IO;
using STT = System.Threading.Tasks;

namespace SenseNet.ODataTests;

[TestClass]
public class ODataImportTests : ODataTestBase
{
    private class ImportResult
    {
        [JsonProperty("path")]             public string Path { get; set; }
        [JsonProperty("name")]             public string Name { get; set; }
        [JsonProperty("type")]             public string Type { get; set; }
        [JsonProperty("action")]           public string Action { get; set; }
        [JsonProperty("retryPermissions")] public bool RetryPermissions { get; set; }
        [JsonProperty("brokenReferences")] public string[] BrokenReferences { get; set; }
        [JsonProperty("messages")]         public string[] Messages { get; set; }
    }

    private class ImportTestUser : ODataTests.TestUser
    {
        public int Id { get; }
        public IEnumerable<int> GetDynamicGroups(int entityId) => throw new NotImplementedException();
        public string Path { get; }
        public bool IsInGroup(int securityGroupId) => throw new NotImplementedException();
        public string AuthenticationType { get; }
        public bool IsAuthenticated { get; }
        public string Name { get; }
        public bool Enabled { get; set; }
        public string Domain { get; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Password { get; set; }
        public string PasswordHash { get; set; }
        public string Username { get; }
        public bool IsOperator { get; }
        public bool IsInGroup(IGroup group) => throw new NotImplementedException();
        public bool IsInOrganizationalUnit(IOrganizationalUnit orgUnit) => throw new NotImplementedException();
        public bool IsInContainer(ISecurityContainer container) => throw new NotImplementedException();
        public DateTime LastLoggedOut { get; set; }
        public MembershipExtension MembershipExtension { get; set; }

        public ImportTestUser(string userName, int id, bool isOperator) : base(userName, id)
        {
            IsOperator = isOperator;
        }
    }

    /* ======================================================================================= SET Owner, CreatedBy etc. */

    [TestMethod]
    public async STT.Task OD_Import_Create_HeadReferences_Admin()
    {
        await ODataTestAsync(async () =>
        {
            var referredContentId = 1;

            var importerUser = await CreateAdminUserAsync("Importer", default);
            Assert.IsTrue(importerUser.IsOperator);
            Assert.IsTrue(IsVisibleFor(importerUser, referredContentId));

            // ACT
            var result = await ImportHeadReferencesAsync(importerUser, referredContentId, CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual("created", result.Action);
            var importedContent = Node.LoadNode(result.Path);
            Assert.AreEqual(referredContentId, importedContent.OwnerId);
            Assert.AreEqual(referredContentId, importedContent.CreatedById);
            Assert.AreEqual(referredContentId, importedContent.VersionCreatedById);
            Assert.AreEqual(referredContentId, importedContent.ModifiedById);
            Assert.AreEqual(referredContentId, importedContent.VersionModifiedById);
            Assert.AreEqual(0, result.BrokenReferences.Length);
            Assert.AreEqual(0, result.Messages.Length);
        });
    }
    [TestMethod]
    [Description("The referred content does not exist so the importer user will be the owner, creator or last modificator.")]
    public async STT.Task OD_Import_Create_HeadReferences_Unknown()
    {
        await ODataTestAsync(async () =>
        {
            var referredContentId = 456789;

            var importerUser = await CreatePublicAdminUserAsync("Importer", default);
            Assert.IsFalse(importerUser.IsOperator);

            // ACT
            var result = await ImportHeadReferencesAsync(importerUser, referredContentId, CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual("created", result.Action);
            var importedContent = Node.LoadNode(result.Path);
            Assert.AreEqual(importerUser.Id, importedContent.OwnerId);
            Assert.AreEqual(importerUser.Id, importedContent.CreatedById);
            Assert.AreEqual(importerUser.Id, importedContent.VersionCreatedById);
            Assert.AreEqual(importerUser.Id, importedContent.ModifiedById);
            Assert.AreEqual(importerUser.Id, importedContent.VersionModifiedById);
            Assert.AreEqual(5, result.BrokenReferences.Length);
            Assert.AreEqual(0, result.Messages.Length);
        });
    }
    [TestMethod]
    [Description("The referred content is invisible, the importer user will be the owner, creator or last modificator.")]
    public async STT.Task OD_Import_Create_HeadReferences_Invisible()
    {
        await ODataTestAsync(async () =>
        {
            var referredContentId = 1;

            var importerUser = await CreatePublicAdminUserAsync("Importer", default);
            Assert.IsFalse(importerUser.IsOperator);
            Assert.IsFalse(IsVisibleFor(importerUser, referredContentId));

            // ACT
            var result = await ImportHeadReferencesAsync(importerUser, referredContentId, CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual("created", result.Action);
            var importedContent = Node.LoadNode(result.Path);
            Assert.AreEqual(importerUser.Id, importedContent.OwnerId);
            Assert.AreEqual(importerUser.Id, importedContent.CreatedById);
            Assert.AreEqual(importerUser.Id, importedContent.VersionCreatedById);
            Assert.AreEqual(importerUser.Id, importedContent.ModifiedById);
            Assert.AreEqual(importerUser.Id, importedContent.VersionModifiedById);
            Assert.AreEqual(5, result.BrokenReferences.Length);
            Assert.AreEqual(0, result.Messages.Length);
        });
    }
    [TestMethod]
    [Description("The referred content is invisible: owner, creator or last modificator are not changed.")]
    public async STT.Task OD_Import_Update_HeadReferences_Invisible()
    {
        await ODataTestAsync(async () =>
        {
            var origUser = await CreatePublicAdminUserAsync("OriginalUser", default);

            var referredContentId = 1;

            var importerUser = await CreatePublicAdminUserAsync("Importer", default);
            Assert.IsFalse(importerUser.IsOperator);
            Assert.IsFalse(IsVisibleFor(importerUser, referredContentId));

            // ACT
            var result = await ReimportHeadReferencesAsync(importerUser, origUser.Id, referredContentId, CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual("updated", result.Action);
            var importedContent = Node.LoadNode(result.Path);
            Assert.AreEqual(origUser.Id, importedContent.OwnerId);
            Assert.AreEqual(origUser.Id, importedContent.CreatedById);
            Assert.AreEqual(origUser.Id, importedContent.VersionCreatedById);
            Assert.AreEqual(importerUser.Id, importedContent.ModifiedById);
            Assert.AreEqual(importerUser.Id, importedContent.VersionModifiedById);
            Assert.AreEqual(5, result.BrokenReferences.Length);
            Assert.AreEqual(0, result.Messages.Length);
        });
    }

    private async STT.Task<ImportResult> ReimportHeadReferencesAsync(IUser importerUser,
        int initialReferredContentId, int referredContentId, CancellationToken cancel)
    {
        var refNode = await Node.LoadNodeAsync(initialReferredContentId, cancel).ConfigureAwait(false);
        var folder = new Folder(await Node.LoadNodeAsync("/Root/Content", cancel))
        {
            Name = "Folder1",
            Owner = refNode,
            CreatedBy = refNode,
            ModifiedBy = refNode,
            VersionCreatedBy = refNode,
            VersionModifiedBy = refNode,
        };
        await folder.SaveAsync(cancel);

        Assert.AreEqual(initialReferredContentId, folder.OwnerId);
        return await ImportHeadReferencesAsync(importerUser, referredContentId, cancel).ConfigureAwait(false);
    }
    private async STT.Task<ImportResult> ImportHeadReferencesAsync(IUser importerUser, int referredContentId, CancellationToken cancel)
    {
        var importContent = new
        {
            ContentName = "Folder1",
            ContentType = "Folder",
            Fields = new
            {
                DisplayName = "Folder 1",
                Owner = referredContentId,
                CreatedBy = referredContentId,
                ModifiedBy = referredContentId,
                VersionCreatedBy = referredContentId,
                VersionModifiedBy = referredContentId,
            }
        };
        var data = JsonConvert.SerializeObject(importContent);

        // ACT
        ODataResponse response;
        using (new CurrentUserBlock(importerUser))
        {
            response = await ODataPostAsync(
                    $"/OData.svc/('Root')/{nameof(ImporterActions.Import)}",
                    "",
                    $"models=[{{'path':'/Root/Content/Folder1', 'data':{data}}}]")
                .ConfigureAwait(false);
        }

        // ASSERT
        AssertNoError(response);
        //var error = GetError(response, false);
        //if (error != null)
        //    Assert.Fail(error.Message);
        Assert.AreEqual(200, response.StatusCode);
        var result = GetObject(response).ToObject<ImportResult>();
        Assert.IsNotNull(result);

        return result;
    }

    /* ======================================================================================= Single reference */

    [TestMethod]
    public async STT.Task OD_Import_Create_SingleReference_Admin()
    {
        await ODataTestAsync(async () =>
        {
            var referredUser = await CreateUserAsync("Boss", default);

            var importerUser = await CreateAdminUserAsync("Importer", default);
            Assert.IsTrue(importerUser.IsOperator);
            Assert.IsTrue(IsVisibleFor(importerUser, referredUser.Id));

            // ACT
            var result = await ImportSingleReferenceAsync(importerUser, referredUser, CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual("created", result.Action);
            var importedContent = Node.Load<User>(result.Path);
            Assert.AreEqual(referredUser.Id, importedContent.GetReference<User>("Manager").Id);
            Assert.AreEqual(0, result.BrokenReferences.Length);
            Assert.AreEqual(0, result.Messages.Length);
        });
    }

    [TestMethod]
    public async STT.Task OD_Import_Create_SingleReference_Unknown()
    {
        await ODataTestAsync(async () =>
        {
            var referredUser = await CreateUserAsync("Boss", default);
            await Node.DeleteAsync(referredUser.Id, default);

            var importerUser = await CreateAdminUserAsync("Importer", default);
            Assert.IsTrue(importerUser.IsOperator);

            // ACT
            var result = await ImportSingleReferenceAsync(importerUser, referredUser, CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual("created", result.Action);
            var importedContent = Node.Load<User>(result.Path);
            Assert.AreEqual(null, importedContent.GetReference<User>("Manager"));
            Assert.AreEqual(1, result.BrokenReferences.Length);
            Assert.AreEqual(0, result.Messages.Length);
        });
    }
    [TestMethod]
    public async STT.Task OD_Import_Create_SingleReference_Invisible()
    {
        await ODataTestAsync(async () =>
        {
            var referredUser = await CreateUserAsync("AgentSmith", default).ConfigureAwait(false);
            var importerUser = await CreatePublicAdminUserAsync("Importer", default).ConfigureAwait(false);

            await Providers.Instance.SecurityHandler.CreateAclEditor()
                .Deny(referredUser.Id, importerUser.Id, false, PermissionType.See)
                .ApplyAsync(default).ConfigureAwait(false);

            Assert.IsFalse(importerUser.IsOperator);
            Assert.IsFalse(IsVisibleFor(importerUser, referredUser.Id));

            // ACT
            var result = await ImportSingleReferenceAsync(importerUser, referredUser, CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual("created", result.Action);
            Assert.AreEqual(1, result.BrokenReferences.Length);
            Assert.AreEqual(0, result.Messages.Length);
            var importedContent = Node.Load<User>(result.Path);
            Assert.IsNotNull(importedContent);
            // Assert for Admin
            Assert.AreEqual(null, importedContent.GetReference<User>("Manager"));
            // Assert for Importer
            using (new CurrentUserBlock(importerUser))
            {
                //UNDONE:xxx BUG: following 2 asserts are failed
                //Assert.AreEqual(null, importedContent.GetReference<User>("Manager"));
                //Assert.AreEqual(false, importedContent.HasReference("Manager", (Node)referredUser));
                //UNDONE:xxx BUG: delete the following assert and use importedContent.GetReference if the bug above resolved.
                var content = Content.Create(importedContent);
                var manager = ((List<Node>)content["Manager"]).FirstOrDefault();
                Assert.IsNull(manager);
            }
        });
    }
    [TestMethod]
    public async STT.Task OD_Import_Update_SingleReference_VisibleToInvisible()
    {
        await ODataTestAsync(async () =>
        {
            var initialReferredUser = await CreateUserAsync("Boss", default).ConfigureAwait(false);
            var referredUser = await CreateUserAsync("AgentSmith", default).ConfigureAwait(false);
            var importerUser = await CreatePublicAdminUserAsync("Importer", default).ConfigureAwait(false);

            await Providers.Instance.SecurityHandler.CreateAclEditor()
                .Deny(referredUser.Id, importerUser.Id, false, PermissionType.See)
                .ApplyAsync(default).ConfigureAwait(false);

            Assert.IsFalse(importerUser.IsOperator);
            Assert.IsFalse(IsVisibleFor(importerUser, referredUser.Id));

            // ACT
            var result = await ReimportSingleReferenceAsync(importerUser, initialReferredUser, referredUser, default)
                .ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual("updated", result.Action);
            Assert.AreEqual(1, result.BrokenReferences.Length);
            Assert.AreEqual(0, result.Messages.Length);
            var importedContent = Node.Load<User>(result.Path);
            Assert.IsNotNull(importedContent);
            // Assert for Admin
            Assert.AreEqual(initialReferredUser.Id, importedContent.GetReference<User>("Manager").Id);
            // Assert for Importer
            using (new CurrentUserBlock(importerUser))
                Assert.AreEqual(initialReferredUser.Id, importedContent.GetReference<User>("Manager").Id);
        });
    }
    [TestMethod]
    public async STT.Task OD_Import_Update_SingleReference_InvisibleToInvisible()
    {
        await ODataTestAsync(async () =>
        {
            var referredUser = await CreateUserAsync("AgentSmith", default).ConfigureAwait(false);
            var importerUser = await CreatePublicAdminUserAsync("Importer", default).ConfigureAwait(false);

            await Providers.Instance.SecurityHandler.CreateAclEditor()
                .Deny(referredUser.Id, importerUser.Id, false, PermissionType.See)
                .ApplyAsync(default).ConfigureAwait(false);

            Assert.IsFalse(importerUser.IsOperator);
            Assert.IsFalse(IsVisibleFor(importerUser, referredUser.Id));

            // ACT
            var result = await ReimportSingleReferenceAsync(importerUser, referredUser, referredUser, default)
                .ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual("updated", result.Action);
            Assert.AreEqual(1, result.BrokenReferences.Length);
            Assert.AreEqual(0, result.Messages.Length);
            var importedContent = Node.Load<User>(result.Path);
            Assert.IsNotNull(importedContent);
            // Assert for Admin
            Assert.AreEqual(referredUser.Id, importedContent.GetReference<User>("Manager").Id);
            // Assert for Importer
            using (new CurrentUserBlock(importerUser))
            {
                //UNDONE:xxx BUG: following 2 asserts are failed
                //Assert.AreEqual(null, importedContent.GetReference<User>("Manager"));
                //Assert.AreEqual(false, importedContent.HasReference("Manager", (Node)referredUser));
                //UNDONE:xxx BUG: delete the following assert and use importedContent.GetReference if the bug above resolved.
                var content = Content.Create(importedContent);
                var manager = ((List<Node>) content["Manager"]).FirstOrDefault();
                Assert.IsNull(manager);
            }
        });
    }

    private async STT.Task<ImportResult> ReimportSingleReferenceAsync(IUser importerUser,
        IUser initialReferredUser, IUser referredUser, CancellationToken cancel)
    {
        var refNode = await Node.LoadNodeAsync(initialReferredUser.Id, cancel).ConfigureAwait(false);
        var newUser = new User(await Node.LoadNodeAsync("/Root/IMS/Public", cancel))
        {
            Name = "ImportedUser1",
            Enabled = true,
            Email = "importeduser1@example.com",
        };
        var newUserContent = Content.Create(newUser);
        newUserContent["Manager"] = initialReferredUser;
        await newUserContent.SaveAsync(cancel);

        var loadedUser = await Node.LoadAsync<User>(newUser.Id, cancel).ConfigureAwait(false);
        Assert.IsNotNull(loadedUser);

        Assert.AreEqual("ImportedUser1", loadedUser.Name);
        Assert.AreEqual(true, loadedUser.Enabled);
        Assert.AreEqual("importeduser1@example.com", loadedUser.Email);
        Assert.AreEqual(initialReferredUser.Path, loadedUser.GetReference<User>("Manager").Path);

        return await ImportSingleReferenceAsync(importerUser, referredUser, cancel).ConfigureAwait(false);
    }
    private async STT.Task<ImportResult> ImportSingleReferenceAsync(IUser importerUser, IUser referredUser, CancellationToken cancel)
    {
        var importContent = new
        {
            ContentName = "ImportedUser1",
            ContentType = "User",
            Fields = new
            {
                //DisplayName = "Imported User 1",
                Manager = referredUser.Path,
            }
        };
        var data = JsonConvert.SerializeObject(importContent);

        // ACT
        ODataResponse response;
        using (new CurrentUserBlock(importerUser))
        {
            response = await ODataPostAsync(
                    $"/OData.svc/('Root')/{nameof(ImporterActions.Import)}",
                    "",
                    $"models=[{{'path':'/Root/IMS/Public/ImportedUser1', 'data':{data}}}]")
                .ConfigureAwait(false);
        }

        // ASSERT
        AssertNoError(response);
        //var error = GetError(response, false);
        //if (error != null)
        //    Assert.Fail(error.Message);
        Assert.AreEqual(200, response.StatusCode);
        var result = GetObject(response).ToObject<ImportResult>();
        Assert.IsNotNull(result);

        return result;
    }

    /* ======================================================================================= Multi reference */

    [TestMethod]
    public async STT.Task OD_Import_Create_MultiReference_Admin()
    {
        await ODataTestAsync(async () =>
        {
            var refUsers = new []
            {
                await CreateUserAsync("U1", default).ConfigureAwait(false),
                await CreateUserAsync("U2", default).ConfigureAwait(false),
            };

            var importerUser = await CreateAdminUserAsync("Importer", default).ConfigureAwait(false);

            // ACT
            var result = await ImportMultiReferenceAsync(importerUser, refUsers, CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual("created", result.Action);
            Assert.AreEqual(0, result.BrokenReferences.Length);
            Assert.AreEqual(0, result.Messages.Length);

            var importedContent = Node.Load<Group>(result.Path);
            Assert.IsNotNull(importedContent);
            var memberNames = importedContent.Members.Select(m => m.Name).OrderBy(x => x);
            Assert.AreEqual("U1,U2", string.Join(",", memberNames));
        });
    }
    [TestMethod]
    public async STT.Task OD_Import_Create_MultiReference_OneUnknown()
    {
        var cancel = new CancellationTokenSource(TimeSpan.FromMinutes(1.0d)).Token;

        await ODataTestAsync(async () =>
        {
            var refUsers = new[]
            {
                await CreateUserAsync("U1", cancel).ConfigureAwait(false),
                await CreateUserAsync("U2", cancel).ConfigureAwait(false),
                await CreateUserAsync("U3", cancel).ConfigureAwait(false),
            };
            await Node.DeleteAsync(refUsers[1].Id, cancel);

            var importerUser = await CreateAdminUserAsync("Importer", cancel).ConfigureAwait(false);

            // ACT
            var result = await ImportMultiReferenceAsync(importerUser, refUsers, CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual("created", result.Action);
            Assert.AreEqual(1, result.BrokenReferences.Length);
            Assert.AreEqual(0, result.Messages.Length);

            var importedContent = Node.Load<Group>(result.Path);
            Assert.IsNotNull(importedContent);
            var memberNames = importedContent.Members.Select(m => m.Name).OrderBy(x => x);
            Assert.AreEqual("U1,U3", string.Join(",", memberNames));
        });
    }
    [TestMethod]
    public async STT.Task OD_Import_Create_MultiReference_OneInvisible()
    {
        var cancel = new CancellationTokenSource(TimeSpan.FromMinutes(1.0d)).Token;

        await ODataTestAsync(async () =>
        {
            var refUsers = new[]
            {
                await CreateUserAsync("U1", cancel).ConfigureAwait(false),
                User.Administrator,
                await CreateUserAsync("U3", cancel).ConfigureAwait(false),
            };

            var importerUser = await CreatePublicAdminUserAsync("Importer", cancel).ConfigureAwait(false);
            Assert.IsTrue(IsVisibleFor(importerUser, refUsers[0].Id));
            Assert.IsFalse(IsVisibleFor(importerUser, refUsers[1].Id));
            Assert.IsTrue(IsVisibleFor(importerUser, refUsers[2].Id));

            // ACT
            var result = await ImportMultiReferenceAsync(importerUser, refUsers, CancellationToken.None)
                .ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual("created", result.Action);
            Assert.AreEqual(1, result.BrokenReferences.Length);
            Assert.AreEqual(0, result.Messages.Length);

            var importedContent = Node.Load<Group>(result.Path);
            Assert.IsNotNull(importedContent);
            var memberNames = importedContent.Members.Select(m => m.Name).OrderBy(x => x);
            Assert.AreEqual("U1,U3", string.Join(",", memberNames));
        });
    }
    [TestMethod]
    public async STT.Task OD_Import_Update_MultiReference_OneInvisible()
    {
        var cancel = new CancellationTokenSource(TimeSpan.FromMinutes(1.0d)).Token;

        await ODataTestAsync(async () =>
        {
            var initialRefUsers = new[]
            {
                await CreateUserAsync("U1", cancel).ConfigureAwait(false),
                User.Administrator,
                await CreateUserAsync("U3", cancel).ConfigureAwait(false),
            };
            var refUsers = new[]
            {
                initialRefUsers[0],
                initialRefUsers[1],
                await CreateUserAsync("U4", cancel).ConfigureAwait(false),
            };

            var importerUser = await CreatePublicAdminUserAsync("Importer", cancel).ConfigureAwait(false);
            Assert.IsTrue(IsVisibleFor(importerUser, refUsers[0].Id));
            Assert.IsFalse(IsVisibleFor(importerUser, refUsers[1].Id));
            Assert.IsTrue(IsVisibleFor(importerUser, refUsers[2].Id));

            // ACT
            var result = await ReimportMultiReferenceAsync(importerUser,
                    initialRefUsers, refUsers, cancel).ConfigureAwait(false);

            // ASSERT
            Assert.AreEqual("updated", result.Action);
            Assert.AreEqual(1, result.BrokenReferences.Length);
            Assert.AreEqual(0, result.Messages.Length);
            var importedContent = Node.Load<Group>(result.Path);
            Assert.IsNotNull(importedContent);
            // Assert for Admin
            var memberNames = importedContent.Members.Select(m => m.Name).OrderBy(x => x).ToArray();
            Assert.AreEqual("Admin,U1,U4", string.Join(",", memberNames));
            // Assert for Importer
            using (new CurrentUserBlock(importerUser))
                memberNames = importedContent.Members.Select(m => m.Name).OrderBy(x => x).ToArray();
            Assert.AreEqual("U1,U4", string.Join(",", memberNames));
        });
    }


    private async STT.Task<ImportResult> ReimportMultiReferenceAsync(IUser importerUser,
        IUser[] initialReferredUsers, IUser[] referredUsers, CancellationToken cancel)
    {
        var group = new Group(await Node.LoadNodeAsync("/Root/IMS/Public", cancel))
        {
            Name = "Group1",
            Members = initialReferredUsers.Cast<Node>()
        };
        await group.SaveAsync(cancel);

        var loadedGroup = await Node.LoadAsync<Group>(group.Id, cancel).ConfigureAwait(false);
        Assert.IsNotNull(loadedGroup);
        var expectedNames = string.Join(",", initialReferredUsers.Select(x => ((User)x).Name));
        var actualNames = string.Join(",", loadedGroup.Members.Select(x => x.Name));
        Assert.AreEqual(expectedNames, actualNames);

        return await ImportMultiReferenceAsync(importerUser, referredUsers, cancel).ConfigureAwait(false);
    }
    private async STT.Task<ImportResult> ImportMultiReferenceAsync(IUser importerUser, IUser[] referredUsers, CancellationToken cancel)
    {
        var importContent = new
        {
            ContentName = "Group1",
            ContentType = "Group",
            Fields = new
            {
                Members = referredUsers.Select(u=>u.Path).ToArray()
            }
        };
        var data = JsonConvert.SerializeObject(importContent);

        // ACT
        ODataResponse response;
        using (new CurrentUserBlock(importerUser))
        {
            response = await ODataPostAsync(
                    $"/OData.svc/('Root')/{nameof(ImporterActions.Import)}",
                    "",
                    $"models=[{{'path':'/Root/IMS/Public/Group1', 'data':{data}}}]")
                .ConfigureAwait(false);
        }

        // ASSERT
        AssertNoError(response);
        //var error = GetError(response, false);
        //if (error != null)
        //    Assert.Fail(error.Message);
        Assert.AreEqual(200, response.StatusCode);
        var result = GetObject(response).ToObject<ImportResult>();
        Assert.IsNotNull(result);

        return result;
    }

    /* ======================================================================================= TOOLS */

    private bool IsVisibleFor(IUser user, int contentId)
    {
        return Providers.Instance.SecurityHandler.HasPermission(user, contentId, PermissionType.See);
    }

    private async STT.Task<IUser> CreateAdminUserAsync(string name, CancellationToken cancel)
    {
        var user = await CreateUserAsync(name, cancel);
        Group.Administrators.AddMember(user);
        return user;
    }
    private async STT.Task<IUser> CreatePublicAdminUserAsync(string name, CancellationToken cancel)
    {
        var user = await CreateUserAsync(name, cancel);
        var group = await Node.LoadAsync<Group>("/Root/IMS/Public/Administrators", cancel);
        group.AddMember(user);
        return user;
    }
    private async STT.Task<IUser> CreateDeveloperUserAsync(string name, CancellationToken cancel)
    {
        var user = await CreateUserAsync(name, cancel);
        var group = await Node.LoadAsync<Group>("/Root/IMS/BuiltIn/Portal/Developers", cancel);
        group.AddMember(user);
        return user;
    }

    private async STT.Task<IUser> CreateUserAsync(string name, CancellationToken cancel)
    {
        //var user = new User(await Node.LoadNodeAsync("/Root/IMS/BuiltIn/Portal", cancel))
        var user = new User(await Node.LoadNodeAsync("/Root/IMS/Public", cancel))
        {
            Name = name,
            Email = $"{name.ToLowerInvariant()}@example.com",
            Enabled = true
        };
        await user.SaveAsync(cancel);
        return user;
    }
}