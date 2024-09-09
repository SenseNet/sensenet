using Microsoft.Extensions.DependencyInjection;
using SenseNet.WebHooks;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class WebHookExtensions
    {
        /// <summary>
        /// Adds a webhook client service as singleton.
        /// Use this method when the default implementation (<c>SenseNet.WebHooks.HttpWebHookClient</c>) needs to be replaced.
        /// </summary>
        public static IServiceCollection AddSenseNetWebHookClient<T>(this IServiceCollection services) where T : class, IWebHookClient
        {
            services.AddSingleton<IWebHookClient, T>();

            return services;
        }
    }
}
