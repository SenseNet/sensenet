using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemExclusiveLockTests : IntegrationTest<InMemPlatform, ExclusiveLockTestCases>
    {
        [TestMethod]
        public void IntT_InMem_ExclusiveLock_SkipIfLocked() { TestCase.ExclusiveLock_SkipIfLocked(); }
        [TestMethod]
        public void IntT_InMem_ExclusiveLock_WaitForReleased() { TestCase.ExclusiveLock_WaitForReleased(); }
        [TestMethod]
        public void IntT_InMem_ExclusiveLock_WaitAndAcquire() { TestCase.ExclusiveLock_WaitAndAcquire(); }
    }
}
