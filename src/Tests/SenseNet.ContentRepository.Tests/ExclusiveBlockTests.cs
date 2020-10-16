using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ExclusiveBlockTests : ExclusiveBlockTestCases
    {
        protected override DataProvider GetMainDataProvider()
        {
            return new InMemoryDataProvider();
        }
        protected override IExclusiveLockDataProviderExtension GetDataProviderExtension()
        {
            return new InMemoryExclusiveLockDataProvider();
        }

        [TestMethod]
        public void ExclusiveBlock_InMem_SkipIfLocked()
        {
            TestCase_SkipIfLocked();
        }
        [TestMethod]
        public void ExclusiveBlock_InMem_WaitForReleased()
        {
            TestCase_WaitForReleased();
        }
        [TestMethod]
        public void ExclusiveBlock_InMem_WaitAndAcquire()
        {
            TestCase_WaitAndAcquire();
        }
    }
}