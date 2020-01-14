using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Security;
using SenseNet.Diagnostics;

namespace SenseNet.Tests.SPA
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = CreateWebHostBuilder(args);
            var host = builder.Build();

            SnTrace.EnableAll();

            using (InMemoryExtensions.StartInMemoryRepository(repositoryBuilder =>
            {
                repositoryBuilder
                    .UseAccessProvider(new UserAccessProvider())
                    .UseLogger(new SnFileSystemEventLogger())
                    .UseTracer(new SnFileSystemTracer());
            }))
            {
                SnTrace.EnableAll();
                host.Run();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
