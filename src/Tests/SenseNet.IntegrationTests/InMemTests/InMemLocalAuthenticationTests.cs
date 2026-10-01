using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Authentication.Local;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemLocalAuthenticationTests : IntegrationTest<InMemPlatform, LocalAuthenticationTestCases>
    {
        [TestMethod]
        public Task IntT_InMem_LocalAuthentication_LoginRefreshRevoke() =>
            TestCase.LocalAuthentication_LoginRefreshRevoke(() => new InMemoryUserLock());

        // This repository exists only in this test process; all its hosts share these gates.
        private sealed class InMemoryUserLock : ILocalAuthenticationUserLock
        {
            private static readonly ConcurrentDictionary<int, SemaphoreSlim> Gates = new();

            public async Task<IAsyncDisposable> TryAcquireAsync(int userId, CancellationToken cancellationToken)
            {
                var gate = Gates.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
                return await gate.WaitAsync(0, cancellationToken) ? new Lease(gate) : null;
            }

            private sealed class Lease(SemaphoreSlim gate) : IAsyncDisposable
            {
                public ValueTask DisposeAsync()
                {
                    gate.Release();
                    return ValueTask.CompletedTask;
                }
            }
        }
    }
}
