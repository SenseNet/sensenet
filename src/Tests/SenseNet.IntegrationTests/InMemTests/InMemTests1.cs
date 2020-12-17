using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemTests1 : IntegrationTest<InMemPlatform, TestCase1>
    {
        [TestMethod]
        public void Integration_InMem_1_1()
        {
            TestCase.TestCase_1_1();
        }
    }
}
