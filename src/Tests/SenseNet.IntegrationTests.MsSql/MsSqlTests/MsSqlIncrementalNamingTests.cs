using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.MsSql.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.MsSql.MsSqlTests
{
    [TestClass]
    public class MsSqlIncrementalNamingTests : IntegrationTest<MsSqlPlatform, IncrementalNamingTestCases>
    {
        [TestMethod]
        public void IntT_MsSql_ContentNaming_AllowIncrementalNaming_Allowed() { TestCase.ContentNaming_AllowIncrementalNaming_Allowed(); }
        [TestMethod]
        public void IntT_MsSql_ContentNaming_AllowIncrementalNaming_Disallowed() { TestCase.ContentNaming_AllowIncrementalNaming_Disallowed(); }
    }
}
