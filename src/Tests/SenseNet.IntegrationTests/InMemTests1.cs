using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.IntegrationTests
{
    [TestClass]
    public class InMemTests1 : InMemIntegrationTestBase<TestCases1>
    {
        [TestMethod]
        public void InMem_Experimental1()
        {
            TestCases.TestCase_1();
        }
    }
}
