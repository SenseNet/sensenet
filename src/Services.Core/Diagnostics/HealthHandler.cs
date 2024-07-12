using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SenseNet.ContentRepository.Security.Clients;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.Search;
using SenseNet.Storage.Diagnostics;
using System.Diagnostics;
using System.Net.Http;
using SenseNet.Services.Core.Authentication;
using AngleSharp.Io;
using System.Drawing;
using System.Net;
using Org.BouncyCastle.Tls;
using SenseNet.ContentRepository;
using SenseNet.Storage.Security;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.Core.Diagnostics;

public interface IHealthHandler
{
    Task<object> GetHealthResponseAsync(HttpContext httpContext);
}

internal class HealthHandler : IHealthHandler
{
    private readonly ILogger<HealthHandler> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public HealthHandler(ILogger<HealthHandler> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<object> GetHealthResponseAsync(HttpContext httpContext)
    {
        if (User.Current != HealthCheckerUser.Instance)
            return new{Message= "HealthService is unavailable.", Reason="Access denied."};

        Exception error;
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
                GetIdentityHealthAsync(services, cancel)
            };
            Task.WaitAll(gettingHealthTasks, cancel);

            var gettingHealthResults = gettingHealthTasks.Select(x => x.Result).ToArray();
            var overallColor = gettingHealthResults.Max(x => x?.Color ?? HealthColor.Green).ToString();

            return new
            {
                Repository_Status = repositoryStatus == null
                    ? (object) "status not available"
                    : new
                    {
                        Running = repositoryStatus.IsRunning,
                        Status = repositoryStatus.Current,
                    },
                Health = new
                {
                    Color = overallColor,
                    Database = gettingHealthTasks[0].Result,
                    BlobStorage = gettingHealthTasks[1].Result,
                    Search = gettingHealthTasks[2].Result,
                    Identity = gettingHealthTasks[3].Result,
                },
                Details = new
                {
                    HealthServiceStatus = "Ready",
                    Database = GetDatabaseDetails(services),
                    BlobStorage = GetBlobsDetails(services),
                    Search = GetSearchDetails(services),
                    Identity = GetIdentityDetails(services),
                    Repository_StatusHistory = repositoryStatus?.GetLog(),
                }
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "HealthService error.");
            error = e;
        }

