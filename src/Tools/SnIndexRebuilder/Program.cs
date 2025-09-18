using System;
using System.Diagnostics;
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
    public class IndexingProgressTracker
    {
        private readonly int _totalNodes;
        private readonly Stopwatch _stopwatch;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private int _indexedCount;
        private DateTime _lastUpdate;
        private double _maxTimePerNodeMs;

        public IndexingProgressTracker(int totalNodes, Microsoft.Extensions.Logging.ILogger logger)
        {
            _totalNodes = totalNodes;
            _logger = logger;
            _stopwatch = Stopwatch.StartNew();
            _lastUpdate = DateTime.Now;
            _maxTimePerNodeMs = 0.0;
        }

        public void UpdateProgress(int indexedCount)
        {
            _indexedCount = indexedCount;
            var now = DateTime.Now;
            
            // Only update every 100 nodes or every 5 seconds
            if (indexedCount % 100 == 0 || (now - _lastUpdate).TotalSeconds >= 5)
            {
                var percentage = (_indexedCount * 100.0) / _totalNodes;
                var elapsed = _stopwatch.Elapsed;
                
                // Calculate average and max time per node
                var avgTimePerNodeMs = elapsed.TotalMilliseconds / _indexedCount;
                
                // Update max time per node (this captures the worst case we've seen so far)
                if (avgTimePerNodeMs > _maxTimePerNodeMs)
                {
                    _maxTimePerNodeMs = avgTimePerNodeMs;
                }
                
                // Calculate ETAs
                var remainingNodes = _totalNodes - _indexedCount;
                var avgEta = TimeSpan.FromMilliseconds(avgTimePerNodeMs * remainingNodes);
                var worstEta = TimeSpan.FromMilliseconds(_maxTimePerNodeMs * remainingNodes);

                var progressMessage = $"Indexed {_indexedCount:N0} / {_totalNodes:N0} nodes ({percentage:F1}%) - " +
                                    $"Elapsed: {elapsed:hh\\:mm\\:ss} - " +
                                    $"ETA: {avgEta:hh\\:mm\\:ss} (avg) / {worstEta:hh\\:mm\\:ss} (worst)";
                
                Console.WriteLine(progressMessage);
                _logger.LogInformation(progressMessage);
                
                _lastUpdate = now;
            }
        }

        public void Complete()
        {
            _stopwatch.Stop();
            var finalMessage = $"Index rebuilding completed! Indexed {_indexedCount:N0} nodes in {_stopwatch.Elapsed:hh\\:mm\\:ss}";
            Console.WriteLine(finalMessage);
            _logger.LogInformation(finalMessage);
        }
    }

    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("SenseNet Index Rebuilder - Console Application");
            Console.WriteLine("===============================================");
            
            // Configure Serilog
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true)
                    .Build())
                .CreateLogger();
            
            Log.Logger = logger;
            var msLogger = new LoggerFactory().AddSerilog(logger).CreateLogger<Program>();
            
            // Parse command line arguments
            var clearActivities = args.Length > 0 && args[0].Equals("--clear-activities", StringComparison.OrdinalIgnoreCase);
            var showHelp = args.Length > 0 && (args[0].Equals("--help", StringComparison.OrdinalIgnoreCase) || args[0].Equals("-h", StringComparison.OrdinalIgnoreCase));
            
            if (showHelp)
            {
                ShowHelp();
                return 0;
            }
            
            var mode = clearActivities ? "Clear Activities + Rebuild" : "Clean Rebuild Only";
            Console.WriteLine($"Mode: {mode}");
            msLogger.LogInformation("Starting SenseNet Index Rebuilder in mode: {Mode}", mode);
            Console.WriteLine();

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
                    msLogger.LogInformation("Repository started successfully with indexing disabled");
                    
                    // Get total node count for progress tracking
                    Console.WriteLine("Getting total node count...");
                    var totalNodes = await Providers.Instance.DataStore.GetNodeCountAsync(CancellationToken.None);
                    Console.WriteLine($"Total nodes to index: {totalNodes:N0}");
                    msLogger.LogInformation("Total nodes to index: {TotalNodes}", totalNodes);
                    
                    if (clearActivities)
                    {
                        // APPROACH 1: Clear old indexing activities table for a truly clean rebuild
                        Console.WriteLine("Clearing old indexing activities...");
                        msLogger.LogInformation("Starting to clear old indexing activities");
                        try
                        {
                            if (Providers.Instance.DataProvider is SenseNet.ContentRepository.Storage.Data.RelationalDataProviderBase relationalProvider)
                            {
                                using (var ctx = relationalProvider.CreateDataContext(CancellationToken.None))
                                {
                                    // Use your proven approach: TRUNCATE + DBCC CHECKIDENT to fully reset identity
                                    await ctx.ExecuteNonQueryAsync("TRUNCATE TABLE IndexingActivities");
                                    await ctx.ExecuteNonQueryAsync("DBCC CHECKIDENT ('IndexingActivities', RESEED, 1)");
                                    Console.WriteLine("Truncated IndexingActivities table and reset identity seed to 1.");
                                    msLogger.LogInformation("Truncated IndexingActivities table and reset identity seed to 1");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Warning: Cannot clear indexing activities - DataProvider is not relational");
                                msLogger.LogWarning("Cannot clear indexing activities - DataProvider is not relational");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Warning: Could not clear indexing activities: {ex.Message}");
                            msLogger.LogWarning(ex, "Could not clear indexing activities");
                            Console.WriteLine("Continuing with rebuild anyway...");
                        }
                        
                        // ALSO CLEAR INDEX DIRECTORY to remove cached LastActivityId from Lucene index
                        Console.WriteLine("Clearing existing index directory...");
                        msLogger.LogInformation("Starting to clear index directory");
                        try
                        {
                            // Try both the working directory and the app domain base directory
                            var indexDirectories = new[]
                            {
                                Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "LocalIndex"),
                                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "LocalIndex")
                            };

                            var clearedAny = false;
                            foreach (var indexDirectory in indexDirectories)
                            {
                                if (Directory.Exists(indexDirectory))
                                {
                                    var subDirectories = Directory.GetDirectories(indexDirectory);
                                    foreach (var subDir in subDirectories)
                                    {
                                        Directory.Delete(subDir, true);
                                        Console.WriteLine($"Deleted index subdirectory: {Path.GetFileName(subDir)} from {indexDirectory}");
                                        clearedAny = true;
                                    }
                                    msLogger.LogInformation("Cleared {DirectoryCount} index subdirectories from {IndexDirectory}", subDirectories.Length, indexDirectory);
                                }
                            }
                            
                            if (!clearedAny)
                            {
                                Console.WriteLine("No existing index directories found to clear.");
                                msLogger.LogInformation("No existing index directories found to clear");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Warning: Could not clear index directory: {ex.Message}");
                            msLogger.LogWarning(ex, "Could not clear index directory");
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
                        msLogger.LogInformation("Starting index rebuild with clearing activities approach");
                        
                        // Get the index populator to perform a clean rebuild
                        var populator = Providers.Instance.SearchManager.GetIndexPopulator();
                        
                        // Create progress tracker
                        var progressTracker = new IndexingProgressTracker(totalNodes, msLogger);
                        
                        // Set up error handling
                        populator.IndexingError += (sender, eventArgs) =>
                        {
                            var errorMessage = $"Indexing error for {eventArgs.Path}: {eventArgs.Exception?.Message}";
                            Console.WriteLine(errorMessage);
                            msLogger.LogError(eventArgs.Exception, "Indexing error for {Path}", eventArgs.Path);
                        };
                        
                        var indexCount = 0;
                        populator.NodeIndexed += (sender, eventArgs) =>
                        {
                            progressTracker.UpdateProgress(++indexCount);
                        };
                        
                        // Since we cleared the index, now rebuild it from the database
                        await populator.ClearAndPopulateAllAsync(CancellationToken.None, Console.Out);
                        
                        progressTracker.Complete();
                    }
                    else
                    {
                        // APPROACH 2: Clean rebuild without clearing activities table
                        // ClearAndPopulateAllAsync handles indexing engine startup internally
                        Console.WriteLine("Enabling indexing for clean rebuild...");
                        Indexing.IsOuterSearchEngineEnabled = true;
                        Providers.Instance.SearchManager.IsOuterEngineEnabled = true;
                        
                        Console.WriteLine("Rebuilding index from scratch (without clearing activities)...");
                        msLogger.LogInformation("Starting index rebuild without clearing activities approach");
                        
                        // Get the index populator
                        var populator = Providers.Instance.SearchManager.GetIndexPopulator();
                        
                        // Create progress tracker
                        var progressTracker = new IndexingProgressTracker(totalNodes, msLogger);
                        
                        // Set up progress monitoring
                        var indexCount = 0;
                        populator.NodeIndexed += (sender, eventArgs) =>
                        {
                            progressTracker.UpdateProgress(++indexCount);
                        };
                        
                        // Set up error handling
                        populator.IndexingError += (sender, eventArgs) =>
                        {
                            var errorMessage = $"Indexing error for {eventArgs.Path}: {eventArgs.Exception?.Message}";
                            Console.WriteLine(errorMessage);
                            msLogger.LogError(eventArgs.Exception, "Indexing error for {Path}", eventArgs.Path);
                        };
                        
                        // ClearAndPopulateAllAsync will:
                        // 1. Start indexing engine if not running (without processing old activities because IsOuterSearchEngineEnabled was false during startup)
                        // 2. Clear the existing index
                        // 3. Rebuild index from current database state
                        // 4. Commit the changes
                        await populator.ClearAndPopulateAllAsync(CancellationToken.None, Console.Out);
                        
                        progressTracker.Complete();
                    }
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
        
        static void ShowHelp()
        {
            Console.WriteLine("SenseNet Index Rebuilder - Console Application");
            Console.WriteLine("===============================================");
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet run [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --clear-activities    Clear IndexingActivities table before rebuild (original approach)");
            Console.WriteLine("  --help, -h           Show this help message");
            Console.WriteLine();
            Console.WriteLine("Default behavior (no arguments):");
            Console.WriteLine("  Performs clean index rebuild without clearing IndexingActivities table");
            Console.WriteLine("  Uses ClearAndPopulateAllAsync with controlled indexing engine startup");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run                    # Clean rebuild without clearing activities");
            Console.WriteLine("  dotnet run --clear-activities # Clear activities table + rebuild");
            Console.WriteLine("  dotnet run --help            # Show this help");
            Console.WriteLine();
            Console.WriteLine("Both approaches:");
            Console.WriteLine("  - Use IsOuterSearchEngineEnabled=false during startup to prevent old activity processing");
            Console.WriteLine("  - Enable indexing after repository start for clean rebuild");
            Console.WriteLine("  - Provide progress monitoring and error handling");
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