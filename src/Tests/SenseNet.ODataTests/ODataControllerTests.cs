using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.OData;
using SenseNet.Search;
using System.Reflection;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests;

#region

public class TestODataController1 : ODataController
{
    [ODataFunction] public object GetData(Content content, string id) { return $"From TestODataController1.GetData({id})"; }
    [ODataAction] public object SetData(Content content, string id, object value) { return null; }
    [ODataFunction] public static object NotOdataControllerMember1(Content content, string id) { return null; }
}

public class TestODataController2 : ODataController
{
    [ODataFunction] public object GetData(string id) { return new{From = $"TestODataController2.GetData({id})"}; }
    [ODataAction] public object SetData(string id, object value) { return new{Trace = $"{id} = {value}"}; }

    [ODataFunction]
    [AllowedRoles(N.R.Everyone, N.R.Visitor)]
    public static object NotOdataControllerMember2(Content content, string id)
    {
        return new {From = $"NotOdataControllerMember2({id})"};
    }
}

public class TestODataController3 : ODataController
{
    [ODataFunction] public object GetData3(Content content, string id) { return $"From TestODataController3.GetData3({id})"; }
    [ODataAction] public object SetData3(Content content, string id, object value) { return $"From TestODataController3.SetData3({id})"; }
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
            services.AddSingleton<IODataControllerFactory, ODataControllerFactory>();
            services.AddODataController<TestODataController1>();
            services.AddODataController<TestODataController2>();
        }, async () =>
        {
            //var xxx = await ODataGetAsync("/OData.svc/('Root')/NotOdataControllerMember2",
            //        "?id=234").ConfigureAwait(false);

            var controllerName = "TestODataController2";
var method = typeof(TestODataController2).GetMethod("GetData");
OperationCenter.AddMethod(method, controllerName);
var key = $"{controllerName}.GetData".ToLowerInvariant();
var op = OperationCenter.Operations[key];

            // ACTION
            var response = await ODataGetAsync($"/OData.svc/('Root')/{controllerName}/GetData", "?id=id21")
                .ConfigureAwait(false);

            // ASSERT
            AssertNoError(response);
            Assert.AreEqual(200, response.StatusCode);
            Assert.AreEqual("{\r\n  \"From\": \"TestODataController2.GetData(id21)\"\r\n}", response.Result);
        });
    }
    [TestMethod]
    public async Task ODC_Prototype_2()
    {
        await ODataTest2Async(services =>
        {
            services.AddSingleton<IODataControllerFactory, ODataControllerFactory>();
            services.AddODataController<TestODataController1>();
            services.AddODataController<TestODataController2>();
        }, async () =>
        {
            var controllerName = "TestODataController2";
var method = typeof(TestODataController2).GetMethod("SetData");
OperationCenter.AddMethod(method, controllerName);
var key = $"{controllerName}.SetData".ToLowerInvariant();
var op = OperationCenter.Operations[key];

            // ACTION
            var response = await ODataPostAsync("/OData.svc/('Root')/TestODataController2/SetData", null,
                    "{id:\"meaning of life\",value:42}")
                .ConfigureAwait(false);

            // ASSERT
            AssertNoError(response);
            Assert.AreEqual(200, response.StatusCode);
            Assert.AreEqual("{\r\n  \"Trace\": \"meaning of life = 42\"\r\n}", response.Result);
        });
    }

    [TestMethod]
    public void ODC_Registration_HappyPath()
    {
        // ACTION
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IODataControllerFactory, ODataControllerFactory>()

            // ACTION
            // register by default name (type name without namespace)
            .AddODataController<TestODataController1>()
            // register by custom name
            .AddODataController<TestODataController2>("Test-OData-Controller2")
            // register by multiple names (empty string and null are equivalent)
            .AddODataController<TestODataController3>(string.Empty)
            .AddODataController<TestODataController3>(typeof(TestODataController3).FullName)

            // overriding by name (last registration will be active)
            .AddODataController<TestODataController1>("builtin")
            .AddODataController<TestODataController2>("BUILTIN")

            //
            .BuildServiceProvider();

        // ASSERT
        var factory = services.GetRequiredService<IODataControllerFactory>();
        Assert.IsTrue(factory.ControllerTypes.ContainsKey("TestODataController1".ToLowerInvariant()));
        Assert.IsTrue(factory.ControllerTypes.ContainsKey("Test-OData-Controller2".ToLowerInvariant()));
        Assert.IsTrue(factory.ControllerTypes.ContainsKey("SenseNet.ODataTests.TestODataController3".ToLowerInvariant()));
        var controller1 = factory.CreateController("TestODataController1");
        var controller2 = factory.CreateController("Test-OData-Controller2");
        var controller3 = factory.CreateController("TestODataController3");
        var controller4 = factory.CreateController(typeof(TestODataController3).FullName);
        var controller5 = factory.CreateController("builtin");
        Assert.IsNotNull(controller1);
        Assert.IsNotNull(controller2);
        Assert.IsNotNull(controller3);
        Assert.IsNotNull(controller4);
        Assert.IsNotNull(controller5);
        Assert.AreEqual(typeof(TestODataController1), controller1.GetType());
        Assert.AreEqual(typeof(TestODataController2), controller2.GetType());
        Assert.AreEqual(typeof(TestODataController3), controller3.GetType());
        Assert.AreEqual(typeof(TestODataController3), controller4.GetType());
        Assert.AreEqual(typeof(TestODataController2), controller5.GetType());

        // Check controller's lifecycle (need to be transient)
        Assert.AreNotSame(
            factory.CreateController("TestODataController1"),
            factory.CreateController("TestODataController1"));
    }
}