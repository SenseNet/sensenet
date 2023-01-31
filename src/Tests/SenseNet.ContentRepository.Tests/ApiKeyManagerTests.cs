using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using STT = System.Threading.Tasks;
using SenseNet.Tests.Core;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Security.ApiKeys;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ApiKeyManagerTests : TestBase
    {
        [TestMethod, TestCategory("ApiKey")]
        public STT.Task ApiKey_GetApiKeys_Admin()
        {
            return ApiKeyManagerTest(async akm =>
            {
                var apiKeys = await akm.GetApiKeysByUserAsync(Identifiers.AdministratorUserId, CancellationToken.None);
                var adminApiKey = apiKeys.First().Value;

                Assert.AreEqual(1, apiKeys.Length);

                // load other users' api keys as an admin
                var user1 = User.Load("public\\user1");
                apiKeys = await akm.GetApiKeysByUserAsync(user1.Id, CancellationToken.None);

                Assert.AreEqual(1, apiKeys.Length);
                Assert.AreNotEqual(adminApiKey, apiKeys.First().Value);
            });
        }
        [TestMethod, TestCategory("ApiKey")]
        public STT.Task ApiKey_GetApiKeys_PublicAdmin()
        {
            return ApiKeyManagerTest(async akm =>
            {
                var testUser = await Node.LoadAsync<User>(Identifiers.PublicAdminPath, CancellationToken.None);
                var originalUser = AccessProvider.Current.GetCurrentUser();

                AccessProvider.Current.SetCurrentUser(testUser);

                try
                {
                    // builtin admin keys are NOT accessible
                    var apiKeys = await akm.GetApiKeysByUserAsync(Identifiers.AdministratorUserId, CancellationToken.None);
                    Assert.AreEqual(0, apiKeys.Length);

                    // own keys
                    var publicAdmin = NodeHead.Get(Identifiers.PublicAdminPath);
                    apiKeys = await akm.GetApiKeysByUserAsync(publicAdmin.Id, CancellationToken.None);
                    Assert.AreEqual(1, apiKeys.Length);

                    // other admin users' keys are accessible
                    var user1 = User.Load("public\\user1");
                    apiKeys = await akm.GetApiKeysByUserAsync(user1.Id, CancellationToken.None);
                    Assert.AreEqual(1, apiKeys.Length);

                    // other users' keys are NOT accessible
                    var user2 = await SystemAccount.ExecuteAsync(() => STT.Task.FromResult(User.Load("domain2\\user2")));
                    apiKeys = await akm.GetApiKeysByUserAsync(user2.Id, CancellationToken.None);
                    Assert.AreEqual(0, apiKeys.Length);
                }
                finally
                {
                    AccessProvider.Current.SetCurrentUser(originalUser);
                }
            });
        }
        [TestMethod, TestCategory("ApiKey")]
        public STT.Task ApiKey_GetApiKeys_RegularUser()
        {
            return ApiKeyManagerTest(async akm =>
            {
                var testUser = await Node.LoadAsync<User>("/Root/IMS/domain2/user2", CancellationToken.None);
                var originalUser = AccessProvider.Current.GetCurrentUser();

                AccessProvider.Current.SetCurrentUser(testUser);

                try
                {
                    // builtin admin keys are NOT accessible
                    var apiKeys = await akm.GetApiKeysByUserAsync(Identifiers.AdministratorUserId, CancellationToken.None);
                    Assert.AreEqual(0, apiKeys.Length);

                    // public admin keys are NOT accessible
                    var publicAdmin = NodeHead.Get(Identifiers.PublicAdminPath);
                    apiKeys = await akm.GetApiKeysByUserAsync(publicAdmin.Id, CancellationToken.None);
                    Assert.AreEqual(0, apiKeys.Length);

                    // other public admin users' keys are NOT accessible
                    var user1 = await SystemAccount.ExecuteAsync(() => STT.Task.FromResult(User.Load("public\\user1")));
                    apiKeys = await akm.GetApiKeysByUserAsync(user1.Id, CancellationToken.None);
                    Assert.AreEqual(0, apiKeys.Length);

                    // own keys are accessible
                    apiKeys = await akm.GetApiKeysByUserAsync(AccessProvider.Current.GetCurrentUser().Id, CancellationToken.None);
                    Assert.AreEqual(1, apiKeys.Length);
                }
                finally
                {
                    AccessProvider.Current.SetCurrentUser(originalUser);
                }
            });
        }

        protected STT.Task ApiKeyManagerTest(Func<IApiKeyManager, STT.Task> callback)
        {
            IApiKeyManager apiKeyManager = null;

            return Test2(null,
                initialize: repoBuilder =>
                {
                    apiKeyManager = (IApiKeyManager)repoBuilder.Services.GetService(typeof(IApiKeyManager));
                },
                callback: async () =>
                {
                    await CreateUsersAndApiKeys(apiKeyManager);
                    await callback(apiKeyManager);
                });
        }
        
        private static async STT.Task CreateUsersAndApiKeys(IApiKeyManager apiKeyManager)
        {
            // public\user1     --> member of PublicAdmins
            // domain2\user2

            var publicDomain = Node.Load<Domain>("/Root/IMS/Public");
            var user1Content = Content.CreateNew("User", publicDomain, "user1");
            user1Content["Enabled"] = true;

            await user1Content.SaveAsync(CancellationToken.None);
            var user1 = (User)user1Content.ContentHandler;

            var publicAdminGroup =
                await Node.LoadAsync<Group>(ApplicationModel.N.R.PublicAdministrators, CancellationToken.None);

            publicAdminGroup.AddMember(user1);

            var domain2 = Content.CreateNew("Domain", Node.LoadNode("/Root/IMS"), "domain2");
            await domain2.SaveAsync(CancellationToken.None);

            var user2 = Content.CreateNew("User", domain2.ContentHandler, "user2");
            await user2.SaveAsync(CancellationToken.None);

            var publicAdmin = NodeHead.Get(Identifiers.PublicAdminPath);

            await apiKeyManager.CreateApiKeyAsync(publicAdmin.Id, DateTime.Today.AddDays(10), CancellationToken.None);
            await apiKeyManager.CreateApiKeyAsync(user1.Id, DateTime.Today.AddDays(10), CancellationToken.None);
            await apiKeyManager.CreateApiKeyAsync(user2.Id, DateTime.Today.AddDays(10), CancellationToken.None);
            await apiKeyManager.CreateApiKeyAsync(Identifiers.AdministratorUserId, DateTime.Today.AddDays(10), CancellationToken.None);
            await apiKeyManager.CreateApiKeyAsync(Identifiers.VisitorUserId, DateTime.Today.AddDays(10), CancellationToken.None);
        }
    }
}
