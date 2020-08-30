using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Tests;
using SenseNet.Services.Core.Authentication;
using SenseNet.Services.Core.Operations;

namespace SenseNet.Services.Core.Tests
{
    [TestClass]
    public class RegistrationTests : TestBase
    {
        #region Test classes
        internal class TestRegistrationProvider1 : IRegistrationProvider
        {
            public string Name => "p1";
            
            public Task<User> CreateProviderUserAsync(Content content, HttpContext context, string provider, string userId, ClaimInfo[] claims,
                CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }

            public Task<User> CreateLocalUserAsync(Content content, HttpContext context, string loginName, string password, string email,
                CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }
        }
        internal class TestRegistrationProvider2 : IRegistrationProvider
        {
            public string Name => "p2";

            public Task<User> CreateProviderUserAsync(Content content, HttpContext context, string provider, string userId, ClaimInfo[] claims,
                CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }

            public Task<User> CreateLocalUserAsync(Content content, HttpContext context, string loginName, string password, string email,
                CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }
        }
        #endregion

        [TestMethod]
        public void Registration_AddProvider()
        {
            var services = new ServiceCollection();

            services.AddSenseNetRegistration()
                .AddProvider<TestRegistrationProvider1>()
                .AddProvider<TestRegistrationProvider2>();

            var serviceProvider = services.BuildServiceProvider();
            var store = serviceProvider.GetRequiredService<RegistrationProviderStore>();

            var p1 = store.Get("p1");
            var p2 = store.Get("p2");
            var pDefault = store.Get("p3");

            Assert.AreEqual("p1", p1.Name);
            Assert.AreEqual("p2", p2.Name);
            Assert.AreEqual("default", pDefault.Name);
            Assert.IsTrue(pDefault is DefaultRegistrationProvider);
        }
    }
}
