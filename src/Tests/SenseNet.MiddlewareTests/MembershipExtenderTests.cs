using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.BackgroundOperations;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Diagnostics;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Services.Core;
using SenseNet.Services.Core.Diagnostics;
using SenseNet.Storage;
using SenseNet.Storage.Security;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.MiddlewareTests
{
    internal class TestMembershipExtenderAdmin : IMembershipExtender
    {
        public MembershipExtension GetExtension(IUser user)
        {
            return new MembershipExtension(new[] { Group.Administrators });
        }
    }
    internal class TestMembershipExtenderOperator : IMembershipExtender
    {
        public MembershipExtension GetExtension(IUser user)
        {
            return new MembershipExtension(new[] { Group.Operators });
        }
    }
    internal class TestMembershipExtenderFail : IMembershipExtender
    {
        public MembershipExtension GetExtension(IUser user)
        {
            throw new InvalidOperationException("Error");
        }
    }

    [TestClass]
    public class MembershipExtenderTests : MiddleWareTestBase
    {
        //UNDONE:TEST: Finalize this method and use it in all tests
        private IServiceCollection AddSenseNetForMembershipExtenderTest(IServiceCollection services,
            Action<RepositoryBuilder, IServiceProvider> buildRepository)
        {
            //UNDONE:    buildRepository is not called
            services/*.ConfigureSenseNet(configuration)*/
                .AddSenseNetILogger()
                .AddSenseNetMsSqlDataProvider()
                .AddSingleton<ISharedLockDataProvider, MsSqlSharedLockDataProvider>()
                .AddSingleton<IExclusiveLockDataProvider, MsSqlExclusiveLockDataProvider>()
                .AddSingleton<IAccessTokenDataProvider, MsSqlAccessTokenDataProvider>()
                .AddSingleton<IPackagingDataProvider, MsSqlPackagingDataProvider>()
                .AddSenseNetBlobStorage()
                .AddSenseNetSecurity(config =>
                {
                    config.SystemUserId = Identifiers.SystemUserId;
                    config.VisitorUserId = Identifiers.VisitorUserId;
                    config.EveryoneGroupId = Identifiers.EveryoneGroupId;
                    config.OwnerGroupId = Identifiers.OwnersGroupId;
                })
                .AddPlatformIndependentServices()
                .AddSenseNetTaskManager()
                .AddSenseNetDocumentPreviewProvider()
                .AddLatestComponentStore()
                .AddSenseNetCors()
                .AddSenseNetIdentityServerClients()
                .AddSenseNetDefaultClientManager()
                .AddSenseNetApiKeys()
                .AddSenseNetRegistration();

            // add sn components defined in the content repository layer
            services.AddRepositoryComponents();

            return services;
        }

        private void BuildBaseServices(IServiceCollection services)
        {
            AddSenseNetForMembershipExtenderTest(services, (repositoryBuilder, provider) =>
                {
                    repositoryBuilder
                        .BuildInMemoryRepository()
                        .UseLogger(provider)
                        .UseAccessProvider(new UserAccessProvider())
                        .UseInactiveAuditEventWriter();
                })
                .AddSenseNetInMemoryProviders();
        }

        [TestMethod, TestCategory("Services")]
        public async Task MW_MembershipExtender_KeepOriginalList_CSrv()
        {
            int[] extensionIds = null;

            await MiddlewareTestAsync(
                // Equivalent to the Startup.ConfigureServices(IServiceCollection) method
                services =>
                {
                    BuildBaseServices(services);
                    services.AddSenseNetMembershipExtender<TestMembershipExtenderAdmin>()
                        .AddSenseNetMembershipExtender<TestMembershipExtenderOperator>();
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

        [TestMethod]
        public async Task MW_MembershipExtender_MultipleRegistrations()
        {
            string[] extenderTypes = null;

            await MiddlewareTestAsync(
                // Equivalent to the Startup.ConfigureServices(IServiceCollection) method
                services =>
                {
                    BuildBaseServices(services);

                    // register 3 extenders
                    services.AddSenseNetMembershipExtender<TestMembershipExtenderAdmin>()
                        .AddSenseNetMembershipExtender<TestMembershipExtenderOperator>()
                        .AddSenseNetMembershipExtender<TestMembershipExtenderAdmin>();
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
                    
                    // SUB ACTION
                    app.UseSenseNetMembershipExtenders();

                    // Copy the test result to check the assertions
                    app.Use(async (context, next) =>
                    {
                        // collect registered extenders
                        extenderTypes = context.RequestServices.GetServices<IMembershipExtender>()
                            .Select(me => me.GetType().FullName).ToArray();

                        if (next != null)
                            await next();
                    });
                }, async client =>
                {
                    // MAIN ACTION
                    var response = await client.GetAsync("/");

                    // ASSERTIONS
                    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
                    Assert.IsNotNull(extenderTypes);
                    Assert.AreEqual(3, extenderTypes.Length);
                    Assert.AreEqual(typeof(TestMembershipExtenderAdmin).FullName, extenderTypes[0]);
                    Assert.AreEqual(typeof(TestMembershipExtenderOperator).FullName, extenderTypes[1]);
                    Assert.AreEqual(typeof(TestMembershipExtenderAdmin).FullName, extenderTypes[2]);
                });
        }

        [TestMethod]
        public async Task MW_MembershipExtender_FailedExtender()
        {
            int[] extensionIds = null;

            await MiddlewareTestAsync(
                // Equivalent to the Startup.ConfigureServices(IServiceCollection) method
                services =>
                {
                    BuildBaseServices(services);
                    //services.AddSingleton<SecurityHandler>();//UNDONE: AddSingleton<SecurityHandler>

                    // the first extender will throw an exception
                    services.AddSenseNetMembershipExtender<TestMembershipExtenderFail>()
                        .AddSenseNetMembershipExtender<TestMembershipExtenderAdmin>();
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
                    
                    // The list should still contain the group id from the second extender,
                    // despite the exception in the first.
                    Assert.IsTrue(extensionIds.Contains(Identifiers.AdministratorsGroupId));
                });
        }
    }
}
