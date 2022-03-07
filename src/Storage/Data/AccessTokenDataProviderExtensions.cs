using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for the AccessToken feature.
    /// </summary>
    public static class AccessTokenDataProviderExtensions
    {
        /// <summary>
        /// Sets the current <see cref="IAccessTokenDataProvider"/> instance that will be responsible
        /// for managing access tokens.
        /// </summary>
        /// <param name="builder">The IRepositoryBuilder instance.</param>
        /// <param name="provider">The extension instance to set.</param>
        /// <returns>The updated IRepositoryBuilder.</returns>
        public static IRepositoryBuilder UseAccessTokenDataProvider(this IRepositoryBuilder builder, IAccessTokenDataProvider provider)
        {
            Providers.Instance.SetProvider(typeof(IAccessTokenDataProvider), provider);
            return builder;
        }
    }
}
