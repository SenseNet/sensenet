using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Security.Clients;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace SenseNet.Services.Core.Diagnostics;

public interface IHealthHandler
{
    Task<HealthResponse> GetHealthResponseAsync(HttpContext httpContext);
}
internal class HealthHandler : IHealthHandler
{
    private readonly ILogger<HealthHandler> _logger;

    public HealthHandler(ILogger<HealthHandler> logger)
    {
        _logger = logger;
    }

    public async Task<HealthResponse> GetHealthResponseAsync(HttpContext httpContext)
    {
        try
        {
            var cancel = httpContext.RequestAborted;
            var services = httpContext.RequestServices;

            var repositoryHostedService = (RepositoryHostedService)services.GetService<IEnumerable<IHostedService>>()?
                .FirstOrDefault(x => x.GetType() == typeof(RepositoryHostedService));

            return new HealthResponse
            {
                HealthServiceStatus = "Ready",
                Repository = repositoryHostedService?.RepositoryStatus ?? "not available",
                Database = await HandleDatabaseAsync(services, cancel),
                BlobStorage = await HandleBlobsAsync(services, cancel),
                Index = await HandleSearchAsync(services.GetService<ISearchManager>(), cancel),
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "HealthService error.");
        }

        return HealthResponse.NotAvailable;
    }

    private string GetProviderName<T>(IServiceProvider services) => services.GetService<T>()?.GetType().FullName ?? "not registered";
    private async Task<object> HandleDatabaseAsync(IServiceProvider services, CancellationToken cancel)
    {
        var dataProvider = services.GetService<DataProvider>();

        object config = null;
        object health = null;

        if (dataProvider != null)
        {
            try
            {
                config = dataProvider.GetConfigurationForHealthDashboard();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "HealthService: Error when getting database configuration.");
                config = $"Error when getting database configuration: {e.Message}";
            }

            try
            {
                health = await dataProvider.GetHealthAsync(cancel) ?? "not available";
            }
            catch (Exception e)
            {
                _logger.LogError(e, "HealthService: Error when getting database health.");
                health = $"Error when getting database health: {e.Message}";
            }
        }

        string schemaWriter;
        try
        {
            schemaWriter = dataProvider?.CreateSchemaWriter().GetType().FullName ?? "not registered";
        }
        catch
        {
            schemaWriter = "not available";
        }

        return new
        {
            Providers = new
            {
                Metadata = dataProvider?.GetType().FullName ?? "not registered",
                DataStore = GetProviderName<IDataStore>(services),
                Schema = schemaWriter,
                AccessToken = GetProviderName<IAccessTokenDataProvider>(services),
                ClientStore = GetProviderName<IClientStoreDataProvider>(services),
                ExclusiveLock = GetProviderName<IExclusiveLockDataProvider>(services),
                Packaging = GetProviderName<IPackagingDataProvider>(services),
                SharedLock = GetProviderName<ISharedLockDataProvider>(services),
                Statistics = GetProviderName<IStatisticalDataProvider>(services),
            },
            Configuration = config,
            Health = health
        };
    }
    private async Task<object> HandleBlobsAsync(IServiceProvider services, CancellationToken cancel)
    {
        var blobStorage = services.GetService<IBlobStorage>();

        object config = null;
        object health = null;

        if (blobStorage != null)
        {
            try
            {
                // BlobStorageOptions  BlobStorage.BlobStorageConfig
                config = blobStorage.GetConfigurationForHealthDashboard();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "HealthService: Error when getting database configuration.");
                config = $"Error when getting database configuration: {e.Message}";
            }

            try
            {
                health = await blobStorage.GetHealthAsync(cancel) ?? "not available";
            }
            catch (Exception e)
            {
                _logger.LogError(e, "HealthService: Error when getting database health.");
                health = $"Error when getting database health: {e.Message}";
            }
        }
        
        var blobProviderStore = services.GetService<IBlobProviderStore>();
        string[] blobProviders;
        try
        {
            blobProviders = blobProviderStore?.Keys.ToArray() ?? Array.Empty<string>();
        }
        catch
        {
            blobProviders = Array.Empty<string>();
        }

        return new
        {
            Providers = new
            {
                BlobStorage = blobStorage?.GetType().FullName ?? "not registered",
                BlobStorageMetaData = GetProviderName<IBlobStorageMetaDataProvider>(services),
                BlobProviderFactory = GetProviderName<IExternalBlobProviderFactory>(services),
                BlobProviderSelector = GetProviderName<IBlobProviderSelector>(services),
                BlobProviderStore = blobProviderStore?.GetType().FullName ?? "not registered",
                BlobProviders = blobProviders
            },
            Configuration = config,
            Health = health
        };
    }
    private async Task<object> HandleSearchAsync(ISearchManager searchManager, CancellationToken cancel)
    {
        if (searchManager == null)
            return "not registered.";

        return new
        {
            Providers = new
            {
                Indexing = searchManager.SearchEngine.QueryEngine.GetType().FullName,
                Querying = searchManager.SearchEngine.QueryEngine.GetType().FullName
            },
            Configuration = "coming soon...",
            Health = "coming soon..."
        };
    }
}

public class HealthResponse
{
    internal static readonly HealthResponse NotRegistered = new() { HealthServiceStatus = "Service not registered." };
    internal static readonly HealthResponse NotAvailable = new() { HealthServiceStatus = "Service not available." };

    public string HealthServiceStatus { get; set; }
    public object Repository { get; set; }
    public object Database { get; set; }
    public object BlobStorage { get; set; }
    public object Index { get; set; }
    public object Authentication { get; set; }
}
