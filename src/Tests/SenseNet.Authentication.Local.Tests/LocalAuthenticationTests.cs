using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Tests.Core;
using SenseNet.Extensions.DependencyInjection;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Authentication.Local.Tests;

[TestClass]
public partial class LocalAuthenticationTests : TestBase
{
    private static void Configure(LocalAuthenticationOptions options, RsaSecurityKey key)
    {
        options.Mode = LocalAuthenticationMode.InternalOnly;
        options.Issuer = "https://repository.example";
        options.SigningKey = key;
        options.LoginNetworks = new[] { "127.0.0.0/8", "::1/128" };
        options.AllowedUserIds = new[] { 1 };
        options.RequireMultiFactor = false;
        options.AttemptsPerMinutePerIp = 100;
    }

    private static TestServer CreateServer(RsaSecurityKey key,
        Action<LocalAuthenticationOptions>? configure = null, string? ip = "127.0.0.1",
        Action<IServiceCollection>? services = null)
    {
        return new TestServer(new WebHostBuilder()
            .ConfigureServices(s =>
            {
                s.AddLogging();
                var sql = Environment.GetEnvironmentVariable("SB167_TEST_SQL");
                if (string.IsNullOrEmpty(sql))
                    s.AddSingleton<ILocalAuthenticationUserLock, TestUserLock>();
                else
                    s.AddSingleton<ILocalAuthenticationUserLock>(
                        new SnWebApplication.Api.Sql.LocalAuth.SqlLocalAuthenticationUserLock(
                            Microsoft.Extensions.Options.Options.Create(new ConnectionStringOptions { Repository = sql })));
                s.AddSingleton(Providers.Instance.Services.GetRequiredService<IAccessTokenDataProvider>());
                services?.Invoke(s);
                s.AddSenseNetLocalAuthentication(o => { Configure(o, key); configure?.Invoke(o); });
            })
            .Configure(app =>
            {
                app.Use((context, next) =>
                {
                    context.Connection.RemoteIpAddress = ip == null ? null : IPAddress.Parse(ip);
                    return next();
                });
                app.UseSenseNetLocalAuthentication();
                app.UseSenseNetAuthentication();
                app.Run(async context =>
                {
                    context.Response.StatusCode = context.User.Identity?.IsAuthenticated == true ? 200 : 401;
                    if (context.Response.StatusCode == 200)
                        await context.Response.WriteAsync(User.Current.Id.ToString());
                });
            }));
    }

    private static HttpClient Client(TestServer server)
    {
        var client = server.CreateClient();
        client.BaseAddress = new Uri("https://repository.example");
        return client;
    }

    private static Task<HttpResponseMessage> Login(HttpClient client, string password = "admin", string? code = null) =>
        client.PostAsJsonAsync("/authentication/local/login", new { username = "builtin\\admin", password, twoFactorCode = code });

