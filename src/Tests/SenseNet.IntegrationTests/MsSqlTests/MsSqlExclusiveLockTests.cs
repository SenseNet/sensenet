using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.MsSqlTests
{
    [TestClass]
    public class MsSqlExclusiveLockTests : IntegrationTest<MsSqlPlatform, ExclusiveLockTestCases>
    {
        #region MsSql specific infrastructure
        private void ResetDatabase(RepositoryBuilder builder)
        {
            using (var ctx = new MsSqlDataContext(ConnectionStrings.ConnectionString, DataOptions.GetLegacyConfiguration(), CancellationToken.None))
            {
                ctx.ExecuteNonQueryAsync(MsSqlExclusiveLockDataProvider.DropScript).GetAwaiter().GetResult();
                ctx.ExecuteNonQueryAsync(MsSqlExclusiveLockDataProvider.CreationScript).GetAwaiter().GetResult();
            }
        }

        [TestInitialize]
        public void InitializeTest()
        {
            TestCase.TestInitializer = ResetDatabase;
        }
        #endregion

        [TestMethod]
        public void IntT_MsSql_ExclusiveLock_SkipIfLocked() { TestCase.ExclusiveLock_SkipIfLocked(); }
        [TestMethod]
        public void IntT_MsSql_ExclusiveLock_WaitForReleased() { TestCase.ExclusiveLock_WaitForReleased(); }
        [TestMethod]
        public void IntT_MsSql_ExclusiveLock_WaitAndAcquire() { TestCase.ExclusiveLock_WaitAndAcquire(); }
    }
}
