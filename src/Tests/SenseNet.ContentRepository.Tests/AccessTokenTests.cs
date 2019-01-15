using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.Virtualization;
using SenseNet.Tests;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class AccessTokenTests : TestBase
    {
        [TestMethod]
        public void Token_Create_ForUser()
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
        public void Token_Create_ForUser_ValueLength()
        {
            var userId = 42;
            var timeout = TimeSpan.FromMinutes(10);

            // ACTION
            var token = AccessTokenVault.CreateToken(userId, timeout);

            // ASSERT
            Assert.IsTrue(token.Value.Length >= 50);
        }
        [TestMethod]
        public void Token_Create_ForUser_Twice()
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
        public void Token_Create_ForUserAndContent()
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
        public void Token_Create_ForUserAndFeature()
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
        public void Token_Create_ForUserContentAndFeature()
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
        public void Token_Get_ForUser()
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
        public void Token_Get_ForUserAndContent()
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
        public void Token_Get_ForUserAndFeature()
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
        public void Token_Get_ForUserContentAndFeature()
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
        public void Token_Get_Expired()
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
        public void Token_GetByUser()
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
            var tokens = AccessTokenVault.GetTokens(userId);

            // ASSERT-1
            Assert.AreEqual(
                string.Join(",", savedTokens.OrderBy(x => x.Id).Select(x => x.Id.ToString())),
                string.Join(",", tokens.OrderBy(x => x.Id).Select(x => x.Id.ToString())));

            // ACTION-2
            Thread.Sleep(1100);
            tokens = AccessTokenVault.GetTokens(userId);

            // ASSERT-2
            // The last 4 tokens are expired
            Assert.AreEqual(
                string.Join(",", savedTokens.Take(4).OrderBy(x => x.Id).Select(x => x.Id.ToString())),
                string.Join(",", tokens.OrderBy(x => x.Id).Select(x => x.Id.ToString())));
        }

        [TestMethod]
        public void Token_Exists()
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
        public void Token_Exists_Missing()
        {
            // ACTION
            var isExists = AccessTokenVault.TokenExists("asdf");

            // ASSERT
            Assert.IsFalse(isExists);
        }
        [TestMethod]
        public void Token_Exists_Expired()
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
        public void Token_AssertExists()
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
        public void Token_AssertExists_Missing()
        {
           AccessTokenVault.AssertTokenExists("asdf");
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidAccessTokenException))]
        public void Token_AssertExists_Expired()
        {
            var userId = 42;
            var timeout = TimeSpan.FromMilliseconds(1);
            var savedToken = AccessTokenVault.CreateToken(userId, timeout);

            // ACTION
            Thread.Sleep(1100);
            AccessTokenVault.AssertTokenExists(savedToken.Value);
        }

        [TestMethod]
        public void Token_Update()
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
        public void Token_UpdateMissing()
        {
            AccessTokenVault.UpdateToken("asdf", DateTime.UtcNow.AddMinutes(30.0d));
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidAccessTokenException))]
        public void Token_UpdateExpired()
        {
            var userId = 42;
            var timeout = TimeSpan.FromMilliseconds(1);
            var savedToken = AccessTokenVault.CreateToken(userId, timeout);

            // ACTION
            Thread.Sleep(1100);
            AccessTokenVault.UpdateToken(savedToken.Value, DateTime.UtcNow.AddMinutes(30.0d));
        }

        [TestMethod]
        public void Token_Delete_Token()
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
        public void Token_Delete_ByUser()
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
        public void Token_Delete_ByContent()
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
            DistributedApplication.Cache.Reset();
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
