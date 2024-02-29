using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.OData;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Storage.Security;
using Task = System.Threading.Tasks.Task;
using Microsoft.Extensions.Logging;

namespace SenseNet.ODataTests;

#region

public class TestODataController1 : ODataController
{
    [ODataFunction] public object GetData(string id) { return $"From TestODataController1.GetData({id})"; }
    [ODataAction] public object SetData(string id, object value) { return null; }

    [ODataFunction]
    [ContentTypes(nameof(User))]
    public IUser GetBoss() { return new TestUser($"Boss of user '{this.Content.Name}'", -42); }

    public object NotOdataControllerMember1(string id) { return null; }
    [ODataFunction] public static object NotOdataControllerMember2(Content content, string id) { return null; }
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
    public async Task ODC_ExecuteControllerFunction()
    {
        await ODataTest2Async(services =>
        {
            services.AddSenseNetODataController<TestODataController1>();
            services.AddSenseNetODataController<TestODataController2>();
        }, async () =>
        {
            // ACTION
            var response = await ODataGetAsync($"/OData.svc/('Root')/TestODataController2/GetData", "?id=id21")
                .ConfigureAwait(false);

            // ASSERT
            AssertNoError(response);
            Assert.AreEqual(200, response.StatusCode);
            Assert.AreEqual("{\r\n  \"From\": \"TestODataController2.GetData(id21)\"\r\n}", response.Result);
        });
    }
    [TestMethod]
    public async Task ODC_ExecuteControllerAction()
    {
        await ODataTest2Async(services =>
        {
            services.AddSenseNetODataController<TestODataController1>();
            services.AddSenseNetODataController<TestODataController2>();
        }, async () =>
        {
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
    public async Task ODC_ExecuteRestrictedControllerFunction()
    {
        await ODataTest2Async(services =>
        {
            services.AddSenseNetODataController<TestODataController1>();
        }, async () =>
        {
            // ACTION-1 Called for non-user content
            var response = await ODataGetAsync("/OData.svc/('Root')/TestODataController1/GetBoss", null)
                .ConfigureAwait(false);

            // ASSERT-1
            var error = GetError(response);
            Assert.AreNotEqual(200, response.StatusCode);
            Assert.AreEqual("Operation not found: TestODataController1.GetBoss()", error.Message);

            // ACTION-2 Called for user content (id of 'Admin' = 1)
            response = await ODataGetAsync("/OData.svc/content(1)/TestODataController1/GetBoss", null)
                .ConfigureAwait(false);

            // ASSERT-1
            AssertNoError(response);
            Assert.AreEqual(200, response.StatusCode);
            dynamic responseUser = JsonConvert.DeserializeObject(response.Result);
            Assert.IsNotNull(responseUser);
            Assert.AreEqual("Boss of user 'Admin'", responseUser.Name.ToString());
        });
    }

    private class TestDataControllerFactoryLogger : ILogger<ODataControllerFactory>
    {
        public List<string> Entries { get; } = new();
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception exception, Func<TState, Exception, string> formatter) => Entries.Add(formatter(state, exception));
        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull { throw new NotImplementedException(); }
    }
    [TestMethod]
    public void ODC_Registration_HappyPath()
    {
        // ACTION
        var services = new ServiceCollection()
            .AddSingleton<ILogger<ODataControllerFactory>, TestDataControllerFactoryLogger>()
            .AddSingleton<IODataControllerFactory, ODataControllerFactory>() // only in this test

            // ACTION
            // register by default name (type name without namespace)
            .AddSenseNetODataController<TestODataController1>()
            // register by custom name
            .AddSenseNetODataController<TestODataController2>("Test-OData-Controller2")
            // register by multiple names (empty string and null are equivalent)
            .AddSenseNetODataController<TestODataController3>(string.Empty)
            .AddSenseNetODataController<TestODataController3>(typeof(TestODataController3).FullName)

            // overriding by name (last registration will be active)
            .AddSenseNetODataController<TestODataController1>("builtin")
            .AddSenseNetODataController<TestODataController2>("BUILTIN")

            //
            .BuildServiceProvider();

        // ASSERT
        var factory = services.GetRequiredService<IODataControllerFactory>();
        factory.Initialize(); // required in tests that doesn't start the repository

        Assert.AreEqual(typeof(TestODataController1), factory.GetControllerType("TestODataController1".ToLowerInvariant()));
        Assert.AreEqual(typeof(TestODataController2), factory.GetControllerType("Test-OData-Controller2".ToLowerInvariant()));
        Assert.AreEqual(typeof(TestODataController2), factory.GetControllerType("builtin"));
        Assert.AreEqual(typeof(TestODataController3), factory.GetControllerType("TestODataController3".ToLowerInvariant()));
        Assert.AreEqual(typeof(TestODataController3), factory.GetControllerType("SenseNet.ODataTests.TestODataController3".ToLowerInvariant()));
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

        // check log
        var logger = (TestDataControllerFactoryLogger)services.GetRequiredService<ILogger<ODataControllerFactory>>();
        var entry = logger.Entries.FirstOrDefault(x => x.StartsWith("ODataControllerFactory initialized"));
        Assert.AreEqual("ODataControllerFactory initialized. Controller names and types: " +
                        "'testodatacontroller1': SenseNet.ODataTests.TestODataController1, " +
                        "'test-odata-controller2': SenseNet.ODataTests.TestODataController2, " +
                        "'testodatacontroller3': SenseNet.ODataTests.TestODataController3, " +
                        "'sensenet.odatatests.testodatacontroller3': SenseNet.ODataTests.TestODataController3, " +
                        "'builtin': SenseNet.ODataTests.TestODataController2.", entry);
    }
}