        return error;
    }

    private string GetProviderName<T>(IServiceProvider services) =>
        services.GetService<T>()?.GetType().FullName ?? "not registered";

    private async Task<HealthResult> GetDatabaseHealthAsync(IServiceProvider services, CancellationToken cancel)
    {
        var dataProvider = services.GetService<DataProvider>();

        HealthResult health = null;

        if (dataProvider != null)
        {
            try
            {
                health = await dataProvider.GetHealthAsync(cancel);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "HealthService: Error when getting database health.");
                health = new HealthResult
                {
                    Color = HealthColor.Red,
                    Reason = $"Error when getting database health: {e.Message}",
                };
            }
        }

        return health;
    }

    private async Task<HealthResult> GetBlobsHealthAsync(IServiceProvider services, CancellationToken cancel)
    {
        var blobStorage = services.GetService<IBlobStorage>();

        HealthResult health = null;

        if (blobStorage != null)
        {
            try
            {
                health = await blobStorage.GetHealthAsync(cancel);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "HealthService: Error when getting blobs health.");
                health = new HealthResult
                {
                    Color = HealthColor.Red,
                    Reason = $"Error when getting blobs health: {e.Message}"
                };
            }
        }

        return health;
    }

    private async Task<HealthResult> GetSearchHealthAsync(IServiceProvider services, CancellationToken cancel)
    {
        var searchEngine = Providers.Instance.SearchEngine;

        HealthResult health = null;

        if (searchEngine != null)
        {
            try
            {
                var healthResult = await GetSearchHealthAsync(searchEngine, cancel);
                var configComparisonMessage = CompareSearchConfigurations(services);

                var color = (string.IsNullOrEmpty(configComparisonMessage)) ? healthResult.Color : HealthColor.Red;
                var method = healthResult.Method;
                if (configComparisonMessage != null)
                    method += " Comparison client and server configuration.";
                var reason = healthResult.Reason;
                if (configComparisonMessage != null && configComparisonMessage.Length > 0)
                {
                    var configMessage = "Configuration problems: " + configComparisonMessage;
                    if (string.IsNullOrEmpty(reason))
                        reason = configMessage;
                    else
                        reason += " " + configMessage;
                }

                health = new HealthResult
                {
                    Color = color,
                    ResponseTime = healthResult.ResponseTime,
                    Reason = reason,
                    Method = method
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "HealthService: Error when getting search health.");
                health = new HealthResult
                {
                    Color = HealthColor.Red,
                    Reason = $"Error when getting search health: {e.Message}",
                };
            }
        }

        return health;

    }
    private Task<HealthResult> GetSearchHealthAsync(
        ISearchEngine searchEngine, CancellationToken cancel)
    {
        IDictionary<string, string> data = null;
        string error = null;

        var result = new HealthResult();

        try
        {
            var timer = Stopwatch.StartNew();
            data = searchEngine.IndexingEngine.GetIndexDocumentByVersionId(1);
            timer.Stop();
            result.ResponseTime = timer.Elapsed;
        }
        catch (Exception e)
        {
            error = e.Message;
        }

        if (error != null)
        {
            result.Color = HealthColor.Red;
            result.Reason = $"ERROR: {error}";
            result.Method = "SearchManager (InProc) tries to get index document by VersionId 1.";
        }
        else if (data == null)
        {
            result.Color = HealthColor.Yellow;
            result.Reason = "No data result.";
            result.Method = "SearchManager (InProc) tries to get index document by VersionId 1.";
        }
        else
        {
            result.Color = HealthColor.Green;
            result.Method =
                "Measure the time of getting the index document by VersionId 1 in secs from SearchManager (InProc).";
        }

        return System.Threading.Tasks.Task.FromResult(result);
    }
    private string CompareSearchConfigurations(IServiceProvider services)
    {
        var searchEngine = Providers.Instance.SearchEngine;
        if (searchEngine == null)
            return null;
        if (!searchEngine.IndexingEngine.IndexIsCentralized)
            return null;

        var config = (IDictionary<string, string>) searchEngine.GetConfigurationForHealthDashboard();
        config.TryGetValue("SearchService_Security_ConnectionString", out var serverSecurityConnectionString);
        config.TryGetValue("SearchService_RabbitMq_ServiceUrl", out var serverSecurityRabbitMqServiceUrl);
        config.TryGetValue("SearchService_RabbitMq_MessageExchange", out var serverSecurityRabbitMqMessageExchange);

        var securitySystem = Providers.Instance.SecurityHandler.SecurityContext.SecuritySystem;
        var clientSecurityConnectionString = securitySystem.DataProvider.ConnectionString;
        var clientSecurityRabbitMqServiceUrl =
            GetStringFieldOrPropertyValue(securitySystem.MessageProvider, "ServiceUrl");
        var clientSecurityRabbitMqMessageExchange =
            GetStringFieldOrPropertyValue(securitySystem.MessageProvider, "MessageExchange");

        var messages = new List<string>();
        if (serverSecurityConnectionString != clientSecurityConnectionString)
            messages.Add("different security connectionString");
        if (serverSecurityRabbitMqServiceUrl != clientSecurityRabbitMqServiceUrl)
            messages.Add("different ServiceUrl of the security RabbitMq");
        if (serverSecurityRabbitMqMessageExchange != clientSecurityRabbitMqMessageExchange)
            messages.Add("different MessageExchange of the security RabbitMq");
        return string.Join(", ", messages);
    }
    private string GetStringFieldOrPropertyValue(object target, string fieldOrPropertyName)
    {
        var fields = target.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        var field = fields.FirstOrDefault(x => x.Name == fieldOrPropertyName);
        if (field != null)
            return field.GetValue(target)?.ToString();

        var properties = target.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
        var property = properties.FirstOrDefault(x => x.Name == fieldOrPropertyName);
        if (property != null)
            return property.GetValue(target)?.ToString();

        return null;
    }

    private async Task<HealthResult> GetIdentityHealthAsync(IServiceProvider services, CancellationToken cancel)
    {
        var authOptions = services.GetService<IOptions<AuthenticationOptions>>()?.Value;
        HealthResult health = null;

        if (authOptions != null)
        {
            try
            {
                health = await GetIdentityHealthAsync(authOptions, cancel);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "HealthService: Error when getting identity server health.");
                health = new HealthResult
                {
                    Color = HealthColor.Red,
                    Reason = $"Error when getting identity server health: {e.Message}",
                };
            }
        }

        return health;
    }
    private async Task<HealthResult> GetIdentityHealthAsync(AuthenticationOptions options, CancellationToken cancel)
    {
        var timeout = TimeSpan.FromSeconds(4);
        var combinedCancel = CancellationTokenSource.CreateLinkedTokenSource(
            new CancellationTokenSource(timeout).Token, cancel).Token;

        HttpResponseMessage response = null;
        string error = null;
        TimeSpan? elapsed = null;
        var timer = Stopwatch.StartNew();

        try
        {
            var url = options.Authority.TrimEnd('/') + "/.well-known/openid-configuration";
            var client = _httpClientFactory.CreateClient();
            response = await client.GetAsync(url, combinedCancel).ConfigureAwait(false);
            elapsed = timer.Elapsed;
        }
        catch (TaskCanceledException ee)
        {
            elapsed = timer.Elapsed;
            error = elapsed > timeout ? $"Response timeout reached ({timeout})." : ee.Message;
        }
        catch (Exception e)
        {
            error = e.Message;
        }
        timer.Stop();

        if (response == null || error != null)
        {
            return new HealthResult
            {
                Color = HealthColor.Red,
                Reason = $"{(response == null ? "No response. " : string.Empty)}Error: '{error}'",
                Method = "Trying to get /.well-known/openid-configuration."
            };
        }

        if (response.StatusCode == HttpStatusCode.OK)
        {
#if NET6_0_OR_GREATER
            var text = await response.Content.ReadAsStringAsync(cancel);
#else
            var text = await response.Content.ReadAsStringAsync();
#endif
            if (text.Contains("scopes_supported") && text.Contains("sensenet"))
            {
                return new HealthResult
                {
                    Color = HealthColor.Green,
                    ResponseTime = elapsed,
                    Method = "Getting and checking /.well-known/openid-configuration."
                };
            }
            return new HealthResult
            {
                Color = HealthColor.Yellow,
                ResponseTime = elapsed,
                Reason = "Not recognized response.",
                Method = "Getting and checking /.well-known/openid-configuration."
            };
        }

        return new HealthResult
        {
            Color = HealthColor.Yellow,
            ResponseTime = elapsed,
            Reason = $"Response status is {(int)response.StatusCode} {response.StatusCode}, expected: {(int)HttpStatusCode.OK} {HttpStatusCode.OK}",
            Method = "Getting and checking /.well-known/openid-configuration."
        };
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
        var searchEngine = Providers.Instance.SearchEngine;
        if (searchEngine == null)
            return "not registered.";

        object config = null;
        try
        {
            config = searchEngine.GetConfigurationForHealthDashboard();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "HealthService: Error when getting 'search' configuration.");
            config = $"Error when getting 'search' configuration: {e.Message}";
        }

        return new
        {
            Providers = new
            {
                Indexing = searchEngine.QueryEngine.GetType().FullName,
                Querying = searchEngine.QueryEngine.GetType().FullName
            },
            Configuration = config,
        };
    }

    private object GetIdentityDetails(IServiceProvider services)
    {
        object config;
        var authOptions = services.GetService<IOptions<AuthenticationOptions>>()?.Value;
        if (authOptions == null)
        {
            config = "not configured";
        }
        else
        {
            config = new
            {
                authOptions.Authority,
                authOptions.ClientApplicationUrl,
                authOptions.AddJwtCookie,
                authOptions.MetadataHost,
            };
        }

        return new
        {
            Configuration = config,
        };
    }
}
