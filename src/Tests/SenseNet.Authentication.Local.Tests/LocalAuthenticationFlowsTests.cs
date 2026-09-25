using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Email;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Storage.Security;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Authentication.Local.Tests;

public partial class LocalAuthenticationTests
{
    private sealed class CapturedMail : ILocalPasswordResetSender
    {
        public List<EmailData> Messages { get; } = new();
        public Task SendAsync(EmailData email, CancellationToken cancellationToken) { Messages.Add(email); return Task.CompletedTask; }
        public string Token => Regex.Match(Messages.Last().Body, @"localResetToken=([A-Za-z0-9_-]{64})").Groups[1].Value;
    }

    private static void Recovery(LocalAuthenticationOptions options)
    {
        options.PasswordRecovery.Enabled = true;
        options.PasswordRecovery.ResetUrl = "https://admin.example/login";
        options.AttemptsPerMinutePerAccount = 50;
    }

    [TestMethod]
    public Task BrandingAndRecoveryExposeOnlyConfiguredPublicFields() => Test(async () =>
    {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test" };
        using var server = CreateServer(key, o =>
        {
            Recovery(o);
            o.Appearance.Title = "Login to Test Repository";
            o.Appearance.ButtonColor = "#123456";
            o.Appearance.BackgroundImageUrl = "https://assets.example/background.png";
        });
        using var client = Client(server);
        var capabilities = await client.GetFromJsonAsync<JsonElement>("/authentication/capabilities");
        var local = capabilities.GetProperty("local");
        Assert.AreEqual("#123456", local.GetProperty("appearance").GetProperty("buttonColor").GetString());
        Assert.AreEqual("/authentication/local/forgot-password", local.GetProperty("forgotPassword").GetString());
        Assert.IsFalse(capabilities.ToString().Contains("LoginNetworks"));
        Assert.IsFalse(capabilities.ToString().Contains("SigningKey"));
        foreach (var configure in new Action<LocalAuthenticationOptions>[]
        {
            o => o.Appearance.ButtonColor = "url(https://bad.example)",
            o => o.Appearance.LogoUrl = "javascript:alert(1)",
            o => { Recovery(o); o.PasswordRecovery.ResetUrl = "http://external.example"; },
            o => { Recovery(o); o.PasswordRecovery.ResetUrl = "https://admin.example/#leak"; }
        })
            Assert.ThrowsException<ArgumentException>(() => CreateServer(key, configure));
    });

