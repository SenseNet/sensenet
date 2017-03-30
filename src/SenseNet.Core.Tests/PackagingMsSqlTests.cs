using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Packaging;
using SenseNet.Packaging.Steps;

namespace SenseNet.Core.Tests
{
    public class TestStepThatCreatesThePackagingTable : Step
    {
        public override void Execute(ExecutionContext context)
        {
            PackagingMsSqlTests.InstallPackagesTable();
        }
    }

    [TestClass]
    public class PackagingMsSqlTests
    {
        private static readonly string ConnectionString =
            //"Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=sensenet;Data Source=(local)";
            "Data Source=.;Initial Catalog=sensenet;User ID=sa;Password=sa;Pooling=False";

        private static readonly string DropPackagesTableSql = @"
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Packages]') AND type in (N'U'))
DROP TABLE [dbo].[Packages]
";

        private static readonly string InstallPackagesTableSql = @"
CREATE TABLE [dbo].[Packages](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[PackageType] [varchar](50) NOT NULL,
	[PackageLevel] [varchar](50) NOT NULL,
	[SenseNetVersion] [varchar](50) NOT NULL,
	[AppId] [varchar](50) NULL,
	[AppVersion] [varchar](50) NULL,
	[ReleaseDate] [datetime] NOT NULL,
	[ExecutionDate] [datetime] NOT NULL,
	[ExecutionResult] [varchar](50) NOT NULL,
	[ExecutionError] [nvarchar](max) NULL,
	[Description] [nvarchar](1000) NULL,
 CONSTRAINT [PK_Packages] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
";

        private static StringBuilder _log;

        [ClassInitialize]
        public static void InitializeDatabase(TestContext context)
        {
            DropPackagesTable();
            InstallPackagesTable();
        }
        [TestInitialize]
        public void InitializeTest()
        {
            // preparing logger
            _log = new StringBuilder();
            var loggers = new[] { new PackagingTestLogger(_log) };
            var loggerAcc = new PrivateType(typeof(Logger));
            loggerAcc.SetStaticField("_loggers", loggers);

            // preparing database
            ConnectionStrings.ConnectionString = ConnectionString;
            var proc = DataProvider.CreateDataProcedure("DELETE FROM [Packages]");
            proc.CommandType = CommandType.Text;
            proc.ExecuteNonQuery();

            RepositoryVersionInfo.Reset();
        }

        // ========================================= Checking dependency tests

        [TestMethod]
        public void Packaging_SQL_DependencyCheck_MissingDependency()
        {
            new PackagingTests().Packaging_DependencyCheck_MissingDependency();
        }
        [TestMethod]
        public void Packaging_SQL_DependencyCheck_CannotInstallExistingComponent()
        {
            new PackagingTests().Packaging_DependencyCheck_CannotInstallExistingComponent();
        }
        [TestMethod]
        public void Packaging_SQL_DependencyCheck_CannotUpdateMissingComponent()
        {
            new PackagingTests().Packaging_DependencyCheck_CannotUpdateMissingComponent();
        }
        [TestMethod]
        public void Packaging_SQL_DependencyCheck_TargetVersionTooSmall()
        {
            new PackagingTests().Packaging_DependencyCheck_TargetVersionTooSmall();
        }

        [TestMethod]
        public void Packaging_SQL_DependencyCheck_DependencyVersion()
        {
            new PackagingTests().Packaging_DependencyCheck_DependencyVersion();
        }

        [TestMethod]
        public void Packaging_SQL_DependencyCheck_DependencyMinimumVersion()
        {
            new PackagingTests().Packaging_DependencyCheck_DependencyMinimumVersion();
        }

        [TestMethod]
        public void Packaging_SQL_DependencyCheck_DependencyMaximumVersion()
        {
            new PackagingTests().Packaging_DependencyCheck_DependencyMaximumVersion();
        }

        [TestMethod]
        public void Packaging_SQL_DependencyCheck_DependencyMinimumVersionExclusive()
        {
            new PackagingTests().Packaging_DependencyCheck_DependencyMinimumVersionExclusive();
        }
        [TestMethod]
        public void Packaging_SQL_DependencyCheck_DependencyMaximumVersionExclusive()
        {
            new PackagingTests().Packaging_DependencyCheck_DependencyMaximumVersionExclusive();
        }

        [TestMethod]
        public void Packaging_SQL_DependencyCheck_LoggingDependencies()
        {
            new PackagingTests().DependencyCheckLoggingDependencies(_log);
        }

        // ========================================= Component lifetime tests

        [TestMethod]
        public void Packaging_SQL_Install_SnInitialComponent()
        {
            // simulate database before installation
            DropPackagesTable();

            // accessing versioninfo does not throw any error
            var verInfo = RepositoryVersionInfo.Instance;

            // there is no any app or package
            Assert.AreEqual(0, verInfo.Applications.Count());
            Assert.AreEqual(0, verInfo.InstalledPackages.Count());

            var manifestXml = new XmlDocument();
            manifestXml.LoadXml(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component42</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>4.42</Version>
                            <Steps>
                                <Phase>
                                    <Trace>Installing database.</Trace>
                                    <TestStepThatCreatesThePackagingTable />
                                </Phase>
                                <Phase><Trace>Installing first component.</Trace></Phase>
                            </Steps>
                        </Package>");
            ApplicationInfo app;
            Package pkg;

            // phase 1 (with step that simulates the installing database)
            PackagingTests.ExecutePhase(manifestXml, 0);

            // validate state after phase 1
            verInfo = RepositoryVersionInfo.Instance;
            Assert.AreEqual(0, verInfo.Applications.Count());
            Assert.AreEqual(1, verInfo.InstalledPackages.Count());
            pkg = verInfo.InstalledPackages.First();
            Assert.AreEqual("Component42", pkg.AppId);
            Assert.AreEqual(ExecutionResult.Unfinished, pkg.ExecutionResult);
            Assert.AreEqual(PackageLevel.Install, pkg.PackageLevel);
            Assert.AreEqual("4.42", pkg.ApplicationVersion.ToString());

            // phase 2
            PackagingTests.ExecutePhase(manifestXml, 1);

            // validate state after phase 2
            verInfo = RepositoryVersionInfo.Instance;
            Assert.AreEqual(1, verInfo.Applications.Count());
            Assert.AreEqual(1, verInfo.InstalledPackages.Count());
            app = verInfo.Applications.First();
            Assert.AreEqual("Component42", app.AppId);
            Assert.AreEqual("4.42", app.Version.ToString());
            Assert.IsNotNull(app.AcceptableVersion);
            Assert.AreEqual("4.42", app.AcceptableVersion.ToString());
            pkg = verInfo.InstalledPackages.First();
            Assert.AreEqual("Component42", pkg.AppId);
            Assert.AreEqual(ExecutionResult.Successful, pkg.ExecutionResult);
            Assert.AreEqual(PackageLevel.Install, pkg.PackageLevel);
            Assert.AreEqual("4.42", pkg.ApplicationVersion.ToString());
        }

        [TestMethod]
        public void Packaging_SQL_Install_NoSteps()
        {
            new PackagingTests().Packaging_Install_NoSteps();
        }

        [TestMethod]
        public void Packaging_SQL_Install_ThreePhases()
        {
            new PackagingTests().Packaging_Install_ThreePhases();
        }

        [TestMethod]
        public void Packaging_SQL_Patch_ThreePhases()
        {
            new PackagingTests().Packaging_Patch_ThreePhases();
        }


        [TestMethod]
        public void Packaging_SQL_Patch_Faulty()
        {
            new PackagingTests().Packaging_Patch_Faulty();
        }

        [TestMethod]
        public void Packaging_SQL_Patch_FixFaulty()
        {
            new PackagingTests().Packaging_Patch_FixFaulty();
        }

        [TestMethod]
        public void Packaging_SQL_Patch_FixMoreFaulty()
        {
            new PackagingTests().Packaging_Patch_FixMoreFaulty();
        }

        // ========================================= RepositoryVersionInfo queries

        [TestMethod]
        public void Packaging_SQL_VersionInfo_Empty()
        {
            new PackagingTests().Packaging_VersionInfo_Empty();
        }
        [TestMethod]
        public void Packaging_SQL_VersionInfo_OnlyUnfinished()
        {
            new PackagingTests().Packaging_VersionInfo_OnlyUnfinished();
        }
        [TestMethod]
        public void Packaging_SQL_VersionInfo_OnlyFaulty()
        {
            new PackagingTests().Packaging_VersionInfo_OnlyFaulty();
        }
        [TestMethod]
        public void Packaging_SQL_VersionInfo_Complex()
        {
            new PackagingTests().Packaging_VersionInfo_Complex();
        }

        /*================================================= tools */

        internal static void DropPackagesTable()
        {
            ExecuteSqlCommand(DropPackagesTableSql);
        }
        internal static void InstallPackagesTable()
        {
            ExecuteSqlCommand(InstallPackagesTableSql);
        }
        private static void ExecuteSqlCommand(string sql)
        {
            ConnectionStrings.ConnectionString = ConnectionString;
            var proc = DataProvider.CreateDataProcedure(sql);
            proc.CommandType = CommandType.Text;
            proc.ExecuteNonQuery();
        }
    }
}
