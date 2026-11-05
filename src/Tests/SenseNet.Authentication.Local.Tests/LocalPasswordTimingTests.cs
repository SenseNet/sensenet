using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Services.Core.Authentication;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Authentication.Local.Tests;

public partial class LocalAuthenticationTests
{
    [DataTestMethod]
    [DataRow("unknown", false)]
    [DataRow("disabled", false)]
    [DataRow("wrong", false)]
    [DataRow("throttled", false)]
    [DataRow("success", false)]
    [DataRow("contended", false)]
    [DataRow("unknown", true)]
    [DataRow("disabled", true)]
    [DataRow("wrong", true)]
    [DataRow("throttled", true)]
    [DataRow("success", true)]
    [DataRow("contended", true)]
    public Task LocalLoginWaitsAfterReleasingTheUserLock(string outcome, bool challenge) => Test(async () =>
    {
        var clock = new ControlledDelayClock();
        var name = "builtin\\admin";
        var password = outcome == "success" ? "admin" : "wrong";
        if (outcome == "unknown") name = "builtin\\unknown-timing-test";
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
        using var rsa = RSA.Create(2048);
        using var server = CreateServer(new RsaSecurityKey(rsa) { KeyId = "test" },
            options => options.AttemptsPerMinutePerAccount = 1,
            services: services => services.AddSingleton<TimeProvider>(clock));
        using var client = Client(server);
        if (outcome == "throttled")
            server.Services.GetRequiredService<PasswordValidationLimiter>().CheckAttempt(User.Administrator.Id);
        var userLock = Providers.Instance.Services.GetRequiredService<IUserAuthenticationLock>();
        var held = outcome == "contended"
            ? await userLock.TryAcquireAsync(User.Administrator.Id, CancellationToken.None) : null;
        if (outcome == "contended") Assert.IsNotNull(held);
        try
        {
            var pending = client.PostAsJsonAsync("/authentication/local/login",
                new { username = name, password, requestMfaChallenge = challenge });
            var timer = await clock.Created.Task.WaitAsync(TimeSpan.FromSeconds(10));
            try
            {
                Assert.IsFalse(pending.IsCompleted, "The HTTP response must wait for the target.");
                Assert.IsTrue(timer.DueTime > TimeSpan.Zero && timer.DueTime <= TimeSpan.FromMilliseconds(1100));
                if (held != null) await held.DisposeAsync();
                held = null;
                await using var acquired = await userLock.TryAcquireAsync(User.Administrator.Id, CancellationToken.None);
                Assert.IsNotNull(acquired, "The response delay must not hold the authentication lock.");
            }
            finally { timer.Fire(); }
            using var response = await pending.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.AreEqual(outcome == "success" ? HttpStatusCode.OK : HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.AreEqual(1, clock.TimerCount, "A login must not receive multiple independent response delays.");
        }
        finally { if (held != null) await held.DisposeAsync(); }
    });

    private sealed class ControlledDelayClock : TimeProvider
    {
        // Freeze measured work time; the test explicitly releases the timer after checking the lock.
        public override long GetTimestamp() => 0;
        public TaskCompletionSource<ControlledTimer> Created { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int TimerCount;
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            Interlocked.Increment(ref TimerCount);
            var timer = new ControlledTimer(callback, state, dueTime);
            Created.TrySetResult(timer);
            return timer;
        }
    }

    private sealed class ControlledTimer(TimerCallback callback, object? state, TimeSpan dueTime) : ITimer
    {
        public TimeSpan DueTime { get; } = dueTime;
        public void Fire() => callback(state);
        public bool Change(TimeSpan dueTime, TimeSpan period) => throw new NotSupportedException();
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
