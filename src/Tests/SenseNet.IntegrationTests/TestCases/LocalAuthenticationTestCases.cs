using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Authentication.Local;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.IntegrationTests.Infrastructure;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.IntegrationTests.TestCases
{
    /// <summary>Reusable by database platforms. Run only against a disposable integration repository.</summary>
    public class LocalAuthenticationTestCases : TestCaseBase
    {
        public Task LocalAuthentication_LoginRefreshRevoke() => IntegrationTestAsync(async () =>
        {
            var name = "local-auth-" + Guid.NewGuid().ToString("N");
            var user = new User(User.Administrator.Parent)
            {
                Name = name, LoginName = name, Enabled = true, Password = "integration-test-password"
            };
            await user.SaveAsync(CancellationToken.None);
            try
            {
                using var rsa = RSA.Create(2048);
                using var server = new TestServer(new WebHostBuilder()
                    .ConfigureServices(services =>
                    {
                        services.AddLogging();
                        services.AddSingleton(Providers.Instance.Services.GetRequiredService<IAccessTokenDataProvider>());
                        services.AddSenseNetLocalAuthentication(options =>
                        {
                            options.Mode = LocalAuthenticationMode.InternalOnly;
                            options.Issuer = "https://integration.example";
                            options.SigningKey = new RsaSecurityKey(rsa) { KeyId = "integration" };
                            options.LoginNetworks = new[] { "127.0.0.0/8" };
                            options.AllowedUserIds = new[] { user.Id };
                            options.RequireMultiFactor = false;
                        });
                    })
                    .Configure(app =>
                    {
                        app.Use((context, next) =>
                        {
                            context.Connection.RemoteIpAddress = IPAddress.Loopback;
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
                using var client = server.CreateClient();
                client.BaseAddress = new Uri("https://integration.example");
                var login = await client.PostAsJsonAsync("/authentication/local/login",
                    new { username = user.Username, password = "integration-test-password" });
                Assert.AreEqual(HttpStatusCode.OK, login.StatusCode);
                var tokens = await login.Content.ReadFromJsonAsync<LocalTokenResponse>();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
                Assert.AreEqual(user.Id.ToString(), await client.GetStringAsync("/protected"));
                var refresh = await client.PostAsJsonAsync("/authentication/local/refresh", new { tokens.RefreshToken });
                Assert.AreEqual(HttpStatusCode.OK, refresh.StatusCode);
                var updated = await refresh.Content.ReadFromJsonAsync<LocalTokenResponse>();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", updated.AccessToken);
                Assert.AreEqual(user.Id.ToString(), await client.GetStringAsync("/protected"));
                Assert.AreEqual(HttpStatusCode.NoContent,
                    (await client.PostAsJsonAsync("/authentication/local/revoke", new { tokens.RefreshToken })).StatusCode);
                Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/protected")).StatusCode);
                Assert.AreEqual(HttpStatusCode.Unauthorized,
                    (await client.PostAsJsonAsync("/authentication/local/refresh", new { tokens.RefreshToken })).StatusCode);
            }
            finally
            {
                await Providers.Instance.Services.GetRequiredService<IAccessTokenDataProvider>()
                    .DeleteAccessTokensAsync(user.Id, 0, "local-auth-session", CancellationToken.None);
                await user.ForceDeleteAsync(CancellationToken.None);
            }
        });
    }
}
