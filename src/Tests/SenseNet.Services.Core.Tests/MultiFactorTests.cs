using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Tests.Core;
using System;
using System.Threading;
using Microsoft.AspNetCore.Http;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Services.Core.Operations;
using SenseNet.Storage.Security;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.Core.Tests
{
    internal class TestMultiFactorProvider : IMultiFactorAuthenticationProvider
    {
        public string GetApplicationName()
        {
            return "test";
        }

        public (string Url, string EntryKey) GenerateSetupCode(string appName, string userName, string key)
        {
            return ("test", "test");
        }

        public bool ValidateTwoFactorCode(string key, string codeToValidate)
        {
            return codeToValidate == "correctcode";
        }
    }

    [TestClass]
    public class MultiFactorTests : TestBase
    {
        [TestMethod, TestCategory("MultiFactor")]
        public Task MultiFactor_ValidateTwoFactorCode_FirstTime()
        {
            return MultiFactorTest(async (context) =>
            {
                Assert.IsFalse(User.Administrator.MultiFactorRegistered, "Multifactor auth is already registered.");

                var userContent = await Content.LoadAsync(Identifiers.AdministratorUserId, CancellationToken.None)
                    .ConfigureAwait(false);
                
                // result should be: admin user properties
                dynamic result = await IdentityOperations.ValidateTwoFactorCode(userContent, context, "correctcode");

                int userId = result.Id;

                Assert.AreEqual(1, userId);
                Assert.IsTrue(User.Administrator.MultiFactorRegistered, "Multifactor auth is NOT registered.");
            });
        }

        [TestMethod, TestCategory("MultiFactor")]
        public Task MultiFactor_ValidateTwoFactorCode_InvalidCode()
        {
            return MultiFactorTest(async (context) =>
            {
                var userContent = await Content.LoadAsync(Identifiers.AdministratorUserId, CancellationToken.None)
                    .ConfigureAwait(false);
                
                try
                {
                    // try it with EMPTY code
                    await IdentityOperations.ValidateTwoFactorCode(userContent, context, string.Empty);
                    Assert.Fail("Expected exception was not thrown for empty code.");
                }
                catch (ArgumentNullException ex)
                {
                    Assert.IsTrue(ex.Message.Contains("twoFactorCode"));
                }

                try
                {
                    // try it with INCORRECT code
                    await IdentityOperations.ValidateTwoFactorCode(userContent, context, "INCORRECTcode");
                    Assert.Fail("Expected exception was not thrown for invalid code.");
                }
                catch (SenseNetSecurityException ex)
                {
                    Assert.AreEqual("Invalid username or two-factor code.", ex.Message);
                }
            });
        }

        [TestMethod, TestCategory("MultiFactor")]
        public Task MultiFactor_ValidateTwoFactorCode_Disabled()
        {
            return MultiFactorTest(async (context) =>
            {
                var userContent = await Content.LoadAsync(Identifiers.AdministratorUserId, CancellationToken.None)
                    .ConfigureAwait(false);

                userContent["MultiFactorEnabled"] = false;
                await userContent.SaveAsync(SavingMode.KeepVersion, CancellationToken.None).ConfigureAwait(false);

                try
                {
                    // try it with correct code - but the feature is DISABLED
                    await IdentityOperations.ValidateTwoFactorCode(userContent, context, "correctcode");
                    Assert.Fail("Expected exception was not thrown for invalid feature flag.");
                }
                catch (InvalidOperationException ex)
                {
                    Assert.AreEqual("Multifactor authentication is not enabled for this user.", ex.Message);
                }
            });
        }

        [TestMethod, TestCategory("MultiFactor")]
        public Task MultiFactor_ValidateCredentialsWithCode()
        {
            return MultiFactorTest(async (context) =>
            {
                var rootContent = await Content.LoadAsync(Identifiers.PortalRootId, CancellationToken.None)
                    .ConfigureAwait(false);

                // result should be: admin user properties
                dynamic result = await IdentityOperations.ValidateCredentialsWithTwoFactorCode(rootContent, context, 
                    "admin", "admin", "correctcode");

                int userId = result.Id;

                Assert.AreEqual(1, userId);

                try
                {
                    // try it with INCORRECT code
                    await IdentityOperations.ValidateCredentialsWithTwoFactorCode(rootContent, context,
                        "admin", "admin", "INCORRECTcode");
                    Assert.Fail("Expected exception was not thrown for invalid code.");
                }
                catch (SenseNetSecurityException ex)
                {
                    Assert.AreEqual("Invalid username or two-factor code.", ex.Message);
                }
            });
        }

        [TestMethod, TestCategory("MultiFactor")]
        public Task MultiFactor_GetMultiFactorInfo()
        {
            return MultiFactorTest(async (context) =>
            {
                var userContent = await Content.LoadAsync(Identifiers.AdministratorUserId, CancellationToken.None)
                    .ConfigureAwait(false);

                // result should be: admin user properties
                dynamic result = IdentityOperations.GetMultiFactorAuthenticationInfo(userContent, context);
                
                Assert.AreEqual(true, result.multiFactorEnabled);
                Assert.AreEqual(false, result.multiFactorRegistered);
                Assert.AreEqual("test", result.qrCodeSetupImageUrl);
                Assert.AreEqual("test", result.manualEntryKey);

                // validate for the first time to flip the register flag
                await IdentityOperations.ValidateTwoFactorCode(userContent, context, "correctcode");

                result = IdentityOperations.GetMultiFactorAuthenticationInfo(userContent, context);

                Assert.AreEqual(true, result.multiFactorEnabled);
                Assert.AreEqual(true, result.multiFactorRegistered);
                Assert.AreEqual("test", result.qrCodeSetupImageUrl);
                Assert.AreEqual("test", result.manualEntryKey);

                // switch OFF the feature
                userContent["MultiFactorEnabled"] = false;
                await userContent.SaveAsync(SavingMode.KeepVersion, CancellationToken.None).ConfigureAwait(false);

                result = IdentityOperations.GetMultiFactorAuthenticationInfo(userContent, context);

                Assert.AreEqual(false, result.multiFactorEnabled);
                Assert.AreEqual(false, result.multiFactorRegistered);
                Assert.AreEqual(string.Empty, result.qrCodeSetupImageUrl);
                Assert.AreEqual(string.Empty, result.manualEntryKey);
            });
        }

        protected Task MultiFactorTest(Func<HttpContext, Task> callback)
        {
            IServiceProvider services = null;

            return Test2(sc =>
                {
                    sc.AddMultiFactorAuthenticationProvider<TestMultiFactorProvider>();
                },
                repoBuilder =>
                {
                    services = repoBuilder.Services;
                }, async () =>
                {
                    var context = new DefaultHttpContext { RequestServices = services };

                    //TODO: build test data
                    var admin = User.Administrator;
                    admin.MultiFactorEnabled = true;
                    await admin.SaveAsync(SavingMode.KeepVersion, CancellationToken.None).ConfigureAwait(false);

                    await callback(context);
                });
        }
    }
}