    [TestMethod]
    public Task PasswordReset_IsPrivateSingleUseAndRevokesLocalSessions() => Test(async () =>
    {
        var admin = User.Administrator;
        admin.Email = "admin@repository.example";
        await admin.SaveAsync(SavingMode.KeepVersion, CancellationToken.None);
        var mail = new CapturedMail();
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test" };
        using var server = CreateServer(key, Recovery, services: s => s.AddSingleton<ILocalPasswordResetSender>(mail));
        using var client = Client(server);
        var session = (await (await Login(client)).Content.ReadFromJsonAsync<LocalTokenResponse>())!;
        var unknown = await client.PostAsJsonAsync("/authentication/local/forgot-password", new { email = "unknown@repository.example" });
        var known = await client.PostAsJsonAsync("/authentication/local/forgot-password", new
        {
            email = admin.Email, resetUrl = "https://attacker.example"
        });
        Assert.AreEqual(HttpStatusCode.Accepted, known.StatusCode);
        Assert.AreEqual(await unknown.Content.ReadAsStringAsync(), await known.Content.ReadAsStringAsync());
        Assert.AreEqual(1, mail.Messages.Count);
        Assert.IsTrue(mail.Messages[0].Body.Contains("https://admin.example/login?repoUrl="));
        Assert.IsFalse(mail.Messages[0].Body.Contains("attacker"));
        var token = mail.Token;
        Assert.AreEqual(64, token.Length);
        var storage = Providers.Instance.Services.GetRequiredService<IAccessTokenDataProvider>();
        Assert.IsFalse((await storage.LoadAccessTokensAsync(1, CancellationToken.None)).Any(t => t.Value == token));
        Assert.AreEqual(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/authentication/local/reset-password",
            new { token, password = "short" })).StatusCode);
        const string newPassword = "a-new-long-password-42";
        using var secondServer = CreateServer(key, Recovery);
        using var secondClient = Client(secondServer);
        var attempts = await Task.WhenAll(
            client.PostAsJsonAsync("/authentication/local/reset-password", new { token, password = newPassword }),
            secondClient.PostAsJsonAsync("/authentication/local/reset-password", new { token, password = newPassword }));
        Assert.AreEqual(1, attempts.Count(r => r.StatusCode == HttpStatusCode.NoContent));
        Assert.AreEqual(1, attempts.Count(r => r.StatusCode == HttpStatusCode.BadRequest));
        Assert.AreEqual(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/authentication/local/reset-password",
            new { token, password = newPassword })).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/protected")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/authentication/local/refresh",
            new { session.RefreshToken })).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Login(client)).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await Login(client, newPassword)).StatusCode);
    });

    [TestMethod]
    public Task PasswordReset_RejectsExpiredDisallowedAndDisabledFlows() => Test(async () =>
    {
        var admin = User.Administrator;
        admin.Email = "admin@repository.example";
        await admin.SaveAsync(SavingMode.KeepVersion, CancellationToken.None);
        var mail = new CapturedMail();
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test" };
        using var server = CreateServer(key, Recovery, services: s => s.AddSingleton<ILocalPasswordResetSender>(mail));
        using var client = Client(server);
        await client.PostAsJsonAsync("/authentication/local/forgot-password", new { email = admin.Email });
        var token = mail.Token;
        await Providers.Instance.Services.GetRequiredService<IAccessTokenDataProvider>().UpdateAccessTokenAsync(
            Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token))), DateTime.UtcNow.AddMinutes(-1), CancellationToken.None);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/authentication/local/reset-password",
            new { token, password = "a-new-long-password-42" })).StatusCode);
        using var disallowedServer = CreateServer(key, o => { Recovery(o); o.AllowedUserIds = new[] { 999 }; },
            services: s => s.AddSingleton<ILocalPasswordResetSender>(mail));
        using var disallowed = Client(disallowedServer);
        await disallowed.PostAsJsonAsync("/authentication/local/forgot-password", new { email = admin.Email });
        Assert.AreEqual(1, mail.Messages.Count);
        using var disabledServer = CreateServer(key);
        using var disabled = Client(disabledServer);
        Assert.AreEqual(HttpStatusCode.NotFound, (await disabled.PostAsJsonAsync("/authentication/local/forgot-password",
            new { email = admin.Email })).StatusCode);
    });

    [TestMethod]
    public Task MfaChallenge_EnrollsWithoutIssuingSessionAndCannotBeReplayed() => Test2(
        s => s.AddMultiFactorAuthenticationProvider<MfaProvider>(), _ => { }, async () =>
    {
        var admin = User.Administrator;
        admin.MultiFactorEnabled = true;
        await admin.SaveAsync(SavingMode.KeepVersion, CancellationToken.None);
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test" };
        using var server = CreateServer(key, o => { o.RequireMultiFactor = true; o.AttemptsPerMinutePerAccount = 50; },
            services: s => s.AddSingleton<IMultiFactorAuthenticationProvider, MfaProvider>());
        using var client = Client(server);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/authentication/local/login",
            new { username = "builtin\\admin", password = "wrong", requestMfaChallenge = true })).StatusCode);
        var begin = await client.PostAsJsonAsync("/authentication/local/login",
            new { username = "builtin\\admin", password = "admin", requestMfaChallenge = true });
        Assert.AreEqual(HttpStatusCode.Accepted, begin.StatusCode);
        var challenge = (await begin.Content.ReadFromJsonAsync<LocalMfaChallenge>())!;
        Assert.AreEqual("test", challenge.ManualEntryKey);
        Assert.IsFalse((await begin.Content.ReadAsStringAsync()).Contains("accessToken"));
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/protected")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/authentication/local/mfa",
            new { challenge.ChallengeToken, twoFactorCode = "wrong" })).StatusCode);
        using var otherServer = CreateServer(key, o => o.AttemptsPerMinutePerAccount = 50,
            services: s => s.AddSingleton<IMultiFactorAuthenticationProvider, MfaProvider>());
        using var other = Client(otherServer);
        var complete = await other.PostAsJsonAsync("/authentication/local/mfa",
            new { challenge.ChallengeToken, twoFactorCode = "correct" });
        Assert.AreEqual(HttpStatusCode.OK, complete.StatusCode);
        Assert.IsTrue(User.Administrator.MultiFactorRegistered);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/authentication/local/mfa",
            new { challenge.ChallengeToken, twoFactorCode = "correct" })).StatusCode);
        var again = await client.PostAsJsonAsync("/authentication/local/login",
            new { username = "builtin\\admin", password = "admin", requestMfaChallenge = true });
        var registered = (await again.Content.ReadFromJsonAsync<LocalMfaChallenge>())!;
        Assert.IsNull(registered.ManualEntryKey);
        Assert.IsNull(registered.QrCodeSetupImageUrl);
    });

    private sealed class FailedMail : ILocalPasswordResetSender
    {
        public Task SendAsync(EmailData message, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Simulated SMTP failure");
    }

    [TestMethod]
    public Task RecoveryDeliveryFailure_DoesNotLeaveAUsableResetToken() => Test(async () =>
    {
        var admin = User.Administrator;
        admin.Email = "admin@repository.example";
        await admin.SaveAsync(SavingMode.KeepVersion, CancellationToken.None);
        using var rsa = RSA.Create(2048);
        using var server = CreateServer(new RsaSecurityKey(rsa) { KeyId = "test" }, Recovery,
            services: s => s.AddSingleton<ILocalPasswordResetSender, FailedMail>());
        using var client = Client(server);
        Assert.AreEqual(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/authentication/local/forgot-password",
            new { email = admin.Email })).StatusCode);
        var tokens = await Providers.Instance.Services.GetRequiredService<IAccessTokenDataProvider>()
            .LoadAccessTokensAsync(admin.Id, CancellationToken.None);
        Assert.IsFalse(tokens.Any(t => t.Feature == "local-auth-reset"));
    });

    [TestMethod]
    public Task RecoveryPreservesMfaAndInvalidatesPendingChallenges() => Test2(
        s => s.AddMultiFactorAuthenticationProvider<MfaProvider>(), _ => { }, async () =>
    {
        var admin = User.Administrator;
        admin.Email = "admin@repository.example";
        admin.MultiFactorEnabled = true;
        await admin.SaveAsync(SavingMode.KeepVersion, CancellationToken.None);
        admin.MultiFactorRegistered = true;
        await admin.SaveAsync(SavingMode.KeepVersion, CancellationToken.None);
        var originalKey = admin.TwoFactorKey;
        var mail = new CapturedMail();
        using var rsa = RSA.Create(2048);
        using var server = CreateServer(new RsaSecurityKey(rsa) { KeyId = "test" }, Recovery, services: s =>
        {
            s.AddSingleton<ILocalPasswordResetSender>(mail);
            s.AddSingleton<IMultiFactorAuthenticationProvider, MfaProvider>();
        });
        using var client = Client(server);
        var begin = await client.PostAsJsonAsync("/authentication/local/login",
            new { username = "builtin\\admin", password = "admin", requestMfaChallenge = true });
        var challenge = (await begin.Content.ReadFromJsonAsync<LocalMfaChallenge>())!;
        await client.PostAsJsonAsync("/authentication/local/forgot-password", new { email = admin.Email });
        Assert.AreEqual(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/authentication/local/reset-password",
            new { token = challenge.ChallengeToken, password = "long-new-password-42" })).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/authentication/local/mfa",
            new { challengeToken = mail.Token, twoFactorCode = "correct" })).StatusCode);
        Assert.AreEqual(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/authentication/local/reset-password",
            new { token = mail.Token, password = "long-new-password-42" })).StatusCode);
        Assert.IsTrue(User.Administrator.MultiFactorEnabled);
        Assert.IsTrue(User.Administrator.MultiFactorRegistered);
        Assert.AreEqual(originalKey, User.Administrator.TwoFactorKey);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/authentication/local/mfa",
            new { challenge.ChallengeToken, twoFactorCode = "correct" })).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Login(client, "long-new-password-42")).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await Login(client, "long-new-password-42", "correct")).StatusCode);
    });

    [TestMethod]
    public Task MfaRejectsExpiredChallengesAndSecondaryEnrollment() => Test2(
        s => s.AddMultiFactorAuthenticationProvider<MfaProvider>(), _ => { }, async () =>
    {
        var admin = User.Administrator;
        admin.MultiFactorEnabled = true;
        await admin.SaveAsync(SavingMode.KeepVersion, CancellationToken.None);
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test" };
        using var secondaryServer = CreateServer(key, o => o.Mode = LocalAuthenticationMode.Secondary,
            services: s => s.AddSingleton<IMultiFactorAuthenticationProvider, MfaProvider>());
        using var secondary = Client(secondaryServer);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await secondary.PostAsJsonAsync("/authentication/local/login",
            new { username = "builtin\\admin", password = "admin", requestMfaChallenge = true })).StatusCode);
        using var server = CreateServer(key, services: s => s.AddSingleton<IMultiFactorAuthenticationProvider, MfaProvider>());
        using var client = Client(server);
        var begin = await client.PostAsJsonAsync("/authentication/local/login",
            new { username = "builtin\\admin", password = "admin", requestMfaChallenge = true });
        var challenge = (await begin.Content.ReadFromJsonAsync<LocalMfaChallenge>())!;
        await Providers.Instance.Services.GetRequiredService<IAccessTokenDataProvider>().UpdateAccessTokenAsync(
            Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(challenge.ChallengeToken))),
            DateTime.UtcNow.AddMinutes(-1), CancellationToken.None);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/authentication/local/mfa",
            new { challenge.ChallengeToken, twoFactorCode = "correct" })).StatusCode);
        Assert.IsFalse(User.Administrator.MultiFactorRegistered);
    });

    [TestMethod]
    public Task RealAuthenticator_ProducesQrAndAcceptsCurrentTotpWithoutEmail() => Test2(
        s => s.AddDefaultMultiFactorAuthenticationProvider(), _ => { }, async () =>
    {
        var admin = User.Administrator;
        admin.Email = "";
        admin.MultiFactorEnabled = true;
        await admin.SaveAsync(SavingMode.KeepVersion, CancellationToken.None);
        using var rsa = RSA.Create(2048);
        using var server = CreateServer(new RsaSecurityKey(rsa) { KeyId = "test" },
            services: s => s.AddDefaultMultiFactorAuthenticationProvider());
        using var client = Client(server);
        var begin = await client.PostAsJsonAsync("/authentication/local/login",
            new { username = "builtin\\admin", password = "admin", requestMfaChallenge = true });
        Assert.AreEqual(HttpStatusCode.Accepted, begin.StatusCode);
        var challenge = (await begin.Content.ReadFromJsonAsync<LocalMfaChallenge>())!;
        Assert.IsTrue(challenge.QrCodeSetupImageUrl!.StartsWith("data:image/png;base64,"));
        Assert.IsTrue(challenge.ManualEntryKey!.Length >= 20);
        // RFC 6238: independently calculate the current six-digit code from the repository secret.
        var counter = BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        if (BitConverter.IsLittleEndian) Array.Reverse(counter);
        var secret = System.Text.Encoding.UTF8.GetBytes(admin.TwoFactorKey[..30]);
        using var hmac = new HMACSHA1(secret);
        var digest = hmac.ComputeHash(counter);
        var offset = digest[^1] & 15;
        var number = ((digest[offset] & 127) << 24) | (digest[offset + 1] << 16) |
            (digest[offset + 2] << 8) | digest[offset + 3];
        var code = (number % 1000000).ToString("D6");
        Assert.AreEqual(HttpStatusCode.OK, (await client.PostAsJsonAsync("/authentication/local/mfa",
            new { challenge.ChallengeToken, twoFactorCode = code })).StatusCode);
        Assert.IsTrue(User.Administrator.MultiFactorRegistered);
    });

}
