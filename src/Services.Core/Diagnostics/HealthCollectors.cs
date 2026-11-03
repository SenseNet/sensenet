using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Security.Clients;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search;
using AuthenticationOptions = SenseNet.Services.Core.Authentication.AuthenticationOptions;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.Core.Diagnostics;

// Each operation owns its scope, including when a non-cooperative provider finishes after timeout.
internal abstract class ScopedHealthCollector(IServiceScopeFactory scopes) : IComponentHealthCollector, IComponentHealthDetailsCollector
{
    public abstract string ComponentName { get; }
    public async Task<HealthResult> CollectAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        using var account = new SystemAccount();
        return await CollectAsync(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
    }
    public async Task<HealthDetailsResult> CollectDetailsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        using var account = new SystemAccount();
        return await DetailsAsync(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
    }
    protected abstract Task<HealthResult> CollectAsync(IServiceProvider services, CancellationToken cancel);
    protected abstract Task<HealthDetailsResult> DetailsAsync(IServiceProvider services, CancellationToken cancel);
    protected static T Required<T>(IServiceProvider services) => services.GetService<T>() ??
        throw new HealthCollectionException(HealthCheckState.Missing, "Health provider is not registered: " + typeof(T).Name);
    protected static string Name<T>(IServiceProvider services) => services.GetService<T>()?.GetType().FullName ?? "not registered";
}

internal sealed class DatabaseHealthCollector(IServiceScopeFactory scopes) : ScopedHealthCollector(scopes)
{
    public override string ComponentName => "Database";
    protected override Task<HealthResult> CollectAsync(IServiceProvider services, CancellationToken cancel) =>
        Required<DataProvider>(services).GetHealthAsync(cancel);
    protected override Task<HealthDetailsResult> DetailsAsync(IServiceProvider services, CancellationToken cancel)
    {
        var provider = Required<DataProvider>(services);
        return Task.FromResult(new HealthDetailsResult(new
        {
            Providers = new
            {
                Metadata = provider.GetType().FullName, DataStore = Name<IDataStore>(services),
                Schema = provider.CreateSchemaWriter()?.GetType().FullName,
                AccessToken = Name<IAccessTokenDataProvider>(services), ClientStore = Name<IClientStoreDataProvider>(services),
                ExclusiveLock = Name<IExclusiveLockDataProvider>(services), Packaging = Name<IPackagingDataProvider>(services),
                SharedLock = Name<ISharedLockDataProvider>(services), Statistics = Name<IStatisticalDataProvider>(services)
            },
            Configuration = provider.GetConfigurationForHealthDashboard()
        }));
    }
}

internal sealed class BlobStorageHealthCollector(IServiceScopeFactory scopes) : ScopedHealthCollector(scopes)
{
    public override string ComponentName => "BlobStorage";
    protected override Task<HealthResult> CollectAsync(IServiceProvider services, CancellationToken cancel) =>
        Required<IBlobStorage>(services).GetHealthAsync(cancel);
    protected override Task<HealthDetailsResult> DetailsAsync(IServiceProvider services, CancellationToken cancel) =>
        Task.FromResult(new HealthDetailsResult(new
        {
            Providers = new
            {
                BlobStorage = Name<IBlobStorage>(services), BlobStorageMetaData = Name<IBlobStorageMetaDataProvider>(services),
                BlobProviderFactory = Name<IExternalBlobProviderFactory>(services), BlobProviderSelector = Name<IBlobProviderSelector>(services),
                BlobProviderStore = Name<IBlobProviderStore>(services),
                BlobProviders = services.GetService<IBlobProviderStore>()?.Keys.ToArray() ?? Array.Empty<string>()
            },
            Configuration = Required<IBlobStorage>(services).GetConfigurationForHealthDashboard()
        }));
}

