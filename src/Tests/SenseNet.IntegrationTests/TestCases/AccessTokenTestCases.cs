using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.IntegrationTests.Infrastructure;

namespace SenseNet.IntegrationTests.TestCases
{
    public class AccessTokenTestCases : TestCaseBase
    {
        public async Task AccessToken_Create_ForUser()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                var userId = 42;
                var timeout = TimeSpan.FromMinutes(10);

                // ACTION
                var token = await AccessTokenVault.CreateTokenAsync(userId, timeout, CancellationToken.None);

                // ASSERT
                Assert.IsTrue(token.Id > 0);
                Assert.IsNotNull(token.Value);
                Assert.AreEqual(userId, token.UserId);
                Assert.AreEqual(0, token.ContentId);
                Assert.IsNull(token.Feature);
                Assert.IsTrue((DateTime.UtcNow - token.CreationDate).TotalMilliseconds < 1000);
                Assert.IsTrue((token.ExpirationDate - DateTime.UtcNow - timeout).TotalMilliseconds < 1000);
            });
        }
        public async Task AccessToken_Create_ForUser_ValueLength()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                var userId = 42;
                var timeout = TimeSpan.FromMinutes(10);

                // ACTION
                var token = await AccessTokenVault.CreateTokenAsync(userId, timeout, CancellationToken.None);

                // ASSERT
                Assert.IsTrue(token.Value.Length >= 50);
            });
        }
        public async Task AccessToken_Create_ForUser_Twice()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                var userId = 42;
                var timeout = TimeSpan.FromMinutes(10);

                // ACTION
                var token1 = await AccessTokenVault.CreateTokenAsync(userId, timeout, CancellationToken.None);
                var token2 = await AccessTokenVault.CreateTokenAsync(userId, timeout, CancellationToken.None);

                // ASSERT
                Assert.AreNotEqual(token1.Id, token2.Id);
                Assert.AreNotEqual(token1.Value, token2.Value);
            });
        }
        public async Task AccessToken_Create_ForUserAndContent()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                var userId = 42;
                var contentId = 142;
                var timeout = TimeSpan.FromMinutes(10);

                // ACTION
                var token = await AccessTokenVault.CreateTokenAsync(userId, timeout, contentId, CancellationToken.None);

                // ASSERT
                Assert.IsTrue(token.Id > 0);
                Assert.IsNotNull(token.Value);
                Assert.AreEqual(userId, token.UserId);
                Assert.AreEqual(contentId, token.ContentId);
                Assert.IsNull(token.Feature);
                Assert.IsTrue((DateTime.UtcNow - token.CreationDate).TotalMilliseconds < 1000);
                Assert.IsTrue((token.ExpirationDate - DateTime.UtcNow - timeout).TotalMilliseconds < 1000);
            });
        }
        public async Task AccessToken_Create_ForUserAndFeature()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                var userId = 42;
                var feature = "Feature1";
                var timeout = TimeSpan.FromMinutes(10);

                // ACTION
                var token = await AccessTokenVault.CreateTokenAsync(userId, timeout, 0, feature, CancellationToken.None);

                // ASSERT
                Assert.IsTrue(token.Id > 0);
                Assert.IsNotNull(token.Value);
                Assert.AreEqual(userId, token.UserId);
                Assert.AreEqual(0, token.ContentId);
                Assert.AreEqual(feature, token.Feature);
                Assert.IsTrue((DateTime.UtcNow - token.CreationDate).TotalMilliseconds < 1000);
                Assert.IsTrue((token.ExpirationDate - DateTime.UtcNow - timeout).TotalMilliseconds < 1000);
            });
        }
        public async Task AccessToken_Create_ForUserContentAndFeature()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                var userId = 42;
                var contentId = 142;
                var feature = "Feature1";
                var timeout = TimeSpan.FromMinutes(10);

                // ACTION
                var token = await AccessTokenVault.CreateTokenAsync(userId, timeout, contentId, feature, CancellationToken.None);

                // ASSERT
                Assert.IsTrue(token.Id > 0);
                Assert.IsNotNull(token.Value);
                Assert.AreEqual(userId, token.UserId);
                Assert.AreEqual(contentId, token.ContentId);
                Assert.AreEqual(feature, token.Feature);
                Assert.IsTrue((DateTime.UtcNow - token.CreationDate).TotalMilliseconds < 1000);
                Assert.IsTrue((token.ExpirationDate - DateTime.UtcNow - timeout).TotalMilliseconds < 1000);
            });
        }

        public async Task AccessToken_Get_ForUser()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                var userId = 42;
                var timeout = TimeSpan.FromMinutes(10);
                var savedToken = await AccessTokenVault.CreateTokenAsync(userId, timeout, CancellationToken.None);

                // ACTION
                var token = await AccessTokenVault.GetTokenAsync(savedToken.Value, CancellationToken.None);

                // ASSERT
                AssertTokensAreEqual(savedToken, token);
            });
        }
        public async Task AccessToken_Get_ForUserAndContent()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                var userId = 42;
                var contentId = 142;
                var timeout = TimeSpan.FromMinutes(10);
                var savedToken = await AccessTokenVault.CreateTokenAsync(userId, timeout, contentId, CancellationToken.None);

                // ACTION
                var token = await AccessTokenVault.GetTokenAsync(savedToken.Value, contentId, CancellationToken.None);

                // ASSERT
                AssertTokensAreEqual(savedToken, token);
                Assert.IsNull(await AccessTokenVault.GetTokenAsync(savedToken.Value, CancellationToken.None));
            });
        }
        public async Task AccessToken_Get_ForUserAndFeature()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                var userId = 42;
                var feature = "Feature1";
                var timeout = TimeSpan.FromMinutes(10);
                var savedToken = await AccessTokenVault.CreateTokenAsync(userId, timeout, 0, feature, CancellationToken.None);

                // ACTION
                var token = await AccessTokenVault.GetTokenAsync(savedToken.Value, 0, feature, CancellationToken.None);

                // ASSERT
                AssertTokensAreEqual(savedToken, token);
                Assert.IsNull(await AccessTokenVault.GetTokenAsync(savedToken.Value, CancellationToken.None));
            });
        }
        public async Task AccessToken_Get_ForUserContentAndFeature()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                var userId = 42;
                var contentId = 142;
                var feature = "Feature1";
                var timeout = TimeSpan.FromMinutes(10);
                var savedToken = await AccessTokenVault.CreateTokenAsync(userId, timeout, contentId, feature, CancellationToken.None);

                // ACTION
                var token = await AccessTokenVault.GetTokenAsync(savedToken.Value, contentId, feature, CancellationToken.None);

                // ASSERT
                AssertTokensAreEqual(savedToken, token);
                Assert.IsNull(await AccessTokenVault.GetTokenAsync(savedToken.Value, CancellationToken.None));
                Assert.IsNull(await AccessTokenVault.GetTokenAsync(savedToken.Value, 0, feature, CancellationToken.None));
                Assert.IsNull(await AccessTokenVault.GetTokenAsync(savedToken.Value, contentId, CancellationToken.None));
            });
        }
        public async Task AccessToken_Get_Expired()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                var userId = 42;
                var timeout = TimeSpan.FromMilliseconds(1);
                var savedToken = await AccessTokenVault.CreateTokenAsync(userId, timeout, CancellationToken.None);

                // ACTION
                Thread.Sleep(10);
                var token = await AccessTokenVault.GetTokenAsync(savedToken.Value, CancellationToken.None);

                // ASSERT
                Assert.IsNull(token);
            });
        }

        public async Task AccessToken_GetByUser()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                var userId = 42;
                var contentId = 142;
                var feature = "Feature1";
                var timeout = TimeSpan.FromMinutes(10);
                var shortTimeout = TimeSpan.FromSeconds(1);
                var savedTokens = new[]
                {
                    await AccessTokenVault.CreateTokenAsync(userId, timeout, CancellationToken.None),
                    await AccessTokenVault.CreateTokenAsync(userId, timeout, contentId, CancellationToken.None),
                    await AccessTokenVault.CreateTokenAsync(userId, timeout, 0, feature, CancellationToken.None),
                    await AccessTokenVault.CreateTokenAsync(userId, timeout, contentId, feature, CancellationToken.None),
                    await AccessTokenVault.CreateTokenAsync(userId, shortTimeout, CancellationToken.None),
                    await AccessTokenVault.CreateTokenAsync(userId, shortTimeout, contentId, CancellationToken.None),
                    await AccessTokenVault.CreateTokenAsync(userId, shortTimeout, 0, feature, CancellationToken.None),
                    await AccessTokenVault.CreateTokenAsync(userId, shortTimeout, contentId, feature, CancellationToken.None),
                };

                // ACTION-1
                var tokens = await AccessTokenVault.GetAllTokensAsync(userId, CancellationToken.None);

                // ASSERT-1
                Assert.AreEqual(
                    string.Join(",", savedTokens.OrderBy(x => x.Id).Select(x => x.Id.ToString())),
                    string.Join(",", tokens.OrderBy(x => x.Id).Select(x => x.Id.ToString())));

                // ACTION-2
                Thread.Sleep(1100);
                tokens = await AccessTokenVault.GetAllTokensAsync(userId, CancellationToken.None);

                // ASSERT-2
                // The last 4 tokens are expired
                Assert.AreEqual(
                    string.Join(",", savedTokens.Take(4).OrderBy(x => x.Id).Select(x => x.Id.ToString())),
                    string.Join(",", tokens.OrderBy(x => x.Id).Select(x => x.Id.ToString())));
            });
        }

        public async Task AccessToken_GetOrAdd_WithFeature()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessToken_GetOrAdd(42, 142, "testfeature");
            });
        }
        public async Task AccessToken_GetOrAdd_WithContent()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessToken_GetOrAdd(42, 142);
            });
        }
        public async Task AccessToken_GetOrAdd_WithUser()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessToken_GetOrAdd(42);
            });
        }
        private async Task AccessToken_GetOrAdd(int userId, int contentId = 0, string feature = null)
        {
            var timeout1 = TimeSpan.FromMinutes(3);
            var timeout2 = TimeSpan.FromMinutes(10);
            var timeout3 = TimeSpan.FromMinutes(20);

            // create three different tokens
            var savedToken1 = await AccessTokenVault.CreateTokenAsync(userId, timeout1, contentId, feature,
                CancellationToken.None).ConfigureAwait(false);
            var savedToken2 = await AccessTokenVault.CreateTokenAsync(userId, timeout2, contentId, feature,
                CancellationToken.None).ConfigureAwait(false);
            var savedToken3 = await AccessTokenVault.CreateTokenAsync(userId, timeout3, contentId, feature,
                CancellationToken.None).ConfigureAwait(false);

            // ACTION: get a token with the same parameters
            var token = await AccessTokenVault.GetOrAddTokenAsync(userId, timeout3, contentId, feature,
                CancellationToken.None).ConfigureAwait(false);

            // ASSERT: we should get the last one
            AssertTokensAreEqual(savedToken3, token);

            // ACTION: get a token with shorter expiration time
            token = await AccessTokenVault.GetOrAddTokenAsync(userId, timeout2, contentId, feature,
                CancellationToken.None).ConfigureAwait(false);

            // ASSERT: we should get the previous one
            AssertTokensAreEqual(savedToken2, token);

            // ACTION: get a token with an even shorter expiration time
            token = await AccessTokenVault.GetOrAddTokenAsync(userId, TimeSpan.FromMinutes(7), contentId, feature,
                CancellationToken.None).ConfigureAwait(false);

            // ASSERT: we should get a totally new one, because the first 
            // token (savedToken1) expires too soon. 
            Assert.AreNotEqual(savedToken1.Value, token.Value);
            Assert.AreNotEqual(savedToken2.Value, token.Value);
            Assert.AreNotEqual(savedToken3.Value, token.Value);
            Assert.IsTrue(token.ExpirationDate < savedToken2.ExpirationDate);
        }

        public async Task AccessToken_Exists()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                var userId = 42;
                var timeout = TimeSpan.FromMinutes(10);
                var savedToken = await AccessTokenVault.CreateTokenAsync(userId, timeout, CancellationToken.None);

                // ACTION
                var isExists = await AccessTokenVault.TokenExistsAsync(savedToken.Value, CancellationToken.None);

                // ASSERT
                Assert.IsTrue(isExists);
            });
        }
        public async Task AccessToken_Exists_Missing()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);

                // ACTION
                var isExists = await AccessTokenVault.TokenExistsAsync("asdf", CancellationToken.None);

                // ASSERT
                Assert.IsFalse(isExists);
            });
        }
        public async Task AccessToken_Exists_Expired()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                var userId = 42;
                var timeout = TimeSpan.FromMilliseconds(1);
                var savedToken = await AccessTokenVault.CreateTokenAsync(userId, timeout, CancellationToken.None);

                // ACTION
                Thread.Sleep(1100);
                var isExists = await AccessTokenVault.TokenExistsAsync(savedToken.Value, CancellationToken.None);

                // ASSERT
                Assert.IsFalse(isExists);
            });
        }

        public async Task AccessToken_AssertExists()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                var userId = 42;
                var timeout = TimeSpan.FromMinutes(10);
                var savedToken = await AccessTokenVault.CreateTokenAsync(userId, timeout, CancellationToken.None);

                // ACTION
                AccessTokenVault.AssertTokenExists(savedToken.Value);

                //Assert.AllRight() :)
            });
        }
        public async Task AccessToken_AssertExists_Missing()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                try
                {
                    await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                    await AccessTokenVault.AssertTokenExistsAsync("asdf", CancellationToken.None);

                    // ASSERT
                    Assert.Fail("Expected InvalidAccessTokenException was not thrown.");
                }
                catch (InvalidAccessTokenException)
                {
                    // do nothing
                }
            });
        }
        public async Task AccessToken_AssertExists_Expired()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                try
                {
                    await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                    var userId = 42;
                    var timeout = TimeSpan.FromMilliseconds(1);
                    var savedToken = await AccessTokenVault.CreateTokenAsync(userId, timeout, CancellationToken.None);

                    // ACTION
                    Thread.Sleep(1100);
                    await AccessTokenVault.AssertTokenExistsAsync(savedToken.Value, CancellationToken.None);

                    // ASSERT
                    Assert.Fail("Expected InvalidAccessTokenException was not thrown.");
                }
                catch (InvalidAccessTokenException)
                {
                    // do nothing
                }
            });
        }

        public async Task AccessToken_Update()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                var userId = 42;
                var timeout = TimeSpan.FromMinutes(10.0d);
                var savedToken = await AccessTokenVault.CreateTokenAsync(userId, timeout, CancellationToken.None);
                Assert.IsTrue(savedToken.ExpirationDate < DateTime.UtcNow.AddMinutes(20.0d));

                // ACTION
                await AccessTokenVault.UpdateTokenAsync(savedToken.Value, DateTime.UtcNow.AddMinutes(30.0d), CancellationToken.None);

                // ASSERT
                var loadedToken = await AccessTokenVault.GetTokenAsync(savedToken.Value, CancellationToken.None);
                Assert.IsNotNull(loadedToken);
                Assert.IsTrue(loadedToken.ExpirationDate > DateTime.UtcNow.AddMinutes(20.0d));
            });
        }
        public async Task AccessToken_UpdateMissing()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                try
                {
                    await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                    await AccessTokenVault.UpdateTokenAsync("asdf", DateTime.UtcNow.AddMinutes(30.0d), CancellationToken.None);

                    Assert.Fail("Expected InvalidAccessTokenException was not thrown.");
                }
                catch (InvalidAccessTokenException)
                {
                    // do nothing
                }
            });
        }
        public async Task AccessToken_UpdateExpired()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                try
                {
                    await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                    var userId = 42;
                    var timeout = TimeSpan.FromMilliseconds(1);
                    var savedToken = await AccessTokenVault.CreateTokenAsync(userId, timeout, CancellationToken.None);

                    // ACTION
                    Thread.Sleep(1100);
                    await AccessTokenVault.UpdateTokenAsync(savedToken.Value, DateTime.UtcNow.AddMinutes(30.0d), CancellationToken.None);

                    // ASSERT
                    Assert.Fail("Expected InvalidAccessTokenException was not thrown.");
                }
                catch (InvalidAccessTokenException)
                {
                    // do nothing
                }
            });
        }

        public async Task AccessToken_Delete_Token()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                var userId1 = 42;
                var userId2 = 43;
                var timeout = TimeSpan.FromMinutes(10);
                var shortTimeout = TimeSpan.FromSeconds(1);
                var savedTokens = new[]
                {
                    await AccessTokenVault.CreateTokenAsync(userId1, timeout, CancellationToken.None),
                    await AccessTokenVault.CreateTokenAsync(userId1, shortTimeout, CancellationToken.None),
                    await AccessTokenVault.CreateTokenAsync(userId2, timeout, CancellationToken.None),
                    await AccessTokenVault.CreateTokenAsync(userId2, shortTimeout, CancellationToken.None),
                };

                // ACTION
                Thread.Sleep(1100);
                await AccessTokenVault.DeleteTokenAsync(savedTokens[0].Value, CancellationToken.None);
                await AccessTokenVault.DeleteTokenAsync(savedTokens[3].Value, CancellationToken.None);

                // ASSERT
                Assert.IsNull(await AccessTokenVault.GetTokenByIdAsync(savedTokens[0].Id, CancellationToken.None));
                Assert.IsNotNull(await AccessTokenVault.GetTokenByIdAsync(savedTokens[1].Id, CancellationToken.None));
                Assert.IsNotNull(await AccessTokenVault.GetTokenByIdAsync(savedTokens[2].Id, CancellationToken.None));
                Assert.IsNull(await AccessTokenVault.GetTokenByIdAsync(savedTokens[3].Id, CancellationToken.None));
            });
        }
        public async Task AccessToken_Delete_ByUser()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                var userId1 = 42;
                var userId2 = 43;
                var timeout = TimeSpan.FromMinutes(10);
                var shortTimeout = TimeSpan.FromSeconds(1);
                var savedTokens = new[]
                {
                    await AccessTokenVault.CreateTokenAsync(userId1, timeout, CancellationToken.None),
                    await AccessTokenVault.CreateTokenAsync(userId1, shortTimeout, CancellationToken.None),
                    await AccessTokenVault.CreateTokenAsync(userId2, timeout, CancellationToken.None),
                    await AccessTokenVault.CreateTokenAsync(userId2, shortTimeout, CancellationToken.None),
                };

                // ACTION
                Thread.Sleep(1100);
                await AccessTokenVault.DeleteTokensByUserAsync(userId1, CancellationToken.None);

                // ASSERT
                Assert.IsNull(await AccessTokenVault.GetTokenByIdAsync(savedTokens[0].Id, CancellationToken.None));
                Assert.IsNull(await AccessTokenVault.GetTokenByIdAsync(savedTokens[1].Id, CancellationToken.None));
                Assert.IsNotNull(await AccessTokenVault.GetTokenByIdAsync(savedTokens[2].Id, CancellationToken.None));
                Assert.IsNotNull(await AccessTokenVault.GetTokenByIdAsync(savedTokens[3].Id, CancellationToken.None));
            });
        }
        public async Task AccessToken_Delete_ByContent()
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                var userId1 = 42;
                var userId2 = 43;
                var contentId1 = 142;
                var contentId2 = 143;
                var timeout = TimeSpan.FromMinutes(10);
                var shortTimeout = TimeSpan.FromSeconds(1);
                var savedTokens = new[]
                {
                    await AccessTokenVault.CreateTokenAsync(userId1, timeout, contentId1, CancellationToken.None),
                    await AccessTokenVault.CreateTokenAsync(userId1, shortTimeout, contentId2, CancellationToken.None),
                    await AccessTokenVault.CreateTokenAsync(userId2, timeout, contentId1, CancellationToken.None),
                    await AccessTokenVault.CreateTokenAsync(userId2, shortTimeout, contentId2, CancellationToken.None),
                };

                // ACTION
                Thread.Sleep(1100);
                await AccessTokenVault.DeleteTokensByContentAsync(contentId1, CancellationToken.None);

                // ASSERT
                Assert.IsNull(await AccessTokenVault.GetTokenByIdAsync(savedTokens[0].Id, CancellationToken.None));
                Assert.IsNotNull(await AccessTokenVault.GetTokenByIdAsync(savedTokens[1].Id, CancellationToken.None));
                Assert.IsNull(await AccessTokenVault.GetTokenByIdAsync(savedTokens[2].Id, CancellationToken.None));
                Assert.IsNotNull(await AccessTokenVault.GetTokenByIdAsync(savedTokens[3].Id, CancellationToken.None));
            });
        }

        /* ===================================================================================== */

        private void AssertTokensAreEqual(AccessToken expected, AccessToken actual)
        {
            Assert.AreNotSame(expected, actual);
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Value, actual.Value);
            Assert.AreEqual(expected.UserId, actual.UserId);
            Assert.AreEqual(expected.ContentId, actual.ContentId);
            Assert.AreEqual(expected.Feature, actual.Feature);
            Assert.AreEqual(expected.CreationDate.ToString("u"), actual.CreationDate.ToString("u"));
            Assert.AreEqual(expected.ExpirationDate.ToString("u"), actual.ExpirationDate.ToString("u"));
        }
    }
}
