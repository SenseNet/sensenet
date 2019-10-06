//UNDONE:ODATA:TEST: Implement 2 ODataSetFieldTests
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.Diagnostics;

namespace SenseNet.ODataTests
{
    [TestClass]
    public class ODataSetFieldTests : ODataTestBase
    {
        /*[TestMethod]*/
        /*public void OData_AllowedChildTypes_Post()
        {
            Test(() =>
            {
                var root = CreateTestRoot();

                try
                {
                    // ACTION: Create a new workspace with empty local list
                    var wsEntity1 = ODataPOST<ODataEntity>($"/OData.svc/content({root.Id})",
                        "metadata=no&$select=Id,Name,Path",
                        "(models=[{\"__ContentType\": \"Workspace\", \"Name\": \"ws1\", \"AllowedChildTypes\": []}])") as ODataEntity;

                    var ws1 = Content.Load(wsEntity1.Id);
                    var localAllowed1 = ((Workspace)ws1.ContentHandler).AllowedChildTypes.ToArray();

                    Assert.AreEqual(0, localAllowed1.Length);

                    // ACTION: Create a new workspace with a local list containing a few types
                    var wsEntity2 = ODataPOST<ODataEntity>($"/OData.svc/content({root.Id})",
                        "metadata=no&$select=Id,Name,Path",
                        "(models=[{\"__ContentType\": \"Workspace\", \"Name\": \"ws2\", \"AllowedChildTypes\": [\"Memo\", \"File\", \"Task\"]}])") as ODataEntity;

                    var ws2 = Content.Load(wsEntity2.Id);

                    // the local list should contain the types set above
                    var localAllowed2 = ((Workspace)ws2.ContentHandler).AllowedChildTypes.ToArray();

                    Assert.AreEqual("File, Memo, Task", string.Join(", ", localAllowed2.Select(ct => ct.Name).OrderBy(n => n)));
                }
                finally
                {
                    root.ForceDelete();
                }
            });
        }*/

        /*[TestMethod]*/
        /*public void OData_AllowedChildTypes_Patch()
        {
            Test(() =>
            {
                var root = CreateTestRoot();

                try
                {
                    var ws1 = Content.CreateNew("Workspace", root, "ws1");
                    ws1.Save();

                    var localAllowedBefore = ((Workspace)ws1.ContentHandler).AllowedChildTypes.ToArray();
                    var effectiveAllowedBefore = ((Workspace)ws1.ContentHandler).EffectiveAllowedChildTypes.ToArray();

                    // this is to make sure that the test runs in a correct environment
                    Assert.IsTrue(effectiveAllowedBefore.Length > 0);
                    Assert.AreEqual(0, localAllowedBefore.Length);

                    var ctFile = ContentType.GetByName("File");
                    var ctMemo = ContentType.GetByName("Memo");
                    var ctTask = ContentType.GetByName("Task");

                    // ACTION: Provide content type identifiers in 3 different ways: name, id and path.
                    var unused1 = ODataPATCH<ODataEntity>($"/OData.svc/content({ws1.Id})",
                        "metadata=no&$select=Id,Name,Path",
                        $"(models=[{{\"AllowedChildTypes\": [\"{ctFile.Name}\", {ctMemo.Id}, \"{ctTask.Path}\"]}}])");

                    ws1 = Content.Load(ws1.Id);

                    // the local list should contain the types set above
                    var localAllowedAfter = ((Workspace)ws1.ContentHandler).AllowedChildTypes.ToArray();
                    var effectiveAllowedAfter = ((Workspace)ws1.ContentHandler).EffectiveAllowedChildTypes.ToArray();

                    // SystemFolder is added on-the-fly for admins
                    Assert.AreEqual("File, Memo, Task", string.Join(", ", localAllowedAfter.Select(ct => ct.Name)));
                    Assert.AreEqual("File, Memo, SystemFolder, Task",
                        string.Join(", ", effectiveAllowedAfter.Select(ct => ct.Name).OrderBy(n => n)));

                    var ctdAllowedList = ws1.ContentType.AllowedChildTypes.ToArray();

                    // ACTION: set the same content types as in the CTD (expected result: clear local list)
                    var unused2 = ODataPATCH<ODataEntity>($"/OData.svc/content({ws1.Id})",
                        "metadata=no&$select=Id,Name,Path",
                        $"(models=[{{\"AllowedChildTypes\": [{string.Join(", ", ctdAllowedList.Select(ct => $"\"{ct.Name}\""))}]}}])");

                    ws1 = Content.Load(ws1.Id);

                    localAllowedAfter = ((Workspace)ws1.ContentHandler).AllowedChildTypes.ToArray();
                    effectiveAllowedAfter = ((Workspace)ws1.ContentHandler).EffectiveAllowedChildTypes.ToArray();

                    var expectedEffectiveNames = ctdAllowedList.Select(ct => ct.Name).Union(new[] { "SystemFolder" })
                        .OrderBy(ct => ct);

                    Assert.AreEqual(0, localAllowedAfter.Length);
                    Assert.AreEqual(string.Join(", ", expectedEffectiveNames),
                        string.Join(", ", effectiveAllowedAfter.Select(ct => ct.Name).OrderBy(n => n)));
                }
                finally
                {
                    root.ForceDelete();
                }
            });
        }*/
    }
}
