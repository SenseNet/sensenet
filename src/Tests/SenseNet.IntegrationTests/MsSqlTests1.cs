using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.IntegrationTests
{
    [TestClass]
    public class MsSqlTests1 : MsSqlIntegrationTestBase<TestCases1>
    {
        [TestMethod]
        public void MsSql_Experimental1()
        {
            TestCases.TestCase_1();
        }
    }
}
