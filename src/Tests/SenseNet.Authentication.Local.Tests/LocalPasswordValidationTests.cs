using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.OData;
using SenseNet.Testing;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Authentication.Local.Tests;

public partial class LocalAuthenticationTests
{
    [DataTestMethod]
    [DataRow(false, null)]
    [DataRow(true, null)]
    [DataRow(true, "invalid-code")]
    public Task LoginFailuresDoNotRevealAccountExistence(bool challenge, string? code) => Test(async () =>
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
        using var rsa = RSA.Create(2048);
        using var server = CreateServer(new RsaSecurityKey(rsa) { KeyId = "test" },
            options => options.AttemptsPerMinutePerAccount = 2);
        using var client = Client(server);
        string? expectedBody = null;
        string? expectedContentType = null;
        foreach (var name in new[] { "builtin\\admin", "builtin\\" + disabled.LoginName,
                     "builtin\\missing-login-test", "ambiguous-login-test" })
        {
            for (var attempt = 0; attempt < 4; attempt++)
            {
                // After two failures, even the correct admin password must be rejected generically.
                var submittedPassword = name == "builtin\\admin" ? (attempt < 2 ? "wrong" : "admin") : password;
                using var response = await client.PostAsJsonAsync("/authentication/local/login", new
                {
                    username = attempt % 2 == 0 ? name : name.ToUpperInvariant(),
                    password = submittedPassword, requestMfaChallenge = challenge, twoFactorCode = code
                });
                var body = await response.Content.ReadAsStringAsync();
                expectedBody ??= body;
                expectedContentType ??= response.Content.Headers.ContentType?.ToString();
                Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, body);
                Assert.AreEqual(expectedBody, body);
                Assert.AreEqual(expectedContentType, response.Content.Headers.ContentType?.ToString());
                Assert.IsNull(response.Headers.RetryAfter);
                Assert.IsTrue(response.Headers.CacheControl?.NoStore == true);
            }
        }
    });

    [TestMethod]
    public Task IpLimitStillReturns429IndependentlyOfUserName() => Test(async () =>
    {
        using var rsa = RSA.Create(2048);
        using var server = CreateServer(new RsaSecurityKey(rsa) { KeyId = "test" },
            options => options.AttemptsPerMinutePerIp = 2);
        using var client = Client(server);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Login(client, "wrong")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/authentication/local/login",
            new { username = "builtin\\missing-login-test", password = "wrong" })).StatusCode);
        foreach (var name in new[] { "builtin\\admin", "builtin\\missing-login-test" })
        {
            using var response = await client.PostAsJsonAsync("/authentication/local/login",
                new { username = name, password = "wrong" });
            Assert.AreEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
            Assert.IsTrue(response.Headers.RetryAfter?.Delta is { TotalSeconds: > 0 and <= 60 });
        }
    });

    [DataTestMethod]
    [DataRow(false, null, false, true)]
    [DataRow(true, null, false, true)]
    [DataRow(false, null, false, false)]
    [DataRow(true, null, false, false)]
    [DataRow(false, null, true, true)]
    [DataRow(false, null, true, false)]
    [DataRow(false, "provided-code", true, true)]
    [DataRow(false, "provided-code", true, false)]
    [DataRow(true, "provided-code", true, true)]
    [DataRow(true, "provided-code", true, false)]
    public Task ODataAndLocalLoginSharePasswordBudget(bool challenge, string? code, bool customValidator,
        bool odataFirst) => Test2(services => services.AddSenseNetOData(), async () =>
    {
        await Providers.Instance.SecurityHandler.CreateAclEditor()
            .Allow(Repository.Root.Id, User.Visitor.Id, false, PermissionType.See, PermissionType.Open)
            .ApplyAsync(CancellationToken.None);
        using var rsa = RSA.Create(2048);
        var customUsers = customValidator ? new CountingPasswordUsers() : null;
        using var server = CreateServer(new RsaSecurityKey(rsa) { KeyId = "test" },
            options => options.AttemptsPerMinutePerAccount = 2,
            services: services =>
            {
                services.AddSenseNetOData();
                if (customUsers != null)
                    services.AddSingleton<ILocalAuthenticationUsers>(customUsers);
            },
            configurePipeline: app =>
            {
                var odata = new ODataMiddleware(null, null, null, NullLogger<ODataMiddleware>.Instance);
                app.Use(async (context, next) =>
                {
                    if (context.Request.Path.StartsWithSegments("/OData.svc"))
                        await odata.InvokeAsync(context, null!);
                    else
                        await next();
                });
            });
        using var client = Client(server);
        const string odataPath = "/OData.svc/('Root')/ValidateCredentials";
        // Successes release their own slots without clearing the earlier failure.
        foreach (var password in new[] { "wrong", "admin", "admin", "wrong" })
        {
            using var attempt = odataFirst
                ? await client.PostAsJsonAsync(odataPath, new { userName = "builtin\\admin", password })
                : await client.PostAsJsonAsync("/authentication/local/login",
                    new { username = "BuiltIn\\Admin", password, requestMfaChallenge = challenge, twoFactorCode = code });
            var expected = password == "admin" ? HttpStatusCode.OK :
                odataFirst ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized;
            Assert.AreEqual(expected, attempt.StatusCode, await attempt.Content.ReadAsStringAsync());
        }

        using var localBlocked = await client.PostAsJsonAsync("/authentication/local/login",
            new { username = "builtin\\admin", password = "admin", requestMfaChallenge = challenge, twoFactorCode = code });
        using var odataBlocked = await client.PostAsJsonAsync(odataPath, new { userName = "builtin\\admin", password = "admin" });
        if (customUsers != null)
        {
            Assert.AreEqual(odataFirst ? 0 : 4, customUsers.ValidationCalls,
                "An exhausted account budget must prevent calling the custom validator.");
            if (!odataFirst)
                Assert.AreEqual(code, customUsers.LastTwoFactorCode);
        }
        Assert.AreEqual(HttpStatusCode.Unauthorized, localBlocked.StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, odataBlocked.StatusCode);
        foreach (var response in new[] { localBlocked, odataBlocked })
            Assert.IsNull(response.Headers.RetryAfter);
    });

    [DataTestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public Task SuccessfulLoginsDoNotConsumePasswordBudget(bool challenge, bool mfa) => Test2(
        services => services.AddMultiFactorAuthenticationProvider<MfaProvider>(), async () =>
    {
        if (mfa)
        {
            var admin = User.Administrator;
            admin.MultiFactorEnabled = true;
            await admin.SaveAsync(CancellationToken.None);
            admin.MultiFactorRegistered = true;
            await admin.SaveAsync(CancellationToken.None);
        }
        using var rsa = RSA.Create(2048);
        using var server = CreateServer(new RsaSecurityKey(rsa) { KeyId = "test" },
            options => options.AttemptsPerMinutePerAccount = 2,
            services: services => services.AddMultiFactorAuthenticationProvider<MfaProvider>());
        using var client = Client(server);
        using var secondWindow = Client(server);
        for (var attempt = 0; attempt < 4; attempt++)
        {
            var window = attempt % 2 == 0 ? client : secondWindow;
            using var response = await window.PostAsJsonAsync("/authentication/local/login", new
            {
                username = "builtin\\admin", password = "admin", requestMfaChallenge = challenge,
                twoFactorCode = mfa && !challenge ? "correct" : null
            });
            Assert.AreEqual(mfa && challenge ? HttpStatusCode.Accepted : HttpStatusCode.OK,
                response.StatusCode, $"Unexpected status for password attempt {attempt + 1}.");
        }
    });

    [TestMethod]
    public Task FailedMfaChecksStillConsumePasswordBudget() => Test2(
        services => services.AddMultiFactorAuthenticationProvider<MfaProvider>(), async () =>
    {
        var admin = User.Administrator;
        admin.MultiFactorEnabled = true;
        await admin.SaveAsync(CancellationToken.None);
        admin.MultiFactorRegistered = true;
        await admin.SaveAsync(CancellationToken.None);
        using var rsa = RSA.Create(2048);
        using var server = CreateServer(new RsaSecurityKey(rsa) { KeyId = "test" },
            options => options.AttemptsPerMinutePerAccount = 2,
            services: services => services.AddMultiFactorAuthenticationProvider<MfaProvider>());
        using var client = Client(server);
        foreach (var code in new[] { "wrong", "correct", "correct", "wrong" })
        {
            using var response = await Login(client, code: code);
            Assert.AreEqual(code == "correct" ? HttpStatusCode.OK : HttpStatusCode.Unauthorized, response.StatusCode);
        }
        using var blocked = await Login(client, code: "correct");
        Assert.AreEqual(HttpStatusCode.Unauthorized, blocked.StatusCode);
        Assert.IsNull(blocked.Headers.RetryAfter);
    });

    [TestMethod]
    public Task PasswordBudgetIsLocalToEachApplicationInstance() => Test(async () =>
    {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test" };
        using var first = CreateServer(key, options => options.AttemptsPerMinutePerAccount = 2);
        using var second = CreateServer(key, options => options.AttemptsPerMinutePerAccount = 2);
        using var firstClient = Client(first);
        using var secondClient = Client(second);
        foreach (var client in new[] { firstClient, secondClient })
        {
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await Login(client, "wrong")).StatusCode);
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await Login(client, "wrong")).StatusCode);
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await Login(client)).StatusCode);
        }
    });

    // Deliberately leaves rate limiting to the common login flow.
    private sealed class CountingPasswordUsers : ILocalAuthenticationUsers
    {
        public int ValidationCalls { get; private set; }
        public string? LastTwoFactorCode { get; private set; }

        public async Task<int?> ValidateAsync(HttpContext context, string username, string password, string? twoFactorCode)
        {
            ValidationCalls++;
            LastTwoFactorCode = twoFactorCode;
            using var account = new SystemAccount();
            var candidate = User.Load(username);
            var user = candidate == null ? null :
                await User.LoadForAuthenticationAsync(candidate.Id, context.RequestAborted);
            return user is { Enabled: true } && user.CheckPasswordMatch(password) ? user.Id : null;
        }

        public Task<bool> IsAllowedAsync(int userId, CancellationToken cancellationToken) =>
            Task.FromResult(userId == User.Administrator.Id);
    }
}
