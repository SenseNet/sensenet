using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.OData;
using SenseNet.OperationFramework;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.ODataTests;

[TestClass]
public class OperationFrameworkTests : ODataTestBase
{
    readonly CancellationToken _cancel = new CancellationToken();

    private class CustomUIAction : UIAction
    {
        public override Task<object> ExecuteAsync(Content content, HttpContext httpContext, object[] parameters)
        {
            var result = new { Name = parameters[0], Value = parameters[1] };
            return Task.FromResult((object)result);
        }
    }

    private static class CustomTools
    {
        public static object StaticMethod(Content content, HttpContext httpContext, string s, int p)
        {
            return new { Message = $"static,{s}={p}" };
        }
        public static Task<object> StaticAsyncMethod(Content content, HttpContext httpContext, string s, int p)
        {
            object result = new { Message = $"static,async,{s}={p}" };
            return Task.FromResult(result);
        }
    }
    private class CustomODataController : ODataController
    {
        public object Method(string s, int p)
        {
            return new { Message = $"controller,{s}={p}" };
        }
        public Task<object> AsyncMethod(string s, int p)
        {
            object result = new { Message = $"controller,async,{s}={p}" };
            return Task.FromResult(result);
        }
    }

    [TestMethod]
    public async Task OperationFramework_EmptyUIAction_GetPost()
    {
        await ODataTestAsync(async () =>
        {
            var actionRoot = await Node.LoadNodeAsync("/Root/(apps)/GenericContent", _cancel);
            var action = new Operation(actionRoot) {Name = "Action1"};
            action.Parameters = "string p1, int p2";
            action.UIDescriptor = @"{
    controls: [
        { name: 'p1', type: 'SnTextBox', displayName: '...', description: '...', mapping: 'string p1'}},
        { name: 'p2', type: 'SnTextBox', displayName: '...', description: '...', mapping: 'int p2'}},
        { name: 'Submit1', type: 'SnSaveButton', displayName: '...', description: '...' }
    ],
    action: { type: 'SubmitAll' }
}";
            await action.SaveAsync(_cancel);

            // ACT-1
            var resource = $"/OData.svc/Root/('Content')/{action.Name}";
            var response = await ODataGetAsync(resource, "");

            Assert.AreEqual(action.UIDescriptor, response.Result);

            // ACT-2
            response = await ODataPostAsync(resource, "", @"{""p1"": ""Value1"", ""p2"": 42}");

            Assert.AreEqual($@"{{""message"":""{typeof(UIAction).FullName}.ExecuteAsynccalled.Parameters:p1=Value1,p2=42""}}", response.Result.Replace(" ", "").Replace("\r", "").Replace("\n", ""));
        });
    }
    [TestMethod]
    public async Task OperationFramework_CustomUIAction_Post()
    {
        await ODataTestAsync(async () =>
        {
            var actionRoot = await Node.LoadNodeAsync("/Root/(apps)/GenericContent", _cancel);
            var action = new Operation(actionRoot) { Name = "Action1" };
            action.Parameters = "string p1, int p2";
            action.UIDescriptor = @"{...}";
            action.ActionTypeName = nameof(CustomUIAction);
            await action.SaveAsync(_cancel);

            // ACT
            var response = await ODataPostAsync("/OData.svc/Root/('Content')/Action1", "",
                @"{""p1"": ""Name"", ""p2"": 42}");

            Assert.AreEqual($@"{{""Name"":""Name"",""Value"":42}}", response.Result.Replace(" ", "").Replace("\r", "").Replace("\n", ""));
        });
    }
    [TestMethod]
    public async Task OperationFramework_ClassAndMethod_SyncAsync_Post()
    {
        await ODataTest2Async(services =>
        {
            services.AddNodeObserver<AppStorageInvalidator>();
        }, async () =>
        {
            var actionRoot = await Node.LoadNodeAsync("/Root/(apps)/GenericContent", _cancel);
            var action = new Operation(actionRoot) { Name = "Action1" };
            action.Parameters = "string s, int p";
            action.UIDescriptor = @"{...}";
            action.ClassName = typeof(CustomTools).FullName;
            action.MethodName = nameof(CustomTools.StaticMethod);
            await action.SaveAsync(_cancel);

            // ACT-1
            var response = await ODataPostAsync("/OData.svc/Root/('Content')/Action1", "",
                @"{""s"": ""Value"", ""p"": 123}");

            // ASSERT-1
            Assert.AreEqual($@"{{""Message"":""static,Value=123""}}", response.Result.Replace(" ", "").Replace("\r", "").Replace("\n", ""));


            // ASSIGN-2
            action = await Node.LoadAsync<Operation>(action.Id, _cancel);
            action.MethodName = nameof(CustomTools.StaticAsyncMethod);
            await action.SaveAsync(_cancel);

            // ACT-2
            response = await ODataPostAsync("/OData.svc/Root/('Content')/Action1", "",
                @"{""s"": ""Value"", ""p"": 987}");

            // ASSERT-2
            Assert.AreEqual($@"{{""Message"":""static,async,Value=987""}}", response.Result.Replace(" ", "").Replace("\r", "").Replace("\n", ""));
        });
    }
    [TestMethod]
    public async Task OperationFramework_ControllerMethod_SyncAsync_Post()
    {
        var controllerName = "RegisteredControllerName";

        await ODataTest2Async(services =>
        {
            services.AddNodeObserver<AppStorageInvalidator>();
            services.AddSingleton<IODataControllerFactory, ODataControllerFactory>();
            services.AddSenseNetODataController<CustomODataController>(controllerName);
        }, async () =>
        {
            var actionRoot = await Node.LoadNodeAsync("/Root/(apps)/GenericContent", _cancel);
            var action = new Operation(actionRoot) { Name = "Action1" };
            action.Parameters = "string s, int p";
            action.UIDescriptor = @"{...}";
            action.ClassName = controllerName;
            action.MethodName = nameof(CustomODataController.Method);
            await action.SaveAsync(_cancel);

            // ACT-1
            var response = await ODataPostAsync("/OData.svc/Root/('Content')/Action1", "",
                @"{""s"": ""Value"", ""p"": 123}");

            // ASSERT-1
            Assert.AreEqual($@"{{""Message"":""controller,Value=123""}}", response.Result.Replace(" ", "").Replace("\r", "").Replace("\n", ""));


            // ASSIGN-2
            action = await Node.LoadAsync<Operation>(action.Id, _cancel);
            action.MethodName = nameof(CustomODataController.AsyncMethod);
            await action.SaveAsync(_cancel);

            // ACT-2
            response = await ODataPostAsync("/OData.svc/Root/('Content')/Action1", "",
                @"{""s"": ""Value"", ""p"": 987}");

            // ASSERT-2
            Assert.AreEqual($@"{{""Message"":""controller,async,Value=987""}}", response.Result.Replace(" ", "").Replace("\r", "").Replace("\n", ""));
        });
    }
}