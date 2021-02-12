using Microsoft.Extensions.DependencyInjection;
using SenseNet.Events;
using SenseNet.WebHooks;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class WebHookExtensions
    {
        /// <summary>
        /// Adds the WebHooks component and the required related services.
        /// </summary>
        public static IServiceCollection AddSenseNetWebHooks(this IServiceCollection services)
        {
            services.AddComponent(provider => new WebHookComponent());

            services.AddHttpClient();
            services.AddSenseNetWebHookClient<HttpWebHookClient>();

            services.AddSingleton<IEventProcessor, LocalWebHookProcessor>();
            services.AddSingleton<IWebHookSubscriptionStore, BuiltInWebHookSubscriptionStore>();

            return services;
        }
    }
}
