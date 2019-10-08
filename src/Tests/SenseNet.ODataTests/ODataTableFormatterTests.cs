using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.OData;
using Task = System.Threading.Tasks.Task;
// ReSharper disable StringLiteralTypo

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataTableFormatterTests : ODataTestBase
    {
        [TestMethod]
        public async Task OD_GET_ServiceDocument_Table()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataGetAsync("/OData.svc", "?$format=table")
                    .ConfigureAwait(false);

                // ASSERT
                var raw = response.Result.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                Assert.IsTrue(raw.StartsWith("<!DOCTYPEhtml>"));
                Assert.IsTrue(raw.Contains("<table><tr><td>Servicedocument</td></tr><tr><td>Root</td></tr></table>"));
                Assert.IsTrue(raw.EndsWith("</html>"));
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_Entity_Table()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataGetAsync("/OData.svc/Root('IMS')", "?$format=table")
                    .ConfigureAwait(false);

                // ASSERT
                var raw = response.Result.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var content = Content.Load("/Root/IMS");
                Assert.IsTrue(raw.StartsWith("<!DOCTYPEhtml>"));
                Assert.IsTrue(raw.Contains("<tr><td>Name</td><td>Value</td></tr>"));
                Assert.IsTrue(raw.Contains($"<tr><td>Id</td><td>{content.Id}</td></tr>"));
                Assert.IsTrue(raw.Contains($"<tr><td>Name</td><td>{content.Name}</td></tr>"));
                Assert.IsTrue(raw.Contains($"<tr><td>Path</td><td>{content.Path}</td></tr>"));
                Assert.IsTrue(raw.EndsWith("</html>"));
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_GET_ChildrenCollection_Table()
        {
            await ODataTestAsync(async () =>
            {
                // ACTION
                var response = await ODataGetAsync(
                        "/OData.svc/Root/IMS/BuiltIn/Portal", "?metadata=no&$select=Id,Name&$format=table")
                    .ConfigureAwait(false);

                // ASSERT
                var raw = response.Result.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                Assert.IsTrue(raw.StartsWith("<!DOCTYPEhtml>"));
                Assert.IsTrue(raw.Contains("<td>Nr.</td><td>Id</td><td>Name</td></tr>"));
                Assert.IsTrue(raw.EndsWith("</html>"));
                var nodes = Node.Load<Folder>("/Root/IMS/BuiltIn/Portal").Children.ToArray();
                foreach (var node in nodes)
                {
                    Assert.IsTrue(raw.Contains($"</td><td>{node.Id}</td><td>{node.Name}</td></tr>"));
                }
            }).ConfigureAwait(false);
        }
        [TestMethod]
        public async Task OD_OP_InvokeAction_Errors()
        {
            await ODataTestAsync(async () =>
            {
                using (new ODataOperationTests.ActionResolverSwindler(new ODataOperationTests.TestActionResolver()))
                {
                    // ACTION
                    var response = await ODataPostAsync(
                            "/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/ODataError",
                            "?$format=table",
                            $@"{{""errorType"":""NodeAlreadyExistsException""}}")
                        .ConfigureAwait(false);

                    // ASSERT
                    var raw = response.Result.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                    Assert.IsTrue(raw.StartsWith("<!DOCTYPEhtml>"));
                    Assert.IsTrue(raw.Contains("<tr><td>Code</td><td>ContentAlreadyExists</td></tr>"));
                    Assert.IsTrue(raw.Contains("<tr><td>Exceptiontype</td><td>NodeAlreadyExistsException</td></tr>"));
                    Assert.IsTrue(raw.EndsWith("</html>"));
                }
            }).ConfigureAwait(false);
        }

    }
}
