using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests;

[TestClass]
public class SeeOnlyTests : TestBase
{
    private class TestSnTracer : ISnTracer
    {
        public List<string> Lines { get; } = new List<string>();
        public void Write(string line) => Lines.Add(line);
        public void Flush() { /* do nothing */ }
    }

    [TestMethod]
    public void SeeOnly_InvalidPropertyAccess_RightMessage()
    {
        Test(() =>
        {
            var propertyName = nameof(Repository.Root.SharingData);
            GenericContent node;
            var tracer = new TestSnTracer();
            using (new CurrentUserBlock(User.PublicAdministrator))
            {
                // Has see-only for this user
                node = Repository.Root;

                Assert.IsTrue(node.Security.HasPermission(PermissionType.See));
                Assert.IsFalse(node.Security.HasPermission(PermissionType.Open));
                try
                {
                    var originalTracers = SnTrace.SnTracers.ToArray();
                    var repositoryCategoryEnabled = SnTrace.Repository.Enabled;
                    try
                    {
                        SnTrace.SnTracers.Clear();
                        SnTrace.SnTracers.AddRange(new[] {tracer});
                        SnTrace.Repository.Enabled = true;

                        // ACTION
                        var _ = node.SharingData;

                        // ASSERT-1
                        Assert.Fail("Expected InvalidOperationException was not thrown.");
                    }
                    finally
                    {
                        SnTrace.SnTracers.Clear();
                        SnTrace.SnTracers.AddRange(originalTracers);
                        SnTrace.Repository.Enabled = repositoryCategoryEnabled;
                    }
                }
                catch (InvalidOperationException ex)
                {
                    // ASSERT-2
                    Assert.IsTrue(ex.Message.Contains("Invalid property access attempt on a See-only node."));
                    Assert.IsFalse(ex.Message.Contains(node.Path));
                    Assert.IsFalse(ex.Message.Contains(propertyName));
                }
            }

            // ASSERT-3 Expected line:
            // ... Repository ... ERROR ... Invalid property access attempt on a See-only node: /Root. Property name: SharingData.
            var line = tracer.Lines.FirstOrDefault(x => x.Contains("Invalid property access attempt on a See-only node"));
            Assert.IsNotNull(line);
            Assert.IsTrue(line.Contains("\tRepository\t"));
            Assert.IsTrue(line.Contains("\tERROR\t"));
            Assert.IsTrue(line.Contains(node.Path));
            Assert.IsTrue(line.Contains(propertyName));
        });
    }
}