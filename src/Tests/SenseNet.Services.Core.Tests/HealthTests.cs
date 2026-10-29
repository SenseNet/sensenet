using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SenseNet.ContentRepository.Security.ApiKeys;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Services.Core.Authentication;
using SenseNet.Services.Core.Diagnostics;

namespace SenseNet.Services.Core.Tests;

[TestClass]
public class HealthTests
{
    private static HealthMonitoringOptions Settings() => new() { InitialJitter = TimeSpan.Zero, JitterRatio = 0 };
    private static HealthSnapshotPublisher Publisher(HealthTestClock clock, HealthMonitoringOptions options = null,
        HealthRepositoryLifecycle lifecycle = null, SenseNetStatus status = null) =>
        new(clock, Options.Create(options ?? Settings()), status ?? new SenseNetStatus(), lifecycle ?? new HealthRepositoryLifecycle());
    private static ComponentHealthSnapshot[] Fresh(HealthTestClock clock, HealthColor color = HealthColor.Green) =>
        HealthComponents.Names.Select(name => new ComponentHealthSnapshot(name, HealthCheckState.Available, color,
            Reason: "private reason", Method: "private method", CheckedAtUtc: clock.GetUtcNow(),
            ValidUntilUtc: clock.GetUtcNow().AddSeconds(30))
        {
            MeasuredAt = clock.GetTimestamp(), DetailsAt = clock.GetTimestamp(), DetailsJson = "{\"Configuration\":\"private config\"}",
            DetailsCheckedAtUtc = clock.GetUtcNow(), DetailsValidUntilUtc = clock.GetUtcNow().AddMinutes(10)
        }).ToArray();
    internal static DefaultHttpContext Context(IServiceProvider services = null)
    {
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Method = "GET";
        context.Request.Path = "/health";
        context.Response.Body = new MemoryStream();
        return context;
    }
    internal static JObject Body(HttpContext context) => JObject.Parse(System.Text.Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray()));
    private static void AssertPublic(JObject json)
    {
        CollectionAssert.AreEqual(new[] { "Health" }, json.Properties().Select(p => p.Name).ToArray());
        var health = (JObject)json["Health"];
        CollectionAssert.AreEquivalent(new[] { "Color", "Database", "BlobStorage", "Search", "Identity" }, health.Properties().Select(p => p.Name).ToArray());
        foreach (var name in HealthComponents.Names)
        {
            CollectionAssert.AreEqual(new[] { "Color" }, ((JObject)health[name]).Properties().Select(p => p.Name).ToArray());
            Assert.AreEqual(JTokenType.String, health[name]["Color"].Type);
        }
        Assert.AreEqual(JTokenType.String, health["Color"].Type);
    }

    [TestMethod]
    public async Task Health_AnonymousAndHealthKeyBypassDownstreamWork_1000Requests()
    {
        var clock = new HealthTestClock();
        var snapshots = Publisher(clock);
        snapshots.Publish(Fresh(clock));
        var lifetime = new HealthRepositoryLifecycle();
        lifetime.SetReady();
        var validator = new HealthCheckerKeyValidator(Options.Create(new ApiKeysOptions { HealthCheckerUser = "dedicated-key" }), clock);
        var downstreamCalls = 0;
        var middleware = new HealthEarlyMiddleware(_ => { Interlocked.Increment(ref downstreamCalls); throw new Exception("Database/identity is unavailable"); },
            snapshots, validator, lifetime, clock, Options.Create(Settings()), Options.Create(new AuthenticationOptions()));
        var publication = snapshots.Current;
        await Task.WhenAll(Enumerable.Range(0, 1000).Select(async i =>
        {
            var context = Context();
            if (i % 2 == 1) context.Request.Headers["apikey"] = "dedicated-key";
            await middleware.InvokeAsync(context);
            var json = Body(context);
            if (i % 2 == 0) AssertPublic(json);
            else { Assert.IsNotNull(json["Details"]["Repository_StatusHistory"]); Assert.IsNotNull(json["Repository_Status"]); }
            Assert.AreEqual("no-store", context.Response.Headers.CacheControl.ToString());
            Assert.AreEqual("application/json; charset=utf-8", context.Response.ContentType);
        }));
        Assert.AreEqual(0, downstreamCalls);
        Assert.AreSame(publication, snapshots.Current);
    }

    [TestMethod]
    public async Task Health_FailedCredentialAndExpiredKeyOnlyExposeColors()
    {
        var clock = new HealthTestClock();
        var lifecycle = new HealthRepositoryLifecycle(); lifecycle.SetReady();
        var snapshots = Publisher(clock);
        var validator = new HealthCheckerKeyValidator(Options.Create(new ApiKeysOptions { HealthCheckerUser = "key" }), clock);
        var middleware = new HealthEarlyMiddleware(_ => throw new InvalidOperationException("secret"), snapshots,
            validator, lifecycle, clock, Options.Create(Settings()), Options.Create(new AuthenticationOptions()));
        foreach (var key in new[] { "wrong", "" })
        {
            var context = Context(); context.Request.Headers["apikey"] = key;
            await middleware.InvokeAsync(context); AssertPublic(Body(context));
        }
        clock.Advance(TimeSpan.FromDays(366));
        var expired = Context(); expired.Request.Headers["apikey"] = "key";
        await middleware.InvokeAsync(expired); AssertPublic(Body(expired));
        Assert.AreEqual("Red", (string)Body(expired)["Health"]["Color"]);
        Assert.IsFalse(new HealthCheckerKeyValidator(Options.Create(new ApiKeysOptions()), clock).IsValid(""));
    }

    [TestMethod]
    public async Task Health_PublicAndDetailedColorsMatch_ExpiryAndRecovery()
    {
        var clock = new HealthTestClock();
        var lifecycle = new HealthRepositoryLifecycle(); lifecycle.SetReady();
        var status = new SenseNetStatus { IsRunning = true }; status.SetStatus(SenseNetStatus.Started);
        var snapshots = Publisher(clock, lifecycle: lifecycle, status: status);
        var components = Fresh(clock); components[2] = components[2] with { Color = HealthColor.Red };
        snapshots.Publish(components);
        foreach (var detailed in new[] { false, true })
        {
            var context = Context(); await snapshots.Current.WriteAsync(context, detailed, clock);
            Assert.AreEqual("Red", (string)Body(context)["Health"]["Color"]);
            Assert.AreEqual("Green", (string)Body(context)["Health"]["Database"]["Color"]);
        }
        clock.Advance(TimeSpan.FromSeconds(5));
        var stale = Context(); await snapshots.Current.WriteAsync(stale, true, clock);
        Assert.AreEqual("SnapshotExpired", (string)Body(stale)["Details"]["HealthServiceStatus"]);
        Assert.IsTrue(HealthComponents.Names.All(n => (string)Body(stale)["Health"][n]["Color"] == "Red"));
        snapshots.Publish(Fresh(clock));
        var recovered = Context(); await snapshots.Current.WriteAsync(recovered, false, clock);
        Assert.AreEqual("Green", (string)Body(recovered)["Health"]["Color"]);
    }

    [TestMethod]
    public async Task Health_ComponentAndDetailsExpiry_DoNotRemainGreen()
    {
        var clock = new HealthTestClock();
        var options = Settings(); options.PublisherMaxAge = TimeSpan.FromMinutes(2);
        var lifecycle = new HealthRepositoryLifecycle(); lifecycle.SetReady();
        var publisher = Publisher(clock, options, lifecycle, new SenseNetStatus { IsRunning = true });
        var measured = Fresh(clock);
        publisher.Publish(measured);
        clock.Advance(TimeSpan.FromSeconds(30));
        var expiredWithoutPublication = Context(); await publisher.Current.WriteAsync(expiredWithoutPublication, false, clock);
        Assert.AreEqual("Red", (string)Body(expiredWithoutPublication)["Health"]["Database"]["Color"]);
        publisher.Publish(measured);
        Assert.AreEqual(HealthCheckState.Stale, publisher.Current.Repository.Components["Database"].State);

        var detailsExpired = Fresh(clock);
        detailsExpired[0] = detailsExpired[0] with { DetailsAt = clock.GetTimestamp() - TimeSpan.FromMinutes(11).Ticks };
        publisher.Publish(detailsExpired);
        Assert.AreEqual(HealthColor.Yellow, publisher.Current.Repository.Components["Database"].Color);
    }

    [TestMethod]
    public void Health_StartupAndStoppingNeverReportReady()
    {
        var clock = new HealthTestClock();
        var lifecycle = new HealthRepositoryLifecycle();
        var status = new SenseNetStatus { IsRunning = true }; // Repository started; callback still running.
        var publisher = Publisher(clock, lifecycle: lifecycle, status: status);
        publisher.Publish(Fresh(clock));
        Assert.AreEqual(HealthColor.Yellow, publisher.Current.Repository.Color);
        Assert.IsFalse(publisher.Current.Repository.Running);
        clock.Advance(TimeSpan.FromSeconds(60)); publisher.Publish(Fresh(clock));
        Assert.AreEqual(HealthColor.Red, publisher.Current.Repository.Color);
        lifecycle.SetReady(); publisher.Publish(Fresh(clock));
        Assert.AreEqual(HealthColor.Green, publisher.Current.Repository.Color);
        lifecycle.Stop(); lifecycle.SetReady(); publisher.Publish(Fresh(clock));
        Assert.AreEqual(HealthColor.Red, publisher.Current.Repository.Color);
    }

    [TestMethod]
    public async Task Health_TimeoutKeepsOneWorkerAndIgnoresLateSuccess()
    {
        var clock = new HealthTestClock();
        var release = new TaskCompletionSource<HealthResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var collector = new TestCollector("Database", _ => release.Task);
        var lifecycle = new HealthRepositoryLifecycle(); lifecycle.SetReady();
        var monitor = Monitor(collector, clock, lifecycle);
        using var stop = new CancellationTokenSource();
        var run = monitor.RunAsync(stop.Token);
        await Until(() => collector.Calls == 1 && clock.TimerCount > 0);
        clock.Advance(TimeSpan.FromSeconds(3));
        await Until(() => monitor.Current.State == HealthCheckState.TimedOut);
        clock.Advance(TimeSpan.FromMinutes(5));
        Assert.AreEqual(1, collector.Calls);
        Assert.IsFalse(run.IsCompleted);
        release.SetResult(new HealthResult { Color = HealthColor.Green });
        await Until(() => clock.TimerCount > 0);
        Assert.AreEqual(HealthColor.Red, monitor.Current.Color);
        clock.Advance(TimeSpan.FromSeconds(10));
        await Until(() => collector.Calls == 2 && monitor.Current.Color == HealthColor.Green);
        stop.Cancel(); await run.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task Health_BlockedComponentDoesNotDelayOtherComponentOrShutdown()
    {
        var clock = new HealthTestClock();
        var release = new TaskCompletionSource<HealthResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var blocked = Monitor(new TestCollector("Database", _ => release.Task), clock);
        var healthy = Monitor(new TestCollector("Search", _ => Task.FromResult(new HealthResult { Color = HealthColor.Green })), clock);
        using var stop = new CancellationTokenSource();
        var blockedRun = blocked.MeasureAsync(stop.Token);
        await healthy.MeasureAsync(stop.Token);
        Assert.AreEqual(HealthColor.Green, healthy.Current.Color);
        stop.Cancel();
        try { await blockedRun; Assert.Fail("The cancelled monitor should stop waiting."); }
        catch (OperationCanceledException) { }
        release.SetException(new InvalidOperationException("late fault"));
    }

    [TestMethod]
    public async Task Health_MeasurementIsDetachedFromMutableProviderResult()
    {
        var clock = new HealthTestClock();
        var result = new HealthResult { Color = HealthColor.Green, Reason = "original" };
        var monitor = Monitor(new TestCollector("Database", _ => Task.FromResult(result)), clock);
        await monitor.MeasureAsync(CancellationToken.None);
        var first = monitor.Current;
        result.Color = HealthColor.Red; result.Reason = "changed";
        Assert.AreEqual(HealthColor.Green, first.Color); Assert.AreEqual("original", first.Reason);
    }

    [TestMethod]
    public async Task Health_ConcurrentPublicationKeepsEachResponseConsistent()
    {
        var clock = new HealthTestClock();
        var lifecycle = new HealthRepositoryLifecycle(); lifecycle.SetReady();
        var publisher = Publisher(clock, lifecycle: lifecycle, status: new SenseNetStatus { IsRunning = true });
        publisher.Publish(Fresh(clock));
        var publish = Task.Run(() => { for (var i = 0; i < 100; i++) publisher.Publish(Fresh(clock, i % 2 == 0 ? HealthColor.Red : HealthColor.Green)); });
        await Task.WhenAll(Enumerable.Range(0, 200).Select(async _ =>
        {
            var response = Context(); await publisher.Current.WriteAsync(response, false, clock);
            var health = Body(response)["Health"];
            Assert.IsTrue(HealthComponents.Names.All(n => (string)health[n]["Color"] == (string)health["Color"]));
        }).Append(publish));
    }

    [TestMethod]
    public async Task Health_LatePublicationCannotReplaceNewerShutdownSnapshot()
    {
        var clock = new HealthTestClock();
        var lifecycle = new HealthRepositoryLifecycle(); lifecycle.SetReady();
        var publisher = Publisher(clock, lifecycle: lifecycle, status: new SenseNetStatus { IsRunning = true });
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        IEnumerable<ComponentHealthSnapshot> Slow()
        {
            entered.SetResult(true);
            release.Wait(TimeSpan.FromSeconds(5));
            foreach (var item in Fresh(clock)) yield return item;
        }
        var older = Task.Run(() => publisher.Publish(Slow()));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        lifecycle.Stop();
        publisher.Publish(Fresh(clock));
        var shutdown = publisher.Current;
        release.Set();
        await older;
        Assert.AreSame(shutdown, publisher.Current);
        Assert.AreEqual(HealthColor.Red, publisher.Current.Repository.Color);
    }

    [TestMethod]
    public void Health_DiagnosticsRedactCredentials()
    {
        var json = HealthDataSanitizer.SerializeDetails(new
        {
            Password = "p1", ClientSecret = "s1", ApiKey = "a1",
            Configuration = "Server=test;Password=\"two words\";User ID=someone;",
            Url = "https://user:pass@example.com/", Token = "t1"
        });
        foreach (var secret in new[] { "p1", "s1", "a1", "two words", "someone", "user:pass", "t1" })
            Assert.IsFalse(json.Contains(secret), secret);
    }

    [TestMethod]
    public void Health_OptionsRejectUnsafeDurations()
    {
        Settings().Validate();
        var options = Settings(); options.Database.MaxAge = TimeSpan.FromSeconds(1);
        Assert.ThrowsException<ArgumentException>(() => options.Validate());
        options = Settings(); options.JitterRatio = double.NaN;
        Assert.ThrowsException<ArgumentException>(() => options.Validate());
    }

    private static ComponentHealthMonitor Monitor(IComponentHealthCollector collector, HealthTestClock clock,
        HealthRepositoryLifecycle lifecycle = null) => new(collector, Settings(), clock, new HealthChangeSignal(),
        lifecycle ?? new HealthRepositoryLifecycle(), NullLogger.Instance);
    internal static async Task Until(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition()) await Task.Delay(1, timeout.Token);
    }
    internal sealed class TestCollector(string name, Func<CancellationToken, Task<HealthResult>> collect) : IComponentHealthCollector
    {
        private int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public string ComponentName => name;
        public Task<HealthResult> CollectAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            return collect(cancellationToken);
        }
    }
}
