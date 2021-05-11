using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.IntegrationTests.Common;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.Packaging;
using SenseNet.Testing;
using ExecutionContext = SenseNet.Packaging.ExecutionContext;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.IntegrationTests.TestCases
{
    public class PackagingTestCases : TestCaseBase
    {

        /* ==================================================================================== Checking dependency tests */

        public void Packaging_DependencyCheck_MissingDependency()
        {
            PackagingTest(() =>
            {
                var expectedErrorType = PackagingExceptionType.DependencyNotFound;
                var actualErrorType = PackagingExceptionType.NotDefined;

                // action
                try
                {
                    ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <Id>Component2</Id>
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
            });
        }
        public void Packaging_DependencyCheck_CannotInstallExistingComponent()
        {
            PackagingTest(() =>
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Install'>
                        <Id>MyCompany.MyComponent</Id>
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
                            <Id>MyCompany.MyComponent</Id>
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
            });
        }
        public void Packaging_DependencyCheck_CannotUpdateMissingComponent()
        {
            PackagingTest(() =>
            {
                try
                {
                    ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Patch'>
                        <Id>Component2</Id>
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
            });
        }
        public void Packaging_DependencyCheck_TargetVersionTooSmall()
        {
            PackagingTest(() =>
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Install'>
                        <Id>MyCompany.MyComponent</Id>
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
                            <Id>MyCompany.MyComponent</Id>
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
            });
        }

        public void Packaging_DependencyCheck_DependencyVersion()
        {
            PackagingTest(() =>
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Install'>
                        <Id>Component1</Id>
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
                            <Id>Component2</Id>
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
            });
        }
        public void Packaging_DependencyCheck_DependencyMinimumVersion()
        {
            PackagingTest(() =>
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Install'>
                        <Id>Component1</Id>
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
                            <Id>Component2</Id>
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
            });
        }
        public void Packaging_DependencyCheck_DependencyMaximumVersion()
        {
            PackagingTest(() =>
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Install'>
                        <Id>Component1</Id>
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
                            <Id>Component2</Id>
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
            });
        }
        public void Packaging_DependencyCheck_DependencyMinimumVersionExclusive()
        {
            PackagingTest(() =>
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Install'>
                        <Id>Component1</Id>
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
                            <Id>Component2</Id>
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
            });
        }
        public void Packaging_DependencyCheck_DependencyMaximumVersionExclusive()
        {
            PackagingTest(() =>
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                    <Package type='Install'>
                        <Id>Component1</Id>
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
                            <Id>Component2</Id>
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
            });
        }

        public void Packaging_DependencyCheck_LoggingDependencies()
        {
            PackagingTest(() =>
            {
                var logger = new StringBuilder();
                var loggers = new[] { new PackagingTestLogger(logger) };
                var loggerAcc = new TypeAccessor(typeof(SenseNet.Packaging.Logger));
                loggerAcc.SetStaticField("_loggers", loggers);

                using (new Swindler<IPackagingLogger[]>(loggers,
                    () => (IPackagingLogger[]) loggerAcc.GetStaticField("_loggers"),
                    value => loggerAcc.SetStaticField("_loggers", loggers)))
                {
                    for (var i = 0; i < 9; i++)
                    {
                        ExecutePhases($@"<?xml version='1.0' encoding='utf-8'?>
                            <Package type='Install'>
                                <Id>Component{i + 1}</Id>
                                <ReleaseDate>2017-01-01</ReleaseDate>
                                <Version>{i + 1}.0</Version>
                            </Package>");
                    }
                    logger.Clear();

                    // action
                    ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                            <Package type='Install'>
                                <Id>MyCompany.MyComponent</Id>
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
            });
        }

        /* ==================================================================================== Component lifetime tests */

        public void Packaging_Install_NoSteps()
        {
            PackagingTest(() =>
            {
                var manifestXml = new XmlDocument();
                manifestXml.LoadXml(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <Id>Component42</Id>
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
                var pkg = RepositoryVersionInfo.Instance.InstalledPackages.FirstOrDefault();
                Assert.IsNotNull(pkg);
                Assert.AreEqual("Component42", pkg.ComponentId);
                Assert.AreEqual(ExecutionResult.Successful, pkg.ExecutionResult);
                Assert.AreEqual(PackageType.Install, pkg.PackageType);
                Assert.AreEqual("4.42", pkg.ComponentVersion.ToString());
            });
        }
        public void Packaging_Install_ThreePhases()
        {
            PackagingTest(() =>
            {
                var manifestXml = new XmlDocument();
                manifestXml.LoadXml(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <Id>Component42</Id>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>4.42</Version>
                            <Steps>
                                <Phase><Trace>Package is running. Phase-1</Trace></Phase>
                                <Phase><Trace>Package is running. Phase-2</Trace></Phase>
                                <Phase><Trace>Package is running. Phase-3</Trace></Phase>
                            </Steps>
                        </Package>");

                // phase 1
                ExecutePhase(manifestXml, 0);

                // validate state after phase 1
                var verInfo = RepositoryVersionInfo.Instance;
                Assert.IsFalse(verInfo.Components.Any());
                Assert.IsTrue(verInfo.InstalledPackages.Any());
                var pkg = RepositoryVersionInfo.Instance.InstalledPackages.FirstOrDefault();
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
                var component = RepositoryVersionInfo.Instance.Components.FirstOrDefault();
                Assert.IsNotNull(component);
                Assert.AreEqual("Component42", component.ComponentId);
                Assert.AreEqual("4.42", component.Version.ToString());
                pkg = RepositoryVersionInfo.Instance.InstalledPackages.FirstOrDefault();
                Assert.IsNotNull(pkg);
                Assert.AreEqual("Component42", pkg.ComponentId);
                Assert.AreEqual(ExecutionResult.Successful, pkg.ExecutionResult);
                Assert.AreEqual(PackageType.Install, pkg.PackageType);
                Assert.AreEqual("4.42", pkg.ComponentVersion.ToString());

                Assert.AreEqual(1, RepositoryVersionInfo.Instance.Components.Count());
                Assert.AreEqual(1, RepositoryVersionInfo.Instance.InstalledPackages.Count());
            });
        }

        public void Packaging_Patch_ThreePhases()
        {
            PackagingTest(() =>
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <Id>MyCompany.MyComponent</Id>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Steps>
                                <Trace>Package is running.</Trace>
                            </Steps>
                        </Package>");

                var manifestXml = new XmlDocument();
                manifestXml.LoadXml(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Patch'>
                            <Id>MyCompany.MyComponent</Id>
                            <ReleaseDate>2017-01-02</ReleaseDate>
                            <Version>1.2</Version>
                            <Steps>
                                <Phase><Trace>Package is running. Phase-1</Trace></Phase>
                                <Phase><Trace>Package is running. Phase-2</Trace></Phase>
                                <Phase><Trace>Package is running. Phase-3</Trace></Phase>
                            </Steps>
                        </Package>");

                // phase 1
                ExecutePhase(manifestXml, 0);

                // validate state after phase 1
                var component = RepositoryVersionInfo.Instance.Components.FirstOrDefault();
                Assert.IsNotNull(component);
                Assert.AreEqual("MyCompany.MyComponent", component.ComponentId);
                Assert.AreEqual("1.0", component.Version.ToString());
                var pkg = RepositoryVersionInfo.Instance.InstalledPackages.LastOrDefault();
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
                Assert.AreEqual("1.0", component.Version.ToString());
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
                pkg = RepositoryVersionInfo.Instance.InstalledPackages.LastOrDefault();
                Assert.IsNotNull(pkg);
                Assert.AreEqual("MyCompany.MyComponent", pkg.ComponentId);
                Assert.AreEqual(ExecutionResult.Successful, pkg.ExecutionResult);
                Assert.AreEqual(PackageType.Patch, pkg.PackageType);
                Assert.AreEqual("1.2", pkg.ComponentVersion.ToString());

                Assert.AreEqual(1, RepositoryVersionInfo.Instance.Components.Count());
                Assert.AreEqual(2, RepositoryVersionInfo.Instance.InstalledPackages.Count());
            });
        }

        public void Packaging_Patch_Faulty()
        {
            PackagingTest(() =>
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <Id>MyCompany.MyComponent</Id>
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
                            <Id>MyCompany.MyComponent</Id>
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
                Assert.AreEqual("1.0", component.Version.ToString());
                var pkg = RepositoryVersionInfo.Instance.InstalledPackages.LastOrDefault();
                Assert.IsNotNull(pkg);
                Assert.AreEqual("MyCompany.MyComponent", pkg.ComponentId);
                Assert.AreEqual(ExecutionResult.Faulty, pkg.ExecutionResult);
                Assert.AreEqual(PackageType.Patch, pkg.PackageType);
                Assert.AreEqual("1.2", pkg.ComponentVersion.ToString());
            });
        }
        public void Packaging_Patch_FixFaulty()
        {
            PackagingTest(() =>
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <Id>MyCompany.MyComponent</Id>
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
                            <Id>MyCompany.MyComponent</Id>
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
                            <Id>MyCompany.MyComponent</Id>
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
                var pkg = RepositoryVersionInfo.Instance.InstalledPackages.LastOrDefault();
                Assert.IsNotNull(pkg);
                Assert.AreEqual("MyCompany.MyComponent", pkg.ComponentId);
                Assert.AreEqual(ExecutionResult.Successful, pkg.ExecutionResult);
                Assert.AreEqual(PackageType.Patch, pkg.PackageType);
                Assert.AreEqual("1.2", pkg.ComponentVersion.ToString());
            });
        }
        public void Packaging_Patch_FixMoreFaulty()
        {
            PackagingTest(() =>
            {
                ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <Id>MyCompany.MyComponent</Id>
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
                            <Id>MyCompany.MyComponent</Id>
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
                            <Id>MyCompany.MyComponent</Id>
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
                var pkg = RepositoryVersionInfo.Instance.InstalledPackages.LastOrDefault();
                Assert.IsNotNull(pkg);
                Assert.AreEqual("MyCompany.MyComponent", pkg.ComponentId);
                Assert.AreEqual(ExecutionResult.Successful, pkg.ExecutionResult);
                Assert.AreEqual(PackageType.Patch, pkg.PackageType);
                Assert.AreEqual("1.2", pkg.ComponentVersion.ToString());
            });
        }

        /* ==================================================================================== RepositoryVersionInfo queries */

        public void Packaging_VersionInfo_Empty()
        {
            PackagingTest(() =>
            {
                var verInfo = RepositoryVersionInfo.Instance;
                var components = verInfo.Components.ToArray();
                var packages = verInfo.InstalledPackages.ToArray();
                Assert.AreEqual(0, components.Length);
                Assert.AreEqual(0, packages.Length);
            });
        }
        public async Task Packaging_VersionInfo_OnlyUnfinished()
        {
            await PackagingTestAsync(async () =>
            {
                await SavePackage("C1", "1.0", "01:00", "2016-01-01", PackageType.Install, ExecutionResult.Unfinished);

                // action
                var verInfo = RepositoryVersionInfo.Instance;

                // check
                var components = verInfo.Components.ToArray();
                var packages = verInfo.InstalledPackages.ToArray();
                Assert.AreEqual(0, components.Length);
                Assert.AreEqual(1, packages.Length);
            }).ConfigureAwait(false);
        }
        public async Task Packaging_VersionInfo_OnlyFaulty()
        {
            await PackagingTestAsync(async () =>
            {
                await SavePackage("C1", "1.0", "01:00", "2016-01-01", PackageType.Install, ExecutionResult.Faulty);

                // action
                var verInfo = RepositoryVersionInfo.Instance;

                // check
                var components = verInfo.Components.ToArray();
                var packages = verInfo.InstalledPackages.ToArray();
                Assert.AreEqual(0, components.Length);
                Assert.AreEqual(1, packages.Length);
            }).ConfigureAwait(false);
        }
        public async Task Packaging_VersionInfo_Complex()
        {
            await PackagingTestAsync(async () =>
            {
                await SavePackage("C1", "1.0", "01:00", "2016-01-01", PackageType.Install, ExecutionResult.Successful);
                await SavePackage("C2", "1.0", "02:00", "2016-01-02", PackageType.Install, ExecutionResult.Successful);
                await SavePackage("C1", "1.1", "03:00", "2016-01-03", PackageType.Patch, ExecutionResult.Faulty);
                await SavePackage("C1", "1.1", "04:00", "2016-01-03", PackageType.Patch, ExecutionResult.Faulty);
                await SavePackage("C1", "1.2", "05:00", "2016-01-06", PackageType.Patch, ExecutionResult.Successful);
                await SavePackage("C2", "1.1", "06:00", "2016-01-07", PackageType.Patch, ExecutionResult.Unfinished);
                await SavePackage("C2", "1.2", "07:00", "2016-01-08", PackageType.Patch, ExecutionResult.Unfinished);
                await SavePackage("C3", "1.0", "08:00", "2016-01-09", PackageType.Install, ExecutionResult.Faulty);
                await SavePackage("C3", "2.0", "08:00", "2016-01-09", PackageType.Install, ExecutionResult.Faulty);

                // action
                var verInfo = RepositoryVersionInfo.Instance;

                // check
                var actual = string.Join(" | ", verInfo.Components
                    .OrderBy(a => a.ComponentId)
                    .Select(a => $"{a.ComponentId}: {a.Version}")
                    .ToArray());
                // 
                var expected = "C1: 1.2 | C2: 1.0";
                Assert.AreEqual(expected, actual);
                Assert.AreEqual(9, verInfo.InstalledPackages.Count());
            }).ConfigureAwait(false);
        }

        public async Task Packaging_VersionInfo_MultipleInstall()
        {
            await PackagingTestAsync(async () =>
            {
                const string packageId = "C1";
                await SavePackage(packageId, "1.0", "01:00", "2016-01-01", PackageType.Install, ExecutionResult.Successful);
                await SavePackage(packageId, "1.0", "02:00", "2016-01-02", PackageType.Install, ExecutionResult.Successful);
                await SavePackage(packageId, "1.1", "03:00", "2016-01-03", PackageType.Install, ExecutionResult.Faulty);
                await SavePackage(packageId, "1.2", "04:00", "2016-01-04", PackageType.Install, ExecutionResult.Faulty);
                await SavePackage("C2", "1.0", "05:00", "2016-01-05", PackageType.Install, ExecutionResult.Successful);
                await SavePackage(packageId, "1.0", "06:00", "2016-01-06", PackageType.Install, ExecutionResult.Successful);

                var verInfo = RepositoryVersionInfo.Instance;

                // check
                var actual = string.Join(" | ", verInfo.Components
                    .OrderBy(a => a.ComponentId)
                    .Select(a => $"{a.ComponentId}: {a.Version}")
                    .ToArray());

                // we expect a separate line for every Install package execution
                var expected = "C1: 1.0 | C2: 1.0";
                Assert.AreEqual(expected, actual);
                Assert.AreEqual(6, verInfo.InstalledPackages.Count());
            }).ConfigureAwait(false);
        }

        /* ==================================================================================== Storing manifest */

        public async Task Packaging_Manifest_StoredButNotLoaded()
        {
            await PackagingTestAsync(async () =>
            {
                // prepare xml source
                var manifest = @"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <Id>Component42</Id>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>4.42</Version>
                            <Steps>
                                <Trace>Package is running. Phase-1</Trace>
                            </Steps>
                        </Package>";
                var xml = new XmlDocument();
                xml.LoadXml(manifest);
                var expected = xml.OuterXml;

                // store
                ExecutePhases(manifest);

                // load
                var verInfo = RepositoryVersionInfo.Instance;

                // manifest is not explicitly loaded
                var package = verInfo.InstalledPackages.FirstOrDefault();
                Assert.IsNull(package?.Manifest);

                // load manifest explicitly
                await PackageManager.Storage.LoadManifestAsync(package, CancellationToken.None);
                var actual = package?.Manifest;
                Assert.AreEqual(expected, actual);
            }).ConfigureAwait(false);
        }

        /* ==================================================================================== Package deletion */

        public async Task Packaging_DeleteOne()
        {
            await PackagingTestAsync(async () =>
            {
                await SavePackage("C1", "1.0", "00:00", "2016-01-01", PackageType.Install, ExecutionResult.Unfinished);
                await SavePackage("C1", "1.0", "01:00", "2016-01-01", PackageType.Install, ExecutionResult.Faulty);
                await SavePackage("C1", "1.0", "02:00", "2016-01-01", PackageType.Install, ExecutionResult.Successful);
                await SavePackage("C1", "1.1", "03:00", "2016-01-03", PackageType.Patch, ExecutionResult.Faulty);
                await SavePackage("C1", "1.1", "04:00", "2016-01-03", PackageType.Patch, ExecutionResult.Faulty);
                await SavePackage("C1", "1.1", "05:00", "2016-01-06", PackageType.Patch, ExecutionResult.Successful);
                await SavePackage("C1", "1.2", "06:00", "2016-01-07", PackageType.Patch, ExecutionResult.Unfinished);
                await SavePackage("C1", "1.2", "07:00", "2016-01-08", PackageType.Patch, ExecutionResult.Unfinished);
                await SavePackage("C1", "1.2", "08:00", "2016-01-09", PackageType.Patch, ExecutionResult.Faulty);
                await SavePackage("C1", "1.2", "09:00", "2016-01-09", PackageType.Patch, ExecutionResult.Faulty);
                await SavePackage("C1", "1.2", "10:00", "2016-01-09", PackageType.Patch, ExecutionResult.Successful);

                // action: delete all faulty and unfinished
                var packs = RepositoryVersionInfo.Instance.InstalledPackages
                    .Where(p => p.ExecutionResult != ExecutionResult.Successful);
                foreach (var package in packs)
                    await PackageManager.Storage.DeletePackageAsync(package, CancellationToken.None);

                RepositoryVersionInfo.Reset();

                // check
                var actual = string.Join(" | ", RepositoryVersionInfo.Instance.InstalledPackages
                    .OrderBy(p => p.ComponentVersion)
                    .Select(p => $"{p.PackageType.ToString()[0]}:{p.ComponentVersion}-{p.ExecutionDate.Hour}")
                    .ToArray());
                var expected = "I:1.0-2 | P:1.1-5 | P:1.2-10";
                Assert.AreEqual(expected, actual);
            }).ConfigureAwait(false);
        }
        public async Task Packaging_DeleteAll()
        {
            await PackagingTestAsync(async () =>
            {
                await SavePackage("C1", "1.0", "02:00", "2016-01-01", PackageType.Install, ExecutionResult.Successful);
                await SavePackage("C1", "1.1", "05:00", "2016-01-06", PackageType.Patch, ExecutionResult.Successful);
                await SavePackage("C1", "1.2", "10:00", "2016-01-09", PackageType.Patch, ExecutionResult.Successful);

                // action
                await PackageManager.Storage.DeleteAllPackagesAsync(CancellationToken.None);

                // check
                Assert.IsFalse(RepositoryVersionInfo.Instance.InstalledPackages.Any());
            }).ConfigureAwait(false);
        }

        /* ==================================================================================== TOOLS */

        private void PackagingTest(Action callback)
        {
            NoRepoIntegrationTest(() =>
            {
                Providers.Instance.DataProvider
                    .GetExtension<IPackagingDataProviderExtension>()
                    .DeleteAllPackagesAsync(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                RepositoryVersionInfo.Reset();
                callback();
            });
        }
        private async Task PackagingTestAsync(Func<Task> callback)
        {
            await NoRepoIntegrationTestAsync(async () =>
            {
                await Providers.Instance.DataProvider
                    .GetExtension<IPackagingDataProviderExtension>()
                    .DeleteAllPackagesAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                RepositoryVersionInfo.Reset();
                await callback().ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private Task SavePackage(string id, string version, string execTime, string releaseDate, PackageType packageType, ExecutionResult result)
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
                Manifest = $"<Package type='{packageType}'/>"
            };
            return PackageManager.Storage.SavePackageAsync(package, CancellationToken.None);
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
            var manifest = Manifest.Parse(manifestXml, phase, true, new PackageParameter[0]);
            var executionContext = ExecutionContext.CreateForTest("packagePath", "targetPath", new string[0], "sandboxPath", manifest, phase, manifest.CountOfPhases, null, console ?? new StringWriter());
            var result = PackageManager.ExecuteCurrentPhase(manifest, executionContext);
            RepositoryVersionInfo.Reset();
            return result;
        }

    }
}
