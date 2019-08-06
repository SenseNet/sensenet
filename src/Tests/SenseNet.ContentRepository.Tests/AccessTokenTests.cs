using System;
using System.Linq;
using System.Threading;
using Tasks = System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Tests;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class AccessTokenTests : TestBase
    {
        [TestMethod]
        public void AccessToken_Create_ForUser()
        {
            var userId = 42;
            var timeout = TimeSpan.FromMinutes(10);

            // ACTION
            var token = AccessTokenVault.CreateToken(userId, timeout);

            // ASSERT
            Assert.IsTrue(token.Id > 0);
            Assert.IsNotNull(token.Value);
            Assert.AreEqual(userId, token.UserId);
            Assert.AreEqual(0, token.ContentId);
            Assert.IsNull(token.Feature);
            Assert.IsTrue((DateTime.UtcNow - token.CreationDate).TotalMilliseconds < 1000);
            Assert.IsTrue((token.ExpirationDate - DateTime.UtcNow - timeout).TotalMilliseconds < 1000);
        }
        [TestMethod]
        public void AccessToken_Create_ForUser_ValueLength()
        {
            var userId = 42;
            var timeout = TimeSpan.FromMinutes(10);

            // ACTION
            var token = AccessTokenVault.CreateToken(userId, timeout);

            // ASSERT
            Assert.IsTrue(token.Value.Length >= 50);
        }
        [TestMethod]
        public void AccessToken_Create_ForUser_Twice()
        {
            var userId = 42;
            var timeout = TimeSpan.FromMinutes(10);

            // ACTION
            var token1 = AccessTokenVault.CreateToken(userId, timeout);
            var token2 = AccessTokenVault.CreateToken(userId, timeout);

            // ASSERT
            Assert.AreNotEqual(token1.Id, token2.Id);
            Assert.AreNotEqual(token1.Value, token2.Value);
        }
        [TestMethod]
        public void AccessToken_Create_ForUserAndContent()
        {
            var userId = 42;
            var contentId = 142;
            var timeout = TimeSpan.FromMinutes(10);

            // ACTION
            var token = AccessTokenVault.CreateToken(userId, timeout, contentId);

            // ASSERT
            Assert.IsTrue(token.Id > 0);
            Assert.IsNotNull(token.Value);
            Assert.AreEqual(userId, token.UserId);
            Assert.AreEqual(contentId, token.ContentId);
            Assert.IsNull(token.Feature);
            Assert.IsTrue((DateTime.UtcNow - token.CreationDate).TotalMilliseconds < 1000);
            Assert.IsTrue((token.ExpirationDate - DateTime.UtcNow - timeout).TotalMilliseconds < 1000);
        }
        [TestMethod]
        public void AccessToken_Create_ForUserAndFeature()
        {
            var userId = 42;
            var feature = "Feature1";
            var timeout = TimeSpan.FromMinutes(10);

            // ACTION
            var token = AccessTokenVault.CreateToken(userId, timeout, 0, feature);

            // ASSERT
            Assert.IsTrue(token.Id > 0);
            Assert.IsNotNull(token.Value);
            Assert.AreEqual(userId, token.UserId);
            Assert.AreEqual(0, token.ContentId);
            Assert.AreEqual(feature, token.Feature);
            Assert.IsTrue((DateTime.UtcNow - token.CreationDate).TotalMilliseconds < 1000);
            Assert.IsTrue((token.ExpirationDate - DateTime.UtcNow - timeout).TotalMilliseconds < 1000);
        }
        [TestMethod]
        public void AccessToken_Create_ForUserContentAndFeature()
        {
            var userId = 42;
            var contentId = 142;
            var feature = "Feature1";
            var timeout = TimeSpan.FromMinutes(10);

            // ACTION
            var token = AccessTokenVault.CreateToken(userId, timeout, contentId, feature);

            // ASSERT
            Assert.IsTrue(token.Id > 0);
            Assert.IsNotNull(token.Value);
            Assert.AreEqual(userId, token.UserId);
            Assert.AreEqual(contentId, token.ContentId);
            Assert.AreEqual(feature, token.Feature);
            Assert.IsTrue((DateTime.UtcNow - token.CreationDate).TotalMilliseconds < 1000);
            Assert.IsTrue((token.ExpirationDate - DateTime.UtcNow - timeout).TotalMilliseconds < 1000);
        }

        [TestMethod]
        public void AccessToken_Get_ForUser()
        {
            var userId = 42;
            var timeout = TimeSpan.FromMinutes(10);
            var savedToken = AccessTokenVault.CreateToken(userId, timeout);

            // ACTION
            var token = AccessTokenVault.GetToken(savedToken.Value);

            // ASSERT
            AssertTokensAreEqual(savedToken, token);
        }
        [TestMethod]
        public void AccessToken_Get_ForUserAndContent()
        {
            var userId = 42;
            var contentId = 142;
            var timeout = TimeSpan.FromMinutes(10);
            var savedToken = AccessTokenVault.CreateToken(userId, timeout, contentId);

            // ACTION
            var token = AccessTokenVault.GetToken(savedToken.Value, contentId);

            // ASSERT
            AssertTokensAreEqual(savedToken, token);
            Assert.IsNull(AccessTokenVault.GetToken(savedToken.Value));
        }
        [TestMethod]
        public void AccessToken_Get_ForUserAndFeature()
        {
            var userId = 42;
            var feature = "Feature1";
            var timeout = TimeSpan.FromMinutes(10);
            var savedToken = AccessTokenVault.CreateToken(userId, timeout, 0, feature);

            // ACTION
            var token = AccessTokenVault.GetToken(savedToken.Value, 0, feature);

            // ASSERT
            AssertTokensAreEqual(savedToken, token);
            Assert.IsNull(AccessTokenVault.GetToken(savedToken.Value));
        }
        [TestMethod]
        public void AccessToken_Get_ForUserContentAndFeature()
        {
            var userId = 42;
            var contentId = 142;
            var feature = "Feature1";
            var timeout = TimeSpan.FromMinutes(10);
            var savedToken = AccessTokenVault.CreateToken(userId, timeout, contentId, feature);

            // ACTION
            var token = AccessTokenVault.GetToken(savedToken.Value, contentId, feature);

            // ASSERT
            AssertTokensAreEqual(savedToken, token);
            Assert.IsNull(AccessTokenVault.GetToken(savedToken.Value));
            Assert.IsNull(AccessTokenVault.GetToken(savedToken.Value, 0, feature));
            Assert.IsNull(AccessTokenVault.GetToken(savedToken.Value, contentId));
        }
        [TestMethod]
        public void AccessToken_Get_Expired()
        {
            var userId = 42;
            var timeout = TimeSpan.FromMilliseconds(1);
            var savedToken = AccessTokenVault.CreateToken(userId, timeout);

            // ACTION
            Thread.Sleep(10);
            var token = AccessTokenVault.GetToken(savedToken.Value);

            // ASSERT
            Assert.IsNull(token);
        }

        [TestMethod]
        public void AccessToken_GetByUser()
        {
            var userId = 42;
            var contentId = 142;
            var feature = "Feature1";
            var timeout = TimeSpan.FromMinutes(10);
            var shortTimeout = TimeSpan.FromSeconds(1);
            var savedTokens = new[]
            {
                AccessTokenVault.CreateToken(userId, timeout),
                AccessTokenVault.CreateToken(userId, timeout, contentId),
                AccessTokenVault.CreateToken(userId, timeout, 0, feature),
                AccessTokenVault.CreateToken(userId, timeout, contentId, feature),
                AccessTokenVault.CreateToken(userId, shortTimeout),
                AccessTokenVault.CreateToken(userId, shortTimeout, contentId),
                AccessTokenVault.CreateToken(userId, shortTimeout, 0, feature),
                AccessTokenVault.CreateToken(userId, shortTimeout, contentId, feature),
            };

            // ACTION-1
            var tokens = AccessTokenVault.GetAllTokens(userId);

            // ASSERT-1
            Assert.AreEqual(
                string.Join(",", savedTokens.OrderBy(x => x.Id).Select(x => x.Id.ToString())),
                string.Join(",", tokens.OrderBy(x => x.Id).Select(x => x.Id.ToString())));

            // ACTION-2
            Thread.Sleep(1100);
            tokens = AccessTokenVault.GetAllTokens(userId);

            // ASSERT-2
            // The last 4 tokens are expired
            Assert.AreEqual(
                string.Join(",", savedTokens.Take(4).OrderBy(x => x.Id).Select(x => x.Id.ToString())),
                string.Join(",", tokens.OrderBy(x => x.Id).Select(x => x.Id.ToString())));
        }

        [TestMethod]
        public void AccessToken_GetOrAdd_WithFeature()
        {
            AccessToken_GetOrAdd(42, 142, "testfeature");
        }
        [TestMethod]
        public void AccessToken_GetOrAdd_WithContent()
        {
            AccessToken_GetOrAdd(42, 142);
        }
        [TestMethod]
        public void AccessToken_GetOrAdd_WithUser()
        {
            AccessToken_GetOrAdd(42);
        }
        private void AccessToken_GetOrAdd(int userId, int contentId = 0, string feature = null)
        {
            var timeout1 = TimeSpan.FromMinutes(3);
            var timeout2 = TimeSpan.FromMinutes(10);
            var timeout3 = TimeSpan.FromMinutes(20);

            // create three different tokens
            var savedToken1 = AccessTokenVault.CreateToken(userId, timeout1, contentId, feature);
            var savedToken2 = AccessTokenVault.CreateToken(userId, timeout2, contentId, feature);
            var savedToken3 = AccessTokenVault.CreateToken(userId, timeout3, contentId, feature);

            // ACTION: get a token with the same parameters
            var token = AccessTokenVault.GetOrAddToken(userId, timeout3, contentId, feature);

            // ASSERT: we should get the last one
            AssertTokensAreEqual(savedToken3, token);

            // ACTION: get a token with shorter expiration time
            token = AccessTokenVault.GetOrAddToken(userId, timeout2, contentId, feature);

            // ASSERT: we should get the previous one
            AssertTokensAreEqual(savedToken2, token);

            // ACTION: get a token with an even shorter expiration time
            token = AccessTokenVault.GetOrAddToken(userId, TimeSpan.FromMinutes(7), contentId, feature);

            // ASSERT: we should get a totally new one, because the first 
            // token (savedToken1) expires too soon. 
            Assert.AreNotEqual(savedToken1.Value, token.Value);
            Assert.AreNotEqual(savedToken2.Value, token.Value);
            Assert.AreNotEqual(savedToken3.Value, token.Value);
            Assert.IsTrue(token.ExpirationDate < savedToken2.ExpirationDate);
        }

        [TestMethod]
        public void AccessToken_Exists()
        {
            var userId = 42;
            var timeout = TimeSpan.FromMinutes(10);
            var savedToken = AccessTokenVault.CreateToken(userId, timeout);

            // ACTION
            var isExists = AccessTokenVault.TokenExists(savedToken.Value);

            // ASSERT
            Assert.IsTrue(isExists);
        }
        [TestMethod]
        public void AccessToken_Exists_Missing()
        {
            // ACTION
            var isExists = AccessTokenVault.TokenExists("asdf");

            // ASSERT
            Assert.IsFalse(isExists);
        }
        [TestMethod]
        public void AccessToken_Exists_Expired()
        {
            var userId = 42;
            var timeout = TimeSpan.FromMilliseconds(1);
            var savedToken = AccessTokenVault.CreateToken(userId, timeout);

            // ACTION
            Thread.Sleep(1100);
            var isExists = AccessTokenVault.TokenExists(savedToken.Value);

            // ASSERT
            Assert.IsFalse(isExists);
        }

        [TestMethod]
        public void AccessToken_AssertExists()
        {
            var userId = 42;
            var timeout = TimeSpan.FromMinutes(10);
            var savedToken = AccessTokenVault.CreateToken(userId, timeout);

            // ACTION
            AccessTokenVault.AssertTokenExists(savedToken.Value);

            //Assert.AllRight() :)
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidAccessTokenException))]
        public async Tasks.Task AccessToken_AssertExists_Missing()
        {
           await/*undone*/ AccessTokenVault.AssertTokenExistsAsync("asdf");
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidAccessTokenException))]
        public async Tasks.Task AccessToken_AssertExists_Expired()
        {
            var userId = 42;
            var timeout = TimeSpan.FromMilliseconds(1);
            var savedToken = AccessTokenVault.CreateToken(userId, timeout);

            // ACTION
            Thread.Sleep(1100);

            await/*undone*/ AccessTokenVault.AssertTokenExistsAsync(savedToken.Value);
        }

        [TestMethod]
        public void AccessToken_Update()
        {
            var userId = 42;
            var timeout = TimeSpan.FromMinutes(10.0d);
            var savedToken = AccessTokenVault.CreateToken(userId, timeout);
            Assert.IsTrue(savedToken.ExpirationDate < DateTime.UtcNow.AddMinutes(20.0d));

            // ACTION
            AccessTokenVault.UpdateToken(savedToken.Value, DateTime.UtcNow.AddMinutes(30.0d));

            // ASSERT
            var loadedToken = AccessTokenVault.GetToken(savedToken.Value);
            Assert.IsNotNull(loadedToken);
            Assert.IsTrue(loadedToken.ExpirationDate > DateTime.UtcNow.AddMinutes(20.0d));
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidAccessTokenException))]
        public void AccessToken_UpdateMissing()
        {
            AccessTokenVault.UpdateToken("asdf", DateTime.UtcNow.AddMinutes(30.0d));
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidAccessTokenException))]
        public void AccessToken_UpdateExpired()
        {
            var userId = 42;
            var timeout = TimeSpan.FromMilliseconds(1);
            var savedToken = AccessTokenVault.CreateToken(userId, timeout);

            // ACTION
            Thread.Sleep(1100);
            AccessTokenVault.UpdateToken(savedToken.Value, DateTime.UtcNow.AddMinutes(30.0d));
        }

        [TestMethod]
        public void AccessToken_Delete_Token()
        {
            var userId1 = 42;
            var userId2 = 43;
            var timeout = TimeSpan.FromMinutes(10);
            var shortTimeout = TimeSpan.FromSeconds(1);
            var savedTokens = new[]
            {
                AccessTokenVault.CreateToken(userId1, timeout),
                AccessTokenVault.CreateToken(userId1, shortTimeout),
                AccessTokenVault.CreateToken(userId2, timeout),
                AccessTokenVault.CreateToken(userId2, shortTimeout),
            };

            // ACTION
            Thread.Sleep(1100);
            AccessTokenVault.DeleteToken(savedTokens[0].Value);
            AccessTokenVault.DeleteToken(savedTokens[3].Value);

            // ASSERT
            Assert.IsNull(AccessTokenVault.GetTokenById(savedTokens[0].Id));
            Assert.IsNotNull(AccessTokenVault.GetTokenById(savedTokens[1].Id));
            Assert.IsNotNull(AccessTokenVault.GetTokenById(savedTokens[2].Id));
            Assert.IsNull(AccessTokenVault.GetTokenById(savedTokens[3].Id));
        }
        [TestMethod]
        public void AccessToken_Delete_ByUser()
        {
            var userId1 = 42;
            var userId2 = 43;
            var timeout = TimeSpan.FromMinutes(10);
            var shortTimeout = TimeSpan.FromSeconds(1);
            var savedTokens = new[]
            {
                AccessTokenVault.CreateToken(userId1, timeout),
                AccessTokenVault.CreateToken(userId1, shortTimeout),
                AccessTokenVault.CreateToken(userId2, timeout),
                AccessTokenVault.CreateToken(userId2, shortTimeout),
            };

            // ACTION
            Thread.Sleep(1100);
            AccessTokenVault.DeleteTokensByUser(userId1);

            // ASSERT
            Assert.IsNull(AccessTokenVault.GetTokenById(savedTokens[0].Id));
            Assert.IsNull(AccessTokenVault.GetTokenById(savedTokens[1].Id));
            Assert.IsNotNull(AccessTokenVault.GetTokenById(savedTokens[2].Id));
            Assert.IsNotNull(AccessTokenVault.GetTokenById(savedTokens[3].Id));
        }
        [TestMethod]
        public void AccessToken_Delete_ByContent()
        {
            var userId1 = 42;
            var userId2 = 43;
            var contentId1 = 142;
            var contentId2 = 143;
            var timeout = TimeSpan.FromMinutes(10);
            var shortTimeout = TimeSpan.FromSeconds(1);
            var savedTokens = new[]
            {
                AccessTokenVault.CreateToken(userId1, timeout, contentId1),
                AccessTokenVault.CreateToken(userId1, shortTimeout, contentId2),
                AccessTokenVault.CreateToken(userId2, timeout, contentId1),
                AccessTokenVault.CreateToken(userId2, shortTimeout, contentId2),
            };

            // ACTION
            Thread.Sleep(1100);
            AccessTokenVault.DeleteTokensByContent(contentId1);

            // ASSERT
            Assert.IsNull(AccessTokenVault.GetTokenById(savedTokens[0].Id));
            Assert.IsNotNull(AccessTokenVault.GetTokenById(savedTokens[1].Id));
            Assert.IsNull(AccessTokenVault.GetTokenById(savedTokens[2].Id));
            Assert.IsNotNull(AccessTokenVault.GetTokenById(savedTokens[3].Id));
        }

        /* ===================================================================================== */

        private static RepositoryInstance _repository;

        [ClassInitialize]
        public static void InitializeRepositoryInstance(TestContext context)
        {
            Cache.Reset();
            ContentTypeManager.Reset();

            var builder = CreateRepositoryBuilderForTest();

            Indexing.IsOuterSearchEngineEnabled = true;

            _repository = Repository.Start(builder);

            using (new SystemAccount())
            {
                SecurityHandler.CreateAclEditor()
                    .Allow(Identifiers.PortalRootId, Identifiers.AdministratorsGroupId, false, PermissionType.BuiltInPermissionTypes)
                    .Allow(Identifiers.PortalRootId, Identifiers.AdministratorUserId, false, PermissionType.BuiltInPermissionTypes)
                    .Apply();
            }
        }
        [ClassCleanup]
        public static void ShutDownRepository()
        {
            _repository?.Dispose();
        }

        [TestInitialize]
        public void DeleteAllAccessTokens()
        {
            AccessTokenVault.DeleteAllAccessTokens();
        }

        /* ------------------------------------------------------------------------------------------------------- */

        private void AssertTokensAreEqual(AccessToken expected, AccessToken actual)
        {
            Assert.AreNotSame(expected, actual);
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Value, actual.Value);
            Assert.AreEqual(expected.UserId, actual.UserId);
            Assert.AreEqual(expected.ContentId, actual.ContentId);
            Assert.AreEqual(expected.Feature, actual.Feature);
            Assert.AreEqual(expected.CreationDate, actual.CreationDate);
            Assert.AreEqual(expected.ExpirationDate, actual.ExpirationDate);
        }

    }
}
