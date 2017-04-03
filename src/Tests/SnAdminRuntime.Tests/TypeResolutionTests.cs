using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Tools.SnAdmin.testability;

namespace SnAdminRuntime.Tests
{
    [TestClass]
    public class TypeResolutionTests
    {
        protected string[] DefaultDirs { get; private set; }
        protected string[] DefaultFiles { get; private set; }
        protected Dictionary<string, XmlDocument> DefaultManifests { get; private set; }

        protected void Initialize()
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
            xml1.LoadXml(@"<Package type='Product' level='Tool'>
  <Name>Sense/Net ECM</Name>
  <ReleaseDate>2016-12-21</ReleaseDate>
  <Description>|package description|</Description>
  <Parameters>
    <Parameter name='@source' description='Source description' />
    <Parameter name='@target' description='Target description'>/Root</Parameter>
    <Parameter name='@resetSecurity' description='ResetSecurity description'>False</Parameter>
    <Parameter name='@sourceIsRelativeTo' description='SourceIsRelativeTo description'>Package</Parameter>
    <Parameter name='@logLevel' description='LogLevel description'>Verbose</Parameter>
    <Parameter name='@traceMessage' description='|parameter description|'>|default value|</Parameter>
  </Parameters>
  <Steps>
    <Phase>
      <Trace>@traceMessage</Trace>
    </Phase>
  </Steps>
</Package>");

            var xml2 = new XmlDocument();
            DefaultManifests.Add(@"Q:\WebApp1\Admin\Pkg2\manifest.xml", xml2);
            xml2.LoadXml(@"<Package type='Product' level='Tool'>
  <Name>Sense/Net ECM</Name>
  <ReleaseDate>2016-12-21</ReleaseDate>
  <Description>Another helper package</Description>
  <Parameters>
    <Parameter name='@source' />
    <Parameter name='@target'>/Root</Parameter>
    <Parameter name='@resetSecurity'>False</Parameter>
    <Parameter name='@sourceIsRelativeTo'>Package</Parameter>
    <Parameter name='@logLevel'>Verbose</Parameter>
  </Parameters>
  <Steps>
	  <Phase>
		  <!-- <StartRepository /> -->
		  <Import Source='@source' Target='@target' ResetSecurity='@resetSecurity' SourceIsRelativeTo='@sourceIsRelativeTo' LogLevel='@logLevel' />
	  </Phase>
  </Steps>
</Package>");
        }

        [TestMethod]
        public void SnAdminRuntime_1()
        {
            // ARRANGE
            var disk = new TestDisk(DefaultDirs, DefaultFiles, DefaultManifests);
            Disk.Instance = disk;
            var args = new[] { "Pkg1", "LOGLEVEL:Console" };
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
