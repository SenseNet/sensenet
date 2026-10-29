using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Caching;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Services.Core.Diagnostics;
using SenseNet.Services.Core.Authentication;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Storage.Security;
using SenseNet.Tests.Core;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.Core.Tests;

[TestClass]
public class HealthAccessTests : TestBase
{
    [TestMethod]
    public Task Health_StartupFailureIsPublishedWithoutWaitingForRepository() => Test(async () =>
    {
        var services = Providers.Instance.Services;
        using var repositoryHost = new RepositoryHostedService(services,
            (_, _) => throw new InvalidOperationException("simulated provider initialization failure"), null);
        await repositoryHost.StartAsync(CancellationToken.None);
        var lifecycle = services.GetRequiredService<HealthRepositoryLifecycle>();
        var status = services.GetRequiredService<ISenseNetStatus>();
        await HealthTests.Until(() => status.Current == "Startup failed");
        Assert.IsTrue(lifecycle.FailedOrStopping);
        Assert.IsFalse(status.IsRunning);
        var publisher = services.GetRequiredService<HealthSnapshotPublisher>();
        var context = HealthTests.Context(services);
        await publisher.Current.WriteAsync(context, false, services.GetRequiredService<TimeProvider>());
        Assert.AreEqual("Red", (string)HealthTests.Body(context)["Health"]["Color"]);
    });

    [TestMethod]
    public Task Health_PipelineUsesAuthenticatedUserBeforeAdministratorElevation()
    {
        User authenticated = null;
        return Test2(services =>
        {
            services.AddSingleton<ISnCache, SnMemoryCache>();
            services.Configure<AuthenticationOptions>(options => options.FindUserAsync = _ => Task.FromResult(authenticated));
        }, async () =>
        {
            var services = Providers.Instance.Services;
            var lifecycle = services.GetRequiredService<HealthRepositoryLifecycle>(); lifecycle.SetReady();
            authenticated = new User(Node.LoadNode("/Root/IMS/BuiltIn/Portal")) { Name = "health-pipeline-user", Enabled = true };
            await authenticated.SaveAsync(CancellationToken.None);
            var app = new ApplicationBuilder(services);
            app.UseSenseNetHealthEarly();
            app.UseSenseNetUser();
            app.Use(async (context, next) => { User.Current = User.Administrator; await next(); });
            app.UseSenseNetHealth();
            var pipeline = app.Build();
            var context = HealthTests.Context(services);
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", authenticated.Id.ToString()) }, "test-validated"));
            await pipeline(context);
            Assert.IsNull(HealthTests.Body(context)["Details"], "Later elevation must not authorize the ordinary user.");
            authenticated = User.Administrator;
            var admin = HealthTests.Context(services); admin.User = context.User;
            await pipeline(admin);
            Assert.IsNotNull(HealthTests.Body(admin)["Details"]);
            var anonymous = HealthTests.Context(services);
            await pipeline(anonymous);
            Assert.IsNull(HealthTests.Body(anonymous)["Details"]);
        });
    }

    [TestMethod]
    public Task Health_HostedCollectorsRunWithoutRequestsAndPublishShutdown()
    {
        var clock = new HealthTestClock();
        var custom = new HealthTests.TestCollector("CustomExample", _ => Task.FromResult(new HealthResult { Color = HealthColor.Green }));
        return Test2(services =>
        {
            services.AddSingleton<TimeProvider>(clock);
            services.AddSingleton<IComponentHealthCollector>(custom);
            services.Configure<HealthMonitoringOptions>(options => { options.InitialJitter = TimeSpan.Zero; options.JitterRatio = 0; });
        }, async () =>
        {
            var services = Providers.Instance.Services;
            services.GetRequiredService<HealthRepositoryLifecycle>().SetReady();
            var store = services.GetRequiredService<IHealthSnapshotStore>();
            Assert.AreEqual(HealthCheckState.Unknown, store.Current.Repository.Components["CustomExample"].State);
            var worker = services.GetServices<IHostedService>().OfType<HealthMonitoringHostedService>().Single();
            await worker.StartAsync(CancellationToken.None);
            try
            {
                await HealthTests.Until(() => store.Current.Repository.Components.Values.All(c => c.CheckedAtUtc.HasValue));
                Assert.AreEqual(HealthCheckState.Available, store.Current.Repository.Components["Database"].State);
                Assert.AreEqual(HealthCheckState.NotApplicable, store.Current.Repository.Components["Identity"].State);
                Assert.AreEqual(HealthColor.Green, store.Current.Repository.Components["CustomExample"].Color);
                Assert.AreEqual(1, custom.Calls, "The registered custom collector must run without HTTP requests.");
                await HealthTests.Until(() => store.Current.Repository.Components["Database"].DetailsCheckedAtUtc.HasValue);
                var previous = store.Current.Repository.Components["Database"].CheckedAtUtc;
                // Ensure the monitor has scheduled its next interval before moving manual time.
                await Task.Delay(20);
                clock.Advance(TimeSpan.FromSeconds(10));
                await HealthTests.Until(() => store.Current.Repository.Components["Database"].CheckedAtUtc > previous);
            }
            finally { await worker.StopAsync(CancellationToken.None); }
            Assert.AreEqual(HealthColor.Red, store.Current.Repository.Color);
            worker.Dispose();
        });
    }

