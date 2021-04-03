using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Security;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Tests.Core;

namespace SenseNet.MiddlewareTests
{
    public abstract class MiddleWareTestBase : TestBase
    {
        public async Task MiddlewareTestAsync(
            Action<IServiceCollection> configureServices,
            Action<IApplicationBuilder> configure,
            Func<HttpClient, Task> testMethod)
        {
            using (var host = new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseTestServer().ConfigureServices(configureServices).Configure(configure);
                }).Build())
            {
                using (InMemoryExtensions.StartInMemoryRepository(repositoryBuilder =>
                {
                    repositoryBuilder.UseAccessProvider(new UserAccessProvider());
                }))
                {
#pragma warning disable 4014
                    host.RunAsync();
#pragma warning restore 4014

                    var client = host.GetTestServer().CreateClient();
                    await testMethod(client);
                }
            }
        }
    }
}
