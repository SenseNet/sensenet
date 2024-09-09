using Microsoft.Extensions.DependencyInjection;
using SenseNet.Storage;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection;

public static class ContentNamingProviderExtensions
{
    /// <summary>
    /// Adds an <c>IContentNamingProvider</c> implementation type to the service collection.
    /// Use this method after calling <c>AddSensenet</c> registration method when the default implementation needs to be replaced.
    /// The default implementation is <c>SenseNet.ContentRepository.CharReplacementContentNamingProvider</c>.
    /// </summary>
    public static IServiceCollection AddContentNamingProvider<T>(this IServiceCollection services) where T : class, IContentNamingProvider
    {
        return services.AddSingleton<IContentNamingProvider, T>();
    }
}