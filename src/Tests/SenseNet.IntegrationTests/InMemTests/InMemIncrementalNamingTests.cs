using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemIncrementalNamingTests : IntegrationTest<InMemPlatform, IncrementalNamingTestCases>
    {
        [TestMethod]
        public void IntT_InMem_ContentNaming_AllowIncrementalNaming_Allowed() { TestCase.ContentNaming_AllowIncrementalNaming_Allowed(); }
        [TestMethod]
        public void IntT_InMem_ContentNaming_AllowIncrementalNaming_Disallowed() { TestCase.ContentNaming_AllowIncrementalNaming_Disallowed(); }
    }
}
