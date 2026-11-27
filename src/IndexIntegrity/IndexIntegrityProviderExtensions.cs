using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SenseNet.ContentRepository.Search.Indexing.Integrity;

namespace SenseNet.Extensions.DependencyInjection;

public static class IndexIntegrityProviderExtensions
{
    public static IServiceCollection AddSenseNetIndexIntegrityProvider(this IServiceCollection services)
    {
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.TryAddSingleton<IndexIntegrityCheckService>();
        services.TryAddSingleton<IndexIntegrityRepairService>();
        return services.AddSenseNetIndexIntegrityProvider<DefaultIndexIntegrityProvider>();
    }

    public static IServiceCollection AddSenseNetIndexIntegrityProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, IIndexIntegrityProvider
    {
        services.RemoveAll<IIndexIntegrityProvider>();
        return services.AddSingleton<IIndexIntegrityProvider, TProvider>();
    }
}
