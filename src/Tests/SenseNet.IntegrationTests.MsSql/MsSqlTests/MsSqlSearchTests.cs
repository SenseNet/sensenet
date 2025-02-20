using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.MsSql.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.MsSql.MsSqlTests
{
    [TestClass]
    public class MsSqlSearchTests : IntegrationTest<MsSqlPlatform, SearchTestCases>
    {
        [TestMethod, TestCategory("Services")]
        public void IntT_MsSql_Search_ReferenceField_CSrv()
        {
            TestCase.Search_ReferenceField();
        }

        //[TestMethod, TestCategory("Services")]
        public void IntT_MsSql_Search_Bug2184_IndexDocumentDeserialization_InvalidEscapeSequence()
        {
            TestCase.Search_Bug2184_IndexDocumentDeserialization_InvalidEscapeSequence().GetAwaiter().GetResult();
        }
    }
}
