using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemLocalAuthenticationTests : IntegrationTest<InMemPlatform, LocalAuthenticationTestCases>
    {
        [TestMethod]
        public Task IntT_InMem_LocalAuthentication_LoginRefreshRevoke() =>
            TestCase.LocalAuthentication_LoginRefreshRevoke();
    }
}
