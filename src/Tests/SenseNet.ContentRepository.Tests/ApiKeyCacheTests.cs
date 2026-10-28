using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Communication.Messaging;
using SenseNet.Configuration;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Security.ApiKeys;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Storage.Security;
using SenseNet.Tests.Core;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ApiKeyCacheTests : TestBase
    {
        private const string HealthCheckerApiKey = "test-health-checker-key";

        [DataTestMethod, TestCategory("ApiKey")]
        [DataRow("ManagerDelete")]
        [DataRow("ManagerDeleteAll")]
        [DataRow("Delete")]
        [DataRow("DeleteSync")]
        [DataRow("User")]
        [DataRow("UserSync")]
        [DataRow("Feature")]
        [DataRow("Filter")]
        [DataRow("All")]
        [DataRow("AllSync")]
        public STT.Task ApiKey_CachedTokenIsInvalidatedAfterDeletion(string operation)
        {
            return CacheTest(async (manager, storage, key) =>
            {
                Assert.IsNotNull(await manager.GetUserByApiKeyAsync(key.Value, CancellationToken.None));

                switch (operation)
                {
                    case "ManagerDelete":
                        await manager.DeleteApiKeyAsync(key.Value, CancellationToken.None);
                        break;
                    case "ManagerDeleteAll":
                        await manager.DeleteApiKeysAsync(CancellationToken.None);
                        break;
                    case "Delete":
                        await AccessTokenVault.DeleteTokenAsync(key.Value, CancellationToken.None);
                        break;
                    case "DeleteSync":
                        AccessTokenVault.DeleteToken(key.Value);
                        break;
                    case "User":
                        await AccessTokenVault.DeleteTokensByUserAsync(Identifiers.AdministratorUserId, CancellationToken.None);
                        break;
                    case "UserSync":
                        AccessTokenVault.DeleteTokensByUser(Identifiers.AdministratorUserId);
                        break;
                    case "Feature":
                        await AccessTokenVault.DeleteTokensByFeatureAsync("apikey", CancellationToken.None);
                        break;
                    case "Filter":
                        await AccessTokenVault.DeleteTokensAsync(Identifiers.AdministratorUserId, 0, null, CancellationToken.None);
                        break;
                    case "All":
                        await AccessTokenVault.DeleteAllAccessTokensAsync(CancellationToken.None);
                        break;
                    case "AllSync":
                        AccessTokenVault.DeleteAllAccessTokens();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(operation));
                }

                Assert.IsNull(await manager.GetUserByApiKeyAsync(key.Value, CancellationToken.None));
            });
        }

        [DataTestMethod, TestCategory("ApiKey")]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public STT.Task ApiKey_CachedTokenIsInvalidatedAfterExpirationChange(bool extend, bool synchronous)
        {
            return CacheTest(async (manager, storage, key) =>
            {
                Assert.IsNotNull(await manager.GetUserByApiKeyAsync(key.Value, CancellationToken.None));
                var loads = storage.LoadCount;
                Assert.IsNotNull(await manager.GetUserByApiKeyAsync(key.Value, CancellationToken.None));
                Assert.AreEqual(loads, storage.LoadCount, "The second lookup should use the cache.");

                var expiration = extend ? key.ExpirationDate.AddDays(1) : DateTime.UtcNow.AddMinutes(-1);
                if (synchronous)
                    AccessTokenVault.UpdateToken(key.Value, expiration);
                else
                    await AccessTokenVault.UpdateTokenAsync(key.Value, expiration, CancellationToken.None);

                var user = await manager.GetUserByApiKeyAsync(key.Value, CancellationToken.None);
                Assert.AreEqual(loads + 1, storage.LoadCount, "The changed token must be reloaded.");
                if (extend)
                {
                    Assert.IsNotNull(user);
                    Assert.AreEqual(expiration, storage.LastLoadedToken.ExpirationDate);
                }
                else
                {
                    Assert.IsNull(user);
                }
            });
        }

        [DataTestMethod, TestCategory("ApiKey")]
        [DataRow(false)]
        [DataRow(true)]
        public STT.Task ApiKey_InFlightReadIsRetriedAfterInvalidation(bool update)
        {
            return CacheTest(async (manager, storage, key) =>
            {
                storage.PauseNextRead = true;
                var loads = storage.LoadCount;
                var lookup = manager.GetUserByApiKeyAsync(key.Value, CancellationToken.None);
                try
                {
                    await storage.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
                    if (update)
                        await AccessTokenVault.UpdateTokenAsync(key.Value, DateTime.UtcNow.AddMinutes(-1), CancellationToken.None);
                    else
                        await AccessTokenVault.DeleteTokenAsync(key.Value, CancellationToken.None);
                }
                finally
                {
                    storage.ReleaseRead.TrySetResult(true);
                    await lookup.WaitAsync(TimeSpan.FromSeconds(10));
                }

                Assert.IsNull(await lookup, "The result read before invalidation must not authenticate the user.");
                Assert.AreEqual(loads + 2, storage.LoadCount, "The interrupted lookup must read storage again.");
                Assert.IsNull(await manager.GetUserByApiKeyAsync(key.Value, CancellationToken.None));
            });
        }

        [TestMethod, TestCategory("ApiKey")]
        public STT.Task ApiKey_RemoteInvalidationRetriesInFlightRead()
        {
            return CacheTest(async (manager, storage, key) =>
            {
                storage.PauseNextRead = true;
                var lookup = manager.GetUserByApiKeyAsync(key.Value, CancellationToken.None);
                try
                {
                    await storage.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
                    // Simulate a database change and the invalidation message from another instance.
                    await storage.DeleteAccessTokenAsync(key.Value, CancellationToken.None);
                    await new ApiKeyManager.ApiKeyManagerCacheResetDistributedAction()
                        .DoActionAsync(onRemote: true, isFromMe: false, CancellationToken.None);
                }
                finally
                {
                    storage.ReleaseRead.TrySetResult(true);
                    await lookup.WaitAsync(TimeSpan.FromSeconds(10));
                }

                Assert.IsNull(await lookup);
                Assert.IsNull(await manager.GetUserByApiKeyAsync(key.Value, CancellationToken.None));
            });
        }

        [TestMethod, TestCategory("ApiKey")]
        public STT.Task ApiKey_HealthCheckerSurvivesCacheInvalidation()
        {
            return CacheTest(async (manager, storage, key) =>
            {
                Assert.AreSame(HealthCheckerUser.Instance,
                    await manager.GetUserByApiKeyAsync(HealthCheckerApiKey, CancellationToken.None));

                await manager.DeleteApiKeysAsync(CancellationToken.None);

                Assert.AreSame(HealthCheckerUser.Instance,
                    await manager.GetUserByApiKeyAsync(HealthCheckerApiKey, CancellationToken.None));
            });
        }

        [TestMethod, TestCategory("ApiKey")]
        public STT.Task ApiKey_InvalidationOutlivesRequestCancellation()
        {
            return CacheTest(async (manager, storage, key) =>
            {
                Assert.IsNotNull(await manager.GetUserByApiKeyAsync(key.Value, CancellationToken.None));
                using var cancellation = new CancellationTokenSource();
                storage.CancelAfterDeletion = cancellation;

                await AccessTokenVault.DeleteTokenAsync(key.Value, cancellation.Token);

                Assert.IsTrue(cancellation.IsCancellationRequested);
                var channel = (RecordingClusterChannel)Providers.Instance.Services.GetRequiredService<IClusterChannel>();
                Assert.AreEqual(CancellationToken.None, channel.LastInvalidationCancellation);
                Assert.IsNull(await manager.GetUserByApiKeyAsync(key.Value, CancellationToken.None));
            });
        }

        [TestMethod, TestCategory("ApiKey")]
        public STT.Task ApiKey_FailedUpdateDoesNotInvalidateCache()
        {
            return CacheTest(async (manager, storage, key) =>
            {
                Assert.IsNotNull(await manager.GetUserByApiKeyAsync(key.Value, CancellationToken.None));
                var loads = storage.LoadCount;

                await Assert.ThrowsExceptionAsync<InvalidAccessTokenException>(() =>
                    AccessTokenVault.UpdateTokenAsync("missing-token", DateTime.UtcNow.AddHours(1), CancellationToken.None));

                Assert.IsNotNull(await manager.GetUserByApiKeyAsync(key.Value, CancellationToken.None));
                Assert.AreEqual(loads, storage.LoadCount);
            });
        }

        private STT.Task CacheTest(Func<IApiKeyManager, ControlledAccessTokenDataProvider, ApiKey, STT.Task> callback)
        {
            var storage = new ControlledAccessTokenDataProvider();
            return Test2(services =>
                {
                    services.AddSingleton<IAccessTokenDataProvider>(storage);
                    services.AddSingleton<IClusterChannel, RecordingClusterChannel>();
                    services.Configure<ApiKeysOptions>(options => options.HealthCheckerUser = HealthCheckerApiKey);
                },
                async () =>
                {
                    var manager = Providers.Instance.Services.GetRequiredService<IApiKeyManager>();
                    var key = await manager.CreateApiKeyAsync(Identifiers.AdministratorUserId,
                        DateTime.UtcNow.AddHours(1), CancellationToken.None);
                    await callback(manager, storage, key);
                });
        }

        private class ControlledAccessTokenDataProvider : InMemoryAccessTokenDataProvider, IAccessTokenDataProvider
        {
            public bool PauseNextRead { get; set; }
            public int LoadCount { get; private set; }
            public AccessToken LastLoadedToken { get; private set; }
            public CancellationTokenSource CancelAfterDeletion { get; set; }
            public STT.TaskCompletionSource<bool> ReadStarted { get; } = new(STT.TaskCreationOptions.RunContinuationsAsynchronously);
            public STT.TaskCompletionSource<bool> ReleaseRead { get; } = new(STT.TaskCreationOptions.RunContinuationsAsynchronously);

            public new async STT.Task<AccessToken> LoadAccessTokenAsync(string tokenValue, int contentId, string feature,
                CancellationToken cancellationToken)
            {
                var pause = PauseNextRead;
                PauseNextRead = false;
                LoadCount++;
                var token = await base.LoadAccessTokenAsync(tokenValue, contentId, feature, cancellationToken);
                LastLoadedToken = token;
                if (pause)
                {
                    ReadStarted.TrySetResult(true);
                    await ReleaseRead.Task.WaitAsync(cancellationToken);
                }
                return token;
            }

            public new async STT.Task DeleteAccessTokenAsync(string tokenValue, CancellationToken cancellationToken)
            {
                await base.DeleteAccessTokenAsync(tokenValue, cancellationToken);
                CancelAfterDeletion?.Cancel();
            }
        }

        private class RecordingClusterChannel : VoidChannel
        {
            public CancellationToken? LastInvalidationCancellation { get; private set; }

            public RecordingClusterChannel(IClusterMessageFormatter formatter, ClusterMemberInfo clusterMemberInfo)
                : base(formatter, clusterMemberInfo)
            {
            }

            public override STT.Task SendAsync(ClusterMessage message, CancellationToken cancellationToken)
            {
                if (message is ApiKeyManager.ApiKeyManagerCacheResetDistributedAction)
                    LastInvalidationCancellation = cancellationToken;

                return STT.Task.CompletedTask;
            }
        }
    }
}
