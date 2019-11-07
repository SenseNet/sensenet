using System;
using System.Threading;
using Microsoft.Extensions.Configuration;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Diagnostics;
using SenseNet.Search.Lucene29;
using SenseNet.Security.EFCSecurityStore;
using SenseNet.Services.InstallData;
using Task = System.Threading.Tasks.Task;

namespace SnConsoleInstaller
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            await new RepositoryBuilder()
                .SetConsole(Console.Out)
                .UseLogger(new SnFileSystemEventLogger())
                .UseTracer(new SnFileSystemTracer())
                .UseConfiguration(config)
                .UseDataProvider(new MsSqlDataProvider())
                .UseSecurityDataProvider(
                    new EFCSecurityDataProvider(connectionString: ConnectionStrings.ConnectionString))
                .UseLucene29LocalSearchEngine($"{Environment.CurrentDirectory}\\App_Data\\LocalIndex")
                .InstallSenseNetAsync(CancellationToken.None);
        }
    }
}
