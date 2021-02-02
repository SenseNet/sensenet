using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;
using SenseNet.Packaging;
using SenseNet.Testing;
using Logger = SenseNet.IntegrationTests.Infrastructure.Logger;

namespace SenseNet.IntegrationTests.MsSqlTests
{
    //UNDONE:<?: Move PackagingTestLogger to a common folder e.g. "SenseNet.IntegrationTests/Implementations"
    public class PackagingTestLogger : IPackagingLogger
    {
        public LogLevel AcceptedLevel => LogLevel.File;
        private readonly StringBuilder _sb;

        public string LogFilePath => "[in memory]";

        public PackagingTestLogger(StringBuilder sb)
        {
            _sb = sb;
        }

        public void Initialize(LogLevel level, string logFilePath) { }
        public void WriteTitle(string title)
        {
            _sb.AppendLine("================================");
            _sb.AppendLine(title);
            _sb.AppendLine("================================");
        }
        public void WriteMessage(string message)
        {
            _sb.AppendLine(message);
        }
    }

    [TestClass]
    public class PatchingMsSqlTests : IntegrationTest<MsSqlPlatform, PatchingTestCases>
    {
        #region MsSql specific infrastructure

        private static StringBuilder _log;
        private static MsSqlDataProvider Db => (MsSqlDataProvider)Providers.Instance.DataProvider;

        private void InitializePackagingTest(RepositoryBuilder builder)
        {
            // preparing logger
            _log = new StringBuilder();
            var loggers = new[] { new PackagingTestLogger(_log) };
            var loggerAcc = new TypeAccessor(typeof(SenseNet.Packaging.Logger));
            loggerAcc.SetStaticField("_loggers", loggers);

            //// set default implementation directly
            //var sqlDb = new MsSqlDataProvider();
            //Providers.Instance.DataProvider = sqlDb;

            //// build database
            //var builder = new RepositoryBuilder();
            //builder.UsePackagingDataProviderExtension(new MsSqlPackagingDataProvider());

            //// preparing database
            //ConnectionStrings.ConnectionString = SenseNet.IntegrationTests.Common.ConnectionStrings.ForPackagingTests;

            using (var ctx = new MsSqlDataContext(CancellationToken.None))
            {
                DropPackagesTable(ctx);
                InstallPackagesTable(ctx);
            }

            RepositoryVersionInfo.Reset();
        }

