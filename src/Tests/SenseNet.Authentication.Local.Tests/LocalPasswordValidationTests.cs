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
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Authentication.Local.Tests;

public partial class LocalAuthenticationTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public Task ODataAndLocalLoginSharePasswordBudget(bool challenge) => Test2(services => services.AddSenseNetOData(), async () =>
    {
        await Providers.Instance.SecurityHandler.CreateAclEditor()
            .Allow(Repository.Root.Id, User.Visitor.Id, false, PermissionType.See, PermissionType.Open)
            .ApplyAsync(CancellationToken.None);
        using var rsa = RSA.Create(2048);
        using var server = CreateServer(new RsaSecurityKey(rsa) { KeyId = "test" },
            options => options.AttemptsPerMinutePerAccount = 2,
            services: services => services.AddSenseNetOData(),
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
        var odataAttempt = await client.PostAsJsonAsync(odataPath, new { userName = "builtin\\admin", password = "wrong" });
        Assert.AreEqual(HttpStatusCode.Forbidden, odataAttempt.StatusCode, await odataAttempt.Content.ReadAsStringAsync());
        var localAttempt = await client.PostAsJsonAsync("/authentication/local/login",
            new { username = "BuiltIn\\Admin", password = "wrong", requestMfaChallenge = challenge });
        Assert.AreEqual(HttpStatusCode.Unauthorized, localAttempt.StatusCode);

        var localBlocked = await client.PostAsJsonAsync("/authentication/local/login",
            new { username = "builtin\\admin", password = "admin", requestMfaChallenge = challenge });
        var odataBlocked = await client.PostAsJsonAsync(odataPath, new { userName = "builtin\\admin", password = "admin" });
        foreach (var response in new[] { localBlocked, odataBlocked })
        {
            Assert.AreEqual(HttpStatusCode.TooManyRequests, response.StatusCode, await response.Content.ReadAsStringAsync());
            Assert.IsTrue(response.Headers.RetryAfter?.Delta is { TotalSeconds: > 0 and <= 60 });
        }
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
            Assert.AreEqual(HttpStatusCode.TooManyRequests, (await Login(client)).StatusCode);
        }
    });
}
