using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.AppModel;
using SenseNet.IntegrationTests.Infrastructure;

namespace SenseNet.IntegrationTests.TestCases
{
    public class AppModelTestCases : TestCaseBase
    {
        public void AppModel_ResolveFromPredefinedPaths_First()
        {
            IntegrationTest(() =>
            {
                var backup = Indexing.IsOuterSearchEngineEnabled;
                Indexing.IsOuterSearchEngineEnabled = false;
                try
                {
                    var paths = new[]
                    {
                        "/Root/AA/BB/CC",
                        "/Root/AA",
                        "/Root/System",
                        "/Root",
                    };

                    // ACTION
                    var nodeHead = ApplicationResolver.ResolveFirstByPaths(paths);

                    // ASSERT
                    Assert.IsNotNull(nodeHead);
                    Assert.IsTrue(nodeHead.Path == "/Root/System", "Path does not equal the expected");
                }
                finally
                {
                    Indexing.IsOuterSearchEngineEnabled = backup;
                }
            });
        }
        public void AppModel_ResolveFromPredefinedPaths_All()
        {
            IntegrationTest(() =>
            {
                var backup = Indexing.IsOuterSearchEngineEnabled;
                Indexing.IsOuterSearchEngineEnabled = false;
                try
                {
                    var paths = new[]
                    {
                        "/Root/AA/BB/CC",
                        "/Root/AA",
                        "/Root/System",
                        "/Root",
                    };

                    // ACTION
                    var nodeHeads = ApplicationResolver.ResolveAllByPaths(paths, false).ToArray();

                    // ASSERT
                    Assert.AreEqual(2, nodeHeads.Length);
                    Assert.AreEqual("/Root/System", nodeHeads[0].Path);
                    Assert.AreEqual("/Root", nodeHeads[1].Path);
                }
                finally
                {
                    Indexing.IsOuterSearchEngineEnabled = backup;
                }
            });
        }
    }
}
