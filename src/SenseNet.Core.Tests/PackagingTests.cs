using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Packaging;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.Core.Tests
{
    #region Implementations
    internal class TestPackageStorageProviderFactory : IPackageStorageProviderFactory
    {
        // An idea: to parallelize test execution, need to store the provider instance in the thread context.
        private IPackageStorageProvider _provider;
        public TestPackageStorageProviderFactory(IPackageStorageProvider provider)
        {
            _provider = provider;
        }
        public IPackageStorageProvider CreateProvider()
        {
            return _provider;
        }
    }

    public class TestPackageStorageProvider : IPackageStorageProvider
    {
        private int _id;
        private List<Package> _storage = new List<Package>();

        private Package ClonePackage(Package source)
        {
            var target = new Package();
            UpdatePackage(source, target);
            return target;
        }
        private void UpdatePackage(Package source, Package target)
        {
            target.Id = source.Id;
            target.Description = source.Description;
            target.AppId = source.AppId;
            target.PackageLevel = source.PackageLevel;
            target.ReleaseDate = source.ReleaseDate;
            target.ExecutionDate = source.ExecutionDate;
            target.ExecutionResult = source.ExecutionResult;
            target.ApplicationVersion = source.ApplicationVersion;
            target.ExecutionError = source.ExecutionError;
        }

        public ApplicationInfo CreateInitialSenseNetVersion(Version version, string description)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));

            var snAppInfo = LoadOfficialSenseNetVersion();
            if (snAppInfo != null)
                return snAppInfo;

            SavePackage(new Package
            {
                Description = description,
                ApplicationVersion = version,
                AppId = null,
                PackageLevel = PackageLevel.Install,
                ReleaseDate = DateTime.UtcNow,
                ExecutionDate = DateTime.UtcNow,
                ExecutionResult = ExecutionResult.Successful
            });

            return LoadOfficialSenseNetVersion();
        }

        public ApplicationInfo LoadOfficialSenseNetVersion()
        {
            var package = _storage
                .Where(p =>
                    (p.ExecutionResult != ExecutionResult.Faulty && p.ExecutionResult != ExecutionResult.Unfinished)
                    && p.AppId == null && p.PackageLevel == PackageLevel.Install)
                .OrderBy(p => p.ApplicationVersion)
                .LastOrDefault();

            if (package == null)
                return null;

            return new ApplicationInfo
            {
                AppId = package.AppId,
                Version = package.ApplicationVersion,
                AcceptableVersion = package.ApplicationVersion,
                Description = package.Description
            };
        }

        public IEnumerable<ApplicationInfo> LoadInstalledApplications()
        {
            var appInfos = new Dictionary<string, ApplicationInfo>();
            foreach (var package in _storage.Where(p => p.PackageLevel == PackageLevel.Install))
            {
                var appId = package.AppId;
                if (appId == null)
                    continue;

                ApplicationInfo appinfo;
                if (!appInfos.TryGetValue(appId, out appinfo))
                {
                    appinfo = new ApplicationInfo
                    {
                        AppId = package.AppId,
                        Version = package.ApplicationVersion,
                        AcceptableVersion = null,
                        Description = package.Description
                    };
                    appInfos.Add(appId, appinfo);
                }

                var nullVersion = new Version(0, 0);
                if (package.ExecutionResult == ExecutionResult.Successful)
                {
                    if (package.ApplicationVersion > (appinfo.AcceptableVersion ?? nullVersion))
                        appinfo.AcceptableVersion = package.ApplicationVersion;
                }
                else
                {
                    if (package.ApplicationVersion > (appinfo.Version ?? nullVersion))
                        appinfo.Version = package.ApplicationVersion;
                }
            }
            return appInfos.Values.ToArray();
        }

        public IEnumerable<Package> LoadInstalledPackages()
        {
            return _storage.Select(ClonePackage).ToArray();
        }

        public void SavePackage(Package package)
        {
            if (package.Id > 0)
                throw new InvalidOperationException("Only new package can be saved.");

            package.Id = ++_id;
            _storage.Add(ClonePackage(package));

            RepositoryVersionInfo.Reset();
        }

        public void UpdatePackage(Package package)
        {
            var existing = _storage.FirstOrDefault(p => p.Id == package.Id);
            if(existing == null)
                throw new InvalidOperationException("Package does not exist. Id: " + package.Id);
            UpdatePackage(package, existing);
        }

        public bool IsPackageExist(string appId, PackageLevel packageLevel, Version version)
        {
            throw new NotImplementedException();
        }

        public void DeletePackage(Package package)
        {
            throw new NotImplementedException();
        }

        public void DeletePackagesExceptFirst()
        {
            if (_storage.Count == 0)
                return;
            throw new NotImplementedException();
        }

        //================================================== Test tools

        public int GetRecordCount()
        {
            return _storage.Count;
        }
    }

    public class PackagingTestLogger : IPackagingLogger
    {
        public LogLevel AcceptedLevel { get { return LogLevel.File; } }
        private StringBuilder _sb;
        public PackagingTestLogger(StringBuilder sb)
        {
            _sb = sb;
        }
        public string LogFilePath { get { return "[in memory]"; } }
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

    #endregion

    [TestClass]
    public class PackagingTests
    {
        private static StringBuilder _log;

        [TestInitialize]
        public void PrepareTest()
        {
            // preparing logger
            _log = new StringBuilder();
            var loggers = new[] { new PackagingTestLogger(_log) };
            var loggerAcc = new PrivateType(typeof(Logger));
            loggerAcc.SetStaticField("_loggers", loggers);

            PackageManager.StorageFactory = new TestPackageStorageProviderFactory(new TestPackageStorageProvider());

            RepositoryVersionInfo.Reset();
        }

        //================================================= new manifest

        [TestMethod]
        public void Packaging_Dependency_ExactVersion()
        {
            var manifest = ParseManifestHead(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component2</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Dependencies>
                                <Dependency id='Component1' version='1.0.1' />
                            </Dependencies>
                            <Steps>
                                <Trace>Package is running.</Trace>
                            </Steps>
                        </Package>");

            var dependencies = manifest.Dependencies.ToArray();
            var dependency = dependencies[0];
            Assert.AreEqual(1, dependencies.Length);
            Assert.AreEqual("Component1", dependency.Id);
            Assert.AreEqual("1.0.1", dependency.MinVersion.ToString());
            Assert.AreEqual("1.0.1", dependency.MaxVersion.ToString());
            Assert.IsFalse(dependency.MinVersionIsExclusive);
            Assert.IsFalse(dependency.MaxVersionIsExclusive);
        }
        [TestMethod]
        public void Packaging_Dependency_MissingId()
        {
            try
            {
                var manifest = ParseManifestHead(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component2</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Dependencies>
                                <Dependency version='1.0.1' />
                            </Dependencies>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.MissingDependencyId, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_Dependency_EmptyId()
        {
            try
            {
                var manifest = ParseManifestHead(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component2</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Dependencies>
                                <Dependency id='' version='1.0.1' />
                            </Dependencies>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.EmptyDependencyId, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_Dependency_MissingVersion()
        {
            try
            {
                ParseManifestHead(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component2</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Dependencies>
                                <Dependency id='Component1' />
                            </Dependencies>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.MissingDependencyVersion, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_Dependency_UnexpectedVersion()
        {
            try
            {
                ParseManifestHead(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component2</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Dependencies>
                                <Dependency id='Component1' version='1.0' maxVersion='2.0' />
                            </Dependencies>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.UnexpectedVersionAttribute, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_Dependency_DoubleMinVersion()
        {
            try
            {
                ParseManifestHead(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component2</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Dependencies>
                                <Dependency id='Component1' minVersion='1.0' minVersionExclusive='2.0' />
                            </Dependencies>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.DoubleMinVersionAttribute, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_Dependency_DoubleMaxVersion()
        {
            try
            {
                ParseManifestHead(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component2</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Dependencies>
                                <Dependency id='Component1' maxVersion='1.0' maxVersionExclusive='2.0' />
                            </Dependencies>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.DoubleMaxVersionAttribute, e.ErrorType);
            }
        }

        [TestMethod]
        public void Packaging_Install_NoDependency()
        {
            var recordCountBefore = GetDbRecordCount();

            // action
            var result = ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>MyCompany.MyComponent</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Steps>
                                <Trace>Package is running.</Trace>
                            </Steps>
                        </Package>");

            // assert
            var log = _log.ToString();
            Assert.IsTrue(log.Contains("Package is running."));
            Assert.IsTrue(result.Successful);
            Assert.AreEqual(0, result.Errors);
            Assert.AreEqual(recordCountBefore + 1, GetDbRecordCount());
        }
        [TestMethod]
        public void Packaging_Install_MissingDependency()
        {
            var recordCountBefore = GetDbRecordCount();
            var expectedErrorType = PackagingExceptionType.DependencyNotFound;
            var actualErrorType = PackagingExceptionType.NotDefined;

            // action
            try
            {
                var result = ExecutePhases(@"<?xml version='1.0' encoding='utf-8'?>
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
                Assert.Fail("PackagingException was not thrown. Expected error type: PackagingExceptionType.DependencyNotFound");
            }
            catch (PackagingException e)
            {
                actualErrorType = e.ErrorType;
            }

            // assert
            Assert.AreEqual(actualErrorType, expectedErrorType);
            Assert.AreEqual(recordCountBefore, GetDbRecordCount());
        }

        //================================================= old manifest

        [TestMethod]
        public void OldPackaging_GetRepositoryVersionInfo()
        {
            PackageManager.Storage.CreateInitialSenseNetVersion(new Version(1, 42), "description");

            var appInfo = RepositoryVersionInfo.Instance.OfficialSenseNetVersion;
            Assert.AreEqual(1, appInfo.Version.Major);
            Assert.AreEqual(42, appInfo.Version.Minor);
        }

        [TestMethod]
        public void Packaging_Install_FirstComponentTwice()
        {
            Assert.Inconclusive();
        }



        private Manifest ParseManifestHead(string manifestXml)
        {
            var xml = new XmlDocument();
            xml.LoadXml(manifestXml);
            var manifest = new Manifest();
            Manifest.ParseHead(xml, manifest);
            return manifest;
        }
        private Manifest ParseManifest(string manifestXml)
        {
            var xml = new XmlDocument();
            xml.LoadXml(manifestXml);
            return Manifest.Parse(xml, 0, true);
        }

        private PackagingResult ExecutePhases(string manifestXml, TextWriter console = null)
        {
            var xml = new XmlDocument();
            xml.LoadXml(manifestXml);
            return ExecutePhases(xml, console);
        }
        private PackagingResult ExecutePhases(XmlDocument manifestXml, TextWriter console = null)
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
        private PackagingResult ExecutePhase(XmlDocument manifestXml, int phase, TextWriter console)
        {
            var manifest = Manifest.Parse(manifestXml, phase, true);
            var executionContext = ExecutionContext.CreateForTest("packagePath", "targetPath", new string[0], "sandboxPath", manifest, phase, manifest.CountOfPhases, null, console);
            return PackageManager.ExecuteCurrentPhase(manifest, executionContext);
        }
        private int GetDbRecordCount()
        {
            return ((TestPackageStorageProvider) PackageManager.Storage).GetRecordCount();
        }
    }
}
