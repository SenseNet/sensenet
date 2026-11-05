using System.Threading;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Services.Core.Authentication;
using SenseNet.Testing;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests;

[TestClass]
public class ODataPasswordValidationTests : ODataTestBase
{
    [DataTestMethod]
    [DataRow("ValidateCredentials")]
    [DataRow("ValidateCredentialsWithTwoFactorCode")]
    public Task CredentialFailuresDoNotRevealAccountExistence(string action) => ODataTest2Async(
        services => services.Configure<PasswordValidationOptions>(options => options.AttemptsPerMinutePerAccount = 2),
        async () =>
        {
            using var domainPolicy = new Swindler<DomainUsagePolicy>(DomainUsagePolicy.NoDomain,
                () => IdentityManagement.DomainUsagePolicy, value => IdentityManagement.DomainUsagePolicy = value);
            const string password = "Synthetic-Password-2026";
            var disabled = new User(User.Administrator.Parent)
            {
                Name = "disabled-login-test", Enabled = false, Password = password
            };
            await disabled.SaveAsync(CancellationToken.None);
            var domain = new Domain(Repository.ImsFolder) { Name = "LoginTestDomain" };
            await domain.SaveAsync(CancellationToken.None);
            foreach (var parent in new[] { domain, User.Administrator.Parent })
            {
                var user = new User(parent) { Name = "ambiguous-login-test", Enabled = true, Password = password };
                await user.SaveAsync(CancellationToken.None);
            }
            await Providers.Instance.SecurityHandler.CreateAclEditor()
                .Allow(Repository.Root.Id, User.Visitor.Id, false, PermissionType.See, PermissionType.Open)
                .ApplyAsync(CancellationToken.None);
            // The MFA action is restricted to administrators; exercise its credential check too.
            using var caller = new CurrentUserBlock(action == "ValidateCredentials" ? User.Visitor : User.Administrator);
            var path = "/OData.svc/('Root')/" + action;
            var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string> { ["ASPNETCORE_ENVIRONMENT"] = "Production" }).Build();
            string expectedBody = null;
            string expectedContentType = null;
            foreach (var name in new[] { "builtin\\admin", "builtin\\" + disabled.LoginName,
                         "builtin\\missing-login-test", "ambiguous-login-test" })
            {
                for (var attempt = 0; attempt < 4; attempt++)
                {
                    var submittedPassword = name == "builtin\\admin" ? (attempt < 2 ? "wrong" : "admin") : password;
                    var request = JsonConvert.SerializeObject(new
                    {
                        userName = attempt % 2 == 0 ? name : name.ToUpperInvariant(),
                        password = submittedPassword, twoFactorCode = "invalid-code"
                    });
                    var context = await ODataProcessRequestEmptyResponseAsync(path, "", request, "POST", config);
                    context.Response.Body.Seek(0, SeekOrigin.Begin);
                    using var reader = new StreamReader(context.Response.Body);
                    var body = await reader.ReadToEndAsync();
                    expectedBody ??= body;
                    expectedContentType ??= context.Response.ContentType;
                    Assert.AreEqual(403, context.Response.StatusCode, body);
                    StringAssert.Contains(body, "Invalid username or password.");
                    Assert.AreEqual(expectedBody, body);
                    Assert.AreEqual(expectedContentType, context.Response.ContentType);
                    Assert.IsFalse(context.Response.Headers.ContainsKey("Retry-After"));
                    Assert.AreEqual("no-store", context.Response.Headers.CacheControl.ToString());
                }
            }
        });

    [TestMethod]
    public Task AnonymousPasswordValidationIsLimitedWithoutLocalAuthentication() => ODataTestAsync(async () =>
    {
        await Providers.Instance.SecurityHandler.CreateAclEditor()
            .Allow(Repository.Root.Id, User.Visitor.Id, false, PermissionType.See, PermissionType.Open)
            .ApplyAsync(CancellationToken.None);
        using var caller = new CurrentUserBlock(User.Visitor);
        const string path = "/OData.svc/('Root')/ValidateCredentials";
        for (var i = 0; i < 5; i++)
        {
            var response = await ODataPostAsync(path, "", "{\"userName\":\"builtin\\\\admin\",\"password\":\"wrong\"}");
            Assert.AreEqual(403, response.StatusCode, response.Result);
        }

        var limited = await ODataProcessRequestEmptyResponseAsync(path, "",
            "{\"userName\":\"BuiltIn\\\\Admin\",\"password\":\"admin\"}", "POST", null);
        Assert.AreEqual(403, limited.Response.StatusCode);
        Assert.IsFalse(limited.Response.Headers.ContainsKey("Retry-After"));
        Assert.AreEqual("no-store", limited.Response.Headers.CacheControl.ToString());
    });
}
