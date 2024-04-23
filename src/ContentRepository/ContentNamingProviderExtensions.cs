using Microsoft.Extensions.DependencyInjection;
using SenseNet.Storage;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection;

public static class ContentNamingProviderExtensions
{
    public static IServiceCollection AddContentNamingProvider<T>(this IServiceCollection services) where T : class, IContentNamingProvider
    {
        return services.AddSingleton<IContentNamingProvider, T>();
    }
}