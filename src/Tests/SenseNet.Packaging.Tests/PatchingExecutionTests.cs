using System;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Packaging.Tests
{
    [TestClass]
    public class PatchingExecutionTests : PatchingTestBase
    {
        [TestMethod]
        public void PatchingExec_Install_1New()
        {
            var patches = new[]
            {
                (ISnPatch)Inst("C1", "v1.0", null, ctx => ExecutionResult.Successful),
            };

            var installed = new ComponentInfo[0];
            var context = new PatchExecutionContext();
            var pm = new PatchManager();
            var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

            Assert.AreEqual(1, after.Length);
            Assert.AreEqual(1, executables.Length);
        }
        [TestMethod]
        public void PatchingExec_Install_1New1Skip()
        {
            var installed = new[]
            {
                Comp("C1", "v1.0")
            };
            var patches = new[]
            {
                // Will be skipped. The different version does not cause any error because this patch is irrelevant.
                (ISnPatch)Inst("C1", "v1.1", null, ctx => ExecutionResult.Successful),
                (ISnPatch)Inst("C2", "v2.3", null, ctx => ExecutionResult.Successful),
            };

            // ACTION
            var context = new PatchExecutionContext();
            var pm = new PatchManager();
            var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

            // ASSERT
            Assert.AreEqual(2, after.Length);
            Assert.AreEqual("C1", after[0].ComponentId);
            Assert.AreEqual(new Version(1, 0), after[0].Version);
            Assert.AreEqual("C2", after[1].ComponentId);
            Assert.AreEqual(new Version(2, 3), after[1].Version);

            Assert.AreEqual(1, executables.Length);
            Assert.AreEqual("C2", executables[0].ComponentId);
            Assert.AreEqual(new Version(2, 3), executables[0].Version);
        }
        [TestMethod]
        public void PatchingExec_Install_Duplicates()
        {
            var installed = new[]
            {
                Comp("C1", "v1.0")
            };
            var patches = new[]
            {
                // Will be skipped. The same id does not cause any error because this patch is irrelevant.
                (ISnPatch)Inst("C1", "v1.0", null, ctx => ExecutionResult.Successful),
                (ISnPatch)Inst("C1", "v1.1", null, ctx => ExecutionResult.Successful),
                // Would be executable but the same id causes an error.
                (ISnPatch)Inst("C2", "v1.0", null, ctx => ExecutionResult.Successful),
                (ISnPatch)Inst("C3", "v1.0", null, ctx => ExecutionResult.Successful),
                (ISnPatch)Inst("C3", "v2.3", null, ctx => ExecutionResult.Successful),
                (ISnPatch)Inst("C4", "v1.0", null, ctx => ExecutionResult.Successful),
                (ISnPatch)Inst("C4", "v2.3", null, ctx => ExecutionResult.Successful),
                (ISnPatch)Inst("C4", "v1.0", null, ctx => ExecutionResult.Successful),
            };

            // ACTION
            var context = new PatchExecutionContext();
            var pm = new PatchManager();
            var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

            // ASSERT
            Assert.AreEqual(1, after.Length);
            Assert.AreEqual(0, executables.Length);
            Assert.AreEqual(2, context.Errors.Length);
            Assert.IsTrue(context.Errors[0].Message.Contains("C3"));
            Assert.IsTrue(context.Errors[1].Message.Contains("C4"));
        }





        //[TestMethod]
        //public void PatchingExec___________()
        //{
        //    var patches = new[]
        //    {
        //        (ISnPatch)Inst("C1", "v1.0", null, ctx =>
        //        {
        //            return ExecutionResult.Successful;
        //        }),
        //        Patch("c1", "1.0 <= v <  2.0", "v2.0", null, ctx =>
        //        {
        //            return ExecutionResult.Successful;
        //        })
        //    };

        //    var installed = new ComponentInfo[0];
        //    var context = new PatchExecutionContext();
        //    var pm = new PatchManager();
        //    var executables = pm.GetExecutablePatches(patches, installed, context, out var after).ToArray();

        //    Assert.AreEqual(1, after.Length);
        //    Assert.AreEqual(2, executables.Length);
        //}
    }
}
