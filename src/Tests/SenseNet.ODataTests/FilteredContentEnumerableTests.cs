using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Linq;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.OData;
using SenseNet.OData.Parser;
using SenseNet.Search;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class FilteredContentEnumerableTests : ODataTestBase
    {
        [TestMethod]
        public void FilteredEnumerable_NodeOrContent()
        {
            ODataTest(() =>
            {
                var refNodes = new NodeList<Node>(ContentType.GetContentTypes().Select(ct => ct.Id).ToArray());
                var refContents = refNodes.Select(Content.Create).ToArray();

                var enumerable1 = new FilteredContentEnumerable(refNodes, null, null, 5, 0);
                var enumerable2 = new FilteredContentEnumerable(refContents, null, null, 5, 0);

                var a = enumerable1.Select(x => x.Name).ToArray();
                var b = enumerable2.Select(x => x.Name).ToArray();
                AssertSequenceEqual(a, b);
            });
        }
        [TestMethod]
        public void FilteredEnumerable_Sort()
        {
            ODataTest(() =>
            {
                // ARRANGE-1 Sort by Name desc
                var allContentTypes = ContentType.GetContentTypes();
                var refNodes = new NodeList<Node>(allContentTypes.Select(ct => ct.Id).ToArray());
                var sort1 = new[] { new SortInfo("Name", true) };
                var expectedNames1 = allContentTypes
                    .OrderByDescending(x => x.Name)
                    .Select(x => x.Name).ToArray();
                // ACTION-1
                var enumerable1 = new FilteredContentEnumerable(refNodes, null, sort1, 0, 0);
                // ASSERT-1
                var actualNames1 = enumerable1.Select(x => x.Name).ToArray();
                AssertSequenceEqual(expectedNames1, actualNames1);

                // ARRANGE-2 Sort by ParentId, Name
                var sort2 = new[] { new SortInfo("ParentId"), new SortInfo("Name") };
                var expectedNames2 = allContentTypes
                    .OrderBy(x => x.ParentId).ThenBy(x => x.Name)
                    .Select(x => x.Name).ToArray();
                // ACTION-2
                var enumerable2 = new FilteredContentEnumerable(refNodes, null, sort2, 0, 0);
                // ASSERT-2
                var actualNames2 = enumerable2.Select(x => x.Name).ToArray();
                AssertSequenceEqual(expectedNames2, actualNames2);
            });
        }
        [TestMethod]
        public void FilteredEnumerable_Windowed()
        {
            ODataTest(() =>
            {
                // ARRANGE
                var allContentTypes = ContentType.GetContentTypes();
                var refNodes = new NodeList<Node>(allContentTypes.Select(ct => ct.Id).ToArray());
                var sort = new[] { new SortInfo("Name", true) };
                var expectedNames = allContentTypes
                    .OrderByDescending(x => x.Name)
                    .Skip(5).Take(10)
                    .Select(x => x.Name).ToArray();
                // ACTION (two windows)
                var enumerable1 = new FilteredContentEnumerable(refNodes, null, sort, 5, 5);
                var enumerable2 = new FilteredContentEnumerable(refNodes, null, sort, 5, 10);
                // ASSERT
                var actualNames = enumerable1.Union(enumerable2).Select(x => x.Name).ToArray();
                AssertSequenceEqual(expectedNames, actualNames);
            });
        }
        [TestMethod]
        public void FilteredEnumerable_Filtered()
        {
            ODataTest(() =>
            {
                // ARRANGE
                var allContentTypes = ContentType.GetContentTypes();
                var refNodes = new NodeList<Node>(allContentTypes.Select(ct => ct.Id).ToArray());
                var sort = new[] { new SortInfo("Name") };
                var filter = new ODataParser().Parse("substringof('Folder', Name) eq true"); ;

                var expectedNames = allContentTypes
                    .Where(x => x.Name.Contains("Folder"))
                    .OrderBy(x => x.Name)
                    .Select(x => x.Name).ToArray();

                // ACTION
                var enumerable = new FilteredContentEnumerable(refNodes, filter, sort, 0, 0);

                // ASSERT
                var actualNames = enumerable.Select(x => x.Name).ToArray();
                AssertSequenceEqual(expectedNames, actualNames);
            });
        }

    }
}
