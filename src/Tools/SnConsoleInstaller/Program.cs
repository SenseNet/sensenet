﻿using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Security;
using SenseNet.Services.Core.Install;
using Serilog;
using Installer = SenseNet.Packaging.Installer;

namespace SnConsoleInstaller
{
    class Program
    {
        static void Main(string[] args)
        {
            using var host = CreateHostBuilder(args).Build();
            var logger = host.Services.GetService<ILogger<Installer>>();

            var builder = new RepositoryBuilder(host.Services)
                .SetConsole(Console.Out)
                .UseLogger(new SnFileSystemEventLogger())
                .UseTracer(new SnFileSystemTracer())
                .UseLucene29LocalSearchEngine(Path.Combine(Environment.CurrentDirectory, "App_Data", "LocalIndex")) as RepositoryBuilder;

            new Installer(builder, null, logger)
                .InstallSenseNet();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(builder => builder
                    .AddJsonFile("appsettings.json", true, true)
                    .AddUserSecrets<Program>()
                )
                .ConfigureServices((hb, services) => services
                    .SetSenseNetConfiguration(hb.Configuration)
                    .AddLogging(logging =>
                    {
                        logging.AddSerilog(new LoggerConfiguration()
                            .ReadFrom.Configuration(hb.Configuration)
                            .CreateLogger());
                    })
                    .AddSenseNetDataProvider()
                    .AddEFCSecurityDataProvider(options =>
                    {
                        options.ConnectionString = ConnectionStrings.ConnectionString;
                    }));
    }
}
