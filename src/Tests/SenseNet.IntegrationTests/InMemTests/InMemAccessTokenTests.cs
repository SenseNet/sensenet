using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemAccessTokenTests : IntegrationTest<InMemPlatform, AccessTokenTestCases>
    {
        [TestMethod]
        public async Task IntT_InMem_AccessToken_Create_ForUser() { await TestCase.AccessToken_Create_ForUser().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_AccessToken_Create_ForUser_ValueLength() { await TestCase.AccessToken_Create_ForUser_ValueLength().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_AccessToken_Create_ForUser_Twice() { await TestCase.AccessToken_Create_ForUser_Twice().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_AccessToken_Create_ForUserAndContent() { await TestCase.AccessToken_Create_ForUserAndContent().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_AccessToken_Create_ForUserAndFeature() { await TestCase.AccessToken_Create_ForUserAndFeature().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_AccessToken_Create_ForUserContentAndFeature() { await TestCase.AccessToken_Create_ForUserContentAndFeature().ConfigureAwait(false); }

        [TestMethod]
        public async Task IntT_InMem_AccessToken_Get_ForUser() { await TestCase.AccessToken_Get_ForUser().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_AccessToken_Get_ForUserAndContent() { await TestCase.AccessToken_Get_ForUserAndContent().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_AccessToken_Get_ForUserAndFeature() { await TestCase.AccessToken_Get_ForUserAndFeature().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_AccessToken_Get_ForUserContentAndFeature() { await TestCase.AccessToken_Get_ForUserContentAndFeature().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_AccessToken_Get_Expired() { await TestCase.AccessToken_Get_Expired().ConfigureAwait(false); }

        [TestMethod]
        public async Task IntT_InMem_AccessToken_GetByUser() { await TestCase.AccessToken_GetByUser().ConfigureAwait(false); }

        [TestMethod]
        public async Task IntT_InMem_AccessToken_Exists() { await TestCase.AccessToken_Exists().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_AccessToken_Exists_Missing() { await TestCase.AccessToken_Exists_Missing().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_AccessToken_Exists_Expired() { await TestCase.AccessToken_Exists_Expired().ConfigureAwait(false); }

        [TestMethod]
        public async Task IntT_InMem_AccessToken_AssertExists() { await TestCase.AccessToken_AssertExists().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_AccessToken_AssertExists_Missing() { await TestCase.AccessToken_AssertExists_Missing().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_AccessToken_AssertExists_Expired() { await TestCase.AccessToken_AssertExists_Expired().ConfigureAwait(false); }

        [TestMethod]
        public async Task IntT_InMem_AccessToken_Update() { await TestCase.AccessToken_Update().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_AccessToken_UpdateMissing() { await TestCase.AccessToken_UpdateMissing().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_AccessToken_UpdateExpired() { await TestCase.AccessToken_UpdateExpired().ConfigureAwait(false); }

        [TestMethod]
        public async Task IntT_InMem_AccessToken_Delete_Token() { await TestCase.AccessToken_Delete_Token().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_AccessToken_Delete_ByUser() { await TestCase.AccessToken_Delete_ByUser().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_InMem_AccessToken_Delete_ByContent() { await TestCase.AccessToken_Delete_ByContent().ConfigureAwait(false); }
    }
}
