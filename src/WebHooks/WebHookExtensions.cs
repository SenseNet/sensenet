using Microsoft.Extensions.DependencyInjection;
using SenseNet.Events;
using SenseNet.WebHooks;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class WebHookExtensions
    {
        public static IServiceCollection AddSenseNetWebHooks(this IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddSenseNetWebHookClient<HttpWebHookClient>();

            services.AddSingleton<IEventProcessor, LocalWebHookProcessor>();

            services.AddSingleton<IWebHookFilter, BuiltInWebHookFilter>();

            return services;
        }
    }
}
