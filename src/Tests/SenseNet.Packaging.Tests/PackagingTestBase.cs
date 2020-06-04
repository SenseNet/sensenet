using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Packaging.Tests.Implementations;
using SenseNet.Tests;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Packaging.Tests
{
    [TestClass]
    public abstract class PackagingTestBase : TestBase
    {
        [AssemblyInitialize]
        public static void InitializeAssembly(TestContext context)
        {
            //TODO: Find a correct solution to avoid assembly duplications and remove this line
            var dummy = typeof(SenseNet.Portal.OData.ODataHandler).Name;
        }

        protected static StringBuilder _log;
        [TestInitialize]
        public void PrepareTest()
        {
            // preparing logger
            _log = new StringBuilder();
            var loggers = new[] { new PackagingTestLogger(_log) };
            var loggerAcc = new PrivateType(typeof(Logger));
            loggerAcc.SetStaticField("_loggers", loggers);

            var builder = CreateRepositoryBuilderForTest();

            builder.UsePackagingDataProviderExtension(new InMemoryPackageStorageProvider());

            RepositoryVersionInfo.Reset();
        }
        [TestCleanup]
        public void AfterTest()
        {
            // do nothing
        }

        protected override RepositoryBuilder CreateRepositoryBuilderForTestInstance()
        {
            var builder = base.CreateRepositoryBuilderForTestInstance();
            builder.UsePackagingDataProviderExtension(new TestPackageStorageProvider());

            return builder;
        }

        /*================================================= tools */

        protected Task SavePackage(string id, string version, string execTime, string releaseDate, PackageType packageType, ExecutionResult result)
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

            return PackageManager.Storage.SavePackageAsync(package, CancellationToken.None);
        }

        protected Manifest ParseManifestHead(string manifestXml)
        {
            var xml = new XmlDocument();
            xml.LoadXml(manifestXml);
            var manifest = new Manifest();
            Manifest.ParseHead(xml, manifest);
            return manifest;
        }
        protected Manifest ParseManifest(string manifestXml, int currentPhase, bool forcedReinstall = false)
        {
            var xml = new XmlDocument();
            xml.LoadXml(manifestXml);
            return Manifest.Parse(xml, currentPhase, true, new PackageParameter[0], forcedReinstall);
        }

        protected PackagingResult ExecutePhases(string manifestXml, TextWriter console = null)
        {
            var xml = new XmlDocument();
            xml.LoadXml(manifestXml);
            return ExecutePhases(xml, console);
        }
        protected PackagingResult ExecutePhases(XmlDocument manifestXml, TextWriter console = null)
        {
            var phase = -1;
            var errors = 0;
            PackagingResult result;
            do
            {
                result = ExecutePhase(manifestXml, ++phase, console);
                errors += result.Errors;
            } while (result.NeedRestart);
            result.Errors = errors;
            return result;
        }
        protected PackagingResult ExecutePhase(XmlDocument manifestXml, int phase, TextWriter console = null)
        {
            var manifest = Manifest.Parse(manifestXml, phase, true, new PackageParameter[0]);
            var executionContext = ExecutionContext.CreateForTest("packagePath", "targetPath", new string[0], "sandboxPath", manifest, phase, manifest.CountOfPhases, null, console);
            var result = PackageManager.ExecuteCurrentPhase(manifest, executionContext);
            RepositoryVersionInfo.Reset();
            return result;
        }

    }
}
