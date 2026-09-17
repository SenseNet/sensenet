using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Authentication.Local.Tests;

public partial class LocalAuthenticationTests
{
    [TestMethod]
    public Task SecondaryRoutesExplicitlyWithoutExternalFallback() => Test(async () =>
    {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test" };
        var user = new User(User.Administrator.Parent) { Name = "LocalMember", LoginName = "LocalMember", Enabled = true, Password = "test-password" };
        await user.SaveAsync(CancellationToken.None);
        ExternalHandler.Calls = 0;
        ExternalHandler.Subject = user.Id.ToString();
        using var server = CreateServer(key, o =>
        {
            o.Mode = LocalAuthenticationMode.Secondary;
            o.AllowedUserIds = new[] { user.Id };
        }, services: s => s.AddAuthentication().AddScheme<AuthenticationSchemeOptions, ExternalHandler>("Bearer", _ => { }));
        using var client = Client(server);
        var login = await client.PostAsJsonAsync("/authentication/local/login", new { username = "builtin\\LocalMember", password = "test-password" });
        Assert.AreEqual(HttpStatusCode.OK, login.StatusCode);
        var tokens = (await login.Content.ReadFromJsonAsync<LocalTokenResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        Assert.AreEqual(user.Id.ToString(), await client.GetStringAsync("/protected"));
        Assert.AreEqual(0, ExternalHandler.Calls);
        // A local token with a bad signature must never reach external auth.
        var parts = tokens.AccessToken.Split('.');
        parts[2] = (parts[2][0] == 'a' ? "b" : "a") + parts[2][1..];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", string.Join(".", parts));
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/protected")).StatusCode);
        Assert.AreEqual(0, ExternalHandler.Calls);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "external");
        Assert.AreEqual(user.Id.ToString(), await client.GetStringAsync("/protected"));
        Assert.AreEqual(1, ExternalHandler.Calls);
    });

    [TestMethod]
    public Task RotationAcceptsPreviousKeyButRejectsWrongAudienceAndExpiredToken() => Test(async () =>
    {
        using var oldRsa = RSA.Create(2048);
        using var newRsa = RSA.Create(2048);
        var oldKey = new RsaSecurityKey(oldRsa) { KeyId = "old" };
        var newKey = new RsaSecurityKey(newRsa) { KeyId = "new" };
        using var original = CreateServer(oldKey);
        using var originalClient = Client(original);
        var tokens = (await (await Login(originalClient)).Content.ReadFromJsonAsync<LocalTokenResponse>())!;
        using var rotated = CreateServer(newKey, o => o.PreviousSigningKeys.Add(new RsaSecurityKey(oldRsa.ExportParameters(false)) { KeyId = "old" }));
        using var client = Client(rotated);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/protected")).StatusCode);
        var originalJwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        foreach (var invalid in new[]
        {
            new JwtSecurityToken("https://repository.example", "wrong-audience", originalJwt.Claims.Where(c => c.Type is "sub" or "sid" or "jti"), DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1), new SigningCredentials(newKey, SecurityAlgorithms.RsaSha256)),
            new JwtSecurityToken("https://repository.example", "sensenet", originalJwt.Claims.Where(c => c.Type is "sub" or "sid" or "jti"), DateTime.UtcNow.AddMinutes(-2), DateTime.UtcNow.AddMinutes(-1), new SigningCredentials(newKey, SecurityAlgorithms.RsaSha256))
        })
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(invalid));
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/protected")).StatusCode);
        }
        using var retired = CreateServer(newKey);
        using var retiredClient = Client(retired);
        retiredClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await retiredClient.GetAsync("/protected")).StatusCode);
    });

    [TestMethod]
    public Task TokenNetworkRestrictionAndDisabledUserInvalidateExistingSession() => Test(async () =>
    {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test" };
        var user = new User(User.Administrator.Parent) { Name = "SessionUser", LoginName = "SessionUser", Enabled = true, Password = "test-password" };
        await user.SaveAsync(CancellationToken.None);
        using var server = CreateServer(key, o => o.AllowedUserIds = new[] { user.Id });
        using var client = Client(server);
        var login = await client.PostAsJsonAsync("/authentication/local/login", new { username = "builtin\\SessionUser", password = "test-password" });
        var tokens = (await login.Content.ReadFromJsonAsync<LocalTokenResponse>())!;
        using var blocked = CreateServer(key, o => { o.AllowedUserIds = new[] { user.Id }; o.TokenNetworks = Array.Empty<string>(); });
        using var blockedClient = Client(blocked);
        blockedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await blockedClient.GetAsync("/protected")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/protected")).StatusCode);
        user.Enabled = false;
        await user.SaveAsync(CancellationToken.None);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/protected")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/authentication/local/refresh", new { tokens.RefreshToken })).StatusCode);
    });

    public sealed class ExternalHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public static int Calls;
        public static string Subject = "1";
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            Calls++;
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", Subject) }, "external")), Scheme.Name)));
        }
    }
}
