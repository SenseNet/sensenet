using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.MsSqlTests
{
    [TestClass]
    public class MsSqlTests2 : IntegrationTest<MsSqlPlatform, TestCase2>
    {
        [TestMethod]
        public void Integration_MsSql_2_1()
        {
            TestCase.TestCase_2_1();
        }
        [TestMethod]
        public void Integration_MsSql_2_2()
        {
            TestCase.TestCase_2_2();
        }
    }
}
