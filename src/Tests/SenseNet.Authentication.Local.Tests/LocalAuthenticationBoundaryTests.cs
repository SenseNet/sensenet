using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.Storage.Security;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Authentication.Local.Tests;

public partial class LocalAuthenticationTests
{
    [TestMethod]
    public Task OnlyExplicitlyTrustedProxyCanSupplyClientIpAndTls() => Test(async () =>
    {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test" };
        foreach (var peer in new[] { "10.0.0.5", "10.0.0.6" })
        {
            using var server = CreateServer(key, o => o.KnownProxies = new[] { "10.0.0.5" }, peer);
            using var client = server.CreateClient();
            client.BaseAddress = new Uri("http://repository.example");
            client.DefaultRequestHeaders.Add("X-Forwarded-For", "127.0.0.1");
            client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");
            Assert.AreEqual(peer == "10.0.0.5" ? HttpStatusCode.OK : HttpStatusCode.Forbidden,
                (await Login(client)).StatusCode);
        }
    });

    [TestMethod]
    public Task BreakGlassAdminRequiresRegisteredMfaAndValidCode() => Test(async () =>
    {
        var admin = User.Administrator;
        admin.MultiFactorEnabled = true;
        await admin.SaveAsync(SavingMode.KeepVersion, CancellationToken.None);
        admin.MultiFactorRegistered = true;
        await admin.SaveAsync(SavingMode.KeepVersion, CancellationToken.None);
        using var rsa = RSA.Create(2048);
        using var server = CreateServer(new RsaSecurityKey(rsa) { KeyId = "test" },
            o => o.Mode = LocalAuthenticationMode.Secondary,
            services: s => s.AddSingleton<IMultiFactorAuthenticationProvider, MfaProvider>());
        using var client = Client(server);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Login(client, code: "wrong")).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await Login(client, code: "correct")).StatusCode);
    });

    [TestMethod]
    public Task JsonOnlyEndpointsRejectFormAndOversizedBodies() => Test(async () =>
    {
        using var rsa = RSA.Create(2048);
        using var server = CreateServer(new RsaSecurityKey(rsa) { KeyId = "test" });
        using var client = Client(server);
        Assert.AreEqual(HttpStatusCode.UnsupportedMediaType,
            (await client.PostAsync("/authentication/local/login", new FormUrlEncodedContent(
                new Dictionary<string, string> { ["username"] = "admin", ["password"] = "admin" }))).StatusCode);
        Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge,
            (await client.PostAsJsonAsync("/authentication/local/login", new { password = new string('x', 9000) })).StatusCode);
    });

    public sealed class MfaProvider : IMultiFactorAuthenticationProvider
    {
        public string GetApplicationName() => "LocalAuthTests";
        public (string Url, string EntryKey) GenerateSetupCode(string userName, string key) => ("test", "test");
        public bool ValidateTwoFactorCode(string key, string codeToValidate) => codeToValidate == "correct";
    }
}
