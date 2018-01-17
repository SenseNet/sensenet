using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Moq;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.Virtualization;
using SenseNet.Services.Virtualization;
using Xunit;
using Xunit.Sdk;

namespace SenseNet.Services.Tests
{
    public class OAuthAuthenticationTests
    {
        private abstract class TestOAuthProvider : OAuthProvider
        {
            public static readonly string UserName = "user1";
            public static readonly string TestProviderName = "testprovider";

            public override string IdentifierFieldName { get; } = "TestOAuthField1";
            public override string ProviderName { get; } = TestProviderName;
            
            public override IOAuthIdentity GetUserData(object tokenData)
            {
                return new OAuthIdentity
                {
                    Username = "user1",
                    Identifier = "user1"
                };
            }
        }

        private class OAuthProviderValid : TestOAuthProvider
        {
            public override string VerifyToken(HttpRequestBase request, out object tokenData)
            {
                tokenData = null;
                return UserName;
            }
        }
        private class OAuthProviderInvalidUser : TestOAuthProvider
        {
            public override string VerifyToken(HttpRequestBase request, out object tokenData)
            {
                tokenData = null;
                return null;
            }
        }
        private class OAuthProviderError : TestOAuthProvider
        {
            public override string VerifyToken(HttpRequestBase request, out object tokenData)
            {
                throw new InvalidOperationException();
            }
        }

        private class TestUser : IUser
        {
            public static TestUser Create(OAuthProvider provider, object tokenData, string userId)
            {
                return new TestUser
                {
                    Username = userId
                };
            }

            public int Id { get; }
            public string Path { get; }
            public bool IsInGroup(int securityGroupId)
            {
                throw new NotImplementedException();
            }

            public string Name { get; }
            public string AuthenticationType { get; }
            public bool IsAuthenticated { get; }
            public IEnumerable<int> GetDynamicGroups(int entityId)
            {
                throw new NotImplementedException();
            }

            public bool Enabled { get; set; }
            public string Domain { get; }
            public string Email { get; set; }
            public string FullName { get; set; }
            public string Password { get; set; }
            public string PasswordHash { get; set; }
            public string Username { get; set; }
            public bool IsInGroup(IGroup @group)
            {
                throw new NotImplementedException();
            }

            public bool IsInOrganizationalUnit(IOrganizationalUnit orgUnit)
            {
                throw new NotImplementedException();
            }

            public bool IsInContainer(ISecurityContainer container)
            {
                throw new NotImplementedException();
            }

            public DateTime? LastLoggedOut { get; set; }

            public MembershipExtension MembershipExtension { get; set; }
        }

        [Fact]
        public void OAuth_ValidUser()
        {
            var oauth = new OAuthManager
            {
                GetProvider = s => new OAuthProviderValid(),
                LoadOrCreateUser = TestUser.Create
            };

            var user = oauth.VerifyUser(TestOAuthProvider.TestProviderName, null);

            Assert.Equal(TestOAuthProvider.UserName, user.Username);
        }
        [Fact]
        public void OAuth_NotVerifiedUser()
        {
            var oauth = new OAuthManager
            {
                GetProvider = s => new OAuthProviderInvalidUser(),
                LoadOrCreateUser = TestUser.Create
            };

            // this must return null (we use the OAuthProviderInvalidUser), even if the LoadOrCreateUser method is defined above
            var user = oauth.VerifyUser(TestOAuthProvider.TestProviderName, null);

            Assert.Null(user);
        }
        [Fact]
        public void OAuth_ProviderError()
        {
            var oauth = new OAuthManager
            {
                GetProvider = s => new OAuthProviderError(),
                LoadOrCreateUser = TestUser.Create
            };

            // this must return null (we use the OAuthProviderError), even if the LoadOrCreateUser method is defined above
            var user = oauth.VerifyUser(TestOAuthProvider.TestProviderName, null);

            Assert.Null(user);
        }
        [Fact]
        public void OAuth_ProviderNotFound()
        {
            var oauth = new OAuthManager
            {
                GetProvider = s => null,
                LoadOrCreateUser = TestUser.Create
            };

            // this must throw an exception, because the provider is null (cannot be found)
            Assert.Throws<InvalidOperationException>(() => oauth.VerifyUser(TestOAuthProvider.TestProviderName, null));
        }
    }
}
