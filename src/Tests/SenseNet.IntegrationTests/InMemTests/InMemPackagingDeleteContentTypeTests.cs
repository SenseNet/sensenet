using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemPackagingDeleteContentTypeTests : IntegrationTest<InMemPlatform, PackagingDeleteContentTypeTestCases>
    {
        [TestMethod]
        public void IntT_InMem_Packaging_Step_DeleteContentType_Parse() { TestCase.Test_Parse(); }

        [TestMethod]
        public async Task IntT_InMem_Packaging_Step_DeleteContentType_DefaultOrInformationOnly() { await TestCase.Test_DefaultOrInformationOnly().ConfigureAwait(false); }

        [TestMethod]
        public async Task IntT_InMem_Packaging_Step_DeleteContentType_Leaf() { await TestCase.Test_Leaf().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_Packaging_Step_DeleteContentType_Subtree() { await TestCase.Test_Subtree().ConfigureAwait(false); }

        [TestMethod]
        public async Task IntT_InMem_Packaging_Step_DeleteContentType_WithInstances() { await TestCase.Test_WithInstances().ConfigureAwait(false); }

        [TestMethod]
        public async Task IntT_InMem_Packaging_Step_DeleteContentType_WithRelatedContentType() { await TestCase.Test_WithRelatedContentType().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_Packaging_Step_DeleteContentType_WithRelatedFieldSetting() { await TestCase.Test_WithRelatedFieldSetting().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_Packaging_Step_DeleteContentType_WithRelatedContent() { await TestCase.Test_WithRelatedContent().ConfigureAwait(false); }

        [TestMethod]
        public async Task IntT_InMem_Packaging_Step_DeleteContentType_Applications() { await TestCase.Test_Applications().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_Packaging_Step_DeleteContentType_ContentTemplate() { await TestCase.Test_ContentTemplate().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_Packaging_Step_DeleteContentType_ContentView() { await TestCase.Test_ContentView().ConfigureAwait(false); }

        [TestMethod]
        public async Task IntT_InMem_Packaging_Step_DeleteContentType_IfNotUsed() { await TestCase.Test_IfNotUsed().ConfigureAwait(false); }
    }
}
