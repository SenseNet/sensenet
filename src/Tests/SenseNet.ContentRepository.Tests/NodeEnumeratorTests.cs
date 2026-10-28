using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;
using SenseNet.Search.Querying;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class NodeEnumeratorTests : TestBase
    {
        [DataTestMethod]
        [DataRow("Name:Apple", "Apple")]
        [DataRow("Name:OnlyElsewhere", "")]
        [DataRow("Name:Apple Name:Pear", "Apple,Pear")]
        [DataRow(null, "Apple,Pear,Banana")]
        public void NodeEnumerator_IndexedFilter_PreservesParent(string filter, string expectedNames)
        {
            Test(true, () =>
            {
                SystemFolder parent;
                SystemFolder[] children;
                using (new SystemAccount())
                {
                    Providers.Instance.SecurityHandler.CreateAclEditor()
                        .Allow(Repository.Root.Id, User.Administrator.Id, false, PermissionType.PermissionTypes)
                        .ApplyAsync(CancellationToken.None).GetAwaiter().GetResult();
                    var root = CreateFolder(Repository.Root, "NodeEnumeratorTests");
                    parent = CreateFolder(root, "Parent");
                    var otherParent = CreateFolder(root, "OtherParent");
                    children = new[]
                    {
                        CreateFolder(parent, "Apple"),
                        CreateFolder(parent, "Pear"),
                        CreateFolder(parent, "Banana")
                    };
                    CreateFolder(otherParent, "Apple");
                    CreateFolder(otherParent, "Pear");
                    CreateFolder(otherParent, "OnlyElsewhere");
                }

                using var enumerator = new TestNodeEnumerator(parent.Path, filter);
                var result = enumerator.GetChildren(parent.Id);

                var expected = children.Where(n => expectedNames.Split(',').Contains(n.Name))
                    .Select(n => n.Id).ToArray();
                CollectionAssert.AreEquivalent(expected, result.Identifiers.ToArray());
            });
        }

        private static SystemFolder CreateFolder(Node parent, string name)
        {
            var folder = new SystemFolder(parent) { Name = name };
            folder.SaveAsync(CancellationToken.None).GetAwaiter().GetResult();
            return folder;
        }

        private class TestNodeEnumerator : NodeEnumerator
        {
            public TestNodeEnumerator(string path, string filter)
                : base(path, ExecutionHint.ForceIndexedEngine, filter, null)
            {
            }

            public QueryResult GetChildren(int parentId) => QueryChildrenFromIndex(parentId);
        }
    }
}
