using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Tools.SnAdmin.Testability;
using SnAdminRuntime.Tests.Implementations;

namespace SnAdminRuntime.Tests
{
    [TestClass]
    public class TypeResolutionTests
    {
        #region Initialization
        private string[] DefaultDirs { get; set; }
        private string[] DefaultFiles { get; set; }
        private Dictionary<string, XmlDocument> DefaultManifests { get; set; }

        [TestInitialize]
        public void InitializeTest()
        {
            DefaultDirs = new[]
            {
                @"Q:\WebApp1",
                @"Q:\WebApp1\bin",
                @"Q:\WebApp1\Admin",
                @"Q:\WebApp1\Admin\bin",
                @"Q:\WebApp1\Admin\Pkg1",
                @"Q:\WebApp1\Admin\Pkg1\import",
                @"Q:\WebApp1\Admin\Pkg1\schema",
                @"Q:\WebApp1\Admin\Pkg2",
                @"Q:\WebApp1\Admin\Pkg2\PackageCustomization",
                @"Q:\WebApp1\Admin\Pkg3",
                @"Q:\WebApp1\Admin\Pkg3\plugins",
                @"Q:\WebApp1\Admin\Pkg3\plugins\phase1",
                @"Q:\WebApp1\Admin\Pkg3\plugins\phase3",
            };

            DefaultFiles = new[]
            {
                // first item is the Assembly.GetExecutingAssembly().Location
                @"Q:\WebApp1\Admin\bin\SnAdmin.exe",
                @"Q:\WebApp1\Admin\Pkg1\manifest.xml",
                @"Q:\WebApp1\Admin\Pkg2\manifest.xml",
                @"Q:\WebApp1\Admin\Pkg2.zip",
                @"Q:\WebApp1\Admin\Pkg3\manifest.xml",
                @"Q:\WebApp1\Admin\Pkg3.zip",
                @"Q:\WebApp1\web.config",
                @"Q:\WebApp1\bin\sandboxitem1.exe",
                @"Q:\WebApp1\bin\sandboxitem2.exe",
            };

            DefaultManifests = new Dictionary<string, XmlDocument>();

            var xml1 = new XmlDocument();
            DefaultManifests.Add(@"Q:\WebApp1\Admin\Pkg1\manifest.xml", xml1);
            xml1.LoadXml(@"<Package type='Tool'>
  <ComponentId>Sense/Net ECM</ComponentId>
  <Version>1.0</Version>
  <ReleaseDate>2016-12-21</ReleaseDate>
  <Steps>
    <Phase><Trace>Message1</Trace></Phase>
    <Phase><Trace>Message2</Trace></Phase>
    <Phase><Trace>Message3</Trace></Phase>
  </Steps>
</Package>");

            var xml2 = new XmlDocument();
            DefaultManifests.Add(@"Q:\WebApp1\Admin\Pkg2\manifest.xml", xml2);
            xml2.LoadXml(@"<Package type='Tool'>
  <ComponentId>Sense/Net ECM</ComponentId>
  <Version>1.0</Version>
  <ReleaseDate>2016-12-21</ReleaseDate>
  <Steps>
    <Phase><Trace>Message1</Trace></Phase>
    <Phase><Trace>Message2</Trace></Phase>
    <Phase><Trace>Message3</Trace></Phase>
  </Steps>
</Package>");

            var xml3 = new XmlDocument();
            DefaultManifests.Add(@"Q:\WebApp1\Admin\Pkg3\manifest.xml", xml3);
            xml3.LoadXml(@"<Package type='Tool'>
  <ComponentId>Sense/Net ECM</ComponentId>
  <Version>1.0</Version>
  <ReleaseDate>2016-12-21</ReleaseDate>
  <Steps>
    <Phase extensions='plugins\phase1'><Trace>Message1</Trace></Phase>
    <Phase><Trace>Message2</Trace></Phase>
    <Phase extensions='plugins\phase3'><Trace>Message3</Trace></Phase>
  </Steps>
</Package>");
        }

        #endregion

        [TestMethod]
        public void SnAdminRuntime_RunFolderOnly()
        {
            // ARRANGE
            var disk = new TestDisk(DefaultDirs, DefaultFiles, DefaultManifests);
            Disk.Instance = disk;
            var typeResolver = new TestTypeResolverWrapper();
            TypeResolverWrapper.Instance = typeResolver;
            var pkgMan = new TestPackageManagerWrapper();
            PackageManagerWrapper.Instance = pkgMan;
            var console = new StringWriter();
            SenseNet.Tools.SnAdmin.SnAdminRuntime.Output = console;
            var args = new[] { @"Q:\WebApp1\Admin\Pkg1", @"TargetDirectory:""Q:\WebApp1""", "PHASE:0", "LOG:", "LOGLEVEL:Console" };

            // ACT
            SenseNet.Tools.SnAdmin.SnAdminRuntime.Main(args);

            // ASSERT
            Assert.AreEqual(
                "CALL: PackageManager(Q:\\WebApp1\\Admin\\Pkg1, Q:\\WebApp1, 0, parameters:string[0])" + Environment.NewLine,
                pkgMan.Log.ToString());
            Assert.AreEqual(
                $"CALL: LoadAssembliesFrom({Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)})" + Environment.NewLine,
                typeResolver.Log.ToString());
        }

        [TestMethod]
        public void SnAdminRuntime_PackageCustomization()
        {
            // ARRANGE
            var disk = new TestDisk(DefaultDirs, DefaultFiles, DefaultManifests);
            Disk.Instance = disk;
            var typeResolver = new TestTypeResolverWrapper();
            TypeResolverWrapper.Instance = typeResolver;
            var pkgMan = new TestPackageManagerWrapper();
            PackageManagerWrapper.Instance = pkgMan;
            var console = new StringWriter();
            SenseNet.Tools.SnAdmin.SnAdminRuntime.Output = console;
            var args = new[] { @"Q:\WebApp1\Admin\Pkg2", @"TargetDirectory:""Q:\WebApp1""", "PHASE:0", "LOG:", "LOGLEVEL:Console" };

            // ACT
            SenseNet.Tools.SnAdmin.SnAdminRuntime.Main(args);

            // ASSERT

            Assert.AreEqual(
                "CALL: PackageManager(Q:\\WebApp1\\Admin\\Pkg2, Q:\\WebApp1, 0, parameters:string[0])" + Environment.NewLine,
                pkgMan.Log.ToString());
            Assert.AreEqual(
                $"CALL: LoadAssembliesFrom({Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)})" + Environment.NewLine +
                @"CALL: LoadAssembliesFrom(Q:\WebApp1\Admin\Pkg2\PackageCustomization)" + Environment.NewLine,
                typeResolver.Log.ToString());
        }

        [TestMethod]
        public void SnAdminRuntime_PhaseCustomization()
        {
            // ARRANGE
            var disk = new TestDisk(DefaultDirs, DefaultFiles, DefaultManifests);
            Disk.Instance = disk;
            var typeResolver = new TestTypeResolverWrapper();
            TypeResolverWrapper.Instance = typeResolver;
            var pkgMan = new TestPackageManagerWrapper();
            PackageManagerWrapper.Instance = pkgMan;
            var console = new StringWriter();
            SenseNet.Tools.SnAdmin.SnAdminRuntime.Output = console;
            var args1 = new[] { @"Q:\WebApp1\Admin\Pkg3", @"TargetDirectory:""Q:\WebApp1""", "PHASE:0", "LOG:", "LOGLEVEL:Console" };
            var args2 = new[] { @"Q:\WebApp1\Admin\Pkg3", @"TargetDirectory:""Q:\WebApp1""", "PHASE:1", "LOG:", "LOGLEVEL:Console" };
            var args3 = new[] { @"Q:\WebApp1\Admin\Pkg3", @"TargetDirectory:""Q:\WebApp1""", "PHASE:2", "LOG:", "LOGLEVEL:Console" };

            // ACT
            SenseNet.Tools.SnAdmin.SnAdminRuntime.Main(args1);
            SenseNet.Tools.SnAdmin.SnAdminRuntime.Main(args2);
            SenseNet.Tools.SnAdmin.SnAdminRuntime.Main(args3);

            // ASSERT
            Assert.AreEqual(
                "CALL: PackageManager(Q:\\WebApp1\\Admin\\Pkg3, Q:\\WebApp1, 0, parameters:string[0])" + Environment.NewLine +
                "CALL: PackageManager(Q:\\WebApp1\\Admin\\Pkg3, Q:\\WebApp1, 1, parameters:string[0])" + Environment.NewLine +
                "CALL: PackageManager(Q:\\WebApp1\\Admin\\Pkg3, Q:\\WebApp1, 2, parameters:string[0])" + Environment.NewLine,
                pkgMan.Log.ToString());
            Assert.AreEqual(
                $"CALL: LoadAssembliesFrom({Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)})" + Environment.NewLine +
                @"CALL: LoadAssembliesFrom(Q:\WebApp1\Admin\Pkg3\plugins\phase1)" + Environment.NewLine +
                $"CALL: LoadAssembliesFrom({Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)})" + Environment.NewLine +
                $"CALL: LoadAssembliesFrom({Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)})" + Environment.NewLine +
                @"CALL: LoadAssembliesFrom(Q:\WebApp1\Admin\Pkg3\plugins\phase3)" + Environment.NewLine,
                typeResolver.Log.ToString());
        }
    }
}
