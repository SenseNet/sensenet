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
using SenseNet.Packaging.Steps;

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
        private List<Package> __storage = new List<Package>();

        private List<Package> Storage
        {
            get
            {
                if (!DatabaseEnabled)
                    throw new Exception("Database is not available");
                return __storage;
            }
        }

        public bool DatabaseEnabled { get; set; }

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
            target.ComponentId = source.ComponentId;
            target.PackageType = source.PackageType;
            target.ReleaseDate = source.ReleaseDate;
            target.ExecutionDate = source.ExecutionDate;
            target.ExecutionResult = source.ExecutionResult;
            target.ComponentVersion = source.ComponentVersion;
            target.ExecutionError = source.ExecutionError;
        }

        public IDataProcedureFactory DataProcedureFactory
        {
            get { throw new NotSupportedException(); }
            set { /* do nothing */ }
        }

        /* ================================================================================================= IPackageStorageProvider */

        public IEnumerable<ComponentInfo> LoadInstalledComponents()
        {
            var nullVersion = new Version(0, 0);
            var componentInfos = new Dictionary<string, ComponentInfo>();
            foreach (var package in Storage
                .Where(p => p.PackageType == PackageType.Install
                    && p.ExecutionResult == ExecutionResult.Successful))
            {
                var componentId = package.ComponentId;
                ComponentInfo component = null;
                if (!componentInfos.TryGetValue(componentId, out component))
                {
                    component = new ComponentInfo
                    {
                        ComponentId = package.ComponentId,
                        Version = package.ComponentVersion,
                        AcceptableVersion = package.ComponentVersion,
                        Description = package.Description
                    };
                    componentInfos.Add(componentId, component);
                }

                if (package.ComponentVersion > (component.AcceptableVersion ?? nullVersion))
                    component.AcceptableVersion = package.ComponentVersion;
            }

            foreach (var package in Storage
                .Where(p => (p.PackageType == PackageType.Install || p.PackageType == PackageType.Patch)))
            {
                var componentId = package.ComponentId;
                ComponentInfo component = null;
                if (componentInfos.TryGetValue(componentId, out component))
                {
                    if ((package.ComponentVersion > (component.AcceptableVersion ?? nullVersion))
                        && package.ExecutionResult == ExecutionResult.Successful)
                        component.AcceptableVersion = package.ComponentVersion;
                    if (package.ComponentVersion > (component.Version ?? nullVersion))
                        component.Version = package.ComponentVersion;
                }
            }
            return componentInfos.Values.ToArray();
        }

        public IEnumerable<Package> LoadInstalledPackages()
        {
            return Storage
                //.Where(p => p.ExecutionResult != ExecutionResult.Unfinished)
                .Select(ClonePackage)
                .ToArray();
        }

        public void SavePackage(Package package)
        {
            if (package.Id > 0)
                throw new InvalidOperationException("Only new package can be saved.");

            package.Id = ++_id;
            Storage.Add(ClonePackage(package));

            RepositoryVersionInfo.Reset();
        }

        public void UpdatePackage(Package package)
        {
            var existing = Storage.FirstOrDefault(p => p.Id == package.Id);
            if(existing == null)
                throw new InvalidOperationException("Package does not exist. Id: " + package.Id);
            UpdatePackage(package, existing);
        }

        public bool IsPackageExist(string componentId, PackageType packageType, Version version)
        {
            throw new NotImplementedException();
        }

        public void DeletePackage(Package package)
        {
            throw new NotImplementedException();
        }

        public void DeletePackagesExceptFirst()
        {
            if (Storage.Count == 0)
                return;
            throw new NotImplementedException();
        }

        // ================================================================================================= Test tools

        public int GetRecordCount()
        {
            return Storage.Count;
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

    public class ForbiddenStep : Step
    {
        public override void Execute(ExecutionContext context)
        {
            throw new PackagingException("Do not use this step.");
        }
    }

    public class TestStepThatSimulatesInstallingDatabase : Step
    {
        public override void Execute(ExecutionContext context)
        {
            ((TestPackageStorageProvider)PackageManager.Storage).DatabaseEnabled = true;
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

            var storage = new TestPackageStorageProvider();
            storage.DatabaseEnabled = true;
            PackageManager.StorageFactory = new TestPackageStorageProviderFactory(storage);

            RepositoryVersionInfo.Reset();
        }

        #region // ========================================= Manifest parsing tests

        [TestMethod]
        public void Packaging_ParseHead_Description()
        {
            Assert.AreEqual("Description text",
                (ParseManifestHead(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component2</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Description>Description text</Description>
                        </Package>")).Description);
        }

        [TestMethod]
        public void Packaging_ParseHead_WrongRootName()
        {
            try
            {
                ParseManifestHead(@"<?xml version='1.0' encoding='utf-8'?>
                        <Manifest type='Install'>
                            <ComponentId>Component2</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                        </Manifest>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.WrongRootName, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_ParseHead_MissingPackageType()
        {
            try
            {
                ParseManifestHead(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package level='Install'>
                            <ComponentId>Component2</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.MissingPackageType, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_ParseHead_InvalidPackageType()
        {
            try
            {
                ParseManifestHead(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='asdf'>
                            <ComponentId>Component2</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.InvalidPackageType, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_ParseHead_MissingComponentId()
        {
            try
            {
                ParseManifestHead(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.MissingComponentId, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_ParseHead_InvalidComponentId()
        {
            try
            {
                ParseManifestHead(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId />
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.InvalidComponentId, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_ParseHead_MissingVersion()
        {
            try
            {
                ParseManifestHead(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component1</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.MissingVersion, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_ParseHead_InvalidVersion()
        {
            try
            {
                ParseManifestHead(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component1</ComponentId>
                            <Version>asdf</Version>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.InvalidVersion, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_ParseHead_MissingReleaseDate()
        {
            try
            {
                ParseManifestHead(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component1</ComponentId>
                            <Version>1.0</Version>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.MissingReleaseDate, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_ParseHead_InvalidReleaseDate()
        {
            try
            {
                ParseManifestHead(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component1</ComponentId>
                            <Version>1.0</Version>
                            <ReleaseDate>asdf</ReleaseDate>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.InvalidReleaseDate, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_ParseHead_TooBigReleaseDate()
        {
            try
            {
                ParseManifestHead(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component1</ComponentId>
                            <Version>1.0</Version>
                            <ReleaseDate>9999-01-01</ReleaseDate>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.TooBigReleaseDate, e.ErrorType);
            }
        }


        [TestMethod]
        public void Packaging_ParseDependency_ExactVersion()
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
        public void Packaging_ParseDependency_VersionControl()
        {
            // version minVersion minVersionExclusive maxVersion maxVersionExclusive
            ValidateDependencyVersion("version='1.0'", "1.0", "1.0", false, false);
            ValidateDependencyVersion("version='1.0' minVersion='1.0'", PackagingExceptionType.UnexpectedVersionAttribute);
            ValidateDependencyVersion("version='1.0' maxVersion='1.0'", PackagingExceptionType.UnexpectedVersionAttribute);
            ValidateDependencyVersion("version='1.0' minVersionExclusive='1.0'", PackagingExceptionType.UnexpectedVersionAttribute);
            ValidateDependencyVersion("version='1.0' maxVersionExclusive='1.0'", PackagingExceptionType.UnexpectedVersionAttribute);

            ValidateDependencyVersion("minVersion='1.0'", "1.0", null, false, false);
            ValidateDependencyVersion("minVersion='1.0' minVersionExclusive='1.0'", PackagingExceptionType.DoubleMinVersionAttribute);
            ValidateDependencyVersion("minVersion='1.0' maxVersion='2.0'", "1.0", "2.0", false, false);
            ValidateDependencyVersion("minVersion='1.0' maxVersionExclusive='2.0'", "1.0", "2.0", false, true);

            ValidateDependencyVersion("minVersionExclusive='1.0'", "1.0", null, true, false);
            ValidateDependencyVersion("minVersionExclusive='1.0' maxVersion='2.0'", "1.0", "2.0", true, false);
            ValidateDependencyVersion("minVersionExclusive='1.0' maxVersionExclusive='2.0'", "1.0", "2.0", true, true);

            ValidateDependencyVersion("maxVersion='2.0'", null, "2.0", false, false);
            ValidateDependencyVersion("maxVersion='2.0' maxVersionExclusive='2.0'", PackagingExceptionType.DoubleMaxVersionAttribute);

            ValidateDependencyVersion("maxVersionExclusive='2.0'", null, "2.0", false, true);
        }
        private void ValidateDependencyVersion(string versionAttrs, string minVer, string maxVer, bool minEx, bool maxEx)
        {
            var manifest = ParseManifestHead($@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component1</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Dependencies>
                                <Dependency id='Component1' {versionAttrs} />
                            </Dependencies>
                        </Package>");

            var dependencies = manifest.Dependencies.ToArray();
            var dependency = dependencies[0];
            Assert.AreEqual(minVer, dependency.MinVersion?.ToString());
            Assert.AreEqual(maxVer, dependency.MaxVersion?.ToString());
            Assert.AreEqual(minEx, dependency.MinVersionIsExclusive);
            Assert.AreEqual(maxEx, dependency.MaxVersionIsExclusive);
        }
        private void ValidateDependencyVersion(string versionAttrs, PackagingExceptionType errorType)
        {
            try
            {
                ParseManifestHead($@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component1</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Dependencies>
                                <Dependency id='Component1' {versionAttrs} />
                            </Dependencies>
                        </Package>");
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(errorType, e.ErrorType);
            }
        }

        [TestMethod]
        public void Packaging_ParseDependency_MissingId()
        {
            try
            {
                ParseManifestHead(@"<?xml version='1.0' encoding='utf-8'?>
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
        public void Packaging_ParseDependency_EmptyId()
        {
            try
            {
                ParseManifestHead(@"<?xml version='1.0' encoding='utf-8'?>
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
        public void Packaging_ParseDependency_MissingVersion()
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
        public void Packaging_ParseDependency_UnexpectedVersion()
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
        public void Packaging_ParseDependency_DoubleMinVersion()
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
        public void Packaging_ParseDependency_DoubleMaxVersion()
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
        public void Packaging_ParseParameters_DefaultValues()
        {
            // action
            var manifest = ParseManifest(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component2</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Parameters>
                                <Parameter name='@param1' />
                                <Parameter name='@param2'>42</Parameter>
                                <Parameter name='@param3'>value3</Parameter>
                            </Parameters>
                        </Package>", 0);

            // check
            Assert.AreEqual("@param1:[null], @param2:42, @param3:value3", 
                string.Join(", ", manifest.Parameters.Select(x => $"{x.Key}:{x.Value ?? "[null]"}").ToArray()));
        }
        [TestMethod]
        public void Packaging_ParseParameters_MissingParameterName()
        {
            try
            {
                ParseManifest(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component2</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Parameters>
                                <Parameter name='@param1' />
                                <Parameter/>
                                <Parameter name='@param3' />
                            </Parameters>
                        </Package>", 0);
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.MissingParameterName, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_ParseParameters_InvalidParameterName()
        {
            try
            {
                ParseManifest(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component2</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Parameters>
                                <Parameter name='@param1' />
                                <Parameter name='param2' />
                                <Parameter name='@param3' />
                            </Parameters>
                        </Package>", 0);
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.InvalidParameterName, e.ErrorType);
            }
        }
        [TestMethod]
        public void Packaging_ParseParameters_DuplicatedParameter()
        {
            try
            {
                ParseManifest(@"<?xml version='1.0' encoding='utf-8'?>
                        <Package type='Install'>
                            <ComponentId>Component2</ComponentId>
                            <ReleaseDate>2017-01-01</ReleaseDate>
                            <Version>1.0</Version>
                            <Parameters>
                                <Parameter name='@param1' />
                                <Parameter name='@param2' />
                                <Parameter name='@param3' />
                                <Parameter name='@param2' />
                            </Parameters>
                        </Package>", 0);
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.DuplicatedParameter, e.ErrorType);
            }
        }

        [TestMethod]
        public void Packaging_ParsePhases_PhaseIndexValidation()
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

            // error 1
            try
            {
                ExecutePhase(manifestXml, -1);
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.InvalidPhase, e.ErrorType);
            }

            // error 2
            try
            {
                ExecutePhase(manifestXml, 4);
                Assert.Fail("PackagingException was not thrown.");
            }
            catch (PackagingException e)
            {
                Assert.AreEqual(PackagingExceptionType.InvalidPhase, e.ErrorType);
            }

            // normal flow
            ExecutePhase(manifestXml, 0);
            ExecutePhase(manifestXml, 1);
            ExecutePhase(manifestXml, 2);

            var component = RepositoryVersionInfo.Instance.Components.FirstOrDefault();
            Assert.IsNotNull(component);
            Assert.IsNotNull(component.AcceptableVersion);
            Assert.AreEqual("4.42", component.Version.ToString());
            Assert.AreEqual("4.42", component.AcceptableVersion.ToString());
            Assert.AreEqual("Component42", component.ComponentId);
        }

        #endregion

        #region // ========================================= Checking dependency tests

        [TestMethod]
        public void Packaging_DependencyCheck_MissingDependency()
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
        public void Packaging_DependencyCheck_CannotInstallExistingComponent()
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
        public void Packaging_DependencyCheck_CannotUpdateMissingComponent()
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
        public void Packaging_DependencyCheck_TargetVersionTooSmall()
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
        public void Packaging_DependencyCheck_DependencyVersion()
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
        public void Packaging_DependencyCheck_DependencyMinimumVersion()
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
        public void Packaging_DependencyCheck_DependencyMaximumVersion()
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
        public void Packaging_DependencyCheck_DependencyMinimumVersionExclusive()
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
        public void Packaging_DependencyCheck_DependencyMaximumVersionExclusive()
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
        public void Packaging_DependencyCheck_LoggingDependencies()
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

        #endregion

        #region // ========================================= Component lifetime tests

        [TestMethod]
        public void Packaging_Install_SnInitialComponent()
        {
            // simulate database before installation
            ((TestPackageStorageProvider) PackageManager.Storage).DatabaseEnabled = false;

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
                                    <TestStepThatSimulatesInstallingDatabase />
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
        public void Packaging_Install_NoSteps()
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
        public void Packaging_Install_ThreePhases()
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
        public void Packaging_Patch_ThreePhases()
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
        public void Packaging_Patch_Faulty()
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
        public void Packaging_Patch_FixFaulty()
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
        public void Packaging_Patch_FixMoreFaulty()
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
        #endregion

        #region // ========================================= RepositoryVersionInfo tests

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

        #endregion

        /*================================================= tools */

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
