using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.OData;
using SenseNet.Search;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests;

#region

public class TestODataController1 : ODataController
{
    private DataProvider _dataProvider;
    private IIndexPopulator _indexPopulator;

    public TestODataController1(DataProvider dataProvider, IIndexPopulator indexPopulator)
    {
        _dataProvider = dataProvider;
        _indexPopulator = indexPopulator;
    }

    [ODataFunction] public object GetData(Content content, string id) { return $"From TestODataController1.GetData({id})"; }
    [ODataAction] public object SetData(Content content, string id, object value) { return null; }
    [ODataFunction] public static object NotOdataControllerMember(Content content, string id) { return null; }
}

public class TestODataController2 : ODataController
{
    private IBlobProvider _blobProvider;
    private ISearchEngine _searchEngine;

    public TestODataController2(IBlobProvider blobProvider, ISearchEngine searchEngine)
    {
        _blobProvider = blobProvider;
        _searchEngine = searchEngine;
    }

    [ODataFunction] public object GetData(Content content, string id) { return new{From = $"TestODataController2.GetData({id})"}; }
    [ODataAction] public object SetData(Content content, string id, object value) { return null; }
    [ODataFunction] public static object NotOdataControllerMember(Content content, string id) { return null; }
}

#endregion

[TestClass]
public class ODataControllerTests : ODataTestBase
{
    [TestMethod]
    public async Task ODC_Prototype_1()
    {
        await ODataTest2Async(services =>
        {
            services.AddSingleton<IODataControllerResolver, ODataControllerResolver>();
            services.AddTransient<TestODataController1>();
            services.AddTransient<TestODataController2>();
        }, async () =>
        {
            // ACTION
            var response = await ODataGetAsync("/OData.svc/('Root')/TestODataController2/GetData", "?id=id21")
                .ConfigureAwait(false);

            // ASSERT
            AssertNoError(response);
            Assert.AreEqual(200, response.StatusCode);
            Assert.AreEqual("{\r\n  \"From\": \"TestODataController2.GetData(id21)\"\r\n}", response.Result);
        });
    }
}