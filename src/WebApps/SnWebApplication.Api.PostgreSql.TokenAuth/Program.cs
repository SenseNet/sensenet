using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace SnWebApplication.Api.PostgreSql.TokenAuth
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Enable legacy timestamp behavior for Npgsql 6+
            // This allows DateTime with Kind=UTC to be written to 'timestamp without time zone' columns.
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                        .ConfigureLogging(loggingConfiguration =>
                            loggingConfiguration.ClearProviders());
                })
                .UseSerilog((hostingContext, loggerConfiguration) =>
                    loggerConfiguration.ReadFrom
                        .Configuration(hostingContext.Configuration));
    }
}
