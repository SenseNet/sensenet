using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ODataTests;

[TestClass]
public class OperationFrameworkTests : ODataTestBase
{
    readonly CancellationToken _cancel = new CancellationToken();

    [TestMethod]
    public async Task OperationFramework_()
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

            Assert.AreEqual(@"{""message"":""Parameters:p1=Value1,p2=42""}", response.Result.Replace(" ", "").Replace("\r", "").Replace("\n", ""));
        });
    }
}