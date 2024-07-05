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
using SenseNet.Storage.Diagnostics;

namespace SenseNet.Services.Core.Diagnostics;

internal class HealthResponse
{
    internal static readonly object NotRegistered = new { HealthServiceStatus = "Service not registered." };
    internal static readonly object NotAvailable = new { HealthServiceStatus = "Service not available." };
}

public interface IHealthHandler
{
    Task<object> GetHealthResponseAsync(HttpContext httpContext);
}
internal class HealthHandler : IHealthHandler
{
    private readonly ILogger<HealthHandler> _logger;

    public HealthHandler(ILogger<HealthHandler> logger)
    {
        _logger = logger;
    }

    public async Task<object> GetHealthResponseAsync(HttpContext httpContext)
    {
        try
        {
            var cancel = httpContext.RequestAborted;
            var services = httpContext.RequestServices;
            var repositoryStatus = services.GetService<ISenseNetStatus>();

            var gettingHealthTasks = new[]
            {
                GetDatabaseHealthAsync(services, cancel),
                GetBlobsHealthAsync(services, cancel),
                GetSearchHealthAsync(services, cancel),
            };
            Task.WaitAll(gettingHealthTasks, cancel);

            return new
            {
                Repository_Status = repositoryStatus == null ? (object)"status not available" : new
                {
                    Running = repositoryStatus.IsRunning,
                    Status = repositoryStatus.Current,
                },
                Health = new
                {
                    Database = gettingHealthTasks[0].Result,
                    BlobStorage = gettingHealthTasks[1].Result,
                    Index = gettingHealthTasks[2].Result,
                },
                Details = new
                {
                    HealthServiceStatus = "Ready",
                    Database = GetDatabaseDetails(services),
                    BlobStorage = GetBlobsDetails(services),
                    Index = GetSearchDetails(services),
                    Repository_StatusHistory = repositoryStatus?.GetLog(),
                }
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "HealthService error.");
        }

        return HealthResponse.NotAvailable;
    }

    private string GetProviderName<T>(IServiceProvider services) => services.GetService<T>()?.GetType().FullName ?? "not registered";
    private async Task<object> GetDatabaseHealthAsync(IServiceProvider services, CancellationToken cancel)
    {
        var dataProvider = services.GetService<DataProvider>();

        object health = null;

        if (dataProvider != null)
        {
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

        return health;
    }
    private async Task<object> GetBlobsHealthAsync(IServiceProvider services, CancellationToken cancel)
    {
        var blobStorage = services.GetService<IBlobStorage>();

        object health = null;

        if (blobStorage != null)
        {
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

        return health;
    }
    private async Task<object> GetSearchHealthAsync(IServiceProvider services, CancellationToken cancel)
    {
        var searchManager = services.GetService<ISearchManager>();

        object health = null;

        if (searchManager != null)
        {
            try
            {
                health = await searchManager.GetHealthAsync(cancel) ?? "not available";
            }
            catch (Exception e)
            {
                _logger.LogError(e, "HealthService: Error when getting search health.");
                health = $"Error when getting search health: {e.Message}";
            }
        }

        return health;

    }

    private object GetDatabaseDetails(IServiceProvider services)
    {
        var dataProvider = services.GetService<DataProvider>();

        object config = null;

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
        };
    }
    private object GetBlobsDetails(IServiceProvider services)
    {
        var blobStorage = services.GetService<IBlobStorage>();

        object config = null;

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
        };
    }
    private object GetSearchDetails(IServiceProvider services)
    {
        var searchManager = services.GetService<ISearchManager>();
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
        };
    }
}