    [TestMethod]
    public Task LoginRefreshRevoke_UsesRepositoryCredentialsAndSharedPersistence() => Test(async () =>
    {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test-1" };
        using var server = CreateServer(key);
        using var client = Client(server);
        var login = await Login(client);
        Assert.AreEqual(HttpStatusCode.OK, login.StatusCode);
        var tokens = (await login.Content.ReadFromJsonAsync<LocalTokenResponse>())!;
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        Assert.AreEqual("1", jwt.Claims.Single(c => c.Type == "sub").Value);
        Assert.AreEqual("test-1", jwt.Header.Kid);
        var records = await Providers.Instance.Services.GetRequiredService<IAccessTokenDataProvider>()
            .LoadAccessTokensAsync(1, CancellationToken.None);
        Assert.IsFalse(records.Any(r => r.Value == tokens.RefreshToken));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/protected")).StatusCode);
        var refresh = await client.PostAsJsonAsync("/authentication/local/refresh", new { tokens.RefreshToken });
        Assert.AreEqual(HttpStatusCode.OK, refresh.StatusCode);
        var refreshed = (await refresh.Content.ReadFromJsonAsync<LocalTokenResponse>())!;
        Assert.AreNotEqual(tokens.AccessToken, refreshed.AccessToken);
        // A second host uses the same provider: no process-local session dependency.
        using var secondServer = CreateServer(key);
        using var secondClient = Client(secondServer);
        secondClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);
        Assert.AreEqual(HttpStatusCode.OK, (await secondClient.GetAsync("/protected")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NoContent,
            (await client.PostAsJsonAsync("/authentication/local/revoke", new { tokens.RefreshToken })).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await secondClient.GetAsync("/protected")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/authentication/local/refresh", new { tokens.RefreshToken })).StatusCode);
    });

    [TestMethod]
    public Task CredentialsAllowlistAndMfaFailClosed() => Test(async () =>
    {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test" };
        foreach (var configure in new Action<LocalAuthenticationOptions>[]
        {
            o => o.AllowedUserIds = new[] { 999 },
            o => o.RequireMultiFactor = true,
            o => o.Mode = LocalAuthenticationMode.Secondary
        })
        {
            using var server = CreateServer(key, configure);
            using var client = Client(server);
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await Login(client)).StatusCode);
        }
        using var allowedServer = CreateServer(key);
        using var allowedClient = Client(allowedServer);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Login(allowedClient, "wrong")).StatusCode);
        using (new SystemAccount())
        {
            var user = new User(User.Administrator.Parent) { Name = "DisabledLocalUser", Enabled = false, Password = "test-password" };
            user.Enabled = false;
            await user.SaveAsync(CancellationToken.None);
        }
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await allowedClient.PostAsJsonAsync("/authentication/local/login", new { username = "builtin\\DisabledLocalUser", password = "test-password" })).StatusCode);
    });

    [TestMethod]
    public Task NetworkTlsAndSpoofedForwardingFailClosed() => Test(async () =>
    {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test" };
        foreach (var ip in new[] { "192.0.2.1", null })
        {
            using var deniedServer = CreateServer(key, ip: ip);
            using var deniedClient = Client(deniedServer);
            Assert.AreEqual(HttpStatusCode.Forbidden, (await Login(deniedClient)).StatusCode);
            var capabilities = await deniedClient.GetFromJsonAsync<JsonElement>("/authentication/capabilities");
            Assert.AreEqual(JsonValueKind.Null, capabilities.GetProperty("local").ValueKind);
        }
        using var server = CreateServer(key);
        using var client = Client(server);
        foreach (var header in new[] { "X-Forwarded-For", "X-Forwarded-Proto", "Forwarded" })
        {
            client.DefaultRequestHeaders.Add(header, "127.0.0.1");
            Assert.AreEqual(HttpStatusCode.Forbidden, (await Login(client)).StatusCode);
            client.DefaultRequestHeaders.Remove(header);
        }
        using var httpClient = server.CreateClient();
        httpClient.BaseAddress = new Uri("http://repository.example");
        Assert.AreEqual(HttpStatusCode.Forbidden, (await Login(httpClient)).StatusCode);
    });

    [TestMethod]
    public Task ThrottlesAccountAcrossConnections() => Test(async () =>
    {
        using var rsa = RSA.Create(2048);
        using var server = CreateServer(new RsaSecurityKey(rsa) { KeyId = "test" }, o => o.AttemptsPerMinutePerAccount = 2);
        using var client = Client(server);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Login(client, "wrong")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Login(client, "wrong")).StatusCode);
        Assert.AreEqual(HttpStatusCode.TooManyRequests, (await Login(client)).StatusCode);
    });

    [TestMethod]
    public void DisabledDoesNotRegisterLocalSchemes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication("Existing");
        services.AddSenseNetLocalAuthentication(_ => { });
        using var provider = services.BuildServiceProvider();
        var schemes = provider.GetRequiredService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
        Assert.IsNull(schemes.GetSchemeAsync(LocalAuthenticationOptions.Scheme).GetAwaiter().GetResult());
    }
}
