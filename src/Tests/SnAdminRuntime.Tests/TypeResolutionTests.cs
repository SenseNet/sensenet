using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Tools.SnAdmin.testability;

namespace SnAdminRuntime.Tests
{
    [TestClass]
    public class TypeResolutionTests
    {
        [TestMethod]
        public void SnAdminRuntime_1()
        {
            // ARRANGE
            var disk = new TestDisk(DefaultDirs, DefaultFiles, DefaultManifests);
            Disk.Instance = disk;
            var args = new[] { "Pkg1", "LOGLEVEL:Console" };
            var console = new StringWriter();
            SnAdminRuntime.Output = console;

            // ACT
            var result = SnAdmin.Main(args);

            // ASSERT
            var consoleText = console.GetStringBuilder().ToString();
            Assert.AreEqual(0, result);
            Assert.AreEqual(1, activator.ExePaths.Count);
            Assert.AreEqual(1, activator.Args.Count);
            Assert.IsTrue(activator.Args[0].Contains("Pkg1"));
            Assert.IsTrue(activator.Args[0].Contains("PHASE:0"));
        }
    }
}
