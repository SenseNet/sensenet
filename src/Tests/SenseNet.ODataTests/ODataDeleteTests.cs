using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.ODataTests
{
    class ODataDeleteTests
    {
        /*[TestMethod]
        public void OData_Deleting()
        {
            Test(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                CreateTestSite();

                var name = Guid.NewGuid().ToString();
                var content = Content.CreateNew("Car", testRoot, name);
                content.Save();
                var path = string.Concat("/OData.svc/", testRoot.Path, "('", name, "')");

                var output = new StringWriter();
                var pc = CreatePortalContext(path, "", output);
                var handler = new ODataHandler();
                handler.ProcessRequest(pc.OwnerHttpContext, "DELETE", null);

                var repoPath = string.Concat(testRoot.Path, "/", name);
                Assert.IsTrue(Node.Exists(repoPath) == false);
            });
        }*/
        /*[TestMethod]
        public void OData_DeletingBy()
        {
            Test(() =>
            {
                InstallCarContentType();
                var testRoot = CreateTestRoot("ODataTestRoot");
                CreateTestSite();

                var name = Guid.NewGuid().ToString();
                var content = Content.CreateNew("Car", testRoot, name);
                content.Save();
                var path = string.Concat("/OData.svc/content(" + content.Id + ")");

                var output = new StringWriter();
                var pc = CreatePortalContext(path, "", output);
                var handler = new ODataHandler();
                handler.ProcessRequest(pc.OwnerHttpContext, "DELETE", null);

                var repoPath = string.Concat(testRoot.Path, "/", name);
                Assert.IsTrue(Node.Exists(repoPath) == false);

            });
        }*/
    }
}
