using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;

namespace SenseNet.IntegrationTests.Demo
{
    [TestClass]
    public class DemoInMemTests1 : IntegrationTest<InMemPlatform, DemoTestCase1>
    {
        [TestMethod] public void IntegrationDemo_InMem_1_1() { TestCase.TestCase_1_1(); }
        [TestMethod] public void IntegrationDemo_InMem_1_2() { TestCase.TestCase_1_2(); }
        [TestMethod] public void IntegrationDemo_InMem_1_3() { TestCase.TestCase_1_3(); }
        [TestMethod] public void IntegrationDemo_InMem_1_4() { TestCase.TestCase_1_4(); }
        [TestMethod] public void IntegrationDemo_InMem_1_5() { TestCase.TestCase_1_5(); }
        [TestMethod] public void IntegrationDemo_InMem_1_6() { TestCase.TestCase_1_6(); }
        [TestMethod] public void IntegrationDemo_InMem_1_7() { TestCase.TestCase_1_7(); }
        [TestMethod] public void IntegrationDemo_InMem_1_8() { TestCase.TestCase_1_8(); }
    }
}
