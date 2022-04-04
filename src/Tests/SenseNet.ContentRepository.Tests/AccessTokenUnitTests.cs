using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class AccessTokenUnitTests : TestBase
    {
        [TestMethod]
        public void UT_AccessToken_Create_MinTimeOut()
        {
            Providers.Instance = new Providers(new ServiceCollection()
                .AddSingleton<IAccessTokenDataProvider, InMemoryAccessTokenDataProvider>()
                .BuildServiceProvider());

            Providers.Instance.DataProvider = new InMemoryDataProvider(); ;

            // ACTION
            var token = AccessTokenVault.CreateTokenAsync(1, TimeSpan.MinValue, CancellationToken.None).Result;

            // ASSERT
            Assert.AreEqual(DateTime.MinValue, token.ExpirationDate);
        }
        [TestMethod]
        public void UT_AccessToken_Create_MaxTimeOut()
        {
            Providers.Instance = new Providers(new ServiceCollection()
                .AddSingleton<IAccessTokenDataProvider, InMemoryAccessTokenDataProvider>()
                .BuildServiceProvider());

            Providers.Instance.DataProvider = new InMemoryDataProvider(); ;

            // ACTION
            var token = AccessTokenVault.CreateTokenAsync(1, TimeSpan.MaxValue, CancellationToken.None).Result;

            // ASSERT
            Assert.AreEqual(DateTime.MaxValue, token.ExpirationDate);
        }

        [TestMethod]
        public void UT_AccessToken_GetOrAdd_MinTimeOut()
        {
            Providers.Instance = new Providers(new ServiceCollection()
                .AddSingleton<IAccessTokenDataProvider, InMemoryAccessTokenDataProvider>()
                .BuildServiceProvider());

            Providers.Instance.DataProvider = new InMemoryDataProvider(); ;

            // ACTION
            var token = AccessTokenVault.GetOrAddToken(1, TimeSpan.MinValue);

            // ASSERT
            Assert.AreEqual(DateTime.MinValue, token.ExpirationDate);
        }
        [TestMethod]
        public void UT_AccessToken_GetOrAdd_MaxTimeOut()
        {
            Providers.Instance = new Providers(new ServiceCollection()
                .AddSingleton<IAccessTokenDataProvider, InMemoryAccessTokenDataProvider>()
                .BuildServiceProvider());

            Providers.Instance.DataProvider = new InMemoryDataProvider(); ;

            // ACTION
            var token = AccessTokenVault.GetOrAddToken(1, TimeSpan.MaxValue);

            // ASSERT
            Assert.AreEqual(DateTime.MaxValue, token.ExpirationDate);
        }
    }
}
