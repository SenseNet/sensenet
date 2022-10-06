using System;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Services.Core.Operations;
using SenseNet.Testing;
using SenseNet.Tests.Core;

namespace SenseNet.Services.Core.Tests
{
    [TestClass]
    public class LoginTests : TestBase
    {
        private const string BuiltIn = "BuiltIn";

        // TestName: Login_{policy}_{domain\?user}_{result}

        [TestMethod]
        public void Login_NoDomain_BuiltIn_Admin_Ok()
        {
            LoginTest(DomainUsagePolicy.NoDomain, BuiltIn, (services) =>
            {
                var result = ValidateCredentials("admin", "admin", services);
                Assert.AreEqual("BuiltIn\\Admin", result.Username);
            });
        }

        [TestMethod, ExpectedException(typeof(MissingDomainException))]
        public void Login_NoDomain_BuiltIn_Admin_Err_WithAdminInPublic()
        {
            LoginTest(DomainUsagePolicy.NoDomain, BuiltIn, (services) =>
            {
                CreateUserInPublicDomain("Admin", "verypublicadmin");
                var result = ValidateCredentials("admin", "verypublicadmin", services);
            });
        }

        [TestMethod]
        public void Login_NoDomain_BuiltIn_PublicAdmin_Ok_WithAdminInPublic()
        {
            LoginTest(DomainUsagePolicy.NoDomain, BuiltIn, (services) =>
            {
                CreateUserInPublicDomain("Admin", "verypublicadmin");
                var result = ValidateCredentials("public\\admin", "verypublicadmin", services);
            });
        }

        [TestMethod]
        public void Login_NoDomain_BuiltIn_BuiltInAdmin_Ok()
        {
            LoginTest(DomainUsagePolicy.NoDomain, BuiltIn, (services) =>
            {
                var result = ValidateCredentials("builtin\\admin", "admin", services);
                Assert.AreEqual("BuiltIn\\Admin", result.Username);
            });
        }

        [TestMethod, ExpectedException(typeof(SenseNetSecurityException))]
        public void Login_NoDomain_BuiltIn_Domain1Admin_Err()
        {
            LoginTest(DomainUsagePolicy.NoDomain, BuiltIn, (services) =>
            {
                var result = ValidateCredentials("domain1\\admin", "admin", services);
            });
        }

        [TestMethod]
        public void Login_NoDomain_BuiltIn_User1_Ok_WithUser1InPublic()
        {
            LoginTest(DomainUsagePolicy.NoDomain, BuiltIn, (services) =>
            {
                CreateUserInPublicDomain("User1", "user1");
                var result = ValidateCredentials("user1", "user1", services);
            });
        }

        [TestMethod]
        public void Login_NoDomain_DoubleCache()
        {
            LoginTest(DomainUsagePolicy.NoDomain, BuiltIn, (services) =>
            {
                var nodeId = CreateUserInPublicDomain("User1", "user1");

                var key1 = "user-*-user1";
                var key2 = "user-public-user1";

                Cache.Reset();

                Assert.IsNull(Cache.Get(key1));
                Assert.IsNull(Cache.Get(key2));

                ValidateCredentials("user1", "user1", services);

                Assert.IsNotNull(Cache.Get(key1));
                Assert.IsNull(Cache.Get(key2));

                ValidateCredentials("public\\user1", "user1", services);

                Assert.IsNotNull(Cache.Get(key1));
                Assert.IsNotNull(Cache.Get(key2));

                var user1 = Node.LoadNode(nodeId);
                user1.Index++;
                user1.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.IsNull(Cache.Get(key1));
                Assert.IsNull(Cache.Get(key2));
            });
        }

        [TestMethod]
        public void Login_DefaultDomain_BuiltIn_Admin_Ok()
        {
            LoginTest(DomainUsagePolicy.DefaultDomain, BuiltIn, (services) =>
            {
                var result = ValidateCredentials("admin", "admin", services);
                Assert.AreEqual("BuiltIn\\Admin", result.Username);
            });
        }