internal sealed class SearchHealthCollector(IServiceScopeFactory scopes) : ScopedHealthCollector(scopes)
{
    public override string ComponentName => "Search";
    private static ISearchEngine Engine => Providers.Instance.SearchEngine ??
        throw new HealthCollectionException(HealthCheckState.Missing, "Search engine is not registered.");
    protected override async Task<HealthResult> CollectAsync(IServiceProvider services, CancellationToken cancel)
    {
        var timer = Stopwatch.StartNew();
        var data = await Engine.IndexingEngine.ReadActivityStatusFromIndexAsync(cancel).ConfigureAwait(false);
        return new HealthResult
        {
            Color = data == null ? HealthColor.Yellow : HealthColor.Green, ResponseTime = timer.Elapsed,
            Reason = data == null ? "No indexing activity status returned." : null,
            Method = "Read indexing activity status."
        };
    }
    protected override Task<HealthDetailsResult> DetailsAsync(IServiceProvider services, CancellationToken cancel)
    {
        var engine = Engine;
        var config = engine.GetConfigurationForHealthDashboard();
        var reason = Compare(engine, config);
        return Task.FromResult(new HealthDetailsResult(new
        {
            Providers = new { Indexing = engine.IndexingEngine.GetType().FullName, Querying = engine.QueryEngine.GetType().FullName },
            Configuration = config
        }, reason == null ? HealthColor.Green : HealthColor.Red, reason));
    }
    private static string Compare(ISearchEngine engine, object configuration)
    {
        if (!engine.IndexingEngine.IndexIsCentralized) return null;
        if (configuration is not IDictionary<string, string> config) return "Search server configuration is unavailable.";
        var security = Providers.Instance.SecurityHandler.SecurityContext.SecuritySystem;
        var expected = new Dictionary<string, string>
        {
            ["SearchService_Security_ConnectionString"] = security.DataProvider.ConnectionString,
            ["SearchService_RabbitMq_ServiceUrl"] = Member(security.MessageProvider, "ServiceUrl"),
            ["SearchService_RabbitMq_MessageExchange"] = Member(security.MessageProvider, "MessageExchange")
        };
        var different = expected.Where(pair => !config.TryGetValue(pair.Key, out var actual) || actual != pair.Value)
            .Select(pair => pair.Key).ToArray();
        return different.Length == 0 ? null : "Client/server configuration differs: " + string.Join(", ", different);
    }
    private static string Member(object target, string name) =>
        target?.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)?.GetValue(target)?.ToString() ??
        target?.GetType().GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)?.GetValue(target)?.ToString();
}

internal sealed class IdentityHealthCollector(IServiceScopeFactory scopes) : ScopedHealthCollector(scopes)
{
    public override string ComponentName => "Identity";
    protected override async Task<HealthResult> CollectAsync(IServiceProvider services, CancellationToken cancel)
    {
        var options = services.GetService<IOptions<AuthenticationOptions>>()?.Value;
        var config = services.GetService<IConfiguration>();
        var localOnly = string.Equals(config?["sensenet:Authentication:Local:Mode"], "InternalOnly", StringComparison.OrdinalIgnoreCase);
        var schemes = services.GetService<IAuthenticationSchemeProvider>();
        var hasSchemes = schemes != null && (await schemes.GetAllSchemesAsync().ConfigureAwait(false)).Any();
        if (localOnly || (!hasSchemes && string.IsNullOrWhiteSpace(options?.Authority)))
            throw new HealthCollectionException(HealthCheckState.NotApplicable, "External identity authentication is not used.");
        var authority = string.IsNullOrWhiteSpace(options?.MetadataHost) ? options?.Authority : options.MetadataHost;
        if (string.IsNullOrWhiteSpace(authority))
            throw new HealthCollectionException(HealthCheckState.Missing, "Identity authority is not configured.");
        var url = authority.AddUrlSchema().TrimEnd('/') + "/.well-known/openid-configuration";
        using var client = Required<IHttpClientFactory>(services).CreateClient("SenseNet.Health.Identity");
        client.MaxResponseContentBufferSize = 128 * 1024;
        var timer = Stopwatch.StartNew();
        using var response = await client.GetAsync(url, cancel).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancel).ConfigureAwait(false);
        return new HealthResult
        {
            Color = response.StatusCode == HttpStatusCode.OK && text.Contains("scopes_supported") && text.Contains("sensenet")
                ? HealthColor.Green : HealthColor.Yellow,
            ResponseTime = timer.Elapsed, Method = "Get and validate OpenID configuration.",
            Reason = response.StatusCode == HttpStatusCode.OK ? null : "Unexpected OpenID configuration response status."
        };
    }
    protected override Task<HealthDetailsResult> DetailsAsync(IServiceProvider services, CancellationToken cancel)
    {
        var options = services.GetService<IOptions<AuthenticationOptions>>()?.Value;
        return Task.FromResult(new HealthDetailsResult(new { Configuration = new
        {
            options?.Authority, options?.ClientApplicationUrl, options?.AddJwtCookie, options?.MetadataHost
        } }));
    }
}
