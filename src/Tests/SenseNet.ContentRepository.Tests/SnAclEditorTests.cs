using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Security;
using SenseNet.Tests.Core;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class SnAclEditorTests : TestBase
    {
        [TestMethod]
        public async STT.Task SnAclEditor_ApplyAsync_DoesNotPersistSystemUserPermissions()
        {
            await Test(async () =>
            {
                var folder = new SystemFolder(Repository.Root) { Name = "TestFolder" };
                await folder.SaveAsync(CancellationToken.None);

                var dataProvider = Providers.Instance.SecurityDataProvider;
                var initialEntries = await dataProvider.LoadPermissionEntriesAsync(new[] { folder.Id },
                    CancellationToken.None);
                Assert.IsFalse(initialEntries.Any(),
                    "The test folder should start without stored permission entries.");

                // Reproduction of https://github.com/SenseNet/sensenet/issues/2204:
                // SystemUser permissions must not be persisted, even in a batch with valid entries.
                await Providers.Instance.SecurityHandler.CreateAclEditor()
                    .Allow(folder.Id, Identifiers.SystemUserId, false, PermissionType.Open)
                    .Allow(folder.Id, User.Visitor.Id, false, PermissionType.Open)
                    .ApplyAsync(CancellationToken.None);

                // Read the backing store directly to verify persistence, not the security cache.
                var storedEntries = (await dataProvider.LoadPermissionEntriesAsync(new[] { folder.Id },
                    CancellationToken.None)).ToArray();
                Assert.IsTrue(storedEntries.Any(e => e.IdentityId == User.Visitor.Id &&
                                                    (e.AllowBits & PermissionType.Open.Mask) != 0),
                    "Valid permission changes in the same batch should still be persisted.");
                Assert.AreEqual(0, storedEntries.Count(e => e.IdentityId == Identifiers.SystemUserId),
                    "SystemUser permissions (IdentityId = -1) must not be persisted.");
                Assert.IsFalse(folder.Security.GetExplicitEntries().Any(e => e.IdentityId == Identifiers.SystemUserId),
                    "SystemUser permissions must not be applied to the security cache.");
            });
        }

        [DataTestMethod]
        [DataRow(0, false, EntryType.Normal)]
        [DataRow(0, true, EntryType.Normal)]
        [DataRow(-1, false, EntryType.Normal)]
        [DataRow(-1, true, EntryType.Normal)]
        [DataRow(-42, false, EntryType.Normal)]
        [DataRow(-42, true, EntryType.Normal)]
        [DataRow(0, false, EntryType.Sharing)]
        [DataRow(0, true, EntryType.Sharing)]
        [DataRow(-1, false, EntryType.Sharing)]
        [DataRow(-1, true, EntryType.Sharing)]
        [DataRow(-42, false, EntryType.Sharing)]
        [DataRow(-42, true, EntryType.Sharing)]
        public async STT.Task SnAclEditor_ApplyAsync_IgnoresNonPositiveIdentitySettings(
            int identityId, bool localOnly, EntryType entryType)
        {
            await Test(async () =>
            {
                var folder = new SystemFolder(Repository.Root) { Name = "TestFolder" };
                await folder.SaveAsync(CancellationToken.None);

                // Exercise the base-typed API as well: Allow/Deny/Set are hidden, not overridden.
                var settings = new Action<AclEditor>[]
                {
                    editor => editor.Allow(folder.Id, identityId, localOnly, PermissionType.Open),
                    editor => editor.Deny(folder.Id, identityId, localOnly, PermissionType.Open),
                    editor => editor.Set(folder.Id, identityId, localOnly,
                        new PermissionBitMask { AllowBits = PermissionType.Open.Mask, DenyBits = PermissionType.Delete.Mask })
                };
                foreach (var setPermissions in settings)
                {
                    var editor = Providers.Instance.SecurityHandler.SecurityContext.CreateAclEditor(entryType);
                    setPermissions(editor);
                    editor.Allow(folder.Id, User.Visitor.Id, localOnly, PermissionType.Open);
                    await editor.ApplyAsync(CancellationToken.None);

                    await AssertNoInvalidIdentityEntriesAsync(folder.Id);
                    var storedEntries = await Providers.Instance.SecurityDataProvider.LoadPermissionEntriesAsync(
                        new[] { folder.Id }, CancellationToken.None);
                    Assert.IsTrue(storedEntries.Any(e => e.IdentityId == User.Visitor.Id &&
                        e.EntryType == entryType && e.LocalOnly == localOnly &&
                        (e.AllowBits & PermissionType.Open.Mask) != 0),
                        "Valid settings in the same batch must still be persisted.");
                    Assert.IsTrue(folder.Security.GetExplicitEntries(entryType).Any(e =>
                        e.IdentityId == User.Visitor.Id && e.LocalOnly == localOnly &&
                        (e.AllowBits & PermissionType.Open.Mask) != 0),
                        "Valid settings in the same batch must still be applied to the security cache.");
                }
            });
        }

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        [DataRow(-42)]
        public async STT.Task SnAclEditor_ApplyAsync_OnlyInvalidSettingsPreserveOtherEntries(int identityId)
        {
            await Test(async () =>
            {
                var folder = new SystemFolder(Repository.Root) { Name = "TestFolder" };
                await folder.SaveAsync(CancellationToken.None);
                var context = Providers.Instance.SecurityHandler.SecurityContext;

                // The ignored Normal entry must not remove the existing Sharing ACL from the cache.
                await context.CreateAclEditor(EntryType.Sharing)
                    .Allow(folder.Id, User.Visitor.Id, false, PermissionType.Open)
                    .ApplyAsync(CancellationToken.None);
                await context.CreateAclEditor()
                    .Allow(folder.Id, identityId, false, PermissionType.Open)
                    .ApplyAsync(CancellationToken.None);

                await AssertNoInvalidIdentityEntriesAsync(folder.Id);
                Assert.IsTrue(folder.Security.GetExplicitEntries(EntryType.Sharing)
                    .Any(e => e.IdentityId == User.Visitor.Id && (e.AllowBits & PermissionType.Open.Mask) != 0),
                    "Ignoring invalid settings must preserve the existing ACL.");
            });
        }

        [DataTestMethod]
        [DataRow(0, EntryType.Normal)]
        [DataRow(-1, EntryType.Normal)]
        [DataRow(-42, EntryType.Normal)]
        [DataRow(0, EntryType.Sharing)]
        [DataRow(-1, EntryType.Sharing)]
        [DataRow(-42, EntryType.Sharing)]
        public async STT.Task SnAclEditor_ApplyAsync_CanRemoveExistingInvalidIdentityEntries(
            int identityId, EntryType entryType)
        {
            await Test(async () =>
            {
                var folder = new SystemFolder(Repository.Root) { Name = "TestFolder" };
                await folder.SaveAsync(CancellationToken.None);
                var context = Providers.Instance.SecurityHandler.SecurityContext;

                // Seed legacy invalid entries through the underlying security component.
                var rawContext = new SecurityContext(context.CurrentUser, context.SecuritySystem);
                await rawContext.CreateAclEditor(entryType)
                    .Allow(folder.Id, identityId, false, PermissionType.Open)
                    .Allow(folder.Id, User.Visitor.Id, false, PermissionType.Open)
                    .ApplyAsync(CancellationToken.None);
                Assert.IsTrue(folder.Security.GetExplicitEntries().Any(e => e.IdentityId == identityId));

                await context.CreateSnAclEditor(entryType)
                    .Reset(folder.Id, identityId, false, ulong.MaxValue, ulong.MaxValue)
                    .ApplyAsync(CancellationToken.None);

                await AssertNoInvalidIdentityEntriesAsync(folder.Id);
                Assert.IsTrue(folder.Security.GetExplicitEntries().Any(e => e.IdentityId == User.Visitor.Id),
                    "Removing an invalid entry must preserve other identities' permissions.");
            });
        }

        [DataTestMethod]
        [DataRow(EntryType.Normal)]
        [DataRow(EntryType.Sharing)]
        public async STT.Task SnAclEditor_ApplyAsync_RemovesLegacyInvalidEntriesWhenClearingValidPermissions(
            EntryType invalidEntryType)
        {
            await Test(async () =>
            {
                var folder = new SystemFolder(Repository.Root) { Name = "TestFolder" };
                await folder.SaveAsync(CancellationToken.None);
                var context = Providers.Instance.SecurityHandler.SecurityContext;
                var rawContext = new SecurityContext(context.CurrentUser, context.SecuritySystem);
                await rawContext.CreateAclEditor(invalidEntryType)
                    .Allow(folder.Id, -42, false, PermissionType.Open)
                    .ApplyAsync(CancellationToken.None);
                await rawContext.CreateAclEditor()
                    .Allow(folder.Id, User.Visitor.Id, false, PermissionType.Open)
                    .ApplyAsync(CancellationToken.None);

                await Providers.Instance.SecurityHandler.CreateAclEditor()
                    .Allow(folder.Id, 0, false, PermissionType.Open)
                    .Reset(folder.Id, User.Visitor.Id, false, ulong.MaxValue, ulong.MaxValue)
                    .ApplyAsync(CancellationToken.None);

                var storedEntries = await Providers.Instance.SecurityDataProvider.LoadPermissionEntriesAsync(
                    new[] { folder.Id }, CancellationToken.None);
                Assert.IsFalse(storedEntries.Any(), "All affected entries must be removed from storage.");
                Assert.AreEqual(0, folder.Security.GetExplicitEntries().Count,
                    "The cache must reflect the same removals as storage.");
            });
        }

        [TestMethod]
        public async STT.Task SnAclEditor_ApplyAsync_IgnoredSettingsPreserveInheritanceChanges()
        {
            await Test(async () =>
            {
                var folder = new SystemFolder(Repository.Root) { Name = "TestFolder" };
                await folder.SaveAsync(CancellationToken.None);
                var context = Providers.Instance.SecurityHandler.SecurityContext;

                await Providers.Instance.SecurityHandler.CreateAclEditor()
                    .Allow(folder.Id, 0, false, PermissionType.Open)
                    .BreakInheritance(folder.Id, Array.Empty<EntryType>())
                    .ApplyAsync(CancellationToken.None);
                Assert.IsFalse(context.IsEntityInherited(folder.Id));
                await AssertNoInvalidIdentityEntriesAsync(folder.Id);

                await Providers.Instance.SecurityHandler.CreateAclEditor()
                    .Allow(folder.Id, -42, false, PermissionType.Open)
                    .UnbreakInheritance(folder.Id, Array.Empty<EntryType>())
                    .ApplyAsync(CancellationToken.None);
                Assert.IsTrue(context.IsEntityInherited(folder.Id));
                await AssertNoInvalidIdentityEntriesAsync(folder.Id);
            });
        }

        private static async STT.Task AssertNoInvalidIdentityEntriesAsync(int entityId)
        {
            var storedEntries = await Providers.Instance.SecurityDataProvider.LoadPermissionEntriesAsync(
                new[] { entityId }, CancellationToken.None);
            Assert.IsFalse(storedEntries.Any(e => e.IdentityId <= 0),
                "Permissions for non-positive identity ids must not be persisted.");
            var cachedEntries = Providers.Instance.SecurityHandler.SecurityContext.GetExplicitEntries(entityId);
            Assert.IsFalse(cachedEntries.Any(e => e.IdentityId <= 0),
                "Permissions for non-positive identity ids must not be applied to the security cache.");
        }
    }
}
