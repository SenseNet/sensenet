using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Tests.Core;
using System;
using System.Linq;
using System.Threading;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security.Clients;
using SenseNet.ContentRepository.Storage;
using SenseNet.Services.Core.Operations;
using Task = System.Threading.Tasks.Task;
using Microsoft.AspNetCore.Http;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Services.Core.Tests
{
    [TestClass]
    public class ClientStoreTests : TestBase
    {
        [TestMethod, TestCategory("ClientStore")]
        public Task ClientStore_GetClients_Admin()
        {
            return ClientStoreTest(async (context, _) =>
            {
                dynamic result = await ClientStoreOperations.GetClients(null, context);
                var clients = (Client[])result.clients;

                // all clients are accessible
                Assert.AreEqual(5, clients.Length);
                Assert.AreEqual(1, clients.Count(c => 
                    c.UserName == "builtin\\admin" && c.Type == ClientType.InternalClient));
            });
        }
        [TestMethod, TestCategory("ClientStore")]
        public Task ClientStore_GetClients_PublicAdmin()
        {
            return ClientStoreTestForPublicAdmin(async (context, _) =>
            {
                dynamic result = await ClientStoreOperations.GetClients(null, context);
                var clients = (Client[])result.clients;

                // only 3 are accessible
                Assert.AreEqual(3, clients.Length);
                Assert.AreEqual(0, clients.Count(c =>
                    c.UserName == "builtin\\admin" && c.Type == ClientType.InternalClient));
                Assert.AreEqual(1, clients.Count(c =>
                    c.UserName == "public\\user1" && c.Type == ClientType.ExternalClient));
                Assert.AreEqual(1, clients.Count(c =>
                    c.UserName == "builtin\\publicadmin" && c.Type == ClientType.ExternalClient));
                Assert.AreEqual(0, clients.Count(c =>
                    c.UserName == "domain2\\user2" && c.Type == ClientType.ExternalClient));
            });
        }
        [TestMethod, TestCategory("ClientStore")]
        public Task ClientStore_GetClients_RegularUser()
        {
            // This test does not take into account that currently clients cannot
            // invoke these methods without being at least public admins.

            return ClientStoreTestForRegularUser(async (context, _) =>
            {
                dynamic result = await ClientStoreOperations.GetClients(null, context);
                var clients = (Client[])result.clients;

                // only 3 are accessible
                Assert.AreEqual(2, clients.Length);
                Assert.AreEqual(0, clients.Count(c =>
                    c.UserName == "builtin\\admin" && c.Type == ClientType.InternalClient));
                Assert.AreEqual(0, clients.Count(c =>
                    c.UserName == "public\\user1" && c.Type == ClientType.ExternalClient));
                Assert.AreEqual(0, clients.Count(c =>
                    c.UserName == "builtin\\publicadmin" && c.Type == ClientType.ExternalClient));
                Assert.AreEqual(1, clients.Count(c =>
                    c.UserName == "domain2\\user2" && c.Type == ClientType.ExternalClient));
            });
        }

        [TestMethod, TestCategory("ClientStore")]
        public Task ClientStore_CreateClient_Admin()
        {
            return ClientStoreTest(async (context, _) =>
            {
                var client = await ClientStoreOperations.CreateClient(null, context, "admin1",
                    ClientType.InternalClient.ToString());

                // check default user
                Assert.AreEqual("builtin\\admin", client.UserName);

                client = await ClientStoreOperations.CreateClient(null, context, "admin2",
                    ClientType.ExternalSpa.ToString(), "not required");

                // check that username is ignored for this type of client
                Assert.AreEqual(null, client.UserName);
            });
        }
        [TestMethod, TestCategory("ClientStore")]
        public Task ClientStore_CreateClient_PublicAdmin()
        {
            return ClientStoreTestForPublicAdmin(async (context, _) =>
            {
                var client = await ClientStoreOperations.CreateClient(null, context, "admin1",
                    ClientType.ExternalClient.ToString());

                // check default user
                Assert.AreEqual("builtin\\publicadmin", client.UserName);

                client = await ClientStoreOperations.CreateClient(null, context, "admin2",
                    ClientType.ExternalClient.ToString(), "public\\user1");

                // check custom user
                Assert.AreEqual("public\\user1", client.UserName);

                var thrown = false;

                try
                {
                    // internal client is not allowed
                    await ClientStoreOperations.CreateClient(null, context, "admin1",
                        ClientType.InternalClient.ToString());
                }
                catch (InvalidOperationException)
                {
                    thrown = true;
                }

                Assert.IsTrue(thrown);

                thrown = false;

                try
                {
                    // not accessible user
                    await ClientStoreOperations.CreateClient(null, context, "admin1",
                        ClientType.ExternalClient.ToString(), "domain2\\user2");
                }
                catch (InvalidOperationException)
                {
                    thrown = true;
                }

                Assert.IsTrue(thrown);
            });
        }
        [TestMethod, TestCategory("ClientStore")]
        public Task ClientStore_CreateClient_RegularUser()
        {
            return ClientStoreTestForRegularUser(async (context, _) =>
            {
                var client = await ClientStoreOperations.CreateClient(null, context, "admin1",
                        ClientType.ExternalClient.ToString());

                // check default user: the current non-admin user, who is not a member of the public admin group
                Assert.AreEqual("domain2\\user2", client.UserName);

                client = await ClientStoreOperations.CreateClient(null, context, "admin2",
                    ClientType.ExternalClient.ToString(), "domain2\\user2");

                // check custom user
                Assert.AreEqual("domain2\\user2", client.UserName);

                var thrown = false;

                try
                {
                    // internal client is not allowed
                    await ClientStoreOperations.CreateClient(null, context, "admin1",
                        ClientType.InternalClient.ToString());
                }
                catch (InvalidOperationException)
                {
                    thrown = true;
                }

                Assert.IsTrue(thrown);

                thrown = false;

                try
                {
                    // not accessible user
                    await ClientStoreOperations.CreateClient(null, context, "admin1",
                        ClientType.ExternalClient.ToString(), "public\\user1");
                }
                catch (InvalidOperationException)
                {
                    thrown = true;
                }

                Assert.IsTrue(thrown);
            });
        }

        [TestMethod, TestCategory("ClientStore")]
        public Task ClientStore_CreateSecret_Admin()
        {
            return ClientStoreTest(async (context, _) =>
            {
                var secret = await ClientStoreOperations.CreateSecret(null, context, "c1");

                Assert.IsNotNull(secret.Value);

                var thrown = false;

                try
                {
                    // SPA client: no secret is allowed
                    await ClientStoreOperations.CreateSecret(null, context, "c2");
                }
                catch (InvalidOperationException)
                {
                    thrown = true;
                }

                Assert.IsTrue(thrown);
            });
        }
        [TestMethod, TestCategory("ClientStore")]
        public Task ClientStore_CreateSecret_PublicAdmin()
        {
            return ClientStoreTestForPublicAdmin(async (context, _) =>
            {
                // client for the public admin user: OK
                var secret = await ClientStoreOperations.CreateSecret(null, context, "c3");
                Assert.IsNotNull(secret.Value);

                // client for another public admin user: OK
                secret = await ClientStoreOperations.CreateSecret(null, context, "c4");
                Assert.IsNotNull(secret.Value);

                var thrown = false;

                try
                {
                    // client for a not accessible user: NOT OK
                    await ClientStoreOperations.CreateSecret(null, context, "c5");
                }
                catch (SenseNetSecurityException)
                {
                    thrown = true;
                }

                Assert.IsTrue(thrown);
            });
        }
        [TestMethod, TestCategory("ClientStore")]
        public Task ClientStore_CreateSecret_RegularUser()
        {
            return ClientStoreTestForRegularUser(async (context, _) =>
            {
                // client for themselves: OK
                var secret = await ClientStoreOperations.CreateSecret(null, context, "c5");
                Assert.IsNotNull(secret.Value);

                var thrown = false;

                try
                {
                    // client for a not accessible user: NOT OK
                    await ClientStoreOperations.CreateSecret(null, context, "c4");
                }
                catch (SenseNetSecurityException)
                {
                    thrown = true;
                }

                Assert.IsTrue(thrown);
            });
        }

        [TestMethod, TestCategory("ClientStore")]
        public Task ClientStore_DeleteClient_Admin()
        {
            return ClientStoreTest(async (context, clientStore) =>
            {
                var client = await clientStore.GetClientAsync("x", "c1");
                Assert.IsNotNull(client);

                await ClientStoreOperations.DeleteClient(null, context, "c1");

                client = await clientStore.GetClientAsync("x", "c1");
                Assert.IsNull(client);

                var thrown = false;

                try
                {
                    await ClientStoreOperations.DeleteClient(null, context, "nonexisting");
                }
                catch (InvalidOperationException)
                {
                    thrown = true;
                }

                Assert.IsTrue(thrown);
            });
        }
        [TestMethod, TestCategory("ClientStore")]
        public Task ClientStore_DeleteClient_PublicAdmin()
        {
            return ClientStoreTestForPublicAdmin(async (context, clientStore) =>
            {
                var client = await clientStore.GetClientAsync("x", "c3");
                Assert.IsNotNull(client);
                client = await clientStore.GetClientAsync("x", "c4");
                Assert.IsNotNull(client);

                await ClientStoreOperations.DeleteClient(null, context, "c3");
                await ClientStoreOperations.DeleteClient(null, context, "c4");

                client = await clientStore.GetClientAsync("x", "c3");
                Assert.IsNull(client);

                client = await clientStore.GetClientAsync("x", "c4");
                Assert.IsNull(client);

                var thrown = false;

                try
                {
                    // delete admin client: NOT OK
                    await ClientStoreOperations.DeleteClient(null, context, "c1");
                }
                catch (SenseNetSecurityException)
                {
                    thrown = true;
                }

                Assert.IsTrue(thrown);

                thrown = false;

                try
                {
                    // delete other users' client: NOT OK
                    await ClientStoreOperations.DeleteClient(null, context, "c5");
                }
                catch (SenseNetSecurityException)
                {
                    thrown = true;
                }

                Assert.IsTrue(thrown);
            });
        }

        [TestMethod, TestCategory("ClientStore")]
        public Task ClientStore_RegenerateSecret_PublicAdmin()
        {
            return ClientStoreTestForPublicAdmin(async (context, _) =>
            {
                var secret1 = await ClientStoreOperations.CreateSecret(null, context, "c3");
                var secret2 = await ClientStoreOperations.RegenerateSecretForRepository(null, context, "c3", secret1.Id);

                Assert.AreEqual(secret1.Id, secret2.Id);
                Assert.AreNotEqual(secret1.Value, secret2.Value);

                var thrown = false;

                try
                {
                    // try to access an admin client secret
                    await ClientStoreOperations.RegenerateSecretForRepository(null, context, "c1", "?");
                }
                catch (SenseNetSecurityException)
                {
                    thrown = true;
                }

                Assert.IsTrue(thrown);
            });
        }

        // Helper methods ===================================================================================

        protected Task ClientStoreTest(Func<HttpContext, ClientStore, Task> callback)
        {
            ClientStore clientStore = null;
            IServiceProvider services = null;

            return Test2(sc =>
                {
                    sc.Configure<ClientStoreOptions>(options =>
                    {
                        options.Authority = "x";
                        options.RepositoryUrl = "x";
                    });
                },
                repoBuilder =>
                {
                    clientStore = repoBuilder.Services.GetRequiredService<ClientStore>();
                    services = repoBuilder.Services;
                }, async () =>
                {
                    var context = new DefaultHttpContext { RequestServices = services };

                    await CreateUsersAndClients(clientStore);
                    await callback(context, clientStore);
                });
        }
        protected Task ClientStoreTestForPublicAdmin(Func<HttpContext, ClientStore, Task> callback)
        {
            return ClientStoreTest(async (context, clientStore) =>
            {
                var testUser = await Node.LoadAsync<User>(Identifiers.PublicAdminPath, CancellationToken.None);
                var originalUser = AccessProvider.Current.GetCurrentUser();

                AccessProvider.Current.SetCurrentUser(testUser);

                try
                {
                    await callback(context, clientStore);
                }
                finally
                {
                    AccessProvider.Current.SetCurrentUser(originalUser);
                }
            });
        }
        protected Task ClientStoreTestForRegularUser(Func<HttpContext, ClientStore, Task> callback)
        {
            return ClientStoreTest(async (context, clientStore) =>
            {
                var testUser = await Node.LoadAsync<User>("/Root/IMS/domain2/user2", CancellationToken.None);
                var originalUser = AccessProvider.Current.GetCurrentUser();

                AccessProvider.Current.SetCurrentUser(testUser);

                try
                {
                    await callback(context, clientStore);
                }
                finally
                {
                    AccessProvider.Current.SetCurrentUser(originalUser);
                }
            });
        }

        private static async Task CreateUsersAndClients(ClientStore clientStore)
        {
            // public\user1     --> member of PublicAdmins
            // domain2\user2

            var publicDomain = Node.Load<Domain>("/Root/IMS/Public");
            var user1Content = Content.CreateNew("User", publicDomain, "user1");
            user1Content["Enabled"] = true;

            await user1Content.SaveAsync(CancellationToken.None);
            var user1 = user1Content.ContentHandler as User;

            var publicAdminGroup =
                await Node.LoadAsync<Group>(ApplicationModel.N.R.PublicAdministrators, CancellationToken.None);

            publicAdminGroup.AddMember(user1);

            var domain2 = Content.CreateNew("Domain", Node.LoadNode("/Root/IMS"), "domain2");
            await domain2.SaveAsync(CancellationToken.None);

            var user2 = Content.CreateNew("User", domain2.ContentHandler, "user2");
            await user2.SaveAsync(CancellationToken.None);

            await clientStore.SaveClientAsync(new Client
            {
                Name = "c1",
                ClientId = "c1",
                Authority = "x",
                UserName = "builtin\\admin",
                Type = ClientType.InternalClient,
                Repository = "x"
            });
            await clientStore.SaveClientAsync(new Client
            {
                Name = "c2",
                ClientId = "c2",
                Authority = "x",
                Type = ClientType.ExternalSpa,
                Repository = "x"
            });
            await clientStore.SaveClientAsync(new Client
            {
                Name = "c3",
                ClientId = "c3",
                Authority = "x",
                UserName = "builtin\\publicadmin",
                Type = ClientType.ExternalClient,
                Repository = "x"
            });
            await clientStore.SaveClientAsync(new Client
            {
                Name = "c4",
                ClientId = "c4",
                Authority = "x",
                UserName = "public\\user1",
                Type = ClientType.ExternalClient,
                Repository = "x"
            });
            await clientStore.SaveClientAsync(new Client
            {
                Name = "c5",
                ClientId = "c5",
                Authority = "x",
                UserName = "domain2\\user2",
                Type = ClientType.ExternalClient,
                Repository = "x"
            });
        }
    }
}
