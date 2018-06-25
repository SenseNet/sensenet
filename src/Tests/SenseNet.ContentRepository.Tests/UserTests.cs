using System.IO;
using System.Linq;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Portal.Virtualization;
using SenseNet.Security;
using SenseNet.Tests;
using SenseNet.Tests.Implementations;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class UserTests : TestBase
    {
        private class TestMembershipExtender : MembershipExtenderBase
        {
            public override MembershipExtension GetExtension(IUser user)
            {
                SnTrace.Test.Write("TestMembershipExtender: getting PortalContext");
                // this line can cause infinite recursion.
                var requestedNode = PortalContext.Current.ContextNode;

                SnTrace.Test.Write("TestMembershipExtender: getting DynamicGroups on requested user.");
                var groupIds = ((User) requestedNode).GetDynamicGroups(2);
                SnTrace.Test.Write("TestMembershipExtender: DynamicGroups: {0}", groupIds);

                var testGroup = (IGroup)Node.LoadNode("/Root/IMS/BuiltIn/Portal/TestGroup");
                return new MembershipExtension(new []{ testGroup });
            }
        }

        [TestMethod]
        public void User_sensenet393_BugReproduction()
        {
            var tracer = new SnDebugViewTracer();
            SnTrace.SnTracers.Add(tracer);
            var securityEnabled = SnTrace.Security.Enabled;
            SnTrace.Security.Enabled = true;
            try
            {
                Test(true, () =>
                {
                    Group group;
                    User user;
                    using (new SystemAccount())
                    {
                        var root = Repository.Root;
                        var ed = SecurityHandler.CreateAclEditor();
                        ed.Set(Repository.Root.Id, User.Administrator.Id, false, PermissionBitMask.AllAllowed);
                        ed.Set(Repository.Root.Id, Group.Administrators.Id, false, PermissionBitMask.AllAllowed);
                        ed.Apply();

                        var portal = Node.LoadNode("/Root/IMS/BuiltIn/Portal");

                        group = new Group(portal)
                        {
                            Name = "TestGroup"
                        };
                        group.Save();

                        user = new User(portal)
                        {
                            Name = "TestUser",
                            Enabled = true,
                            Email = "mail@example.com"
                        };
                        user.Save();

                        Group.Administrators.AddMember(user);
                        User.Current = user;
                    }

                    Providers.Instance.MembershipExtender = new TestMembershipExtender();

                    var simulatedOutput = new StringWriter();
                    var simulatedWorkerRequest = new SimulatedHttpRequest(@"\", @"C:\Inetpub\wwwroot", user.Path, "",
                        simulatedOutput, "localhost_forms");
                    var simulatedHttpContext = new HttpContext(simulatedWorkerRequest);
                    var portalContext = PortalContext.Create(simulatedHttpContext);
                    HttpContext.Current = simulatedHttpContext;

                    // This line caused StackOverflowException
                    var additionalGroups = user.GetDynamicGroups(2);

                    // The bug is fixed if the code can run up to this point
                    // but we test the full feature.
                    Assert.AreEqual(group.Id, additionalGroups.First());
                });
            }
            finally
            {
                SnTrace.Security.Enabled = securityEnabled;
                SnTrace.SnTracers.Remove(tracer);
            }
        }
    }
}
