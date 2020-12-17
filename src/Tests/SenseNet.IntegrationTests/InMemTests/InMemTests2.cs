using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemTests2 : IntegrationTest<InMemPlatform, TestCase2>
    {
        [TestMethod]
        public void Integration_InMem_2_1()
        {
            TestCase.TestCase_2_1();
        }
        [TestMethod]
        public void Integration_InMem_2_2()
        {
            TestCase.TestCase_2_2();
        }
    }
}