        [TestMethod]
        public void Login_DefaultDomain_BuiltIn_BuiltInAdmin_Ok()
        {
            LoginTest(DomainUsagePolicy.DefaultDomain, BuiltIn, (services) =>
            {
                var result = ValidateCredentials("builtin\\admin", "admin", services);
                Assert.AreEqual("BuiltIn\\Admin", result.Username);
            });
        }

        [TestMethod, ExpectedException(typeof(SenseNetSecurityException))]
        public void Login_DefaultDomain_BuiltIn_Domain1Admin_Err()
        {
            LoginTest(DomainUsagePolicy.DefaultDomain, BuiltIn, (services) =>
            {
                var result = ValidateCredentials("domain1\\admin", "admin", services);
            });
        }

        [TestMethod, ExpectedException(typeof(SenseNetSecurityException))]
        public void Login_DefaultDomain_Domain1_Admin_Err()
        {
            LoginTest(DomainUsagePolicy.DefaultDomain, "Domain1", (services) =>
            {
                var result = ValidateCredentials("admin", "admin", services);
            });
        }

        [TestMethod]
        public void Login_DefaultDomain_Domain1_BuiltInAdmin_Ok()
        {
            LoginTest(DomainUsagePolicy.DefaultDomain, "Domain1", (services) =>
            {
                var result = ValidateCredentials("builtin\\admin", "admin", services);
                Assert.AreEqual("BuiltIn\\Admin", result.Username);
            });
        }

        [TestMethod, ExpectedException(typeof(SenseNetSecurityException))]
        public void Login_DefaultDomain_Domain1_Domain1Admin_Err()
        {
            LoginTest(DomainUsagePolicy.DefaultDomain, "Domain1", (services) =>
            {
                var result = ValidateCredentials("domain1\\admin", "admin", services);
            });
        }

        [TestMethod, ExpectedException(typeof(MissingDomainException))]
        public void Login_MandatoryDomain_BuiltIn_Admin_Err()
        {
            LoginTest(DomainUsagePolicy.MandatoryDomain, BuiltIn, (services) =>
            {
                var result = ValidateCredentials("admin", "admin", services);
            });
        }

        [TestMethod]
        public void Login_MandatoryDomain_BuiltIn_BuiltInAdmin_Ok()
        {
            LoginTest(DomainUsagePolicy.MandatoryDomain, BuiltIn, (services) =>
            {
                var result = ValidateCredentials("builtin\\admin", "admin", services);
                Assert.AreEqual("BuiltIn\\Admin", result.Username);
            });
        }


        private int CreateUserInPublicDomain(string loginName, string password)
        {
            var user = new User(Node.LoadNode("/Root/IMS/Public"))
            {
                Name = loginName,
                LoginName = loginName,
                Password = password,
                Email = "very-public"+ loginName + "@example.com",
                Enabled = true
            };
            user.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            return user.Id;
        }
        private IdentityOperations.CredentialValidationResult ValidateCredentials(string userName, string password, IServiceProvider services)
        {
            return IdentityOperations.ValidateCredentials(
                Content.Create(Repository.Root),
                new DefaultHttpContext()
                {
                    RequestServices = services ?? new ServiceCollection().BuildServiceProvider()
                },
                userName,
                password);
        }

        private void LoginTest(DomainUsagePolicy domainUsagePolicy, string defaultDomain, Action<IServiceProvider> callback)
        {
            IServiceProvider services = null;

            Test2(sc => { sc.AddLogging(); },
                repoBuilder =>
                {
                    services = repoBuilder.Services;
                },
                () =>
            {
                if (defaultDomain != BuiltIn)
                    new Domain(Repository.ImsFolder) {Name = defaultDomain}.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();

                using (new Swindler<DomainUsagePolicy>(domainUsagePolicy,
                           () => IdentityManagement.DomainUsagePolicy,
                           (policy => IdentityManagement.DomainUsagePolicy = policy)))
                using (new Swindler<string>(defaultDomain,
                           () => IdentityManagement.DefaultDomain,
                           (domain => IdentityManagement.DefaultDomain = domain)))
                {
                    callback(services);
                }
            });
        }
    }
}
