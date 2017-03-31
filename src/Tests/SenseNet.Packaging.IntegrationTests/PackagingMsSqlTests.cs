using System.Data;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Packaging;
using SenseNet.Packaging.Steps;
using System;
using System.IO;
using System.Collections.Generic;
using SenseNet.Packaging.IntegrationTests.Implementations;

namespace SenseNet.Packaging.IntegrationTests
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
	[ComponentId] [nvarchar](450) NULL,
	[ComponentVersion] [varchar](50) NULL,
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
            var expectedErrorType = PackagingExceptionType.DependencyNotFound;
            var actualErrorType = PackagingExceptionType.NotDefined;

            // action
            try
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component2</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Dependencies>
                                <Dependency id='Component1' version='7.0' />
                            </Dependencies>
                            <Steps>
                                <Trace>Package is running.</Trace>
                            </Steps>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                actualErrorType = e.ErrorType;
            }

            // assert
            Assert.AreEqual(actualErrorType, expectedErrorType);
        }
        [TestMethod]
        public void Packaging_SQL_DependencyCheck_CannotInstallExistingComponent()
        {
            ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Install'>
                        <ComponentId>MyCompany.MyComponent</ComponentId>
                        <ReleaseDate>2017-01-01</ReleaseDate>
                        <Version>1.0</Version>
                        <Steps>
                            <Trace>Package is running.</Trace>
                        </Steps>
                    </Package>");

            // action
            try
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>MyCompany.MyComponent</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.1</Version>
                            <Steps>
                                <Trace>Package is running.</Trace>
                            </Steps>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.CannotInstallExistingComponent, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_SQL_DependencyCheck_CannotUpdateMissingComponent()
        {
            try
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Patch'>
                        <ComponentId>Component2</ComponentId>
                        <ReleaseDate>2017-01-01</ReleaseDate>
                        <Version>1.1</Version>
                        <Steps>
                            <Trace>Package is running.</Trace>
                        </Steps>
                    </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.CannotUpdateMissingComponent, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_SQL_DependencyCheck_TargetVersionTooSmall()
        {
            ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Install'>
                        <ComponentId>MyCompany.MyComponent</ComponentId>
                        <ReleaseDate>2017-01-01</ReleaseDate>
                        <Version>1.0</Version>
                        <Steps>
                            <Trace>Package is running.</Trace>
                        </Steps>
                    </Package>");

            // action
            try
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Patch'>
                            <ComponentId>MyCompany.MyComponent</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Steps>
                                <Trace>Package is running.</Trace>
                            </Steps>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.TargetVersionTooSmall, e.ErrorType);
            }
        }

        [TestMethod]
        public void Packaging_SQL_DependencyCheck_DependencyVersion()
        {
            ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Install'>
                        <ComponentId>Component1</ComponentId>
                        <ReleaseDate>2017-01-01</ReleaseDate>
                        <Version>1.0</Version>
                        <Steps>
                            <Trace>Package is running.</Trace>
                        </Steps>
                    </Package>");

            // action
            try
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component2</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.1</Version>
                            <Dependencies>
                                <Dependency id='Component1' version='1.2' />
                            </Dependencies>
                            <Steps>
                                <Trace>Package is running.</Trace>
                            </Steps>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.DependencyVersion, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_SQL_DependencyCheck_DependencyMinimumVersion()
        {
            ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Install'>
                        <ComponentId>Component1</ComponentId>
                        <ReleaseDate>2017-01-01</ReleaseDate>
                        <Version>1.0</Version>
                        <Steps>
                            <Trace>Package is running.</Trace>
                        </Steps>
                    </Package>");

            // action
            try
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component2</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.1</Version>
                            <Dependencies>
                                <Dependency id='Component1' minVersion='1.1' />
                            </Dependencies>
                            <Steps>
                                <Trace>Package is running.</Trace>
                            </Steps>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.DependencyMinimumVersion, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_SQL_DependencyCheck_DependencyMaximumVersion()
        {
            ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Install'>
                        <ComponentId>Component1</ComponentId>
                        <ReleaseDate>2017-01-01</ReleaseDate>
                        <Version>3.0</Version>
                        <Steps>
                            <Trace>Package is running.</Trace>
                        </Steps>
                    </Package>");

            // action
            try
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component2</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>2.1</Version>
                            <Dependencies>
                                <Dependency id='Component1' maxVersion='2.0' />
                            </Dependencies>
                            <Steps>
                                <Trace>Package is running.</Trace>
                            </Steps>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.DependencyMaximumVersion, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_SQL_DependencyCheck_DependencyMinimumVersionExclusive()
        {
            ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Install'>
                        <ComponentId>Component1</ComponentId>
                        <ReleaseDate>2017-01-01</ReleaseDate>
                        <Version>1.0</Version>
                        <Steps>
                            <Trace>Package is running.</Trace>
                        </Steps>
                    </Package>");

            // action
            try
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component2</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.1</Version>
                            <Dependencies>
                                <Dependency id='Component1' minVersionExclusive='1.0' />
                            </Dependencies>
                            <Steps>
                                <Trace>Package is running.</Trace>
                            </Steps>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.DependencyMinimumVersion, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_SQL_DependencyCheck_DependencyMaximumVersionExclusive()
        {
            ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Install'>
                        <ComponentId>Component1</ComponentId>
                        <ReleaseDate>2017-01-01</ReleaseDate>
                        <Version>3.0</Version>
                        <Steps>
                            <Trace>Package is running.</Trace>
                        </Steps>
                    </Package>");

            // action
            try
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component2</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>2.0</Version>
                            <Dependencies>
                                <Dependency id='Component1' maxVersionExclusive='2.0' />
                            </Dependencies>
                            <Steps>
                                <Trace>Package is running.</Trace>
                            </Steps>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.DependencyMaximumVersion, e.ErrorType);
            }
        }

        [TestMethod]
        public void Packaging_SQL_DependencyCheck_LoggingDependencies()
        {
            DependencyCheckLoggingDependencies(_log);
        }
        internal void DependencyCheckLoggingDependencies(StringBuilder logger)
        {
            for (var i = 0; i < 9; i++)
            {
                ExecutePhases($@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component{i + 1}</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>{i + 1}.0</Version>
                        </Package>");
            }
            logger.Clear();

            // action
            ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>MyCompany.MyComponent</ComponentId>
                            <ReleaseDate>2017-01-02</ReleaseDate>
                            <Version>1.2</Version>
                            <Dependencies>
                                <Dependency id='Component1' version=            '1.0' />
                                <Dependency id='Component2' minVersion=         '1.0' />
                                <Dependency id='Component3' maxVersion=         '9.0' />
                                <Dependency id='Component4' minVersionExclusive='1.0' />
                                <Dependency id='Component5' maxVersionExclusive='9.0' />
                                <Dependency id='Component6' minVersion=         '1.0' maxVersion=         '10.0' />
                                <Dependency id='Component7' minVersion=         '1.0' maxVersionExclusive='10.0' />
                                <Dependency id='Component8' minVersionExclusive='1.0' maxVersion=         '10.0' />
                                <Dependency id='Component9' minVersionExclusive='1.0' maxVersionExclusive='10.0' />
                            </Dependencies>
                        </Package>");

            // check
            var log = logger.ToString();
            var relevantLines = new List<string>();
            using (var reader = new StringReader(log))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("Component") && !line.StartsWith("ComponentId"))
                        relevantLines.Add(line);
                }
            }

            Assert.AreEqual("Component1: 1.0 = 1.0 (current)", relevantLines[0]);
            Assert.AreEqual("Component2: 1.0 <= 2.0 (current)", relevantLines[1]);
            Assert.AreEqual("Component3: 3.0 (current) <= 9.0", relevantLines[2]);
            Assert.AreEqual("Component4: 1.0 < 4.0 (current)", relevantLines[3]);
            Assert.AreEqual("Component5: 5.0 (current) <= 9.0", relevantLines[4]);
            Assert.AreEqual("Component6: 1.0 <= 6.0 (current) <= 10.0", relevantLines[5]);
            Assert.AreEqual("Component7: 1.0 <= 7.0 (current) <= 10.0", relevantLines[6]);
            Assert.AreEqual("Component8: 1.0 < 8.0 (current) < 10.0", relevantLines[7]);
            Assert.AreEqual("Component9: 1.0 < 9.0 (current) < 10.0", relevantLines[8]);
        }

        // ========================================= Component lifetime tests

        [TestMethod]
        public void Packaging_SQL_Install_SnInitialComponent()
        {
            // simulate database before installation
            DropPackagesTable();

            // accessing versioninfo does not throw any error
            var verInfo = RepositoryVersionInfo.Instance;

            // there is no any component or package
            Assert.AreEqual(0, verInfo.Components.Count());
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
            ComponentInfo component;
            Package pkg;

            // phase 1 (with step that simulates the installing database)
            ExecutePhase(manifestXml, 0);

            // validate state after phase 1
            verInfo = RepositoryVersionInfo.Instance;
            Assert.AreEqual(0, verInfo.Components.Count());
            Assert.AreEqual(1, verInfo.InstalledPackages.Count());
            pkg = verInfo.InstalledPackages.First();
            Assert.AreEqual("Component42", pkg.ComponentId);
            Assert.AreEqual(ExecutionResult.Unfinished, pkg.ExecutionResult);
            Assert.AreEqual(PackageType.Install, pkg.PackageType);
            Assert.AreEqual("4.42", pkg.ComponentVersion.ToString());

            // phase 2
            ExecutePhase(manifestXml, 1);

            // validate state after phase 2
            verInfo = RepositoryVersionInfo.Instance;
            Assert.AreEqual(1, verInfo.Components.Count());
            Assert.AreEqual(1, verInfo.InstalledPackages.Count());
            component = verInfo.Components.First();
            Assert.AreEqual("Component42", component.ComponentId);
            Assert.AreEqual("4.42", component.Version.ToString());
            Assert.IsNotNull(component.AcceptableVersion);
            Assert.AreEqual("4.42", component.AcceptableVersion.ToString());
            pkg = verInfo.InstalledPackages.First();
            Assert.AreEqual("Component42", pkg.ComponentId);
            Assert.AreEqual(ExecutionResult.Successful, pkg.ExecutionResult);
            Assert.AreEqual(PackageType.Install, pkg.PackageType);
            Assert.AreEqual("4.42", pkg.ComponentVersion.ToString());
        }

        [TestMethod]
        public void Packaging_SQL_Install_NoSteps()
        {
            var manifestXml = new XmlDocument();
            manifestXml.LoadXml(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component42</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>4.42</Version>
                        </Package>");

            // action
            ExecutePhase(manifestXml, 0);

            // check
            Assert.AreEqual(1, RepositoryVersionInfo.Instance.Components.Count());
            Assert.AreEqual(1, RepositoryVersionInfo.Instance.InstalledPackages.Count());
            var component = RepositoryVersionInfo.Instance.Components.FirstOrDefault();
            Assert.IsNotNull(component);
            Assert.AreEqual("Component42", component.ComponentId);
            Assert.AreEqual("4.42", component.Version.ToString());
            Assert.IsNotNull(component.AcceptableVersion);
            Assert.AreEqual("4.42", component.AcceptableVersion.ToString());
            var pkg = RepositoryVersionInfo.Instance.InstalledPackages.FirstOrDefault();
            Assert.IsNotNull(pkg);
            Assert.AreEqual("Component42", pkg.ComponentId);
            Assert.AreEqual(ExecutionResult.Successful, pkg.ExecutionResult);
            Assert.AreEqual(PackageType.Install, pkg.PackageType);
            Assert.AreEqual("4.42", pkg.ComponentVersion.ToString());
        }
        [TestMethod]
        public void Packaging_SQL_Install_ThreePhases()
        {
            var manifestXml = new XmlDocument();
            manifestXml.LoadXml(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component42</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>4.42</Version>
                            <Steps>
                                <Phase><Trace>Package is running. Phase-1</Trace></Phase>
                                <Phase><Trace>Package is running. Phase-2</Trace></Phase>
                                <Phase><Trace>Package is running. Phase-3</Trace></Phase>
                            </Steps>
                        </Package>");
            ComponentInfo component;
            Package pkg;

            // phase 1
            ExecutePhase(manifestXml, 0);

            // validate state after phase 1
            var verInfo = RepositoryVersionInfo.Instance;
            Assert.IsFalse(verInfo.Components.Any());
            Assert.IsTrue(verInfo.InstalledPackages.Any());
            pkg = RepositoryVersionInfo.Instance.InstalledPackages.FirstOrDefault();
            Assert.IsNotNull(pkg);
            Assert.AreEqual("Component42", pkg.ComponentId);
            Assert.AreEqual(ExecutionResult.Unfinished, pkg.ExecutionResult);
            Assert.AreEqual(PackageType.Install, pkg.PackageType);
            Assert.AreEqual("4.42", pkg.ComponentVersion.ToString());

            // phase 2
            ExecutePhase(manifestXml, 1);

            // validate state after phase 2
            verInfo = RepositoryVersionInfo.Instance;
            Assert.IsFalse(verInfo.Components.Any());
            Assert.IsTrue(verInfo.InstalledPackages.Any());
            pkg = RepositoryVersionInfo.Instance.InstalledPackages.FirstOrDefault();
            Assert.IsNotNull(pkg);
            Assert.AreEqual("Component42", pkg.ComponentId);
            Assert.AreEqual(ExecutionResult.Unfinished, pkg.ExecutionResult);
            Assert.AreEqual(PackageType.Install, pkg.PackageType);
            Assert.AreEqual("4.42", pkg.ComponentVersion.ToString());

            // phase 3
            ExecutePhase(manifestXml, 2);

            // validate state after phase 3
            component = RepositoryVersionInfo.Instance.Components.FirstOrDefault();
            Assert.IsNotNull(component);
            Assert.AreEqual("Component42", component.ComponentId);
            Assert.AreEqual("4.42", component.Version.ToString());
            Assert.IsNotNull(component.AcceptableVersion);
            Assert.AreEqual("4.42", component.AcceptableVersion.ToString());
            pkg = RepositoryVersionInfo.Instance.InstalledPackages.FirstOrDefault();
            Assert.IsNotNull(pkg);
            Assert.AreEqual("Component42", pkg.ComponentId);
            Assert.AreEqual(ExecutionResult.Successful, pkg.ExecutionResult);
            Assert.AreEqual(PackageType.Install, pkg.PackageType);
            Assert.AreEqual("4.42", pkg.ComponentVersion.ToString());

            Assert.AreEqual(1, RepositoryVersionInfo.Instance.Components.Count());
            Assert.AreEqual(1, RepositoryVersionInfo.Instance.InstalledPackages.Count());
        }

        [TestMethod]
        public void Packaging_SQL_Patch_ThreePhases()
        {
            ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>MyCompany.MyComponent</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Steps>
                                <Trace>Package is running.</Trace>
                            </Steps>
                        </Package>");

            var manifestXml = new XmlDocument();
            manifestXml.LoadXml(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Patch'>
                            <ComponentId>MyCompany.MyComponent</ComponentId>
                            <ReleaseDate>2017-01-02</ReleaseDate>
                            <Version>1.2</Version>
                            <Steps>
                                <Phase><Trace>Package is running. Phase-1</Trace></Phase>
                                <Phase><Trace>Package is running. Phase-2</Trace></Phase>
                                <Phase><Trace>Package is running. Phase-3</Trace></Phase>
                            </Steps>
                        </Package>");

            ComponentInfo component;
            Package pkg;

            // phase 1
            ExecutePhase(manifestXml, 0);

            // validate state after phase 1
            component = RepositoryVersionInfo.Instance.Components.FirstOrDefault();
            Assert.IsNotNull(component);
            Assert.AreEqual("MyCompany.MyComponent", component.ComponentId);
            Assert.AreEqual("1.2", component.Version.ToString());
            Assert.IsNotNull(component.AcceptableVersion);
            Assert.AreEqual("1.0", component.AcceptableVersion.ToString());
            pkg = RepositoryVersionInfo.Instance.InstalledPackages.LastOrDefault();
            Assert.IsNotNull(pkg);
            Assert.AreEqual("MyCompany.MyComponent", pkg.ComponentId);
            Assert.AreEqual(ExecutionResult.Unfinished, pkg.ExecutionResult);
            Assert.AreEqual(PackageType.Patch, pkg.PackageType);
            Assert.AreEqual("1.2", pkg.ComponentVersion.ToString());

            // phase 2
            ExecutePhase(manifestXml, 1);

            // validate state after phase 2
            component = RepositoryVersionInfo.Instance.Components.FirstOrDefault();
            Assert.IsNotNull(component);
            Assert.AreEqual("MyCompany.MyComponent", component.ComponentId);
            Assert.AreEqual("1.2", component.Version.ToString());
            Assert.IsNotNull(component.AcceptableVersion);
            Assert.AreEqual("1.0", component.AcceptableVersion.ToString());
            pkg = RepositoryVersionInfo.Instance.InstalledPackages.LastOrDefault();
            Assert.IsNotNull(pkg);
            Assert.AreEqual("MyCompany.MyComponent", pkg.ComponentId);
            Assert.AreEqual(ExecutionResult.Unfinished, pkg.ExecutionResult);
            Assert.AreEqual(PackageType.Patch, pkg.PackageType);
            Assert.AreEqual("1.2", pkg.ComponentVersion.ToString());

            // phase 3
            ExecutePhase(manifestXml, 2);

            // validate state after phase 3
            component = RepositoryVersionInfo.Instance.Components.FirstOrDefault();
            Assert.IsNotNull(component);
            Assert.AreEqual("MyCompany.MyComponent", component.ComponentId);
            Assert.AreEqual("1.2", component.Version.ToString());
            Assert.IsNotNull(component.AcceptableVersion);
            Assert.AreEqual("1.2", component.AcceptableVersion.ToString());
            pkg = RepositoryVersionInfo.Instance.InstalledPackages.LastOrDefault();
            Assert.IsNotNull(pkg);
            Assert.AreEqual("MyCompany.MyComponent", pkg.ComponentId);
            Assert.AreEqual(ExecutionResult.Successful, pkg.ExecutionResult);
            Assert.AreEqual(PackageType.Patch, pkg.PackageType);
            Assert.AreEqual("1.2", pkg.ComponentVersion.ToString());

            Assert.AreEqual(1, RepositoryVersionInfo.Instance.Components.Count());
            Assert.AreEqual(2, RepositoryVersionInfo.Instance.InstalledPackages.Count());
        }


        [TestMethod]
        public void Packaging_SQL_Patch_Faulty()
        {
            ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>MyCompany.MyComponent</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Steps>
                                <Trace>Package is running.</Trace>
                            </Steps>
                        </Package>");

            // action
            try
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Patch'>
                            <ComponentId>MyCompany.MyComponent</ComponentId>
                            <ReleaseDate>2017-01-02</ReleaseDate>
                            <Version>1.2</Version>
                            <Steps>
                                <ForbiddenStep />
                            </Steps>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (Exception)
            {
                // do not compensate anything
            }

            // check
            Assert.AreEqual(1, RepositoryVersionInfo.Instance.Components.Count());
            Assert.AreEqual(2, RepositoryVersionInfo.Instance.InstalledPackages.Count());
            var component = RepositoryVersionInfo.Instance.Components.FirstOrDefault();
            Assert.IsNotNull(component);
            Assert.AreEqual("MyCompany.MyComponent", component.ComponentId);
            Assert.AreEqual("1.2", component.Version.ToString());
            Assert.IsNotNull(component.AcceptableVersion);
            Assert.AreEqual("1.0", component.AcceptableVersion.ToString());
            var pkg = RepositoryVersionInfo.Instance.InstalledPackages.LastOrDefault();
            Assert.IsNotNull(pkg);
            Assert.AreEqual("MyCompany.MyComponent", pkg.ComponentId);
            Assert.AreEqual(ExecutionResult.Faulty, pkg.ExecutionResult);
            Assert.AreEqual(PackageType.Patch, pkg.PackageType);
            Assert.AreEqual("1.2", pkg.ComponentVersion.ToString());
        }
        [TestMethod]
        public void Packaging_SQL_Patch_FixFaulty()
        {
            ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>MyCompany.MyComponent</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Steps>
                                <Trace>Package is running.</Trace>
                            </Steps>
                        </Package>");

            try
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Patch'>
                            <ComponentId>MyCompany.MyComponent</ComponentId>
                            <ReleaseDate>2017-01-02</ReleaseDate>
                            <Version>1.2</Version>
                            <Steps>
                                <ForbiddenStep />
                            </Steps>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (Exception)
            {
                // do not compensate anything
            }

            // action
            ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Patch'>
                            <ComponentId>MyCompany.MyComponent</ComponentId>
                            <ReleaseDate>2017-01-02</ReleaseDate>
                            <Version>1.2</Version>
                            <Steps>
                                <Trace>Package is running.</Trace>
                            </Steps>
                        </Package>");

            // check
            Assert.AreEqual(1, RepositoryVersionInfo.Instance.Components.Count());
            Assert.AreEqual(3, RepositoryVersionInfo.Instance.InstalledPackages.Count());
            var component = RepositoryVersionInfo.Instance.Components.FirstOrDefault();
            Assert.IsNotNull(component);
            Assert.AreEqual("MyCompany.MyComponent", component.ComponentId);
            Assert.AreEqual("1.2", component.Version.ToString());
            Assert.IsNotNull(component.AcceptableVersion);
            Assert.AreEqual("1.2", component.AcceptableVersion.ToString());
            var pkg = RepositoryVersionInfo.Instance.InstalledPackages.LastOrDefault();
            Assert.IsNotNull(pkg);
            Assert.AreEqual("MyCompany.MyComponent", pkg.ComponentId);
            Assert.AreEqual(ExecutionResult.Successful, pkg.ExecutionResult);
            Assert.AreEqual(PackageType.Patch, pkg.PackageType);
            Assert.AreEqual("1.2", pkg.ComponentVersion.ToString());
        }
        [TestMethod]
        public void Packaging_SQL_Patch_FixMoreFaulty()
        {
            ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>MyCompany.MyComponent</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Steps>
                                <Trace>Package is running.</Trace>
                            </Steps>
                        </Package>");

            for (int i = 0; i < 2; i++)
            {
                try
                {
                    ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Patch'>
                            <ComponentId>MyCompany.MyComponent</ComponentId>
                            <ReleaseDate>2017-01-02</ReleaseDate>
                            <Version>1.2</Version>
                            <Steps>
                                <ForbiddenStep />
                            </Steps>
                        </Package>");
                    Assert.Fail("PackagingException was not thrown.");
                }
                catch (Exception)
                {
                    // do not compensate anything
                }
            }

            // action
            ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Patch'>
                            <ComponentId>MyCompany.MyComponent</ComponentId>
                            <ReleaseDate>2017-01-02</ReleaseDate>
                            <Version>1.2</Version>
                            <Steps>
                                <Trace>Package is running.</Trace>
                            </Steps>
                        </Package>");

            // check
            Assert.AreEqual(1, RepositoryVersionInfo.Instance.Components.Count());
            Assert.AreEqual(4, RepositoryVersionInfo.Instance.InstalledPackages.Count());
            var component = RepositoryVersionInfo.Instance.Components.FirstOrDefault();
            Assert.IsNotNull(component);
            Assert.AreEqual("MyCompany.MyComponent", component.ComponentId);
            Assert.AreEqual("1.2", component.Version.ToString());
            Assert.IsNotNull(component.AcceptableVersion);
            Assert.AreEqual("1.2", component.AcceptableVersion.ToString());
            var pkg = RepositoryVersionInfo.Instance.InstalledPackages.LastOrDefault();
            Assert.IsNotNull(pkg);
            Assert.AreEqual("MyCompany.MyComponent", pkg.ComponentId);
            Assert.AreEqual(ExecutionResult.Successful, pkg.ExecutionResult);
            Assert.AreEqual(PackageType.Patch, pkg.PackageType);
            Assert.AreEqual("1.2", pkg.ComponentVersion.ToString());
        }

        // ========================================= RepositoryVersionInfo queries

        [TestMethod]
        public void Packaging_VersionInfo_Empty()
        {
            var verInfo = RepositoryVersionInfo.Instance;
            var components = verInfo.Components.ToArray();
            var packages = verInfo.InstalledPackages.ToArray();
            Assert.AreEqual(0, components.Length);
            Assert.AreEqual(0, packages.Length);
        }
        [TestMethod]
        public void Packaging_VersionInfo_OnlyUnfinished()
        {
            SavePackage("C1", "1.0", "01:00", "2016-01-01", PackageType.Install, ExecutionResult.Unfinished);

            // action
            var verInfo = RepositoryVersionInfo.Instance;

            // check
            var components = verInfo.Components.ToArray();
            var packages = verInfo.InstalledPackages.ToArray();
            Assert.AreEqual(0, components.Length);
            Assert.AreEqual(1, packages.Length);
        }
        [TestMethod]
        public void Packaging_VersionInfo_OnlyFaulty()
        {
            SavePackage("C1", "1.0", "01:00", "2016-01-01", PackageType.Install, ExecutionResult.Faulty);

            // action
            var verInfo = RepositoryVersionInfo.Instance;

            // check
            var components = verInfo.Components.ToArray();
            var packages = verInfo.InstalledPackages.ToArray();
            Assert.AreEqual(0, components.Length);
            Assert.AreEqual(1, packages.Length);
        }

        [TestMethod]
        public void Packaging_VersionInfo_Complex()
        {
            SavePackage("C1", "1.0", "01:00", "2016-01-01", PackageType.Install, ExecutionResult.Successful);
            SavePackage("C2", "1.0", "02:00", "2016-01-02", PackageType.Install, ExecutionResult.Successful);
            SavePackage("C1", "1.1", "03:00", "2016-01-03", PackageType.Patch, ExecutionResult.Faulty);
            SavePackage("C1", "1.1", "04:00", "2016-01-03", PackageType.Patch, ExecutionResult.Faulty);
            SavePackage("C1", "1.2", "05:00", "2016-01-06", PackageType.Patch, ExecutionResult.Successful);
            SavePackage("C2", "1.1", "06:00", "2016-01-07", PackageType.Patch, ExecutionResult.Unfinished);
            SavePackage("C2", "1.2", "07:00", "2016-01-08", PackageType.Patch, ExecutionResult.Unfinished);
            SavePackage("C3", "1.0", "08:00", "2016-01-09", PackageType.Install, ExecutionResult.Faulty);
            SavePackage("C3", "2.0", "08:00", "2016-01-09", PackageType.Install, ExecutionResult.Faulty);

            // action
            var verInfo = RepositoryVersionInfo.Instance;

            // check
            var actual = string.Join(" | ", verInfo.Components
                .OrderBy(a => a.ComponentId)
                .Select(a => $"{a.ComponentId}: {a.AcceptableVersion} ({a.Version})")
                .ToArray());
            // 
            var expected = "C1: 1.2 (1.2) | C2: 1.0 (1.2)";
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(9, verInfo.InstalledPackages.Count());
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

        /*--------------------------------------------------------*/

        private void SavePackage(string id, string version, string execTime, string releaseDate, PackageType packageType, ExecutionResult result)
        {
            var package = new Package
            {
                ComponentId = id,
                ComponentVersion = Version.Parse(version),
                Description = $"{id}-Description",
                ExecutionDate = DateTime.Parse($"2017-03-30 {execTime}"),
                ReleaseDate = DateTime.Parse(releaseDate),
                ExecutionError = null,
                ExecutionResult = result,
                PackageType = packageType,
            };
            PackageManager.Storage.SavePackage(package);
        }

        internal static Manifest ParseManifestHead(string manifestXml)
        {
            var xml = new XmlDocument();
            xml.LoadXml(manifestXml);
            var manifest = new Manifest();
            Manifest.ParseHead(xml, manifest);
            return manifest;
        }
        internal static Manifest ParseManifest(string manifestXml, int currentPhase)
        {
            var xml = new XmlDocument();
            xml.LoadXml(manifestXml);
            return Manifest.Parse(xml, currentPhase, true);
        }

        internal static PackagingResult ExecutePhases(string manifestXml, TextWriter console = null)
        {
            var xml = new XmlDocument();
            xml.LoadXml(manifestXml);
            return ExecutePhases(xml, console);
        }
        internal static PackagingResult ExecutePhases(XmlDocument manifestXml, TextWriter console = null)
        {
            var phase = -1;
            var errors = 0;
            PackagingResult result;
            do
            {
                result = ExecutePhase(manifestXml, ++phase, console ?? new StringWriter());
                errors += result.Errors;
            } while (result.NeedRestart);
            result.Errors = errors;
            return result;
        }
        internal static PackagingResult ExecutePhase(XmlDocument manifestXml, int phase, TextWriter console = null)
        {
            var manifest = Manifest.Parse(manifestXml, phase, true);
            var executionContext = ExecutionContext.CreateForTest("packagePath", "targetPath", new string[0], "sandboxPath", manifest, phase, manifest.CountOfPhases, null, console ?? new StringWriter());
            var result = PackageManager.ExecuteCurrentPhase(manifest, executionContext);
            RepositoryVersionInfo.Reset();
            return result;
        }

    }
}
