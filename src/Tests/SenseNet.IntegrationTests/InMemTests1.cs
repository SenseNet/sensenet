using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Extensions.DependencyInjection;

namespace SenseNet.IntegrationTests
{
    [TestClass]
    public class InMemTests1 : InMemIntegrationTestBase<TestCases1>
    {
        protected override RepositoryBuilder ConfigureRepository(RepositoryBuilder builder)
        {
            return (RepositoryBuilder)base.ConfigureRepository(builder)
                .UseTraceCategories("Test", "Event", "Custom", "Database");
        }

        [TestMethod]
        public void InMem_Experimental1()
        {
            TestCases.TestCase_1();
        }
    }
}
