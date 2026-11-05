using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Services.Core.Authentication;
using SenseNet.Services.Core.Operations;
using SenseNet.Testing;
using SenseNet.Tests.Core;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.Core.Tests;

[TestClass]
public class PasswordValidationTests : TestBase
{
    [TestMethod]
    public void ParallelAttemptsCannotExceedDefaultLimit()
    {
        var limiter = new PasswordValidationLimiter(new HealthTestClock(),
            Options.Create(new PasswordValidationOptions()));
        var accepted = 0;
        Parallel.For(0, 100, _ =>
        {
            try
            {
                limiter.CheckAttempt(1);
                Interlocked.Increment(ref accepted);
            }
            catch (PasswordValidationThrottledException) { }
        });
        Assert.AreEqual(5, accepted);
        limiter.CheckAttempt(2); // Another account has its own budget.
    }

    [TestMethod]
    public void SlidingWindowExpiresIndividualAttemptsAndRejectionsDoNotExtendIt()
    {
        var clock = new HealthTestClock();
        var limiter = new PasswordValidationLimiter(clock,
            Options.Create(new PasswordValidationOptions { AttemptsPerMinutePerAccount = 2 }));
        limiter.CheckAttempt(1);
        clock.Advance(TimeSpan.FromSeconds(30));
        limiter.CheckAttempt(1);
        Assert.AreEqual(30, Assert.ThrowsException<PasswordValidationThrottledException>(
            () => limiter.CheckAttempt(1)).RetryAfterSeconds);
        clock.Advance(TimeSpan.FromSeconds(29.5));
        Assert.AreEqual(1, Assert.ThrowsException<PasswordValidationThrottledException>(
            () => limiter.CheckAttempt(1)).RetryAfterSeconds);
        clock.Advance(TimeSpan.FromSeconds(0.5));
        limiter.CheckAttempt(1); // Only the attempt at t=0 has expired.
        Assert.AreEqual(30, Assert.ThrowsException<PasswordValidationThrottledException>(
            () => limiter.CheckAttempt(1)).RetryAfterSeconds);
        clock.Advance(TimeSpan.FromSeconds(30));
        limiter.CheckAttempt(1);
        clock.Advance(TimeSpan.FromMinutes(2));
        limiter.CheckAttempt(1); // Idle state is reclaimed and the account remains usable.
        limiter.CheckAttempt(1);
    }

    [TestMethod]
    public void RegistrationSharesOneLimiterAcrossRequestScopes()
    {
        var services = new ServiceCollection();
        services.AddSenseNetPasswordValidation(options => options.AttemptsPerMinutePerAccount = 1);
        services.AddSenseNetPasswordValidation();
        using var provider = services.BuildServiceProvider();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var limiter = first.ServiceProvider.GetRequiredService<PasswordValidationLimiter>();
        limiter.CheckAttempt(1);
        Assert.AreSame(limiter, second.ServiceProvider.GetRequiredService<PasswordValidationLimiter>());
        Assert.ThrowsException<PasswordValidationThrottledException>(() =>
            second.ServiceProvider.GetRequiredService<PasswordValidationLimiter>().CheckAttempt(1));
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void InvalidLimitsAreRejected(int limit)
    {
        var services = new ServiceCollection();
        services.AddSenseNetPasswordValidation(options => options.AttemptsPerMinutePerAccount = limit);
        using var provider = services.BuildServiceProvider();
        Assert.ThrowsException<OptionsValidationException>(() => provider.GetRequiredService<PasswordValidationLimiter>());
    }

    [TestMethod]
    public Task AliasesAndTwoFactorValidationShareBudgetBeforeHashing()
    {
        var clock = new HealthTestClock();
        var hashes = new CountingPasswordHashProvider();
        return Test2(services =>
        {
            services.AddSingleton<TimeProvider>(clock);
            services.AddSingleton<IPasswordHashProvider>(hashes);
            services.Configure<PasswordValidationOptions>(options => options.AttemptsPerMinutePerAccount = 2);
        }, async () =>
        {
            using var domainPolicy = new Swindler<DomainUsagePolicy>(DomainUsagePolicy.DefaultDomain,
                () => IdentityManagement.DomainUsagePolicy, value => IdentityManagement.DomainUsagePolicy = value);
            using var defaultDomain = new Swindler<string>("BuiltIn",
                () => IdentityManagement.DefaultDomain, value => IdentityManagement.DefaultDomain = value);
            var user = new User(User.Administrator.Parent)
            {
                Name = "password-limit-test", LoginName = "password-limit-alias",
                Enabled = true, Password = "Synthetic-Password-2026"
            };
            await user.SaveAsync(CancellationToken.None);
            hashes.CheckCount = 0;
            var context = new DefaultHttpContext { RequestServices = Providers.Instance.Services };
            context.Connection.RemoteIpAddress = IPAddress.Loopback;
            Assert.ThrowsException<SenseNetSecurityException>(() => IdentityOperations.ValidateCredentials(
                null, context, "password-limit-alias", "wrong"));
            Assert.AreEqual(1, hashes.CheckCount);
            Assert.AreEqual(user.Id, IdentityOperations.ValidateCredentials(
                null, context, "BuiltIn\\password-limit-alias", "Synthetic-Password-2026").Id);

            context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.1");
            Assert.ThrowsException<SenseNetSecurityException>(() => IdentityOperations.ValidateCredentials(
                null, context, "BUILTIN\\PASSWORD-LIMIT-ALIAS", "Synthetic-Password-2026"));
            await Assert.ThrowsExceptionAsync<SenseNetSecurityException>(() =>
                IdentityOperations.ValidateCredentialsWithTwoFactorCode(null, context,
                    "builtin\\password-limit-alias", "Synthetic-Password-2026", "123456"));
            Assert.AreEqual(2, hashes.CheckCount, "Rejected attempts must not evaluate the password hash.");

            clock.Advance(TimeSpan.FromSeconds(60));
            Assert.AreEqual(user.Id, IdentityOperations.ValidateCredentials(
                null, context, "password-limit-alias", "Synthetic-Password-2026").Id);
            Assert.AreEqual(3, hashes.CheckCount);
        });
    }

    private sealed class CountingPasswordHashProvider : SenseNetPasswordHashProvider
    {
        public int CheckCount;
        public override bool Check(string text, string hash, IPasswordSaltProvider saltProvider)
        {
            Interlocked.Increment(ref CheckCount);
            return base.Check(text, hash, saltProvider);
        }
    }
}
