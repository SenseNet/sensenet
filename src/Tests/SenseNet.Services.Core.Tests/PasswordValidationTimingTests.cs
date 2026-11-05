using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Services.Core.Authentication;
using SenseNet.Services.Core.Operations;
using SenseNet.Testing;
using SenseNet.Tests.Core;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.Core.Tests;

[TestClass]
public class PasswordValidationTimingTests : TestBase
{
    [TestMethod]
    public async Task WaitsOnlyForTheRemainderOfTheTarget()
    {
        var clock = new HealthTestClock();
        var attempt = new PasswordValidationTiming(clock).Begin(CancellationToken.None);
        clock.Advance(TimeSpan.FromMilliseconds(350));
        var pending = attempt.DisposeAsync().AsTask();
        Assert.IsFalse(pending.IsCompleted);
        clock.Advance(TimeSpan.FromMilliseconds(549));
        Assert.IsFalse(pending.IsCompleted, "The total duration must be at least 900 ms.");
        clock.Advance(TimeSpan.FromMilliseconds(201));
        await pending.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(0, clock.TimerCount);
    }

    [TestMethod]
    public async Task SlowAttemptsDoNotReceiveAnAdditionalDelay()
    {
        var clock = new HealthTestClock();
        var attempt = new PasswordValidationTiming(clock).Begin(CancellationToken.None);
        clock.Advance(TimeSpan.FromSeconds(2));
        var pending = attempt.DisposeAsync();
        Assert.IsTrue(pending.IsCompletedSuccessfully);
        await pending;
        Assert.AreEqual(0, clock.TimerCount);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task CancellationStopsTheWait(bool alreadyCancelled)
    {
        var clock = new HealthTestClock();
        using var cancellation = new CancellationTokenSource();
        var attempt = new PasswordValidationTiming(clock).Begin(cancellation.Token);
        if (alreadyCancelled) cancellation.Cancel();
        var pending = attempt.DisposeAsync().AsTask();
        if (!alreadyCancelled)
        {
            Assert.IsFalse(pending.IsCompleted);
            cancellation.Cancel();
        }
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => pending);
        Assert.AreEqual(0, clock.TimerCount);
    }

    [TestMethod]
    public async Task ConcurrentAttemptsHaveIndependentDeadlines()
    {
        var clock = new HealthTestClock();
        var timing = new PasswordValidationTiming(clock);
        var first = timing.Begin(CancellationToken.None).DisposeAsync().AsTask();
        clock.Advance(TimeSpan.FromMilliseconds(500));
        var second = timing.Begin(CancellationToken.None).DisposeAsync().AsTask();
        clock.Advance(TimeSpan.FromMilliseconds(600));
        await first.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsFalse(second.IsCompleted);
        clock.Advance(TimeSpan.FromMilliseconds(500));
        await second.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [DataTestMethod]
    [DataRow("unknown", false)]
    [DataRow("disabled", false)]
    [DataRow("wrong", false)]
    [DataRow("throttled", false)]
    [DataRow("success", false)]
    [DataRow("unknown", true)]
    [DataRow("wrong", true)]
    [DataRow("throttled", true)]
    [DataRow("missing-domain", false)]
    public Task CredentialOutcomesWaitBeforeReturningOrThrowing(string outcome, bool twoFactor)
    {
        var clock = new HealthTestClock();
        return Test2(services =>
        {
            services.AddSingleton<TimeProvider>(clock);
            services.AddSingleton<IPasswordHashProvider>(new ElapsingHashProvider(clock));
            services.Configure<PasswordValidationOptions>(options => options.AttemptsPerMinutePerAccount = 1);
        }, async () =>
        {
            using var policy = new Swindler<DomainUsagePolicy>(outcome == "missing-domain"
                    ? DomainUsagePolicy.MandatoryDomain : DomainUsagePolicy.NoDomain,
                () => IdentityManagement.DomainUsagePolicy, value => IdentityManagement.DomainUsagePolicy = value);
            var name = "builtin\\admin";
            var password = outcome == "success" ? "admin" : "wrong";
            if (outcome == "unknown") name = "builtin\\unknown-timing-test";
            if (outcome == "missing-domain") name = "admin";
            if (outcome == "disabled")
            {
                var user = new User(User.Administrator.Parent)
                {
                    Name = "disabled-timing-test", Enabled = false, Password = "Synthetic-Password-2026"
                };
                await user.SaveAsync(CancellationToken.None);
                name = "builtin\\" + user.LoginName;
                password = "Synthetic-Password-2026";
            }
            var context = new DefaultHttpContext { RequestServices = Providers.Instance.Services };
            if (outcome == "throttled")
                context.RequestServices.GetRequiredService<PasswordValidationLimiter>().CheckAttempt(User.Administrator.Id);
            var started = clock.GetTimestamp();
            var pending = twoFactor
                ? IdentityOperations.ValidateCredentialsWithTwoFactorCode(null, context, name, password, "invalid")
                : IdentityOperations.ValidateCredentials(null, context, name, password);
            Assert.IsFalse(pending.IsCompleted);
            Assert.AreEqual(1, clock.TimerCount);
            clock.Advance(TimeSpan.FromMilliseconds(899) - clock.GetElapsedTime(started));
            Assert.IsFalse(pending.IsCompleted, "Neither a result nor an exception should escape before the target.");
            clock.Advance(TimeSpan.FromMilliseconds(201));
            if (outcome == "success")
                Assert.AreEqual(User.Administrator.Id, (await pending.WaitAsync(TimeSpan.FromSeconds(5))).Id);
            else if (outcome == "missing-domain")
                await Assert.ThrowsExceptionAsync<MissingDomainException>(() => pending.WaitAsync(TimeSpan.FromSeconds(5)));
            else
                await Assert.ThrowsExceptionAsync<SenseNetSecurityException>(() => pending.WaitAsync(TimeSpan.FromSeconds(5)));
        });
    }

    private sealed class ElapsingHashProvider(HealthTestClock clock) : SenseNetPasswordHashProvider
    {
        public override bool Check(string text, string hash, IPasswordSaltProvider saltProvider)
        {
            clock.Advance(TimeSpan.FromMilliseconds(500));
            return base.Check(text, hash, saltProvider);
        }
    }
}
