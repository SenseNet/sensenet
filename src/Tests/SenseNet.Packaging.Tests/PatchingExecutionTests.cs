using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Packaging.Tests
{
    [TestClass]
    public class PatchingExecutionTests : PatchingTestBase
    {
        [TestMethod]
        public void PatchingExec_()
        {
            var patches = new[]
            {
                (ISnPatch)new ComponentInstaller
                {
                    ComponentId = "C1",
                    Version = new Version(1, 0),
                    Description = "C1 installer description",
                    ReleaseDate = new DateTime(2020, 07, 31),
                    Execute = ctx =>
                    {
                        Logger.LogMessage("asdf");
                        return ExecutionResult.Successful;
                    }
                },
                new SnPatch
                {
                    ComponentId = "C1",
                    Version = new Version(2, 0),
                    Description = "C1 patch description",
                    ReleaseDate = new DateTime(2020, 07, 31),
                    Boundary = ParseBoundary("1.0 <= v <  2.0"),
                }
            };

            var installed = new ComponentInfo[0];
            var context = new PatchExecutionContext();
            var pm = new PatchManager();
            var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

            Assert.AreEqual(2, executables.Length);
            Assert.AreEqual(1, after.Length);
        }
    }
}
