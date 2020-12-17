using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.MsSqlTests
{
    [TestClass]
    public class MsSqlTests1 : IntegrationTest<MsSqlPlatform, TestCase1>
    {
        [TestMethod]
        public void Integration_MsSql_1_1()
        {
            TestCase.TestCase_1_1();
        }
    }
}
