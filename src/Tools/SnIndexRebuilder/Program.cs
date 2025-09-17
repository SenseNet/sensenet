using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Search.Lucene29;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.Tools;
using Serilog;

namespace SnIndexRebuilder
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("SenseNet Index Rebuilder - Console Application");
            Console.WriteLine("===============================================");

            try
            {
                var services = CreateServices(args);
                Providers.Instance = new Providers(services);

                var connectionString = services.GetRequiredService<IConfiguration>().GetConnectionString("SnCrMsSql");
                Console.WriteLine($"Using connection string: {connectionString}");
                
                Providers.Instance.ResetBlobProviders(new ConnectionStringOptions { Repository = connectionString });

                Console.WriteLine("Starting SenseNet repository...");

                // CRITICAL: Disable outer search engine to prevent processing old indexing activities
                // This is the special working mode used by SnInitialDataGenerator and tests
                Indexing.IsOuterSearchEngineEnabled = false;
                
                var builder = new RepositoryBuilder(services)
                    .SetConsole(Console.Out)
                    .UseLucene29LocalSearchEngine(services.GetService<ILogger<Lucene29SearchEngine>>(),
                        Path.Combine(Environment.CurrentDirectory, "App_Data", "LocalIndex")) as RepositoryBuilder;

                // Disable automatic indexing activity processing during startup
                builder.StartIndexingEngine = false;

                // Start repository with indexing disabled
                using (var repositoryInstance = Repository.Start(builder as RepositoryBuilder))
                {
                    Console.WriteLine("Repository started successfully (indexing disabled)!");
                    
                    // Optional: Clear old indexing activities for a truly clean rebuild
                    Console.WriteLine("Clearing old indexing activities...");
                    try
                    {
                        if (Providers.Instance.DataProvider is SenseNet.ContentRepository.Storage.Data.RelationalDataProviderBase relationalProvider)
                        {
                            using (var ctx = relationalProvider.CreateDataContext(CancellationToken.None))
                            {
                                var deletedCount = await ctx.ExecuteNonQueryAsync("DELETE FROM IndexingActivities");
                                Console.WriteLine($"Cleared {deletedCount} old indexing activities.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Warning: Cannot clear indexing activities - DataProvider is not relational");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not clear indexing activities: {ex.Message}");
                        Console.WriteLine("Continuing with rebuild anyway...");
                    }
                    Console.WriteLine("Enabling indexing for clean rebuild...");
                    // Now enable indexing for the rebuild operation
                    Indexing.IsOuterSearchEngineEnabled = true;
                    Providers.Instance.SearchManager.IsOuterEngineEnabled = true;
                    
                    Console.WriteLine("Starting indexing engine...");
                    repositoryInstance.StartIndexingEngine();
                    
                    Console.WriteLine("Clearing existing index...");
                    var indexingEngine = Providers.Instance.SearchManager.SearchEngine.IndexingEngine;
                    await indexingEngine.ClearIndexAsync(CancellationToken.None);
                    
                    Console.WriteLine("Rebuilding index from scratch...");
                    
                    // Get the index populator to perform a clean rebuild
                    var populator = Providers.Instance.SearchManager.GetIndexPopulator();
                    
                    // Set up error handling
                    populator.IndexingError += (sender, eventArgs) =>
                    {
                        Console.WriteLine($"Indexing error for {eventArgs.Path}: {eventArgs.Exception?.Message}");
                    };
                    
                    var indexCount = 0;
                    populator.NodeIndexed += (sender, eventArgs) =>
                    {
                        if (++indexCount % 100 == 0)
                            Console.WriteLine($"Indexed {indexCount} nodes...");
                    };
                    
                    // Since we cleared the index, now rebuild it from the database
                    await populator.ClearAndPopulateAllAsync(CancellationToken.None, Console.Out);
                    
                    Console.WriteLine($"Index rebuilding completed! Total nodes indexed: {indexCount}");
                }

                Console.WriteLine("Operation completed successfully!");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return 1;
            }
        }

        static IServiceProvider CreateServices(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables();
            var configuration = configurationBuilder.Build();

            var connectionString = configuration.GetConnectionString("SnCrMsSql");

            var serviceCollection = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddSenseNet(configuration, (repositoryBuilder, provider) =>
                {
                    repositoryBuilder
                        .UseLogger(provider);
                    // Configure search engine separately on the main builder
                })
                .AddEFCSecurityDataProvider(options =>
                {
                    options.ConnectionString = connectionString;
                })
                .AddSenseNetMsSqlProviders()
                .AddSenseNetTracer<SnFileSystemTracer>();

            return serviceCollection.BuildServiceProvider();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(builder => builder
                    .AddJsonFile("appsettings.json", true, true)
                    .AddUserSecrets<Program>()
                    .AddEnvironmentVariables()
                )
                .ConfigureServices((hb, services) =>
                {
                    var connectionString = hb.Configuration.GetConnectionString("SnCrMsSql");
                    Console.WriteLine($"Using connection string: {connectionString}");

                    if (string.IsNullOrEmpty(connectionString))
                    {
                        Console.WriteLine("ERROR: Connection string is null or empty!");
                        return;
                    }

                    // Use the same pattern as MsSql integration tests
                    services
                        .AddSenseNet(hb.Configuration, (repositoryBuilder, provider) =>
                        {
                            repositoryBuilder
                                .UseLogger(provider);
                                // Skip search engine for now to test repository startup
                        })
                        .AddEFCSecurityDataProvider(options =>
                        {
                            options.ConnectionString = connectionString;
                        })
                        .AddSenseNetMsSqlProviders()
                        .AddSenseNetTracer<SnFileSystemTracer>();
                });
    }
}