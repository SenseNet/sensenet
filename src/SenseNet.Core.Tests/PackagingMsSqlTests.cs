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

namespace SenseNet.Core.Tests
{
    [TestClass]
    public class PackagingMsSqlTests
    {
        private static StringBuilder _log;

        [TestInitialize]
        public void InitializeTest()
        {
            // preparing logger
            _log = new StringBuilder();
            var loggers = new[] { new PackagingTestLogger(_log) };
            var loggerAcc = new PrivateType(typeof(Logger));
            loggerAcc.SetStaticField("_loggers", loggers);

            // preparing database
            ConnectionStrings.ConnectionString = "Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=sensenet;Data Source=(local)";
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
            Assert.Inconclusive();
            new PackagingTests().Packaging_Install_SnInitialComponent();
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

    }
}
