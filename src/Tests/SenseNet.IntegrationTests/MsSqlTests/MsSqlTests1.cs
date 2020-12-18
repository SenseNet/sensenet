using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.MsSqlTests
{
    [TestClass]
    public class MsSqlTests1 : IntegrationTest<MsSqlPlatform, TestCase1>
    {
        [TestMethod] public void Integration_MsSql_1_1() { TestCase.TestCase_1_1(); }
        [TestMethod] public void Integration_MsSql_1_2() { TestCase.TestCase_1_2(); }
        [TestMethod] public void Integration_MsSql_1_3() { TestCase.TestCase_1_3(); }
        [TestMethod] public void Integration_MsSql_1_4() { TestCase.TestCase_1_4(); }
        [TestMethod] public void Integration_MsSql_1_5() { TestCase.TestCase_1_5(); }
        [TestMethod] public void Integration_MsSql_1_6() { TestCase.TestCase_1_6(); }
        [TestMethod] public void Integration_MsSql_1_7() { TestCase.TestCase_1_7(); }
        [TestMethod] public void Integration_MsSql_1_8() { TestCase.TestCase_1_8(); }
    }
}
