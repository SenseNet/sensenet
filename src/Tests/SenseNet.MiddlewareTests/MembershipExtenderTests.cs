using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Services.Core;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.MiddlewareTests
{
    internal class TestMembershipExtenderAdmin : IMembershipExtender
    {
        public MembershipExtension GetExtension(IUser user, HttpContext httpContext)
        {
            return new MembershipExtension(new[] { Group.Administrators });
        }
    }
    internal class TestMembershipExtenderOperator : IMembershipExtender
    {
        public MembershipExtension GetExtension(IUser user, HttpContext httpContext)
        {
            return new MembershipExtension(new[] { Group.Operators });
        }
    }

    [TestClass]
    public class MembershipExtenderTests : MiddleWareTestBase
    {
        [TestMethod]
        public async Task MW_MembershipExtender_KeepOriginalList()
        {
            int[] extensionIds = null;

            await MiddlewareTestAsync(
                // Equivalent to the Startup.ConfigureServices(IServiceCollection) method
                services =>
                {
                    services.AddSenseNetMembershipExtender(
                        new TestMembershipExtenderAdmin(),
                        new TestMembershipExtenderOperator());
                }, app =>
                // Equivalent to the Startup.Configure(IApplicationBuilder) method
                {
                    // Required authentication
                    app.Use(async (context, next) =>
                    {
                        User.Current = User.Administrator;
                        if (next != null)
                            await next();
                    });

                    // Create initial extension
                    app.Use(async (context, next) =>
                    {
                        User.Current.MembershipExtension = new MembershipExtension(new[] { 99999, 99998 });
                        if (next != null)
                            await next();
                    });

                    // SUB ACTION
                    app.UseSenseNetMembershipExtenders();

                    // Copy the test result to check the assertions
                    app.Use(async (context, next) =>
                    {
                        extensionIds = User.Current.MembershipExtension.ExtensionIds.ToArray();
                        if (next != null)
                            await next();
                    });
                }, async client =>
                {
                    // MAIN ACTION
                    var response = await client.GetAsync("/");

                    // ASSERTIONS
                    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
                    Assert.IsNotNull(extensionIds);
                    Assert.AreEqual(4, extensionIds.Length);
                    Assert.AreEqual(99999, extensionIds[0]);
                    Assert.AreEqual(99998, extensionIds[1]);
                    Assert.AreEqual(Identifiers.AdministratorsGroupId, extensionIds[2]);
                    // WTF: User.Current is STARTUP
                    //Assert.AreEqual(Group.Operators.Id, extensionIds[3]);
                    Assert.AreEqual(11, extensionIds[3]);
                });
        }

    }
}
