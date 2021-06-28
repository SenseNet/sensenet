using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.MsSqlTests
{
    [TestClass]
    public class MsSqlExclusiveLockTests : IntegrationTest<MsSqlPlatform, ExclusiveLockTestCases>
    {
        [TestMethod]
        public void IntT_MsSql_ExclusiveLock_SkipIfLocked() { TestCase.ExclusiveLock_SkipIfLocked(); }
        [TestMethod]
        public void IntT_MsSql_ExclusiveLock_WaitForReleased() { TestCase.ExclusiveLock_WaitForReleased(); }
        [TestMethod]
        public void IntT_MsSql_ExclusiveLock_WaitAndAcquire() { TestCase.ExclusiveLock_WaitAndAcquire(); }
    }
}
