using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Packaging;
using SenseNet.Tools.SnAdmin.Testability;

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
            var result = SenseNet.Tools.SnAdmin.SnAdminRuntime.Main(args);

            // ASSERT
            console.Flush();
            var consoleText = console.GetStringBuilder().ToString();

            Assert.AreEqual(
                "CALL: PackageManager(Q:\\WebApp1\\Admin\\Pkg1, Q:\\WebApp1, 0, parameters:string[0])",
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
            var result = SenseNet.Tools.SnAdmin.SnAdminRuntime.Main(args);

            // ASSERT
            console.Flush();
            var consoleText = console.GetStringBuilder().ToString();

            Assert.AreEqual(
                "CALL: PackageManager(Q:\\WebApp1\\Admin\\Pkg2, Q:\\WebApp1, 0, parameters:string[0])",
                pkgMan.Log.ToString());
            Assert.AreEqual(
                $"CALL: LoadAssembliesFrom({Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)})" + Environment.NewLine +
                @"CALL: LoadAssembliesFrom(Q:\WebApp1\Admin\Pkg2\PackageCustomization)" + Environment.NewLine,
                typeResolver.Log.ToString());
        }
    }

    //UNDONE: move to new file: TestPackageManagerWrapper
    public class TestPackageManagerWrapper : IPackageManagerWrapper
    {
        public StringBuilder Log { get; } = new StringBuilder();

        public PackagingResult Execute(string packagePath, string targetDirectory, int phase, string[] parameters, TextWriter output)
        {
            Log.Append($"CALL: PackageManager({packagePath}, {targetDirectory}, {phase}, parameters:string[{parameters.Length}])");
            var result = new PackagingResult();
            var resultAcc = new PrivateObject(result);
            resultAcc.SetProperty("Successful", false);
            return result;
        }

        public string GetXmlSchema()
        {
            throw new NotImplementedException();
        }

        public string GetHelp()
        {
            throw new NotImplementedException();
        }
    }

    //UNDONE: move to new file: TestTypeResolverWrapper
    public class TestTypeResolverWrapper : ITypeResolverWrapper
    {
        public StringBuilder Log { get; } = new StringBuilder();

        public object CreateInstance(string typeName)
        {
            throw new NotImplementedException();
        }

        public object CreateInstance(string typeName, params object[] args)
        {
            throw new NotImplementedException();
        }

        public T CreateInstance<T>(string typeName) where T : new()
        {
            throw new NotImplementedException();
        }

        public T CreateInstance<T>(string typeName, params object[] args)
        {
            throw new NotImplementedException();
        }

        public Type FindTypeInAppDomain(string typeName, bool throwOnError = true)
        {
            throw new NotImplementedException();
        }

        public Assembly[] GetAssemblies()
        {
            throw new NotImplementedException();
        }

        public Type GetType(string typeName, bool throwOnError = true)
        {
            throw new NotImplementedException();
        }

        public Type[] GetTypesByBaseType(Type baseType)
        {
            throw new NotImplementedException();
        }

        public Type[] GetTypesByInterface(Type interfaceType)
        {
            throw new NotImplementedException();
        }

        public string[] LoadAssembliesFrom(string path)
        {
            Log.AppendLine($"CALL: LoadAssembliesFrom({path})");
            return new string[0];
        }
    }
}
