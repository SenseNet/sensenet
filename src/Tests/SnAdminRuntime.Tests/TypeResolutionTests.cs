using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Tools.SnAdmin.Testability;

namespace SnAdminRuntime.Tests
{
    [TestClass]
    public class TypeResolutionTests
    {
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
            };

            DefaultFiles = new[]
            {
                // first item is the Assembly.GetExecutingAssembly().Location
                @"Q:\WebApp1\Admin\bin\SnAdmin.exe",
                @"Q:\WebApp1\Admin\Pkg1\manifest.xml",
                @"Q:\WebApp1\Admin\Pkg2\manifest.xml",
                @"Q:\WebApp1\Admin\Pkg2.zip",
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
    <Phase ><Trace>Message1</Trace></Phase>
    <Phase><Trace>Message2</Trace></Phase>
    <Phase><Trace>Message3</Trace></Phase>
  </Steps>
</Package>");
        }

        [TestMethod]
        public void SnAdminRuntime_1()
        {
            // ARRANGE
            var disk = new TestDisk(DefaultDirs, DefaultFiles, DefaultManifests);
            Disk.Instance = disk;
            var args = new[] { @"Q:\WebApp1\Admin\Pkg1", @"TargetDirectory:""Q:\WebApp1""", "LOG:", "LOGLEVEL:Console" };
            var console = new StringWriter();
            SenseNet.Tools.SnAdmin.SnAdminRuntime.Output = console;

            // ACT
            var result = SenseNet.Tools.SnAdmin.SnAdminRuntime.Main(args);

            // ASSERT
            var consoleText = console.GetStringBuilder().ToString();

            Assert.Inconclusive();
        }
    }
}
