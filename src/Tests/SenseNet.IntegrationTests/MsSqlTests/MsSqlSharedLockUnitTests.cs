using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.MsSqlTests
{
    [TestClass]
    public class MsSqlSharedLockUnitTests : IntegrationTest<MsSqlPlatform, SharedLockUnitTestsCases>
    {
        [TestMethod]
        public async Task UT_MsSql_SharedLock_LockAndGetLock() { await TestCase.SharedLock_LockAndGetLock().ConfigureAwait(false); }
        [TestMethod]
        public async Task UT_MsSql_SharedLock_GetTimedOut() { await TestCase.SharedLock_GetTimedOut().ConfigureAwait(false); }
        [TestMethod]
        public async Task UT_MsSql_SharedLock_Lock_Same() { await TestCase.SharedLock_Lock_Same().ConfigureAwait(false); }
        [TestMethod]
        [ExpectedException(typeof(LockedNodeException))]
        public async Task UT_MsSql_SharedLock_Lock_Different() { await TestCase.SharedLock_Lock_Different().ConfigureAwait(false); }
        [TestMethod]
        public async Task UT_MsSql_SharedLock_Lock_DifferentTimedOut() { await TestCase.SharedLock_Lock_DifferentTimedOut().ConfigureAwait(false); }


        [TestMethod]
        public async Task UT_MsSql_SharedLock_ModifyLock() { await TestCase.SharedLock_ModifyLock().ConfigureAwait(false); }
        [TestMethod]
        [ExpectedException(typeof(LockedNodeException))]
        public async Task UT_MsSql_SharedLock_ModifyLockDifferent() { await TestCase.SharedLock_ModifyLockDifferent().ConfigureAwait(false); }
        [TestMethod]
        [ExpectedException(typeof(SharedLockNotFoundException))]
        public async Task UT_MsSql_SharedLock_ModifyLock_Missing() { await TestCase.SharedLock_ModifyLock_Missing().ConfigureAwait(false); }
        [TestMethod]
        [ExpectedException(typeof(SharedLockNotFoundException))]
        public async Task UT_MsSql_SharedLock_ModifyLock_TimedOut() { await TestCase.SharedLock_ModifyLock_TimedOut().ConfigureAwait(false); }


        [TestMethod]
        public async Task UT_MsSql_SharedLock_RefreshLock() { await TestCase.SharedLock_RefreshLock().ConfigureAwait(false); }
        [TestMethod]
        [ExpectedException(typeof(LockedNodeException))]
        public async Task UT_MsSql_SharedLock_RefreshLock_Different() { await TestCase.SharedLock_RefreshLock_Different().ConfigureAwait(false); }
        [TestMethod]
        [ExpectedException(typeof(SharedLockNotFoundException))]
        public async Task UT_MsSql_SharedLock_RefreshLock_Missing() { await TestCase.SharedLock_RefreshLock_Missing().ConfigureAwait(false); }
        [TestMethod]
        [ExpectedException(typeof(SharedLockNotFoundException))]
        public async Task UT_MsSql_SharedLock_RefreshLock_TimedOut() { await TestCase.SharedLock_RefreshLock_TimedOut().ConfigureAwait(false); }


        [TestMethod]
        public async Task UT_MsSql_SharedLock_Unlock() { await TestCase.SharedLock_Unlock().ConfigureAwait(false); }
        [TestMethod]
        [ExpectedException(typeof(LockedNodeException))]
        public async Task UT_MsSql_SharedLock_Unlock_Different() { await TestCase.SharedLock_Unlock_Different().ConfigureAwait(false); }
        [TestMethod]
        [ExpectedException(typeof(SharedLockNotFoundException))]
        public async Task UT_MsSql_SharedLock_Unlock_Missing() { await TestCase.SharedLock_Unlock_Missing().ConfigureAwait(false); }
        [TestMethod]
        [ExpectedException(typeof(SharedLockNotFoundException))]
        public async Task UT_MsSql_SharedLock_Unlock_TimedOut() { await TestCase.SharedLock_Unlock_TimedOut().ConfigureAwait(false); }
    }
}