    [TestMethod]
    public Task Health_AccessMatrixAndMembershipRevocation() => Test2(services => services.AddSingleton<ISnCache, SnMemoryCache>(), async () =>
    {
        var lifecycle = new HealthRepositoryLifecycle(); lifecycle.SetReady();
        var access = new HealthAccessEvaluator(Providers.Instance.CacheProvider, Providers.Instance.SecurityHandler, lifecycle);
        var parent = Node.LoadNode("/Root/IMS/BuiltIn/Portal");
        var developerGroup = Node.Load<Group>(N.R.Developers);
        if (developerGroup == null)
        {
            developerGroup = new Group(parent) { Name = "Developers" };
            await developerGroup.SaveAsync(CancellationToken.None);
        }
        // Initial data nests Developers under Administrators. Test its independent grant.
        Group.Administrators.RemoveMember(developerGroup);
        var user = new User(parent) { Name = "health-user", Enabled = true };
        await user.SaveAsync(CancellationToken.None);
        var nested = new Group(parent) { Name = "health-nested" };
        await nested.SaveAsync(CancellationToken.None);
        var context = HealthTests.Context();

        Assert.IsFalse(access.CanReadDetails(context), "Ambient administrator must not grant access.");
        HealthAccessEvaluator.SetAuthenticatedUser(context, HealthCheckerUser.Instance);
        Assert.IsTrue(access.CanReadDetails(context));
        HealthAccessEvaluator.SetAuthenticatedUser(context, User.Administrator);
        Assert.IsTrue(access.CanReadDetails(context));
        HealthAccessEvaluator.SetAuthenticatedUser(context, user);
        access.RefreshDeveloperGroup();
        Assert.IsFalse(access.CanReadDetails(context));
        developerGroup.AddMember(user);
        access.RefreshDeveloperGroup();
        Assert.IsTrue(access.CanReadDetails(context), "Direct Developers membership.");
        developerGroup.RemoveMember(user);
        access.RefreshDeveloperGroup();
        Assert.IsFalse(access.CanReadDetails(context), "Revoked membership.");
        nested.AddMember(user);
        developerGroup.AddMember(nested);
        access.RefreshDeveloperGroup();
        Assert.IsTrue(access.CanReadDetails(context), "Transitive Developers membership.");
        developerGroup.RemoveMember(nested);
        access.RefreshDeveloperGroup();
        Assert.IsFalse(access.CanReadDetails(context));

        var administrators = Group.Administrators;
        administrators.AddMember(nested);
        Assert.IsTrue(access.CanReadDetails(context), "Transitive Administrators membership.");
        administrators.RemoveMember(nested);
        Assert.IsFalse(access.CanReadDetails(context));
        lifecycle.Stop();
        HealthAccessEvaluator.SetAuthenticatedUser(context, User.Administrator);
        Assert.IsFalse(access.CanReadDetails(context), "Unavailable repository only exposes colors to normal users.");
        HealthAccessEvaluator.SetAuthenticatedUser(context, HealthCheckerUser.Instance);
        Assert.IsTrue(access.CanReadDetails(context), "Dedicated health identity survives repository shutdown.");
    });

    [TestMethod]
    public Task Health_DevelopersRenameInvalidatesResolvedId() => Test2(services => services.AddSingleton<ISnCache, SnMemoryCache>(), async () =>
    {
        var parent = Node.LoadNode("/Root/IMS/BuiltIn/Portal");
        var developers = Node.Load<Group>(N.R.Developers);
        if (developers == null)
        {
            developers = new Group(parent) { Name = "Developers" };
            await developers.SaveAsync(CancellationToken.None);
        }
        Group.Administrators.RemoveMember(developers);
        var user = new User(parent) { Name = "health-rename-user", Enabled = true };
        await user.SaveAsync(CancellationToken.None);
        developers.AddMember(user);
        var lifecycle = new HealthRepositoryLifecycle(); lifecycle.SetReady();
        var access = new HealthAccessEvaluator(Providers.Instance.CacheProvider, Providers.Instance.SecurityHandler, lifecycle);
        access.RefreshDeveloperGroup();
        var context = HealthTests.Context(); HealthAccessEvaluator.SetAuthenticatedUser(context, user);
        Assert.IsTrue(access.CanReadDetails(context));
        developers.Name = "FormerDevelopers";
        await developers.SaveAsync(CancellationToken.None);
        Assert.AreEqual("/Root/IMS/BuiltIn/Portal/FormerDevelopers", developers.Path);
        Assert.IsFalse(Providers.Instance.SecurityHandler.IsInGroup(user.Id, Identifiers.AdministratorsGroupId), "Test user must not be administrator.");
        Assert.IsNull(Providers.Instance.CacheProvider.Get("SenseNet.Health.DevelopersGroup"), "Resolved id is still cached after rename.");
        Assert.IsFalse(access.CanReadDetails(context), "Cached group id must be invalidated before background refresh.");
        access.RefreshDeveloperGroup();
        Assert.IsFalse(access.CanReadDetails(context));
    });
}
