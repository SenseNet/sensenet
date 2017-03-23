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
            target.Name = source.Name;
            target.Description = source.Description;
            target.AppId = source.AppId;
            target.PackageLevel = source.PackageLevel;
            target.PackageType = source.PackageType;
            target.ReleaseDate = source.ReleaseDate;
            target.ExecutionDate = source.ExecutionDate;
            target.ExecutionResult = source.ExecutionResult;
            target.ApplicationVersion = source.ApplicationVersion;
            target.SenseNetVersion = source.SenseNetVersion;
            target.ExecutionError = source.ExecutionError;
        }

        public ApplicationInfo CreateInitialSenseNetVersion(string name, Version version, string description)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (version == null)
                throw new ArgumentNullException(nameof(version));
            if (name.Length == 0)
                throw new ArgumentException("The name cannot be empty.");

            var snAppInfo = LoadOfficialSenseNetVersion();
            if (snAppInfo != null)
                return snAppInfo;

            SavePackage(new Package
            {
                Name = name,
                Description = description,
                SenseNetVersion = version,
                AppId = null,
                PackageType = PackageType.Product,
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
                .OrderBy(p => p.SenseNetVersion)
                .LastOrDefault();

            if (package == null)
                return null;

            return new ApplicationInfo
            {
                Name = package.Name,
                AppId = package.AppId,
                Version = package.SenseNetVersion,
                AcceptableVersion = package.SenseNetVersion,
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
                        Name = package.Name,
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

        public bool IsPackageExist(string appId, PackageType packageType, PackageLevel packageLevel, Version version)
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

        [TestMethod]
        public void Packaging_GetRepositoryVersionInfo()
        {
            PackageManager.Storage.CreateInitialSenseNetVersion(
                "Sense/Net ECM", new Version(1, 42), "description");

            var appInfo = RepositoryVersionInfo.Instance.OfficialSenseNetVersion;
            Assert.AreEqual(1, appInfo.Version.Major);
            Assert.AreEqual(42, appInfo.Version.Minor);
        }

        [TestMethod]
        public void Packaging_Install_FirstComponent()
        {
            var appId = Guid.NewGuid().ToString();

            var manifestSrc = @"<Package type='Application' level='Install'>
                                    <Name>Sense/Net ECM</Name>
                                    <Description>Sensenet Core</Description>
                                    <AppId>" + appId + @"</AppId>
                                    <VersionControl target='1.1' expectedMin='1.0' />
                                    <ReleaseDate>2014-04-01</ReleaseDate>
                                    <Steps><Trace>Tool is running.</Trace></Steps>
                                </Package>";

            var xml = new XmlDocument();
            xml.LoadXml(manifestSrc);

            var manifestAcc = new PrivateType(typeof(Manifest));
            var pkgManAcc = new PrivateType(typeof(PackageManager));

            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, 0, true);
            var console = new StringWriter();
            for (int phase = 0; phase < manifest.CountOfPhases; phase++)
            {
                var executionContext = ExecutionContext.CreateForTest("packagePath", "targetPath", new string[0], "sandboxPath", manifest, phase, manifest.CountOfPhases, null, console);
                var result = (PackagingResult)pkgManAcc.InvokeStatic("ExecuteCurrentPhase", manifest, executionContext);
            }

            var log = _log.ToString();
            Assert.IsTrue(log.Contains("Tool is running."));
            Assert.IsTrue(log.Contains("Errors: 0"));
        }
        [TestMethod]
        public void Packaging_Install_FirstComponentTwice()
        {
            var appId = Guid.NewGuid().ToString();

            var manifestSrc = @"<Package type='Application' level='Install'>
                                    <Name>Sense/Net ECM</Name>
                                    <Description>Sensenet Core</Description>
                                    <AppId>" + appId + @"</AppId>
                                    <VersionControl target='1.1' expectedMin='1.0' />
                                    <ReleaseDate>2014-04-01</ReleaseDate>
                                    <Steps><Trace>Tool is running.</Trace></Steps>
                                </Package>";

            var xml = new XmlDocument();
            xml.LoadXml(manifestSrc);

            var manifestAcc = new PrivateType(typeof(Manifest));
            var pkgManAcc = new PrivateType(typeof(PackageManager));
            var console = new StringWriter();

            // first
            var manifest = (Manifest)manifestAcc.InvokeStatic("Parse", xml, 0, true);
            for (int phase = 0; phase < manifest.CountOfPhases; phase++)
            {
                var executionContext = ExecutionContext.CreateForTest("packagePath", "targetPath", new string[0], "sandboxPath", manifest, phase, manifest.CountOfPhases, null, console);
                var result = (PackagingResult)pkgManAcc.InvokeStatic("ExecuteCurrentPhase", manifest, executionContext);
            }

            // second
            try
            {
                manifest = (Manifest) manifestAcc.InvokeStatic("Parse", xml, 0, true);
                Assert.Fail("PackagePreconditionException exception was not thrown.");
            }
            catch (PackagePreconditionException)
            {

            }
        }
    }
}
