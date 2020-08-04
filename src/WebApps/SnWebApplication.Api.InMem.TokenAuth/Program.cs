using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Security;
using SenseNet.Extensions.DependencyInjection;

namespace SnWebApplication.Api.InMem.TokenAuth
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = CreateHostBuilder(args);
            var host = builder.Build();

            using (InMemoryExtensions.StartInMemoryRepository(repositoryBuilder =>
            {
                repositoryBuilder.UseAccessProvider(new UserAccessProvider());
            }))
            {
                // put repository initialization here (e.g. import)

                host.Run();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
