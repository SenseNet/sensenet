using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.MsSql.Platforms;
using SenseNet.IntegrationTests.TestCases;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.IntegrationTests.MsSql.MsSqlTests
{
    [TestClass]
    public class MsSqlLocalAuthenticationTests : IntegrationTest<MsSqlPlatform, LocalAuthenticationTestCases>
    {
        [TestMethod]
        public Task IntT_MsSql_LocalAuthentication_LoginRefreshRevoke() =>
            TestCase.LocalAuthentication_LoginRefreshRevoke();
    }
}
