using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemTests2 : IntegrationTest<InMemPlatform, TestCase2>
    {
        [TestMethod] public void Integration_InMem_2_1() { TestCase.TestCase_2_1(); }
        [TestMethod] public void Integration_InMem_2_2() { TestCase.TestCase_2_2(); }
        [TestMethod] public void Integration_InMem_2_3() { TestCase.TestCase_2_3(); }
        [TestMethod] public void Integration_InMem_2_4i() { TestCase.TestCase_2_4i(); }
        [TestMethod] public void Integration_InMem_2_5() { TestCase.TestCase_2_5(); }
        [TestMethod] public void Integration_InMem_2_6() { TestCase.TestCase_2_6(); }
        [TestMethod] public void Integration_InMem_2_7() { TestCase.TestCase_2_7(); }
        [TestMethod] public void Integration_InMem_2_8() { TestCase.TestCase_2_8(); }
    }
}
