﻿using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.MsSqlTests
{
    [TestClass]
    public class MsSqlAccessTokenTests : IntegrationTest<MsSqlPlatform, AccessTokenTestCases>
    {
        [TestMethod]
        public async Task IntT_MsSql_AccessToken_Create_ForUser() { await TestCase.AccessToken_Create_ForUser().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_MsSql_AccessToken_Create_ForUser_ValueLength() { await TestCase.AccessToken_Create_ForUser_ValueLength().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_MsSql_AccessToken_Create_ForUser_Twice() { await TestCase.AccessToken_Create_ForUser_Twice().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_MsSql_AccessToken_Create_ForUserAndContent() { await TestCase.AccessToken_Create_ForUserAndContent().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_MsSql_AccessToken_Create_ForUserAndFeature() { await TestCase.AccessToken_Create_ForUserAndFeature().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_MsSql_AccessToken_Create_ForUserContentAndFeature() { await TestCase.AccessToken_Create_ForUserContentAndFeature().ConfigureAwait(false); }

        [TestMethod]
        public async Task IntT_MsSql_AccessToken_Get_ForUser() { await TestCase.AccessToken_Get_ForUser().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_MsSql_AccessToken_Get_ForUserAndContent() { await TestCase.AccessToken_Get_ForUserAndContent().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_MsSql_AccessToken_Get_ForUserAndFeature() { await TestCase.AccessToken_Get_ForUserAndFeature().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_MsSql_AccessToken_Get_ForUserContentAndFeature() { await TestCase.AccessToken_Get_ForUserContentAndFeature().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_MsSql_AccessToken_Get_Expired() { await TestCase.AccessToken_Get_Expired().ConfigureAwait(false); }

        [TestMethod]
        public async Task IntT_MsSql_AccessToken_GetByUser() { await TestCase.AccessToken_GetByUser().ConfigureAwait(false); }

        [TestMethod]
        public async Task IntT_MsSql_AccessToken_GetOrAdd_WithFeature() { await TestCase.AccessToken_GetOrAdd_WithFeature().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_MsSql_AccessToken_GetOrAdd_WithContent() { await TestCase.AccessToken_GetOrAdd_WithContent().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_MsSql_AccessToken_GetOrAdd_WithUser() { await TestCase.AccessToken_GetOrAdd_WithUser().ConfigureAwait(false); }

        [TestMethod]
        public async Task IntT_MsSql_AccessToken_Exists() { await TestCase.AccessToken_Exists().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_MsSql_AccessToken_Exists_Missing() { await TestCase.AccessToken_Exists_Missing().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_MsSql_AccessToken_Exists_Expired() { await TestCase.AccessToken_Exists_Expired().ConfigureAwait(false); }

        [TestMethod, TestCategory("Services")]
        public async Task IntT_MsSql_AccessToken_AssertExists_CSrv() { await TestCase.AccessToken_AssertExists().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_MsSql_AccessToken_AssertExists_Missing() { await TestCase.AccessToken_AssertExists_Missing().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_MsSql_AccessToken_AssertExists_Expired() { await TestCase.AccessToken_AssertExists_Expired().ConfigureAwait(false); }

        [TestMethod]
        public async Task IntT_MsSql_AccessToken_Update() { await TestCase.AccessToken_Update().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_MsSql_AccessToken_UpdateMissing() { await TestCase.AccessToken_UpdateMissing().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_MsSql_AccessToken_UpdateExpired() { await TestCase.AccessToken_UpdateExpired().ConfigureAwait(false); }

        [TestMethod]
        public async Task IntT_MsSql_AccessToken_Delete_Token() { await TestCase.AccessToken_Delete_Token().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_MsSql_AccessToken_Delete_ByUser() { await TestCase.AccessToken_Delete_ByUser().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_MsSql_AccessToken_Delete_ByContent() { await TestCase.AccessToken_Delete_ByContent().ConfigureAwait(false); }

        [TestMethod]
        public async Task IntT_MsSql_AccessToken_Delete_ByFeature() { await TestCase.AccessToken_Delete_ByFeature().ConfigureAwait(false); }
        [TestMethod]
        public async Task IntT_MsSql_AccessToken_Delete_ByUser_And_Feature() { await TestCase.AccessToken_Delete_ByUser_And_Feature().ConfigureAwait(false); }
    }
}
