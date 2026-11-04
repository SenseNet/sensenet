using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Services.Core.Operations;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Storage.Security;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Authentication.Local.Tests;

public partial class LocalAuthenticationTests
{
    private const string ChangedPassword = "changed-password-12345";

    [DataTestMethod]
    [DataRow("action")]
    [DataRow("field")]
    [DataRow("hash")]
    public Task PasswordChange_RevokesSessionsOnAllHostsAndPreservesOtherCredentials(string route) => Test(async () =>
    {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test" };
        using var first = CreateServer(key);
        using var second = CreateServer(key);
        using var client = Client(first);
        using var otherClient = Client(second);
        var session = (await (await Login(client)).Content.ReadFromJsonAsync<LocalTokenResponse>())!;
        var otherSession = (await (await Login(otherClient)).Content.ReadFromJsonAsync<LocalTokenResponse>())!;
        var storage = Providers.Instance.Services.GetRequiredService<IAccessTokenDataProvider>();
        var user = User.Administrator;
        var mfaKey = user.TwoFactorKey;
        foreach (var feature in new[] { "local-auth-mfa", "local-auth-reset", "apikey", "unrelated-feature" })
            await SeedToken(storage, user.Id, feature, feature);
        var preserved = (await storage.LoadAccessTokensAsync(user.Id, CancellationToken.None))
            .Where(t => !t.Feature.StartsWith("local-auth-")).Select(t => t.Value).ToArray();
        await SeedToken(storage, User.Visitor.Id, "local-auth-session", "another-user-session");

        if (route == "action")
            await IdentityOperations.ChangePassword(Content.Create(user), new DefaultHttpContext(), ChangedPassword);
        else
        {
            if (route == "hash") user.PasswordHash = PasswordHashProvider.EncodePassword(ChangedPassword, user);
            else user.Password = ChangedPassword;
            await user.SaveAsync(SavingMode.KeepVersion, CancellationToken.None);
        }

        foreach (var old in new[] { session, otherSession })
        {
            otherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", old.AccessToken);
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await otherClient.GetAsync("/protected")).StatusCode);
            otherClient.DefaultRequestHeaders.Authorization = null;
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await otherClient.PostAsJsonAsync(
                "/authentication/local/refresh", new { old.RefreshToken })).StatusCode);
        }
        var remaining = await storage.LoadAccessTokensAsync(user.Id, CancellationToken.None);
        Assert.IsFalse(remaining.Any(t => t.Feature.StartsWith("local-auth-")));
        CollectionAssert.AreEquivalent(preserved, remaining.Select(t => t.Value).ToArray());
        Assert.AreEqual(mfaKey, (await User.LoadForAuthenticationAsync(user.Id, CancellationToken.None)).TwoFactorKey);
        Assert.IsTrue((await storage.LoadAccessTokensAsync(User.Visitor.Id, CancellationToken.None))
            .Any(t => t.Value == "another-user-session"));
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Login(otherClient)).StatusCode);
        var login = await Login(otherClient, ChangedPassword);
        Assert.AreEqual(HttpStatusCode.OK, login.StatusCode);
        var newSession = (await login.Content.ReadFromJsonAsync<LocalTokenResponse>())!;
        otherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newSession.AccessToken);
        Assert.AreEqual(HttpStatusCode.OK, (await otherClient.GetAsync("/protected")).StatusCode);

        // Reusing the saved User instance for metadata must not reapply its old password assignment.
        user.Email = "updated@example.test";
        await user.SaveAsync(SavingMode.KeepVersion, CancellationToken.None);
        Assert.AreEqual(HttpStatusCode.OK, (await otherClient.GetAsync("/protected")).StatusCode);
    });

    [TestMethod]
    public Task CreatingUser_DoesNotRevokeOtherUsersSessions() => Test(async () =>
    {
        var storage = Providers.Instance.Services.GetRequiredService<IAccessTokenDataProvider>();
        await SeedToken(storage, 1, "local-auth-session", "existing-session");
        var user = new User(User.Administrator.Parent) { Name = "new-local-user", Password = ChangedPassword };
        await user.SaveAsync(CancellationToken.None);
        Assert.IsTrue((await storage.LoadAccessTokensAsync(1, CancellationToken.None)).Any(t => t.Value == "existing-session"));
    });

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public Task HashMigration_PreservesSessionsOnlyWithoutAPendingPasswordChange(bool changePassword) => Test2(
        s => s.AddPasswordHashProviderForMigration<Sha256PasswordHashProvider>(), _ => { }, async () =>
    {
        var user = User.Administrator;
        user.PasswordHash = new Sha256PasswordHashProvider().Encode("admin", user);
        await user.SaveAsync(CancellationToken.None);
        var storage = Providers.Instance.Services.GetRequiredService<IAccessTokenDataProvider>();
        await SeedToken(storage, user.Id, "local-auth-session", "migration-session");
        var setting = typeof(SenseNet.Configuration.Security).GetProperty("EnablePasswordHashMigration")!;
        var original = setting.GetValue(null);
        setting.SetValue(null, true);
        try
        {
            if (changePassword) user.Password = ChangedPassword;
            Assert.IsTrue(await UserAuthenticationScope.ExecuteAsync(user.Id,
                _ => Task.FromResult(user.CheckPasswordMatch("admin")), CancellationToken.None));
            var stored = await User.LoadForAuthenticationAsync(user.Id, CancellationToken.None);
            Assert.IsTrue(PasswordHashProvider.CheckPassword(changePassword ? ChangedPassword : "admin", stored.PasswordHash, stored));
            var tokens = await storage.LoadAccessTokensAsync(user.Id, CancellationToken.None);
            Assert.AreEqual(!changePassword, tokens.Any(t => t.Value == "migration-session"));
        }
        finally { setting.SetValue(null, original); }
    });

    [TestMethod]
    public Task PasswordChange_CannotPassAnInFlightLogin() => Test(async () =>
    {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test" };
        var gate = new PausedUsers(pauseValidation: true);
        using var first = CreateServer(key, services: s => s.AddSingleton<ILocalAuthenticationUsers>(gate));
        using var second = CreateServer(key);
        using var client = Client(first);
        using var otherClient = Client(second);
        var login = Login(client);
        try
        {
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var user = User.Administrator;
            user.Password = ChangedPassword;
            await Assert.ThrowsExceptionAsync<UserAuthenticationLockException>(() => user.SaveAsync(CancellationToken.None));
            Assert.IsTrue((await User.LoadForAuthenticationAsync(1, CancellationToken.None)).CheckPasswordMatch("admin"));
        }
        finally { gate.Release.TrySetResult(); }
        var session = (await (await login).Content.ReadFromJsonAsync<LocalTokenResponse>())!;
        var fresh = await User.LoadForAuthenticationAsync(1, CancellationToken.None);
        fresh.Password = ChangedPassword;
        await fresh.SaveAsync(CancellationToken.None);
        otherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await otherClient.GetAsync("/protected")).StatusCode);
        otherClient.DefaultRequestHeaders.Authorization = null;
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Login(otherClient)).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await Login(otherClient, ChangedPassword)).StatusCode);
    });

    [TestMethod]
    public Task ConcurrentRefresh_CannotResurrectARevokedSession() => Test(async () =>
    {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test" };
        using var first = CreateServer(key);
        using var client = Client(first);
        var session = (await (await Login(client)).Content.ReadFromJsonAsync<LocalTokenResponse>())!;
        var gate = new PausedUsers(pauseValidation: false);
        using var second = CreateServer(key, services: s => s.AddSingleton<ILocalAuthenticationUsers>(gate));
        using var otherClient = Client(second);
        var refresh = otherClient.PostAsJsonAsync("/authentication/local/refresh", new { session.RefreshToken });
        try
        {
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var user = User.Administrator;
            user.Password = ChangedPassword;
            await user.SaveAsync(CancellationToken.None);
        }
        finally { gate.Release.TrySetResult(); }
        // A refresh that read the session before revocation may finish, but its JWT must be unusable.
        var response = await refresh;
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var racedSession = (await response.Content.ReadFromJsonAsync<LocalTokenResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", racedSession.AccessToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/protected")).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(
            "/authentication/local/refresh", new { racedSession.RefreshToken })).StatusCode);
    });

    [TestMethod]
    public Task PasswordChange_BlocksLoginAndMfaUntilRevocationCompletes() => Test2(s =>
    {
        s.AddSingleton<IAccessTokenDataProvider>(_ => FailingTokenProvider.Create());
        s.AddMultiFactorAuthenticationProvider<MfaProvider>();
    }, _ => { }, async () =>
    {
        var user = User.Administrator;
        user.Email = "admin@example.test";
        user.MultiFactorEnabled = true;
        await user.SaveAsync(CancellationToken.None);
        user.MultiFactorRegistered = true;
        await user.SaveAsync(CancellationToken.None);
        var secret = user.TwoFactorKey;
        using var rsa = RSA.Create(2048);
        var mail = new CapturedMail();
        using var server = CreateServer(new RsaSecurityKey(rsa) { KeyId = "test" }, Recovery, services: s =>
        {
            s.AddSingleton<ILocalPasswordResetSender>(mail);
            s.AddSingleton<IMultiFactorAuthenticationProvider, MfaProvider>();
        });
        using var client = Client(server);
        var begin = await client.PostAsJsonAsync("/authentication/local/login",
            new { username = "builtin\\admin", password = "admin", requestMfaChallenge = true });
        Assert.AreEqual(HttpStatusCode.Accepted, begin.StatusCode);
        var challenge = (await begin.Content.ReadFromJsonAsync<LocalMfaChallenge>())!;
        await client.PostAsJsonAsync("/authentication/local/forgot-password", new { user.Email });
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = (FailingTokenProvider)Providers.Instance.Services.GetRequiredService<IAccessTokenDataProvider>();
        provider.BeforeRevocation = async () =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(15));
        };
        user.Password = ChangedPassword;
        var save = user.SaveAsync(CancellationToken.None);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await Login(client, code: "correct")).StatusCode);
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/authentication/local/mfa",
                new { challenge.ChallengeToken, twoFactorCode = "correct" })).StatusCode);
        }
        finally { release.TrySetResult(); }
        await save;
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/authentication/local/mfa",
            new { challenge.ChallengeToken, twoFactorCode = "correct" })).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/authentication/local/reset-password",
            new { token = mail.Token, password = "another-new-password-42" })).StatusCode);
        var stored = await User.LoadForAuthenticationAsync(1, CancellationToken.None);
        Assert.IsTrue(stored.MultiFactorRegistered);
        Assert.AreEqual(secret, stored.TwoFactorKey);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Login(client, ChangedPassword)).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await Login(client, ChangedPassword, "correct")).StatusCode);
    });

    [TestMethod]
    public Task FailedRevocation_PreventsPasswordPersistence() => Test2(
        s => s.AddSingleton<IAccessTokenDataProvider>(_ => FailingTokenProvider.Create()), _ => { }, async () =>
    {
        var storage = Providers.Instance.Services.GetRequiredService<IAccessTokenDataProvider>();
        var failing = (FailingTokenProvider)storage;
        failing.FailRevocation = true;
        var user = User.Administrator;
        user.Password = ChangedPassword;
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => user.SaveAsync(CancellationToken.None));
        var stored = await User.LoadForAuthenticationAsync(1, CancellationToken.None);
        Assert.IsTrue(stored.CheckPasswordMatch("admin"));
        Assert.IsFalse(stored.CheckPasswordMatch(ChangedPassword));
        failing.FailRevocation = false;
        await user.SaveAsync(CancellationToken.None); // The failed operation released its lock.
        Assert.IsTrue((await User.LoadForAuthenticationAsync(1, CancellationToken.None)).CheckPasswordMatch(ChangedPassword));
    });

    private static Task SeedToken(IAccessTokenDataProvider storage, int userId, string feature, string value) =>
        storage.SaveAccessTokenAsync(new AccessToken
        {
            UserId = userId, Feature = feature, Value = value,
            CreationDate = DateTime.UtcNow, ExpirationDate = DateTime.UtcNow.AddHours(1)
        }, CancellationToken.None);

    private sealed class PausedUsers(bool pauseValidation) : ILocalAuthenticationUsers
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<int?> ValidateAsync(HttpContext context, string username, string password, string? twoFactorCode)
        {
            using var account = new SystemAccount();
            var user = await User.LoadForAuthenticationAsync(1, context.RequestAborted);
            if (!user.CheckPasswordMatch(password)) return null;
            if (pauseValidation) await Pause(context.RequestAborted);
            return user.Id;
        }
        public async Task<bool> IsAllowedAsync(int userId, CancellationToken cancellationToken)
        {
            if (!pauseValidation) await Pause(cancellationToken);
            return true;
        }
        private async Task Pause(CancellationToken cancel)
        {
            Entered.TrySetResult();
            await Release.Task.WaitAsync(TimeSpan.FromSeconds(15), cancel);
        }
    }

    public class FailingTokenProvider : DispatchProxy
    {
        private readonly IAccessTokenDataProvider _inner = new InMemoryAccessTokenDataProvider();
        public bool FailRevocation { get; set; }
        public Func<Task>? BeforeRevocation { get; set; }
        public static IAccessTokenDataProvider Create() => Create<IAccessTokenDataProvider, FailingTokenProvider>();
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == nameof(IAccessTokenDataProvider.DeleteAccessTokensAsync))
                return DeleteAsync(method, args);
            return method!.Invoke(_inner, args);
        }
        private async Task DeleteAsync(MethodInfo method, object?[]? args)
        {
            if (FailRevocation) throw new InvalidOperationException("Simulated token storage failure.");
            if (BeforeRevocation != null) await BeforeRevocation();
            await (Task)method.Invoke(_inner, args)!;
        }
    }
}
