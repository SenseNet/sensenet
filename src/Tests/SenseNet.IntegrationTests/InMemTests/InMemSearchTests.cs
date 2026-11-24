using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemSearchTests : IntegrationTest<InMemPlatform, SearchTestCases>
    {
        [TestMethod, TestCategory("Services")]
        public void IntT_InMem_Search_ReferenceField_CSrv()
        {
            TestCase.Search_ReferenceField();
        }

        [TestMethod, TestCategory("Services")]
        public Task IntT_InMem_Search_Bug2184_TextFieldsRoundTripThroughStoredIndexDocument() =>
            TestCase.Search_Bug2184_TextFieldsRoundTripThroughStoredIndexDocument();
    }
}
