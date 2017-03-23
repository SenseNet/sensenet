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

        private Package ClonePackage(Package src)
        {
            return new Package
            {
                Id = src.Id,
                Name = src.Name,
                Edition = src.Edition,
                Description = src.Description,
                AppId = src.AppId,
                PackageLevel = src.PackageLevel,
                PackageType = src.PackageType,
                ReleaseDate = src.ReleaseDate,
                ExecutionDate = src.ExecutionDate,
                ExecutionResult = src.ExecutionResult,
                ApplicationVersion = src.ApplicationVersion,
                SenseNetVersion = src.SenseNetVersion,
                ExecutionError = src.ExecutionError
            };
        }

        public ApplicationInfo CreateInitialSenseNetVersion(string name, string edition, Version version, string description)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (edition == null)
                throw new ArgumentNullException(nameof(edition));
            if (version == null)
                throw new ArgumentNullException(nameof(version));
            if (name.Length == 0)
                throw new ArgumentException("The name cannot be empty.");
            if (edition.Length == 0)
                throw new ArgumentException("The edition cannot be empty.");

            var snAppInfo = LoadOfficialSenseNetVersion();
            if (snAppInfo != null)
                return snAppInfo;

            SavePackage(new Package
            {
                Name = name,
                Edition = edition,
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
                Edition = package.Edition,
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
                        Edition = package.Edition,
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
                    if (package.ApplicationVersion > appinfo.AcceptableVersion)
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
            throw new NotImplementedException();
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

            // cleaning packages table
            PackageManager.Storage.DeletePackagesExceptFirst();

            RepositoryVersionInfo.Reset();
        }

        [TestMethod]
        public void Packaging_GetRepositoryVersionInfo()
        {
            PackageManager.Storage.CreateInitialSenseNetVersion(
                "Sense/Net ECM", "Test", new Version(1, 42), "description");

            var appInfo = RepositoryVersionInfo.Instance.OfficialSenseNetVersion;
            Assert.AreEqual(1, appInfo.Version.Major);
            Assert.AreEqual(42, appInfo.Version.Minor);
        }

    }
}