        internal static void DropPackagesTable(SnDataContext ctx)
        {
            var sql = @"
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Packages]') AND type in (N'U'))
DROP TABLE [dbo].[Packages]
";
            ctx.ExecuteNonQueryAsync(sql).GetAwaiter().GetResult();
        }
        internal static void InstallPackagesTable(SnDataContext ctx)
        {
            var sql = @"
CREATE TABLE [dbo].[Packages](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [PackageType] [varchar](50) NOT NULL,
    [ComponentId] [nvarchar](450) NULL,
    [ComponentVersion] [varchar](50) NULL,
    [ReleaseDate] [datetime2](7) NOT NULL,
    [ExecutionDate] [datetime2](7) NOT NULL,
    [ExecutionResult] [varchar](50) NOT NULL,
    [ExecutionError] [nvarchar](max) NULL,
    [Description] [nvarchar](1000) NULL,
    [Manifest] [nvarchar](max) NULL,
 CONSTRAINT [PK_Packages] PRIMARY KEY CLUSTERED 
(
    [Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
";

            ctx.ExecuteNonQueryAsync(sql).GetAwaiter().GetResult();
        }

        [TestInitialize]
        public void InitializeTest()
        {
            TestCase.TestInitializer = InitializePackagingTest;
        }

        #endregion


        [TestMethod]
        public void IntT_MsSql_PatchingSystem_InstalledComponents()
        {
            TestCase.PatchingSystem_InstalledComponents();
        }
        [TestMethod]
        public void IntT_MsSql_PatchingSystem_InstalledComponents_Descriptions()
        {
            TestCase.PatchingSystem_InstalledComponents_Descriptions();
        }

        /* ===================================================================== INFRASTRUCTURE TESTS */

        [TestMethod]
        public void IntT_MsSql_Patching_System_SaveAndReloadFaultyInstaller()
        {
            TestCase.Patching_System_SaveAndReloadFaultyInstaller();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_System_ReSaveAndReloadInstaller()
        {
            TestCase.Patching_System_ReSaveAndReloadInstaller();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_System_SaveAndReloadSnPatch()
        {
            TestCase.Patching_System_SaveAndReloadSnPatch();
        }

        [TestMethod]
        public void IntT_MsSql_Patching_System_SaveAndReload_Installer_FaultyBefore()
        {
            TestCase.Patching_System_SaveAndReload_Installer_FaultyBefore();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_System_SaveAndReload_Installer_SuccessfulBefore()
        {
            TestCase.Patching_System_SaveAndReload_Installer_SuccessfulBefore();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_System_SaveAndReload_SnPatch_FaultyBefore()
        {
            TestCase.Patching_System_SaveAndReload_SnPatch_FaultyBefore();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_System_SaveAndReload_SnPatch_SuccessfulBefore()
        {
            TestCase.Patching_System_SaveAndReload_SnPatch_SuccessfulBefore();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_System_SaveAndReloadExecutionError()
        {
            TestCase.Patching_System_SaveAndReloadExecutionError();
        }

        [TestMethod]
        public void IntT_MsSql_Patching_System_InstalledComponents()
        {
            TestCase.Patching_System_InstalledComponents();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_System_InstalledComponents_Descriptions()
        {
            TestCase.Patching_System_InstalledComponents_Descriptions();
        }

        [TestMethod]
        public void IntT_MsSql_Patching_System_LoadInstalledComponents()
        {
            TestCase.Patching_System_LoadInstalledComponents();
        }

        [TestMethod]
        public void IntT_MsSql_Patching_System_Load_Issue1174()
        {
            TestCase.Patching_System_Load_Issue1174();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_System_Load_Issue1174_All_Installers()
        {
            TestCase.Patching_System_Load_Issue1174_All_Installers();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_System_Load_Issue1174_All_Patches()
        {
            TestCase.Patching_System_Load_Issue1174_All_Patches();
        }


        /* ===================================================================== EXECUTION TESTS */

        [TestMethod]
        public void IntT_MsSql_Patching_Exec_NoAction()
        {
            TestCase.Patching_Exec_NoAction();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_Exec_NoAfterAction()
        {
            TestCase.Patching_Exec_NoAfterAction();
        }

        [TestMethod]
        public void IntT_MsSql_Patching_Exec_InstallOne_Success()
        {
            TestCase.Patching_Exec_InstallOne_Success();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_Exec_InstallOne_FaultyBefore()
        {
            TestCase.Patching_Exec_InstallOne_FaultyBefore();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_Exec_PatchOne_Success()
        {
            TestCase.Patching_Exec_PatchOne_Success();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_Exec_PatchOne_Faulty()
        {
            TestCase.Patching_Exec_PatchOne_Faulty();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_Exec_SkipPatch_FaultyInstaller()
        {
            TestCase.Patching_Exec_SkipPatch_FaultyInstaller();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_Exec_SkipPatch_FaultySnPatch()
        {
            TestCase.Patching_Exec_SkipPatch_FaultySnPatch();
        }

        [TestMethod]
        public void IntT_MsSql_Patching_Exec_SkipPatch_MoreFaultyChains()
        {
            TestCase.Patching_Exec_SkipPatch_MoreFaultyChains();
        }

        [TestMethod]
        public void IntT_MsSql_Patching_Exec_WaitForDependency_WaitingBeforeAndAfter()
        {
            TestCase.Patching_Exec_WaitForDependency_WaitingBeforeAndAfter();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_Exec_InstallerIsLast()
        {
            TestCase.Patching_Exec_InstallerIsLast();
        }

        /* ===================================================================== EXECUTION VS VERSIONINFO TESTS */

        [TestMethod]
        public void IntT_MsSql_Patching_Exec_ComponentLifeCycleVsVersionInfo()
        {
            TestCase.Patching_Exec_ComponentLifeCycleVsVersionInfo();
        }

        /* ======================================================================= CONDITIONAL EXECUTION TESTS */

        // Patch vary component versions conditionally
        [TestMethod]
        public void IntT_MsSql_Patching_Exec_ConditionalActions_a()
        {
            TestCase.Patching_Exec_ConditionalActions_a();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_Exec_ConditionalActions_b()
        {
            TestCase.Patching_Exec_ConditionalActions_b();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_Exec_ConditionalActions_c()
        {
            TestCase.Patching_Exec_ConditionalActions_c();
        }

        [TestMethod]
        public void IntT_MsSql_Patching_Exec_ConditionalActions_AllConditions_a()
        {
            TestCase.Patching_Exec_ConditionalActions_AllConditions_a();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_Exec_ConditionalActions_AllConditions_b()
        {
            TestCase.Patching_Exec_ConditionalActions_AllConditions_b();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_Exec_ConditionalActions_AllConditions_c()
        {
            TestCase.Patching_Exec_ConditionalActions_AllConditions_c();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_Exec_ConditionalActions_AllConditions_d()
        {
            TestCase.Patching_Exec_ConditionalActions_AllConditions_d();
        }
        [TestMethod]
        public void IntT_MsSql_Patching_Exec_ConditionalActions_AllConditions_e()
        {
            TestCase.Patching_Exec_ConditionalActions_AllConditions_e();
        }
    }
}
