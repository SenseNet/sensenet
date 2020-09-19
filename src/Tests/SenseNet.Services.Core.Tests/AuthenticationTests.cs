using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Services.Core.Authentication;
using SenseNet.Services.Core.Operations;
using SenseNet.Tests;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.Core.Tests
{
    [TestClass]
    public class AuthenticationTests : TestBase
    {
        [TestMethod]
        public Task Authentication_Validate_Disabled()
        {
            return Test(async () =>
            {
                var services = new ServiceCollection();
                services.Configure<RegistrationOptions>(options => { })
                    .AddSenseNetRegistration();

                var provider = services.BuildServiceProvider();
                var context = new DefaultHttpContext {RequestServices = provider};
                var root = Content.Create(Repository.Root);
                const string userName = "testuser123";

                var user = await IdentityOperations.CreateLocalUser(root, context, userName, userName, userName + "@example.com");

                // check if the user can log in
                dynamic result = IdentityOperations.ValidateCredentials(root, context, "public\\" + userName, userName);
                Assert.AreEqual(user.Id, result.id);

                // ACTION: disable the user
                user["Enabled"] = false;
                user.SaveSameVersion();

                var thrown = false;
                try
                {
                    // important to look for the user in the appropriate domain
                    IdentityOperations.ValidateCredentials(root, context, "public\\" + userName, userName);
                }
                catch (SenseNetSecurityException)
                {
                    thrown = true;
                }

                Assert.IsTrue(thrown, "Security exception was not thrown, a disabled user could log in.");
            });
        }
    }
}
