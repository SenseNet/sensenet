using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SenseNet.ContentRepository;

namespace SnWebApplication.Api.Sql.TokenAuth
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = CreateHostBuilder(args);
            var host = builder.Build();
            var config = host.Services.GetService(typeof(IConfiguration)) as IConfiguration;
            var environment = host.Services.GetService(typeof(IHostEnvironment)) as IHostEnvironment;

            var repositoryBuilder = Startup.GetRepositoryBuilder(config, environment);

            using (Repository.Start(repositoryBuilder))
            {
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
