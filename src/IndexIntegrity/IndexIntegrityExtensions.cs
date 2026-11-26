using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SenseNet.ContentRepository.Search.Indexing.Integrity;
using SenseNet.OData.Operations;
using SenseNet.OData.Operations.IndexIntegrity;

namespace SenseNet.Extensions.DependencyInjection;

public static class IndexIntegrityExtensions
{
    public static IServiceCollection AddSenseNetIndexIntegrityChecker(this IServiceCollection services,
        IConfiguration configuration) =>
        AddChecker(services, configuration, static s => s.AddSenseNetIndexIntegrityProvider());

    public static IServiceCollection AddSenseNetIndexIntegrityChecker<TProvider>(this IServiceCollection services,
        IConfiguration configuration) where TProvider : class, IIndexIntegrityProvider =>
        AddChecker(services, configuration, static s => s.AddSenseNetIndexIntegrityProvider<TProvider>());

    private static IServiceCollection AddChecker(IServiceCollection services, IConfiguration configuration,
        Action<IServiceCollection> addProvider)
    {
        var options = new IndexIntegrityOptions();
        configuration.Bind(options);
        if (!options.Enabled)
            return services;
        if (options.Retention < TimeSpan.Zero || options.Timeout <= TimeSpan.Zero ||
            options.MaxRunBytes <= 0 || options.MaxTotalBytes <= 0 || options.MinimumFreeBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(configuration), "Invalid index integrity time or storage limit.");

        addProvider(services);
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.TryAddSingleton(options);
        services.TryAddSingleton<IndexIntegrityFileStore>();
        services.TryAddSingleton<IndexIntegrityWorker>();
        services.AddHostedService(provider => provider.GetRequiredService<IndexIntegrityWorker>());
        services.AddAntiforgery();
        return services.AddSenseNetODataController<IndexIntegrityController>(IndexIntegrityController.RouteName);
    }
}
